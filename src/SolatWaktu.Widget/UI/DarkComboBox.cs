using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SolatWaktu.Widget.Services;
using SolatWaktu.Widget.Theme;

namespace SolatWaktu.Widget.UI;

/// <summary>
/// A fully owner-drawn dropdown that matches the dark widget UI. Native
/// ComboBox controls keep the system arrow button light even in dark mode, so
/// this control uses a flat button plus a dark context menu instead.
/// </summary>
internal sealed class DarkComboBox : Control
{
    private readonly ContextMenuStrip _menu;
    private readonly List<string> _items = [];
    private int _selectedIndex = -1;
    private bool _hover;
    private bool _open;

    public DarkComboBox()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.UserPaint
            | ControlStyles.Selectable,
            true);

        BackColor = Color.FromArgb(32, 32, 51);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9.5f);
        Cursor = Cursors.Hand;
        TabStop = true;

        _menu = DarkMenuFactory.Create();
        _menu.ShowImageMargin = false;
        _menu.ItemClicked += (_, e) =>
        {
            if (e.ClickedItem?.Tag is int index)
            {
                SelectedIndex = index;
            }
        };
        _menu.Closed += (_, _) =>
        {
            _open = false;
            Invalidate();
        };
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<string> Items => _items;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var index = _items.Count == 0 ? -1 : Math.Clamp(value, -1, _items.Count - 1);
            if (index == _selectedIndex)
            {
                return;
            }

            _selectedIndex = index;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? SelectedItem =>
        _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null;

    public event EventHandler? SelectedIndexChanged;

    public void SetItems(IEnumerable<string> items)
    {
        _items.Clear();
        _items.AddRange(items);

        _menu.Items.Clear();
        for (var index = 0; index < _items.Count; index++)
        {
            var item = new ToolStripMenuItem(_items[index])
            {
                Tag = index,
                ForeColor = Color.FromArgb(236, 240, 255),
                Padding = new Padding(6, 3, 6, 3),
                Checked = index == _selectedIndex
            };
            _menu.Items.Add(item);
        }

        if (_selectedIndex >= _items.Count)
        {
            _selectedIndex = _items.Count - 1;
        }

        Invalidate();
    }

    protected override bool IsInputKey(Keys keyData) =>
        keyData is Keys.Down or Keys.Up or Keys.Space or Keys.Enter || base.IsInputKey(keyData);

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        OpenMenu();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyCode is Keys.Down or Keys.Space or Keys.Enter)
        {
            OpenMenu();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Up)
        {
            SelectedIndex--;
            e.Handled = true;
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hover = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hover = false;
        Invalidate();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using var path = DrawingHelper.RoundedRect(rect, 8f);

        var background = !Enabled
            ? Color.FromArgb(26, 26, 42)
            : _hover || _open
                ? Color.FromArgb(41, 41, 64)
                : Color.FromArgb(32, 32, 51);

        using (var fill = new SolidBrush(background))
        {
            graphics.FillPath(fill, path);
        }

        var borderColor = Focused
            ? Color.FromArgb(170, 0, 212, 170)
            : Color.FromArgb(_hover ? 74 : 54, 255, 255, 255);
        using (var border = new Pen(borderColor, 1f))
        {
            graphics.DrawPath(border, path);
        }

        var textColor = Enabled
            ? Color.FromArgb(238, 243, 252)
            : Color.FromArgb(120, 220, 226, 240);
        using var textBrush = new SolidBrush(textColor);
        using var near = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };

        graphics.DrawString(
            SelectedItem ?? string.Empty,
            Font,
            textBrush,
            new RectangleF(10, 0, Math.Max(0, Width - 34), Height),
            near);

        using var iconFont = new Font(AppResources.IconFontFamily, 8f);
        using var chevronBrush = new SolidBrush(Enabled
            ? Color.FromArgb(200, 226, 235, 250)
            : Color.FromArgb(90, 220, 226, 240));
        using var far = new StringFormat
        {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Center
        };
        graphics.DrawString(
            "\uE70D",
            iconFont,
            chevronBrush,
            new RectangleF(0, 0, Width - 10, Height),
            far);
    }

    private void OpenMenu()
    {
        if (!Enabled || _items.Count == 0)
        {
            return;
        }

        foreach (ToolStripItem item in _menu.Items)
        {
            if (item is ToolStripMenuItem menuItem)
            {
                menuItem.Checked = (int?)menuItem.Tag == _selectedIndex;
            }
        }

        // ContextMenuStrip sizes items to their text by default. Force the items
        // to fill the custom-width menu so the whole row is clickable.
        var itemWidth = Math.Max(120, Width - 6);
        foreach (ToolStripItem item in _menu.Items)
        {
            item.AutoSize = false;
            item.Size = new Size(itemWidth, 26);
            item.Margin = Padding.Empty;
        }

        _menu.MinimumSize = new Size(Width, 0);
        _open = true;
        Invalidate();
        _menu.Show(this, new Point(0, Height));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _menu.Dispose();
        }

        base.Dispose(disposing);
    }
}

using System.Drawing;
using System.Windows.Forms;
using SolatWaktu.Core.Models;
using SolatWaktu.Core.Services;
using SolatWaktu.Widget.Localization;
using SolatWaktu.Widget.Services;

namespace SolatWaktu.Widget.UI;

internal sealed class ZonePickerForm : Form
{
    private readonly ZoneCatalog _catalog;
    private readonly TextBox _searchBox;
    private readonly ListBox _zoneList;
    private readonly Label _statusLabel;
    private readonly Button _okButton;
    private readonly Button _cancelButton;
    private readonly List<Zone> _filtered = [];

    public ZonePickerForm(ZoneCatalog catalog, string? selectedZoneCode)
    {
        _catalog = catalog;
        SelectedZone = catalog.Find(selectedZoneCode) ?? catalog.Find("WLY01");

        Text = AppText.Get("zone.title");
        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(21, 21, 34);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9.5f);
        ClientSize = new Size(520, 600);

        var title = new Label
        {
            Text = AppText.Get("zone.title"),
            Font = new Font("Segoe UI", 17f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(24, 20)
        };

        var subtitle = new Label
        {
            Text = AppText.Get("zone.subtitle"),
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(170, 225, 232, 245),
            AutoSize = true,
            Location = new Point(26, 56)
        };

        _searchBox = new TextBox
        {
            PlaceholderText = AppText.Get("zone.search"),
            Location = new Point(24, 88),
            Width = 472,
            Font = new Font("Segoe UI", 10.5f),
            BackColor = Color.FromArgb(32, 32, 51),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        _searchBox.TextChanged += (_, _) => ApplyFilter();

        _zoneList = new ListBox
        {
            Location = new Point(24, 130),
            Size = new Size(472, 360),
            DrawMode = DrawMode.OwnerDrawFixed,
            BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(27, 27, 43),
            ForeColor = Color.White,
            IntegralHeight = false,
            Font = new Font("Segoe UI", 10f)
        };
        _zoneList.DrawItem += ZoneList_DrawItem;
        _zoneList.DoubleClick += (_, _) => AcceptSelection();
        _zoneList.SelectedIndexChanged += (_, _) => UpdateStatus();

        _statusLabel = new Label
        {
            AutoSize = false,
            Location = new Point(26, 504),
            Size = new Size(230, 30),
            ForeColor = Color.FromArgb(165, 225, 232, 245),
            Font = new Font("Segoe UI", 8.8f),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _okButton = CreateDialogButton(AppText.Get("common.select"), Color.FromArgb(0, 180, 140));
        _okButton.Location = new Point(266, 528);
        _okButton.Size = new Size(112, 38);
        _okButton.Click += (_, _) => AcceptSelection();

        _cancelButton = CreateDialogButton(AppText.Get("common.cancel"), Color.FromArgb(43, 43, 64));
        _cancelButton.Location = new Point(388, 528);
        _cancelButton.Size = new Size(108, 38);
        _cancelButton.DialogResult = DialogResult.Cancel;

        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        Controls.AddRange([title, subtitle, _searchBox, _zoneList, _statusLabel, _okButton, _cancelButton]);

        ApplyFilter();
        Shown += (_, _) =>
        {
            _searchBox.Focus();
            _zoneList.ItemHeight = (int)Math.Round(50 * (DeviceDpi / 96f));
            SelectCurrent();
        };
    }

    public Zone? SelectedZone { get; private set; }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        NativeMethods.EnableDarkTitleBar(Handle);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        DialogPlacement.CenterOnOwnerScreen(this);
    }

    private void ApplyFilter()
    {
        var selectedCode = SelectedZone?.JakimCode;
        _filtered.Clear();
        _filtered.AddRange(_catalog.Search(_searchBox.Text));

        _zoneList.BeginUpdate();
        _zoneList.Items.Clear();
        foreach (var zone in _filtered)
        {
            _zoneList.Items.Add(zone);
        }
        _zoneList.EndUpdate();

        if (_filtered.Count == 0)
        {
            _statusLabel.Text = AppText.Get("zone.none");
            _okButton.Enabled = false;
            return;
        }

        _okButton.Enabled = true;
        var index = selectedCode is null
            ? 0
            : _filtered.FindIndex(zone =>
                string.Equals(zone.JakimCode, selectedCode, StringComparison.OrdinalIgnoreCase));
        _zoneList.SelectedIndex = index >= 0 ? index : 0;
        UpdateStatus();
    }

    private void SelectCurrent()
    {
        if (SelectedZone is null)
        {
            return;
        }

        var index = _filtered.FindIndex(zone =>
            string.Equals(zone.JakimCode, SelectedZone.JakimCode, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            _zoneList.SelectedIndex = index;
            _zoneList.TopIndex = Math.Max(0, index - 2);
        }
    }

    private void UpdateStatus()
    {
        _statusLabel.Text = _filtered.Count == 0
            ? AppText.Get("zone.none")
            : AppText.Format("zone.found", _filtered.Count);
    }

    private void AcceptSelection()
    {
        if (_zoneList.SelectedItem is Zone zone)
        {
            SelectedZone = zone;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    private void ZoneList_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _zoneList.Items.Count)
        {
            return;
        }

        var zone = (Zone)_zoneList.Items[e.Index];
        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

        using var background = new SolidBrush(selected
            ? Color.FromArgb(50, 50, 78)
            : Color.FromArgb(27, 27, 43));
        e.Graphics.FillRectangle(background, e.Bounds);

        if (selected)
        {
            using var accent = new SolidBrush(Color.FromArgb(0, 212, 170));
            e.Graphics.FillRectangle(accent, e.Bounds.X, e.Bounds.Y + 10, 3, e.Bounds.Height - 20);
        }

        var scale = DeviceDpi / 96f;
        var left = e.Bounds.X + (int)Math.Round(16 * scale);
        var width = e.Bounds.Width - (int)Math.Round(30 * scale);

        using var titleFont = new Font("Segoe UI", 10.4f, FontStyle.Bold);
        using var detailFont = new Font("Segoe UI", 8.6f);
        using var titleBrush = new SolidBrush(Color.White);
        using var detailBrush = new SolidBrush(Color.FromArgb(155, 210, 220, 238));

        var titleRect = new Rectangle(left, e.Bounds.Y + (int)Math.Round(7 * scale), width, (int)Math.Round(22 * scale));
        var detailRect = new Rectangle(left, e.Bounds.Y + (int)Math.Round(27 * scale), width, (int)Math.Round(18 * scale));

        var flags = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter;
        TextRenderer.DrawText(e.Graphics, MalayText.ShortZoneName(zone), titleFont, titleRect, Color.White, flags);
        TextRenderer.DrawText(
            e.Graphics,
            $"{zone.Negeri}  •  {zone.JakimCode}",
            detailFont,
            detailRect,
            Color.FromArgb(160, 210, 220, 238),
            flags);
    }

    private static Button CreateDialogButton(string text, Color backColor)
    {
        return new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            BackColor = backColor,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            FlatAppearance = { BorderSize = 0 },
            UseVisualStyleBackColor = false,
            TabStop = true
        };
    }
}

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SolatWaktu.Widget.UI;

/// <summary>
/// A dark-theme checkbox with an explicit tick mark. The native flat checkbox
/// renders a white box with a near-white check on dark backgrounds, which makes
/// checked and unchecked states almost indistinguishable.
/// </summary>
internal sealed class DarkCheckBox : CheckBox
{
    private const int BoxSize = 16;
    private const int BoxLeft = 2;

    private bool _hover;

    public DarkCheckBox()
    {
        SetStyle(
            ControlStyles.UserPaint
            | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw,
            true);

        AutoSize = false;
        Height = 24;
        Cursor = Cursors.Hand;
        BackColor = Color.Transparent;
        ForeColor = Color.FromArgb(232, 238, 250);
        Font = new Font("Segoe UI", 9.3f);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using (var background = new SolidBrush(ResolveBackground()))
        {
            graphics.FillRectangle(background, ClientRectangle);
        }

        var enabled = Enabled;
        var scale = DeviceDpi / 96f;
        var boxSize = (int)Math.Round(BoxSize * scale);
        var boxLeft = (int)Math.Round(BoxLeft * scale);
        var box = new Rectangle(boxLeft, (Height - boxSize) / 2, boxSize, boxSize);
        var accent = Color.FromArgb(0, 190, 140);
        var fill = !enabled
            ? Color.FromArgb(30, 30, 46)
            : Checked
                ? accent
                : Color.FromArgb(28, 28, 44);
        var border = !enabled
            ? Color.FromArgb(115, 60, 60, 82)
            : Checked
                ? Color.FromArgb(0, 212, 170)
                : _hover
                    ? Color.FromArgb(205, 120, 120, 155)
                    : Color.FromArgb(145, 92, 92, 120);

        using (var path = RoundedRect(box, Math.Max(2, (int)Math.Round(4 * scale))))
        {
            using (var fillBrush = new SolidBrush(fill))
            {
                graphics.FillPath(fillBrush, path);
            }

            using var borderPen = new Pen(border, 1f);
            graphics.DrawPath(borderPen, path);
        }

        if (Checked)
        {
            using var tickPen = new Pen(
                enabled ? Color.White : Color.FromArgb(125, 190, 196, 214),
                1.8f * scale)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };

            graphics.DrawLines(tickPen,
            [
                new PointF(box.Left + 3.5f * scale, box.Top + 8f * scale),
                new PointF(box.Left + 6.5f * scale, box.Top + 11f * scale),
                new PointF(box.Left + 12.5f * scale, box.Top + 4.5f * scale)
            ]);
        }

        if (Focused && enabled)
        {
            using var focusPen = new Pen(Color.FromArgb(150, 0, 212, 170), 1f)
            {
                DashStyle = DashStyle.Dot
            };
            graphics.DrawRectangle(
                focusPen,
                box.Left - 2 * scale,
                box.Top - 2 * scale,
                box.Width + 3 * scale,
                box.Height + 3 * scale);
        }

        var textColor = enabled ? ForeColor : Color.FromArgb(130, 180, 190, 210);
        var textLeft = box.Right + (int)Math.Round(7 * scale);
        var textRect = new Rectangle(
            textLeft,
            0,
            Math.Max(0, Width - textLeft),
            Height);

        TextRenderer.DrawText(
            graphics,
            Text,
            Font,
            textRect,
            textColor,
            TextFormatFlags.Left
            | TextFormatFlags.VerticalCenter
            | TextFormatFlags.NoPrefix
            | TextFormatFlags.EndEllipsis);
    }

    protected override void OnCheckedChanged(EventArgs e)
    {
        base.OnCheckedChanged(e);
        Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        Invalidate();
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

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        Invalidate();
    }

    private Color ResolveBackground()
    {
        Control? control = Parent;
        while (control is not null && control.BackColor.A < 255)
        {
            control = control.Parent;
        }

        return control?.BackColor ?? Color.FromArgb(21, 21, 34);
    }

    private static GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

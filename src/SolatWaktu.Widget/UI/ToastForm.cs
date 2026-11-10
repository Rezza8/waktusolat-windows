using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SolatWaktu.Core.Services;
using SolatWaktu.Widget.Localization;
using SolatWaktu.Widget.Services;
using SolatWaktu.Widget.Theme;

namespace SolatWaktu.Widget.UI;

/// <summary>
/// A small, non-activating desktop notification styled like a Windows toast.
/// </summary>
internal sealed class ToastForm : Form
{
    private readonly string _prayerName;
    private readonly DateTime _time;
    private readonly string _zoneName;
    private readonly bool _use24Hour;
    private readonly bool _isTest;
    private readonly Rectangle _workingArea;
    private readonly System.Windows.Forms.Timer _closeTimer;

    public ToastForm(
        string prayerName,
        DateTime time,
        string zoneName,
        bool use24Hour,
        Rectangle workingArea,
        bool isTest = false)
    {
        _prayerName = prayerName;
        _time = time;
        _zoneName = zoneName;
        _use24Hour = use24Hour;
        _isTest = isTest;
        _workingArea = workingArea;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(31, 31, 46);
        ClientSize = new Size(370, 104);
        DoubleBuffered = true;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.UserPaint,
            true);

        _closeTimer = new System.Windows.Forms.Timer { Interval = 8000 };
        _closeTimer.Tick += (_, _) => Close();
        Cursor = _isTest ? Cursors.Default : Cursors.Hand;
    }

    public event EventHandler? OpenRequested;

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ClassStyle |= 0x00020000;
            parameters.ExStyle |= 0x00000080 | 0x08000000; // WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE
            return parameters;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        UpdateRegion();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        UpdateRegion();
        var margin = (int)Math.Round(22 * (DeviceDpi / 96f));
        Location = new Point(
            Math.Max(_workingArea.Left + margin, _workingArea.Right - Width - margin),
            Math.Max(_workingArea.Top + margin, _workingArea.Bottom - Height - margin));
        _closeTimer.Start();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateRegion();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

        var scale = DeviceDpi / 96f;
        var rect = new RectangleF(0, 0, ClientSize.Width, ClientSize.Height);
        using var path = DrawingHelper.RoundedRect(rect, 16 * scale);
        using (var background = new SolidBrush(Color.FromArgb(31, 31, 46)))
        {
            graphics.FillPath(background, path);
        }

        using (var border = new Pen(Color.FromArgb(38, 255, 255, 255), Math.Max(1f, scale)))
        {
            graphics.DrawPath(border, path);
        }

        var accent = Color.FromArgb(0, 212, 170);

        // Accent rail.
        using (var accentBrush = new SolidBrush(accent))
        using (var accentPath = DrawingHelper.RoundedRect(
                   new RectangleF(6 * scale, 18 * scale, 4 * scale, (ClientSize.Height - 36 * scale)),
                   2 * scale))
        {
            graphics.FillPath(accentBrush, accentPath);
        }

        var iconSize = 42 * scale;
        var iconRect = new RectangleF(24 * scale, (ClientSize.Height - iconSize) / 2f, iconSize, iconSize);
        var mosque = AppResources.Mosque;
        if (mosque is not null)
        {
            using var iconPath = DrawingHelper.RoundedRect(iconRect, 11 * scale);
            var previousClip = graphics.Clip;
            graphics.SetClip(iconPath);
            graphics.DrawImage(mosque, iconRect);
            graphics.Clip = previousClip;
        }
        else
        {
            using var iconBrush = new SolidBrush(accent);
            DrawingHelper.FillRoundedRect(graphics, iconBrush, iconRect, 11 * scale);
        }

        var textLeft = 80 * scale;
        var textRight = ClientSize.Width - 22 * scale;

        using var titleFont = new Font(AppResources.BodyFontFamily, 11.5f, FontStyle.Bold);
        using var bodyFont = new Font(AppResources.BodyFontFamily, 9.2f, FontStyle.Regular);
        using var hintFont = new Font(AppResources.BodyFontFamily, 8.4f, FontStyle.Bold);

        using var titleBrush = new SolidBrush(Color.White);
        using var bodyBrush = new SolidBrush(Color.FromArgb(185, 225, 232, 245));
        using var hintBrush = new SolidBrush(accent);

        using var nearCenter = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };

        graphics.DrawString(
            AppText.Format("toast.title", _prayerName),
            titleFont,
            titleBrush,
            new RectangleF(textLeft, 15 * scale, textRight - textLeft, 24 * scale),
            nearCenter);
        graphics.DrawString(
            $"{MalayText.FormatTime(_time, _use24Hour)}  •  {_zoneName}",
            bodyFont,
            bodyBrush,
            new RectangleF(textLeft, 40 * scale, textRight - textLeft, 20 * scale),
            nearCenter);
        graphics.DrawString(
            AppText.Get(_isTest ? "toast.test" : "toast.open"),
            hintFont,
            hintBrush,
            new RectangleF(textLeft, 64 * scale, textRight - textLeft, 18 * scale),
            nearCenter);

        // Small close affordance.
        using var closePen = new Pen(Color.FromArgb(130, 255, 255, 255), Math.Max(1.2f, 1.4f * scale))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        var closeX = ClientSize.Width - 20 * scale;
        var closeY = 18 * scale;
        var closeSize = 5 * scale;
        graphics.DrawLine(closePen, closeX - closeSize, closeY - closeSize, closeX + closeSize, closeY + closeSize);
        graphics.DrawLine(closePen, closeX + closeSize, closeY - closeSize, closeX - closeSize, closeY + closeSize);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);

        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        var scale = DeviceDpi / 96f;
        var closeBounds = new RectangleF(
            ClientSize.Width - 34 * scale,
            4 * scale,
            28 * scale,
            28 * scale);

        // Clicking the X dismisses the toast; clicking anywhere else opens the widget.
        if (!_isTest && !closeBounds.Contains(e.Location))
        {
            OpenRequested?.Invoke(this, EventArgs.Empty);
        }

        Close();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _closeTimer.Stop();
        _closeTimer.Dispose();
        base.OnFormClosed(e);
    }

    private void UpdateRegion()
    {
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        using var path = DrawingHelper.RoundedRect(
            new RectangleF(0, 0, ClientSize.Width, ClientSize.Height),
            16 * (DeviceDpi / 96f));
        var oldRegion = Region;
        Region = new Region(path);
        oldRegion?.Dispose();
    }
}

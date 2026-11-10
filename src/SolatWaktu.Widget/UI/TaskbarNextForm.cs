using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using SolatWaktu.Core.Models;
using SolatWaktu.Core.Services;
using SolatWaktu.Widget.Localization;
using SolatWaktu.Widget.Services;
using SolatWaktu.Widget.Theme;

namespace SolatWaktu.Widget.UI;

/// <summary>
/// A compact live countdown to the next prayer that sits inside the Windows
/// taskbar. Windows 11 no longer supports third-party deskbands, so this is a
/// non-activating topmost window aligned to the taskbar rectangle instead.
/// </summary>
internal sealed class TaskbarNextForm : Form
{
    private const int BaseHeight = 38;
    private const int MinimumWidth = 200;
    private const float BaseCornerRadius = 11f;
    private const float ContentLeftPadding = 20f;
    private const float ContentRightPadding = 16f;
    private const float CountdownDetailGap = 9f;
    private const float CountdownFontSize = 11.2f;
    private const string CountdownProbe = "00:00:00";

    private ThemeDefinition _theme;
    private TaskbarNextAlignment _alignment;
    private PrayerOccurrence? _next;
    private TimeSpan? _remaining;
    private bool _use24Hour;
    private bool _hover;
    private Rectangle _lastTaskbarBounds = Rectangle.Empty;
    private readonly System.Windows.Forms.Timer _topMostGuard;

    public TaskbarNextForm(ThemeDefinition theme, TaskbarNextAlignment alignment)
    {
        _theme = theme;
        _alignment = alignment;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        AutoScaleDimensions = new SizeF(96f, 96f);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(21, 21, 34);
        ClientSize = new Size(260, BaseHeight);
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.UserPaint,
            true);

        _topMostGuard = new System.Windows.Forms.Timer { Interval = 200 };
        _topMostGuard.Tick += (_, _) => EnsureAboveTaskbar();
    }

    public event EventHandler? OpenRequested;

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ClassStyle |= 0x00020000; // CS_DROPSHADOW
            parameters.ExStyle |= 0x00000080 | 0x08000000; // WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE
            return parameters;
        }
    }

    public void ApplyAppearance(
        ThemeDefinition theme,
        TaskbarNextAlignment alignment,
        bool use24Hour)
    {
        _theme = theme;
        _alignment = alignment;
        _use24Hour = use24Hour;

        if (_alignment == TaskbarNextAlignment.Off)
        {
            if (Visible)
            {
                Hide();
            }

            _lastTaskbarBounds = Rectangle.Empty;
            return;
        }

        if (!Visible)
        {
            Show();
        }

        UpdateBarSize();
        Reposition(force: true);
        EnsureAboveTaskbar();
        Invalidate();
    }

    public void UpdateNext(PrayerOccurrence? next, TimeSpan? remaining, bool use24Hour)
    {
        if (next is null)
        {
            remaining = null;
        }

        var previousDetail = BuildDetailText();
        var changed = !Equals(_next, next) || use24Hour != _use24Hour;
        _next = next;
        _remaining = remaining;
        _use24Hour = use24Hour;

        if (!Visible)
        {
            return;
        }

        if (changed
            || !string.Equals(previousDetail, BuildDetailText(), StringComparison.Ordinal))
        {
            UpdateBarSize();
            Reposition(force: true);
            Invalidate();
            return;
        }

        // The countdown changes every second even though the bar size and
        // position stay fixed.
        Invalidate();
        Reposition(force: false);
    }

    public void Reposition(bool force)
    {
        if (!IsHandleCreated || _alignment == TaskbarNextAlignment.Off)
        {
            return;
        }

        var screen = Screen.PrimaryScreen ?? Screen.FromPoint(Cursor.Position);
        var scale = DeviceDpi / 96f;
        var margin = (int)Math.Round(12 * scale);
        var gap = (int)Math.Round(6 * scale);

        if (!NativeMethods.TryGetTaskbarRect(out var taskbar))
        {
            var area = screen.WorkingArea;
            taskbar = new Rectangle(
                area.Left,
                area.Bottom,
                area.Width,
                Math.Max(1, screen.Bounds.Bottom - area.Bottom));
        }

        var screenBounds = Screen.FromRectangle(taskbar).Bounds;
        var horizontalTaskbar = taskbar.Width >= taskbar.Height;
        var overlayTaskbar = true;
        int x;
        int y;

        if (horizontalTaskbar)
        {
            // The bar is centred vertically inside the taskbar so it visually
            // becomes part of it. Right alignment is kept clear of running-app
            // buttons and the notification area, including the clock.
            var atBottom = taskbar.Top >= screenBounds.Top + screenBounds.Height / 2;

            if (_alignment == TaskbarNextAlignment.Right)
            {
                var safeRight = taskbar.Right - margin;
                if (NativeMethods.TryGetTaskbarTrayRect(out var tray))
                {
                    safeRight = tray.Left - margin;
                }

                var safeLeft = taskbar.Left + margin;
                if (NativeMethods.TryGetTaskbarAppAreaRect(out var appArea))
                {
                    safeLeft = Math.Max(safeLeft, appArea.Right + margin);
                }

                if (safeRight - safeLeft >= Width)
                {
                    x = safeRight - Width;
                }
                else
                {
                    // Not enough free space between the app buttons and the
                    // tray; float above the taskbar instead of covering them.
                    overlayTaskbar = false;
                    x = taskbar.Right - Width - margin;
                }
            }
            else
            {
                x = _alignment switch
                {
                    TaskbarNextAlignment.Left => taskbar.Left + margin,
                    _ => taskbar.Left + (taskbar.Width - Width) / 2
                };
            }

            y = overlayTaskbar
                ? taskbar.Top + (taskbar.Height - Height) / 2
                : atBottom
                    ? taskbar.Top - Height - gap
                    : taskbar.Bottom + gap;
        }
        else
        {
            var atRight = taskbar.Left >= screenBounds.Left + screenBounds.Width / 2;

            if (_alignment == TaskbarNextAlignment.Right)
            {
                var safeBottom = taskbar.Bottom - margin;
                if (NativeMethods.TryGetTaskbarTrayRect(out var tray))
                {
                    safeBottom = tray.Top - margin;
                }

                var safeTop = taskbar.Top + margin;
                if (NativeMethods.TryGetTaskbarAppAreaRect(out var appArea))
                {
                    safeTop = Math.Max(safeTop, appArea.Bottom + margin);
                }

                if (safeBottom - safeTop >= Height)
                {
                    y = safeBottom - Height;
                }
                else
                {
                    overlayTaskbar = false;
                    y = taskbar.Bottom - Height - margin;
                }
            }
            else
            {
                y = _alignment switch
                {
                    TaskbarNextAlignment.Left => taskbar.Top + margin,
                    _ => taskbar.Top + (taskbar.Height - Height) / 2
                };
            }

            x = overlayTaskbar
                ? taskbar.Left + (taskbar.Width - Width) / 2
                : atRight
                    ? taskbar.Left - Width - gap
                    : taskbar.Right + gap;
        }

        var maxX = Math.Max(screenBounds.Left, screenBounds.Right - Width);
        var maxY = Math.Max(screenBounds.Top, screenBounds.Bottom - Height);
        x = Math.Clamp(x, screenBounds.Left, maxX);
        y = Math.Clamp(y, screenBounds.Top, maxY);

        if (force || taskbar != _lastTaskbarBounds || Location.X != x || Location.Y != y)
        {
            _lastTaskbarBounds = taskbar;
            Location = new Point(x, y);
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        NativeMethods.EnableRoundedCorners(Handle);
        UpdateBarSize();
        UpdateRegion();
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);

        if (Visible)
        {
            _topMostGuard.Start();
            EnsureAboveTaskbar();
        }
        else
        {
            _topMostGuard.Stop();
        }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateRegion();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        UpdateBarSize();
        UpdateRegion();
        Reposition(force: true);
        Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var scale = DeviceDpi / 96f;
        var rect = new RectangleF(0, 0, ClientSize.Width, ClientSize.Height);
        using var path = DrawingHelper.RoundedRect(rect, BaseCornerRadius * scale);

        using (var gradient = DrawingHelper.CreateGradientBrush(rect, _theme))
        {
            graphics.FillPath(gradient, path);
        }

        using (var overlay = new SolidBrush(Color.FromArgb(_theme.OverlayAlpha, 8, 8, 16)))
        {
            graphics.FillPath(overlay, path);
        }

        using (var border = new Pen(
                   Color.FromArgb(_hover ? 88 : 46, 255, 255, 255),
                   Math.Max(1f, scale)))
        {
            graphics.DrawPath(border, path);
        }

        var railHeight = Math.Max(1f, ClientSize.Height - 20 * scale);
        using (var railBrush = new SolidBrush(Color.FromArgb(235, _theme.AccentSoft)))
        using (var railPath = DrawingHelper.RoundedRect(
                   new RectangleF(6 * scale, (ClientSize.Height - railHeight) / 2f, 3 * scale, railHeight),
                   1.5f * scale))
        {
            graphics.FillPath(railBrush, railPath);
        }

        var countdown = BuildCountdownText();
        var detail = BuildDetailText();

        using var countdownFont = CreateCountdownFont();
        using var detailFont = CreateDetailFont();
        using var countdownBrush = new SolidBrush(Color.White);
        using var detailBrush = new SolidBrush(Color.FromArgb(224, 255, 255, 255));
        using var accentBrush = new SolidBrush(Color.FromArgb(235, _theme.AccentSoft));
        using var near = DrawingHelper.NearCenter();
        using var centered = DrawingHelper.CenterCenter();

        // The slot is measured from a fixed probe so the bar width does not
        // jitter as the digits change every second.
        var countdownSlotWidth = graphics.MeasureString(CountdownProbe, countdownFont).Width;
        var detailSize = graphics.MeasureString(detail, detailFont);
        var dotGap = CountdownDetailGap * scale;
        var availableWidth = Math.Max(
            1f,
            ClientSize.Width - (ContentLeftPadding + ContentRightPadding) * scale);
        var contentWidth = countdownSlotWidth + dotGap + detailSize.Width;
        var startX = ContentLeftPadding * scale + Math.Max(0f, (availableWidth - contentWidth) / 2f);

        graphics.DrawString(
            countdown,
            countdownFont,
            countdownBrush,
            new RectangleF(startX, 0, countdownSlotWidth, ClientSize.Height),
            centered);

        var dotSize = 3 * scale;
        graphics.FillEllipse(
            accentBrush,
            startX + countdownSlotWidth + (dotGap - dotSize) / 2f,
            (ClientSize.Height - dotSize) / 2f,
            dotSize,
            dotSize);

        var detailLeft = startX + countdownSlotWidth + dotGap;
        var detailWidth = Math.Max(1f, ClientSize.Width - detailLeft - ContentRightPadding * scale);
        graphics.DrawString(
            detail,
            detailFont,
            detailBrush,
            new RectangleF(detailLeft, 0, detailWidth, ClientSize.Height),
            near);
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

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button == MouseButtons.Left)
        {
            OpenRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private string BuildCountdownText() =>
        _next is null || _remaining is null
            ? "--:--:--"
            : MalayText.FormatClockCountdown(_remaining.Value);

    private string BuildDetailText() =>
        _next is null
            ? AppText.Get("common.waitingData")
            : $"{(_next.IsTomorrow ? AppText.Get("common.tomorrow") + " " : string.Empty)}{_next.Name} {MalayText.FormatTime(_next.Time, _use24Hour)}";

    private static Font CreateCountdownFont() =>
        new("Consolas", CountdownFontSize, FontStyle.Bold);

    private static Font CreateDetailFont() =>
        new(AppResources.HeadingFontFamily, 9.2f, FontStyle.Bold);

    private bool UpdateBarSize()
    {
        if (!IsHandleCreated)
        {
            return false;
        }

        var scale = DeviceDpi / 96f;
        using var graphics = CreateGraphics();
        using var countdownFont = CreateCountdownFont();
        using var detailFont = CreateDetailFont();

        var countdownSlotWidth = graphics.MeasureString(CountdownProbe, countdownFont).Width;
        var detailSize = graphics.MeasureString(BuildDetailText(), detailFont);
        var contentWidth = countdownSlotWidth + CountdownDetailGap * scale + detailSize.Width;
        var width = (int)Math.Ceiling(
            contentWidth + (ContentLeftPadding + ContentRightPadding) * scale);
        width = Math.Max((int)Math.Round(MinimumWidth * scale), width);
        var height = (int)Math.Round(BaseHeight * scale);
        var target = new Size(width, height);

        if (ClientSize == target)
        {
            return false;
        }

        ClientSize = target;
        return true;
    }

    private void EnsureAboveTaskbar()
    {
        if (!Visible || !IsHandleCreated || ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        var center = PointToScreen(new Point(ClientSize.Width / 2, ClientSize.Height / 2));
        if (NativeMethods.IsTaskbarAtPoint(center))
        {
            NativeMethods.BringToTopMost(Handle);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _topMostGuard.Stop();
            _topMostGuard.Dispose();
        }

        base.Dispose(disposing);
    }

    private void UpdateRegion()
    {
        if (!IsHandleCreated || ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        using var path = DrawingHelper.RoundedRect(
            new RectangleF(0, 0, ClientSize.Width, ClientSize.Height),
            BaseCornerRadius * DeviceDpi / 96f);
        var previous = Region;
        Region = new Region(path);
        previous?.Dispose();
    }
}

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Windows.Forms;
using SolatWaktu.Core.Models;
using SolatWaktu.Core.Services;
using SolatWaktu.Widget.Localization;
using SolatWaktu.Widget.Services;
using SolatWaktu.Widget.Theme;

namespace SolatWaktu.Widget.UI;

internal sealed class MainForm : Form
{
    private const int BaseWidth = 350;
    private const int BaseHeight = 500;
    private const int BaseCompactHeight = 238;
    private const int BaseRowHeight = 32;
    private const float BaseCornerRadius = 8f;

    private readonly SettingsService _settingsService;
    private readonly PrayerTimeService _prayerService;
    private readonly ZoneCatalog _catalog;
    private readonly System.Windows.Forms.Timer _clockTimer;
    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _trayMenu;
    private readonly ContextMenuStrip _formMenu;
    private readonly ToolStripMenuItem _trayShowItem;
    private readonly ToolStripMenuItem _trayRefreshItem;
    private readonly ToolStripMenuItem _trayZoneItem;
    private readonly ToolStripMenuItem _traySettingsItem;
    private readonly ToolStripMenuItem _trayStartupItem;
    private readonly ToolStripMenuItem _trayExitItem;
    private readonly ToolStripMenuItem _formRefreshItem;
    private readonly ToolStripMenuItem _formZoneItem;
    private readonly ToolStripMenuItem _formSettingsItem;
    private readonly ToolStripMenuItem _formHideItem;
    private readonly ToolStripMenuItem _formExitItem;
    private readonly AzanPlayer _azanPlayer;
    private readonly TaskbarNextForm _taskbarNext;
    private readonly HashSet<long> _notified = [];

    private readonly DateTime _startedAt = MalaysiaTime.Now;

    private AppSettings _settings;
    private ThemeDefinition _theme;
    private Zone _zone;

    private PrayerMonthResult? _currentMonthResult;
    private PrayerDay? _today;
    private PrayerDay? _yesterday;
    private PrayerDay? _tomorrow;
    private PrayerScheduleResult? _schedule;
    private DateOnly? _loadedDate;
    private DateTimeOffset? _lastUpdated;
    private DateTime _lastAutoRefreshUtc = DateTime.MinValue;
    private bool _fromCache;
    private bool _loading = true;
    private bool _loadFailed;
    private bool _updateFailed;
    private bool _refreshing;
    private bool _pendingRefresh;
    private bool _reallyExit;
    private bool _compact;
    private bool _trayHintShown;
    private ToastForm? _toast;
    private string _lastTrayText = string.Empty;

    private float _layoutScale = 1f;
    private float _fontScale = 1f;

    private Font? _titleFont;
    private Font? _dateFont;
    private Font? _hijriFont;
    private Font? _heroLabelFont;
    private Font? _heroNameFont;
    private Font? _heroTimeFont;
    private Font? _countdownFont;
    private Font? _prayerNameFont;
    private Font? _prayerNameBoldFont;
    private Font? _prayerTimeFont;
    private Font? _footerFont;
    private Font? _iconFont;
    private Font? _errorTitleFont;
    private Font? _errorBodyFont;

    private RectangleF _settingsButton;
    private RectangleF _pinButton;
    private RectangleF _closeButton;
    private RectangleF _retryButton;
    private int _hoverButton = -1;
    private bool _dragging;
    private Point _dragStartCursor;
    private Point _dragStartLocation;

    public MainForm(
        SettingsService settingsService,
        AppSettings settings,
        PrayerTimeService prayerService)
    {
        _settingsService = settingsService;
        _settings = settings;
        _prayerService = prayerService;
        _catalog = ZoneCatalog.Default;
        _zone = _catalog.Find(settings.ZoneCode) ?? _catalog.Find("WLY01")!;
        _theme = ThemeCatalog.Get(settings.Theme);
        _compact = settings.CompactMode;
        AppText.SetLanguage(settings.Language);
        _azanPlayer = new AzanPlayer();
        _taskbarNext = new TaskbarNextForm(_theme, settings.TaskbarNext);
        _taskbarNext.OpenRequested += (_, _) => ShowWidget();

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        Text = NativeMethods.WindowTitle;
        BackColor = Color.FromArgb(21, 21, 34);
        DoubleBuffered = true;
        TopMost = settings.AlwaysOnTop;
        Opacity = Math.Clamp(settings.OpacityPercent, 70, 100) / 100f;
        KeyPreview = true;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.UserPaint,
            true);

        _trayMenu = DarkMenuFactory.Create();
        _trayShowItem = DarkMenuFactory.Item(
            AppText.Get("main.menu.show"),
            (_, _) => ShowWidget());
        _trayRefreshItem = DarkMenuFactory.Item(
            AppText.Get("main.menu.refresh"),
            (_, _) => _ = RefreshDataAsync(forceRefresh: true));
        _trayZoneItem = DarkMenuFactory.Item(
            AppText.Get("main.menu.changeZone"),
            (_, _) => PickZone());
        _traySettingsItem = DarkMenuFactory.Item(
            AppText.Get("main.menu.settings"),
            (_, _) => OpenSettings());
        _trayStartupItem = new ToolStripMenuItem(AppText.Get("main.menu.startWithWindows"))
        {
            Checked = settings.StartWithWindows,
            CheckOnClick = false,
            ForeColor = Color.FromArgb(236, 240, 255),
            Padding = new Padding(6, 3, 6, 3)
        };
        _trayStartupItem.Click += (_, _) => ToggleStartup();
        _trayExitItem = DarkMenuFactory.Item(
            AppText.Get("main.menu.exit"),
            (_, _) => ExitApplication());

        _trayMenu.Items.AddRange(
        [
            _trayShowItem,
            _trayRefreshItem,
            _trayZoneItem,
            _traySettingsItem,
            new ToolStripSeparator(),
            _trayStartupItem,
            new ToolStripSeparator(),
            _trayExitItem
        ]);

        _trayIcon = new NotifyIcon
        {
            Text = AppText.Get("common.trayTitle"),
            Icon = AppResources.LoadAppIcon(),
            Visible = true,
            ContextMenuStrip = _trayMenu
        };
        _trayIcon.DoubleClick += (_, _) => ShowWidget();

        _formMenu = DarkMenuFactory.Create();
        _formRefreshItem = DarkMenuFactory.Item(
            AppText.Get("main.menu.refresh"),
            (_, _) => _ = RefreshDataAsync(forceRefresh: true));
        _formZoneItem = DarkMenuFactory.Item(
            AppText.Get("main.menu.changeZone"),
            (_, _) => PickZone());
        _formSettingsItem = DarkMenuFactory.Item(
            AppText.Get("main.menu.settings"),
            (_, _) => OpenSettings());
        _formHideItem = DarkMenuFactory.Item(
            AppText.Get("main.menu.hide"),
            (_, _) => HideToTray());
        _formExitItem = DarkMenuFactory.Item(
            AppText.Get("main.menu.exit"),
            (_, _) => ExitApplication());
        _formMenu.Items.AddRange(
        [
            _formRefreshItem,
            _formZoneItem,
            _formSettingsItem,
            new ToolStripSeparator(),
            _formHideItem,
            _formExitItem
        ]);
        ContextMenuStrip = _formMenu;

        _clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _clockTimer.Tick += ClockTimer_Tick;

        ApplyLayout(initial: true);
        RebuildFonts();
        UpdateRegion();

        Load += MainForm_Load;
        FormClosing += MainForm_FormClosing;
        MouseDown += MainForm_MouseDown;
        MouseMove += MainForm_MouseMove;
        MouseUp += MainForm_MouseUp;
        MouseDoubleClick += MainForm_MouseDoubleClick;
        MouseLeave += MainForm_MouseLeave;
        KeyDown += MainForm_KeyDown;

        _taskbarNext.ApplyAppearance(_theme, _settings.TaskbarNext, _settings.Use24HourFormat);
        _taskbarNext.UpdateNext(null, null, _settings.Use24HourFormat);
        _clockTimer.Start();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ClassStyle |= 0x00020000; // CS_DROPSHADOW
            return parameters;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        NativeMethods.EnableRoundedCorners(Handle);
        NativeMethods.EnableDarkTitleBar(Handle);
        UpdateRegion();
    }

    protected override void WndProc(ref Message m)
    {
        if (NativeMethods.ShowWidgetMessage != 0 && m.Msg == NativeMethods.ShowWidgetMessage)
        {
            ShowWidget();
            return;
        }

        base.WndProc(ref m);
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        ApplyLayout(initial: false);
        RebuildFonts();
        UpdateRegion();
        Invalidate();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        CalculateButtonRects();
        UpdateRegion();
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        // Fill with the opaque base colour first. If a repaint is interrupted,
        // the window must never expose the default white background.
        pevent.Graphics.Clear(BackColor);
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

        var scale = _layoutScale;
        var rect = new RectangleF(0, 0, ClientSize.Width, ClientSize.Height);
        using var outerPath = DrawingHelper.RoundedRect(rect, BaseCornerRadius * scale);

        // Paint the complete client rectangle before clipping to the rounded
        // window region. Painting only inside the rounded path made its
        // anti-aliased edge blend with the dark form background, leaving a
        // dark seam around the corners and edges.
        using (var gradient = DrawingHelper.CreateGradientBrush(rect, _theme))
        {
            graphics.FillRectangle(gradient, rect);
        }

        using (var overlay = new SolidBrush(Color.FromArgb(_theme.OverlayAlpha, 8, 8, 16)))
        {
            graphics.FillRectangle(overlay, rect);
        }

        using (var sheen = new LinearGradientBrush(
                   rect,
                   Color.FromArgb(26, 255, 255, 255),
                   Color.FromArgb(0, 255, 255, 255),
                   90f))
        {
            // Keep the highlight in the top 52% only. The brush must cover the
            // whole fill rect; if its rect is shorter, GDI+ tiles the gradient
            // and paints a second white band over the lower half.
            sheen.InterpolationColors = new ColorBlend
            {
                Colors =
                [
                    Color.FromArgb(26, 255, 255, 255),
                    Color.FromArgb(0, 255, 255, 255),
                    Color.FromArgb(0, 255, 255, 255)
                ],
                Positions = [0f, 0.52f, 1f]
            };
            graphics.FillRectangle(sheen, rect);
        }

        graphics.SetClip(outerPath);
        DrawHeader(graphics, scale);

        if (_schedule is not null)
        {
            DrawHero(graphics, scale);
            if (!_compact)
            {
                DrawPrayerList(graphics, scale);
            }

            DrawFooter(graphics, scale);
        }
        else if (_loading)
        {
            DrawLoading(graphics, scale);
        }
        else
        {
            DrawError(graphics, scale);
        }

        graphics.ResetClip();
        using var border = new Pen(Color.FromArgb(42, 255, 255, 255), Math.Max(1f, scale));
        graphics.DrawPath(border, outerPath);
    }

    private void DrawHeader(Graphics graphics, float scale)
    {
        var today = _today?.Date ?? DateOnly.FromDateTime(MalaysiaTime.Now);
        var titleRect = new RectangleF(
            20 * scale,
            14 * scale,
            Math.Max(10f, _settingsButton.Left - 12 * scale - 20 * scale),
            34 * scale);

        using var nearCenter = DrawingHelper.NearCenter();
        using var titleBrush = new SolidBrush(Color.White);
        graphics.DrawString(MalayText.ShortZoneName(_zone), _titleFont!, titleBrush, titleRect, nearCenter);

        using var secondaryBrush = new SolidBrush(Color.FromArgb(245, 255, 255, 255));
        var dateRect = new RectangleF(20 * scale, 45 * scale, ClientSize.Width - 40 * scale, 20 * scale);
        graphics.DrawString(
            MalayText.FormatLongDate(today, _settings.Language),
            _dateFont!,
            secondaryBrush,
            dateRect,
            nearCenter);

        if (!_compact && _today is not null)
        {
            var hijri = MalayText.FormatHijri(_today.Hijri, _settings.Language);
            if (!string.IsNullOrWhiteSpace(hijri))
            {
                using var accentDotBrush = new SolidBrush(Color.FromArgb(255, _theme.AccentSoft));
                graphics.FillEllipse(
                    accentDotBrush,
                    21 * scale,
                    67 * scale,
                    3.5f * scale,
                    3.5f * scale);
                using var hijriBrush = new SolidBrush(Color.FromArgb(240, 255, 255, 255));
                var hijriRect = new RectangleF(31 * scale, 59 * scale, ClientSize.Width - 58 * scale, 20 * scale);
                graphics.DrawString(hijri, _hijriFont!, hijriBrush, hijriRect, nearCenter);
            }
        }

        DrawIconButton(graphics, scale, _settingsButton, 0, "\uE713", Color.FromArgb(228, 245, 247, 255));
        DrawIconButton(
            graphics,
            scale,
            _pinButton,
            1,
            "\uE718",
            _settings.AlwaysOnTop
                ? DrawingHelper.MixWithWhite(_theme.AccentSoft, 0.45f)
                : Color.FromArgb(190, 245, 247, 255));
        DrawIconButton(graphics, scale, _closeButton, 2, "\uE8BB", Color.FromArgb(222, 245, 247, 255));
    }

    private void DrawIconButton(
        Graphics graphics,
        float scale,
        RectangleF rect,
        int index,
        string glyph,
        Color color)
    {
        var hovered = _hoverButton == index;
        using var fill = new SolidBrush(Color.FromArgb(hovered ? 42 : 16, 255, 255, 255));
        DrawingHelper.FillRoundedRect(graphics, fill, rect, 9 * scale);

        using var brush = new SolidBrush(color);
        using var center = DrawingHelper.CenterCenter();
        graphics.DrawString(glyph, _iconFont!, brush, rect, center);
    }

    private void DrawHero(Graphics graphics, float scale)
    {
        var schedule = _schedule!;
        var next = schedule.Next;
        var top = _compact ? 76 * scale : 92 * scale;

        using var centered = DrawingHelper.CenterCenter();
        using var whiteBrush = new SolidBrush(Color.White);
        using var secondaryBrush = new SolidBrush(Color.FromArgb(245, 255, 255, 255));

        var label = next is null
            ? AppText.Get("main.hero.next")
            : next.IsTomorrow
                ? AppText.Get("main.hero.nextTomorrow")
                : AppText.Get("main.hero.next");
        using var labelBrush = new SolidBrush(Color.FromArgb(245, 255, 255, 255));
        graphics.DrawString(
            label,
            _heroLabelFont!,
            labelBrush,
            new RectangleF(20 * scale, top, ClientSize.Width - 40 * scale, 16 * scale),
            centered);

        var name = next?.Name ?? AppText.Get("common.waitingData");
        graphics.DrawString(
            name,
            _heroNameFont!,
            whiteBrush,
            new RectangleF(20 * scale, (top + 16 * scale), ClientSize.Width - 40 * scale, 34 * scale),
            centered);

        var timeText = next is null
            ? "--:--"
            : MalayText.FormatTime(next.Time, _settings.Use24HourFormat);
        graphics.DrawString(
            timeText,
            _heroTimeFont!,
            whiteBrush,
            new RectangleF(20 * scale, (top + 48 * scale), ClientSize.Width - 40 * scale, 44 * scale),
            centered);

        if (next is not null)
        {
            var countdown = Capitalize(MalayText.FormatCountdown(schedule.Remaining, _settings.Language));
            var textSize = graphics.MeasureString(countdown, _countdownFont!);
            var pillWidth = Math.Min(ClientSize.Width - 40 * scale, textSize.Width + 22 * scale);
            var pill = new RectangleF(
                (ClientSize.Width - pillWidth) / 2f,
                top + 96 * scale,
                pillWidth,
                26 * scale);

            using (var pillFill = new SolidBrush(Color.FromArgb(72, 8, 8, 16)))
            using (var pillBorder = new Pen(Color.FromArgb(120, _theme.AccentSoft), Math.Max(1f, scale)))
            {
                DrawingHelper.FillRoundedRect(graphics, pillFill, pill, 13 * scale);
                DrawingHelper.DrawRoundedRect(graphics, pillBorder, pill, 13 * scale);
            }

            using var countdownBrush = new SolidBrush(Color.White);
            using var center = DrawingHelper.CenterCenter();
            graphics.DrawString(countdown, _countdownFont!, countdownBrush, pill, center);
        }
        else
        {
            graphics.DrawString(
                AppText.Get("main.hero.refresh"),
                _dateFont!,
                secondaryBrush,
                new RectangleF(20 * scale, top + 98 * scale, ClientSize.Width - 40 * scale, 22 * scale),
                centered);
        }

        var progressTop = top + 130 * scale;
        var progressRect = new RectangleF(20 * scale, progressTop, ClientSize.Width - 40 * scale, 5 * scale);
        using (var track = new SolidBrush(Color.FromArgb(45, 255, 255, 255)))
        {
            DrawingHelper.FillRoundedRect(graphics, track, progressRect, 2.5f * scale);
        }

        if (next is not null && schedule.Progress > 0)
        {
            var fillWidth = Math.Max(5 * scale, (float)(progressRect.Width * schedule.Progress));
            var fillRect = new RectangleF(progressRect.X, progressRect.Y, fillWidth, progressRect.Height);
            using var fill = new SolidBrush(Color.FromArgb(235, _theme.AccentSoft));
            DrawingHelper.FillRoundedRect(graphics, fill, fillRect, 2.5f * scale);
        }
    }

    private void DrawPrayerList(Graphics graphics, float scale)
    {
        if (_today is null)
        {
            return;
        }

        var kinds = new List<PrayerKind>();
        if (_settings.ShowImsak)
        {
            kinds.Add(PrayerKind.Imsak);
        }

        kinds.Add(PrayerKind.Fajr);
        kinds.Add(PrayerKind.Syuruk);
        if (_settings.ShowDhuha)
        {
            kinds.Add(PrayerKind.Dhuha);
        }

        kinds.Add(PrayerKind.Dhuhr);
        kinds.Add(PrayerKind.Asr);
        kinds.Add(PrayerKind.Maghrib);
        kinds.Add(PrayerKind.Isha);

        var now = MalaysiaTime.Now;
        var listTop = 246 * scale;
        var rowHeight = BaseRowHeight * scale;
        var next = _schedule?.Next;

        using var nameBrush = new SolidBrush(Color.White);
        using var timeBrush = new SolidBrush(Color.White);
        using var passedBrush = new SolidBrush(Color.FromArgb(200, 232, 238, 250));
        using var near = DrawingHelper.NearCenter();
        using var far = DrawingHelper.FarCenter();

        for (var index = 0; index < kinds.Count; index++)
        {
            var kind = kinds[index];
            var time = _today.GetTime(kind);
            if (time <= DateTime.MinValue)
            {
                continue;
            }

            var y = listTop + index * rowHeight;
            var isNext = next is not null && !next.IsTomorrow && next.Kind == kind;
            var isPassed = !isNext && time <= now;
            var rowRect = new RectangleF(14 * scale, y, ClientSize.Width - 28 * scale, rowHeight - 2 * scale);

            if (isNext)
            {
                using var highlight = new SolidBrush(Color.FromArgb(62, 8, 8, 16));
                DrawingHelper.FillRoundedRect(graphics, highlight, rowRect, 9 * scale);
                using var bar = new SolidBrush(_theme.AccentSoft);
                graphics.FillRectangle(
                    bar,
                    rowRect.X + 2 * scale,
                    rowRect.Y + 8 * scale,
                    3 * scale,
                    rowRect.Height - 16 * scale);
            }

            var textLeft = 28 * scale;
            var nameRect = new RectangleF(textLeft, y, ClientSize.Width - textLeft - 120 * scale, rowHeight);
            var timeRect = new RectangleF(ClientSize.Width - 120 * scale, y, 92 * scale, rowHeight);

            var nameFont = isNext ? _prayerNameBoldFont! : _prayerNameFont!;
            var nameColor = isPassed ? passedBrush : nameBrush;
            var timeColor = isPassed ? passedBrush : timeBrush;

            graphics.DrawString(kind.MalayName(), nameFont, nameColor, nameRect, near);
            graphics.DrawString(
                MalayText.FormatTime(time, _settings.Use24HourFormat),
                _prayerTimeFont!,
                timeColor,
                timeRect,
                far);

            if (index < kinds.Count - 1)
            {
                using var separator = new Pen(Color.FromArgb(18, 255, 255, 255), Math.Max(1f, scale));
                graphics.DrawLine(
                    separator,
                    24 * scale,
                    y + rowHeight - 1,
                    ClientSize.Width - 24 * scale,
                    y + rowHeight - 1);
            }
        }
    }

    private void DrawFooter(Graphics graphics, float scale)
    {
        var y = ClientSize.Height - 26 * scale;
        var now = MalaysiaTime.Now;
        var updated = _lastUpdated is { } fetched
            ? MalayText.FormatUpdated(fetched, now)
            : null;

        var status = _updateFailed
            ? AppText.Get("common.updateFailed")
            : _fromCache
                ? updated is null
                    ? AppText.Get("common.offline")
                    : AppText.Format("main.status.offlineUpdated", updated)
                : updated is null
                    ? string.Empty
                    : AppText.Format("main.status.updated", updated);

        var warning = _updateFailed || _fromCache;
        using var dotBrush = new SolidBrush(warning
            ? Color.FromArgb(230, 255, 184, 76)
            : DrawingHelper.MixWithWhite(_theme.Accent, 0.35f));
        graphics.FillEllipse(dotBrush, 21 * scale, y + 6 * scale, 4 * scale, 4 * scale);

        using var footerBrush = new SolidBrush(Color.FromArgb(240, 255, 255, 255));
        using var statusBrush = new SolidBrush(warning
            ? Color.FromArgb(225, 255, 196, 107)
            : Color.FromArgb(240, 255, 255, 255));
        using var near = DrawingHelper.NearCenter();
        using var far = DrawingHelper.FarCenter();

        graphics.DrawString(
            AppText.Get("common.source"),
            _footerFont!,
            footerBrush,
            new RectangleF(31 * scale, y, 140 * scale, 16 * scale),
            near);

        if (!string.IsNullOrEmpty(status))
        {
            graphics.DrawString(
                status,
                _footerFont!,
                statusBrush,
                new RectangleF(ClientSize.Width - 240 * scale, y, 220 * scale, 16 * scale),
                far);
        }
    }

    private void DrawLoading(Graphics graphics, float scale)
    {
        _retryButton = RectangleF.Empty;
        var top = _compact ? 100 * scale : 168 * scale;
        using var titleBrush = new SolidBrush(Color.White);
        using var bodyBrush = new SolidBrush(Color.FromArgb(225, 255, 255, 255));
        using var center = DrawingHelper.CenterCenter();

        graphics.DrawString(
            AppText.Get("main.loading.title"),
            _errorTitleFont!,
            titleBrush,
            new RectangleF(20 * scale, top, ClientSize.Width - 40 * scale, 30 * scale),
            center);

        graphics.DrawString(
            AppText.Get("main.loading.body"),
            _errorBodyFont!,
            bodyBrush,
            new RectangleF(20 * scale, top + 34 * scale, ClientSize.Width - 40 * scale, 22 * scale),
            center);
    }

    private void DrawError(Graphics graphics, float scale)
    {
        var top = _compact ? 92 * scale : 150 * scale;
        using var titleBrush = new SolidBrush(Color.White);
        using var bodyBrush = new SolidBrush(Color.FromArgb(225, 255, 255, 255));
        using var accentBrush = new SolidBrush(Color.White);
        using var center = DrawingHelper.CenterCenter();

        graphics.DrawString(
            _loadFailed
                ? AppText.Get("main.error.load")
                : AppText.Get("main.error.title"),
            _errorTitleFont!,
            titleBrush,
            new RectangleF(20 * scale, top, ClientSize.Width - 40 * scale, 32 * scale),
            center);

        graphics.DrawString(
            AppText.Get("main.error.body"),
            _errorBodyFont!,
            bodyBrush,
            new RectangleF(20 * scale, top + 36 * scale, ClientSize.Width - 40 * scale, 22 * scale),
            center);

        var buttonWidth = 132 * scale;
        var buttonHeight = 36 * scale;
        _retryButton = new RectangleF(
            (ClientSize.Width - buttonWidth) / 2f,
            top + 78 * scale,
            buttonWidth,
            buttonHeight);

        using (var fill = new SolidBrush(Color.FromArgb(235, DrawingHelper.Darken(_theme.Accent, 0.4f))))
        {
            DrawingHelper.FillRoundedRect(graphics, fill, _retryButton, 10 * scale);
        }

        using var buttonFont = new Font(AppResources.BodyFontFamily, 9.5f * _fontScale, FontStyle.Bold);
        graphics.DrawString(AppText.Get("common.retry"), buttonFont, accentBrush, _retryButton, center);
    }

    private async void MainForm_Load(object? sender, EventArgs e)
    {
        ApplyLayout(initial: false);
        RebuildFonts();
        UpdateRegion();

        if (_settings.StartWithWindows)
        {
            StartupService.SetEnabled(true);
        }

        await RefreshDataAsync(forceRefresh: false);
    }

    private async void ClockTimer_Tick(object? sender, EventArgs e)
    {
        var now = MalaysiaTime.Now;
        var today = DateOnly.FromDateTime(now);

        if (_loadedDate is not null && _loadedDate != today)
        {
            _schedule = null;
            _today = null;
            _yesterday = null;
            _tomorrow = null;
            _loadedDate = null;
            _loading = true;
            _loadFailed = false;
            _updateFailed = false;
            _notified.Clear();
            Invalidate();
        }

        if (_loadedDate is null)
        {
            if (!_refreshing)
            {
                await RefreshDataAsync(forceRefresh: false);
            }

            return;
        }

        if (_schedule is null)
        {
            if (!_refreshing)
            {
                await RefreshDataAsync(forceRefresh: false);
            }

            return;
        }

        RecalculateSchedule(now);
        CheckPrayerNotifications(now);
        _taskbarNext.UpdateNext(_schedule?.Next, _schedule?.Remaining, _settings.Use24HourFormat);
        Invalidate();

        if (_schedule?.Next is null
            && DateTime.UtcNow - _lastAutoRefreshUtc > TimeSpan.FromMinutes(5)
            && !_refreshing)
        {
            _lastAutoRefreshUtc = DateTime.UtcNow;
            await RefreshDataAsync(forceRefresh: false);
        }
    }

    private async Task RefreshDataAsync(bool forceRefresh)
    {
        if (_refreshing)
        {
            return;
        }

        _refreshing = true;
        var requestedZone = _zone.JakimCode;
        if (_schedule is null)
        {
            _loading = true;
            _loadFailed = false;
            Invalidate();
        }

        try
        {
            var now = MalaysiaTime.Now;
            var today = DateOnly.FromDateTime(now);
            var current = await _prayerService.GetMonthAsync(
                requestedZone,
                today.Year,
                today.Month,
                forceRefresh);

            if (!string.Equals(requestedZone, _zone.JakimCode, StringComparison.OrdinalIgnoreCase))
            {
                _pendingRefresh = true;
                return;
            }

            var todayDay = current.Month.GetDay(today)
                ?? throw new PrayerDataException("Tiada data waktu solat untuk hari ini.");

            var yesterdayDate = today.AddDays(-1);
            PrayerMonthResult? previousResult = null;
            var previousMonth = current.Month;
            if (yesterdayDate.Month != today.Month || yesterdayDate.Year != today.Year)
            {
                previousResult = await _prayerService.GetMonthAsync(
                    requestedZone,
                    yesterdayDate.Year,
                    yesterdayDate.Month,
                    forceRefresh: false);

                if (!string.Equals(requestedZone, _zone.JakimCode, StringComparison.OrdinalIgnoreCase))
                {
                    _pendingRefresh = true;
                    return;
                }

                previousMonth = previousResult.Month;
            }

            var tomorrowDate = today.AddDays(1);
            PrayerMonthResult? nextResult = null;
            var nextMonth = current.Month;
            if (tomorrowDate.Month != today.Month || tomorrowDate.Year != today.Year)
            {
                nextResult = await _prayerService.GetMonthAsync(
                    requestedZone,
                    tomorrowDate.Year,
                    tomorrowDate.Month,
                    forceRefresh: false);

                if (!string.Equals(requestedZone, _zone.JakimCode, StringComparison.OrdinalIgnoreCase))
                {
                    _pendingRefresh = true;
                    return;
                }

                nextMonth = nextResult.Month;
            }

            var previousDay = previousMonth.GetDay(yesterdayDate);
            var tomorrowDay = nextMonth.GetDay(tomorrowDate);

            if (_loadedDate != today)
            {
                _notified.Clear();
            }

            _currentMonthResult = current;
            _today = todayDay;
            _yesterday = previousDay;
            _tomorrow = tomorrowDay;
            _loadedDate = today;
            _lastUpdated = current.Month.FetchedAt;
            _fromCache = current.FromCache
                || (previousResult?.FromCache ?? false)
                || (nextResult?.FromCache ?? false);
            _loadFailed = false;
            _updateFailed = false;
            _loading = false;

            RecalculateSchedule(now);
            MarkPastPrayers(now);
            UpdateTrayText();
            _taskbarNext.UpdateNext(_schedule?.Next, _schedule?.Remaining, _settings.Use24HourFormat);

            if (today.Day >= 25)
            {
                _ = PrefetchNextMonthAsync(today);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Debug.WriteLine($"Prayer refresh failed: {ex}");
            _loading = false;
            if (_schedule is null)
            {
                _loadFailed = true;
                _fromCache = false;
            }
            else
            {
                _updateFailed = true;
                _fromCache = true;
            }
        }
        finally
        {
            _refreshing = false;
            Invalidate();

            if (_pendingRefresh)
            {
                _pendingRefresh = false;
                _ = RefreshDataAsync(forceRefresh: true);
            }
        }
    }

    private async Task PrefetchNextMonthAsync(DateOnly today)
    {
        try
        {
            var next = new DateOnly(today.Year, today.Month, 1).AddMonths(1);
            await _prayerService.GetMonthAsync(_zone.JakimCode, next.Year, next.Month);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Next-month prefetch failed: {ex.Message}");
        }
    }

    private void RecalculateSchedule(DateTime now)
    {
        if (_today is null)
        {
            _schedule = null;
            return;
        }

        _schedule = PrayerScheduleCalculator.Calculate(_today, _yesterday, _tomorrow, now);
        UpdateTrayText();
    }

    private void MarkPastPrayers(DateTime now)
    {
        if (_today is null)
        {
            return;
        }

        foreach (var occurrence in PrayerKindExtensions.ObligatoryPrayers
                     .Select(kind => (Kind: kind, Time: _today.GetTime(kind)))
                     .Where(item => item.Time > DateTime.MinValue))
        {
            if (occurrence.Time < _startedAt
                || now - occurrence.Time > TimeSpan.FromMinutes(2))
            {
                _notified.Add(NotificationKey(_today.Date, occurrence.Kind));
            }
        }
    }

    private void CheckPrayerNotifications(DateTime now)
    {
        if (_today is null || _schedule is null)
        {
            return;
        }

        foreach (var occurrence in _schedule.TodayPrayers)
        {
            var key = NotificationKey(_today.Date, occurrence.Kind);
            if (_notified.Contains(key) || now < occurrence.Time)
            {
                continue;
            }

            var late = now - occurrence.Time;
            _notified.Add(key);

            if (late <= TimeSpan.FromMinutes(2))
            {
                ShowPrayerNotification(occurrence);
            }
        }
    }

    private void ShowPrayerNotification(PrayerOccurrence occurrence)
    {
        if (_settings.NotificationsEnabled)
        {
            _toast?.Close();
            var workingArea = Screen.FromControl(this).WorkingArea;
            var toast = new ToastForm(
                occurrence.Name,
                occurrence.Time,
                MalayText.ShortZoneName(_zone),
                _settings.Use24HourFormat,
                workingArea);
            toast.OpenRequested += (_, _) => ShowWidget();
            toast.FormClosed += (_, _) =>
            {
                if (ReferenceEquals(_toast, toast))
                {
                    _toast = null;
                }
            };
            _toast = toast;
            toast.Show();
        }

        if (_settings.AzanEnabled && _settings.AzanPrayers.Contains(occurrence.Kind))
        {
            _azanPlayer.Play(_settings.AzanFile, _settings.AzanVolume);
        }
    }

    private void ApplyLanguageTexts()
    {
        _trayShowItem.Text = AppText.Get("main.menu.show");
        _trayRefreshItem.Text = AppText.Get("main.menu.refresh");
        _trayZoneItem.Text = AppText.Get("main.menu.changeZone");
        _traySettingsItem.Text = AppText.Get("main.menu.settings");
        _trayStartupItem.Text = AppText.Get("main.menu.startWithWindows");
        _trayExitItem.Text = AppText.Get("main.menu.exit");

        _formRefreshItem.Text = AppText.Get("main.menu.refresh");
        _formZoneItem.Text = AppText.Get("main.menu.changeZone");
        _formSettingsItem.Text = AppText.Get("main.menu.settings");
        _formHideItem.Text = AppText.Get("main.menu.hide");
        _formExitItem.Text = AppText.Get("main.menu.exit");
    }

    private void UpdateTrayText()
    {
        if (_trayIcon is null)
        {
            return;
        }

        var next = _schedule?.Next;
        var zoneName = MalayText.ShortZoneName(_zone);
        var text = next is null
            ? AppText.Format("main.tray.noNext", zoneName)
            : $"{(next.IsTomorrow ? AppText.Get("common.tomorrow") + " " : string.Empty)}{next.Name} {MalayText.FormatTime(next.Time, _settings.Use24HourFormat)}  •  {zoneName}";

        if (text.Length > 63)
        {
            text = text[..60] + "...";
        }

        if (!string.Equals(text, _lastTrayText, StringComparison.Ordinal))
        {
            _lastTrayText = text;
            _trayIcon.Text = text;
        }
    }

    private void MainForm_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        if (_closeButton.Contains(e.Location))
        {
            HideToTray();
            return;
        }

        if (_pinButton.Contains(e.Location))
        {
            ToggleAlwaysOnTop();
            return;
        }

        if (_settingsButton.Contains(e.Location))
        {
            OpenSettings();
            return;
        }

        if (!_retryButton.IsEmpty && _retryButton.Contains(e.Location))
        {
            _ = RefreshDataAsync(forceRefresh: true);
            return;
        }

        if (e.Y <= 86 * _layoutScale)
        {
            _dragging = true;
            _dragStartCursor = Cursor.Position;
            _dragStartLocation = Location;
            Capture = true;
            Cursor = Cursors.SizeAll;
        }
    }

    private void MainForm_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_dragging)
        {
            var delta = new Size(
                Cursor.Position.X - _dragStartCursor.X,
                Cursor.Position.Y - _dragStartCursor.Y);
            Location = new Point(_dragStartLocation.X + delta.Width, _dragStartLocation.Y + delta.Height);
            return;
        }

        var hover = -1;
        if (_closeButton.Contains(e.Location))
        {
            hover = 2;
        }
        else if (_pinButton.Contains(e.Location))
        {
            hover = 1;
        }
        else if (_settingsButton.Contains(e.Location))
        {
            hover = 0;
        }

        if (hover != _hoverButton)
        {
            _hoverButton = hover;
            Invalidate();
        }

        if (hover >= 0
            || (!_retryButton.IsEmpty && _retryButton.Contains(e.Location)))
        {
            Cursor = Cursors.Hand;
        }
        else if (e.Y <= 86 * _layoutScale)
        {
            Cursor = Cursors.SizeAll;
        }
        else
        {
            Cursor = Cursors.Default;
        }
    }

    private void MainForm_MouseUp(object? sender, MouseEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        Capture = false;
        SnapToEdge();
        SaveWindowPosition();
        Cursor = Cursors.Default;
        Invalidate();
    }

    private void MainForm_MouseDoubleClick(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || e.Y > 86 * _layoutScale)
        {
            return;
        }

        if (_closeButton.Contains(e.Location)
            || _pinButton.Contains(e.Location)
            || _settingsButton.Contains(e.Location))
        {
            return;
        }

        _compact = !_compact;
        _settings.CompactMode = _compact;
        SaveSettings();
        ApplyLayout(initial: false);
        RebuildFonts();
        UpdateRegion();
        Invalidate();
    }

    private void MainForm_MouseLeave(object? sender, EventArgs e)
    {
        if (_hoverButton != -1 && !_dragging)
        {
            _hoverButton = -1;
            Invalidate();
        }
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            HideToTray();
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.R)
        {
            _ = RefreshDataAsync(forceRefresh: true);
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.Oemcomma)
        {
            OpenSettings();
            e.Handled = true;
        }
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        SaveWindowPosition();

        if (!_reallyExit
            && e.CloseReason is not (CloseReason.WindowsShutDown or CloseReason.TaskManagerClosing))
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        _reallyExit = true;
        _trayIcon.Visible = false;
    }

    private void HideToTray()
    {
        Hide();

        if (!_trayHintShown)
        {
            _trayHintShown = true;
            _trayIcon.BalloonTipTitle = AppText.Get("main.balloon.title");
            _trayIcon.BalloonTipText = AppText.Get("main.balloon.text");
            _trayIcon.BalloonTipIcon = ToolTipIcon.Info;
            _trayIcon.ShowBalloonTip(4000);
        }
    }

    private void ShowWidget()
    {
        if (!Visible)
        {
            Show();
        }

        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Normal;
        }

        TopMost = true;
        TopMost = _settings.AlwaysOnTop;
        NativeMethods.SetForegroundWindow(Handle);
        Activate();
        BringToFront();
    }

    private void ExitApplication()
    {
        _reallyExit = true;
        _clockTimer.Stop();
        _trayIcon.Visible = false;
        _toast?.Close();
        _azanPlayer.Stop();
        _taskbarNext.Hide();
        Close();
    }

    private void ToggleAlwaysOnTop()
    {
        _settings.AlwaysOnTop = !_settings.AlwaysOnTop;
        TopMost = _settings.AlwaysOnTop;
        SaveSettings();
        Invalidate();
    }

    private void ToggleStartup()
    {
        _settings.StartWithWindows = !_settings.StartWithWindows;
        _trayStartupItem.Checked = _settings.StartWithWindows;
        StartupService.SetEnabled(_settings.StartWithWindows);
        SaveSettings();
    }

    private void PickZone()
    {
        if (!Visible)
        {
            ShowWidget();
        }

        var restoreTopMost = TopMost;
        TopMost = false;
        try
        {
            using var picker = new ZonePickerForm(_catalog, _settings.ZoneCode);
            if (picker.ShowDialog(this) != DialogResult.OK
                || picker.SelectedZone is null
                || string.Equals(picker.SelectedZone.JakimCode, _zone.JakimCode, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _zone = picker.SelectedZone;
            _settings.ZoneCode = _zone.JakimCode;
            SaveSettings();
            ResetSchedule();
            _ = RefreshDataAsync(forceRefresh: true);
        }
        finally
        {
            TopMost = restoreTopMost;
        }
    }

    private void OpenSettings()
    {
        if (!Visible)
        {
            ShowWidget();
        }

        var originalSettings = _settings.Clone();
        var originalLocation = Location;
        var restoreTopMost = TopMost;
        TopMost = false;

        AppSettings? updatedSettings = null;
        try
        {
            using var form = new SettingsForm(
                _catalog,
                _settings,
                _azanPlayer,
                draft => ApplySettings(draft, preview: true));
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                updatedSettings = form.Result;
            }
        }
        finally
        {
            TopMost = restoreTopMost;
        }

        if (updatedSettings is null)
        {
            ApplySettings(originalSettings, preview: false);
            Location = ClampToScreen(originalLocation, Size);
            Invalidate();
            Update();
            return;
        }

        ApplySettings(updatedSettings, preview: false);
        SaveSettings(showError: true);
    }

    /// <summary>
    /// Applies a settings snapshot to the running widget. In preview mode the
    /// visual settings update immediately, while side effects that cannot be
    /// rolled back (topmost state and the startup registry entry) are deferred
    /// until the user saves.
    /// </summary>
    private void ApplySettings(AppSettings settings, bool preview)
    {
        var previousZone = _zone.JakimCode;

        _settings = settings.Clone();
        AppText.SetLanguage(_settings.Language);
        ApplyLanguageTexts();
        _zone = _catalog.Find(_settings.ZoneCode) ?? _catalog.Find("WLY01")!;
        _theme = ThemeCatalog.Get(_settings.Theme);
        _compact = _settings.CompactMode;

        Opacity = Math.Clamp(_settings.OpacityPercent, 70, 100) / 100f;

        _trayStartupItem.Checked = _settings.StartWithWindows;
        if (!preview)
        {
            TopMost = _settings.AlwaysOnTop;
            StartupService.SetEnabled(_settings.StartWithWindows);
        }

        ApplyLayout(initial: false);
        RebuildFonts();
        UpdateRegion();
        UpdateTrayText();
        _taskbarNext.ApplyAppearance(_theme, _settings.TaskbarNext, _settings.Use24HourFormat);
        _taskbarNext.UpdateNext(_schedule?.Next, _schedule?.Remaining, _settings.Use24HourFormat);
        _toast?.Invalidate();
        Invalidate();
        Update();

        if (!string.Equals(previousZone, _zone.JakimCode, StringComparison.OrdinalIgnoreCase))
        {
            ResetSchedule();
            if (_refreshing)
            {
                _pendingRefresh = true;
            }
            else
            {
                _ = RefreshDataAsync(forceRefresh: true);
            }
        }
    }

    private void ResetSchedule()
    {
        _currentMonthResult = null;
        _today = null;
        _yesterday = null;
        _tomorrow = null;
        _schedule = null;
        _loadedDate = null;
        _lastUpdated = null;
        _fromCache = false;
        _taskbarNext.UpdateNext(null, null, _settings.Use24HourFormat);
        _loading = true;
        _loadFailed = false;
        _updateFailed = false;
        _notified.Clear();
        _lastTrayText = string.Empty;
        UpdateTrayText();
        Invalidate();
    }

    private void SaveSettings(bool showError = false)
    {
        _settings.WindowX = Location.X;
        _settings.WindowY = Location.Y;

        try
        {
            _settingsService.Save(_settings);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Could not save settings: {ex.Message}");
            if (showError)
            {
                MessageBox.Show(
                    this,
                    AppText.Get("main.error.saveSettings"),
                    AppText.Get("common.errorTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }

    private void SaveWindowPosition()
    {
        _settings.WindowX = Location.X;
        _settings.WindowY = Location.Y;
        SaveSettings();
    }

    private void SnapToEdge()
    {
        var screen = Screen.FromControl(this);
        var area = screen.WorkingArea;
        var margin = 22 * _layoutScale;
        var x = Location.X;
        var y = Location.Y;

        if (Math.Abs(x - area.Left) < margin)
        {
            x = area.Left;
        }
        else if (Math.Abs(area.Right - (x + Width)) < margin)
        {
            x = area.Right - Width;
        }

        if (Math.Abs(y - area.Top) < margin)
        {
            y = area.Top;
        }
        else if (Math.Abs(area.Bottom - (y + Height)) < margin)
        {
            y = area.Bottom - Height;
        }

        Location = new Point(x, y);
    }

    private void ApplyLayout(bool initial)
    {
        _fontScale = _settings.WidgetSize switch
        {
            WidgetSizePreset.Kecil => 0.90f,
            WidgetSizePreset.Besar => 1.12f,
            _ => 1.0f
        };

        _layoutScale = _fontScale * (DeviceDpi / 96f);
        var extraRows = _settings.ShowImsak ? 1 : 0;
        extraRows += _settings.ShowDhuha ? 1 : 0;
        var baseHeight = _compact ? BaseCompactHeight : BaseHeight + extraRows * BaseRowHeight;
        var targetSize = new Size(
            (int)Math.Round(BaseWidth * _layoutScale),
            (int)Math.Round(baseHeight * _layoutScale));

        if (initial)
        {
            var desired = GetInitialLocation(targetSize);
            Size = targetSize;
            Location = ClampToScreen(desired, targetSize);
        }
        else
        {
            var currentLocation = Location;
            Size = targetSize;
            Location = ClampToScreen(currentLocation, targetSize);
        }

        CalculateButtonRects();
        UpdateRegion();
        Invalidate();
    }

    private Point GetInitialLocation(Size targetSize)
    {
        if (_settings.WindowX is int savedX && _settings.WindowY is int savedY)
        {
            var savedRect = new Rectangle(savedX, savedY, targetSize.Width, targetSize.Height);
            if (Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(savedRect)))
            {
                return new Point(savedX, savedY);
            }
        }

        var area = Screen.PrimaryScreen?.WorkingArea ?? SystemInformation.WorkingArea;
        var margin = (int)Math.Round(24 * _layoutScale);
        return new Point(
            area.Right - targetSize.Width - margin,
            area.Bottom - targetSize.Height - margin);
    }

    private static Point ClampToScreen(Point location, Size size)
    {
        var screen = Screen.FromRectangle(new Rectangle(location, size));
        var area = screen.WorkingArea;
        var x = Math.Clamp(location.X, area.Left, Math.Max(area.Left, area.Right - size.Width));
        var y = Math.Clamp(location.Y, area.Top, Math.Max(area.Top, area.Bottom - size.Height));
        return new Point(x, y);
    }

    private void CalculateButtonRects()
    {
        var scale = _layoutScale;
        var buttonSize = 30 * scale;
        var gap = 6 * scale;
        var top = 14 * scale;
        var right = ClientSize.Width - 14 * scale;

        _closeButton = new RectangleF(right - buttonSize, top, buttonSize, buttonSize);
        _pinButton = new RectangleF(_closeButton.Left - gap - buttonSize, top, buttonSize, buttonSize);
        _settingsButton = new RectangleF(_pinButton.Left - gap - buttonSize, top, buttonSize, buttonSize);
    }

    private void RebuildFonts()
    {
        DisposeFonts();

        var body = AppResources.BodyFontFamily;
        var heading = AppResources.HeadingFontFamily;
        var icon = AppResources.IconFontFamily;

        _titleFont = new Font(heading, 16.2f * _fontScale, FontStyle.Bold, GraphicsUnit.Point);
        _dateFont = new Font(body, 9.4f * _fontScale, FontStyle.Regular, GraphicsUnit.Point);
        _hijriFont = new Font(body, 9.1f * _fontScale, FontStyle.Regular, GraphicsUnit.Point);
        _heroLabelFont = new Font(body, 8.1f * _fontScale, FontStyle.Bold, GraphicsUnit.Point);
        _heroNameFont = new Font(heading, 22.5f * _fontScale, FontStyle.Bold, GraphicsUnit.Point);
        _heroTimeFont = new Font(heading, 30f * _fontScale, FontStyle.Bold, GraphicsUnit.Point);
        _countdownFont = new Font(body, 10.1f * _fontScale, FontStyle.Bold, GraphicsUnit.Point);
        _prayerNameFont = new Font(body, 10.2f * _fontScale, FontStyle.Regular, GraphicsUnit.Point);
        _prayerNameBoldFont = new Font(body, 10.2f * _fontScale, FontStyle.Bold, GraphicsUnit.Point);
        _prayerTimeFont = new Font(body, 11.1f * _fontScale, FontStyle.Bold, GraphicsUnit.Point);
        _footerFont = new Font(body, 8.5f * _fontScale, FontStyle.Regular, GraphicsUnit.Point);
        _iconFont = new Font(icon, 10.5f * _fontScale, FontStyle.Regular, GraphicsUnit.Point);
        _errorTitleFont = new Font(heading, 14f * _fontScale, FontStyle.Bold, GraphicsUnit.Point);
        _errorBodyFont = new Font(body, 9.3f * _fontScale, FontStyle.Regular, GraphicsUnit.Point);
    }

    private void DisposeFonts()
    {
        _titleFont?.Dispose();
        _dateFont?.Dispose();
        _hijriFont?.Dispose();
        _heroLabelFont?.Dispose();
        _heroNameFont?.Dispose();
        _heroTimeFont?.Dispose();
        _countdownFont?.Dispose();
        _prayerNameFont?.Dispose();
        _prayerNameBoldFont?.Dispose();
        _prayerTimeFont?.Dispose();
        _footerFont?.Dispose();
        _iconFont?.Dispose();
        _errorTitleFont?.Dispose();
        _errorBodyFont?.Dispose();

        _titleFont = null;
        _dateFont = null;
        _hijriFont = null;
        _heroLabelFont = null;
        _heroNameFont = null;
        _heroTimeFont = null;
        _countdownFont = null;
        _prayerNameFont = null;
        _prayerNameBoldFont = null;
        _prayerTimeFont = null;
        _footerFont = null;
        _iconFont = null;
        _errorTitleFont = null;
        _errorBodyFont = null;
    }

    private void UpdateRegion()
    {
        if (!IsHandleCreated || ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        using var path = DrawingHelper.RoundedRect(
            new RectangleF(0, 0, ClientSize.Width, ClientSize.Height),
            BaseCornerRadius * _layoutScale);
        var previous = Region;
        Region = new Region(path);
        previous?.Dispose();
    }

    private static string Capitalize(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return char.ToUpper(text[0], CultureInfo.InvariantCulture) + text[1..];
    }

    private static long NotificationKey(DateOnly date, PrayerKind kind) =>
        ((long)date.DayNumber << 8) | (byte)kind;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _clockTimer.Stop();
            _clockTimer.Dispose();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayMenu.Dispose();
            ContextMenuStrip = null;
            _formMenu.Dispose();
            _toast?.Dispose();
            _toast = null;
            _azanPlayer.Dispose();
            _taskbarNext.Dispose();
            DisposeFonts();
        }

        base.Dispose(disposing);
    }
}

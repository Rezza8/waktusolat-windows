using System.Drawing;
using System.Windows.Forms;
using SolatWaktu.Core.Models;
using SolatWaktu.Core.Services;
using SolatWaktu.Widget.Localization;
using SolatWaktu.Widget.Services;
using SolatWaktu.Widget.Theme;

namespace SolatWaktu.Widget.UI;

internal sealed class SettingsForm : Form
{
    private const string CreditText = "Created By Rez Ren Using Opencode";

    private readonly ZoneCatalog _catalog;
    private readonly AppSettings _settings;
    private readonly Action<AppSettings>? _preview;
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly Label _zoneLabel;
    private readonly Label _languageLabel;
    private readonly Label _displayLabel;
    private readonly Label _themeLabel;
    private readonly Label _formatLabel;
    private readonly Label _sizeLabel;
    private readonly Label _opacityLabel;
    private readonly Label _taskbarLabel;
    private readonly Label _taskbarHint;
    private readonly Label _notifyLabel;
    private readonly Label _azanTimesLabel;
    private readonly Label _volumeLabel;
    private readonly Label _azanFileLabel;
    private readonly Button _zoneButton;
    private readonly DarkComboBox _languageCombo;
    private readonly DarkComboBox _themeCombo;
    private readonly DarkComboBox _formatCombo;
    private readonly DarkComboBox _sizeCombo;
    private readonly TrackBar _opacityTrack;
    private readonly Panel _opacitySliderHost;
    private readonly Label _opacityValue;
    private readonly DarkComboBox _taskbarPositionCombo;
    private readonly CheckBox _alwaysOnTop;
    private readonly CheckBox _startWithWindows;
    private readonly CheckBox _showImsak;
    private readonly CheckBox _showDhuha;
    private readonly CheckBox _notifications;
    private readonly CheckBox _azan;
    private readonly FlowLayoutPanel _azanPrayerPanel;
    private readonly TrackBar _volumeTrack;
    private readonly Panel _volumeSliderHost;
    private readonly Label _volumeValue;
    private readonly DarkComboBox _azanFileCombo;
    private readonly Button _testNotificationButton;
    private readonly Button _saveButton;
    private readonly Button _cancelButton;
    private readonly Label _creditLabel;
    private readonly AzanPlayer _azanPlayer;
    private readonly System.Windows.Forms.Timer _testSoundTimer;
    private readonly Button _testSoundButton;
    private readonly List<(Control Control, string Key)> _localizedControls = [];

    private Zone _selectedZone;
    private AppSettings? _result;
    private bool _suppressPreview;
    private bool _testSoundStarted;
    private ToastForm? _testToast;

    public SettingsForm(
        ZoneCatalog catalog,
        AppSettings settings,
        AzanPlayer azanPlayer,
        Action<AppSettings>? preview = null)
    {
        _catalog = catalog;
        _settings = settings.Clone();
        _azanPlayer = azanPlayer;
        _preview = preview;
        _suppressPreview = true;
        _testSoundTimer = new System.Windows.Forms.Timer { Interval = 500 };
        _testSoundTimer.Tick += TestSoundTimer_Tick;
        AppText.SetLanguage(_settings.Language);

        Text = AppText.Get("settings.title");
        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScroll = true;
        BackColor = Color.FromArgb(21, 21, 34);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9.5f);
        ClientSize = new Size(560, 718);

        _selectedZone = _catalog.Find(_settings.ZoneCode) ?? _catalog.Find("WLY01")!;

        _titleLabel = new Label
        {
            Text = AppText.Get("settings.title"),
            Font = new Font("Segoe UI", 19f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(24, 18)
        };
        _localizedControls.Add((_titleLabel, "settings.title"));

        _subtitleLabel = new Label
        {
            Text = AppText.Get("settings.subtitle"),
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(160, 210, 220, 238),
            AutoSize = true,
            Location = new Point(26, 52)
        };
        _localizedControls.Add((_subtitleLabel, "settings.subtitle"));

        _zoneLabel = CreateLocalizedSection("settings.section.zone", 24, 86);
        _languageLabel = CreateLocalizedSection("settings.section.language", 304, 86);
        _zoneButton = new Button
        {
            Location = new Point(24, 108),
            Size = new Size(268, 44),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(32, 32, 51),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 12, 0),
            Font = new Font("Segoe UI", 9.7f),
            AutoEllipsis = true,
            UseVisualStyleBackColor = false,
            FlatAppearance = { BorderSize = 0 }
        };
        _zoneButton.Click += (_, _) => PickZone();

        _languageCombo = CreateCombo(new Point(304, 108), new Size(232, 44));
        _languageCombo.SetItems(
        [
            AppText.Get("settings.language.malay"),
            AppText.Get("settings.language.english")
        ]);
        _languageCombo.SelectedIndex = _settings.Language == AppLanguage.English ? 1 : 0;
        _languageCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressPreview)
            {
                return;
            }

            ApplyLanguage((AppLanguage)Math.Clamp(_languageCombo.SelectedIndex, 0, 1));
            Preview();
        };

        _displayLabel = CreateLocalizedSection("settings.section.display", 24, 172);
        _themeLabel = CreateLocalizedField("settings.theme", 24, 200);
        _formatLabel = CreateLocalizedField("settings.timeFormat", 304, 200);
        _sizeLabel = CreateLocalizedField("settings.size", 416, 200);

        _themeCombo = CreateCombo(new Point(24, 220), new Size(268, 32));
        _themeCombo.SetItems(ThemeCatalog.All.Select(theme => theme.Name));
        var themeIndex = ThemeCatalog.All
            .Select((theme, index) => (theme, index))
            .FirstOrDefault(pair => string.Equals(pair.theme.Name, _settings.Theme, StringComparison.OrdinalIgnoreCase))
            .index;
        _themeCombo.SelectedIndex = themeIndex;
        _themeCombo.SelectedIndexChanged += (_, _) => Preview();

        _formatCombo = CreateCombo(new Point(304, 220), new Size(100, 32));
        _formatCombo.SetItems([AppText.Get("common.format24"), AppText.Get("common.format12")]);
        _formatCombo.SelectedIndex = _settings.Use24HourFormat ? 0 : 1;
        _formatCombo.SelectedIndexChanged += (_, _) => Preview();

        _sizeCombo = CreateCombo(new Point(416, 220), new Size(120, 32));
        _sizeCombo.SetItems(
        [
            AppText.Get("common.small"),
            AppText.Get("common.medium"),
            AppText.Get("common.large")
        ]);
        _sizeCombo.SelectedIndex = (int)_settings.WidgetSize;
        _sizeCombo.SelectedIndexChanged += (_, _) => Preview();

        _opacityLabel = CreateLocalizedField("settings.opacity", 24, 266);
        _opacityValue = CreateValueLabel(470, 266, $"{_settings.OpacityPercent}%");
        _opacityTrack = new TrackBar
        {
            Location = new Point(24, 286),
            Width = 512,
            Height = 32,
            Minimum = 70,
            Maximum = 100,
            TickFrequency = 5,
            Value = _settings.OpacityPercent,
            BackColor = Color.FromArgb(21, 21, 34),
            TickStyle = TickStyle.None,
            SmallChange = 1,
            LargeChange = 5
        };
        _opacityTrack.ValueChanged += (_, _) =>
        {
            _opacityValue.Text = $"{_opacityTrack.Value}%";
            Preview();
        };
        _opacitySliderHost = CreateSliderHost(_opacityTrack, new Point(24, 286), 512);

        _taskbarLabel = CreateLocalizedField("settings.taskbar", 24, 384);
        _taskbarPositionCombo = CreateCombo(new Point(24, 404), new Size(268, 32));
        _taskbarPositionCombo.SetItems(
        [
            AppText.Get("common.off"),
            AppText.Get("common.left"),
            AppText.Get("common.center"),
            AppText.Get("common.right")
        ]);
        _taskbarPositionCombo.SelectedIndex = (int)_settings.TaskbarNext;
        _taskbarPositionCombo.SelectedIndexChanged += (_, _) => Preview();

        _taskbarHint = new Label
        {
            Text = AppText.Get("settings.taskbarHint"),
            ForeColor = Color.FromArgb(130, 210, 220, 238),
            Font = new Font("Segoe UI", 8.5f),
            AutoSize = true,
            Location = new Point(304, 410)
        };
        _localizedControls.Add((_taskbarHint, "settings.taskbarHint"));

        _alwaysOnTop = CreateLocalizedCheckBox("settings.alwaysOnTop", 24, 322, 220, _settings.AlwaysOnTop);
        _startWithWindows = CreateLocalizedCheckBox("settings.startWithWindows", 260, 322, 260, _settings.StartWithWindows);
        _showImsak = CreateLocalizedCheckBox("settings.showImsak", 24, 352, 220, _settings.ShowImsak);
        _showDhuha = CreateLocalizedCheckBox("settings.showDhuha", 260, 352, 220, _settings.ShowDhuha);

        _alwaysOnTop.CheckedChanged += (_, _) => Preview();
        _startWithWindows.CheckedChanged += (_, _) => Preview();
        _showImsak.CheckedChanged += (_, _) => Preview();
        _showDhuha.CheckedChanged += (_, _) => Preview();

        var divider = new Panel
        {
            Location = new Point(24, 448),
            Size = new Size(512, 1),
            BackColor = Color.FromArgb(54, 54, 80)
        };

        _notifyLabel = CreateLocalizedSection("settings.section.notifications", 24, 462);
        _notifications = CreateLocalizedCheckBox("settings.notifications", 24, 488, 260, _settings.NotificationsEnabled);
        _notifications.CheckedChanged += (_, _) => Preview();
        _azan = CreateLocalizedCheckBox("settings.azan", 300, 488, 220, _settings.AzanEnabled);
        _azan.CheckedChanged += (_, _) =>
        {
            UpdateAzanEnabledState();
            Preview();
        };

        _azanTimesLabel = CreateLocalizedField("settings.azanTimes", 24, 520);
        _azanPrayerPanel = new FlowLayoutPanel
        {
            Location = new Point(24, 540),
            Size = new Size(512, 32),
            WrapContents = false,
            AutoScroll = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        foreach (var kind in PrayerKindExtensions.ObligatoryPrayers)
        {
            var checkbox = CreateCheckBox(
                kind.MalayName(),
                0,
                0,
                92,
                _settings.AzanPrayers.Contains(kind));
            checkbox.Tag = kind;
            checkbox.Margin = new Padding(0, 0, 6, 0);
            checkbox.CheckedChanged += (_, _) => Preview();
            _azanPrayerPanel.Controls.Add(checkbox);
        }

        _volumeLabel = CreateLocalizedField("settings.volume", 24, 576);
        _volumeValue = CreateValueLabel(470, 576, $"{_settings.AzanVolume}%");
        _volumeTrack = new TrackBar
        {
            Location = new Point(24, 596),
            Width = 340,
            Height = 32,
            Minimum = 0,
            Maximum = 100,
            TickFrequency = 10,
            Value = _settings.AzanVolume,
            BackColor = Color.FromArgb(21, 21, 34),
            TickStyle = TickStyle.None,
            SmallChange = 1,
            LargeChange = 5
        };
        _volumeTrack.ValueChanged += (_, _) =>
        {
            _volumeValue.Text = $"{_volumeTrack.Value}%";
            Preview();
        };
        _volumeSliderHost = CreateSliderHost(_volumeTrack, new Point(24, 596), 340);

        _azanFileLabel = CreateLocalizedField("settings.azanType", 390, 576);
        _azanFileCombo = CreateCombo(new Point(390, 596), new Size(146, 32));
        _azanFileCombo.SetItems([AppText.Get("common.short"), AppText.Get("common.long")]);
        _azanFileCombo.SelectedIndex = string.Equals(_settings.AzanFile, "long", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        _azanFileCombo.SelectedIndexChanged += (_, _) => Preview();

        _testNotificationButton = CreateLocalizedButton("common.testNotification", Color.FromArgb(43, 43, 64));
        _testNotificationButton.Location = new Point(24, 654);
        _testNotificationButton.Size = new Size(130, 40);
        _testNotificationButton.Click += (_, _) => TestNotification();

        _testSoundButton = CreateDialogButton(AppText.Get("common.testSound"), Color.FromArgb(43, 43, 64));
        _testSoundButton.Location = new Point(164, 654);
        _testSoundButton.Size = new Size(130, 40);
        _testSoundButton.Click += (_, _) => ToggleTestSound();

        _saveButton = CreateLocalizedButton("common.save", Color.FromArgb(0, 180, 140));
        _saveButton.Location = new Point(312, 654);
        _saveButton.Size = new Size(112, 40);
        _saveButton.Click += (_, _) => SaveAndClose();

        _cancelButton = CreateLocalizedButton("common.cancel", Color.FromArgb(43, 43, 64));
        _cancelButton.Location = new Point(432, 654);
        _cancelButton.Size = new Size(104, 40);
        _cancelButton.DialogResult = DialogResult.Cancel;

        _creditLabel = new Label
        {
            Text = CreditText,
            Font = new Font("Segoe UI", 7.8f),
            ForeColor = Color.FromArgb(120, 190, 200, 225),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(24, 697),
            Size = new Size(512, 16)
        };

        AcceptButton = _saveButton;
        CancelButton = _cancelButton;

        Controls.AddRange(
        [
            _titleLabel, _subtitleLabel,
            _zoneLabel, _zoneButton, _languageLabel, _languageCombo, _displayLabel,
            _themeLabel, _formatLabel, _sizeLabel, _themeCombo, _formatCombo, _sizeCombo,
            _opacityLabel, _opacityValue, _opacitySliderHost,
            _taskbarLabel, _taskbarPositionCombo, _taskbarHint,
            _alwaysOnTop, _startWithWindows, _showImsak, _showDhuha,
            divider, _notifyLabel, _notifications, _azan, _azanTimesLabel, _azanPrayerPanel,
            _volumeLabel, _volumeValue, _volumeSliderHost, _azanFileLabel, _azanFileCombo,
            _testNotificationButton, _testSoundButton,
            _saveButton, _cancelButton, _creditLabel
        ]);

        UpdateAzanEnabledState();
        UpdateZoneButton();
        _suppressPreview = false;
        Shown += (_, _) => _zoneButton.Focus();
    }

    public AppSettings Result => _result ?? CreateDraft();

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

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _testSoundTimer.Stop();
        _testSoundTimer.Dispose();

        if (_testSoundStarted)
        {
            _testSoundStarted = false;
            _azanPlayer.Stop();
        }

        _testToast?.Close();
        _testToast = null;
        base.OnFormClosed(e);
    }

    private void PickZone()
    {
        using var picker = new ZonePickerForm(_catalog, _selectedZone.JakimCode);
        if (picker.ShowDialog(this) == DialogResult.OK && picker.SelectedZone is not null)
        {
            _selectedZone = picker.SelectedZone;
            UpdateZoneButton();
            Preview();
        }
    }

    private void UpdateZoneButton()
    {
        _zoneButton.Text = _selectedZone.DisplayName;
    }

    private void ApplyLanguage(AppLanguage language)
    {
        AppText.SetLanguage(language);
        Text = AppText.Get("settings.title");

        foreach (var (control, key) in _localizedControls)
        {
            control.Text = AppText.Get(key);
        }

        var formatIndex = _formatCombo.SelectedIndex;
        var sizeIndex = _sizeCombo.SelectedIndex;
        var taskbarIndex = _taskbarPositionCombo.SelectedIndex;
        var azanFileIndex = _azanFileCombo.SelectedIndex;

        _suppressPreview = true;
        _languageCombo.SetItems(
        [
            AppText.Get("settings.language.malay"),
            AppText.Get("settings.language.english")
        ]);
        _languageCombo.SelectedIndex = language == AppLanguage.English ? 1 : 0;

        _formatCombo.SetItems([AppText.Get("common.format24"), AppText.Get("common.format12")]);
        _formatCombo.SelectedIndex = formatIndex;

        _sizeCombo.SetItems(
        [
            AppText.Get("common.small"),
            AppText.Get("common.medium"),
            AppText.Get("common.large")
        ]);
        _sizeCombo.SelectedIndex = sizeIndex;

        _taskbarPositionCombo.SetItems(
        [
            AppText.Get("common.off"),
            AppText.Get("common.left"),
            AppText.Get("common.center"),
            AppText.Get("common.right")
        ]);
        _taskbarPositionCombo.SelectedIndex = taskbarIndex;

        _azanFileCombo.SetItems([AppText.Get("common.short"), AppText.Get("common.long")]);
        _azanFileCombo.SelectedIndex = azanFileIndex;
        _suppressPreview = false;

        _testSoundButton.Text = AppText.Get(
            _testSoundStarted ? "common.stopSound" : "common.testSound");
    }

    private void UpdateAzanEnabledState()
    {
        var enabled = _azan.Checked;
        _azanPrayerPanel.Enabled = enabled;
        _volumeTrack.Enabled = enabled;
        _volumeValue.Enabled = enabled;
        _azanFileCombo.Enabled = enabled;

        foreach (Control control in _azanPrayerPanel.Controls)
        {
            control.Enabled = enabled;
        }
    }

    private void TestNotification()
    {
        var draft = CreateDraft();

        _testToast?.Close();
        var toast = new ToastForm(
            "Zohor",
            DateTime.Today.AddHours(12).AddMinutes(30),
            MalayText.ShortZoneName(_selectedZone),
            draft.Use24HourFormat,
            Screen.FromControl(this).WorkingArea,
            isTest: true);
        toast.FormClosed += (_, _) =>
        {
            if (ReferenceEquals(_testToast, toast))
            {
                _testToast = null;
            }
        };
        _testToast = toast;
        toast.Show();
    }

    private void ToggleTestSound()
    {
        if (_testSoundStarted)
        {
            StopTestSound();
            return;
        }

        var draft = CreateDraft();
        _azanPlayer.Play(draft.AzanFile, draft.AzanVolume);
        if (!_azanPlayer.IsPlaying)
        {
            MessageBox.Show(
                this,
                AppText.Get("settings.error.audio"),
                AppText.Get("common.testSound"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _testSoundStarted = true;
        _testSoundButton.Text = AppText.Get("common.stopSound");
        _testSoundTimer.Start();
    }

    private void StopTestSound()
    {
        _testSoundStarted = false;
        _azanPlayer.Stop();
        ResetTestSound();
    }

    private void ResetTestSound()
    {
        _testSoundTimer.Stop();
        _testSoundButton.Text = AppText.Get("common.testSound");
    }

    private void TestSoundTimer_Tick(object? sender, EventArgs e)
    {
        if (_azanPlayer.IsPlaying)
        {
            return;
        }

        _testSoundStarted = false;
        ResetTestSound();
    }

    private AppSettings CreateDraft()
    {
        var draft = _settings.Clone();
        draft.ZoneCode = _selectedZone.JakimCode;
        draft.Language = _languageCombo.SelectedIndex == 1
            ? AppLanguage.English
            : AppLanguage.Malay;
        draft.Theme = _themeCombo.SelectedItem as string ?? "Midnight";
        draft.Use24HourFormat = _formatCombo.SelectedIndex == 0;
        draft.WidgetSize = (WidgetSizePreset)Math.Clamp(_sizeCombo.SelectedIndex, 0, 2);
        draft.TaskbarNext = (TaskbarNextAlignment)Math.Clamp(_taskbarPositionCombo.SelectedIndex, 0, 3);
        draft.OpacityPercent = _opacityTrack.Value;
        draft.AlwaysOnTop = _alwaysOnTop.Checked;
        draft.StartWithWindows = _startWithWindows.Checked;
        draft.ShowImsak = _showImsak.Checked;
        draft.ShowDhuha = _showDhuha.Checked;
        draft.NotificationsEnabled = _notifications.Checked;
        draft.AzanEnabled = _azan.Checked;
        draft.AzanVolume = _volumeTrack.Value;
        draft.AzanFile = _azanFileCombo.SelectedIndex == 1 ? "long" : "short";

        var selectedPrayers = new HashSet<PrayerKind>();
        foreach (Control control in _azanPrayerPanel.Controls)
        {
            if (control is CheckBox { Checked: true, Tag: PrayerKind kind })
            {
                selectedPrayers.Add(kind);
            }
        }
        draft.AzanPrayers = selectedPrayers;

        SettingsService.Normalize(draft);
        return draft;
    }

    private void Preview()
    {
        if (_suppressPreview)
        {
            return;
        }

        _preview?.Invoke(CreateDraft());
    }

    private void SaveAndClose()
    {
        _result = CreateDraft();
        DialogResult = DialogResult.OK;
        Close();
    }

    private static Panel CreateSliderHost(TrackBar track, Point location, int width)
    {
        var host = new Panel
        {
            Location = location,
            Size = new Size(width, 34),
            BackColor = Color.FromArgb(21, 21, 34),
            TabStop = false
        };

        // Native TrackBar enforces a ~45 px height even when Height is set to
        // 32, which made it overlap the first checkbox row. The host clips the
        // unused padding without shrinking the slider itself.
        track.Location = Point.Empty;
        track.Width = width;
        track.Height = 32;
        host.Controls.Add(track);
        return host;
    }

    private Label CreateLocalizedSection(string key, int x, int y)
    {
        var label = CreateSectionLabel(AppText.Get(key), x, y);
        _localizedControls.Add((label, key));
        return label;
    }

    private Label CreateLocalizedField(string key, int x, int y)
    {
        var label = CreateFieldLabel(AppText.Get(key), x, y);
        _localizedControls.Add((label, key));
        return label;
    }

    private CheckBox CreateLocalizedCheckBox(
        string key,
        int x,
        int y,
        int width,
        bool isChecked)
    {
        var checkBox = CreateCheckBox(AppText.Get(key), x, y, width, isChecked);
        _localizedControls.Add((checkBox, key));
        return checkBox;
    }

    private Button CreateLocalizedButton(string key, Color backColor)
    {
        var button = CreateDialogButton(AppText.Get(key), backColor);
        _localizedControls.Add((button, key));
        return button;
    }

    private static Label CreateSectionLabel(string text, int x, int y) => new()
    {
        Text = text,
        ForeColor = Color.FromArgb(0, 212, 170),
        Font = new Font("Segoe UI", 8.2f, FontStyle.Bold),
        AutoSize = true,
        Location = new Point(x, y)
    };

    private static Label CreateFieldLabel(string text, int x, int y) => new()
    {
        Text = text,
        ForeColor = Color.FromArgb(170, 210, 220, 238),
        Font = new Font("Segoe UI", 8.8f),
        AutoSize = true,
        Location = new Point(x, y)
    };

    private static Label CreateValueLabel(int x, int y, string text) => new()
    {
        Text = text,
        ForeColor = Color.FromArgb(225, 236, 240, 255),
        Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        AutoSize = false,
        TextAlign = ContentAlignment.MiddleRight,
        Size = new Size(66, 20),
        Location = new Point(x, y)
    };

    private static DarkComboBox CreateCombo(Point location, Size size) => new()
    {
        Location = location,
        Size = size,
        Font = new Font("Segoe UI", 9.5f)
    };

    private static CheckBox CreateCheckBox(string text, int x, int y, int width, bool isChecked) => new DarkCheckBox
    {
        Text = text,
        Location = new Point(x, y),
        Width = width,
        Height = 24,
        Checked = isChecked,
        ForeColor = Color.FromArgb(232, 238, 250),
        BackColor = Color.Transparent,
        Font = new Font("Segoe UI", 9.3f)
    };

    private static Button CreateDialogButton(string text, Color backColor) => new()
    {
        Text = text,
        FlatStyle = FlatStyle.Flat,
        BackColor = backColor,
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 9.6f, FontStyle.Bold),
        FlatAppearance = { BorderSize = 0 },
        UseVisualStyleBackColor = false
    };
}

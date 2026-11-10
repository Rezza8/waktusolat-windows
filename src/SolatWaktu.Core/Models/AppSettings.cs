namespace SolatWaktu.Core.Models;

public enum WidgetSizePreset
{
    Kecil,
    Sederhana,
    Besar
}

public enum TaskbarNextAlignment
{
    Off,
    Left,
    Center,
    Right
}

public sealed class AppSettings
{
    public string ZoneCode { get; set; } = "WLY01";
    public string Theme { get; set; } = "Midnight";
    public AppLanguage Language { get; set; } = AppLanguage.Malay;
    public bool Use24HourFormat { get; set; } = true;
    public bool AlwaysOnTop { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public bool NotificationsEnabled { get; set; } = true;
    public bool AzanEnabled { get; set; }
    public HashSet<PrayerKind> AzanPrayers { get; set; } =
    [
        PrayerKind.Fajr,
        PrayerKind.Dhuhr,
        PrayerKind.Asr,
        PrayerKind.Maghrib,
        PrayerKind.Isha
    ];

    public int AzanVolume { get; set; } = 70;
    public string AzanFile { get; set; } = "short";
    public int OpacityPercent { get; set; } = 100;
    public WidgetSizePreset WidgetSize { get; set; } = WidgetSizePreset.Sederhana;
    public TaskbarNextAlignment TaskbarNext { get; set; } = TaskbarNextAlignment.Off;
    public bool CompactMode { get; set; }
    public bool ShowImsak { get; set; }
    public bool ShowDhuha { get; set; }
    public int? WindowX { get; set; }
    public int? WindowY { get; set; }

    public AppSettings Clone()
    {
        var clone = (AppSettings)MemberwiseClone();
        clone.AzanPrayers = new HashSet<PrayerKind>(AzanPrayers);
        return clone;
    }
}

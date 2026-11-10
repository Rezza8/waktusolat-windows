namespace SolatWaktu.Core.Models;

/// <summary>
/// Prayer times for one calendar day, expressed in Malaysia time.
/// </summary>
public sealed class PrayerDay
{
    public DateOnly Date { get; init; }

    /// <summary>Hijri date in the API format, for example 1448-05-29.</summary>
    public string Hijri { get; init; } = string.Empty;

    public DateTime Imsak { get; init; }
    public DateTime Fajr { get; init; }
    public DateTime Syuruk { get; init; }
    public DateTime Dhuha { get; init; }
    public DateTime Dhuhr { get; init; }
    public DateTime Asr { get; init; }
    public DateTime Maghrib { get; init; }
    public DateTime Isha { get; init; }

    public DateTime GetTime(PrayerKind kind) => kind switch
    {
        PrayerKind.Imsak => Imsak,
        PrayerKind.Fajr => Fajr,
        PrayerKind.Syuruk => Syuruk,
        PrayerKind.Dhuha => Dhuha,
        PrayerKind.Dhuhr => Dhuhr,
        PrayerKind.Asr => Asr,
        PrayerKind.Maghrib => Maghrib,
        PrayerKind.Isha => Isha,
        _ => DateTime.MinValue
    };

    public bool HasTime(PrayerKind kind) => GetTime(kind) > DateTime.MinValue;
}

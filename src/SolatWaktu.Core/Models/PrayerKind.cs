namespace SolatWaktu.Core.Models;

public enum PrayerKind
{
    Imsak,
    Fajr,
    Syuruk,
    Dhuha,
    Dhuhr,
    Asr,
    Maghrib,
    Isha
}

public static class PrayerKindExtensions
{
    /// <summary>The five obligatory daily prayers, in order.</summary>
    public static readonly IReadOnlyList<PrayerKind> ObligatoryPrayers =
    [
        PrayerKind.Fajr,
        PrayerKind.Dhuhr,
        PrayerKind.Asr,
        PrayerKind.Maghrib,
        PrayerKind.Isha
    ];

    public static string MalayName(this PrayerKind kind) => kind switch
    {
        PrayerKind.Imsak => "Imsak",
        PrayerKind.Fajr => "Subuh",
        PrayerKind.Syuruk => "Syuruk",
        PrayerKind.Dhuha => "Dhuha",
        PrayerKind.Dhuhr => "Zohor",
        PrayerKind.Asr => "Asar",
        PrayerKind.Maghrib => "Maghrib",
        PrayerKind.Isha => "Isyak",
        _ => kind.ToString()
    };

    public static bool IsObligatory(this PrayerKind kind) =>
        kind is PrayerKind.Fajr or PrayerKind.Dhuhr or PrayerKind.Asr
            or PrayerKind.Maghrib or PrayerKind.Isha;
}

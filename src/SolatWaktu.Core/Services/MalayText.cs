using System.Globalization;
using SolatWaktu.Core.Models;

namespace SolatWaktu.Core.Services;

public static class MalayText
{
    private static readonly string[] Weekdays =
    [
        "Ahad", "Isnin", "Selasa", "Rabu", "Khamis", "Jumaat", "Sabtu"
    ];

    private static readonly string[] GregorianMonths =
    [
        "Januari", "Februari", "Mac", "April", "Mei", "Jun",
        "Julai", "Ogos", "September", "Oktober", "November", "Disember"
    ];

    private static readonly string[] HijriMonths =
    [
        "Muharram", "Safar", "Rabiulawal", "Rabiulakhir", "Jamadilawal",
        "Jamadilakhir", "Rejab", "Syaaban", "Ramadan", "Syawal",
        "Zulkaedah", "Zulhijjah"
    ];

    private static readonly string[] EnglishWeekdays =
    [
        "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
    ];

    private static readonly string[] EnglishGregorianMonths =
    [
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    ];

    private static readonly string[] EnglishHijriMonths =
    [
        "Muharram", "Safar", "Rabi' al-Awwal", "Rabi' al-Thani", "Jumada al-Awwal",
        "Jumada al-Thani", "Rajab", "Sha'ban", "Ramadan", "Shawwal",
        "Dhu al-Qi'dah", "Dhu al-Hijjah"
    ];

    public static string WeekdayName(DayOfWeek day) =>
        WeekdayName(day, AppLanguage.Malay);

    public static string WeekdayName(DayOfWeek day, AppLanguage language) =>
        (language == AppLanguage.English ? EnglishWeekdays : Weekdays)[(int)day];

    public static string GregorianMonthName(int month) =>
        GregorianMonthName(month, AppLanguage.Malay);

    public static string GregorianMonthName(int month, AppLanguage language) =>
        month is >= 1 and <= 12
            ? (language == AppLanguage.English ? EnglishGregorianMonths : GregorianMonths)[month - 1]
            : month.ToString(CultureInfo.InvariantCulture);

    public static string HijriMonthName(int month) =>
        HijriMonthName(month, AppLanguage.Malay);

    public static string HijriMonthName(int month, AppLanguage language) =>
        month is >= 1 and <= 12
            ? (language == AppLanguage.English ? EnglishHijriMonths : HijriMonths)[month - 1]
            : month.ToString(CultureInfo.InvariantCulture);

    /// <summary>Returns a long date in the selected language, for example
    /// "Ahad, 9 November 2026" or "Sunday, 9 November 2026".</summary>
    public static string FormatLongDate(DateOnly date) =>
        FormatLongDate(date, AppLanguage.Malay);

    public static string FormatLongDate(DateOnly date, AppLanguage language) =>
        $"{WeekdayName(date.DayOfWeek, language)}, {date.Day} {GregorianMonthName(date.Month, language)} {date.Year}";

    public static string FormatShortDate(DateOnly date) =>
        FormatShortDate(date, AppLanguage.Malay);

    public static string FormatShortDate(DateOnly date, AppLanguage language) =>
        $"{date.Day} {GregorianMonthName(date.Month, language)} {date.Year}";

    /// <summary>Converts 1448-05-29 to "29 Jamadilawal 1448" or
    /// "29 Jumada al-Awwal 1448" depending on the selected language.</summary>
    public static string FormatHijri(string hijri) =>
        FormatHijri(hijri, AppLanguage.Malay);

    public static string FormatHijri(string hijri, AppLanguage language)
    {
        if (string.IsNullOrWhiteSpace(hijri))
        {
            return string.Empty;
        }

        var parts = hijri.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 3
            && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var year)
            && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var month)
            && int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var day))
        {
            return $"{day} {HijriMonthName(month, language)} {year}";
        }

        return hijri;
    }

    public static string FormatTime(DateTime time, bool use24Hour)
    {
        if (time <= DateTime.MinValue)
        {
            return "--:--";
        }

        if (use24Hour)
        {
            return time.ToString("HH:mm", CultureInfo.InvariantCulture);
        }

        var suffix = time.Hour < 12 ? "AM" : "PM";
        return $"{time:h:mm} {suffix}";
    }

    /// <summary>Countdown text for the widget, for example "dalam 1j 42m 13s"
    /// or "in 1h 42m 13s".</summary>
    public static string FormatCountdown(TimeSpan remaining) =>
        FormatCountdown(remaining, AppLanguage.Malay);

    public static string FormatCountdown(TimeSpan remaining, AppLanguage language)
    {
        if (remaining <= TimeSpan.Zero)
        {
            return language == AppLanguage.English ? "now" : "sekarang";
        }

        return (language == AppLanguage.English ? "in " : "dalam ")
            + FormatCountdownShort(remaining, language);
    }

    /// <summary>Fixed-width countdown clock for the taskbar mini bar, for example
    /// "01:42:13". Negative values and zero render as "00:00:00".</summary>
    public static string FormatClockCountdown(TimeSpan remaining)
    {
        if (remaining <= TimeSpan.Zero)
        {
            return "00:00:00";
        }

        var totalSeconds = (long)remaining.TotalSeconds;
        var hours = totalSeconds / 3600;
        var minutes = totalSeconds % 3600 / 60;
        var seconds = totalSeconds % 60;

        return $"{hours:00}:{minutes:00}:{seconds:00}";
    }

    public static string FormatCountdownShort(TimeSpan remaining) =>
        FormatCountdownShort(remaining, AppLanguage.Malay);

    public static string FormatCountdownShort(TimeSpan remaining, AppLanguage language)
    {
        if (remaining < TimeSpan.Zero)
        {
            remaining = TimeSpan.Zero;
        }

        var totalSeconds = (long)remaining.TotalSeconds;
        var days = totalSeconds / 86400;
        var hours = totalSeconds % 86400 / 3600;
        var minutes = totalSeconds % 3600 / 60;
        var seconds = totalSeconds % 60;

        if (language == AppLanguage.English)
        {
            if (days > 0)
            {
                return hours > 0 ? $"{days}d {hours}h {minutes}m" : $"{days}d {minutes}m";
            }

            if (hours > 0)
            {
                return $"{hours}h {minutes}m {seconds}s";
            }

            if (minutes > 0)
            {
                return $"{minutes}m {seconds}s";
            }

            return $"{seconds}s";
        }

        if (days > 0)
        {
            return hours > 0 ? $"{days} hari {hours}j {minutes}m" : $"{days} hari {minutes}m";
        }

        if (hours > 0)
        {
            return $"{hours}j {minutes}m {seconds}s";
        }

        if (minutes > 0)
        {
            return $"{minutes}m {seconds}s";
        }

        return $"{seconds}s";
    }

    public static string FormatUpdated(DateTimeOffset fetchedAtUtc, DateTime nowMalaysia)
    {
        var local = fetchedAtUtc.ToOffset(MalaysiaTime.Offset);
        if (local.Date == nowMalaysia.Date)
        {
            return local.ToString("HH:mm", CultureInfo.InvariantCulture);
        }

        return local.ToString("d MMM", CultureInfo.InvariantCulture);
    }

    /// <summary>A compact, human-friendly zone title for the widget header.</summary>
    public static string ShortZoneName(Zone zone)
    {
        return zone.JakimCode.ToUpperInvariant() switch
        {
            "WLY01" => "Kuala Lumpur",
            "WLY02" => "Putrajaya",
            _ => ShortenFirstDistrict(zone.Daerah)
        };
    }

    private static string ShortenFirstDistrict(string daerah)
    {
        if (string.IsNullOrWhiteSpace(daerah))
        {
            return "Malaysia";
        }

        var first = daerah.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? daerah;

        return first.Length <= 28 ? first : first[..27].TrimEnd() + "...";
    }
}

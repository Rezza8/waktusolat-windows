namespace SolatWaktu.Core.Models;

/// <summary>
/// A month of prayer-time data for one zone.
/// </summary>
public sealed class PrayerMonth
{
    public string ZoneCode { get; init; } = string.Empty;
    public int Year { get; init; }
    public int Month { get; init; }

    /// <summary>The authoritative data source, normally JAKIM.</summary>
    public string Source { get; init; } = "JAKIM";

    /// <summary>The technical provider used to obtain the data.</summary>
    public string Provider { get; init; } = string.Empty;

    public DateTimeOffset FetchedAt { get; init; } = DateTimeOffset.UtcNow;
    public List<PrayerDay> Days { get; init; } = [];

    public PrayerDay? GetDay(DateOnly date) => Days.FirstOrDefault(day => day.Date == date);

    public bool Contains(DateOnly date) => GetDay(date) is not null;
}

/// <summary>
/// Result of a prayer-month request. FromCache is true when the network failed
/// and the app had to use older cached data.
/// </summary>
public sealed record PrayerMonthResult(PrayerMonth Month, bool FromCache, string? Warning = null);

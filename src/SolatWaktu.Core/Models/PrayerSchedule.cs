namespace SolatWaktu.Core.Models;

public sealed record PrayerOccurrence(PrayerKind Kind, DateTime Time, bool IsTomorrow)
{
    public string Name => Kind.MalayName();
}

/// <summary>
/// The calculated state of the widget at a point in time.
/// </summary>
public sealed class PrayerScheduleResult
{
    public required PrayerDay Today { get; init; }
    public PrayerDay? Yesterday { get; init; }
    public PrayerDay? Tomorrow { get; init; }

    public PrayerOccurrence? Next { get; init; }
    public PrayerOccurrence? Previous { get; init; }

    public TimeSpan Remaining { get; init; }
    public double Progress { get; init; }

    public IReadOnlyList<PrayerOccurrence> TodayPrayers { get; init; } = [];
}

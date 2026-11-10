using SolatWaktu.Core.Models;

namespace SolatWaktu.Core.Services;

/// <summary>
/// Calculates the next prayer, the previous prayer, and progress between them.
/// </summary>
public static class PrayerScheduleCalculator
{
    public static PrayerScheduleResult Calculate(
        PrayerDay today,
        PrayerDay? yesterday,
        PrayerDay? tomorrow,
        DateTime now)
    {
        ArgumentNullException.ThrowIfNull(today);

        var todayPrayers = PrayerKindExtensions.ObligatoryPrayers
            .Select(kind => new PrayerOccurrence(kind, today.GetTime(kind), IsTomorrow: false))
            .Where(occurrence => occurrence.Time > DateTime.MinValue)
            .OrderBy(occurrence => occurrence.Time)
            .ToArray();

        PrayerOccurrence? previous = todayPrayers
            .LastOrDefault(occurrence => occurrence.Time <= now);

        if (previous is null && yesterday is not null && yesterday.Isha > DateTime.MinValue)
        {
            previous = new PrayerOccurrence(PrayerKind.Isha, yesterday.Isha, IsTomorrow: false);
        }

        PrayerOccurrence? next = todayPrayers
            .FirstOrDefault(occurrence => occurrence.Time > now);

        if (next is null && tomorrow is not null && tomorrow.Fajr > DateTime.MinValue)
        {
            next = new PrayerOccurrence(PrayerKind.Fajr, tomorrow.Fajr, IsTomorrow: true);
        }

        var remaining = TimeSpan.Zero;
        var progress = 0d;

        if (next is not null)
        {
            remaining = next.Time - now;
            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }

            if (previous is not null)
            {
                var total = (next.Time - previous.Time).TotalSeconds;
                if (total > 0)
                {
                    var elapsed = (now - previous.Time).TotalSeconds;
                    progress = Math.Clamp(elapsed / total, 0d, 1d);
                }
            }
        }

        return new PrayerScheduleResult
        {
            Today = today,
            Yesterday = yesterday,
            Tomorrow = tomorrow,
            Next = next,
            Previous = previous,
            Remaining = remaining,
            Progress = progress,
            TodayPrayers = todayPrayers
        };
    }
}

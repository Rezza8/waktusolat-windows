using SolatWaktu.Core.Models;

namespace SolatWaktu.Core.Providers;

public interface IPrayerTimeProvider
{
    /// <summary>Technical provider name, for diagnostics.</summary>
    string Name { get; }

    /// <summary>True when this provider can serve the requested month.</summary>
    bool Supports(int year, int month);

    Task<PrayerMonth> FetchMonthAsync(string zoneCode, int year, int month, CancellationToken cancellationToken = default);
}

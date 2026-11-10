namespace SolatWaktu.Core.Services;

/// <summary>
/// Malaysia is UTC+8 all year round, so a fixed offset keeps calculations
/// independent of the PC's regional settings.
/// </summary>
public static class MalaysiaTime
{
    public static TimeSpan Offset { get; } = TimeSpan.FromHours(8);

    public static DateTime Now => DateTimeOffset.UtcNow.ToOffset(Offset).DateTime;

    public static DateTimeOffset NowOffset => DateTimeOffset.UtcNow.ToOffset(Offset);

    public static DateTime FromUnixSeconds(long seconds) =>
        DateTimeOffset.FromUnixTimeSeconds(seconds).ToOffset(Offset).DateTime;

    public static DateTime ToMalaysia(DateTimeOffset value) => value.ToOffset(Offset).DateTime;
}

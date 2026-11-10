using SolatWaktu.Core.Models;
using SolatWaktu.Core.Services;

namespace SolatWaktu.Core.Tests;

public sealed class PrayerCacheTests
{
    private static PrayerMonth Month(string zone = "WLY01", int year = 2026, int month = 11) => new()
    {
        ZoneCode = zone,
        Year = year,
        Month = month,
        FetchedAt = DateTimeOffset.UtcNow,
        Days =
        [
            new PrayerDay
            {
                Date = new DateOnly(year, month, 9),
                Hijri = "1448-05-29",
                Fajr = new DateTime(year, month, 9, 5, 48, 0),
                Dhuhr = new DateTime(year, month, 9, 13, 0, 0),
                Asr = new DateTime(year, month, 9, 16, 21, 0),
                Maghrib = new DateTime(year, month, 9, 18, 58, 0),
                Isha = new DateTime(year, month, 9, 20, 10, 0)
            }
        ]
    };

    [Fact]
    public void WriteAndRead_RoundTripsMonth()
    {
        using var temp = new TemporaryDirectory();
        var cache = new PrayerCache(temp.Path);

        cache.Write(Month());
        var result = cache.TryRead("WLY01", 2026, 11);

        Assert.NotNull(result);
        Assert.Equal("1448-05-29", result.Days[0].Hijri);
        Assert.Equal(new DateTime(2026, 11, 9, 5, 48, 0), result.Days[0].Fajr);
    }

    [Fact]
    public void TryRead_ReturnsNullForDifferentZone()
    {
        using var temp = new TemporaryDirectory();
        var cache = new PrayerCache(temp.Path);
        cache.Write(Month());

        Assert.Null(cache.TryRead("JHR01", 2026, 11));
    }

    [Fact]
    public void TryRead_ReturnsNullForCorruptFile()
    {
        using var temp = new TemporaryDirectory();
        var cache = new PrayerCache(temp.Path);
        Directory.CreateDirectory(temp.Path);
        File.WriteAllText(Path.Combine(temp.Path, "WLY01-2026-11.json"), "{ not json");

        Assert.Null(cache.TryRead("WLY01", 2026, 11));
    }

    [Fact]
    public void Clear_RemovesCachedFiles()
    {
        using var temp = new TemporaryDirectory();
        var cache = new PrayerCache(temp.Path);
        cache.Write(Month());

        cache.Clear();

        Assert.Empty(Directory.EnumerateFiles(temp.Path, "*.json"));
    }
}

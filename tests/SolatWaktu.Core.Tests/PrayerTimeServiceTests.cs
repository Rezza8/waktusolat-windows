using System.Net.Http;
using SolatWaktu.Core.Models;
using SolatWaktu.Core.Services;

namespace SolatWaktu.Core.Tests;

public sealed class PrayerTimeServiceTests
{
    private static PrayerMonth CachedMonth(DateTimeOffset fetchedAt) => new()
    {
        ZoneCode = "WLY01",
        Year = 2026,
        Month = 11,
        Source = "JAKIM",
        Provider = "test",
        FetchedAt = fetchedAt,
        Days =
        [
            new PrayerDay
            {
                Date = new DateOnly(2026, 11, 9),
                Hijri = "1448-05-29",
                Imsak = new DateTime(2026, 11, 9, 5, 38, 0),
                Fajr = new DateTime(2026, 11, 9, 5, 48, 0),
                Syuruk = new DateTime(2026, 11, 9, 6, 57, 0),
                Dhuha = new DateTime(2026, 11, 9, 7, 22, 0),
                Dhuhr = new DateTime(2026, 11, 9, 13, 0, 0),
                Asr = new DateTime(2026, 11, 9, 16, 21, 0),
                Maghrib = new DateTime(2026, 11, 9, 18, 58, 0),
                Isha = new DateTime(2026, 11, 9, 20, 10, 0)
            }
        ]
    };

    [Fact]
    public async Task FreshDiskCache_IsReturnedWithoutNetwork()
    {
        using var temp = new TemporaryDirectory();
        var cache = new PrayerCache(temp.Path);
        cache.Write(CachedMonth(DateTimeOffset.UtcNow));

        using var http = new HttpClient(new StubHttpMessageHandler(_ =>
            throw new HttpRequestException("network disabled for test")));
        using var service = new PrayerTimeService(http, temp.Path);

        var result = await service.GetMonthAsync("WLY01", 2026, 11);

        Assert.False(result.FromCache);
        Assert.Equal(2026, result.Month.Year);
    }

    [Fact]
    public async Task StaleCache_IsUsedWhenNetworkFails()
    {
        using var temp = new TemporaryDirectory();
        var cache = new PrayerCache(temp.Path);
        cache.Write(CachedMonth(DateTimeOffset.UtcNow.AddDays(-2)));

        using var http = new HttpClient(new StubHttpMessageHandler(_ =>
            throw new HttpRequestException("network disabled for test")));
        using var service = new PrayerTimeService(http, temp.Path);

        var result = await service.GetMonthAsync("WLY01", 2026, 11);

        Assert.True(result.FromCache);
        Assert.NotNull(result.Warning);
        Assert.Single(result.Month.Days);
    }

    [Fact]
    public async Task NoCacheAndNetworkFailure_ThrowsFriendlyException()
    {
        using var temp = new TemporaryDirectory();

        using var http = new HttpClient(new StubHttpMessageHandler(_ =>
            throw new HttpRequestException("network disabled for test")));
        using var service = new PrayerTimeService(http, temp.Path);

        var exception = await Assert.ThrowsAsync<PrayerDataException>(
            () => service.GetMonthAsync("WLY01", 2026, 11));

        Assert.Contains("waktu solat", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}

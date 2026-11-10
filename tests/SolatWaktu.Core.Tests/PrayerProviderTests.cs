using System.Net.Http;
using SolatWaktu.Core.Providers;

namespace SolatWaktu.Core.Tests;

public sealed class PrayerProviderTests
{
    private static long Epoch(int day, int hour, int minute) =>
        new DateTimeOffset(2026, 11, day, hour, minute, 0, TimeSpan.FromHours(8)).ToUnixTimeSeconds();

    [Fact]
    public async Task WaktusolatProvider_ParsesEpochTimesIntoMalaysiaTime()
    {
        var json = $$"""
        {
          "zone": "WLY01",
          "year": 2026,
          "month": "NOV",
          "month_number": 11,
          "last_updated": null,
          "prayers": [
            {
              "day": 9,
              "hijri": "1448-05-29",
              "imsak": {{Epoch(9, 5, 38)}},
              "fajr": {{Epoch(9, 5, 48)}},
              "syuruk": {{Epoch(9, 6, 57)}},
              "dhuha": {{Epoch(9, 7, 22)}},
              "dhuhr": {{Epoch(9, 13, 0)}},
              "asr": {{Epoch(9, 16, 21)}},
              "maghrib": {{Epoch(9, 18, 58)}},
              "isha": {{Epoch(9, 20, 10)}}
            }
          ]
        }
        """;

        using var http = new HttpClient(StubHttpMessageHandler.Json(json));
        var provider = new WaktusolatProvider(http);

        var month = await provider.FetchMonthAsync("WLY01", 2026, 11);

        var day = Assert.Single(month.Days);
        Assert.Equal("JAKIM", month.Source);
        Assert.Equal("waktusolat.app", month.Provider);
        Assert.Equal(new DateTime(2026, 11, 9, 5, 48, 0), day.Fajr);
        Assert.Equal(new DateTime(2026, 11, 9, 18, 58, 0), day.Maghrib);
        Assert.Equal("1448-05-29", day.Hijri);
    }

    [Fact]
    public async Task JakimProvider_ParsesTimeStrings()
    {
        const string json = """
        {
          "prayerTime": [
            {
              "hijri": "1448-05-29",
              "date": "09-Nov-2026",
              "day": "Monday",
              "imsak": "05:38:00",
              "fajr": "05:48:00",
              "syuruk": "06:57:00",
              "dhuha": "07:22:00",
              "dhuhr": "13:00:00",
              "asr": "16:21:00",
              "maghrib": "18:58:00",
              "isha": "20:10:00"
            }
          ],
          "status": "OK!",
          "periodType": "month",
          "zone": "WLY01"
        }
        """;

        using var http = new HttpClient(StubHttpMessageHandler.Json(json));
        var provider = new JakimProvider(http);

        var month = await provider.FetchMonthAsync("WLY01", 2026, 11);

        var day = Assert.Single(month.Days);
        Assert.Equal("e-Solat JAKIM", month.Provider);
        Assert.Equal(new DateTime(2026, 11, 9, 5, 48, 0), day.Fajr);
        Assert.Equal(new DateTime(2026, 11, 9, 20, 10, 0), day.Isha);
    }
}

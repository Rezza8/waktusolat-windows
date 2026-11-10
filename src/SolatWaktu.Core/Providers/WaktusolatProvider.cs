using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using SolatWaktu.Core.Models;
using SolatWaktu.Core.Services;

namespace SolatWaktu.Core.Providers;

/// <summary>
/// Structured monthly data from api.waktusolat.app, which mirrors JAKIM's
/// official e-Solat timetable. Supports any month, including next month.
/// </summary>
public sealed class WaktusolatProvider : IPrayerTimeProvider
{
    private static readonly Uri BaseUri = new("https://api.waktusolat.app/");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public WaktusolatProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string Name => "waktusolat.app";

    public bool Supports(int year, int month) => true;

    public async Task<PrayerMonth> FetchMonthAsync(
        string zoneCode,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var url = new Uri(
            BaseUri,
            $"v2/solat/{Uri.EscapeDataString(zoneCode)}?year={year}&month={month}");

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new PrayerDataException(
                $"waktusolat.app returned HTTP {(int)response.StatusCode} ({response.StatusCode}).");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var dto = JsonSerializer.Deserialize<ResponseDto>(json, JsonOptions)
            ?? throw new PrayerDataException("waktusolat.app returned an empty response.");

        if (dto.Prayers is null || dto.Prayers.Count == 0)
        {
            throw new PrayerDataException("waktusolat.app returned no prayer entries.");
        }

        if (dto.MonthNumber is > 0 and <= 12 && dto.MonthNumber != month)
        {
            throw new PrayerDataException(
                $"waktusolat.app returned month {dto.MonthNumber}, expected {month}.");
        }

        var days = new List<PrayerDay>(dto.Prayers.Count);
        foreach (var prayer in dto.Prayers)
        {
            if (prayer.Day is < 1 or > 31)
            {
                continue;
            }

            DateOnly date;
            try
            {
                date = new DateOnly(year, month, prayer.Day);
            }
            catch (ArgumentOutOfRangeException)
            {
                continue;
            }

            var day = new PrayerDay
            {
                Date = date,
                Hijri = prayer.Hijri ?? string.Empty,
                Imsak = FromEpoch(prayer.Imsak),
                Fajr = FromEpoch(prayer.Fajr),
                Syuruk = FromEpoch(prayer.Syuruk),
                Dhuha = FromEpoch(prayer.Dhuha),
                Dhuhr = FromEpoch(prayer.Dhuhr),
                Asr = FromEpoch(prayer.Asr),
                Maghrib = FromEpoch(prayer.Maghrib),
                Isha = FromEpoch(prayer.Isha)
            };

            if (day.Fajr <= DateTime.MinValue
                || day.Dhuhr <= DateTime.MinValue
                || day.Asr <= DateTime.MinValue
                || day.Maghrib <= DateTime.MinValue
                || day.Isha <= DateTime.MinValue)
            {
                continue;
            }

            days.Add(day);
        }

        if (days.Count == 0)
        {
            throw new PrayerDataException("waktusolat.app returned no valid prayer days.");
        }

        return new PrayerMonth
        {
            ZoneCode = zoneCode.ToUpperInvariant(),
            Year = year,
            Month = month,
            Source = "JAKIM",
            Provider = Name,
            FetchedAt = DateTimeOffset.UtcNow,
            Days = days.OrderBy(day => day.Date).ToList()
        };
    }

    private static DateTime FromEpoch(long seconds) =>
        seconds <= 0 ? DateTime.MinValue : MalaysiaTime.FromUnixSeconds(seconds);

    private sealed class ResponseDto
    {
        [JsonPropertyName("zone")]
        public string? Zone { get; set; }

        [JsonPropertyName("year")]
        public int Year { get; set; }

        [JsonPropertyName("month_number")]
        public int MonthNumber { get; set; }

        [JsonPropertyName("prayers")]
        public List<PrayerDto>? Prayers { get; set; }
    }

    private sealed class PrayerDto
    {
        [JsonPropertyName("day")]
        public int Day { get; set; }

        [JsonPropertyName("hijri")]
        public string? Hijri { get; set; }

        [JsonPropertyName("imsak")]
        public long Imsak { get; set; }

        [JsonPropertyName("fajr")]
        public long Fajr { get; set; }

        [JsonPropertyName("syuruk")]
        public long Syuruk { get; set; }

        [JsonPropertyName("dhuha")]
        public long Dhuha { get; set; }

        [JsonPropertyName("dhuhr")]
        public long Dhuhr { get; set; }

        [JsonPropertyName("asr")]
        public long Asr { get; set; }

        [JsonPropertyName("maghrib")]
        public long Maghrib { get; set; }

        [JsonPropertyName("isha")]
        public long Isha { get; set; }
    }
}

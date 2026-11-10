using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using SolatWaktu.Core.Models;
using SolatWaktu.Core.Services;

namespace SolatWaktu.Core.Providers;

/// <summary>
/// Official JAKIM e-Solat monthly timetable. Its public endpoint currently
/// serves the current month, so it is used as the first choice for this month
/// and skipped for future months.
/// </summary>
public sealed class JakimProvider : IPrayerTimeProvider
{
    private const string Endpoint = "https://www.e-solat.gov.my/index.php";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public JakimProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string Name => "e-Solat JAKIM";

    public bool Supports(int year, int month)
    {
        var now = MalaysiaTime.Now;
        return now.Year == year && now.Month == month;
    }

    public async Task<PrayerMonth> FetchMonthAsync(
        string zoneCode,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var url = $"{Endpoint}?r=esolatApi/takwimsolat&period=month&zone={Uri.EscapeDataString(zoneCode)}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new PrayerDataException(
                $"e-Solat JAKIM returned HTTP {(int)response.StatusCode} ({response.StatusCode}).");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var dto = JsonSerializer.Deserialize<ResponseDto>(json, JsonOptions)
            ?? throw new PrayerDataException("e-Solat JAKIM returned an empty response.");

        if (dto.PrayerTime is null || dto.PrayerTime.Count == 0)
        {
            throw new PrayerDataException("e-Solat JAKIM returned no prayer entries.");
        }

        if (!string.IsNullOrWhiteSpace(dto.Status)
            && !dto.Status.Contains("OK", StringComparison.OrdinalIgnoreCase))
        {
            throw new PrayerDataException($"e-Solat JAKIM returned status '{dto.Status}'.");
        }

        var days = new List<PrayerDay>(dto.PrayerTime.Count);
        foreach (var entry in dto.PrayerTime)
        {
            if (!DateTime.TryParseExact(
                    entry.Date,
                    "dd-MMM-yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedDate))
            {
                continue;
            }

            var date = DateOnly.FromDateTime(parsedDate);
            if (date.Year != year || date.Month != month)
            {
                continue;
            }

            var day = new PrayerDay
            {
                Date = date,
                Hijri = entry.Hijri ?? string.Empty,
                Imsak = Combine(date, entry.Imsak),
                Fajr = Combine(date, entry.Fajr),
                Syuruk = Combine(date, entry.Syuruk),
                Dhuha = Combine(date, entry.Dhuha),
                Dhuhr = Combine(date, entry.Dhuhr),
                Asr = Combine(date, entry.Asr),
                Maghrib = Combine(date, entry.Maghrib),
                Isha = Combine(date, entry.Isha)
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
            throw new PrayerDataException("e-Solat JAKIM returned no valid prayer days for this month.");
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

    private static DateTime Combine(DateOnly date, string? text)
    {
        if (string.IsNullOrWhiteSpace(text)
            || !TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out var time))
        {
            return DateTime.MinValue;
        }

        var combined = date.ToDateTime(TimeOnly.MinValue).Add(time);
        return DateTime.SpecifyKind(combined, DateTimeKind.Unspecified);
    }

    private sealed class ResponseDto
    {
        [JsonPropertyName("prayerTime")]
        public List<PrayerTimeDto>? PrayerTime { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("zone")]
        public string? Zone { get; set; }
    }

    private sealed class PrayerTimeDto
    {
        [JsonPropertyName("hijri")]
        public string? Hijri { get; set; }

        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [JsonPropertyName("imsak")]
        public string? Imsak { get; set; }

        [JsonPropertyName("fajr")]
        public string? Fajr { get; set; }

        [JsonPropertyName("syuruk")]
        public string? Syuruk { get; set; }

        [JsonPropertyName("dhuha")]
        public string? Dhuha { get; set; }

        [JsonPropertyName("dhuhr")]
        public string? Dhuhr { get; set; }

        [JsonPropertyName("asr")]
        public string? Asr { get; set; }

        [JsonPropertyName("maghrib")]
        public string? Maghrib { get; set; }

        [JsonPropertyName("isha")]
        public string? Isha { get; set; }
    }
}

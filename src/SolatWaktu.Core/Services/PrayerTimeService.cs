using System.Diagnostics;
using System.Net.Http.Headers;
using SolatWaktu.Core.Models;
using SolatWaktu.Core.Providers;

namespace SolatWaktu.Core.Services;

/// <summary>
/// Orchestrates online providers, the disk cache, and in-memory reuse.
/// </summary>
public sealed class PrayerTimeService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly PrayerCache _cache;
    private readonly IPrayerTimeProvider[] _providers;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _memoryLock = new();
    private readonly Dictionary<string, PrayerMonthResult> _memory = new(StringComparer.OrdinalIgnoreCase);

    public PrayerTimeService(HttpClient? httpClient = null, string? cacheDirectory = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _ownsHttpClient = httpClient is null;

        if (_httpClient.Timeout == Timeout.InfiniteTimeSpan || _httpClient.Timeout > TimeSpan.FromSeconds(30))
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(20);
        }

        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SolatWaktuWidget", "1.0"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        _cache = new PrayerCache(cacheDirectory);
        _providers =
        [
            new JakimProvider(_httpClient),
            new WaktusolatProvider(_httpClient)
        ];
    }

    public string CacheDirectory => _cache.CacheDirectory;

    public async Task<PrayerMonthResult> GetMonthAsync(
        string zoneCode,
        int year,
        int month,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(zoneCode))
        {
            throw new ArgumentException("Zone code is required.", nameof(zoneCode));
        }

        var key = BuildKey(zoneCode, year, month);
        var diskCache = _cache.TryRead(zoneCode, year, month);

        if (!forceRefresh)
        {
            if (TryGetMemory(key, out var memoryResult)
                && IsFresh(memoryResult.Month, year, month, DateTimeOffset.UtcNow))
            {
                return memoryResult;
            }

            if (diskCache is not null
                && IsFresh(diskCache, year, month, DateTimeOffset.UtcNow))
            {
                var result = new PrayerMonthResult(diskCache, FromCache: false);
                Remember(key, result);
                return result;
            }
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!forceRefresh && TryGetMemory(key, out var memoryResult)
                && IsFresh(memoryResult.Month, year, month, DateTimeOffset.UtcNow))
            {
                return memoryResult;
            }

            var failures = new List<Exception>();

            foreach (var provider in _providers)
            {
                if (!provider.Supports(year, month))
                {
                    continue;
                }

                try
                {
                    var monthData = await provider
                        .FetchMonthAsync(zoneCode, year, month, cancellationToken)
                        .ConfigureAwait(false);

                    try
                    {
                        _cache.Write(monthData);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        Debug.WriteLine($"Could not write prayer cache: {ex.Message}");
                    }

                    var result = new PrayerMonthResult(monthData, FromCache: false);
                    Remember(key, result);
                    return result;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                    Debug.WriteLine($"Prayer provider '{provider.Name}' failed: {ex.Message}");
                }
            }

            if (diskCache is not null)
            {
                return new PrayerMonthResult(
                    diskCache,
                    FromCache: true,
                    Warning: "Data cache digunakan kerana sumber dalam talian tidak dapat dihubungi.");
            }

            var detail = failures.Count == 0
                ? "Tiada penyedia data yang tersedia untuk bulan ini."
                : string.Join(" | ", failures.Select(error => error.Message));

            throw new PrayerDataException(
                "Tidak dapat memuatkan waktu solat. Sila semak sambungan internet anda.",
                new AggregateException(detail, failures));
        }
        finally
        {
            _gate.Release();
        }
    }

    public void ClearCache()
    {
        _cache.Clear();
        lock (_memoryLock)
        {
            _memory.Clear();
        }
    }

    public void Dispose()
    {
        _gate.Dispose();
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private static string BuildKey(string zoneCode, int year, int month) =>
        $"{zoneCode.ToUpperInvariant()}:{year:D4}:{month:D2}";

    private static bool IsFresh(PrayerMonth month, int year, int monthNumber, DateTimeOffset nowUtc)
    {
        var nowMalaysia = MalaysiaTime.Now;
        var isCurrentMonth = nowMalaysia.Year == year && nowMalaysia.Month == monthNumber;
        var maxAge = isCurrentMonth ? TimeSpan.FromHours(6) : TimeSpan.FromDays(7);
        var age = nowUtc - month.FetchedAt;

        return age >= TimeSpan.Zero && age <= maxAge;
    }

    private bool TryGetMemory(string key, out PrayerMonthResult result)
    {
        lock (_memoryLock)
        {
            return _memory.TryGetValue(key, out result!);
        }
    }

    private void Remember(string key, PrayerMonthResult result)
    {
        lock (_memoryLock)
        {
            _memory[key] = result;
        }
    }
}

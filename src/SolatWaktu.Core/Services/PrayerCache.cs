using System.Text.Json;
using System.Text.Json.Serialization;
using SolatWaktu.Core.Models;

namespace SolatWaktu.Core.Services;

/// <summary>
/// Disk cache of monthly prayer data. Files live under
/// %LocalAppData%\SolatWaktuWidget\cache.
/// </summary>
public sealed class PrayerCache
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public PrayerCache(string? cacheDirectory = null)
    {
        CacheDirectory = cacheDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SolatWaktuWidget",
            "cache");
    }

    public string CacheDirectory { get; }

    public PrayerMonth? TryRead(string zoneCode, int year, int month)
    {
        var path = GetPath(zoneCode, year, month);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(path);
            var cached = JsonSerializer.Deserialize<PrayerMonth>(json, JsonOptions);
            if (cached is null
                || !string.Equals(cached.ZoneCode, zoneCode, StringComparison.OrdinalIgnoreCase)
                || cached.Year != year
                || cached.Month != month
                || cached.Days.Count == 0)
            {
                return null;
            }

            return cached;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void Write(PrayerMonth month)
    {
        ArgumentNullException.ThrowIfNull(month);
        Directory.CreateDirectory(CacheDirectory);

        var path = GetPath(month.ZoneCode, month.Year, month.Month);
        var tempPath = path + ".tmp";
        var json = JsonSerializer.Serialize(month, JsonOptions);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, path, overwrite: true);
    }

    public void Clear()
    {
        if (!Directory.Exists(CacheDirectory))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(CacheDirectory, "*.json"))
        {
            try
            {
                File.Delete(file);
            }
            catch (IOException)
            {
                // A locked cache file is harmless; it will be replaced later.
            }
            catch (UnauthorizedAccessException)
            {
                // Same as above.
            }
        }
    }

    private string GetPath(string zoneCode, int year, int month)
    {
        var safeZone = new string(zoneCode
            .Where(char.IsLetterOrDigit)
            .ToArray())
            .ToUpperInvariant();

        if (safeZone.Length == 0)
        {
            safeZone = "UNKNOWN";
        }

        return Path.Combine(CacheDirectory, $"{safeZone}-{year:D4}-{month:D2}.json");
    }
}

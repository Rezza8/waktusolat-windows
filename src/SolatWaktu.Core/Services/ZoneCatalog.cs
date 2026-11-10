using System.Reflection;
using System.Text.Json;
using SolatWaktu.Core.Models;

namespace SolatWaktu.Core.Services;

/// <summary>
/// The complete list of JAKIM zones, embedded in the app so the picker works offline.
/// </summary>
public sealed class ZoneCatalog
{
    private const string ResourceName = "SolatWaktu.Core.Data.zones.json";

    private readonly IReadOnlyList<Zone> _zones;

    public static ZoneCatalog Default { get; } = new(LoadZones());

    public ZoneCatalog(IEnumerable<Zone> zones)
    {
        _zones = zones
            .Where(zone => !string.IsNullOrWhiteSpace(zone.JakimCode))
            .OrderBy(zone => zone.Negeri, StringComparer.OrdinalIgnoreCase)
            .ThenBy(zone => zone.Daerah, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<Zone> Zones => _zones;

    public IReadOnlyList<string> States =>
        _zones.Select(zone => zone.Negeri).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    public Zone? Find(string? code) =>
        string.IsNullOrWhiteSpace(code)
            ? null
            : _zones.FirstOrDefault(zone =>
                string.Equals(zone.JakimCode, code.Trim(), StringComparison.OrdinalIgnoreCase));

    public IEnumerable<Zone> Search(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return _zones;
        }

        var needle = query.Trim();
        return _zones.Where(zone =>
            zone.JakimCode.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || zone.Negeri.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || zone.Daerah.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }

    private static List<Zone> LoadZones()
    {
        var assembly = typeof(ZoneCatalog).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded zone catalogue '{ResourceName}' was not found.");

        var zones = JsonSerializer.Deserialize<List<Zone>>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (zones is null || zones.Count == 0)
        {
            throw new InvalidOperationException("The embedded zone catalogue is empty.");
        }

        return zones;
    }
}

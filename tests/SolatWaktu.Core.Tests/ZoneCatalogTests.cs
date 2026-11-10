using SolatWaktu.Core.Services;

namespace SolatWaktu.Core.Tests;

public sealed class ZoneCatalogTests
{
    [Fact]
    public void EmbeddedCatalogue_ContainsAllSixtyZones()
    {
        Assert.Equal(60, ZoneCatalog.Default.Zones.Count);
        Assert.Equal(14, ZoneCatalog.Default.States.Count);
    }

    [Fact]
    public void Find_ReturnsZoneIgnoringCase()
    {
        var zone = ZoneCatalog.Default.Find("wly01");
        Assert.NotNull(zone);
        Assert.Equal("Wilayah Persekutuan", zone.Negeri);
    }

    [Fact]
    public void Search_MatchesStateAndDistrict()
    {
        var selangor = ZoneCatalog.Default.Search("Selangor").ToArray();
        Assert.NotEmpty(selangor);
        Assert.All(selangor, zone => Assert.Equal("Selangor", zone.Negeri));

        var johorBahru = ZoneCatalog.Default.Search("Johor Bahru").ToArray();
        Assert.Contains(johorBahru, zone => zone.JakimCode == "JHR02");
    }
}

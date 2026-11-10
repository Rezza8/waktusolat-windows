using SolatWaktu.Core.Models;
using SolatWaktu.Core.Services;

namespace SolatWaktu.Core.Tests;

public sealed class SettingsServiceTests
{
    [Fact]
    public void Load_ReturnsDefaultsWhenFileMissing()
    {
        using var temp = new TemporaryDirectory();
        var service = new SettingsService(Path.Combine(temp.Path, "settings.json"));

        var settings = service.Load();

        Assert.Equal("WLY01", settings.ZoneCode);
        Assert.Equal("Midnight", settings.Theme);
        Assert.Equal(AppLanguage.Malay, settings.Language);
        Assert.True(settings.NotificationsEnabled);
        Assert.False(settings.AzanEnabled);
        Assert.Equal(WidgetSizePreset.Sederhana, settings.WidgetSize);
        Assert.Equal(TaskbarNextAlignment.Off, settings.TaskbarNext);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsValues()
    {
        using var temp = new TemporaryDirectory();
        var service = new SettingsService(Path.Combine(temp.Path, "settings.json"));
        var settings = new AppSettings
        {
            ZoneCode = "JHR02",
            Theme = "Ocean",
            Language = AppLanguage.English,
            Use24HourFormat = false,
            AlwaysOnTop = false,
            AzanEnabled = true,
            AzanPrayers = [PrayerKind.Fajr, PrayerKind.Maghrib],
            WidgetSize = WidgetSizePreset.Besar,
            TaskbarNext = TaskbarNextAlignment.Right,
            WindowX = 100,
            WindowY = 200
        };

        service.Save(settings);
        var loaded = service.Load();

        Assert.Equal("JHR02", loaded.ZoneCode);
        Assert.Equal("Ocean", loaded.Theme);
        Assert.Equal(AppLanguage.English, loaded.Language);
        Assert.False(loaded.Use24HourFormat);
        Assert.False(loaded.AlwaysOnTop);
        Assert.True(loaded.AzanEnabled);
        Assert.Equal([PrayerKind.Fajr, PrayerKind.Maghrib], loaded.AzanPrayers.OrderBy(kind => kind));
        Assert.Equal(WidgetSizePreset.Besar, loaded.WidgetSize);
        Assert.Equal(TaskbarNextAlignment.Right, loaded.TaskbarNext);
        Assert.Equal(100, loaded.WindowX);
        Assert.Equal(200, loaded.WindowY);
    }

    [Fact]
    public void Load_ReturnsDefaultsForCorruptFile()
    {
        using var temp = new TemporaryDirectory();
        var path = Path.Combine(temp.Path, "settings.json");
        File.WriteAllText(path, "{ broken");
        var service = new SettingsService(path);

        var settings = service.Load();

        Assert.Equal("WLY01", settings.ZoneCode);
        Assert.NotNull(service.LastLoadError);
    }

    [Fact]
    public void Normalize_ClampsOpacityAndVolume()
    {
        var settings = new AppSettings
        {
            OpacityPercent = 20,
            AzanVolume = 150,
            Theme = "",
            ZoneCode = ""
        };

        SettingsService.Normalize(settings);

        Assert.Equal(70, settings.OpacityPercent);
        Assert.Equal(100, settings.AzanVolume);
        Assert.Equal("Midnight", settings.Theme);
        Assert.Equal("WLY01", settings.ZoneCode);
    }

    [Fact]
    public void Normalize_ResetsInvalidTaskbarPosition()
    {
        var settings = new AppSettings
        {
            TaskbarNext = (TaskbarNextAlignment)99
        };

        SettingsService.Normalize(settings);

        Assert.Equal(TaskbarNextAlignment.Off, settings.TaskbarNext);
    }

    [Fact]
    public void Normalize_ResetsInvalidLanguage()
    {
        var settings = new AppSettings
        {
            Language = (AppLanguage)99
        };

        SettingsService.Normalize(settings);

        Assert.Equal(AppLanguage.Malay, settings.Language);
    }

    [Fact]
    public void Load_LegacyFileWithoutLanguageDefaultsToMalay()
    {
        using var temp = new TemporaryDirectory();
        var path = Path.Combine(temp.Path, "settings.json");
        File.WriteAllText(
            path,
            """
            {
              "ZoneCode": "SBH07",
              "Theme": "Pelangi"
            }
            """);
        var service = new SettingsService(path);

        var settings = service.Load();

        Assert.Equal("SBH07", settings.ZoneCode);
        Assert.Equal("Pelangi", settings.Theme);
        Assert.Equal(AppLanguage.Malay, settings.Language);
    }

    [Theory]
    [InlineData("English", AppLanguage.English)]
    [InlineData("english", AppLanguage.English)]
    [InlineData("EN", AppLanguage.English)]
    [InlineData("Malay", AppLanguage.Malay)]
    [InlineData("Klingon", AppLanguage.Malay)]
    public void Load_ReadsLanguageTolerantly(string storedValue, AppLanguage expected)
    {
        using var temp = new TemporaryDirectory();
        var path = Path.Combine(temp.Path, "settings.json");
        File.WriteAllText(path, $$"""{ "Language": "{{storedValue}}" }""");
        var service = new SettingsService(path);

        var settings = service.Load();

        Assert.Equal(expected, settings.Language);
    }
}

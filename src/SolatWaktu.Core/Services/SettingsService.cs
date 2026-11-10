using System.Text.Json;
using System.Text.Json.Serialization;
using SolatWaktu.Core.Models;

namespace SolatWaktu.Core.Services;

/// <summary>
/// Loads and saves user settings under %LocalAppData%\SolatWaktuWidget.
/// </summary>
public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new AppLanguageJsonConverter(),
            new JsonStringEnumConverter()
        }
    };

    public SettingsService(string? settingsPath = null)
    {
        SettingsPath = settingsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SolatWaktuWidget",
            "settings.json");
    }

    public string SettingsPath { get; }

    public bool Exists => File.Exists(SettingsPath);

    public string? LastLoadError { get; private set; }

    public AppSettings Load()
    {
        LastLoadError = null;

        if (!File.Exists(SettingsPath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (settings is null)
            {
                LastLoadError = "Fail tetapan kosong.";
                return new AppSettings();
            }

            Normalize(settings);
            return settings;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            LastLoadError = ex.Message;
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Normalize(settings);

        var directory = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(directory);

        var tempPath = SettingsPath + ".tmp";
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, SettingsPath, overwrite: true);
    }

    public static void Normalize(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ZoneCode))
        {
            settings.ZoneCode = "WLY01";
        }

        if (string.IsNullOrWhiteSpace(settings.Theme))
        {
            settings.Theme = "Midnight";
        }

        if (!Enum.IsDefined(settings.Language))
        {
            settings.Language = AppLanguage.Malay;
        }

        settings.OpacityPercent = Math.Clamp(settings.OpacityPercent, 70, 100);
        settings.AzanVolume = Math.Clamp(settings.AzanVolume, 0, 100);
        settings.AzanPrayers ??= [];

        if (!Enum.IsDefined(settings.TaskbarNext))
        {
            settings.TaskbarNext = TaskbarNextAlignment.Off;
        }

        if (settings.AzanFile is not ("short" or "long"))
        {
            settings.AzanFile = "short";
        }
    }
}

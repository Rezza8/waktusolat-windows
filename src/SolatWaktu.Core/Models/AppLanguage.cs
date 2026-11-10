using System.Text.Json;
using System.Text.Json.Serialization;

namespace SolatWaktu.Core.Models;

public enum AppLanguage
{
    Malay,
    English
}

/// <summary>
/// Reads language values tolerantly. Older settings files do not contain a
/// language property, and hand-edited files may contain an unknown value; both
/// should fall back to Bahasa Melayu instead of discarding the whole settings
/// file.
/// </summary>
public sealed class AppLanguageJsonConverter : JsonConverter<AppLanguage>
{
    public override AppLanguage Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (string.Equals(value, nameof(AppLanguage.English), StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "EN", StringComparison.OrdinalIgnoreCase))
            {
                return AppLanguage.English;
            }

            return AppLanguage.Malay;
        }

        reader.Skip();
        return AppLanguage.Malay;
    }

    public override void Write(
        Utf8JsonWriter writer,
        AppLanguage value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value == AppLanguage.English
            ? nameof(AppLanguage.English)
            : nameof(AppLanguage.Malay));
    }
}

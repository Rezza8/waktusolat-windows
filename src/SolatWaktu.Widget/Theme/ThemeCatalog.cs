using System.Drawing;

namespace SolatWaktu.Widget.Theme;

public sealed record ThemeDefinition(
    string Name,
    Color Top,
    Color Bottom,
    Color Accent,
    Color AccentSoft,
    int OverlayAlpha = 55,
    Color[]? CustomStops = null)
{
    public Color[] GradientColors => CustomStops is { Length: > 1 }
        ? CustomStops
        : [Top, Bottom];
}

public static class ThemeCatalog
{
    public static IReadOnlyList<ThemeDefinition> All { get; } =
    [
        new("Midnight", Color.FromArgb(0x66, 0x7E, 0xEA), Color.FromArgb(0x76, 0x4B, 0xA2),
            Color.FromArgb(0x00, 0xD4, 0xAA), Color.FromArgb(0x00, 0xF5, 0xC8), 72),
        new("Ocean", Color.FromArgb(0x4F, 0xAC, 0xFE), Color.FromArgb(0x00, 0xF2, 0xFE),
            Color.FromArgb(0x00, 0xB4, 0xD8), Color.FromArgb(0x00, 0xE5, 0xFF), 132),
        new("Royal", Color.FromArgb(0xA1, 0x8C, 0xD1), Color.FromArgb(0xFB, 0xC2, 0xEB),
            Color.FromArgb(0xA8, 0x55, 0xF7), Color.FromArgb(0xD8, 0xB4, 0xFE), 126),
        new("Forest", Color.FromArgb(0x84, 0xFA, 0xB0), Color.FromArgb(0x8F, 0xD3, 0xF4),
            Color.FromArgb(0x10, 0xB9, 0x81), Color.FromArgb(0x6E, 0xE7, 0xB7), 152),
        new("Ember", Color.FromArgb(0xFA, 0x70, 0x9A), Color.FromArgb(0xFE, 0xE1, 0x40),
            Color.FromArgb(0xF9, 0x73, 0x16), Color.FromArgb(0xFD, 0xBA, 0x74), 133),
        new("Crimson", Color.FromArgb(0xFF, 0x9A, 0x9E), Color.FromArgb(0xFE, 0xCF, 0xEF),
            Color.FromArgb(0xEF, 0x44, 0x44), Color.FromArgb(0xFC, 0xA5, 0xA5), 131),
        new("Pelangi", Color.FromArgb(0x1A, 0x00, 0x33), Color.FromArgb(0x2D, 0x1B, 0x4E),
            Color.FromArgb(0xFF, 0xC1, 0x07), Color.FromArgb(0xFF, 0xD5, 0x4F), 62,
            [
                Color.FromArgb(0x2A, 0x08, 0x45),
                Color.FromArgb(0x4A, 0x14, 0x8C),
                Color.FromArgb(0x0D, 0x47, 0xA1),
                Color.FromArgb(0x00, 0x69, 0x5C),
                Color.FromArgb(0xB2, 0x6A, 0x00)
            ])
    ];

    public static ThemeDefinition Get(string? name) =>
        All.FirstOrDefault(theme => string.Equals(theme.Name, name, StringComparison.OrdinalIgnoreCase))
        ?? All[0];
}

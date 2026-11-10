namespace SolatWaktu.Core.Models;

/// <summary>
/// A JAKIM prayer-time zone. The code is the official zone identifier (for example WLY01).
/// </summary>
public sealed record Zone(string JakimCode, string Negeri, string Daerah)
{
    public string DisplayName => $"{Negeri} - {Daerah} ({JakimCode})";

    public override string ToString() => DisplayName;
}

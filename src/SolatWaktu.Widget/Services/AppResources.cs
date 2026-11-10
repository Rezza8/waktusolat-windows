using System.Drawing;
using System.Drawing.Text;
using System.Reflection;

namespace SolatWaktu.Widget.Services;

internal static class AppResources
{
    private const string IconResource = "SolatWaktu.Widget.Assets.app.ico";
    private const string MosqueResource = "SolatWaktu.Widget.Assets.mosque-green.png";

    private static readonly Lazy<Bitmap?> MosqueImage = new(LoadMosqueImage);
    private static readonly Lazy<string> HeadingFamily = new(() => ResolveFamily("Segoe UI Variable Display"));
    private static readonly Lazy<string> BodyFamily = new(() => ResolveFamily("Segoe UI Variable Text"));
    private static readonly Lazy<string> IconFamily = new(() => ResolveFamily("Segoe MDL2 Assets"));

    public static string HeadingFontFamily => HeadingFamily.Value;
    public static string BodyFontFamily => BodyFamily.Value;
    public static string IconFontFamily => IconFamily.Value;

    public static Bitmap? Mosque => MosqueImage.Value;

    public static Icon LoadAppIcon()
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(IconResource);
            if (stream is not null)
            {
                return new Icon(stream);
            }
        }
        catch (Exception)
        {
            // Fall back to the executable icon or the system icon.
        }

        try
        {
            var executable = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(executable))
            {
                var icon = Icon.ExtractAssociatedIcon(executable);
                if (icon is not null)
                {
                    return icon;
                }
            }
        }
        catch (Exception)
        {
        }

        return (Icon)SystemIcons.Application.Clone();
    }

    private static Bitmap? LoadMosqueImage()
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(MosqueResource);
            if (stream is null)
            {
                return null;
            }

            using var source = Image.FromStream(stream);
            return new Bitmap(source);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string ResolveFamily(string preferred)
    {
        try
        {
            using var installed = new InstalledFontCollection();
            if (installed.Families.Any(family =>
                    string.Equals(family.Name, preferred, StringComparison.OrdinalIgnoreCase)))
            {
                return preferred;
            }
        }
        catch (Exception)
        {
        }

        return "Segoe UI";
    }
}

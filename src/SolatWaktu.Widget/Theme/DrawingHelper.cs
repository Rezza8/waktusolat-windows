using System.Drawing;
using System.Drawing.Drawing2D;

namespace SolatWaktu.Widget.Theme;

internal static class DrawingHelper
{
    public static GraphicsPath RoundedRect(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();

        if (radius <= 0.1f || rect.Width <= 0 || rect.Height <= 0)
        {
            path.AddRectangle(rect);
            return path;
        }

        var diameter = Math.Min(radius * 2f, Math.Min(rect.Width, rect.Height));
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void FillRoundedRect(Graphics graphics, Brush brush, RectangleF rect, float radius)
    {
        using var path = RoundedRect(rect, radius);
        graphics.FillPath(brush, path);
    }

    public static void DrawRoundedRect(Graphics graphics, Pen pen, RectangleF rect, float radius)
    {
        using var path = RoundedRect(rect, radius);
        graphics.DrawPath(pen, path);
    }

    public static Color Alpha(Color color, int alpha) =>
        Color.FromArgb(Math.Clamp(alpha, 0, 255), color.R, color.G, color.B);

    public static Color MixWithWhite(Color color, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return Color.FromArgb(
            color.A,
            (int)Math.Round(color.R + (255 - color.R) * amount),
            (int)Math.Round(color.G + (255 - color.G) * amount),
            (int)Math.Round(color.B + (255 - color.B) * amount));
    }

    public static Color Darken(Color color, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return Color.FromArgb(
            color.A,
            (int)Math.Round(color.R * (1f - amount)),
            (int)Math.Round(color.G * (1f - amount)),
            (int)Math.Round(color.B * (1f - amount)));
    }

    public static LinearGradientBrush CreateGradientBrush(RectangleF rect, ThemeDefinition theme)
    {
        var colors = theme.GradientColors;
        var brush = new LinearGradientBrush(rect, colors[0], colors[^1], 38f);

        if (colors.Length > 2)
        {
            var blend = new ColorBlend
            {
                Colors = colors,
                Positions = Enumerable.Range(0, colors.Length)
                    .Select(index => (float)index / (colors.Length - 1))
                    .ToArray()
            };
            brush.InterpolationColors = blend;
        }

        return brush;
    }

    public static StringFormat EllipsisFormat()
    {
        return new StringFormat
        {
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap,
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center
        };
    }

    public static StringFormat NearCenter() => new()
    {
        Alignment = StringAlignment.Near,
        LineAlignment = StringAlignment.Center,
        FormatFlags = StringFormatFlags.NoWrap,
        Trimming = StringTrimming.EllipsisCharacter
    };

    public static StringFormat FarCenter() => new()
    {
        Alignment = StringAlignment.Far,
        LineAlignment = StringAlignment.Center,
        FormatFlags = StringFormatFlags.NoWrap
    };

    public static StringFormat CenterCenter() => new()
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center
    };
}

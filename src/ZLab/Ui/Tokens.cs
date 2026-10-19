using System.Drawing.Drawing2D;
using ZLab.Services;

namespace ZLab.Ui;

public static class Tokens
{
    public const int Space1 = 4;
    public const int Space2 = 8;
    public const int Space3 = 12;
    public const int Space4 = 16;
    public const int Space5 = 24;

    public const int ContentPadX = 24;
    public const int ContentPadTop = 20;
    public const int SectionGap = 24;
    public const int ItemGap = 12;

    public const int RadiusCard = 12;
    public const int RadiusButton = 6;
    public const int RadiusInner = 8;

    public const int BarHeightTile = 6;
    public const int BarHeightCard = 4;

    public const float CaptionPt = 9f;
    public const float BodyPt = 9.75f;
    public const float BodyStrongPt = 10.5f;
    public const float SubtitlePt = 11.25f;
    public const float TitlePt = 12f;
    public const float MetricPt = 18f;
    public const float MetricUnitPt = 9.75f;
    public const float CodePt = 15f;

    private static string? _uiFamily;
    private static string? _displayFamily;
    private static string? _monoFamily;
    private static string? _iconFamily;

    public static string UiFamily => _uiFamily ??= FirstAvailable(
        "Segoe UI Variable Text", "Segoe UI");

    public static string DisplayFamily => _displayFamily ??= FirstAvailable(
        "Segoe UI Variable Display", "Segoe UI");

    public static string MonoFamily => _monoFamily ??= FirstAvailable(
        "Cascadia Mono", "Consolas");

    public static string IconFamily => _iconFamily ??= FirstAvailable(
        "Segoe Fluent Icons", "Segoe MDL2 Assets");

    public static Font Caption() => new(UiFamily, CaptionPt, FontStyle.Regular);
    public static Font Body() => new(UiFamily, BodyPt, FontStyle.Regular);
    public static Font BodyStrong() => new(UiFamily, BodyStrongPt, FontStyle.Bold);
    public static Font Subtitle() => new(UiFamily, SubtitlePt, FontStyle.Bold);
    public static Font Title() => new(UiFamily, TitlePt, FontStyle.Bold);
    public static Font Metric() => new(DisplayFamily, MetricPt, FontStyle.Bold);
    public static Font MetricUnit() => new(UiFamily, MetricUnitPt, FontStyle.Regular);
    public static Font Code() => new(MonoFamily, CodePt, FontStyle.Bold);
    public static Font Icon(float pt = 12f) => new(IconFamily, pt, FontStyle.Regular);

    public static Color SeverityColor(double percent, Palette p) =>
        UiMath.Severity(percent) switch
        {
            SeverityKind.Ok => p.Ok,
            SeverityKind.Warn => p.Warn,
            _ => p.Bad
        };

    public static Color Mix(Color color, Color background, float opacity)
    {
        opacity = Math.Clamp(opacity, 0f, 1f);
        var r = (int)(color.R * opacity + background.R * (1 - opacity));
        var g = (int)(color.G * opacity + background.G * (1 - opacity));
        var b = (int)(color.B * opacity + background.B * (1 - opacity));
        return Color.FromArgb(r, g, b);
    }

    public static Color Darken(Color color, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return Color.FromArgb(
            color.A,
            (int)(color.R * (1 - amount)),
            (int)(color.G * (1 - amount)),
            (int)(color.B * (1 - amount)));
    }

    public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        var d = radius * 2;
        var arc = new Rectangle(bounds.Location, new Size(d, d));
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - d;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - d;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static string FirstAvailable(params string[] families)
    {
        var installed = new HashSet<string>(
            FontFamily.Families.Select(f => f.Name),
            StringComparer.OrdinalIgnoreCase);
        foreach (var name in families)
        {
            if (installed.Contains(name))
                return name;
        }

        return "Segoe UI";
    }
}

using System.Globalization;

namespace ZLab.Services;

/// <summary>
/// Display strings for host metrics (pt-BR), matching Windows-style byte labels.
/// </summary>
public static class DisplayFormat
{
    private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");
    private const long KiB = 1024L;
    private const long MiB = 1024L * 1024;
    private const long GiB = 1024L * 1024 * 1024;
    private const long TiB = 1024L * 1024 * 1024 * 1024;

    public static (string Number, string Unit) BytesParts(long bytes)
    {
        if (bytes < 0)
            bytes = 0;

        if (bytes < GiB)
        {
            var mb = (long)Math.Round(bytes / (double)MiB, MidpointRounding.AwayFromZero);
            return (mb.ToString(Pt), "MB");
        }

        if (bytes < 10 * GiB)
        {
            var gb = bytes / (double)GiB;
            return (gb.ToString("0.0", Pt), "GB");
        }

        if (bytes < TiB)
        {
            var gb = (long)Math.Round(bytes / (double)GiB, MidpointRounding.AwayFromZero);
            return (gb.ToString(Pt), "GB");
        }

        var tb = bytes / (double)TiB;
        return (tb.ToString("0.0", Pt), "TB");
    }

    public static string Bytes(long bytes)
    {
        var (n, u) = BytesParts(bytes);
        return n + " " + u;
    }

    public static string Percent(double value)
    {
        if (value > 0 && value < 1)
            return "<1%";
        return ((int)Math.Round(value, MidpointRounding.AwayFromZero)).ToString(Pt) + "%";
    }

    public static int PercentInt(double value) =>
        (int)Math.Round(value, MidpointRounding.AwayFromZero);

    public static IReadOnlyList<(string Number, string Unit)> UptimeParts(TimeSpan uptime)
    {
        if (uptime < TimeSpan.Zero)
            uptime = TimeSpan.Zero;

        if (uptime.TotalHours < 1)
            return new[] { (((int)uptime.TotalMinutes).ToString(Pt), "min") };

        if (uptime.TotalDays < 1)
            return new[]
            {
                (((int)uptime.TotalHours).ToString(Pt), "h"),
                (uptime.Minutes.ToString(Pt), "min")
            };

        return new[]
        {
            (((int)uptime.TotalDays).ToString(Pt), "d"),
            (uptime.Hours.ToString(Pt), "h")
        };
    }

    public static string Uptime(TimeSpan uptime)
    {
        var parts = UptimeParts(uptime);
        return string.Join(" ", parts.Select(p => p.Number + " " + p.Unit));
    }

    public static string Since(DateTime bootLocal, DateTime nowLocal)
    {
        var time = bootLocal.ToString("HH:mm", Pt);
        var bootDay = bootLocal.Date;
        var today = nowLocal.Date;
        if (bootDay == today)
            return "desde hoje, " + time;
        if (bootDay == today.AddDays(-1))
            return "desde ontem, " + time;
        return "desde " + bootLocal.ToString("dd/MM", Pt) + ", " + time;
    }

    public static string UsersConnected(int users) => users switch
    {
        0 => "nenhum usuário conectado",
        1 => "1 usuário conectado",
        _ => users.ToString(Pt) + " usuários conectados"
    };

    public static long KbToBytes(long kb) => kb * KiB;
}

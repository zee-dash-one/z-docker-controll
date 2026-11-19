using System.Globalization;

namespace ZLab.Services;

public sealed record WslTailscaleSnapshot(
    bool Exists,
    bool Running,
    DateTimeOffset? StartedAt,
    int Restarts,
    double? CpuPercent,
    double? MemPercent,
    string CpuText,
    string MemText)
{
    public static WslTailscaleSnapshot Absent { get; } = new(
        false, false, null, 0, null, null, "—", "—");
}

/// <summary>
/// Parses docker inspect and docker stats lines for the local WSL Tailscale container.
/// </summary>
public static class WslTailscaleParse
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static WslTailscaleSnapshot Parse(string? inspect, string? stats)
    {
        if (string.IsNullOrWhiteSpace(inspect))
            return WslTailscaleSnapshot.Absent;

        var parts = inspect.Trim().Split('|');
        if (parts.Length < 3)
            return WslTailscaleSnapshot.Absent;

        var state = parts[0].Trim();
        var running = string.Equals(state, "running", StringComparison.OrdinalIgnoreCase);
        DateTimeOffset? started = null;
        if (DateTimeOffset.TryParse(parts[1].Trim(), Inv, DateTimeStyles.RoundtripKind, out var dto))
            started = dto;
        _ = int.TryParse(parts[2].Trim(), NumberStyles.Integer, Inv, out var restarts);

        double? cpu = null;
        double? mem = null;
        var cpuText = "—";
        var memText = "—";
        if (running && !string.IsNullOrWhiteSpace(stats))
        {
            var statParts = stats.Trim().Split('|');
            if (statParts.Length >= 1)
            {
                cpuText = statParts[0].Trim();
                cpu = Percent(statParts[0]);
            }

            if (statParts.Length >= 2)
                memText = statParts[1].Trim().Replace(" / ", " de ", StringComparison.Ordinal);

            if (statParts.Length >= 3)
                mem = Percent(statParts[2]);
        }

        return new WslTailscaleSnapshot(true, running, started, restarts, cpu, mem, cpuText, memText);
    }

    private static double? Percent(string text)
    {
        var raw = text.Trim().TrimEnd('%').Trim();
        if (double.TryParse(raw, NumberStyles.Float, Inv, out var value))
            return value;
        return null;
    }
}

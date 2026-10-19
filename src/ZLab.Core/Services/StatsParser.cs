using System.Globalization;
using ZLab.Models;

namespace ZLab.Services;

public sealed class StatsSnapshot
{
    public HostStats? Host { get; init; }
}

public static class StatsParser
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static StatsSnapshot Parse(string output)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!string.IsNullOrEmpty(output))
        {
            foreach (var raw in output.Split('\n'))
            {
                var line = raw.TrimEnd('\r');
                if (line.Length == 0)
                    continue;
                if (line.StartsWith("section=", StringComparison.Ordinal))
                    break;
                var eq = line.IndexOf('=');
                if (eq <= 0)
                    continue;
                var key = line[..eq];
                var value = line[(eq + 1)..];
                map[key] = value;
            }
        }

        var host = TryBuildHost(map);
        return new StatsSnapshot { Host = host };
    }

    private static HostStats? TryBuildHost(Dictionary<string, string> map)
    {
        if (!TryLong(map, "now_epoch", out var nowEpoch)
            || !TryLong(map, "uptime_s", out var uptimeS)
            || !TryInt(map, "users", out var users)
            || !TryInt(map, "cores", out var cores)
            || !TryULong(map, "cpu_total", out var cpuTotal)
            || !TryULong(map, "cpu_idle", out var cpuIdle)
            || !TryLong(map, "mem_total_kb", out var memTotalKb)
            || !TryLong(map, "mem_available_kb", out var memAvailKb)
            || !TryLoad(map, out var l1, out var l5, out var l15)
            || !TryDisk(map, "disk_root", out var root)
            || !TryDisk(map, "disk_docker", out var docker)
            || !TryDisk(map, "disk_data", out var data))
        {
            return null;
        }

        var serverNow = DateTimeOffset.FromUnixTimeSeconds(nowEpoch);
        return new HostStats(
            Uptime: TimeSpan.FromSeconds(uptimeS),
            ServerNow: serverNow,
            Users: users,
            Cores: cores,
            CpuTotal: cpuTotal,
            CpuIdle: cpuIdle,
            MemTotalBytes: DisplayFormat.KbToBytes(memTotalKb),
            MemAvailableBytes: DisplayFormat.KbToBytes(memAvailKb),
            Load1: l1,
            Load5: l5,
            Load15: l15,
            Root: root,
            Docker: docker,
            Data: data);
    }

    private static bool TryLoad(Dictionary<string, string> map, out double l1, out double l5, out double l15)
    {
        l1 = l5 = l15 = 0;
        if (!map.TryGetValue("load", out var raw))
            return false;
        var parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
            return false;
        return double.TryParse(parts[0], NumberStyles.Float, Inv, out l1)
            && double.TryParse(parts[1], NumberStyles.Float, Inv, out l5)
            && double.TryParse(parts[2], NumberStyles.Float, Inv, out l15);
    }

    private static bool TryDisk(Dictionary<string, string> map, string key, out DiskUsage disk)
    {
        disk = new DiskUsage(false, 0, 0);
        if (!map.TryGetValue(key, out var raw))
            return false;
        if (string.Equals(raw, "absent", StringComparison.Ordinal))
        {
            disk = new DiskUsage(false, 0, 0);
            return true;
        }

        var parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return false;
        if (!long.TryParse(parts[0], NumberStyles.Integer, Inv, out var used)
            || !long.TryParse(parts[1], NumberStyles.Integer, Inv, out var total))
            return false;
        disk = new DiskUsage(true, used, total);
        return true;
    }

    private static bool TryLong(Dictionary<string, string> map, string key, out long value)
    {
        value = 0;
        return map.TryGetValue(key, out var raw)
            && long.TryParse(raw, NumberStyles.Integer, Inv, out value);
    }

    private static bool TryInt(Dictionary<string, string> map, string key, out int value)
    {
        value = 0;
        return map.TryGetValue(key, out var raw)
            && int.TryParse(raw, NumberStyles.Integer, Inv, out value);
    }

    private static bool TryULong(Dictionary<string, string> map, string key, out ulong value)
    {
        value = 0;
        return map.TryGetValue(key, out var raw)
            && ulong.TryParse(raw, NumberStyles.Integer, Inv, out value);
    }
}

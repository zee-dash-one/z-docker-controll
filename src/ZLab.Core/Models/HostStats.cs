namespace ZLab.Models;

public sealed record DiskUsage(bool Present, long UsedBytes, long TotalBytes)
{
    public double UsedPercent =>
        !Present || TotalBytes <= 0 ? 0 : 100.0 * UsedBytes / TotalBytes;

    public double FreePercent =>
        !Present || TotalBytes <= 0 ? 0 : 100.0 * Math.Max(0, TotalBytes - UsedBytes) / TotalBytes;
}

public sealed record HostStats(
    TimeSpan Uptime,
    DateTimeOffset ServerNow,
    int Users,
    int Cores,
    ulong CpuTotal,
    ulong CpuIdle,
    long MemTotalBytes,
    long MemAvailableBytes,
    double Load1,
    double Load5,
    double Load15,
    DiskUsage Root,
    DiskUsage Docker,
    DiskUsage Data)
{
    public long MemUsedBytes => Math.Max(0, MemTotalBytes - MemAvailableBytes);
}

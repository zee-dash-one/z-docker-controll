using System.Globalization;
using ZLab.Models;

namespace ZLab.Services;

public enum ServiceCardBodyKind
{
    Pending,
    Monitor,
    StoppedBox,
    AbsentBox,
    ErrorMonitor
}

public sealed record ServiceCardView(
    StatusPillKindUi Pill,
    string AddressLabel,
    string? OpenUrl,
    ServiceCardBodyKind Body,
    string StateLine,
    double? CpuPercent,
    string CpuText,
    double? MemPercent,
    string MemText,
    bool CanStart,
    bool CanStop,
    bool CanOpen,
    bool CanCreate,
    bool CanUpdate,
    bool CanLogs,
    bool CanUsage);

public enum StatusPillKindUi
{
    Online,
    Stopped,
    Error,
    Unknown
}

public static class ServiceCardPresenter
{
    private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");

    public static ServiceCardView From(
        RemoteService service,
        ContainerStats? container,
        int cores,
        DateTimeOffset nowUtc,
        bool hasReading)
    {
        var openUrl = service.LanUrl ?? service.HttpsLabel;
        var address = StripScheme(openUrl ?? "");

        if (!hasReading)
        {
            return new ServiceCardView(
                Pill: StatusPillKindUi.Unknown,
                AddressLabel: address,
                OpenUrl: openUrl,
                Body: ServiceCardBodyKind.Pending,
                StateLine: "—",
                CpuPercent: null,
                CpuText: "—",
                MemPercent: null,
                MemText: "—",
                CanStart: false,
                CanStop: false,
                CanOpen: false,
                CanCreate: false,
                CanUpdate: false,
                CanLogs: false,
                CanUsage: false);
        }

        if (container is null)
        {
            return new ServiceCardView(
                Pill: StatusPillKindUi.Unknown,
                AddressLabel: address,
                OpenUrl: openUrl,
                Body: ServiceCardBodyKind.AbsentBox,
                StateLine: "Container não criado · use Criar no menu ⋯",
                CpuPercent: null,
                CpuText: "—",
                MemPercent: null,
                MemText: "—",
                CanStart: false,
                CanStop: false,
                CanOpen: false,
                CanCreate: true,
                CanUpdate: true,
                CanLogs: false,
                CanUsage: false);
        }

        var state = (container.State ?? "").Trim().ToLowerInvariant();
        return state switch
        {
            "running" => Running(service, container, cores, nowUtc, address, openUrl),
            "exited" or "created" or "paused" => Stopped(address, openUrl, canLogs: true),
            "restarting" => Errorish(address, openUrl, "Reiniciando…", canStop: true, canLogs: true),
            "dead" => Errorish(address, openUrl, "Com erro", canStop: true, canLogs: true),
            _ => Stopped(address, openUrl, canLogs: true)
        };
    }

    public static double? CpuOfHost(double? dockerCpuPercent, int cores)
    {
        if (dockerCpuPercent is null || cores <= 0)
            return null;
        return dockerCpuPercent.Value / cores;
    }

    public static string CpuText(double? hostPercent)
    {
        if (hostPercent is null)
            return "—";
        return hostPercent.Value.ToString("0.0", Pt) + "%";
    }

    public static string MemText(long? used, long? limit)
    {
        if (used is null || limit is null || limit <= 0)
            return "—";
        return DisplayFormat.Bytes(used.Value) + " de " + DisplayFormat.Bytes(limit.Value);
    }

    public static string RestartsLabel(int n) => n switch
    {
        1 => "1 reinício",
        _ => n.ToString(Pt) + " reinícios"
    };

    public static ContainerStatusKind ToStatusKind(ContainerStats? c, bool hasReading)
    {
        if (!hasReading)
            return ContainerStatusKind.Unknown;
        if (c is null)
            return ContainerStatusKind.Absent;
        return (c.State ?? "").Trim().ToLowerInvariant() switch
        {
            "running" => ContainerStatusKind.Up,
            "restarting" => ContainerStatusKind.Busy,
            "dead" => ContainerStatusKind.Error,
            "exited" or "created" or "paused" => ContainerStatusKind.Down,
            _ => ContainerStatusKind.Down
        };
    }

    private static ServiceCardView Running(
        RemoteService service,
        ContainerStats c,
        int cores,
        DateTimeOffset nowUtc,
        string address,
        string? openUrl)
    {
        var uptime = c.StartedAt is DateTimeOffset start
            ? DisplayFormat.Uptime(nowUtc - start)
            : "—";
        var stateLine = "Ativo há " + uptime + " · " + RestartsLabel(c.Restarts);
        // CpuPercentOfHost from the parser is still Docker's per-core-sum %.
        var cpuHost = CpuOfHost(c.CpuPercentOfHost, cores);
        double? memPct = null;
        if (c.MemUsedBytes is long u && c.MemLimitBytes is long lim && lim > 0)
            memPct = 100.0 * u / lim;

        return new ServiceCardView(
            Pill: StatusPillKindUi.Online,
            AddressLabel: address,
            OpenUrl: openUrl,
            Body: ServiceCardBodyKind.Monitor,
            StateLine: stateLine,
            CpuPercent: cpuHost,
            CpuText: CpuText(cpuHost),
            MemPercent: memPct,
            MemText: MemText(c.MemUsedBytes, c.MemLimitBytes),
            CanStart: false,
            CanStop: true,
            CanOpen: openUrl is not null,
            CanCreate: false,
            CanUpdate: true,
            CanLogs: true,
            CanUsage: true);
    }

    private static ServiceCardView Stopped(string address, string? openUrl, bool canLogs) => new(
        Pill: StatusPillKindUi.Stopped,
        AddressLabel: address,
        OpenUrl: openUrl,
        Body: ServiceCardBodyKind.StoppedBox,
        StateLine: "Container parado · sem consumo",
        CpuPercent: null,
        CpuText: "—",
        MemPercent: null,
        MemText: "—",
        CanStart: true,
        CanStop: false,
        CanOpen: false,
        CanCreate: false,
        CanUpdate: true,
        CanLogs: canLogs,
        CanUsage: false);

    private static ServiceCardView Errorish(
        string address,
        string? openUrl,
        string stateLine,
        bool canStop,
        bool canLogs) => new(
        Pill: StatusPillKindUi.Error,
        AddressLabel: address,
        OpenUrl: openUrl,
        Body: ServiceCardBodyKind.ErrorMonitor,
        StateLine: stateLine,
        CpuPercent: null,
        CpuText: "—",
        MemPercent: null,
        MemText: "—",
        CanStart: false,
        CanStop: canStop,
        CanOpen: false,
        CanCreate: false,
        CanUpdate: true,
        CanLogs: canLogs,
        CanUsage: false);

    private static string StripScheme(string url)
    {
        if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return url["https://".Length..];
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            return url["http://".Length..];
        return url;
    }
}

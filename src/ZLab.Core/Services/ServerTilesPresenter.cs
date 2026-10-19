using System.Globalization;
using ZLab.Models;

namespace ZLab.Services;

public sealed record TileView(
    bool IsEmpty,
    IReadOnlyList<(string Number, string Unit)> ValueParts,
    string Caption,
    double? Percent,
    string? FooterText,
    string ToolTip,
    double? SparkSample);

public sealed record ServerTilesView(
    TileView Cpu,
    TileView Memory,
    TileView DiskRoot,
    TileView DiskDocker,
    TileView Data,
    TileView Uptime);

public static class ServerTilesPresenter
{
    private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly (string, string)[] Dash = { ("—", "") };

    public static ServerTilesView Empty() => new(
        Cpu: DashTile("", ""),
        Memory: DashTile("", ""),
        DiskRoot: DashTile("", ""),
        DiskDocker: DashTile("", ""),
        Data: DashTile("", ""),
        Uptime: DashTile("", ""));

    public static ServerTilesView From(HostStats? host, double? cpuPercent, DateTime nowLocal)
    {
        if (host is null)
            return Empty();

        var cpuValue = cpuPercent is double c
            ? new[] { (DisplayFormat.PercentInt(c).ToString(Pt), "%") }
            : Dash;
        var loadTip =
            "Carga média (1, 5 e 15 min): "
            + host.Load1.ToString("0.00", Pt) + " · "
            + host.Load5.ToString("0.00", Pt) + " · "
            + host.Load15.ToString("0.00", Pt)
            + ". É quantos processos, em média, querem CPU ao mesmo tempo. Com "
            + host.Cores.ToString(Pt)
            + " núcleos, abaixo de "
            + host.Cores.ToString(Pt)
            + " está tranquilo.";

        var memUsedPct = host.MemTotalBytes <= 0
            ? 0
            : 100.0 * host.MemUsedBytes / host.MemTotalBytes;
        var memCaption =
            "de " + DisplayFormat.Bytes(host.MemTotalBytes)
            + " · " + DisplayFormat.Bytes(host.MemAvailableBytes) + " livres";

        var bootLocal = nowLocal - host.Uptime;

        return new ServerTilesView(
            Cpu: new TileView(
                IsEmpty: false,
                ValueParts: cpuValue,
                Caption: "média dos " + host.Cores.ToString(Pt) + " núcleos",
                Percent: null,
                FooterText: null,
                ToolTip: loadTip,
                SparkSample: cpuPercent),
            Memory: new TileView(
                IsEmpty: false,
                ValueParts: new[] { DisplayFormat.BytesParts(host.MemUsedBytes) },
                Caption: memCaption,
                Percent: memUsedPct,
                FooterText: null,
                ToolTip: "Em uso = total − disponível (o mesmo critério do Gerenciador de Tarefas).",
                SparkSample: null),
            DiskRoot: DiskTile(host.Root, "Partição / (ubuntu-lv)"),
            DiskDocker: DiskTile(host.Docker, "Partição /var/lib/docker (docker-lv)"),
            Data: host.Data.Present
                ? DiskTile(host.Data, "Quando /data for montado, este bloco mostra o uso igual aos outros discos.")
                : new TileView(
                    IsEmpty: true,
                    ValueParts: new[] { ("Não instalado", "") },
                    Caption: "HD de 500 GB ainda não montado",
                    Percent: null,
                    FooterText: null,
                    ToolTip: "Quando /data for montado, este bloco mostra o uso igual aos outros discos.",
                    SparkSample: null),
            Uptime: new TileView(
                IsEmpty: false,
                ValueParts: DisplayFormat.UptimeParts(host.Uptime),
                Caption: DisplayFormat.Since(bootLocal, nowLocal),
                Percent: null,
                FooterText: DisplayFormat.UsersConnected(host.Users),
                ToolTip: "",
                SparkSample: null));
    }

    private static TileView DiskTile(DiskUsage disk, string tip)
    {
        if (!disk.Present)
        {
            return new TileView(
                IsEmpty: true,
                ValueParts: new[] { ("Não instalado", "") },
                Caption: "",
                Percent: null,
                FooterText: null,
                ToolTip: tip,
                SparkSample: null);
        }

        var freePct = DisplayFormat.PercentInt(disk.FreePercent);
        return new TileView(
            IsEmpty: false,
            ValueParts: new[] { DisplayFormat.BytesParts(disk.UsedBytes) },
            Caption: "de " + DisplayFormat.Bytes(disk.TotalBytes) + " · " + freePct.ToString(Pt) + "% livre",
            Percent: disk.UsedPercent,
            FooterText: null,
            ToolTip: tip,
            SparkSample: null);
    }

    private static TileView DashTile(string caption, string tip) => new(
        IsEmpty: false,
        ValueParts: Dash,
        Caption: caption,
        Percent: null,
        FooterText: null,
        ToolTip: tip,
        SparkSample: null);
}

using ZLab.Models;
using ZLab.Services;

namespace ZLab.Tests;

public class ServerTilesPresenterTests
{
    [Fact]
    public void Memory_and_absent_data_match_example()
    {
        var host = new HostStats(
            Uptime: TimeSpan.FromSeconds(39840),
            ServerNow: DateTimeOffset.FromUnixTimeSeconds(1792387824),
            Users: 2,
            Cores: 4,
            CpuTotal: 19971908,
            CpuIdle: 19568684,
            MemTotalBytes: 16211000L * 1024,
            MemAvailableBytes: 14740000L * 1024,
            Load1: 0.10,
            Load5: 0.05,
            Load15: 0.04,
            Root: new DiskUsage(true, 12884901888, 52613349376),
            Docker: new DiskUsage(true, 95420416, 157840048128),
            Data: new DiskUsage(false, 0, 0));

        var view = ServerTilesPresenter.From(host, cpuPercent: 7, nowLocal: new DateTime(2026, 10, 19, 16, 0, 0));

        Assert.Equal("1,4", view.Memory.ValueParts[0].Number);
        Assert.Equal("GB", view.Memory.ValueParts[0].Unit);
        Assert.Equal("de 15 GB · 14 GB livres", view.Memory.Caption);
        Assert.NotNull(view.Memory.Percent);
        Assert.Equal(9, DisplayFormat.PercentInt(view.Memory.Percent!.Value));

        Assert.True(view.Data.IsEmpty);
        Assert.Equal("Não instalado", view.Data.ValueParts[0].Number);
        Assert.Equal("HD de 500 GB ainda não montado", view.Data.Caption);

        Assert.Equal("91", view.DiskDocker.ValueParts[0].Number);
        Assert.Equal("MB", view.DiskDocker.ValueParts[0].Unit);
        Assert.Contains("0,10", view.Cpu.ToolTip);
    }

    [Fact]
    public void Empty_shows_dashes()
    {
        var view = ServerTilesPresenter.Empty();
        Assert.Equal("—", view.Cpu.ValueParts[0].Number);
        Assert.False(view.Data.IsEmpty);
    }
}

using ZLab.Services;

namespace ZLab.Tests;

public class WslTailscaleParseTests
{
    [Fact]
    public void Parse_running_stats()
    {
        var snap = WslTailscaleParse.Parse(
            "running|2026-10-20T14:00:00.000000000Z|2",
            "0.72%|608.9MiB / 15GiB|4.05%");

        Assert.True(snap.Exists);
        Assert.True(snap.Running);
        Assert.Equal(2, snap.Restarts);
        Assert.Equal(0.72, snap.CpuPercent);
        Assert.Equal(4.05, snap.MemPercent);
        Assert.Equal("608.9MiB de 15GiB", snap.MemText);
        Assert.NotNull(snap.StartedAt);
    }

    [Fact]
    public void Parse_missing_inspect_is_absent()
    {
        var snap = WslTailscaleParse.Parse(" ", null);
        Assert.False(snap.Exists);
        Assert.False(snap.Running);
    }

    [Fact]
    public void Services_list_does_not_include_wsl_tailscale()
    {
        Assert.DoesNotContain(RemoteCommands.Services, s => s.Id == "wsl-tailscale");
    }
}

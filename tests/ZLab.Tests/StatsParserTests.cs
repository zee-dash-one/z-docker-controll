using ZLab.Services;

namespace ZLab.Tests;

public class StatsParserTests
{
    private const string Sample = """
        v=1
        now_epoch=1792387824
        uptime_s=39840
        users=2
        cores=4
        cpu_total=19971908
        cpu_idle=19568684
        mem_total_kb=16211000
        mem_available_kb=14740000
        load=0.10 0.05 0.04
        disk_root=12884901888 52613349376
        disk_docker=95420416 157840048128
        disk_data=absent
        """;

    [Fact]
    public void Parse_sample_maps_values()
    {
        var snap = StatsParser.Parse(Sample);
        Assert.NotNull(snap.Host);
        var h = snap.Host!;
        Assert.Equal(TimeSpan.FromSeconds(39840), h.Uptime);
        Assert.Equal(2, h.Users);
        Assert.Equal(4, h.Cores);
        Assert.Equal(19971908ul, h.CpuTotal);
        Assert.Equal(19568684ul, h.CpuIdle);
        Assert.Equal(16211000L * 1024, h.MemTotalBytes);
        Assert.Equal(14740000L * 1024, h.MemAvailableBytes);
        Assert.Equal(0.10, h.Load1, 3);
        Assert.Equal(0.05, h.Load5, 3);
        Assert.Equal(0.04, h.Load15, 3);
        Assert.True(h.Root.Present);
        Assert.Equal(12884901888L, h.Root.UsedBytes);
        Assert.True(h.Docker.Present);
        Assert.Equal(95420416L, h.Docker.UsedBytes);
        Assert.False(h.Data.Present);
    }

    [Fact]
    public void Parse_ignores_junk_lines()
    {
        var snap = StatsParser.Parse(Sample + "\nlixo\nfoo=bar\n");
        Assert.NotNull(snap.Host);
    }

    [Fact]
    public void Parse_missing_mem_returns_null_host()
    {
        var without = string.Join(
            "\n",
            Sample.Split('\n', StringSplitOptions.None)
                .Where(l => !l.StartsWith("mem_total_kb=", StringComparison.Ordinal)));
        var snap = StatsParser.Parse(without);
        Assert.Null(snap.Host);
    }

    [Fact]
    public void Parse_stops_at_section()
    {
        var snap = StatsParser.Parse(Sample + "\nsection=containers\nmem_total_kb=1\n");
        Assert.NotNull(snap.Host);
        Assert.Equal(16211000L * 1024, snap.Host!.MemTotalBytes);
    }
}

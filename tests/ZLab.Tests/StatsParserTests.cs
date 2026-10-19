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
    public void Parse_stops_host_keys_at_section()
    {
        var snap = StatsParser.Parse(Sample + "\nsection=containers\nmem_total_kb=1\n");
        Assert.NotNull(snap.Host);
        Assert.Equal(16211000L * 1024, snap.Host!.MemTotalBytes);
    }

    [Fact]
    public void Parse_containers_example_lines()
    {
        var text = Sample + """

            section=containers
            c=9router	running	2026-10-16T22:14:03.123456789Z	0	3.21%	85.2MiB / 15.5GiB
            c=tailscale-mausbot	exited	2026-10-18T10:00:00Z	2	-	-
            """;
        var snap = StatsParser.Parse(text);
        Assert.Equal(2, snap.Containers.Count);
        var nine = Assert.Single(snap.Containers, c => c.Name == "9router");
        Assert.Equal("running", nine.State);
        Assert.NotNull(nine.StartedAt);
        Assert.Equal(0, nine.Restarts);
        Assert.Equal(3.21, nine.CpuPercentOfHost);
        Assert.NotNull(nine.MemUsedBytes);
        Assert.NotNull(nine.MemLimitBytes);

        var ts = Assert.Single(snap.Containers, c => c.Name == "tailscale-mausbot");
        Assert.Equal("exited", ts.State);
        Assert.Null(ts.CpuPercentOfHost);
        Assert.Null(ts.MemUsedBytes);
        Assert.Equal(2, ts.Restarts);
    }

    [Fact]
    public void Parse_started_at_truncates_nine_fraction_digits()
    {
        var dto = StatsParser.ParseStartedAt("2026-10-16T22:14:03.123456789Z");
        Assert.NotNull(dto);
        Assert.Equal(2026, dto!.Value.Year);
    }

    [Fact]
    public void Parse_started_at_zero_is_null()
    {
        Assert.Null(StatsParser.ParseStartedAt("0001-01-01T00:00:00Z"));
    }
}


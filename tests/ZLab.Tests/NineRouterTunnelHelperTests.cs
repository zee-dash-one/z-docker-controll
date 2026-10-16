using ZLab.Models;
using ZLab.Services;

namespace ZLab.Tests;

public sealed class NineRouterTunnelHelperTests
{
    [Fact]
    public void ComputeCliToken_MatchesKnownFixture()
    {
        var token = NineRouterTunnelHelper.ComputeCliToken("mid", "secret");
        Assert.Equal(16, token.Length);
        Assert.Matches("^[0-9a-f]{16}$", token);
        Assert.Equal(
            NineRouterTunnelHelper.ComputeCliToken("mid", "secret"),
            token);
        Assert.NotEqual(
            NineRouterTunnelHelper.ComputeCliToken("mid", "other"),
            token);
    }

    [Fact]
    public void ResolveVolumeName_PrefersNamedVolume()
    {
        var item = new ServiceItem
        {
            Id = "9router",
            Volumes = new List<string> { "9router-data:/app/data" }
        };
        Assert.Equal("9router-data", NineRouterTunnelHelper.ResolveVolumeName(item));
    }

    [Fact]
    public void ResolveVolumeName_FallsBackToDefault()
    {
        var item = new ServiceItem { Id = "9router", Volumes = new List<string>() };
        Assert.Equal(NineRouterTunnelHelper.DefaultVolumeName, NineRouterTunnelHelper.ResolveVolumeName(item));
    }

    [Fact]
    public async Task EnableAsync_RejectsNonNineRouter()
    {
        var helper = new NineRouterTunnelHelper(new DockerCli());
        var r = await helper.EnableAsync(new ServiceItem { Id = "omniroute", HostPort = 20129 });
        Assert.False(r.Ok);
        Assert.Contains("9router", r.Message, StringComparison.OrdinalIgnoreCase);
    }
}

using ZLab.Services;

namespace ZLab.Tests;

public class DockerMemParserTests
{
    [Theory]
    [InlineData("85.2MiB / 15.5GiB", 89338675L, 16642998272L)] // approx
    [InlineData("512KiB / 1GiB", 524288L, 1073741824L)]
    [InlineData("1.2GB / 16GB", 1200000000L, 16000000000L)]
    public void Parses_pairs(string text, long used, long limit)
    {
        Assert.True(DockerMemParser.TryParsePair(text, out var u, out var lim));
        Assert.Equal(used, u);
        Assert.Equal(limit, lim);
    }

    [Fact]
    public void Dash_is_false()
    {
        Assert.False(DockerMemParser.TryParsePair("-", out _, out _));
    }
}

using ZLab.Services;

namespace ZLab.Tests;

public class DisplayFormatTests
{
    [Theory]
    [InlineData(95420416L, "91", "MB")]
    [InlineData(1503238554L, "1,4", "GB")]
    [InlineData(12884901888L, "12", "GB")]
    [InlineData(157840048128L, "147", "GB")]
    public void BytesParts_windows_style(long bytes, string number, string unit)
    {
        var (n, u) = DisplayFormat.BytesParts(bytes);
        Assert.Equal(number, n);
        Assert.Equal(unit, u);
    }

    [Fact]
    public void Uptime_formats()
    {
        Assert.Equal("11 h 4 min", DisplayFormat.Uptime(TimeSpan.FromSeconds(39840)));
        Assert.Equal("2 d 4 h", DisplayFormat.Uptime(TimeSpan.FromSeconds(187200)));
        Assert.Equal("35 min", DisplayFormat.Uptime(TimeSpan.FromSeconds(2100)));
    }

    [Fact]
    public void Since_today_yesterday_and_earlier()
    {
        var now = new DateTime(2026, 10, 19, 16, 0, 0);
        Assert.Equal("desde hoje, 15:26", DisplayFormat.Since(new DateTime(2026, 10, 19, 15, 26, 0), now));
        Assert.Equal("desde ontem, 15:26", DisplayFormat.Since(new DateTime(2026, 10, 18, 15, 26, 0), now));
        Assert.Equal("desde 16/10, 15:26", DisplayFormat.Since(new DateTime(2026, 10, 16, 15, 26, 0), now));
    }

    [Fact]
    public void Percent_under_one()
    {
        Assert.Equal("<1%", DisplayFormat.Percent(0.4));
        Assert.Equal("9%", DisplayFormat.Percent(9.2));
    }
}

using ZLab.Services;

namespace ZLab.Tests;

public class CpuUsageCalculatorTests
{
    [Fact]
    public void First_sample_is_null()
    {
        var calc = new CpuUsageCalculator();
        Assert.Null(calc.Percent(1000, 800));
    }

    [Fact]
    public void Second_sample_computes_percent()
    {
        var calc = new CpuUsageCalculator();
        Assert.Null(calc.Percent(1000, 800));
        // Δtotal=100, Δidle=50 → 50%
        var pct = calc.Percent(1100, 850);
        Assert.NotNull(pct);
        Assert.Equal(50.0, pct!.Value, 5);
    }

    [Fact]
    public void Counter_rewind_returns_null()
    {
        var calc = new CpuUsageCalculator();
        Assert.Null(calc.Percent(1000, 800));
        Assert.NotNull(calc.Percent(1100, 850));
        Assert.Null(calc.Percent(500, 400));
    }
}

using ZLab.Services;

namespace ZLab.Tests;

public class UiTokensTests
{
    [Theory]
    [InlineData(0, SeverityKind.Ok)]
    [InlineData(69.9, SeverityKind.Ok)]
    [InlineData(70, SeverityKind.Warn)]
    [InlineData(90, SeverityKind.Warn)]
    [InlineData(90.1, SeverityKind.Bad)]
    [InlineData(100, SeverityKind.Bad)]
    public void Severity_thresholds(double percent, SeverityKind expected)
    {
        Assert.Equal(expected, UiMath.Severity(percent));
    }

    [Theory]
    [InlineData(800, 180, 12, 6, 4)]
    [InlineData(200, 180, 12, 6, 1)]
    [InlineData(1200, 180, 12, 6, 6)]
    [InlineData(400, 180, 12, 3, 2)]
    public void ColumnsFor_matches_grid_rule(int width, int min, int gap, int max, int expected)
    {
        Assert.Equal(expected, UiMath.ColumnsFor(width, min, gap, max));
    }
}

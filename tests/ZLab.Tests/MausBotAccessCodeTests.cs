using ZLab.Services;

namespace ZLab.Tests;

public class MausBotAccessCodeTests
{
    [Fact]
    public void Parse_last_occurrence_wins()
    {
        var logs = """
            2026-10-19T12:00:00.000000000Z pairing code:  OLDOLD
            noise
            2026-10-19T14:02:00.000000000Z pairing code:  ABC123
            """;
        var result = MausBotAccessCode.Parse(logs);
        Assert.True(result.Found);
        Assert.Equal("ABC123", result.Code);
        Assert.Equal(new DateTimeOffset(2026, 10, 19, 14, 2, 0, TimeSpan.Zero), result.SeenAt);
        Assert.Null(result.Problem);
    }

    [Fact]
    public void Parse_openmausbot_grouped_format()
    {
        var logs = "2026-10-19T10:00:00Z pairing code:  RR8Y-BLR6-H939\n";
        var result = MausBotAccessCode.Parse(logs);
        Assert.True(result.Found);
        Assert.Equal("RR8Y-BLR6-H939", result.Code);
    }

    [Fact]
    public void Parse_none_returns_not_found_message()
    {
        var result = MausBotAccessCode.Parse("listening on :8799\nready\n");
        Assert.False(result.Found);
        Assert.Null(result.Code);
        Assert.Equal(MausBotAccessCode.NotFoundProblem, result.Problem);
    }

    [Fact]
    public void ToString_does_not_include_code()
    {
        var result = new AccessCodeResult(true, "ABC123", DateTimeOffset.Parse("2026-10-19T14:02:00Z"), null);
        var text = result.ToString();
        Assert.DoesNotContain("ABC123", text, StringComparison.Ordinal);
        Assert.Contains("Found=True", text, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatForDisplay_groups_of_two()
    {
        Assert.Equal("AB C1 23", MausBotAccessCode.FormatForDisplay("ABC123"));
        Assert.Equal("48 31 92", MausBotAccessCode.FormatForDisplay("483192"));
    }
}

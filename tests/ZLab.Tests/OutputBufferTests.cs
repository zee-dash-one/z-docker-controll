using ZLab.Services;

namespace ZLab.Tests;

public class OutputBufferTests
{
    [Fact]
    public void Drops_oldest_when_over_max()
    {
        var buf = new OutputBuffer();
        for (var i = 0; i < OutputBuffer.MaxLines + 25; i++)
            buf.Append("linha-" + i);

        Assert.Equal(OutputBuffer.MaxLines, buf.Count);
        Assert.Equal("linha-25", buf.Lines[0].Message);
        Assert.Equal("linha-" + (OutputBuffer.MaxLines + 24), buf.Last!.Message);
    }

    [Fact]
    public void Unread_increments_and_mark_read_clears()
    {
        var buf = new OutputBuffer();
        buf.Append("a");
        buf.Append("b");
        Assert.Equal(2, buf.UnreadCount);
        buf.MarkRead();
        Assert.Equal(0, buf.UnreadCount);
        buf.Append("c");
        Assert.Equal(1, buf.UnreadCount);
    }

    [Fact]
    public void Clear_resets_lines_and_unread()
    {
        var buf = new OutputBuffer();
        buf.Append("x");
        buf.Clear();
        Assert.Equal(0, buf.Count);
        Assert.Equal(0, buf.UnreadCount);
        Assert.Null(buf.Last);
    }

    [Fact]
    public void Classify_maps_error_and_warn()
    {
        Assert.Equal(OutputLineKind.Error, OutputBuffer.Classify("O comando remoto falhou."));
        Assert.Equal(OutputLineKind.Warn, OutputBuffer.Classify("aviso: disco cheio"));
        Assert.Equal(OutputLineKind.Info, OutputBuffer.Classify("Sessão aberta."));
    }
}

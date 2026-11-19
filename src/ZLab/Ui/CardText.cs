namespace ZLab.Ui;

internal static class CardText
{
    public const TextFormatFlags ValueFlags =
        TextFormatFlags.Right
        | TextFormatFlags.VerticalCenter
        | TextFormatFlags.NoPrefix
        | TextFormatFlags.EndEllipsis
        | TextFormatFlags.SingleLine;

    public static int ValueWidth(Font font, string cpu, string mem, int pad)
    {
        var flags = TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
        var w = Math.Max(
            TextRenderer.MeasureText(cpu, font, Size.Empty, flags).Width,
            TextRenderer.MeasureText(mem, font, Size.Empty, flags).Width);
        return Math.Max(w + pad, pad);
    }
}

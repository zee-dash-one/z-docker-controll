namespace ZLab.Ui;

public enum StatusPillKind
{
    Online,
    Stopped,
    Error,
    Unknown
}

public sealed class StatusPill : Control
{
    private StatusPillKind _kind = StatusPillKind.Unknown;

    public StatusPill()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw
            | ControlStyles.SupportsTransparentBackColor,
            true);
        Height = LogicalToDeviceUnits(22);
        ThemeManager.ThemeChanged += (_, _) => Invalidate();
        RecalcWidth();
    }

    public StatusPillKind Kind
    {
        get => _kind;
        set
        {
            _kind = value;
            RecalcWidth();
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var p = ThemeManager.Current;
        var (text, fg, bg) = Resolve(p);
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var path = Tokens.RoundedRect(bounds, Height / 2))
        using (var brush = new SolidBrush(bg))
            g.FillPath(brush, path);

        var pad = LogicalToDeviceUnits(8);
        var dot = LogicalToDeviceUnits(6);
        var cy = Height / 2;
        using (var brush = new SolidBrush(fg))
            g.FillEllipse(brush, pad, cy - dot / 2, dot, dot);

        using var font = new Font(Tokens.UiFamily, Tokens.CaptionPt, FontStyle.Bold);
        var textRect = new Rectangle(pad + dot + LogicalToDeviceUnits(6), 0, Width, Height);
        TextRenderer.DrawText(
            g,
            text,
            font,
            textRect,
            fg,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
    }

    private void RecalcWidth()
    {
        using var font = new Font(Tokens.UiFamily, Tokens.CaptionPt, FontStyle.Bold);
        var text = Resolve(ThemeManager.Current).Text;
        var size = TextRenderer.MeasureText(text, font);
        Width = LogicalToDeviceUnits(8 + 6 + 6) + size.Width + LogicalToDeviceUnits(10);
    }

    private (string Text, Color Fg, Color Bg) Resolve(Palette p) => _kind switch
    {
        StatusPillKind.Online => ("No ar", p.Ok, p.OkSoft),
        StatusPillKind.Stopped => ("Parado", p.Muted, p.MutedSoft),
        StatusPillKind.Error => ("Erro", p.Bad, p.BadSoft),
        _ => ("—", p.Muted, p.MutedSoft)
    };
}

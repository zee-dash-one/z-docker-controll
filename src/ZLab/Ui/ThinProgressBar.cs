using System.Drawing.Drawing2D;

namespace ZLab.Ui;

public enum ProgressColorMode
{
    Severity,
    Accent
}

public sealed class ThinProgressBar : Control
{
    private double _value;
    private int _barHeight = Tokens.BarHeightTile;
    private ProgressColorMode _colorMode = ProgressColorMode.Severity;

    public ThinProgressBar()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw
            | ControlStyles.SupportsTransparentBackColor,
            true);
        Height = LogicalToDeviceUnits(Tokens.BarHeightTile + 2);
        ThemeManager.ThemeChanged += (_, _) => Invalidate();
    }

    public double Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, 0, 100);
            Invalidate();
        }
    }

    public int BarHeight
    {
        get => _barHeight;
        set
        {
            _barHeight = Math.Max(2, value);
            Invalidate();
        }
    }

    public ProgressColorMode ColorMode
    {
        get => _colorMode;
        set
        {
            _colorMode = value;
            Invalidate();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            ThemeManager.ThemeChanged -= (_, _) => Invalidate();
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var p = ThemeManager.Current;
        var h = LogicalToDeviceUnits(_barHeight);
        var y = Math.Max(0, (Height - h) / 2);
        var bounds = new Rectangle(0, y, Width - 1, h);
        var radius = h / 2;
        using (var path = Tokens.RoundedRect(bounds, radius))
        using (var brush = new SolidBrush(p.Track))
            g.FillPath(brush, path);

        if (_value <= 0)
            return;

        var fillPct = Math.Max(_value, 2.0);
        var fillWidth = Math.Max(radius * 2, (int)Math.Round((Width - 1) * (fillPct / 100.0)));
        fillWidth = Math.Min(fillWidth, Width - 1);
        var fillBounds = new Rectangle(0, y, fillWidth, h);
        var color = _colorMode == ProgressColorMode.Accent
            ? p.Accent
            : Tokens.SeverityColor(_value, p);
        using (var path = Tokens.RoundedRect(fillBounds, radius))
        using (var brush = new SolidBrush(color))
            g.FillPath(brush, path);
    }
}

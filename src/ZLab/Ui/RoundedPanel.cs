using System.Drawing.Drawing2D;

namespace ZLab.Ui;

public enum BorderStyleKind
{
    Solid,
    Dashed,
    None
}

public class RoundedPanel : Panel
{
    private int _radius = Tokens.RadiusCard;
    private BorderStyleKind _borderKind = BorderStyleKind.Solid;
    private Color? _fillOverride;

    public RoundedPanel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw
            | ControlStyles.SupportsTransparentBackColor,
            true);
        UpdateColors();
        ThemeManager.ThemeChanged += OnThemeChanged;
    }

    public int Radius
    {
        get => _radius;
        set
        {
            _radius = value;
            Invalidate();
        }
    }

    public BorderStyleKind BorderStyleKind
    {
        get => _borderKind;
        set
        {
            _borderKind = value;
            Invalidate();
        }
    }

    public Color? FillColorOverride
    {
        get => _fillOverride;
        set
        {
            _fillOverride = value;
            Invalidate();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            ThemeManager.ThemeChanged -= OnThemeChanged;
        base.Dispose(disposing);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // painted in OnPaint
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var p = ThemeManager.Current;
        var parentBg = Parent?.BackColor ?? p.Bg;
        using (var brush = new SolidBrush(parentBg))
            g.FillRectangle(brush, ClientRectangle);

        var fill = _fillOverride ?? p.Surface;
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        var radius = LogicalToDeviceUnits(_radius);
        using var path = Tokens.RoundedRect(bounds, radius);
        using (var brush = new SolidBrush(fill))
            g.FillPath(brush, path);

        if (_borderKind != BorderStyleKind.None)
        {
            using var pen = new Pen(p.Stroke, 1f);
            if (_borderKind == BorderStyleKind.Dashed)
                pen.DashStyle = DashStyle.Dash;
            g.DrawPath(pen, path);
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        UpdateColors();
        Invalidate();
    }

    private void UpdateColors()
    {
        BackColor = ThemeManager.Current.Surface;
        ForeColor = ThemeManager.Current.Text;
    }
}

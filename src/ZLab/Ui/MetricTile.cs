using System.Drawing.Drawing2D;

namespace ZLab.Ui;

public enum MetricFooterKind
{
    None,
    Bar,
    Sparkline,
    Text
}

public sealed class MetricTile : RoundedPanel
{
    private string _glyph = "";
    private string _title = "";
    private IReadOnlyList<(string Number, string Unit)> _valueParts = Array.Empty<(string, string)>();
    private string _caption = "";
    private double? _percent;
    private MetricFooterKind _footerKind = MetricFooterKind.None;
    private string _footerText = "";
    private bool _isEmpty;
    private readonly ThinProgressBar _bar = new();
    private readonly Sparkline _spark = new();
    private readonly ToolTip _tip = new();

    public MetricTile()
    {
        Size = new Size(193, 118);
        Padding = new Padding(16, 14, 16, 14);
        Controls.Add(_bar);
        Controls.Add(_spark);
        _bar.Visible = false;
        _spark.Visible = false;
        _bar.ColorMode = ProgressColorMode.Severity;
        _bar.BarHeight = Tokens.BarHeightTile;
        ThemeManager.ThemeChanged += OnTheme;
    }

    public string Glyph
    {
        get => _glyph;
        set { _glyph = value ?? ""; Invalidate(); }
    }

    public string Title
    {
        get => _title;
        set { _title = value ?? ""; Invalidate(); }
    }

    public IReadOnlyList<(string Number, string Unit)> ValueParts
    {
        get => _valueParts;
        set { _valueParts = value ?? Array.Empty<(string, string)>(); Invalidate(); }
    }

    public string Caption
    {
        get => _caption;
        set { _caption = value ?? ""; Invalidate(); }
    }

    public double? Percent
    {
        get => _percent;
        set
        {
            _percent = value;
            if (value is double d)
                _bar.Value = d;
            Invalidate();
            LayoutFooter();
        }
    }

    public MetricFooterKind FooterKind
    {
        get => _footerKind;
        set { _footerKind = value; LayoutFooter(); Invalidate(); }
    }

    public string FooterText
    {
        get => _footerText;
        set { _footerText = value ?? ""; Invalidate(); }
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        set
        {
            _isEmpty = value;
            BorderStyleKind = value ? BorderStyleKind.Dashed : BorderStyleKind.Solid;
            FillColorOverride = value ? Color.Transparent : null;
            LayoutFooter();
            Invalidate();
        }
    }

    public string ToolTipText
    {
        get => "";
        set => _tip.SetToolTip(this, value);
    }

    public Sparkline Spark => _spark;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ThemeManager.ThemeChanged -= OnTheme;
            _tip.Dispose();
        }
        base.Dispose(disposing);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        LayoutFooter();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var p = ThemeManager.Current;
        var padX = LogicalToDeviceUnits(16);
        var padY = LogicalToDeviceUnits(14);
        var x = padX;
        var y = padY;

        using var captionFont = Tokens.Caption();
        using var iconFont = Tokens.Icon(12f);
        if (!string.IsNullOrEmpty(_glyph))
        {
            TextRenderer.DrawText(g, _glyph, iconFont, new Rectangle(x, y, LogicalToDeviceUnits(16), LogicalToDeviceUnits(16)), p.Text2,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            x += LogicalToDeviceUnits(24);
        }

        TextRenderer.DrawText(g, _title, captionFont, new Rectangle(x, y, Width - x - padX - LogicalToDeviceUnits(40), LogicalToDeviceUnits(16)), p.Text2,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

        if (_percent is double pct && !_isEmpty)
        {
            var pctText = ((int)Math.Round(pct)).ToString(System.Globalization.CultureInfo.GetCultureInfo("pt-BR")) + "%";
            using var strong = Tokens.Caption();
            var sz = TextRenderer.MeasureText(pctText, strong);
            TextRenderer.DrawText(g, pctText, strong,
                new Rectangle(Width - padX - sz.Width, y, sz.Width, LogicalToDeviceUnits(16)),
                p.Text,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        y += LogicalToDeviceUnits(8 + 16);
        x = padX;
        if (_isEmpty)
        {
            using var emptyFont = new Font(Tokens.UiFamily, 12.75f, FontStyle.Regular);
            var emptyText = _valueParts.Count > 0 ? _valueParts[0].Number : "Não instalado";
            TextRenderer.DrawText(g, emptyText, emptyFont,
                new Rectangle(x, y + LogicalToDeviceUnits(4), Width - padX * 2, LogicalToDeviceUnits(28)),
                p.Text3,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            return;
        }

        using var metricFont = Tokens.Metric();
        using var unitFont = Tokens.MetricUnit();
        foreach (var (number, unit) in _valueParts)
        {
            var numSize = TextRenderer.MeasureText(number, metricFont);
            TextRenderer.DrawText(g, number, metricFont,
                new Rectangle(x, y, numSize.Width, LogicalToDeviceUnits(28)),
                p.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            x += numSize.Width;
            if (!string.IsNullOrEmpty(unit))
            {
                x += LogicalToDeviceUnits(2);
                var unitSize = TextRenderer.MeasureText(unit, unitFont);
                TextRenderer.DrawText(g, unit, unitFont,
                    new Rectangle(x, y + LogicalToDeviceUnits(8), unitSize.Width, LogicalToDeviceUnits(18)),
                    p.Text3,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                x += unitSize.Width + LogicalToDeviceUnits(6);
            }
        }

        if (!string.IsNullOrEmpty(_caption))
        {
            y += LogicalToDeviceUnits(30);
            TextRenderer.DrawText(g, _caption, captionFont,
                new Rectangle(padX, y, Width - padX * 2, LogicalToDeviceUnits(16)),
                p.Text3,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        }

        if (_footerKind == MetricFooterKind.Text && !string.IsNullOrEmpty(_footerText))
        {
            var fy = Height - padY - LogicalToDeviceUnits(16);
            TextRenderer.DrawText(g, _footerText, captionFont,
                new Rectangle(padX, fy, Width - padX * 2, LogicalToDeviceUnits(16)),
                p.Text3,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }

    private void LayoutFooter()
    {
        var padX = LogicalToDeviceUnits(16);
        var padY = LogicalToDeviceUnits(14);
        var footY = Height - padY - LogicalToDeviceUnits(Tokens.BarHeightTile);
        _bar.Visible = !_isEmpty && _footerKind == MetricFooterKind.Bar;
        _spark.Visible = !_isEmpty && _footerKind == MetricFooterKind.Sparkline;
        if (_bar.Visible)
        {
            _bar.SetBounds(padX, footY, Width - padX * 2, LogicalToDeviceUnits(Tokens.BarHeightTile + 2));
            _bar.BringToFront();
        }

        if (_spark.Visible)
        {
            _spark.SetBounds(padX, Height - padY - LogicalToDeviceUnits(30), Width - padX * 2, LogicalToDeviceUnits(30));
            _spark.BringToFront();
        }
    }

    private void OnTheme(object? sender, EventArgs e) => Invalidate();
}

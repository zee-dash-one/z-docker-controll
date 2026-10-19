namespace ZLab.Ui;

public enum ZButtonVariant
{
    Primary,
    Secondary,
    Ghost,
    Danger
}

public sealed class ZButton : Control, IButtonControl
{
    private ZButtonVariant _variant = ZButtonVariant.Secondary;
    private string _glyph = "";
    private bool _iconOnly;
    private bool _compact;
    private bool _hover;
    private bool _pressed;
    private DialogResult _dialogResult = DialogResult.None;

    public ZButton()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw
            | ControlStyles.Selectable
            | ControlStyles.SupportsTransparentBackColor,
            true);
        Cursor = Cursors.Hand;
        TabStop = true;
        Font = Tokens.Body();
        Height = LogicalToDeviceUnits(32);
        ThemeManager.ThemeChanged += OnTheme;
    }

    public ZButtonVariant Variant
    {
        get => _variant;
        set { _variant = value; Invalidate(); }
    }

    public string Glyph
    {
        get => _glyph;
        set { _glyph = value ?? ""; Invalidate(); Recalc(); }
    }

    public bool IconOnly
    {
        get => _iconOnly;
        set { _iconOnly = value; Recalc(); Invalidate(); }
    }

    public bool Compact
    {
        get => _compact;
        set
        {
            _compact = value;
            Height = LogicalToDeviceUnits(value ? 28 : 32);
            Invalidate();
        }
    }

    public DialogResult DialogResult
    {
        get => _dialogResult;
        set => _dialogResult = value;
    }

    public void NotifyDefault(bool value) { }

    public void PerformClick()
    {
        if (CanSelect)
            OnClick(EventArgs.Empty);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            ThemeManager.ThemeChanged -= OnTheme;
        base.Dispose(disposing);
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        Recalc();
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); } base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Space or Keys.Enter)
        {
            PerformClick();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var p = ThemeManager.Current;
        var (bg, fg, border) = ResolveColors(p);
        if (_pressed)
            bg = Tokens.Darken(bg, 0.06f);
        if (!Enabled)
        {
            bg = Tokens.Mix(bg, p.Bg, 0.38f);
            fg = Tokens.Mix(fg, p.Bg, 0.38f);
            border = Tokens.Mix(border, p.Bg, 0.38f);
        }

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        var radius = LogicalToDeviceUnits(Tokens.RadiusButton);
        using (var path = Tokens.RoundedRect(bounds, radius))
        {
            using (var brush = new SolidBrush(bg))
                g.FillPath(brush, path);
            if (_variant != ZButtonVariant.Ghost || _hover)
            {
                using var pen = new Pen(border);
                g.DrawPath(pen, path);
            }
        }

        if (Focused && ShowFocusCues)
        {
            using var focusPen = new Pen(p.Accent, 2f);
            var focus = Rectangle.Inflate(bounds, -2, -2);
            using var path = Tokens.RoundedRect(focus, Math.Max(1, radius - 2));
            g.DrawPath(focusPen, path);
        }

        var pad = LogicalToDeviceUnits(12);
        var gap = LogicalToDeviceUnits(8);
        var hasGlyph = !string.IsNullOrEmpty(_glyph);
        var hasText = !_iconOnly && !string.IsNullOrEmpty(Text);
        using var iconFont = Tokens.Icon(11f);
        using var textFont = Tokens.Body();

        var iconSize = hasGlyph ? TextRenderer.MeasureText(_glyph, iconFont).Width : 0;
        var textSize = hasText ? TextRenderer.MeasureText(Text, textFont).Width : 0;
        var total = iconSize + (hasGlyph && hasText ? gap : 0) + textSize;
        var x = Math.Max(pad, (Width - total) / 2);
        var y = 0;
        var h = Height;

        if (hasGlyph)
        {
            TextRenderer.DrawText(
                g, _glyph, iconFont,
                new Rectangle(x, y, iconSize, h),
                fg,
                TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
            x += iconSize + (hasText ? gap : 0);
        }

        if (hasText)
        {
            TextRenderer.DrawText(
                g, Text, textFont,
                new Rectangle(x, y, textSize + 4, h),
                fg,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        }
    }

    private (Color Bg, Color Fg, Color Border) ResolveColors(Palette p)
    {
        return _variant switch
        {
            ZButtonVariant.Primary => (
                _hover ? p.AccentHover : p.Accent,
                p.OnAccent,
                _hover ? p.AccentHover : p.Accent),
            ZButtonVariant.Danger => (
                _hover ? p.Surface3 : p.Surface2,
                p.Bad,
                p.Stroke2),
            ZButtonVariant.Ghost => (
                _hover ? p.Surface3 : Color.Transparent,
                p.Text2,
                Color.Transparent),
            _ => (
                _hover ? p.Surface3 : p.Surface2,
                p.Text,
                p.Stroke2)
        };
    }

    private void Recalc()
    {
        if (_iconOnly)
        {
            Width = Height = LogicalToDeviceUnits(32);
            return;
        }

        using var iconFont = Tokens.Icon(11f);
        using var textFont = Tokens.Body();
        var w = LogicalToDeviceUnits(24);
        if (!string.IsNullOrEmpty(_glyph))
            w += TextRenderer.MeasureText(_glyph, iconFont).Width + LogicalToDeviceUnits(8);
        if (!string.IsNullOrEmpty(Text))
            w += TextRenderer.MeasureText(Text, textFont).Width;
        Width = Math.Max(LogicalToDeviceUnits(32), w);
    }

    private void OnTheme(object? sender, EventArgs e) => Invalidate();
}

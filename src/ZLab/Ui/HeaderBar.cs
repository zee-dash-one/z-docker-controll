namespace ZLab.Ui;

public sealed class HeaderBar : Panel
{
    private readonly Label _brandTitle = new();
    private readonly Label _brandSub = new();
    private readonly Panel _chip = new();
    private readonly Label _chipText = new();
    private readonly Panel _dot = new();
    private bool _connected;
    private string _userHost = "";

    public ZButton EnterButton { get; } = new() { Text = "Entrar", Glyph = Glyphs.Unlock, Variant = ZButtonVariant.Primary };
    public ZButton EndSessionButton { get; } = new() { Text = "Encerrar sessão", Glyph = Glyphs.Lock, Variant = ZButtonVariant.Secondary };
    public ZButton ThemeButton { get; } = new() { Glyph = Glyphs.Brightness, IconOnly = true, Variant = ZButtonVariant.Ghost };
    public ZButton CloseButton { get; } = new() { Text = "Fechar Z-Lab", Glyph = Glyphs.Cancel, Variant = ZButtonVariant.Ghost };

    public HeaderBar()
    {
        Height = LogicalToDeviceUnits(60);
        Padding = new Padding(LogicalToDeviceUnits(24), 0, LogicalToDeviceUnits(24), 0);
        DoubleBuffered = true;

        _brandTitle.Text = "Z-Lab";
        _brandTitle.Font = Tokens.Title();
        _brandTitle.AutoSize = true;
        _brandSub.Text = "Painel do z-server-01";
        _brandSub.Font = Tokens.Caption();
        _brandSub.AutoSize = true;

        _chip.Height = LogicalToDeviceUnits(28);
        _chip.Padding = new Padding(LogicalToDeviceUnits(12), 0, LogicalToDeviceUnits(12), 0);
        _dot.Size = new Size(LogicalToDeviceUnits(8), LogicalToDeviceUnits(8));
        _chipText.AutoSize = true;
        _chipText.Font = Tokens.Caption();
        _chip.Controls.Add(_dot);
        _chip.Controls.Add(_chipText);

        new ToolTip().SetToolTip(ThemeButton, "Tema claro/escuro");
        new ToolTip().SetToolTip(CloseButton, "Fecha o app inteiro, inclusive o ícone da bandeja");

        Controls.Add(_brandTitle);
        Controls.Add(_brandSub);
        Controls.Add(_chip);
        Controls.Add(EnterButton);
        Controls.Add(EndSessionButton);
        Controls.Add(ThemeButton);
        Controls.Add(CloseButton);

        ThemeManager.ThemeChanged += OnTheme;
        ApplyColors();
        SetSession(false, "");
    }

    public void SetSession(bool connected, string userHost)
    {
        _connected = connected;
        _userHost = userHost ?? "";
        _chipText.Text = connected
            ? _userHost + " · Sessão aberta"
            : "Sem sessão";
        ApplyColors();
        PerformLayout();
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            ThemeManager.ThemeChanged -= OnTheme;
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var p = ThemeManager.Current;
        using (var brush = new SolidBrush(p.Surface))
            g.FillRectangle(brush, ClientRectangle);
        using (var pen = new Pen(p.Stroke))
            g.DrawLine(pen, 0, Height - 1, Width, Height - 1);

        var mark = LogicalToDeviceUnits(32);
        var markRect = new Rectangle(LogicalToDeviceUnits(24), (Height - mark) / 2, mark, mark);
        using (var path = Tokens.RoundedRect(markRect, LogicalToDeviceUnits(8)))
        using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                   markRect, p.Accent, Color.FromArgb(0x8B, 0x5C, 0xF6), 135f))
            g.FillPath(brush, path);
        using var zFont = new Font(Tokens.UiFamily, 12f, FontStyle.Bold);
        TextRenderer.DrawText(g, "Z", zFont, markRect, Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        // chip background
        var chipBounds = new Rectangle(_chip.Left, _chip.Top, _chip.Width - 1, _chip.Height - 1);
        using (var path = Tokens.RoundedRect(chipBounds, _chip.Height / 2))
        using (var brush = new SolidBrush(p.Surface2))
        using (var pen = new Pen(p.Stroke))
        {
            g.FillPath(brush, path);
            g.DrawPath(pen, path);
        }

        // divider between session buttons and theme
        var divX = ThemeButton.Left - LogicalToDeviceUnits(8);
        var divH = LogicalToDeviceUnits(28);
        var divY = (Height - divH) / 2;
        using (var pen = new Pen(p.Stroke))
            g.DrawLine(pen, divX, divY, divX, divY + divH);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        var pad = LogicalToDeviceUnits(24);
        var mark = LogicalToDeviceUnits(32);
        var brandX = pad + mark + LogicalToDeviceUnits(10);
        _brandTitle.Location = new Point(brandX, LogicalToDeviceUnits(12));
        _brandSub.Location = new Point(brandX, _brandTitle.Bottom - 2);

        var chipW = TextRenderer.MeasureText(_chipText.Text, _chipText.Font).Width
                    + LogicalToDeviceUnits(12 + 8 + 12 + 12);
        _chip.Size = new Size(chipW, LogicalToDeviceUnits(28));
        _chip.Location = new Point(_brandSub.Right + LogicalToDeviceUnits(16), (Height - _chip.Height) / 2);
        _dot.Location = new Point(LogicalToDeviceUnits(12), (_chip.Height - _dot.Height) / 2);
        _chipText.Location = new Point(_dot.Right + LogicalToDeviceUnits(8), (_chip.Height - _chipText.PreferredHeight) / 2);

        var x = Width - pad;
        CloseButton.Location = new Point(x - CloseButton.Width, (Height - CloseButton.Height) / 2);
        x = CloseButton.Left - LogicalToDeviceUnits(8);
        ThemeButton.Location = new Point(x - ThemeButton.Width, (Height - ThemeButton.Height) / 2);
        // vertical divider between session group and theme
        x = ThemeButton.Left - LogicalToDeviceUnits(16);
        EndSessionButton.Location = new Point(x - EndSessionButton.Width, (Height - EndSessionButton.Height) / 2);
        x = EndSessionButton.Left - LogicalToDeviceUnits(8);
        EnterButton.Location = new Point(x - EnterButton.Width, (Height - EnterButton.Height) / 2);
        Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // painted in OnPaint
    }

    private void OnTheme(object? sender, EventArgs e)
    {
        ApplyColors();
        ThemeButton.Glyph = ThemeManager.Current == Palette.Dark ? Glyphs.Brightness : Glyphs.QuietHours;
        Invalidate();
    }

    private void ApplyColors()
    {
        var p = ThemeManager.Current;
        BackColor = p.Surface;
        _brandTitle.ForeColor = p.Text;
        _brandTitle.BackColor = p.Surface;
        _brandSub.ForeColor = p.Text3;
        _brandSub.BackColor = p.Surface;
        _chip.BackColor = Color.Transparent;
        _chipText.ForeColor = p.Text2;
        _chipText.BackColor = Color.Transparent;
        _dot.BackColor = _connected ? p.Ok : p.Muted;
    }
}

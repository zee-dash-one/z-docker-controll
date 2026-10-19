namespace ZLab.Ui;

public sealed class EmptyStatePanel : Panel
{
    private readonly RoundedPanel _card = new() { Radius = 16 };
    private readonly Panel _iconHost = new();
    private readonly Label _title = new();
    private readonly Label _body = new();
    private readonly Label _hint = new();
    private readonly Panel _dot = new();

    public ZButton WakeButton { get; } = new()
    {
        Text = "Ligar servidor",
        Glyph = Glyphs.Power,
        Compact = true,
        Variant = ZButtonVariant.Secondary
    };

    public EmptyStatePanel()
    {
        DoubleBuffered = true;
        _card.Size = new Size(LogicalToDeviceUnits(520), LogicalToDeviceUnits(280));

        _iconHost.Size = new Size(LogicalToDeviceUnits(64), LogicalToDeviceUnits(64));
        _iconHost.Paint += DrawIcon;

        _title.Text = "Nenhuma sessão aberta";
        _title.Font = new Font(Tokens.UiFamily, 16.5f, FontStyle.Bold);
        _title.AutoSize = true;
        _body.Text =
            "Clique em Entrar (no alto, à direita) para conectar ao z-server-01. O painel do servidor e os serviços aparecem depois do login.";
        _body.Font = Tokens.Body();
        _body.AutoSize = true;
        _hint.Font = Tokens.Caption();
        _hint.AutoSize = true;
        _dot.Size = new Size(LogicalToDeviceUnits(8), LogicalToDeviceUnits(8));

        _card.Controls.Add(_iconHost);
        _card.Controls.Add(_title);
        _card.Controls.Add(_body);
        _card.Controls.Add(_dot);
        _card.Controls.Add(_hint);
        _card.Controls.Add(WakeButton);
        _card.Paint += DrawDivider;
        Controls.Add(_card);

        ThemeManager.ThemeChanged += OnTheme;
        ApplyColors();
        SetServerResponds(false);
    }

    public void SetServerResponds(bool responds)
    {
        _hint.Text = responds ? "z-server-01 responde" : "z-server-01 não responde";
        WakeButton.Visible = !responds;
        ApplyColors();
        PerformLayout();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            ThemeManager.ThemeChanged -= OnTheme;
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        using var brush = new SolidBrush(ThemeManager.Current.Bg);
        e.Graphics.FillRectangle(brush, ClientRectangle);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        _card.Location = new Point(
            Math.Max(0, (Width - _card.Width) / 2),
            Math.Max(0, (Height - _card.Height) / 2));

        var pad = LogicalToDeviceUnits(40);
        _iconHost.Location = new Point((_card.Width - _iconHost.Width) / 2, pad);
        _title.Location = new Point(pad, _iconHost.Bottom + LogicalToDeviceUnits(16));
        _title.MaximumSize = new Size(_card.Width - pad * 2, 0);
        _body.MaximumSize = new Size(_card.Width - pad * 2, 0);
        _body.Location = new Point(pad, _title.Bottom + LogicalToDeviceUnits(8));

        var footY = _card.Height - pad - WakeButton.Height;
        _dot.Location = new Point(pad, footY + (WakeButton.Height - _dot.Height) / 2);
        _hint.Location = new Point(
            _dot.Right + LogicalToDeviceUnits(10),
            footY + (WakeButton.Height - _hint.PreferredHeight) / 2);
        WakeButton.Location = new Point(_card.Width - pad - WakeButton.Width, footY);
    }

    private void DrawIcon(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var p = ThemeManager.Current;
        var rect = new Rectangle(0, 0, _iconHost.Width - 1, _iconHost.Height - 1);
        using (var path = Tokens.RoundedRect(rect, LogicalToDeviceUnits(16)))
        using (var brush = new SolidBrush(p.AccentSoft))
            g.FillPath(brush, path);
        using var font = Tokens.Icon(28f);
        TextRenderer.DrawText(
            g, Glyphs.Network, font, rect, p.Accent,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }

    private void DrawDivider(object? sender, PaintEventArgs e)
    {
        var p = ThemeManager.Current;
        var pad = LogicalToDeviceUnits(40);
        var y = _hint.Top - LogicalToDeviceUnits(16);
        using var pen = new Pen(p.Stroke);
        e.Graphics.DrawLine(pen, pad, y, _card.Width - pad, y);
    }

    private void OnTheme(object? sender, EventArgs e) => ApplyColors();

    private void ApplyColors()
    {
        var p = ThemeManager.Current;
        BackColor = p.Bg;
        _iconHost.BackColor = p.Surface;
        _title.ForeColor = p.Text;
        _title.BackColor = p.Surface;
        _body.ForeColor = p.Text2;
        _body.BackColor = p.Surface;
        _hint.ForeColor = p.Text3;
        _hint.BackColor = p.Surface;
        _dot.BackColor = WakeButton.Visible ? p.Muted : p.Ok;
        Invalidate(true);
    }
}

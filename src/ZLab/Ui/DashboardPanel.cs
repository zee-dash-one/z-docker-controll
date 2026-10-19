namespace ZLab.Ui;

/// <summary>
/// Shell for the connected view. Metrics (todo 03) and service cards (todo 04) fill the placeholders.
/// </summary>
public sealed class DashboardPanel : Panel
{
    private readonly SectionHeader _serverHeader = new();
    private readonly Panel _serverBody = new();
    private readonly SectionHeader _servicesHeader = new();
    private readonly Panel _servicesBody = new();
    private readonly Label _serverPlaceholder = new();
    private readonly Label _servicesPlaceholder = new();

    public ZButton WakeButton { get; } = new()
    {
        Text = "Ligar",
        Glyph = Glyphs.Power,
        Compact = true,
        Variant = ZButtonVariant.Secondary
    };

    public ZButton RebootButton { get; } = new()
    {
        Text = "Reiniciar",
        Glyph = Glyphs.Refresh,
        Compact = true,
        Variant = ZButtonVariant.Secondary
    };

    public ZButton ShutdownButton { get; } = new()
    {
        Text = "Desligar",
        Glyph = Glyphs.Power,
        Compact = true,
        Variant = ZButtonVariant.Danger
    };

    public DashboardPanel()
    {
        AutoScroll = true;
        Padding = new Padding(
            LogicalToDeviceUnits(24),
            LogicalToDeviceUnits(20),
            LogicalToDeviceUnits(24),
            0);

        _serverHeader.Title = "Servidor";
        _serverHeader.Subtitle = "z-server-01 · Ubuntu 26.04 · Intel i3-4170, 4 núcleos";
        _serverHeader.Actions.Add(WakeButton);
        _serverHeader.Actions.Add(RebootButton);
        _serverHeader.Actions.Add(ShutdownButton);
        _serverHeader.Dock = DockStyle.Top;
        _serverHeader.Height = LogicalToDeviceUnits(40);

        _serverPlaceholder.Text = "Blocos de métricas entram na tarefa 03.";
        _serverPlaceholder.Font = Tokens.Caption();
        _serverPlaceholder.AutoSize = true;
        _serverBody.Dock = DockStyle.Top;
        _serverBody.Height = LogicalToDeviceUnits(72);
        _serverBody.Padding = new Padding(0, LogicalToDeviceUnits(8), 0, LogicalToDeviceUnits(16));
        _serverBody.Controls.Add(_serverPlaceholder);

        _servicesHeader.Title = "Serviços";
        _servicesHeader.Subtitle = "Compose em /opt/zlab";
        _servicesHeader.Dock = DockStyle.Top;
        _servicesHeader.Height = LogicalToDeviceUnits(40);

        _servicesPlaceholder.Text = "Cards de serviço entram na tarefa 04.";
        _servicesPlaceholder.Font = Tokens.Caption();
        _servicesPlaceholder.AutoSize = true;
        _servicesBody.Dock = DockStyle.Fill;
        _servicesBody.Padding = new Padding(0, LogicalToDeviceUnits(8), 0, 0);
        _servicesBody.Controls.Add(_servicesPlaceholder);

        // Dock order: Fill first, then Top (last Top is highest).
        Controls.Add(_servicesBody);
        Controls.Add(_servicesHeader);
        Controls.Add(_serverBody);
        Controls.Add(_serverHeader);

        ThemeManager.ThemeChanged += OnTheme;
        ApplyColors();
    }

    public void SetEnergyVisible(bool visible)
    {
        WakeButton.Visible = visible;
        RebootButton.Visible = visible;
        ShutdownButton.Visible = visible;
        _serverHeader.PerformLayout();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            ThemeManager.ThemeChanged -= OnTheme;
        base.Dispose(disposing);
    }

    private void OnTheme(object? sender, EventArgs e) => ApplyColors();

    private void ApplyColors()
    {
        var p = ThemeManager.Current;
        BackColor = p.Bg;
        _serverBody.BackColor = p.Bg;
        _servicesBody.BackColor = p.Bg;
        _serverPlaceholder.ForeColor = p.Text3;
        _serverPlaceholder.BackColor = p.Bg;
        _servicesPlaceholder.ForeColor = p.Text3;
        _servicesPlaceholder.BackColor = p.Bg;
    }
}

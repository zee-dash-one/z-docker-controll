using ZLab.Services;

namespace ZLab.Ui;

/// <summary>
/// Connected view: server metric tiles + services placeholder (cards in todo 04).
/// </summary>
public sealed class DashboardPanel : Panel
{
    private readonly SectionHeader _serverHeader = new();
    private readonly MetricsRow _metrics = new();
    private readonly SectionHeader _servicesHeader = new();
    private readonly Panel _servicesBody = new();
    private readonly Label _servicesPlaceholder = new();

    public MetricTile CpuTile => _metrics.Cpu;
    public MetricTile MemoryTile => _metrics.Memory;
    public MetricTile DiskRootTile => _metrics.DiskRoot;
    public MetricTile DiskDockerTile => _metrics.DiskDocker;
    public MetricTile DataTile => _metrics.Data;
    public MetricTile UptimeTile => _metrics.Uptime;

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

        _metrics.Dock = DockStyle.Top;
        _metrics.Padding = new Padding(0, LogicalToDeviceUnits(8), 0, LogicalToDeviceUnits(24));

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

        Controls.Add(_servicesBody);
        Controls.Add(_servicesHeader);
        Controls.Add(_metrics);
        Controls.Add(_serverHeader);

        ThemeManager.ThemeChanged += OnTheme;
        ApplyColors();
        ApplyTiles(ServerTilesPresenter.Empty(), pushSpark: false);
    }

    public void SetEnergyVisible(bool visible)
    {
        WakeButton.Visible = visible;
        RebootButton.Visible = visible;
        ShutdownButton.Visible = visible;
        _serverHeader.PerformLayout();
    }

    public void ApplyTiles(ServerTilesView view, bool pushSpark)
    {
        ApplyTile(CpuTile, view.Cpu, Glyphs.Cpu, "CPU", MetricFooterKind.Sparkline, pushSpark);
        ApplyTile(MemoryTile, view.Memory, Glyphs.Ram, "Memória", MetricFooterKind.Bar, false);
        ApplyTile(DiskRootTile, view.DiskRoot, Glyphs.HardDrive, "Disco do sistema", MetricFooterKind.Bar, false);
        ApplyTile(DiskDockerTile, view.DiskDocker, Glyphs.HardDrive, "Disco Docker", MetricFooterKind.Bar, false);
        ApplyTile(DataTile, view.Data, Glyphs.HardDrive, "Dados /data", MetricFooterKind.None, false);
        ApplyTile(UptimeTile, view.Uptime, Glyphs.Clock, "Ligado há", MetricFooterKind.Text, false);
        if (!string.IsNullOrEmpty(view.Uptime.FooterText))
            UptimeTile.FooterText = Glyphs.People + "  " + view.Uptime.FooterText;
    }

    public void ClearMetrics()
    {
        CpuTile.Spark.Clear();
        ApplyTiles(ServerTilesPresenter.Empty(), pushSpark: false);
    }

    private static void ApplyTile(
        MetricTile tile,
        TileView view,
        string glyph,
        string title,
        MetricFooterKind footer,
        bool pushSpark)
    {
        tile.Glyph = glyph;
        tile.Title = title;
        tile.IsEmpty = view.IsEmpty;
        tile.ValueParts = view.ValueParts;
        tile.Caption = view.Caption;
        tile.Percent = view.Percent;
        tile.FooterKind = view.IsEmpty ? MetricFooterKind.None : footer;
        tile.FooterText = view.FooterText ?? "";
        tile.ToolTipText = view.ToolTip;
        if (pushSpark && view.SparkSample is double sample)
            tile.Spark.Push(sample);
        else if (view.SparkSample is null && footer == MetricFooterKind.Sparkline && view.ValueParts.Count == 1
                 && view.ValueParts[0].Number == "—")
        {
            // keep existing spark samples until clear
        }
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
        _servicesBody.BackColor = p.Bg;
        _servicesPlaceholder.ForeColor = p.Text3;
        _servicesPlaceholder.BackColor = p.Bg;
    }
}

/// <summary>
/// Six metric tiles: 6 columns when width ≥ 1100, else 3×2. Gap 12, height 118.
/// </summary>
public sealed class MetricsRow : Panel
{
    private const int Gap = 12;
    private const int TileH = 118;
    private const int BreakWidth = 1100;

    public MetricTile Cpu { get; } = new();
    public MetricTile Memory { get; } = new();
    public MetricTile DiskRoot { get; } = new();
    public MetricTile DiskDocker { get; } = new();
    public MetricTile Data { get; } = new();
    public MetricTile Uptime { get; } = new();

    public MetricsRow()
    {
        DoubleBuffered = true;
        foreach (var tile in All())
            Controls.Add(tile);
        Cpu.Spark.MaxMode = SparkMaxMode.Fixed100;
    }

    private MetricTile[] All() => new[] { Cpu, Memory, DiskRoot, DiskDocker, Data, Uptime };

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        var pad = Padding;
        var innerW = Math.Max(0, Width - pad.Horizontal);
        var gap = LogicalToDeviceUnits(Gap);
        var tileH = LogicalToDeviceUnits(TileH);
        var cols = innerW >= LogicalToDeviceUnits(BreakWidth) ? 6 : 3;
        var rows = cols == 6 ? 1 : 2;
        var tileW = (innerW - gap * (cols - 1)) / cols;
        if (tileW < 1)
            tileW = 1;

        var tiles = All();
        for (var i = 0; i < tiles.Length; i++)
        {
            var col = i % cols;
            var row = i / cols;
            tiles[i].SetBounds(
                pad.Left + col * (tileW + gap),
                pad.Top + row * (tileH + gap),
                tileW,
                tileH);
        }

        Height = pad.Vertical + rows * tileH + (rows - 1) * gap;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        PerformLayout();
    }
}

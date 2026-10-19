using System.Globalization;
using ZLab.Models;
using ZLab.Services;

namespace ZLab.Ui;

/// <summary>
/// Connected view: server metric tiles + service cards.
/// </summary>
public sealed class DashboardPanel : Panel
{
    private readonly SectionHeader _serverHeader = new();
    private readonly MetricsRow _metrics = new();
    private readonly SectionHeader _servicesHeader = new();
    private readonly Label _updatedLabel = new();
    private readonly ZButton _refresh = new()
    {
        Text = "Atualizar",
        Glyph = Glyphs.Sync,
        Compact = true,
        Variant = ZButtonVariant.Ghost
    };
    private readonly ResponsiveGrid _grid = new()
    {
        MinItemWidth = 360,
        MaxColumns = 3,
        ItemHeight = 184,
        Gap = 12,
        Dock = DockStyle.Top
    };
    private readonly Dictionary<string, ServiceCard> _cards = new(StringComparer.Ordinal);
    private DateTime _lastStatsLocal = DateTime.MinValue;
    private readonly System.Windows.Forms.Timer _ageTimer = new() { Interval = 1000 };
    private bool _hasReading;

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

    public event EventHandler? RefreshRequested;
    public event EventHandler<RemoteService>? StartRequested;
    public event EventHandler<RemoteService>? StopRequested;
    public event EventHandler<RemoteService>? LogsRequested;
    public event EventHandler<ServiceCard>? AccessCodeRequested;
    public event EventHandler<RemoteService>? CreateRequested;
    public event EventHandler<RemoteService>? UpdateRequested;
    public event EventHandler<RemoteService>? UsageRequested;
    public event EventHandler<string>? CardMessage;

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
        _servicesHeader.Subtitle = "6 serviços";
        _updatedLabel.AutoSize = true;
        _updatedLabel.Font = Tokens.Caption();
        _servicesHeader.Actions.Add(_updatedLabel);
        _servicesHeader.Actions.Add(_refresh);
        _servicesHeader.Dock = DockStyle.Top;
        _servicesHeader.Height = LogicalToDeviceUnits(40);
        _refresh.Click += (_, _) => RefreshRequested?.Invoke(this, EventArgs.Empty);

        foreach (var service in RemoteCommands.Services)
        {
            var card = new ServiceCard();
            card.Bind(service);
            WireCard(card);
            _cards[service.Id] = card;
            _grid.Controls.Add(card);
        }

        Controls.Add(_grid);
        Controls.Add(_servicesHeader);
        Controls.Add(_metrics);
        Controls.Add(_serverHeader);

        ThemeManager.ThemeChanged += OnTheme;
        _ageTimer.Tick += (_, _) => UpdateAgeLabel();
        _ageTimer.Start();
        ApplyColors();
        ApplyTiles(ServerTilesPresenter.Empty(), pushSpark: false);
        SetServicesSummary(0, 0);
        UpdateAgeLabel();
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

    public void ApplyStats(StatsSnapshot snapshot, DateTimeOffset nowUtc)
    {
        _hasReading = true;
        _lastStatsLocal = DateTime.Now;
        var cores = snapshot.Host?.Cores > 0 ? snapshot.Host.Cores : 4;
        var byName = snapshot.Containers.ToDictionary(c => c.Name, StringComparer.Ordinal);

        var up = 0;
        var down = 0;
        foreach (var service in RemoteCommands.Services)
        {
            byName.TryGetValue(service.Id, out var c);
            var view = ServiceCardPresenter.From(service, c, cores, nowUtc, hasReading: true);
            if (_cards.TryGetValue(service.Id, out var card))
                card.Apply(view);

            var kind = ServiceCardPresenter.ToStatusKind(c, true);
            if (kind == ContainerStatusKind.Up)
                up++;
            else
                down++;
        }

        SetServicesSummary(up, down);
        UpdateAgeLabel();
    }

    public void ClearMetrics()
    {
        CpuTile.Spark.Clear();
        ApplyTiles(ServerTilesPresenter.Empty(), pushSpark: false);
        _hasReading = false;
        foreach (var service in RemoteCommands.Services)
        {
            if (_cards.TryGetValue(service.Id, out var card))
                card.Apply(ServiceCardPresenter.From(service, null, 4, DateTimeOffset.UtcNow, false));
        }

        SetServicesSummary(0, 0);
        UpdateAgeLabel();
    }

    public IReadOnlyDictionary<string, ContainerStatusKind> LastStatusMap(StatsSnapshot? snapshot)
    {
        var map = new Dictionary<string, ContainerStatusKind>(StringComparer.Ordinal);
        var byName = snapshot?.Containers.ToDictionary(c => c.Name, StringComparer.Ordinal)
                     ?? new Dictionary<string, ContainerStats>(StringComparer.Ordinal);
        foreach (var service in RemoteCommands.Services)
        {
            byName.TryGetValue(service.Id, out var c);
            map[service.Id] = ServiceCardPresenter.ToStatusKind(c, _hasReading);
        }

        return map;
    }

    private void WireCard(ServiceCard card)
    {
        card.StartClicked += (_, _) =>
        {
            if (card.Service is not null)
                StartRequested?.Invoke(this, card.Service);
        };
        card.StopClicked += (_, _) =>
        {
            if (card.Service is not null)
                StopRequested?.Invoke(this, card.Service);
        };
        card.LogsClicked += (_, _) =>
        {
            if (card.Service is not null)
                LogsRequested?.Invoke(this, card.Service);
        };
        card.AccessCodeClicked += (_, _) => AccessCodeRequested?.Invoke(this, card);
        card.CreateClicked += (_, _) =>
        {
            if (card.Service is not null)
                CreateRequested?.Invoke(this, card.Service);
        };
        card.UpdateClicked += (_, _) =>
        {
            if (card.Service is not null)
                UpdateRequested?.Invoke(this, card.Service);
        };
        card.UsageClicked += (_, _) =>
        {
            if (card.Service is not null)
                UsageRequested?.Invoke(this, card.Service);
        };
        card.OpenFailed += (_, msg) => CardMessage?.Invoke(this, msg);
    }

    private void SetServicesSummary(int up, int down)
    {
        var total = RemoteCommands.Services.Count;
        // "6 serviços · 5 no ar · 1 parado" — "no ar" highlighted via plain text (WinForms Label).
        _servicesHeader.Subtitle = total + " serviços · " + up + " no ar · " + down + " parado";
        _servicesHeader.PerformLayout();
    }

    private void UpdateAgeLabel()
    {
        if (_lastStatsLocal == DateTime.MinValue || !_hasReading)
        {
            _updatedLabel.Text = "—";
            return;
        }

        var sec = Math.Max(0, (int)(DateTime.Now - _lastStatsLocal).TotalSeconds);
        _updatedLabel.Text = "atualizado há " + sec.ToString(CultureInfo.GetCultureInfo("pt-BR")) + " s";
        _servicesHeader.PerformLayout();
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
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ThemeManager.ThemeChanged -= OnTheme;
            _ageTimer.Stop();
            _ageTimer.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnTheme(object? sender, EventArgs e) => ApplyColors();

    private void ApplyColors()
    {
        var p = ThemeManager.Current;
        BackColor = p.Bg;
        _updatedLabel.ForeColor = p.Text3;
        _updatedLabel.BackColor = p.Bg;
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

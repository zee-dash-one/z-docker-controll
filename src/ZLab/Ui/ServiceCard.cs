using System.Diagnostics;
using ZLab.Services;

namespace ZLab.Ui;

public sealed class ServiceCard : RoundedPanel
{
    private readonly StatusPill _pill = new();
    private readonly ThinProgressBar _cpuBar = new() { BarHeight = Tokens.BarHeightCard, ColorMode = ProgressColorMode.Severity };
    private readonly ThinProgressBar _memBar = new() { BarHeight = Tokens.BarHeightCard, ColorMode = ProgressColorMode.Severity };
    private readonly ZButton _playStop = new() { IconOnly = true, Variant = ZButtonVariant.Ghost };
    private readonly ZButton _logs = new() { IconOnly = true, Glyph = Glyphs.Document, Variant = ZButtonVariant.Ghost };
    private readonly ZButton _open = new() { IconOnly = true, Glyph = Glyphs.OpenInNewWindow, Variant = ZButtonVariant.Ghost };
    private readonly ZButton _more = new() { IconOnly = true, Glyph = Glyphs.More, Variant = ZButtonVariant.Ghost };
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _menuCreate = new("Criar");
    private readonly ToolStripMenuItem _menuUpdate = new("Atualizar");
    private readonly ToolStripMenuItem _menuUsage = new("Uso");
    private readonly ToolTip _tips = new();

    private RemoteService? _service;
    private ServiceCardView _view = ServiceCardPresenter.From(
        new RemoteService("x", "x", false, null, null), null, 4, DateTimeOffset.UtcNow, false);
    private string _glyph = Glyphs.Network;
    private bool _addressHover;

    public ServiceCard()
    {
        Radius = Tokens.RadiusCard;
        Padding = new Padding(
            LogicalToDeviceUnits(16),
            LogicalToDeviceUnits(14),
            LogicalToDeviceUnits(16),
            LogicalToDeviceUnits(10));

        _menu = ThemedContextMenu.Create();
        _menu.Items.AddRange(new ToolStripItem[] { _menuCreate, _menuUpdate, _menuUsage });

        Controls.Add(_pill);
        Controls.Add(_cpuBar);
        Controls.Add(_memBar);
        Controls.Add(_playStop);
        Controls.Add(_logs);
        Controls.Add(_open);
        Controls.Add(_more);

        _tips.SetToolTip(_logs, "Logs");
        _tips.SetToolTip(_open, "Abrir");
        _tips.SetToolTip(_more, "Mais ações");

        _playStop.Click += (_, _) =>
        {
            if (_view.CanStop)
                StopClicked?.Invoke(this, EventArgs.Empty);
            else if (_view.CanStart)
                StartClicked?.Invoke(this, EventArgs.Empty);
        };
        _logs.Click += (_, _) => LogsClicked?.Invoke(this, EventArgs.Empty);
        _open.Click += (_, _) =>
        {
            if (_view.OpenUrl is string url)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    OpenFailed?.Invoke(this, ex.Message);
                }
            }
        };
        _more.Click += (_, _) => _menu.Show(_more, new Point(0, _more.Height));
        _menuCreate.Click += (_, _) => CreateClicked?.Invoke(this, EventArgs.Empty);
        _menuUpdate.Click += (_, _) => UpdateClicked?.Invoke(this, EventArgs.Empty);
        _menuUsage.Click += (_, _) => UsageClicked?.Invoke(this, EventArgs.Empty);

        MouseMove += OnMouseMoveAddress;
        MouseLeave += (_, _) => { _addressHover = false; Cursor = Cursors.Default; Invalidate(); };
        MouseClick += OnAddressClick;

        ThemeManager.ThemeChanged += OnTheme;
    }

    public RemoteService? Service => _service;

    public event EventHandler? StartClicked;
    public event EventHandler? StopClicked;
    public event EventHandler? LogsClicked;
    public event EventHandler? CreateClicked;
    public event EventHandler? UpdateClicked;
    public event EventHandler? UsageClicked;
    public event EventHandler<string>? OpenFailed;

    public void Bind(RemoteService service)
    {
        _service = service;
        _glyph = GlyphFor(service);
        ApplyView(ServiceCardPresenter.From(service, null, 4, DateTimeOffset.UtcNow, false));
    }

    public void Apply(ServiceCardView view)
    {
        ApplyView(view);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ThemeManager.ThemeChanged -= OnTheme;
            _tips.Dispose();
            _menu.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var p = ThemeManager.Current;
        var padX = LogicalToDeviceUnits(16);
        var padTop = LogicalToDeviceUnits(14);
        var icon = LogicalToDeviceUnits(32);

        var iconRect = new Rectangle(padX, padTop, icon, icon);
        using (var path = Tokens.RoundedRect(iconRect, LogicalToDeviceUnits(8)))
        using (var brush = new SolidBrush(p.AccentSoft))
            g.FillPath(brush, path);
        using (var iconFont = Tokens.Icon(14f))
            TextRenderer.DrawText(g, _glyph, iconFont, iconRect, p.Accent,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        var nameX = iconRect.Right + LogicalToDeviceUnits(10);
        var nameW = Math.Max(40, _pill.Left - nameX - LogicalToDeviceUnits(8));
        using var nameFont = Tokens.BodyStrong();
        using var capFont = Tokens.Caption();
        TextRenderer.DrawText(g, _service?.DisplayName ?? "", nameFont,
            new Rectangle(nameX, padTop, nameW, LogicalToDeviceUnits(16)), p.Text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

        var addrColor = _addressHover && !string.IsNullOrEmpty(_view.OpenUrl) ? p.AccentHover : p.Accent;
        TextRenderer.DrawText(g, _view.AddressLabel, capFont,
            new Rectangle(nameX, padTop + LogicalToDeviceUnits(16), nameW, LogicalToDeviceUnits(14)), addrColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

        var bodyTop = padTop + icon + LogicalToDeviceUnits(12);
        if (_view.Body is ServiceCardBodyKind.StoppedBox or ServiceCardBodyKind.AbsentBox)
        {
            var box = new Rectangle(padX, bodyTop, Width - padX * 2, LogicalToDeviceUnits(68));
            using (var path = Tokens.RoundedRect(box, LogicalToDeviceUnits(8)))
            using (var pen = new Pen(p.Stroke2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                g.DrawPath(pen, path);
            var msg = Glyphs.Stop + "  " + _view.StateLine;
            TextRenderer.DrawText(g, msg, capFont, box, p.Text3,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
        else
        {
            DrawMonitorRow(g, p, capFont, padX, bodyTop, "Estado", _view.StateLine, null, full: true);
            DrawMonitorRow(g, p, capFont, padX, bodyTop + LogicalToDeviceUnits(8 + 14), "CPU", _view.CpuText, _cpuBar, full: false);
            DrawMonitorRow(g, p, capFont, padX, bodyTop + LogicalToDeviceUnits(2 * (8 + 14)), "Memória", _view.MemText, _memBar, full: false);
        }

        var actionY = Height - LogicalToDeviceUnits(10) - LogicalToDeviceUnits(32);
        using (var pen = new Pen(p.Stroke))
            g.DrawLine(pen, padX, actionY - LogicalToDeviceUnits(8), Width - padX, actionY - LogicalToDeviceUnits(8));
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        var padX = LogicalToDeviceUnits(16);
        var padTop = LogicalToDeviceUnits(14);
        _pill.Location = new Point(Width - padX - _pill.Width, padTop + LogicalToDeviceUnits(5));

        var bodyTop = padTop + LogicalToDeviceUnits(32) + LogicalToDeviceUnits(12);
        var keyW = LogicalToDeviceUnits(64);
        var valW = LogicalToDeviceUnits(116);
        var barX = padX + keyW + LogicalToDeviceUnits(4);
        var barW = Math.Max(20, Width - padX - barX - valW - LogicalToDeviceUnits(8));
        var showBars = _view.Body is ServiceCardBodyKind.Monitor or ServiceCardBodyKind.ErrorMonitor or ServiceCardBodyKind.Pending;
        _cpuBar.Visible = showBars;
        _memBar.Visible = showBars;
        if (showBars)
        {
            _cpuBar.SetBounds(barX, bodyTop + LogicalToDeviceUnits(8 + 14) + LogicalToDeviceUnits(5), barW, LogicalToDeviceUnits(6));
            _memBar.SetBounds(barX, bodyTop + LogicalToDeviceUnits(2 * (8 + 14)) + LogicalToDeviceUnits(5), barW, LogicalToDeviceUnits(6));
        }

        var actionY = Height - LogicalToDeviceUnits(10) - LogicalToDeviceUnits(32);
        _playStop.Location = new Point(padX, actionY);
        _logs.Location = new Point(_playStop.Right + LogicalToDeviceUnits(4), actionY);
        _more.Location = new Point(Width - padX - _more.Width, actionY);
        _open.Location = new Point(_more.Left - LogicalToDeviceUnits(4) - _open.Width, actionY);
    }

    private void ApplyView(ServiceCardView view)
    {
        _view = view;
        _pill.Kind = view.Pill switch
        {
            StatusPillKindUi.Online => StatusPillKind.Online,
            StatusPillKindUi.Stopped => StatusPillKind.Stopped,
            StatusPillKindUi.Error => StatusPillKind.Error,
            _ => StatusPillKind.Unknown
        };

        if (view.CanStop)
        {
            _playStop.Glyph = Glyphs.Stop;
            _tips.SetToolTip(_playStop, "Parar");
        }
        else
        {
            _playStop.Glyph = Glyphs.Play;
            _tips.SetToolTip(_playStop, "Subir");
        }

        _playStop.Enabled = view.CanStart || view.CanStop;
        _logs.Enabled = view.CanLogs;
        _open.Enabled = view.CanOpen;
        _menuCreate.Enabled = view.CanCreate;
        _menuUpdate.Enabled = view.CanUpdate;
        _menuUsage.Enabled = view.CanUsage;

        _cpuBar.Value = view.CpuPercent ?? 0;
        _memBar.Value = view.MemPercent ?? 0;
        if (view.CpuPercent is null)
            _cpuBar.Value = 0;
        if (view.MemPercent is null)
            _memBar.Value = 0;

        PerformLayout();
        Invalidate();
    }

    private void DrawMonitorRow(
        Graphics g,
        Palette p,
        Font capFont,
        int padX,
        int y,
        string key,
        string value,
        ThinProgressBar? bar,
        bool full)
    {
        var keyW = LogicalToDeviceUnits(64);
        var valW = LogicalToDeviceUnits(116);
        TextRenderer.DrawText(g, key, capFont, new Rectangle(padX, y, keyW, LogicalToDeviceUnits(14)), p.Text3,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        if (full)
        {
            var w = Width - padX - keyW - padX;
            TextRenderer.DrawText(g, value, capFont, new Rectangle(padX + keyW, y, w, LogicalToDeviceUnits(14)), p.Text2,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        }
        else
        {
            TextRenderer.DrawText(g, value, capFont,
                new Rectangle(Width - padX - valW, y, valW, LogicalToDeviceUnits(14)), p.Text2,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }

    private void OnMouseMoveAddress(object? sender, MouseEventArgs e)
    {
        var padX = LogicalToDeviceUnits(16);
        var padTop = LogicalToDeviceUnits(14);
        var nameX = padX + LogicalToDeviceUnits(32 + 10);
        var rect = new Rectangle(nameX, padTop + LogicalToDeviceUnits(16), Math.Max(40, _pill.Left - nameX - 8), LogicalToDeviceUnits(14));
        var hover = rect.Contains(e.Location) && !string.IsNullOrEmpty(_view.OpenUrl);
        if (hover != _addressHover)
        {
            _addressHover = hover;
            Cursor = hover ? Cursors.Hand : Cursors.Default;
            Invalidate(rect);
        }
    }

    private void OnAddressClick(object? sender, MouseEventArgs e)
    {
        if (!_addressHover || _view.OpenUrl is null)
            return;
        try
        {
            Process.Start(new ProcessStartInfo(_view.OpenUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            OpenFailed?.Invoke(this, ex.Message);
        }
    }

    private void OnTheme(object? sender, EventArgs e) => Invalidate();

    private static string GlyphFor(RemoteService s) => s.Id switch
    {
        "mausbot" => Glyphs.Robot,
        "tailscale" or "tailscale-9router" or "tailscale-mausbot" => Glyphs.Shield,
        _ => Glyphs.Network
    };
}

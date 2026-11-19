using ZLab.Services;

namespace ZLab.Ui;

/// <summary>
/// Local card for the WSL Tailscale container. Same chrome as a service card, no SSH actions.
/// </summary>
public sealed class WslTailscaleCard : RoundedPanel
{
    private readonly StatusPill _pill = new();
    private readonly ThinProgressBar _cpuBar = new() { BarHeight = Tokens.BarHeightCard, ColorMode = ProgressColorMode.Severity };
    private readonly ThinProgressBar _memBar = new() { BarHeight = Tokens.BarHeightCard, ColorMode = ProgressColorMode.Severity };
    private readonly ZButton _playStop = new() { IconOnly = true, Variant = ZButtonVariant.Ghost };
    private readonly ZButton _more = new() { IconOnly = true, Glyph = Glyphs.More, Variant = ZButtonVariant.Ghost };
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _menuRemove = new("Remover");
    private readonly ToolTip _tips = new();

    private bool _configured;
    private WslTailscaleSnapshot _snap = WslTailscaleSnapshot.Absent;
    private bool _monitor;

    public WslTailscaleCard()
    {
        Radius = Tokens.RadiusCard;
        _menu = ThemedContextMenu.Create();
        _menu.Items.Add(_menuRemove);
        Controls.Add(_pill);
        Controls.Add(_cpuBar);
        Controls.Add(_memBar);
        Controls.Add(_playStop);
        Controls.Add(_more);
        _tips.SetToolTip(_more, "Mais ações");
        _playStop.Click += (_, _) =>
        {
            if (_snap.Running)
                StopClicked?.Invoke(this, EventArgs.Empty);
            else
                StartClicked?.Invoke(this, EventArgs.Empty);
        };
        _more.Click += (_, _) => _menu.Show(_more, new Point(0, _more.Height));
        _menuRemove.Click += (_, _) => RemoveClicked?.Invoke(this, EventArgs.Empty);
        ThemeManager.ThemeChanged += (_, _) => Invalidate();
        Apply(_snap, configured: false);
    }

    public event EventHandler? StartClicked;
    public event EventHandler? StopClicked;
    public event EventHandler? RemoveClicked;

    public void Apply(WslTailscaleSnapshot snapshot, bool configured)
    {
        _snap = snapshot;
        _configured = configured;
        _monitor = snapshot.Running;
        _pill.Kind = snapshot.Running ? StatusPillKind.Online : StatusPillKind.Stopped;
        if (snapshot.Running)
        {
            _playStop.Glyph = Glyphs.Stop;
            _tips.SetToolTip(_playStop, "Parar");
        }
        else
        {
            _playStop.Glyph = Glyphs.Play;
            _tips.SetToolTip(_playStop, "Subir");
        }

        _playStop.Enabled = true;
        _cpuBar.Value = snapshot.CpuPercent ?? 0;
        _memBar.Value = snapshot.MemPercent ?? 0;
        _cpuBar.Visible = _monitor;
        _memBar.Visible = _monitor;
        PerformLayout();
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
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
            TextRenderer.DrawText(g, Glyphs.Shield, iconFont, iconRect, p.Accent,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        var nameX = iconRect.Right + LogicalToDeviceUnits(10);
        var nameW = Math.Max(40, _pill.Left - nameX - LogicalToDeviceUnits(8));
        using var nameFont = Tokens.BodyStrong();
        using var capFont = Tokens.Caption();
        TextRenderer.DrawText(g, "wsl-tailscale", nameFont,
            new Rectangle(nameX, padTop, nameW, LogicalToDeviceUnits(16)), p.Text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
        TextRenderer.DrawText(g, WslTailscaleController.AddressLabel, capFont,
            new Rectangle(nameX, padTop + LogicalToDeviceUnits(16), nameW, LogicalToDeviceUnits(16)), p.Accent,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);

        var bodyTop = padTop + icon + LogicalToDeviceUnits(12);
        if (!_monitor)
        {
            var box = new Rectangle(padX, bodyTop, Width - padX * 2, LogicalToDeviceUnits(68));
            using (var path = Tokens.RoundedRect(box, LogicalToDeviceUnits(8)))
            using (var pen = new Pen(p.Stroke2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                g.DrawPath(pen, path);
            var msg = Glyphs.Stop + "  " + StateLine();
            TextRenderer.DrawText(g, msg, capFont, box, p.Text3,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.WordBreak);
        }
        else
        {
            var step = LogicalToDeviceUnits(24);
            DrawRow(g, p, capFont, padX, bodyTop, "Estado", StateLine(), full: true);
            DrawRow(g, p, capFont, padX, bodyTop + step, "CPU", _snap.CpuText, full: false);
            DrawRow(g, p, capFont, padX, bodyTop + 2 * step, "Memória", _snap.MemText, full: false);
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
        var keyW = LogicalToDeviceUnits(72);
        using var capFont = Tokens.Caption();
        var valW = CardText.ValueWidth(capFont, _snap.CpuText, _snap.MemText, LogicalToDeviceUnits(8));
        var barX = padX + keyW + LogicalToDeviceUnits(4);
        var barW = Math.Max(20, Width - padX - barX - valW - LogicalToDeviceUnits(8));
        var step = LogicalToDeviceUnits(24);
        if (_monitor)
        {
            _cpuBar.SetBounds(barX, bodyTop + step + LogicalToDeviceUnits(5), barW, LogicalToDeviceUnits(6));
            _memBar.SetBounds(barX, bodyTop + 2 * step + LogicalToDeviceUnits(5), barW, LogicalToDeviceUnits(6));
        }

        var actionY = Height - LogicalToDeviceUnits(10) - LogicalToDeviceUnits(32);
        _playStop.Location = new Point(padX, actionY);
        _more.Location = new Point(Width - padX - _more.Width, actionY);
    }

    private string StateLine()
    {
        if (_snap.Running)
        {
            var uptime = _snap.StartedAt is DateTimeOffset start
                ? DisplayFormat.Uptime(DateTimeOffset.UtcNow - start)
                : "—";
            return "Ativo há " + uptime + " · " + ServiceCardPresenter.RestartsLabel(_snap.Restarts);
        }

        return _configured ? "Container parado · sem consumo" : "Desativado";
    }

    private void DrawRow(Graphics g, Palette p, Font capFont, int padX, int y, string key, string value, bool full)
    {
        var keyW = LogicalToDeviceUnits(72);
        var rowH = LogicalToDeviceUnits(16);
        TextRenderer.DrawText(g, key, capFont, new Rectangle(padX, y, keyW, rowH), p.Text3,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        if (full)
        {
            var w = Width - padX - keyW - padX;
            TextRenderer.DrawText(g, value, capFont, new Rectangle(padX + keyW, y, Math.Max(20, w), rowH), p.Text2,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            return;
        }

        var valW = CardText.ValueWidth(capFont, value, value, LogicalToDeviceUnits(8));
        TextRenderer.DrawText(g, value, capFont,
            new Rectangle(Width - padX - valW, y, valW, rowH), p.Text2,
            CardText.ValueFlags);
    }
}

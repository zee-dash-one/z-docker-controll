using ZLab.Models;

namespace ZLab.Ui;

/// <summary>
/// Demo window for theme tokens and custom controls. Open with --galeria.
/// </summary>
internal sealed class ComponentGalleryForm : Form
{
    public ComponentGalleryForm()
    {
        Text = "Z-Lab — galeria de componentes";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(980, 720);
        Font = Tokens.Body();
        ThemeManager.Apply(ThemeMode.Dark);
        ThemeManager.ApplyTo(this);
        ThemeManager.ThemeChanged += OnTheme;

        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(Tokens.ContentPadX, Tokens.ContentPadTop, Tokens.ContentPadX, Tokens.ContentPadTop)
        };

        var themeBtn = new ZButton
        {
            Text = "Alternar tema",
            Glyph = Glyphs.Brightness,
            Variant = ZButtonVariant.Secondary,
            Location = new Point(Tokens.ContentPadX, Tokens.ContentPadTop)
        };
        themeBtn.Click += (_, _) => ThemeManager.ToggleLightDark();

        var y = Tokens.ContentPadTop + 48;
        scroll.Controls.Add(themeBtn);

        AddLabel(scroll, "MetricTile", ref y);
        var tiles = new ResponsiveGrid
        {
            Location = new Point(Tokens.ContentPadX, y),
            Width = ClientSize.Width - Tokens.ContentPadX * 2,
            MinItemWidth = 180,
            MaxColumns = 6,
            ItemHeight = 118,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        tiles.Controls.Add(MakeTile("CPU", Glyphs.Cpu, [("7", "%")], "média dos 4 núcleos", 7, MetricFooterKind.Sparkline, false));
        tiles.Controls.Add(MakeTile("Memória", Glyphs.Ram, [("1,4", "GB")], "de 15 GB · 13 GB livres", 9, MetricFooterKind.Bar, false));
        tiles.Controls.Add(MakeTile("Disco do sistema", Glyphs.HardDrive, [("12", "GB")], "de 49 GB · 37 GB livres", 26, MetricFooterKind.Bar, false));
        tiles.Controls.Add(MakeTile("Disco Docker", Glyphs.HardDrive, [("91", "MB")], "de 147 GB · 147 GB livres", 1, MetricFooterKind.Bar, false));
        var empty = MakeTile("Dados /data", Glyphs.HardDrive, [("Não instalado", "")], "HD 500 GB ainda não montado", null, MetricFooterKind.None, true);
        tiles.Controls.Add(empty);
        tiles.Controls.Add(MakeTile("Ligado há", Glyphs.Clock, [("11", "h"), ("4", "min")], "desde ontem 15:26", null, MetricFooterKind.Text, false, "2 usuários conectados"));
        var sparkTile = (MetricTile)tiles.Controls[0];
        var rnd = new Random(1);
        for (var i = 0; i < 40; i++)
            sparkTile.Spark.Push(rnd.NextDouble() * 30 + 5);
        scroll.Controls.Add(tiles);
        y += 140;

        AddLabel(scroll, "StatusPill", ref y);
        var x = Tokens.ContentPadX;
        foreach (StatusPillKind kind in Enum.GetValues<StatusPillKind>())
        {
            var pill = new StatusPill { Kind = kind, Location = new Point(x, y) };
            scroll.Controls.Add(pill);
            x += pill.Width + 12;
        }

        y += 40;
        AddLabel(scroll, "ZButton", ref y);
        x = Tokens.ContentPadX;
        foreach (ZButtonVariant v in Enum.GetValues<ZButtonVariant>())
        {
            var btn = new ZButton
            {
                Text = v.ToString(),
                Variant = v,
                Glyph = Glyphs.Power,
                Location = new Point(x, y)
            };
            scroll.Controls.Add(btn);
            x += btn.Width + 8;
        }

        y += 48;
        x = Tokens.ContentPadX;
        var disabled = new ZButton { Text = "Desabilitado", Variant = ZButtonVariant.Primary, Location = new Point(x, y), Enabled = false };
        scroll.Controls.Add(disabled);
        x += disabled.Width + 8;
        var icon = new ZButton { Glyph = Glyphs.Sync, IconOnly = true, Location = new Point(x, y) };
        new ToolTip().SetToolTip(icon, "Atualizar");
        scroll.Controls.Add(icon);

        y += 56;
        AddLabel(scroll, "ThinProgressBar", ref y);
        foreach (var pct in new[] { 9.0, 74.0, 95.0 })
        {
            var bar = new ThinProgressBar
            {
                Value = pct,
                Width = 280,
                Height = 12,
                Location = new Point(Tokens.ContentPadX, y)
            };
            scroll.Controls.Add(bar);
            y += 20;
        }

        Controls.Add(scroll);
        Resize += (_, _) =>
        {
            tiles.Width = Math.Max(200, ClientSize.Width - Tokens.ContentPadX * 2 - SystemInformation.VerticalScrollBarWidth);
        };
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ThemeManager.ApplyTo(this);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            ThemeManager.ThemeChanged -= OnTheme;
        base.Dispose(disposing);
    }

    private void OnTheme(object? sender, EventArgs e)
    {
        ThemeManager.ApplyTo(this);
        Invalidate(true);
    }

    private static MetricTile MakeTile(
        string title,
        string glyph,
        (string, string)[] parts,
        string caption,
        double? percent,
        MetricFooterKind footer,
        bool empty,
        string? footerText = null)
    {
        return new MetricTile
        {
            Title = title,
            Glyph = glyph,
            ValueParts = parts,
            Caption = caption,
            Percent = percent,
            FooterKind = footer,
            IsEmpty = empty,
            FooterText = footerText ?? ""
        };
    }

    private static void AddLabel(Control parent, string text, ref int y)
    {
        var label = new Label
        {
            Text = text,
            Font = Tokens.Subtitle(),
            AutoSize = true,
            Location = new Point(Tokens.ContentPadX, y),
            ForeColor = ThemeManager.Current.Text
        };
        parent.Controls.Add(label);
        y += 28;
    }
}

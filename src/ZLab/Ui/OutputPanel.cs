using System.Drawing.Drawing2D;
using ZLab.Services;

namespace ZLab.Ui;

/// <summary>
/// Collapsible Saída log: 40 px collapsed, 220 px expanded.
/// </summary>
public sealed class OutputPanel : Panel
{
    private readonly OutputBuffer _buffer = new();
    private readonly Panel _bar = new();
    private readonly Label _glyph = new();
    private readonly Label _title = new();
    private readonly Label _badge = new();
    private readonly Label _preview = new();
    private readonly ZButton _chevron = new() { IconOnly = true, Variant = ZButtonVariant.Ghost };
    private readonly ZButton _copy = new()
    {
        Text = "Copiar",
        Glyph = Glyphs.Copy,
        Compact = true,
        Variant = ZButtonVariant.Ghost,
        Visible = false
    };
    private readonly ZButton _clear = new()
    {
        Text = "Limpar",
        Compact = true,
        Variant = ZButtonVariant.Ghost,
        Visible = false
    };
    private readonly RichTextBox _box = new()
    {
        ReadOnly = true,
        BorderStyle = BorderStyle.None,
        DetectUrls = false,
        ScrollBars = RichTextBoxScrollBars.Vertical,
        Visible = false,
        WordWrap = false
    };
    private readonly ToolTip _tips = new();
    private bool _expanded;
    private Font? _mono;
    private string _badgeText = "";

    public OutputPanel()
    {
        Dock = DockStyle.Bottom;
        DoubleBuffered = true;
        Height = LogicalToDeviceUnits(40);

        _bar.Dock = DockStyle.Top;
        _bar.Height = LogicalToDeviceUnits(40);
        _bar.Cursor = Cursors.Hand;

        _glyph.AutoSize = true;
        _glyph.Text = Glyphs.Document;
        _title.AutoSize = true;
        _title.Text = "Saída";
        _badge.AutoSize = true;
        _badge.TextAlign = ContentAlignment.MiddleCenter;
        _badge.Visible = false;
        _preview.AutoSize = false;
        _preview.AutoEllipsis = true;

        _chevron.Glyph = Glyphs.ChevronUp;
        _tips.SetToolTip(_chevron, "Mostrar saída");
        _tips.SetToolTip(_copy, "Copiar toda a saída");
        _tips.SetToolTip(_clear, "Limpar a saída");

        _bar.Controls.Add(_glyph);
        _bar.Controls.Add(_title);
        _bar.Controls.Add(_badge);
        _bar.Controls.Add(_preview);
        _bar.Controls.Add(_copy);
        _bar.Controls.Add(_clear);
        _bar.Controls.Add(_chevron);

        Controls.Add(_box);
        Controls.Add(_bar);

        _bar.Click += (_, _) => Toggle();
        _glyph.Click += (_, _) => Toggle();
        _title.Click += (_, _) => Toggle();
        _preview.Click += (_, _) => Toggle();
        _badge.Click += (_, _) => Toggle();
        _chevron.Click += (_, _) => Toggle();
        _copy.Click += (_, _) => CopyAll();
        _clear.Click += (_, _) =>
        {
            _buffer.Clear();
            RefreshView();
        };

        ThemeManager.ThemeChanged += OnTheme;
        ApplyColors();
        LayoutBar();
    }

    public bool IsExpanded => _expanded;

    public OutputBuffer Buffer => _buffer;

    public void Append(string text)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => Append(text));
            return;
        }

        _buffer.Append(text);
        if (_expanded)
        {
            _buffer.MarkRead();
            AppendToBox(_buffer.Last);
        }

        RefreshChrome();
    }

    public void Toggle()
    {
        SetExpanded(!_expanded);
    }

    public void SetExpanded(bool expanded)
    {
        if (_expanded == expanded)
            return;
        _expanded = expanded;
        Height = LogicalToDeviceUnits(_expanded ? 220 : 40);
        _box.Visible = _expanded;
        _copy.Visible = _expanded;
        _clear.Visible = _expanded;
        _chevron.Glyph = _expanded ? Glyphs.ChevronDown : Glyphs.ChevronUp;
        _tips.SetToolTip(_chevron, _expanded ? "Esconder saída" : "Mostrar saída");
        if (_expanded)
        {
            _buffer.MarkRead();
            RebuildBox();
        }

        RefreshChrome();
        LayoutBar();
        LayoutBox();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ThemeManager.ThemeChanged -= OnTheme;
            _tips.Dispose();
            _mono?.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        _bar.Height = LogicalToDeviceUnits(40);
        LayoutBar();
        LayoutBox();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        var p = ThemeManager.Current;
        using (var brush = new SolidBrush(p.Surface))
            g.FillRectangle(brush, ClientRectangle);
        using (var pen = new Pen(p.Stroke))
            g.DrawLine(pen, 0, 0, Width, 0);
    }

    protected override void OnPaintBackground(PaintEventArgs e) { }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        _mono?.Dispose();
        _mono = null;
        ApplyFonts();
        Height = LogicalToDeviceUnits(_expanded ? 220 : 40);
        Invalidate();
    }

    private void LayoutBar()
    {
        var pad = LogicalToDeviceUnits(24);
        _glyph.Font = Tokens.Icon(12f);
        _title.Font = new Font(Tokens.UiFamily, Tokens.BodyPt, FontStyle.Bold);
        _badge.Font = Tokens.Caption();
        ApplyFonts();

        _glyph.Location = new Point(pad, (_bar.Height - _glyph.PreferredHeight) / 2);
        _title.Location = new Point(_glyph.Right + LogicalToDeviceUnits(8), (_bar.Height - _title.PreferredHeight) / 2);

        var x = _title.Right + LogicalToDeviceUnits(8);
        if (_badge.Visible)
        {
            var bw = Math.Max(LogicalToDeviceUnits(18), _badge.PreferredWidth + LogicalToDeviceUnits(10));
            var bh = LogicalToDeviceUnits(18);
            _badge.SetBounds(x, (_bar.Height - bh) / 2, bw, bh);
            x = _badge.Right + LogicalToDeviceUnits(12);
        }

        _chevron.Location = new Point(_bar.Width - pad - _chevron.Width, (_bar.Height - _chevron.Height) / 2);
        var rightLimit = _chevron.Left - LogicalToDeviceUnits(8);
        if (_expanded)
        {
            _clear.Location = new Point(rightLimit - _clear.Width, (_bar.Height - _clear.Height) / 2);
            _copy.Location = new Point(_clear.Left - LogicalToDeviceUnits(4) - _copy.Width, (_bar.Height - _copy.Height) / 2);
            rightLimit = _copy.Left - LogicalToDeviceUnits(8);
            _preview.Visible = false;
        }
        else
        {
            _preview.Visible = true;
            _preview.SetBounds(x, (_bar.Height - LogicalToDeviceUnits(16)) / 2, Math.Max(40, rightLimit - x), LogicalToDeviceUnits(16));
        }
    }

    private void LayoutBox()
    {
        if (!_expanded)
            return;
        var pad = LogicalToDeviceUnits(24);
        _box.SetBounds(
            pad,
            _bar.Bottom + LogicalToDeviceUnits(4),
            Math.Max(40, Width - pad * 2),
            Math.Max(40, Height - _bar.Height - LogicalToDeviceUnits(8)));
    }

    private void RefreshChrome()
    {
        var unread = _expanded ? 0 : _buffer.UnreadCount;
        if (unread > 0)
        {
            _badgeText = unread > 99 ? "99+" : unread.ToString();
            _badge.Text = "";
            _badge.Visible = true;
            _badge.Width = Math.Max(LogicalToDeviceUnits(18), TextRenderer.MeasureText(_badgeText, Tokens.Caption()).Width + LogicalToDeviceUnits(10));
        }
        else
        {
            _badge.Visible = false;
            _badgeText = "";
            _badge.Text = "";
        }

        var last = _buffer.Last;
        if (last is null)
            _preview.Text = "";
        else
        {
            var formatted = _buffer.FormatLine(last);
            // Caption time in Text3 + mono body — approximate with one string; color via Paint optional.
            _preview.Text = formatted;
        }

        LayoutBar();
        Invalidate();
    }

    private void RefreshView()
    {
        if (_expanded)
            RebuildBox();
        RefreshChrome();
    }

    private void RebuildBox()
    {
        _box.Clear();
        ApplyFonts();
        foreach (var line in _buffer.Lines)
            AppendToBox(line);
        if (_box.TextLength > 0)
        {
            _box.SelectionStart = _box.TextLength;
            _box.ScrollToCaret();
        }
    }

    private void AppendToBox(OutputLine? line)
    {
        if (line is null)
            return;
        var p = ThemeManager.Current;
        var color = line.Kind switch
        {
            OutputLineKind.Error => p.Bad,
            OutputLineKind.Warn => p.Warn,
            _ => p.Text2
        };
        if (_box.TextLength > 0)
            _box.AppendText(Environment.NewLine);
        _box.SelectionStart = _box.TextLength;
        _box.SelectionLength = 0;
        _box.SelectionColor = color;
        _box.SelectionFont = MonoFont();
        _box.AppendText(_buffer.FormatLine(line));
    }

    private void CopyAll()
    {
        try
        {
            var text = _buffer.FormatAll();
            if (text.Length > 0)
                Clipboard.SetText(text);
        }
        catch
        {
            // clipboard busy
        }
    }

    private void OnTheme(object? sender, EventArgs e)
    {
        ApplyColors();
        if (_expanded)
            RebuildBox();
        Invalidate();
    }

    private void ApplyColors()
    {
        var p = ThemeManager.Current;
        BackColor = p.Surface;
        _bar.BackColor = p.Surface;
        _glyph.ForeColor = p.Text3;
        _glyph.BackColor = p.Surface;
        _title.ForeColor = p.Text;
        _title.BackColor = p.Surface;
        _preview.ForeColor = p.Text2;
        _preview.BackColor = p.Surface;
        _badge.ForeColor = p.Text2;
        _badge.BackColor = p.Surface3;
        _badge.Paint -= PaintBadge;
        _badge.Paint += PaintBadge;
        _box.BackColor = p.Surface;
        _box.ForeColor = p.Text2;
        ApplyFonts();
    }

    private void PaintBadge(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var p = ThemeManager.Current;
        var bounds = new Rectangle(0, 0, _badge.Width - 1, _badge.Height - 1);
        using var path = Tokens.RoundedRect(bounds, LogicalToDeviceUnits(9));
        using (var brush = new SolidBrush(p.Surface3))
            g.FillPath(brush, path);
        TextRenderer.DrawText(
            g, _badgeText, Tokens.Caption(), bounds, p.Text2,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }

    private void ApplyFonts()
    {
        _box.Font = MonoFont();
        _preview.Font = MonoFont();
    }

    private Font MonoFont()
    {
        _mono ??= new Font(Tokens.MonoFamily, 9f, FontStyle.Regular);
        return _mono;
    }
}

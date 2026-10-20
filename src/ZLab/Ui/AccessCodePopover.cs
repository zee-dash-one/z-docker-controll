using System.Globalization;
using ZLab.Services;

namespace ZLab.Ui;

/// <summary>
/// Borderless popover that holds the MausBot pairing code in memory only.
/// </summary>
public sealed class AccessCodePopover : Form
{
    private readonly Label _title = new();
    private readonly Label _hint = new();
    private readonly Panel _codeBox = new();
    private readonly Label _codeLabel = new();
    private readonly ZButton _copy = new()
    {
        Text = "Copiar",
        Glyph = Glyphs.Copy,
        Compact = true,
        Variant = ZButtonVariant.Primary
    };
    private readonly Label _footer = new();
    private readonly Label _problem = new();
    private readonly System.Windows.Forms.Timer _autoClose = new() { Interval = 60_000 };
    private readonly System.Windows.Forms.Timer _copiedReset = new() { Interval = 2_000 };

    private string? _code;
    private bool _closing;

    public AccessCodePopover()
    {
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        KeyPreview = true;
        Width = LogicalToDeviceUnits(340);
        DoubleBuffered = true;

        _title.AutoSize = true;
        _title.Font = Tokens.BodyStrong();
        _hint.AutoSize = false;
        _hint.Font = Tokens.Caption();
        _hint.Text = "Lido agora do log do container MausBot. Não é salvo em lugar nenhum.";
        _footer.AutoSize = false;
        _footer.Font = Tokens.Caption();
        _problem.AutoSize = false;
        _problem.Font = Tokens.Body();
        _problem.Visible = false;

        _codeBox.Controls.Add(_codeLabel);
        _codeBox.Controls.Add(_copy);
        _codeLabel.AutoSize = false;
        _codeLabel.AutoEllipsis = true;
        _codeLabel.Font = Tokens.Code();
        _codeLabel.TextAlign = ContentAlignment.MiddleLeft;

        Controls.Add(_title);
        Controls.Add(_hint);
        Controls.Add(_codeBox);
        Controls.Add(_problem);
        Controls.Add(_footer);

        _copy.Click += (_, _) => CopyCode();
        _autoClose.Tick += (_, _) => CloseSafe();
        _copiedReset.Tick += (_, _) =>
        {
            _copiedReset.Stop();
            _copy.Text = "Copiar";
            if (Visible)
            {
                LayoutChildren();
                ApplyClip();
            }
        };

        ThemeManager.ThemeChanged += OnTheme;
        ApplyColors();
        LayoutChildren();
    }

    public void ShowResult(AccessCodeResult result, Control anchor, Form owner)
    {
        Owner = owner;
        _code = result.Found ? result.Code : null;

        var p = ThemeManager.Current;
        _title.Text = Glyphs.Permissions + "  Código de primeiro acesso";
        _title.ForeColor = p.Accent;

        if (result.Found && _code is not null)
        {
            _codeBox.Visible = true;
            _problem.Visible = false;
            _codeLabel.Text = MausBotAccessCode.FormatForDisplay(_code);
            _copy.Text = "Copiar";
            var when = result.SeenAt?.ToLocalTime() ?? DateTimeOffset.Now;
            _footer.Text = "Visto às "
                + when.ToString("HH:mm", CultureInfo.GetCultureInfo("pt-BR"))
                + " · use em "
                + MausBotAccessCode.PairUrl;
        }
        else
        {
            _codeBox.Visible = false;
            _problem.Visible = true;
            _problem.Text = result.Problem ?? MausBotAccessCode.NotFoundProblem;
            _footer.Text = "use em " + MausBotAccessCode.PairUrl;
        }

        LayoutChildren();
        PositionNear(anchor, owner);
        Show(owner);
        LayoutChildren();
        PositionNear(anchor, owner);
        ApplyClip();
        Native.TrySetRoundCorners(Handle);
        Native.TrySetImmersiveDarkMode(Handle, ThemeManager.Current == Palette.Dark);
        Activate();
        _autoClose.Stop();
        _autoClose.Start();
    }

    public void ClearCode() => _code = null;

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var p = ThemeManager.Current;
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        var radius = LogicalToDeviceUnits(12);
        using var path = Tokens.RoundedRect(bounds, radius);
        using (var brush = new SolidBrush(p.Surface))
            g.FillPath(brush, path);
        using (var pen = new Pen(p.Stroke2))
            g.DrawPath(pen, path);
    }

    protected override void OnPaintBackground(PaintEventArgs e) { }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        CloseSafe();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            e.Handled = true;
            CloseSafe();
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Native.TrySetRoundCorners(Handle);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
            return cp;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ThemeManager.ThemeChanged -= OnTheme;
            _autoClose.Stop();
            _autoClose.Dispose();
            _copiedReset.Stop();
            _copiedReset.Dispose();
            _code = null;
        }

        base.Dispose(disposing);
    }

    private void CopyCode()
    {
        if (string.IsNullOrEmpty(_code))
            return;
        try
        {
            Clipboard.SetText(_code);
            _copy.Text = "Copiado";
            LayoutChildren();
            ApplyClip();
            _copiedReset.Stop();
            _copiedReset.Start();
        }
        catch
        {
            // clipboard busy — ignore
        }
    }

    private void CloseSafe()
    {
        if (_closing || IsDisposed)
            return;
        _closing = true;
        _autoClose.Stop();
        _copiedReset.Stop();
        _code = null;
        try
        {
            Close();
        }
        catch
        {
            // already closing
        }
    }

    private void PositionNear(Control anchor, Form owner)
    {
        var screen = anchor.PointToScreen(new Point(0, anchor.Height + LogicalToDeviceUnits(6)));
        var card = anchor.Parent;
        var rightEdge = card is not null
            ? card.PointToScreen(new Point(card.Width, 0)).X
            : screen.X + Width;
        var x = rightEdge - Width;
        var y = screen.Y;

        var wa = Screen.FromControl(owner).WorkingArea;
        if (x + Width > wa.Right)
            x = wa.Right - Width;
        if (x < wa.Left)
            x = wa.Left;
        if (y + Height > wa.Bottom)
            y = Math.Max(wa.Top, anchor.PointToScreen(Point.Empty).Y - Height - LogicalToDeviceUnits(6));
        if (y < wa.Top)
            y = wa.Top;

        Location = new Point(x, y);
    }

    private void LayoutChildren()
    {
        SuspendLayout();
        var pad = LogicalToDeviceUnits(16);
        var gap = LogicalToDeviceUnits(8);
        var innerW = Math.Max(LogicalToDeviceUnits(280), Width - pad * 2);
        var y = pad;

        _title.MaximumSize = new Size(innerW, 0);
        _title.Location = new Point(pad, y);
        y += Math.Max(_title.Height, _title.PreferredHeight) + gap;

        var hintH = MeasureWrapped(_hint.Text, _hint.Font, innerW);
        _hint.SetBounds(pad, y, innerW, hintH);
        y += hintH + LogicalToDeviceUnits(12);

        if (_codeBox.Visible)
        {
            var copyW = Math.Max(_copy.Width, LogicalToDeviceUnits(96));
            var copyH = Math.Max(_copy.Height, LogicalToDeviceUnits(28));
            var boxH = Math.Max(LogicalToDeviceUnits(44), copyH + LogicalToDeviceUnits(12));
            _codeBox.SetBounds(pad, y, innerW, boxH);
            _copy.SetBounds(
                innerW - copyW - gap,
                Math.Max(0, (boxH - copyH) / 2),
                copyW,
                copyH);
            _codeLabel.SetBounds(
                LogicalToDeviceUnits(12),
                0,
                Math.Max(LogicalToDeviceUnits(80), _copy.Left - LogicalToDeviceUnits(16)),
                boxH);
            y += boxH + LogicalToDeviceUnits(12);
        }
        else if (_problem.Visible)
        {
            var problemH = MeasureWrapped(_problem.Text, _problem.Font, innerW);
            _problem.SetBounds(pad, y, innerW, problemH);
            y += problemH + LogicalToDeviceUnits(12);
        }

        var footerH = MeasureWrapped(_footer.Text, _footer.Font, innerW);
        _footer.SetBounds(pad, y, innerW, footerH);
        y += footerH + pad;
        Height = y;
        ResumeLayout(true);
        Invalidate(true);
    }

    private void ApplyClip()
    {
        var radius = LogicalToDeviceUnits(12);
        using var path = Tokens.RoundedRect(new Rectangle(0, 0, Width, Height), radius);
        var next = new Region(path);
        var previous = Region;
        Region = next;
        previous?.Dispose();
    }

    private static int MeasureWrapped(string text, Font font, int width)
    {
        if (string.IsNullOrEmpty(text))
            return font.Height;
        var size = TextRenderer.MeasureText(
            text,
            font,
            new Size(Math.Max(1, width), int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
        return Math.Max(font.Height, size.Height);
    }

    private void OnTheme(object? sender, EventArgs e)
    {
        ApplyColors();
        Invalidate();
    }

    private void ApplyColors()
    {
        var p = ThemeManager.Current;
        BackColor = p.Surface;
        _title.ForeColor = p.Accent;
        _title.BackColor = p.Surface;
        _hint.ForeColor = p.Text3;
        _hint.BackColor = p.Surface;
        _footer.ForeColor = p.Text3;
        _footer.BackColor = p.Surface;
        _problem.ForeColor = p.Text2;
        _problem.BackColor = p.Surface;
        _codeBox.BackColor = p.Surface2;
        _codeLabel.ForeColor = p.Text;
        _codeLabel.BackColor = p.Surface2;
        _codeBox.Paint -= PaintCodeBox;
        _codeBox.Paint += PaintCodeBox;
    }

    private void PaintCodeBox(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var p = ThemeManager.Current;
        var bounds = new Rectangle(0, 0, _codeBox.Width - 1, _codeBox.Height - 1);
        using var path = Tokens.RoundedRect(bounds, LogicalToDeviceUnits(8));
        using (var brush = new SolidBrush(p.Surface2))
            g.FillPath(brush, path);
        using (var pen = new Pen(p.Stroke))
            g.DrawPath(pen, path);
    }

}

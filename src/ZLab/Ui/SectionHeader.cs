namespace ZLab.Ui;

public sealed class SectionHeader : Panel
{
    private readonly Label _title = new();
    private readonly Label _subtitle = new();
    private readonly FlowLayoutPanel _actions = new();

    public SectionHeader()
    {
        Height = LogicalToDeviceUnits(36);
        _title.AutoSize = true;
        _title.Font = Tokens.Subtitle();
        _subtitle.AutoSize = true;
        _subtitle.Font = Tokens.Caption();
        _actions.AutoSize = true;
        _actions.FlowDirection = FlowDirection.LeftToRight;
        _actions.WrapContents = false;
        Controls.Add(_title);
        Controls.Add(_subtitle);
        Controls.Add(_actions);
        ThemeManager.ThemeChanged += OnTheme;
        ApplyColors();
    }

    public string Title
    {
        get => _title.Text;
        set => _title.Text = value;
    }

    public string Subtitle
    {
        get => _subtitle.Text;
        set => _subtitle.Text = value;
    }

    public Control.ControlCollection Actions => _actions.Controls;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            ThemeManager.ThemeChanged -= OnTheme;
        base.Dispose(disposing);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        var pad = 0;
        _title.Location = new Point(pad, (Height - _title.PreferredHeight) / 2);
        _subtitle.Location = new Point(_title.Right + LogicalToDeviceUnits(12), (Height - _subtitle.PreferredHeight) / 2);
        _actions.Location = new Point(Width - _actions.PreferredSize.Width, (Height - _actions.PreferredSize.Height) / 2);
    }

    private void OnTheme(object? sender, EventArgs e) => ApplyColors();

    private void ApplyColors()
    {
        BackColor = ThemeManager.Current.Bg;
        _title.ForeColor = ThemeManager.Current.Text;
        _subtitle.ForeColor = ThemeManager.Current.Text3;
        _actions.BackColor = ThemeManager.Current.Bg;
    }
}

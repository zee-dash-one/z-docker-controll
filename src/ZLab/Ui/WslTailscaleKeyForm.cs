namespace ZLab.Ui;

/// <summary>
/// Asks for the Tailscale auth key. The value stays in the text box until the caller saves it.
/// </summary>
internal sealed class WslTailscaleKeyForm : Form
{
    private readonly TextBox _key = new() { UseSystemPasswordChar = true };

    public WslTailscaleKeyForm()
    {
        Text = "wsl-tailscale";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = Tokens.Body();
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(LogicalToDeviceUnits(420), LogicalToDeviceUnits(148));

        var hint = new Label
        {
            AutoSize = false,
            Text = "Cole a auth key com a tag tag:winwsl. Ela fica só no .env local.",
            Font = Tokens.Caption()
        };
        var ok = new ZButton
        {
            Text = "Salvar",
            Variant = ZButtonVariant.Primary,
            DialogResult = DialogResult.OK
        };
        var cancel = new ZButton
        {
            Text = "Cancelar",
            Variant = ZButtonVariant.Secondary,
            DialogResult = DialogResult.Cancel
        };

        Controls.Add(hint);
        Controls.Add(_key);
        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;
        ok.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        var pad = LogicalToDeviceUnits(16);
        hint.SetBounds(pad, pad, ClientSize.Width - pad * 2, LogicalToDeviceUnits(32));
        _key.SetBounds(pad, hint.Bottom + LogicalToDeviceUnits(8), ClientSize.Width - pad * 2, LogicalToDeviceUnits(24));
        cancel.Location = new Point(ClientSize.Width - pad - cancel.Width, _key.Bottom + LogicalToDeviceUnits(12));
        ok.Location = new Point(cancel.Left - LogicalToDeviceUnits(8) - ok.Width, cancel.Top);

        var p = ThemeManager.Current;
        BackColor = p.Surface;
        ForeColor = p.Text;
        hint.ForeColor = p.Text3;
        hint.BackColor = p.Surface;
        _key.BackColor = p.Surface2;
        _key.ForeColor = p.Text;
    }

    public string AuthKey => _key.Text;
}

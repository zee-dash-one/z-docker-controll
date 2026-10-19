namespace ZLab;

internal sealed class LoginForm : Form
{
    private readonly TextBox _user = new();
    private readonly TextBox _password = new();

    public LoginForm()
    {
        Text = "Z-Lab — sessão";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(420, 210);
        Font = new Font("Segoe UI", 9F);

        var host = new Label
        {
            AutoSize = false,
            Left = 16,
            Top = 16,
            Width = 388,
            Height = 32,
            Text = "Servidor 192.168.2.37, porta 22. A chave do disco não entra nesta sessão."
        };
        var userLabel = new Label { Left = 16, Top = 56, Width = 80, Text = "Usuário" };
        _user.Left = 100;
        _user.Top = 52;
        _user.Width = 280;

        var passLabel = new Label { Left = 16, Top = 92, Width = 80, Text = "Senha" };
        _password.Left = 100;
        _password.Top = 88;
        _password.Width = 280;
        _password.UseSystemPasswordChar = true;

        var ok = new Button
        {
            Text = "Entrar",
            Left = 220,
            Top = 140,
            Width = 80,
            DialogResult = DialogResult.OK
        };
        var cancel = new Button
        {
            Text = "Cancelar",
            Left = 308,
            Top = 140,
            Width = 80,
            DialogResult = DialogResult.Cancel
        };

        AcceptButton = ok;
        CancelButton = cancel;
        Controls.AddRange(new Control[] { host, userLabel, _user, passLabel, _password, ok, cancel });
    }

    public string UserName => _user.Text.Trim();

    public char[] CopyPassword()
    {
        var copy = _password.Text.ToCharArray();
        _password.Clear();
        return copy;
    }
}

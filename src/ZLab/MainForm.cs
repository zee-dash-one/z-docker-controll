using ZLab.Services;
using ZLab.Ui;

namespace ZLab;

internal sealed class MainForm : Form
{
    private readonly ServerMonitor _monitor;
    private readonly StatusWatcher _watcher;
    private readonly Func<Task> _wakeAsync;
    private readonly Func<Task> _loginAsync;
    private readonly Action _endSession;
    private readonly Func<Task> _exitAsync;

    private readonly HeaderBar _header = new();
    private readonly EmptyStatePanel _empty = new();
    private readonly DashboardPanel _dashboard = new();
    private readonly Panel _content = new();
    private readonly Panel _output = new();
    private readonly Label _outputTitle = new();
    private readonly Label _outputLine = new();
    private readonly System.Windows.Forms.Timer _probeTimer = new();

    private SessionState _sessionState = SessionState.Disconnected;
    private bool _busy;
    private bool _waking;
    private bool _serverResponds;
    private string _lastOutput = "";

    public MainForm(
        ServerMonitor monitor,
        StatusWatcher watcher,
        Func<Task> wakeAsync,
        Func<Task> loginAsync,
        Action endSession,
        Func<Task> exitAsync,
        Icon icon)
    {
        _monitor = monitor;
        _watcher = watcher;
        _wakeAsync = wakeAsync;
        _loginAsync = loginAsync;
        _endSession = endSession;
        _exitAsync = exitAsync;

        Text = "Z-Lab";
        Icon = icon;
        Font = Tokens.Body();
        ClientSize = new Size(1280, 800);
        MinimumSize = new Size(1024, 700);
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;

        BuildChrome();
        WireEvents();

        ThemeManager.ThemeChanged += OnTheme;
        _watcher.SnapshotUpdated += OnSnapshot;
        ApplyThemeColors();
        ApplySessionState(SessionState.Disconnected);
        _ = ProbeServerAsync();
        _probeTimer.Interval = 10_000;
        _probeTimer.Tick += async (_, _) => await ProbeServerAsync();
        _probeTimer.Start();

        FormClosing += (_, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        };
    }

    public static string StatusText(ContainerStatusKind status) => status switch
    {
        ContainerStatusKind.Up => "No ar",
        ContainerStatusKind.Down => "Parado",
        ContainerStatusKind.Absent => "Ausente",
        ContainerStatusKind.Busy => "Ocupado",
        ContainerStatusKind.Error => "Erro",
        _ => "Sem sessão"
    };

    public void SetSessionState(SessionState state) => ApplySessionState(state);

    public void SetWaking(bool waking)
    {
        _waking = waking;
        if (waking)
            Note("Pacote enviado. Aguardando a porta 22 (até 2 minutos)…");
        ApplySessionState(_sessionState);
    }

    public void Note(string text)
    {
        if (IsDisposed)
            return;
        if (InvokeRequired)
        {
            BeginInvoke(() => Note(text));
            return;
        }

        _lastOutput = DateTime.Now.ToString("HH:mm:ss") + "  " + text;
        _outputLine.Text = _lastOutput;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ThemeManager.ThemeChanged -= OnTheme;
            _watcher.SnapshotUpdated -= OnSnapshot;
            _probeTimer.Stop();
            _probeTimer.Dispose();
        }

        base.Dispose(disposing);
    }

    private void BuildChrome()
    {
        _header.Dock = DockStyle.Top;

        _output.Dock = DockStyle.Bottom;
        _output.Height = LogicalToDeviceUnits(40);
        _outputTitle.Text = "Saída";
        _outputTitle.Font = Tokens.Caption();
        _outputTitle.AutoSize = true;
        _outputLine.Font = Tokens.Caption();
        _outputLine.AutoSize = false;
        _outputLine.AutoEllipsis = true;
        _output.Controls.Add(_outputTitle);
        _output.Controls.Add(_outputLine);
        _output.Paint += (_, e) =>
        {
            var p = ThemeManager.Current;
            using var brush = new SolidBrush(p.Surface);
            e.Graphics.FillRectangle(brush, _output.ClientRectangle);
            using var pen = new Pen(p.Stroke);
            e.Graphics.DrawLine(pen, 0, 0, _output.Width, 0);
        };
        _output.Layout += (_, _) =>
        {
            var pad = LogicalToDeviceUnits(24);
            _outputTitle.Location = new Point(pad, (_output.Height - _outputTitle.PreferredHeight) / 2);
            var lineX = _outputTitle.Right + LogicalToDeviceUnits(16);
            _outputLine.SetBounds(
                lineX,
                (_output.Height - _outputLine.PreferredHeight) / 2,
                Math.Max(40, _output.Width - lineX - pad),
                _outputLine.PreferredHeight);
        };

        _content.Dock = DockStyle.Fill;
        _empty.Dock = DockStyle.Fill;
        _dashboard.Dock = DockStyle.Fill;
        _content.Controls.Add(_dashboard);
        _content.Controls.Add(_empty);

        Controls.Add(_content);
        Controls.Add(_output);
        Controls.Add(_header);
    }

    private void WireEvents()
    {
        _header.EnterButton.Click += async (_, _) =>
        {
            try { await _loginAsync(); }
            catch (Exception ex) { Note(ex.Message); }
        };
        _header.EndSessionButton.Click += (_, _) => _endSession();
        _header.ThemeButton.Click += (_, _) =>
        {
            var next = ThemeManager.Current == Palette.Dark ? ThemeMode.Light : ThemeMode.Dark;
            ThemeManager.Apply(next);
        };
        _header.CloseButton.Click += async (_, _) => await _exitAsync();

        _empty.WakeButton.Click += async (_, _) => await _wakeAsync();
        _dashboard.WakeButton.Click += async (_, _) => await _wakeAsync();
        _dashboard.RebootButton.Click += async (_, _) => await PowerAsync(shutdown: false);
        _dashboard.ShutdownButton.Click += async (_, _) => await PowerAsync(shutdown: true);

        var gap = new Padding(0, 0, LogicalToDeviceUnits(8), 0);
        _dashboard.WakeButton.Margin = gap;
        _dashboard.RebootButton.Margin = gap;
    }

    private async Task PowerAsync(bool shutdown)
    {
        var confirm = MessageBox.Show(
            shutdown ? RemoteCommands.ShutdownConfirm : RemoteCommands.RebootConfirm,
            shutdown ? "Z-Lab — Desligar" : "Z-Lab — Reiniciar",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (confirm != DialogResult.Yes)
            return;

        await MutateAsync(shutdown ? "Desligar" : "Reiniciar", () =>
            shutdown
                ? _monitor.ShutdownAsync(CancellationToken.None)
                : _monitor.RebootAsync(CancellationToken.None));
        _endSession();
    }

    private async Task MutateAsync(string verb, Func<Task<RemoteCommandResult>> action)
    {
        if (_busy)
            return;
        _busy = true;
        ApplySessionState(_sessionState);
        try
        {
            var result = await action().ConfigureAwait(true);
            Note(verb + " — código " + result.ExitCode);
            var text = result.CombinedText();
            if (text.Length > 0)
                Note(text);
            if (!result.Succeeded)
            {
                MessageBox.Show(
                    text.Length == 0 ? "O comando remoto falhou." : text,
                    "Z-Lab",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
        finally
        {
            _busy = false;
            ApplySessionState(_sessionState);
        }
    }

    private void OnSnapshot(object? sender, StatusSnapshotEventArgs e)
    {
        if (IsDisposed)
            return;
        BeginInvoke(() =>
        {
            if (_sessionState == SessionState.Connecting)
                return;

            if (e.Connected)
            {
                _serverResponds = true;
                ApplySessionState(SessionState.Connected);
            }
            else if (_sessionState == SessionState.Connected)
            {
                ApplySessionState(SessionState.Disconnected);
            }
        });
    }

    private void ApplySessionState(SessionState state)
    {
        _sessionState = state;
        var responds = state == SessionState.Connected || _serverResponds;
        var flags = SessionUiRules.For(state, responds, _busy || _waking);

        _header.EnterButton.Enabled = flags.EnterEnabled;
        _header.EnterButton.Text = flags.EnterText;
        _header.EnterButton.Variant = flags.EnterPrimary ? ZButtonVariant.Primary : ZButtonVariant.Secondary;
        _header.EndSessionButton.Enabled = flags.EndSessionEnabled;

        var userHost = (_monitor.Session.UserName ?? "") + "@" + RemoteCommands.Host;
        _header.SetSession(state == SessionState.Connected, userHost);

        _empty.Visible = flags.ShowEmpty;
        _dashboard.Visible = flags.ShowDashboard;
        _dashboard.SetEnergyVisible(flags.EnergyVisible);
        _dashboard.WakeButton.Enabled = flags.WakeEnabled;
        _dashboard.RebootButton.Enabled = flags.RebootEnabled;
        _dashboard.ShutdownButton.Enabled = flags.ShutdownEnabled;

        _empty.SetServerResponds(_serverResponds);
        _empty.WakeButton.Enabled = !_busy && !_waking && !_serverResponds;
    }

    private async Task ProbeServerAsync()
    {
        if (_sessionState == SessionState.Connected)
            return;
        try
        {
            var open = await PortProbe.IsOpenAsync(
                RemoteCommands.Host, RemoteCommands.Port, 1500, CancellationToken.None).ConfigureAwait(true);
            if (IsDisposed)
                return;
            if (InvokeRequired)
            {
                BeginInvoke(() =>
                {
                    _serverResponds = open;
                    ApplySessionState(_sessionState);
                });
            }
            else
            {
                _serverResponds = open;
                ApplySessionState(_sessionState);
            }
        }
        catch
        {
            // probe failures count as offline
        }
    }

    private void OnTheme(object? sender, EventArgs e) => ApplyThemeColors();

    private void ApplyThemeColors()
    {
        var p = ThemeManager.Current;
        BackColor = p.Bg;
        _content.BackColor = p.Bg;
        _output.BackColor = p.Surface;
        _outputTitle.ForeColor = p.Text3;
        _outputTitle.BackColor = p.Surface;
        _outputLine.ForeColor = p.Text2;
        _outputLine.BackColor = p.Surface;
        Invalidate(true);
    }
}

using ZLab.Services;

namespace ZLab;

internal sealed class MainForm : Form
{
    private readonly ServerMonitor _monitor;
    private readonly StatusWatcher _watcher;
    private readonly Func<Task> _wakeAsync;
    private readonly Func<Task> _loginAsync;
    private readonly Action _endSession;
    private readonly Func<Task> _exitAsync;
    private readonly Dictionary<string, Card> _cards = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ContainerStatusKind> _statuses = new(StringComparer.Ordinal);
    private readonly TextBox _log = new();
    private readonly Label _session = new();
    private readonly TextBox _uptime = new();
    private readonly TextBox _load = new();
    private readonly TextBox _memory = new();
    private readonly TextBox _diskRoot = new();
    private readonly TextBox _diskDocker = new();
    private readonly TextBox _diskData = new();
    private readonly Button _liga = new();
    private readonly Button _desligar = new();
    private readonly Button _reiniciar = new();
    private readonly Button _encerrar = new();
    private bool _busy;
    private bool _waking;
    private bool _connected;

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
        Font = new Font("Segoe UI", 9F);
        ClientSize = new Size(980, 740);
        StartPosition = FormStartPosition.CenterScreen;

        var top = BuildTop();
        top.Dock = DockStyle.Top;
        top.Height = 248;

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(8),
            WrapContents = true
        };
        foreach (var service in RemoteCommands.Services)
        {
            var card = BuildCard(service);
            _cards[service.Id] = card;
            _statuses[service.Id] = ContainerStatusKind.Unknown;
            flow.Controls.Add(card.Group);
        }

        var logHost = new Panel { Dock = DockStyle.Bottom, Height = 160 };
        var logLabel = new Label { Text = "Saída", Dock = DockStyle.Top, Height = 20 };
        _log.Dock = DockStyle.Fill;
        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Vertical;
        _log.Font = new Font("Consolas", 9F);
        logHost.Controls.Add(_log);
        logHost.Controls.Add(logLabel);

        Controls.Add(flow);
        Controls.Add(logHost);
        Controls.Add(top);

        FormClosing += (_, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        };

        _watcher.SnapshotUpdated += OnSnapshot;
        ApplyConnection(false);
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

    public void SetWaking(bool waking)
    {
        _waking = waking;
        ApplyConnection(_connected);
        if (waking)
            _session.Text = "Pacote enviado. Aguardando a porta 22 (até 2 minutos)…";
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

        _log.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + text + Environment.NewLine);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _watcher.SnapshotUpdated -= OnSnapshot;
        base.Dispose(disposing);
    }

    private Panel BuildTop()
    {
        var panel = new Panel();
        _session.Left = 12;
        _session.Top = 8;
        _session.Width = 640;
        _session.Text = "Sem sessão";

        _liga.Text = "Liga";
        _liga.SetBounds(12, 32, 90, 28);
        _liga.Click += async (_, _) => await _wakeAsync();

        _desligar.Text = "Desligar";
        _desligar.SetBounds(108, 32, 90, 28);
        _desligar.Click += async (_, _) => await PowerAsync(shutdown: true);

        _reiniciar.Text = "Reiniciar";
        _reiniciar.SetBounds(204, 32, 90, 28);
        _reiniciar.Click += async (_, _) => await PowerAsync(shutdown: false);

        var entrar = new Button { Text = "Entrar", Bounds = new Rectangle(300, 32, 90, 28) };
        entrar.Click += async (_, _) => await _loginAsync();

        _encerrar.Text = "Encerrar sessão";
        _encerrar.SetBounds(396, 32, 130, 28);
        _encerrar.Click += (_, _) => _endSession();

        var sair = new Button { Text = "Sair", Bounds = new Rectangle(532, 32, 80, 28) };
        sair.Click += async (_, _) => await _exitAsync();

        AddReading(panel, "Tempo no ar", _uptime, 0);
        AddReading(panel, "Carga", _load, 1);
        AddReading(panel, "Memória", _memory, 2);
        AddReading(panel, "Disco /", _diskRoot, 3);
        AddReading(panel, "Disco Docker", _diskDocker, 4);
        AddReading(panel, "Disco /data", _diskData, 5);

        panel.Controls.AddRange(new Control[] { _session, _liga, _desligar, _reiniciar, entrar, _encerrar, sair });
        return panel;
    }

    private static void AddReading(Control parent, string caption, TextBox box, int row)
    {
        var top = 68 + (row * 28);
        parent.Controls.Add(new Label { Text = caption, Left = 12, Top = top + 4, Width = 90 });
        box.Left = 104;
        box.Top = top;
        box.Width = 840;
        box.Height = 24;
        box.ReadOnly = true;
        parent.Controls.Add(box);
    }

    private Card BuildCard(RemoteService service)
    {
        var group = new GroupBox
        {
            Text = service.DisplayName,
            Width = 340,
            Height = 200,
            Margin = new Padding(8)
        };
        var status = new Label { Left = 12, Top = 24, Width = 270, Text = "Sem sessão" };
        var meta = new Label
        {
            Left = 12,
            Top = 46,
            Width = 270,
            Height = 32,
            Text = service.LanUrl ?? service.HttpsLabel ?? ""
        };

        var subir = Make("Subir", 12, 88, async () => await MutateAsync(service, "Subir", () => _monitor.StartAsync(service.Id, CancellationToken.None)));
        var parar = Make("Parar", 96, 88, async () => await MutateAsync(service, "Parar", () => _monitor.StopAsync(service.Id, CancellationToken.None)));
        var criar = Make("Criar", 180, 88, async () => await MutateAsync(service, "Criar", () => _monitor.CreateAsync(service.Id, CancellationToken.None)));
        var atualizar = Make("Atualizar", 12, 124, async () => await UpdateAsync(service));
        var logs = Make("Logs", 96, 124, async () => await ShowOutputAsync(service.DisplayName + " logs", () => _monitor.LogsAsync(service.Id, CancellationToken.None)));
        var uso = Make("Uso", 180, 124, async () => await ShowOutputAsync(service.DisplayName + " uso", () => _monitor.StatsAsync(service.Id, CancellationToken.None)));
        Button? abrir = null;
        if (service.LanUrl is not null)
        {
            var url = service.LanUrl;
            abrir = Make("Abrir", 264, 124, () =>
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
                return Task.CompletedTask;
            });
            group.Controls.Add(abrir);
        }

        group.Controls.AddRange(new Control[] { status, meta, subir, parar, criar, atualizar, logs, uso });
        return new Card(service, group, status, subir, parar, criar, atualizar, logs, uso, abrir);
    }

    private static Button Make(string text, int x, int y, Func<Task> click)
    {
        var button = new Button { Text = text, Left = x, Top = y, Width = 80, Height = 28 };
        button.Click += async (_, _) =>
        {
            try
            {
                await click();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Z-Lab", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };
        return button;
    }

    private async Task UpdateAsync(RemoteService service)
    {
        var confirm = MessageBox.Show(
            RemoteCommands.UpdateConfirm,
            "Z-Lab — Atualizar",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);
        if (confirm != DialogResult.Yes)
            return;
        await MutateAsync(service, "Atualizar", () => _monitor.UpdateAsync(service.Id, CancellationToken.None));
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

        await MutateAsync(null, shutdown ? "Desligar" : "Reiniciar", () =>
            shutdown ? _monitor.ShutdownAsync(CancellationToken.None) : _monitor.RebootAsync(CancellationToken.None));
    }

    private async Task MutateAsync(RemoteService? service, string verb, Func<Task<RemoteCommandResult>> action)
    {
        if (_busy)
            return;
        _busy = true;
        ApplyButtons();
        try
        {
            var result = await action().ConfigureAwait(true);
            Note(verb + (service is null ? "" : " " + service.DisplayName) + " — código " + result.ExitCode);
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
            ApplyButtons();
        }
    }

    private async Task ShowOutputAsync(string title, Func<Task<RemoteCommandResult>> action)
    {
        if (_busy)
            return;
        _busy = true;
        ApplyButtons();
        try
        {
            var result = await action().ConfigureAwait(true);
            Note(title + " — código " + result.ExitCode);
            Note(result.CombinedText());
        }
        finally
        {
            _busy = false;
            ApplyButtons();
        }
    }

    private void OnSnapshot(object? sender, StatusSnapshotEventArgs e)
    {
        if (IsDisposed)
            return;
        BeginInvoke(() =>
        {
            _connected = e.Connected;
            foreach (var kv in e.Statuses)
                _statuses[kv.Key] = kv.Value;
            if (e.Host is not null)
            {
                _uptime.Text = e.Host.Uptime;
                _load.Text = e.Host.Load;
                _memory.Text = e.Host.Memory;
                _diskRoot.Text = e.Host.DiskRoot;
                _diskDocker.Text = e.Host.DiskDocker;
                _diskData.Text = e.Host.DiskData;
            }
            else if (!e.Connected)
            {
                _uptime.Clear();
                _load.Clear();
                _memory.Clear();
                _diskRoot.Clear();
                _diskDocker.Clear();
                _diskData.Clear();
            }

            ApplyConnection(e.Connected);
        });
    }

    private void ApplyConnection(bool connected)
    {
        _connected = connected;
        if (!_waking)
        {
            _session.Text = connected
                ? "Sessão em " + (_monitor.Session.UserName ?? "") + "@192.168.0.112"
                : "Sem sessão. Com a máquina desligada, só Liga responde.";
        }

        ApplyButtons();
    }

    private void ApplyButtons()
    {
        var session = _connected && !_busy && !_waking;
        _liga.Enabled = !_connected && !_waking && !_busy;
        _desligar.Enabled = session;
        _reiniciar.Enabled = session;
        _encerrar.Enabled = _connected && !_busy;
        foreach (var card in _cards.Values)
        {
            _statuses.TryGetValue(card.Service.Id, out var status);
            card.Status.Text = StatusText(status);
            var up = status == ContainerStatusKind.Up;
            var down = status == ContainerStatusKind.Down;
            var absent = status == ContainerStatusKind.Absent;
            card.Subir.Enabled = session && down;
            card.Parar.Enabled = session && up;
            card.Criar.Enabled = session && absent;
            card.Atualizar.Enabled = session && (up || down || absent);
            card.Logs.Enabled = session && (up || down);
            card.Uso.Enabled = session && up;
            if (card.Abrir is not null)
                card.Abrir.Enabled = session && up;
        }
    }

    private sealed class Card
    {
        public Card(
            RemoteService service,
            GroupBox group,
            Label status,
            Button subir,
            Button parar,
            Button criar,
            Button atualizar,
            Button logs,
            Button uso,
            Button? abrir)
        {
            Service = service;
            Group = group;
            Status = status;
            Subir = subir;
            Parar = parar;
            Criar = criar;
            Atualizar = atualizar;
            Logs = logs;
            Uso = uso;
            Abrir = abrir;
        }

        public RemoteService Service { get; }
        public GroupBox Group { get; }
        public Label Status { get; }
        public Button Subir { get; }
        public Button Parar { get; }
        public Button Criar { get; }
        public Button Atualizar { get; }
        public Button Logs { get; }
        public Button Uso { get; }
        public Button? Abrir { get; }
    }
}

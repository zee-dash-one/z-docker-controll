using Renci.SshNet.Common;
using ZLab.Services;

namespace ZLab;

/// <summary>
/// Tray shell. Exit does not stop containers on z-server-01.
/// </summary>
internal sealed class TrayApp : IDisposable
{
    public const string ExitConfirmText =
        "Sair do Z-Lab? Os containers no z-server-01 continuam em execução.";

    private readonly SshSession _session;
    private readonly ServerMonitor _monitor;
    private readonly StatusWatcher _watcher;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly Icon _icon;
    private MainForm? _mainForm;
    private bool _exiting;
    private bool _disposed;
    private bool _waking;

    public TrayApp(SshSession session, ServerMonitor monitor, StatusWatcher watcher)
    {
        _session = session;
        _monitor = monitor;
        _watcher = watcher;
        _session.AuthFailed += (_, _) =>
        {
            try
            {
                _mainForm?.Note("A autenticação falhou. A sessão foi encerrada.");
                _mainForm?.SetSessionState(SessionState.Disconnected);
            }
            catch
            {
                // tray can outlive the form
            }
        };

        _icon = LoadIcon();
        _menu = new ContextMenuStrip();
        _notifyIcon = new NotifyIcon
        {
            Icon = _icon,
            Text = "Z-Lab",
            Visible = true,
            ContextMenuStrip = _menu
        };
        _notifyIcon.DoubleClick += (_, _) => ShowPanel();
        _menu.Opening += (_, _) => RebuildMenu();
        _watcher.TransitionDetected += OnTransition;
        _watcher.SnapshotUpdated += OnSnapshot;
        _watcher.Start();
    }

    public void Run() => Application.Run();

    public void ShowPanel()
    {
        if (_mainForm is null || _mainForm.IsDisposed)
        {
            _mainForm = new MainForm(_monitor, _watcher, WakeAsync, LoginAsync, EndSession, RequestExitAsync, _icon);
            _mainForm.FormClosed += (_, _) => { _mainForm = null; };
        }

        _mainForm.Show();
        _mainForm.WindowState = FormWindowState.Normal;
        _mainForm.BringToFront();
        _mainForm.Activate();
    }

    public Task<bool> RequestExitAsync()
    {
        if (_exiting)
            return Task.FromResult(false);

        var confirm = MessageBox.Show(
            ExitConfirmText,
            "Z-Lab — Fechar",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);
        if (confirm != DialogResult.Yes)
            return Task.FromResult(false);

        _exiting = true;
        ExitProcess();
        return Task.FromResult(true);
    }

    private void ExitProcess()
    {
        _watcher.Stop();
        _session.EndSession();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        Application.Exit();
    }

    private async Task WakeAsync()
    {
        if (_waking || _session.IsConnected)
            return;
        _waking = true;
        _mainForm?.SetWaking(true);
        try
        {
            var sent = await Task.Run(() => WakeOnLan.Send()).ConfigureAwait(true);
            _mainForm?.Note("Magic packet enviado: " + sent + " bytes para 192.168.0.255:9.");
            var up = await PortProbe.WaitUntilOpenAsync(
                RemoteCommands.Host,
                RemoteCommands.Port,
                TimeSpan.FromMinutes(2),
                TimeSpan.FromSeconds(5),
                CancellationToken.None).ConfigureAwait(true);
            if (!up)
            {
                _mainForm?.Note("A porta 22 não respondeu em 2 minutos. A tarefa 08 da BIOS (Wake-on-LAN) ainda está aberta.");
                MessageBox.Show(
                    "O pacote foi enviado, mas a porta 22 não respondeu em 2 minutos. A placa precisa estar armada (tarefa 08 da BIOS).",
                    "Z-Lab — Ligar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            _mainForm?.Note("Porta 22 aceitando conexão.");
            await LoginAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Z-Lab — Ligar", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _waking = false;
            _mainForm?.SetWaking(false);
        }
    }

    private async Task LoginAsync()
    {
        if (_session.IsConnected)
            return;

        _mainForm?.SetSessionState(SessionState.Connecting);

        var open = await PortProbe.IsOpenAsync(RemoteCommands.Host, RemoteCommands.Port, 3000, CancellationToken.None)
            .ConfigureAwait(true);
        if (!open)
        {
            _mainForm?.SetSessionState(SessionState.Disconnected);
            MessageBox.Show(
                "A porta 22 não está aceitando conexão. Use Ligar servidor se a máquina estiver desligada.",
                "Z-Lab",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var dialog = new LoginForm();
        if (dialog.ShowDialog(_mainForm) != DialogResult.OK)
        {
            _mainForm?.SetSessionState(SessionState.Disconnected);
            return;
        }

        var password = dialog.CopyPassword();
        try
        {
            if (dialog.UserName.Length == 0 || password.Length == 0)
            {
                _mainForm?.SetSessionState(SessionState.Disconnected);
                MessageBox.Show("Informe usuário e senha.", "Z-Lab", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var knownHosts = ReadKnownHosts();
            await Task.Run(() => _session.Start(dialog.UserName, password, knownHosts)).ConfigureAwait(true);
            _mainForm?.Note("Sessão aberta em " + dialog.UserName + "@192.168.0.112.");
            _mainForm?.SetSessionState(SessionState.Connected);
            if (_watcher.LastStatuses.Count == 0)
                _watcher.Start();
        }
        catch (SshAuthenticationException)
        {
            _mainForm?.SetSessionState(SessionState.Disconnected);
            _mainForm?.Note("Falha na autenticação.");
            MessageBox.Show(
                "Falha na autenticação. Confira usuário e senha. O sshd do z-server-01 precisa aceitar senha.",
                "Z-Lab",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            _mainForm?.SetSessionState(SessionState.Disconnected);
            var reason = _session.LastReject;
            _mainForm?.Note(string.IsNullOrWhiteSpace(reason) ? ex.Message : reason);
            MessageBox.Show(
                string.IsNullOrWhiteSpace(reason) ? ex.Message : reason,
                "Z-Lab",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        finally
        {
            Array.Clear(password, 0, password.Length);
        }
    }

    private void EndSession()
    {
        _session.EndSession();
        _mainForm?.Note("Sessão encerrada. A senha foi zerada.");
        _mainForm?.SetSessionState(SessionState.Disconnected);
    }

    private void RebuildMenu()
    {
        _menu.Items.Clear();
        _menu.Items.Add("Abrir painel", null, (_, _) => ShowPanel());
        _menu.Items.Add(new ToolStripSeparator());

        var statuses = _watcher.LastStatuses;
        foreach (var service in RemoteCommands.Services)
        {
            var item = service;
            var sub = new ToolStripMenuItem(item.DisplayName);
            statuses.TryGetValue(item.Id, out var st);
            sub.DropDownItems.Add(new ToolStripMenuItem("Status: " + MainForm.StatusText(st)) { Enabled = false });
            sub.DropDownItems.Add("Subir", null, async (_, _) =>
            {
                await RunMenuAsync(() => _monitor.StartAsync(item.Id, CancellationToken.None));
            });
            sub.DropDownItems.Add("Parar", null, async (_, _) =>
            {
                await RunMenuAsync(() => _monitor.StopAsync(item.Id, CancellationToken.None));
            });
            _menu.Items.Add(sub);
        }

        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Ligar servidor", null, async (_, _) => await WakeAsync());
        _menu.Items.Add("Encerrar sessão", null, (_, _) => EndSession());
        _menu.Items.Add("Fechar Z-Lab", null, async (_, _) => await RequestExitAsync());
        UpdateTooltip(statuses);
    }

    private async Task RunMenuAsync(Func<Task<RemoteCommandResult>> action)
    {
        try
        {
            var result = await action().ConfigureAwait(true);
            if (!result.Succeeded)
            {
                MessageBox.Show(
                    result.CombinedText(),
                    "Z-Lab",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Z-Lab", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnSnapshot(object? sender, StatusSnapshotEventArgs e)
    {
        try
        {
            if (_notifyIcon.Visible)
                UpdateTooltip(e.Statuses);
        }
        catch
        {
            // ignore tooltip failures
        }
    }

    private void UpdateTooltip(IReadOnlyDictionary<string, ContainerStatusKind> statuses)
    {
        var parts = new List<string>();
        foreach (var service in RemoteCommands.Services)
        {
            var shortName = service.Id switch
            {
                "9router" => "9R",
                "omniroute" => "Omni",
                "mausbot" => "Maus",
                _ => service.Id.Length <= 6 ? service.Id : service.Id[..6]
            };
            parts.Add(statuses.TryGetValue(service.Id, out var st)
                ? shortName + ":" + MainForm.StatusText(st)
                : shortName + ":…");
        }

        var tip = "Z-Lab — " + string.Join(" ", parts);
        if (tip.Length > 63)
            tip = tip[..60] + "…";
        _notifyIcon.Text = tip;
    }

    private void OnTransition(object? sender, StatusNotifyEventArgs e)
    {
        var text = e.Kind switch
        {
            TransitionNotifyKind.Caiu => e.DisplayName + " caiu",
            TransitionNotifyKind.Subiu => e.DisplayName + " subiu",
            _ => e.DisplayName
        };
        try
        {
            _notifyIcon.ShowBalloonTip(4000, "Z-Lab", text, ToolTipIcon.Info);
        }
        catch
        {
            // ignore
        }
    }

    private static string ReadKnownHosts()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".ssh",
            "known_hosts");
        return File.Exists(path) ? File.ReadAllText(path) : "";
    }

    private static Icon LoadIcon()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "z-lab.ico");
        if (File.Exists(path))
            return new Icon(path);
        return SystemIcons.Application;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _watcher.TransitionDetected -= OnTransition;
        _watcher.SnapshotUpdated -= OnSnapshot;
        _watcher.Dispose();
        _session.Dispose();
        try
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
        catch
        {
            // already disposed on ExitProcess
        }

        _menu.Dispose();
        _icon.Dispose();
        _mainForm?.Dispose();
    }
}

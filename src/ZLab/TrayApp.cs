using ZLab.Services;

namespace ZLab;

/// <summary>
/// NotifyIcon lifecycle, tray menu, Exit stop-all flow.
/// </summary>
internal sealed class TrayApp : IDisposable
{
    public const string ExitConfirmText =
        "Parar os serviços gerenciados e sair do Z-Lab? Se algum não parar, o app não fecha.";

    private readonly ServiceCatalogStore _store;
    private readonly ContainerService _containers;
    private readonly BackupService _backups;
    private readonly LanExposeHelper _lan;
    private readonly LocalHostsHelper _localHosts;
    private readonly StatusWatcher _watcher;
    private readonly ScheduledBackupService _scheduledBackups;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly Icon _icon;
    private MainForm? _mainForm;
    private bool _exiting;
    private bool _disposed;

    public TrayApp(
        ServiceCatalogStore store,
        ContainerService containers,
        BackupService backups,
        LanExposeHelper lan,
        LocalHostsHelper localHosts,
        StatusWatcher watcher,
        ScheduledBackupService scheduledBackups)
    {
        _store = store;
        _containers = containers;
        _backups = backups;
        _lan = lan;
        _localHosts = localHosts;
        _watcher = watcher;
        _scheduledBackups = scheduledBackups;

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
        _scheduledBackups.Completed += OnAutoBackupCompleted;
        _watcher.Start();
        _scheduledBackups.Start();
        _ = DiscoverDockerAsync();
    }

    public void Run()
    {
        Application.Run();
    }

    public void ShowPanel()
    {
        if (_mainForm is null || _mainForm.IsDisposed)
        {
            _mainForm = new MainForm(_store, _containers, _backups, _lan, _localHosts, _watcher, _icon, RequestExitAsync);
            _mainForm.FormClosed += (_, _) => { _mainForm = null; };
        }

        _mainForm.Show();
        _mainForm.WindowState = FormWindowState.Normal;
        _mainForm.BringToFront();
        _mainForm.Activate();
    }

    public async Task<bool> RequestExitAsync()
    {
        if (_exiting)
            return false;

        var confirm = MessageBox.Show(
            ExitConfirmText,
            "Z-Lab — Sair",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);

        if (confirm != DialogResult.Yes)
            return false;

        _exiting = true;
        try
        {
            var doc = _store.Document;
            if (doc is null)
            {
                ExitProcess();
                return true;
            }

            var targets = doc.Services
                .Where(s => s.Managed && s.Enabled)
                .ToList();

            var failures = new List<string>();
            foreach (var item in targets)
            {
                var result = await _containers.StopAsync(item).ConfigureAwait(true);
                if (!result.Ok)
                    failures.Add($"{item.DisplayName}: {result.Message}");
            }

            if (failures.Count > 0)
            {
                MessageBox.Show(
                    "Não foi possível parar todos os serviços. O Z-Lab permanece aberto.\n\n"
                    + string.Join("\n", failures),
                    "Z-Lab — Erro ao sair",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            ExitProcess();
            return true;
        }
        finally
        {
            _exiting = false;
        }
    }

    private void ExitProcess()
    {
        _scheduledBackups.Stop();
        _watcher.Stop();
        _notifyIcon.Visible = false;
        Application.Exit();
    }

    private void RebuildMenu()
    {
        _menu.Items.Clear();
        _menu.Items.Add("Abrir painel", null, (_, _) => ShowPanel());
        _menu.Items.Add(new ToolStripSeparator());

        var doc = _store.Document;
        var statuses = _watcher.LastStatuses;
        if (doc is not null)
        {
            foreach (var item in doc.Services.Where(s => s.Enabled))
            {
                var service = item;
                var sub = new ToolStripMenuItem(service.DisplayName);

                statuses.TryGetValue(service.Id, out var st);
                var statusItem = new ToolStripMenuItem(
                    "Status: " + (statuses.ContainsKey(service.Id)
                        ? MainForm.StatusText(st)
                        : "…"))
                {
                    Enabled = false
                };
                sub.DropDownItems.Add(statusItem);
                sub.DropDownItems.Add(new ToolStripSeparator());

                sub.DropDownItems.Add("Iniciar", null, async (_, _) =>
                {
                    await _containers.StartAsync(service).ConfigureAwait(true);
                    _mainForm?.RequestRefresh();
                });
                sub.DropDownItems.Add("Parar", null, async (_, _) =>
                {
                    await _containers.StopAsync(service).ConfigureAwait(true);
                    _mainForm?.RequestRefresh();
                });
                sub.DropDownItems.Add("Reiniciar", null, async (_, _) =>
                {
                    await _containers.RestartAsync(service).ConfigureAwait(true);
                    _mainForm?.RequestRefresh();
                });
                _menu.Items.Add(sub);
            }
        }

        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Sair", null, async (_, _) => await RequestExitAsync());
        UpdateTooltip();
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
            // Ignore tooltip failures
        }
    }

    private void UpdateTooltip(IReadOnlyDictionary<string, ContainerStatusKind>? statuses = null)
    {
        statuses ??= _watcher.LastStatuses;
        var doc = _store.Document;
        if (doc is null)
        {
            SetNotifyText("Z-Lab");
            return;
        }

        var parts = new List<string>();
        foreach (var item in doc.Services.Where(s => s.Enabled))
        {
            var shortName = TooltipShortName(item.Id, item.DisplayName);
            if (statuses.TryGetValue(item.Id, out var st))
                parts.Add($"{shortName}: {MainForm.StatusText(st)}");
            else
                parts.Add($"{shortName}: …");
        }

        var tip = parts.Count == 0
            ? "Z-Lab"
            : "Z-Lab — " + string.Join(" | ", parts);
        SetNotifyText(tip);
    }

    private void SetNotifyText(string text)
    {
        // NotifyIcon.Text max 63 chars
        if (text.Length > 63)
            text = text[..60] + "…";
        _notifyIcon.Text = text;
    }

    private static string TooltipShortName(string id, string displayName) => id switch
    {
        "9router" => "9R",
        "omniroute" => "Omni",
        _ => displayName.Length <= 8 ? displayName : displayName[..8]
    };

    private void OnAutoBackupCompleted(object? sender, AutoBackupEventArgs e)
    {
        try
        {
            _notifyIcon.ShowBalloonTip(
                5000,
                "Z-Lab — Backup diário",
                e.Message,
                e.Ok ? ToolTipIcon.Info : ToolTipIcon.Warning);
        }
        catch
        {
            // Ignore balloon failures
        }

        _mainForm?.RefreshAutoBackupLabels();
    }

    private void OnTransition(object? sender, StatusNotifyEventArgs e)
    {
        var title = "Z-Lab";
        var text = e.Kind switch
        {
            TransitionNotifyKind.Caiu => $"{e.DisplayName} caiu",
            TransitionNotifyKind.Subiu => $"{e.DisplayName} subiu",
            _ => e.DisplayName
        };

        try
        {
            _notifyIcon.ShowBalloonTip(4000, title, text, ToolTipIcon.Info);
        }
        catch
        {
            // Ignore balloon failures on headless/unsupported hosts
        }

        _mainForm?.RequestRefresh();
    }

    private async Task DiscoverDockerAsync()
    {
        try
        {
            await _containers.EnsureDiscoveredAsync().ConfigureAwait(true);
            _mainForm?.RequestRefresh();
        }
        catch
        {
            // Discovery errors surface when ops run
        }
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
        _scheduledBackups.Completed -= OnAutoBackupCompleted;
        _scheduledBackups.Dispose();
        _watcher.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
        _icon.Dispose();
        _mainForm?.Dispose();
    }
}

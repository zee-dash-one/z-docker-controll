using System.Diagnostics;
using System.Text;
using ZLab.Models;
using ZLab.Services;

namespace ZLab;

/// <summary>
/// Operator panel: enabled cards + disabled catalog + logs (PT-BR).
/// </summary>
internal sealed class MainForm : Form
{
    private static readonly Color BrandOrange = Color.FromArgb(0xE6, 0x7A, 0x2E);
    private static readonly Color ChromeBg = Color.FromArgb(0xF3, 0xF4, 0xF6);
    private static readonly Color Surface = Color.White;
    private static readonly Color SurfaceMuted = Color.FromArgb(0xE5, 0xE7, 0xEB);
    private static readonly Color Border = Color.FromArgb(0xD1, 0xD5, 0xDB);
    private static readonly Color TextPrimary = Color.FromArgb(0x11, 0x18, 0x27);
    private static readonly Color TextMuted = Color.FromArgb(0x6B, 0x72, 0x80);
    private static readonly Color StatusUp = Color.FromArgb(0x15, 0x80, 0x3D);
    private static readonly Color StatusDown = Color.FromArgb(0xB4, 0x53, 0x09);
    private static readonly Color StatusAbsent = TextMuted;
    private static readonly Color StatusBusy = BrandOrange;
    private static readonly Color StatusError = Color.FromArgb(0xB9, 0x1C, 0x1C);

    private readonly ServiceCatalogStore _store;
    private readonly ContainerService _containers;
    private readonly BackupService _backups;
    private readonly LanExposeHelper _lan;
    private readonly LocalHostsHelper _localHosts;
    private readonly NineRouterTunnelHelper _nineTunnel;
    private readonly StatusWatcher _watcher;
    private readonly Func<Task<bool>> _requestExit;

    private readonly Label _modeLabel;
    private readonly Label _backupFolderLabel;
    private readonly Panel _activeHost;
    private readonly Panel _catalogHost;
    private readonly FlowLayoutPanel _activeFlow;
    private readonly FlowLayoutPanel _catalogFlow;
    private readonly TextBox _logsBox;
    private readonly TextBox _commandLogBox;
    private readonly TabControl _logsTabs;
    private readonly StringBuilder _commandLog = new();
    private const int CommandLogMaxChars = 200_000;
    private readonly Label _activeEmptyLabel;
    private readonly Dictionary<string, ContainerStatusKind> _statuses = new(StringComparer.Ordinal);
    private readonly HashSet<string> _busyIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ActiveCardRef> _activeCards = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Control> _catalogRows = new(StringComparer.Ordinal);

    private bool _refreshQueued;

    private const int ActiveCardWidth = 380;
    private const int ActionButtonCols = 3;
    private const int ActionButtonHeight = 30;
    private const int ActionButtonRowGap = 4;

    private sealed class ActiveCardRef
    {
        public required string Id { get; init; }
        public required Panel Card { get; init; }
        public required Label StatusLabel { get; init; }
        public Label? AutoBackupLabel { get; init; }
        public required List<Button> ActionButtons { get; init; }
        public Button? StatsButton { get; init; }
    }

    public MainForm(
        ServiceCatalogStore store,
        ContainerService containers,
        BackupService backups,
        LanExposeHelper lan,
        LocalHostsHelper localHosts,
        NineRouterTunnelHelper nineTunnel,
        StatusWatcher watcher,
        Icon icon,
        Func<Task<bool>> requestExit)
    {
        _store = store;
        _containers = containers;
        _backups = backups;
        _lan = lan;
        _localHosts = localHosts;
        _nineTunnel = nineTunnel;
        _watcher = watcher;
        _requestExit = requestExit;

        Text = "Z-Lab";
        Icon = icon;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(980, 720);
        Size = new Size(1200, 840);
        BackColor = ChromeBg;
        Font = new Font("Segoe UI", 9f);
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 52)); // Serviços ativos — scroll
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 22)); // Catálogo — scroll
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 26)); // Logs

        var header = new Panel { Dock = DockStyle.Fill };
        var title = new Label
        {
            Text = "Z-Lab",
            Font = new Font("Segoe UI Semibold", 14f),
            ForeColor = TextPrimary,
            AutoSize = true,
            Location = new Point(0, 4)
        };
        _modeLabel = new Label
        {
            Text = "Modo: …",
            ForeColor = Color.FromArgb(0x1D, 0x4E, 0xD8),
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        header.Controls.Add(title);
        header.Controls.Add(_modeLabel);
        header.Resize += (_, _) =>
        {
            _modeLabel.Location = new Point(header.ClientSize.Width - _modeLabel.Width - 4, 8);
        };
        root.Controls.Add(header, 0, 0);

        var tools = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true
        };
        _backupFolderLabel = new Label
        {
            AutoSize = true,
            ForeColor = TextMuted,
            Margin = new Padding(0, 8, 8, 0)
        };
        var btnChangeBackup = MakeButton("Alterar", async () => await ChangeBackupFolderAsync());
        var btnOpenBackup = MakeButton("Abrir pasta", () => { OpenBackupFolder(); return Task.CompletedTask; });
        var btnBackupAll = MakeButton("Backup todos", async () => await BackupAllAsync());
        var btnOpenConfig = MakeButton("Config", () => { OpenConfigFolder(); return Task.CompletedTask; });
        var btnLocalHosts = MakeButton("Hosts locais", async () => await ConfigureLocalHostsAsync());
        var btnReload = MakeButton("Recarregar", async () => await ReloadAsync());
        var btnExit = MakeButton("Sair", async () => await _requestExit());
        tools.Controls.Add(_backupFolderLabel);
        tools.Controls.Add(btnChangeBackup);
        tools.Controls.Add(btnOpenBackup);
        tools.Controls.Add(btnBackupAll);
        tools.Controls.Add(btnOpenConfig);
        tools.Controls.Add(btnLocalHosts);
        tools.Controls.Add(btnReload);
        tools.Controls.Add(btnExit);
        root.Controls.Add(tools, 0, 1);

        root.Controls.Add(SectionLabel("Serviços ativos"), 0, 2);

        // Host scrolls; flow grows with content (classic WinForms scroll pattern).
        _activeHost = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = ChromeBg };
        _activeFlow = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(4),
            Location = Point.Empty,
            BackColor = ChromeBg
        };
        _activeEmptyLabel = new Label
        {
            Text = "Nenhum serviço ativo. Ative um item no Catálogo (desativados) abaixo.",
            ForeColor = TextMuted,
            AutoSize = true,
            Padding = new Padding(4)
        };
        _activeHost.Controls.Add(_activeFlow);
        _activeHost.Resize += (_, _) => SyncFlowToHost(_activeHost, _activeFlow);
        root.Controls.Add(_activeHost, 0, 3);

        root.Controls.Add(SectionLabel("Catálogo (desativados)"), 0, 4);

        _catalogHost = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = ChromeBg };
        _catalogFlow = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(4),
            Location = Point.Empty,
            BackColor = ChromeBg
        };
        _catalogHost.Controls.Add(_catalogFlow);
        _catalogHost.Resize += (_, _) => SyncFlowToHost(_catalogHost, _catalogFlow);
        root.Controls.Add(_catalogHost, 0, 5);

        var logsHost = new Panel { Dock = DockStyle.Fill };
        _logsTabs = new TabControl { Dock = DockStyle.Fill };

        var tabContainer = new TabPage("Container");
        _logsBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            Font = new Font("Consolas", 9f),
            BackColor = Surface,
            ForeColor = TextPrimary,
            WordWrap = false
        };
        tabContainer.Controls.Add(_logsBox);

        var tabCommands = new TabPage("Comandos");
        var commandsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(0)
        };
        commandsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        commandsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var cmdTools = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        var btnClearCmd = MakeButton("Limpar", () =>
        {
            ClearCommandLog();
            return Task.CompletedTask;
        });
        cmdTools.Controls.Add(btnClearCmd);
        _commandLogBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            Font = new Font("Consolas", 9f),
            BackColor = Surface,
            ForeColor = TextPrimary,
            WordWrap = false
        };
        commandsLayout.Controls.Add(cmdTools, 0, 0);
        commandsLayout.Controls.Add(_commandLogBox, 0, 1);
        tabCommands.Controls.Add(commandsLayout);

        _logsTabs.TabPages.Add(tabContainer);
        _logsTabs.TabPages.Add(tabCommands);
        logsHost.Controls.Add(_logsTabs);
        root.Controls.Add(logsHost, 0, 6);

        Controls.Add(root);

        FormClosing += OnFormClosing;
        _watcher.SnapshotUpdated += OnSnapshot;
        _containers.Cli.CommandExecuted += OnCommandExecuted;
        Shown += (_, _) =>
        {
            UpdateModeLabel();
            UpdateBackupLabel();
            SyncFlowToHost(_activeHost, _activeFlow);
            SyncFlowToHost(_catalogHost, _catalogFlow);
            RebuildUi();
            _ = RefreshStatusesAsync();
        };
    }

    private static void SyncFlowToHost(Panel host, FlowLayoutPanel flow)
    {
        if (host.IsDisposed || flow.IsDisposed)
            return;
        var w = Math.Max(280, host.ClientSize.Width - 4);
        if (host.VerticalScroll.Visible)
            w = Math.Max(280, host.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4);
        flow.SuspendLayout();
        flow.Width = w;
        flow.MaximumSize = new Size(w, 0);
        flow.MinimumSize = new Size(w, 0);
        flow.ResumeLayout(true);
    }

    public void RequestRefresh()
    {
        if (IsDisposed)
            return;
        if (InvokeRequired)
        {
            BeginInvoke(RequestRefresh);
            return;
        }

        if (_refreshQueued)
            return;
        _refreshQueued = true;
        BeginInvoke(async () =>
        {
            _refreshQueued = false;
            UpdateModeLabel();
            UpdateBackupLabel();
            await RefreshStatusesAsync();
        });
    }

    public void RequestStructuralRefresh()
    {
        if (IsDisposed)
            return;
        if (InvokeRequired)
        {
            BeginInvoke(RequestStructuralRefresh);
            return;
        }

        UpdateModeLabel();
        UpdateBackupLabel();
        RebuildUi();
        _ = RefreshStatusesAsync();
    }

    /// <summary>
    /// Refresh per-card last-auto-backup labels after the daily job finishes.
    /// </summary>
    public void RefreshAutoBackupLabels()
    {
        if (IsDisposed)
            return;
        if (InvokeRequired)
        {
            BeginInvoke(RefreshAutoBackupLabels);
            return;
        }

        ApplyAutoBackupLabels();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
    }

    private void OnSnapshot(object? sender, StatusSnapshotEventArgs e)
    {
        if (IsDisposed)
            return;
        BeginInvoke(() =>
        {
            foreach (var kv in e.Statuses)
                _statuses[kv.Key] = kv.Value;
            ApplyStatuses();
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _watcher.SnapshotUpdated -= OnSnapshot;
            _containers.Cli.CommandExecuted -= OnCommandExecuted;
        }
        base.Dispose(disposing);
    }

    private void OnCommandExecuted(object? sender, CommandAuditEventArgs e)
    {
        if (IsDisposed)
            return;
        if (InvokeRequired)
        {
            BeginInvoke(() => OnCommandExecuted(sender, e));
            return;
        }

        AppendCommandAudit(e);
    }

    private void AppendCommandAudit(CommandAuditEventArgs e)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[{e.Timestamp:HH:mm:ss}] ({e.ModeLabel}) $ {e.FormatCommandLine()}");
        sb.AppendLine($"exit={e.ExitCode}");
        if (!string.IsNullOrWhiteSpace(e.StdOut))
        {
            sb.AppendLine("--- stdout ---");
            sb.AppendLine(e.StdOut);
        }

        if (!string.IsNullOrWhiteSpace(e.StdErr))
        {
            sb.AppendLine("--- stderr ---");
            sb.AppendLine(e.StdErr);
        }

        sb.AppendLine();

        _commandLog.Append(sb);
        if (_commandLog.Length > CommandLogMaxChars)
            _commandLog.Remove(0, _commandLog.Length - CommandLogMaxChars);

        _commandLogBox.Text = _commandLog.ToString();
        _commandLogBox.SelectionStart = _commandLogBox.TextLength;
        _commandLogBox.ScrollToCaret();
    }

    private void ClearCommandLog()
    {
        _commandLog.Clear();
        _commandLogBox.Clear();
    }

    private void AppendCommandNote(string note)
    {
        _commandLog.AppendLine($"[{DateTimeOffset.Now:HH:mm:ss}] {note}");
        _commandLog.AppendLine();
        _commandLogBox.Text = _commandLog.ToString();
        _commandLogBox.SelectionStart = _commandLogBox.TextLength;
        _commandLogBox.ScrollToCaret();
    }

    private void ShowCommandLogTab()
    {
        if (_logsTabs.TabPages.Count > 1)
            _logsTabs.SelectedIndex = 1;
    }

    private void ShowContainerLogTab()
    {
        if (_logsTabs.TabPages.Count > 0)
            _logsTabs.SelectedIndex = 0;
    }

    private async Task RefreshStatusesAsync()
    {
        var doc = _store.Document;
        if (doc is null)
            return;

        foreach (var item in doc.Services.Where(s => s.Enabled))
        {
            try
            {
                var st = await _containers.GetStatusAsync(item).ConfigureAwait(true);
                _statuses[item.Id] = st;
            }
            catch
            {
                _statuses[item.Id] = ContainerStatusKind.Error;
            }
        }

        ApplyStatuses();
    }

    private void RebuildUi()
    {
        var doc = _store.Document;
        _activeFlow.SuspendLayout();
        _catalogFlow.SuspendLayout();
        _activeFlow.Controls.Clear();
        _catalogFlow.Controls.Clear();
        _activeCards.Clear();
        _catalogRows.Clear();

        if (doc is null)
        {
            _activeFlow.ResumeLayout();
            _catalogFlow.ResumeLayout();
            return;
        }

        var enabled = doc.Services.Where(s => s.Enabled).ToList();
        var disabled = doc.Services.Where(s => !s.Enabled).ToList();

        if (enabled.Count == 0)
            _activeFlow.Controls.Add(_activeEmptyLabel);
        else
        {
            foreach (var item in enabled)
            {
                var cardRef = BuildActiveCard(item);
                _activeCards[item.Id] = cardRef;
                _activeFlow.Controls.Add(cardRef.Card);
            }
        }

        foreach (var item in disabled)
        {
            var row = BuildCatalogRow(item);
            _catalogRows[item.Id] = row;
            _catalogFlow.Controls.Add(row);
        }

        _activeFlow.ResumeLayout(true);
        _catalogFlow.ResumeLayout(true);
        SyncFlowToHost(_activeHost, _activeFlow);
        SyncFlowToHost(_catalogHost, _catalogFlow);
        ApplyStatuses();
    }

    private void ApplyStatuses()
    {
        foreach (var kv in _activeCards)
        {
            var id = kv.Key;
            var cardRef = kv.Value;
            var busy = _busyIds.Contains(id);
            _statuses.TryGetValue(id, out var status);
            if (busy)
                status = ContainerStatusKind.Busy;

            cardRef.StatusLabel.Text = StatusText(status);
            cardRef.StatusLabel.ForeColor = StatusColor(status);

            foreach (var btn in cardRef.ActionButtons)
            {
                if (ReferenceEquals(btn, cardRef.StatsButton))
                    btn.Enabled = !busy && status == ContainerStatusKind.Up;
                else
                    btn.Enabled = !busy;
            }
        }

        ApplyAutoBackupLabels();
    }

    private void ApplyAutoBackupLabels()
    {
        var doc = _store.Document;
        var backupRoot = doc?.Settings.BackupFolder;
        foreach (var kv in _activeCards)
        {
            var cardRef = kv.Value;
            if (cardRef.AutoBackupLabel is null)
                continue;

            var item = doc?.Services.FirstOrDefault(s =>
                string.Equals(s.Id, kv.Key, StringComparison.Ordinal));
            cardRef.AutoBackupLabel.Text = ResolveAutoBackupLabel(item, backupRoot);
        }
    }

    private static string ResolveAutoBackupLabel(ServiceItem? item, string? backupRoot)
    {
        if (item is null || !BackupFolderNaming.IsBackupEligible(item.Backup?.Mode))
            return "";

        if (BackupFolderNaming.TryParseLastAutoBackupAt(item.LastAutoBackupAt, out _))
            return BackupFolderNaming.FormatLastAutoBackupLabel(item.LastAutoBackupAt);

        var fromDisk = BackupFolderNaming.TryReadAutoSlotTimestamp(backupRoot, item.Id);
        if (fromDisk is { } when)
            return "Backup auto: " + when.ToLocalTime().ToString("dd/MM/yyyy - HH:mm");

        return BackupFolderNaming.FormatLastAutoBackupLabel(null);
    }

    private ActiveCardRef BuildActiveCard(ServiceItem item)
    {
        var busy = _busyIds.Contains(item.Id);
        _statuses.TryGetValue(item.Id, out var status);
        if (busy)
            status = ContainerStatusKind.Busy;

        var actionButtons = new List<Button>();
        Button? statsButton = null;
        var pendingActions = new List<(string Text, Func<Task> Action, bool IsStats)>();

        void Queue(string text, Func<Task> action, bool isStats = false) =>
            pendingActions.Add((text, action, isStats));

        Queue("Iniciar", async () =>
        {
            var r = await _containers.StartAsync(item);
            if (!r.Ok) ShowError(r.Message);
        });
        Queue("Parar", async () =>
        {
            var r = await _containers.StopAsync(item);
            if (!r.Ok) ShowError(r.Message);
        });
        Queue("Reiniciar", async () =>
        {
            var r = await _containers.RestartAsync(item);
            if (!r.Ok) ShowError(r.Message);
        });
        Queue("Logs", async () =>
        {
            var lines = _store.Document?.Settings.LogTailLines ?? 80;
            var r = await _containers.GetLogsAsync(item, lines);
            if (!r.Ok)
            {
                ShowError(r.Message);
                return;
            }

            var text = r.Output ?? "";
            _logsBox.Text = string.IsNullOrWhiteSpace(text)
                ? "(sem linhas de log)"
                : text;
            ShowContainerLogTab();
        });
        Queue("Stats", async () =>
        {
            var r = await _containers.GetStatsAsync(item);
            if (!r.Ok)
            {
                ShowError(r.Message);
                return;
            }

            _logsBox.Text = r.Output ?? "";
            ShowContainerLogTab();
        }, isStats: true);
        Queue("Atualizar", async () =>
        {
            var confirm = MessageBox.Show(
                $"Atualizar {item.DisplayName}? Isso fará pull da imagem e recriará o container preservando volumes.",
                "Z-Lab — Atualizar",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes)
                return;

            AppendCommandNote($"—— Atualizar {item.DisplayName} ——");
            ShowCommandLogTab();
            var r = await _containers.UpdateAsync(item);
            if (!r.Ok)
            {
                ShowError(r.Message);
            }
            else
            {
                ShowInfo(string.IsNullOrWhiteSpace(r.Message)
                    ? $"{item.DisplayName} atualizado."
                    : r.Message);
                if (string.Equals(item.Id, "9router", StringComparison.Ordinal))
                {
                    var reenable = MessageBox.Show(
                        "Reativar o tunnel Cloudflare do 9Router agora?\n"
                        + "(O Update recria o container e o tunnel cai.)",
                        "Z-Lab — Tunnel 9Router",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button1);
                    if (reenable == DialogResult.Yes)
                        await EnableNineRouterTunnelAsync(item);
                }
            }
            ShowCommandLogTab();
        });

        if (!string.IsNullOrWhiteSpace(item.OpenUrl))
        {
            Queue("Abrir", () =>
            {
                if (!ContainerService.TryOpenUrl(item, out var err))
                    ShowError(err ?? "Falha ao abrir URL.");
                return Task.CompletedTask;
            });
        }

        if (BackupFolderNaming.IsBackupEligible(item.Backup?.Mode))
            Queue("Backup", async () => await BackupOneAsync(item));

        if (item.ExposeLan)
        {
            Queue("Expor LAN", async () =>
            {
                var r = await _lan.ExposeAsync(item);
                if (!r.Ok) ShowError(r.Message);
                else ShowInfo(r.Message);
            });
        }

        if (string.Equals(item.Id, "9router", StringComparison.Ordinal))
        {
            Queue("Tunnel", async () => await EnableNineRouterTunnelAsync(item));
            Queue("Tunnel status", async () =>
            {
                AppendCommandNote($"—— Tunnel status {item.DisplayName} ——");
                ShowCommandLogTab();
                var r = await _nineTunnel.GetStatusAsync(item);
                if (!r.Ok)
                {
                    ShowError(r.Message);
                    return;
                }

                var detail = r.Message;
                if (!string.IsNullOrWhiteSpace(r.TunnelUrl))
                    detail += "\n" + r.TunnelUrl;
                if (!string.IsNullOrWhiteSpace(r.PublicUrl))
                    detail += "\n" + r.PublicUrl;
                ShowInfo(detail);
            });
        }

        Queue("Desativar", async () =>
        {
            _store.SetEnabled(item.Id, false);
            _watcher.ResetBaselineFor(item.Id);
            RequestStructuralRefresh();
            await Task.CompletedTask;
        });

        var actionRows = (pendingActions.Count + ActionButtonCols - 1) / ActionButtonCols;
        const int headerHeight = 94;
        var actionsHeight = actionRows * (ActionButtonHeight + ActionButtonRowGap) + 8;
        var cardHeight = headerHeight + actionsHeight + 12;

        var card = new Panel
        {
            Width = ActiveCardWidth,
            Height = cardHeight,
            Margin = new Padding(6),
            BackColor = Surface,
            Padding = new Padding(10),
            BorderStyle = BorderStyle.FixedSingle
        };

        var name = new Label
        {
            Text = item.DisplayName,
            Font = new Font("Segoe UI Semibold", 11f),
            ForeColor = TextPrimary,
            AutoSize = true,
            Location = new Point(8, 6)
        };
        var statusLabel = new Label
        {
            Text = StatusText(status),
            ForeColor = StatusColor(status),
            AutoSize = true,
            Location = new Point(8, 30)
        };

        Label? autoBackupLabel = null;
        if (BackupFolderNaming.IsBackupEligible(item.Backup?.Mode))
        {
            autoBackupLabel = new Label
            {
                Text = ResolveAutoBackupLabel(item, _store.Document?.Settings.BackupFolder),
                ForeColor = TextMuted,
                AutoSize = true,
                Location = new Point(8, 50)
            };
        }

        var meta = new Label
        {
            Text = item.HostPort > 0 ? $"{item.Category} · :{item.HostPort}" : item.Category,
            ForeColor = TextMuted,
            AutoSize = true,
            Location = new Point(8, autoBackupLabel is null ? 52 : 70)
        };

        var btnWidth = (ActiveCardWidth - 28 - (ActionButtonCols - 1) * 4) / ActionButtonCols;
        var actions = new FlowLayoutPanel
        {
            Location = new Point(6, headerHeight),
            Size = new Size(ActiveCardWidth - 20, actionsHeight),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        foreach (var (text, action, isStats) in pendingActions)
        {
            var b = MakeButton(text, async () =>
            {
                if (!_busyIds.Add(item.Id))
                    return;
                ApplyStatuses();
                try
                {
                    await action();
                }
                finally
                {
                    _busyIds.Remove(item.Id);
                    ApplyStatuses();
                    await RefreshStatusesAsync();
                }
            }, manageEnabled: false);
            b.Width = btnWidth;
            b.Height = ActionButtonHeight;
            b.Margin = new Padding(2);
            b.AutoSize = false;
            b.Enabled = !busy && (!isStats || status == ContainerStatusKind.Up);
            actions.Controls.Add(b);
            actionButtons.Add(b);
            if (isStats)
                statsButton = b;
        }

        card.Controls.Add(name);
        card.Controls.Add(statusLabel);
        if (autoBackupLabel is not null)
            card.Controls.Add(autoBackupLabel);
        card.Controls.Add(meta);
        card.Controls.Add(actions);

        return new ActiveCardRef
        {
            Id = item.Id,
            Card = card,
            StatusLabel = statusLabel,
            AutoBackupLabel = autoBackupLabel,
            ActionButtons = actionButtons,
            StatsButton = statsButton
        };
    }

    private Control BuildCatalogRow(ServiceItem item)
    {
        const int rowWidth = 340;
        var row = new Panel
        {
            Width = rowWidth,
            Height = 72,
            Margin = new Padding(6),
            BackColor = SurfaceMuted,
            Padding = new Padding(8),
            BorderStyle = BorderStyle.FixedSingle
        };

        var btn = MakeButton("Ativar", async () =>
        {
            _store.SetEnabled(item.Id, true);
            _watcher.ResetBaselineFor(item.Id);
            MessageBox.Show(
                "Serviço ativado. Use Iniciar para criar/subir o container se ainda não existir.",
                "Z-Lab",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            RequestStructuralRefresh();
            await Task.CompletedTask;
        });
        btn.AutoSize = true;
        btn.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        var name = new Label
        {
            Text = item.DisplayName,
            Font = new Font("Segoe UI Semibold", 10f),
            ForeColor = TextPrimary,
            AutoSize = true,
            Location = new Point(8, 8)
        };
        var badge = new Label
        {
            Text = "Desativado — Ative para controlar este serviço.",
            ForeColor = TextMuted,
            AutoSize = false,
            Location = new Point(8, 34),
            Height = 28
        };

        row.Controls.Add(name);
        row.Controls.Add(badge);
        row.Controls.Add(btn);

        void LayoutCatalog()
        {
            btn.Location = new Point(row.ClientSize.Width - btn.Width - 8, 8);
            badge.Width = Math.Max(80, row.ClientSize.Width - 24);
            name.MaximumSize = new Size(Math.Max(80, btn.Left - 16), 0);
        }

        row.Resize += (_, _) => LayoutCatalog();
        LayoutCatalog();
        return row;
    }

    private async Task BackupOneAsync(ServiceItem item)
    {
        var warn = MessageBox.Show(
            "Backups podem conter senhas, chaves e dados sensíveis. Continuar?",
            "Z-Lab — Aviso de secrets",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (warn != DialogResult.Yes)
            return;

        var folder = await EnsureBackupFolderAsync();
        if (folder is null)
            return;

        if (!TryPromptBackupName(out var namingMode, out var customName))
            return;

        var result = await _backups.BackupAsync(
            item,
            folder,
            attemptColdFallback: true,
            confirmColdFallback: () =>
            {
                var r = MessageBox.Show(
                    "Backup a quente falhou. Parar o container, fazer backup e iniciar novamente?",
                    "Z-Lab — Backup a frio",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                return Task.FromResult(r == DialogResult.Yes);
            },
            namingMode: namingMode,
            customName: customName);

        if (result.Ok)
        {
            var tip = result.OutputPath is not null
                ? $"Backup ok: {result.OutputPath}\n\n(OmniRoute: procure storage.sqlite; 9Router: data.sqlite)"
                : "Backup ok.";
            ShowInfo(tip);
        }
        else
            ShowError(result.Message);
    }

    private async Task BackupAllAsync()
    {
        var doc = _store.Document;
        if (doc is null)
            return;

        var warn = MessageBox.Show(
            "Backups podem conter senhas, chaves e dados sensíveis. Executar backup de todos os elegíveis?",
            "Z-Lab — Aviso de secrets",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (warn != DialogResult.Yes)
            return;

        var folder = await EnsureBackupFolderAsync();
        if (folder is null)
            return;

        if (!TryPromptBackupName(out var namingMode, out var customName))
            return;

        var results = await _backups.BackupAllEligibleAsync(
            doc.Services,
            folder,
            confirmColdFallback: item =>
            {
                var r = MessageBox.Show(
                    $"Backup a quente de {item.DisplayName} falhou. Parar → Backup → Iniciar?",
                    "Z-Lab — Backup a frio",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                return Task.FromResult(r == DialogResult.Yes);
            },
            namingMode: namingMode,
            customName: customName);

        var ok = results.Count(r => r.Ok);
        var fail = results.Count(r => !r.Ok && !r.Skipped);
        var skipped = results.Count(r => r.Skipped);
        ShowInfo($"Backup concluído. Ok: {ok}, Falha: {fail}, Ignorados: {skipped}.");
    }

    /// <summary>
    /// Prompts for optional backup name. Empty → ManualStamp; otherwise ManualNamed.
    /// Returns false if the operator cancelled.
    /// </summary>
    private bool TryPromptBackupName(out BackupNamingMode namingMode, out string? customName)
    {
        namingMode = BackupNamingMode.ManualStamp;
        customName = null;

        using var dlg = new Form
        {
            Text = "Z-Lab — Nome do backup",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(420, 140)
        };
        var label = new Label
        {
            Text = "Nome (opcional). Vazio = carimbo data/hora.",
            AutoSize = true,
            Location = new Point(12, 12)
        };
        var box = new TextBox
        {
            Location = new Point(12, 40),
            Width = 390
        };
        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(246, 90),
            Width = 75
        };
        var cancel = new Button
        {
            Text = "Cancelar",
            DialogResult = DialogResult.Cancel,
            Location = new Point(327, 90),
            Width = 75
        };
        dlg.Controls.Add(label);
        dlg.Controls.Add(box);
        dlg.Controls.Add(ok);
        dlg.Controls.Add(cancel);
        dlg.AcceptButton = ok;
        dlg.CancelButton = cancel;

        if (dlg.ShowDialog(this) != DialogResult.OK)
            return false;

        var raw = box.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(raw))
            return true;

        var sanitized = BackupFolderNaming.SanitizeCustomName(raw);
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            ShowError("Nome de backup inválido.");
            return false;
        }

        namingMode = BackupNamingMode.ManualNamed;
        customName = sanitized;
        return true;
    }

    private Task<string?> EnsureBackupFolderAsync()
    {
        var doc = _store.Document;
        var current = doc?.Settings.BackupFolder ?? "";
        if (!string.IsNullOrWhiteSpace(current) && Directory.Exists(current))
            return Task.FromResult<string?>(current);

        using var dlg = new FolderBrowserDialog
        {
            Description = "Escolha a pasta de backup do Z-Lab",
            UseDescriptionForTitle = true
        };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return Task.FromResult<string?>(null);

        if (doc is not null)
        {
            doc.Settings.BackupFolder = dlg.SelectedPath;
            _store.Save(doc);
            UpdateBackupLabel();
        }

        return Task.FromResult<string?>(dlg.SelectedPath);
    }

    private Task ChangeBackupFolderAsync()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Alterar pasta de backup do Z-Lab",
            UseDescriptionForTitle = true
        };
        var doc = _store.Document;
        if (doc is not null && !string.IsNullOrWhiteSpace(doc.Settings.BackupFolder)
            && Directory.Exists(doc.Settings.BackupFolder))
            dlg.SelectedPath = doc.Settings.BackupFolder;

        if (dlg.ShowDialog(this) != DialogResult.OK || doc is null)
            return Task.CompletedTask;

        doc.Settings.BackupFolder = dlg.SelectedPath;
        _store.Save(doc);
        UpdateBackupLabel();
        return Task.CompletedTask;
    }

    private async Task ReloadAsync()
    {
        var ok = _store.TryReload(out var error);
        if (!ok)
        {
            ShowError("Falha ao recarregar o catálogo. Mantendo estado anterior.\n\n" + (error ?? ""));
            return;
        }

        foreach (var s in _store.Document?.Services ?? Enumerable.Empty<ServiceItem>())
            _watcher.ResetBaselineFor(s.Id);

        UpdateBackupLabel();
        RebuildUi();
        await RefreshStatusesAsync();
        ShowInfo("Catálogo recarregado.");
    }

    private async Task EnableNineRouterTunnelAsync(ServiceItem item)
    {
        AppendCommandNote($"—— Tunnel enable {item.DisplayName} ——");
        ShowCommandLogTab();
        var r = await _nineTunnel.EnableAsync(item);
        if (!r.Ok)
        {
            ShowError(r.Message);
            return;
        }

        var detail = r.Message;
        if (!string.IsNullOrWhiteSpace(r.TunnelUrl))
            detail += "\n" + r.TunnelUrl;
        if (!string.IsNullOrWhiteSpace(r.PublicUrl))
            detail += "\n" + r.PublicUrl;
        ShowInfo(detail);
    }

    private async Task ConfigureLocalHostsAsync()
    {
        var doc = _store.Document;
        if (doc?.Services is null || doc.Services.Count == 0)
        {
            ShowError("Catálogo vazio.");
            return;
        }

        var aliases = LocalHostsHelper.CollectAliases(doc.Services);
        if (aliases.Count == 0)
        {
            ShowError(
                "Nenhum serviço com localHost no catálogo.\n"
                + "Defina localHost (ex.: 9router-local) em services.json.");
            return;
        }

        var lines = string.Join(
            Environment.NewLine,
            aliases.Select(a => $"  127.0.0.1  {a.HostName}  →  {a.OpenUrl}"));
        var confirm = MessageBox.Show(
            "Isso configura aliases no hosts do Windows para isolar cookies entre os gateways:"
            + Environment.NewLine + Environment.NewLine
            + lines
            + Environment.NewLine + Environment.NewLine
            + "Pode pedir permissão de Administrador (UAC)."
            + Environment.NewLine
            + "Depois o botão Abrir usará esses nomes."
            + Environment.NewLine + Environment.NewLine
            + "Continuar?",
            "Z-Lab — Hosts locais",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button1);
        if (confirm != DialogResult.Yes)
            return;

        var result = await _localHosts.EnsureAsync(doc.Services).ConfigureAwait(true);
        if (!result.Ok)
        {
            ShowError(result.Message);
            return;
        }

        LocalHostsHelper.ApplyOpenUrls(doc.Services, aliases);
        try
        {
            _store.Save(doc);
        }
        catch (Exception ex)
        {
            ShowError("Hosts ok, mas falhou ao salvar openUrl no catálogo:\n" + ex.Message);
            return;
        }

        RebuildUi();
        var urlList = string.Join(Environment.NewLine, result.OpenUrls.Select(u => "  " + u));
        ShowInfo(
            (result.AlreadyConfigured
                ? "Aliases já estavam no hosts. openUrl atualizado."
                : result.Message)
            + Environment.NewLine + Environment.NewLine
            + "URLs:"
            + Environment.NewLine
            + urlList);
    }

    private void OpenBackupFolder()
    {
        var folder = _store.Document?.Settings.BackupFolder;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            ShowError("Pasta de backup não definida ou inexistente. Use Alterar para escolher uma pasta.");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ShowError("Não foi possível abrir a pasta de backup.\n" + ex.Message);
        }
    }

    private void OpenConfigFolder()
    {
        var dir = _store.GetConfigDirectory();
        try
        {
            Directory.CreateDirectory(dir);
            Process.Start(new ProcessStartInfo
            {
                FileName = dir,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ShowError("Não foi possível abrir a pasta de configuração.\n" + ex.Message);
        }
    }

    private void UpdateModeLabel()
    {
        var mode = _containers.Cli.IsAvailable
            ? _containers.Cli.ModeLabel
            : (_containers.Cli.UnavailableReason is null ? "…" : "indisponível");
        _modeLabel.Text = $"Modo: {mode}";
        if (_modeLabel.Parent is not null)
            _modeLabel.Location = new Point(_modeLabel.Parent.ClientSize.Width - _modeLabel.Width - 4, 8);
    }

    private void UpdateBackupLabel()
    {
        var folder = _store.Document?.Settings.BackupFolder;
        _backupFolderLabel.Text = string.IsNullOrWhiteSpace(folder)
            ? "Pasta backup: (não definida)"
            : "Pasta backup: " + folder;
    }

    private static Label SectionLabel(string text) =>
        new()
        {
            Text = text,
            Font = new Font("Segoe UI Semibold", 10f),
            ForeColor = TextPrimary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

    private static Button MakeButton(string text, Func<Task> onClick, bool manageEnabled = true)
    {
        var b = new Button
        {
            Text = text,
            AutoSize = true,
            Margin = new Padding(2),
            FlatStyle = FlatStyle.System
        };
        b.Click += async (_, _) =>
        {
            if (manageEnabled)
                b.Enabled = false;
            try
            {
                await onClick();
            }
            finally
            {
                if (manageEnabled)
                    b.Enabled = true;
            }
        };
        return b;
    }

    internal static string StatusText(ContainerStatusKind status) => status switch
    {
        ContainerStatusKind.Up => "Up",
        ContainerStatusKind.Down => "Parado",
        ContainerStatusKind.Absent => "Ausente",
        ContainerStatusKind.Busy => "Em andamento…",
        ContainerStatusKind.Error => "Erro",
        _ => "—"
    };

    private static Color StatusColor(ContainerStatusKind status) => status switch
    {
        ContainerStatusKind.Up => StatusUp,
        ContainerStatusKind.Down => StatusDown,
        ContainerStatusKind.Absent => StatusAbsent,
        ContainerStatusKind.Busy => StatusBusy,
        ContainerStatusKind.Error => StatusError,
        _ => TextMuted
    };

    private static void ShowError(string message) =>
        MessageBox.Show(message, "Z-Lab", MessageBoxButtons.OK, MessageBoxIcon.Error);

    private static void ShowInfo(string message) =>
        MessageBox.Show(message, "Z-Lab", MessageBoxButtons.OK, MessageBoxIcon.Information);
}

namespace ZLab.Services;

public sealed class ServerPoll
{
    public required bool Connected { get; init; }
    public required IReadOnlyDictionary<string, ContainerStatusKind> Statuses { get; init; }
}

/// <summary>
/// Runs the closed command list on one SSH session.
/// </summary>
public sealed class ServerMonitor
{
    private readonly SshSession _session;
    private readonly MonitorSettingsStore _settings;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public ServerMonitor(SshSession session, MonitorSettingsStore settings)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public SshSession Session => _session;

    public bool IsConnected => _session.IsConnected;

    public int PollSeconds => _settings.Current.PollSeconds > 0 ? _settings.Current.PollSeconds : 8;

    public async Task<ServerPoll> PollAsync(CancellationToken cancellationToken)
    {
        if (!_session.IsConnected)
        {
            return new ServerPoll
            {
                Connected = false,
                Statuses = UnknownStatuses()
            };
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var statuses = new Dictionary<string, ContainerStatusKind>(StringComparer.Ordinal);
            foreach (var service in RemoteCommands.Services)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var ps = await Task.Run(
                    () => _session.Run(RemoteCommands.Ps(service.Id), TimeSpan.FromSeconds(30)),
                    cancellationToken).ConfigureAwait(false);
                statuses[service.Id] = ps.Succeeded
                    ? ComposePsParser.Parse(ps.StdOut)
                    : ContainerStatusKind.Error;
            }

            return new ServerPoll
            {
                Connected = true,
                Statuses = statuses
            };
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<RemoteCommandResult> StartAsync(string serviceId, CancellationToken cancellationToken) =>
        RunAsync(RemoteCommands.Start(serviceId), TimeSpan.FromMinutes(2), cancellationToken);

    public Task<RemoteCommandResult> StopAsync(string serviceId, CancellationToken cancellationToken) =>
        RunAsync(RemoteCommands.Stop(serviceId), TimeSpan.FromMinutes(2), cancellationToken);

    public Task<RemoteCommandResult> CreateAsync(string serviceId, CancellationToken cancellationToken) =>
        RunAsync(RemoteCommands.Up(serviceId), TimeSpan.FromMinutes(5), cancellationToken);

    public async Task<RemoteCommandResult> UpdateAsync(string serviceId, CancellationToken cancellationToken)
    {
        var pull = await RunAsync(RemoteCommands.Pull(serviceId), TimeSpan.FromMinutes(15), cancellationToken)
            .ConfigureAwait(false);
        if (!pull.Succeeded)
            return pull;
        return await RunAsync(RemoteCommands.Up(serviceId), TimeSpan.FromMinutes(5), cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<RemoteCommandResult> LogsAsync(string serviceId, CancellationToken cancellationToken) =>
        RunAsync(RemoteCommands.Logs(serviceId), TimeSpan.FromSeconds(40), cancellationToken);

    public Task<RemoteCommandResult> StatsAsync(string serviceId, CancellationToken cancellationToken) =>
        RunAsync(RemoteCommands.Stats(serviceId), TimeSpan.FromSeconds(40), cancellationToken);

    public Task<RemoteCommandResult> ShutdownAsync(CancellationToken cancellationToken) =>
        RunPower(RemoteCommands.Shutdown(), cancellationToken);

    public Task<RemoteCommandResult> RebootAsync(CancellationToken cancellationToken) =>
        RunPower(RemoteCommands.Reboot(), cancellationToken);

    private async Task<RemoteCommandResult> RunPower(string command, CancellationToken cancellationToken)
    {
        try
        {
            return await RunAsync(command, TimeSpan.FromSeconds(20), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is System.Net.Sockets.SocketException or Renci.SshNet.Common.SshConnectionException or IOException)
        {
            _session.EndSession();
            return new RemoteCommandResult { ExitCode = 0, StdOut = "Conexão encerrada com o desligamento." };
        }
    }

    private async Task<RemoteCommandResult> RunAsync(string command, TimeSpan timeout, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await Task.Run(() => _session.Run(command, timeout), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static Dictionary<string, ContainerStatusKind> UnknownStatuses()
    {
        var map = new Dictionary<string, ContainerStatusKind>(StringComparer.Ordinal);
        foreach (var service in RemoteCommands.Services)
            map[service.Id] = ContainerStatusKind.Unknown;
        return map;
    }
}

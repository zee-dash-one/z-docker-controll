namespace ZLab.Services;

public sealed class StatusNotifyEventArgs : EventArgs
{
    public required string ServiceId { get; init; }
    public required string DisplayName { get; init; }
    public required TransitionNotifyKind Kind { get; init; }
    public required ContainerStatusKind Status { get; init; }
}

public sealed class StatusSnapshotEventArgs : EventArgs
{
    public required IReadOnlyDictionary<string, ContainerStatusKind> Statuses { get; init; }
    public bool Connected { get; init; }
}

/// <summary>
/// Polls the six remote services and raises transition balloons.
/// </summary>
public sealed class StatusWatcher : IDisposable
{
    private readonly ServerMonitor _monitor;
    private readonly Dictionary<string, ContainerStatusKind?> _baseline = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ContainerStatusKind> _lastStatuses = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private int _pollSeconds = 8;

    public StatusWatcher(ServerMonitor monitor)
    {
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
    }

    public event EventHandler<StatusNotifyEventArgs>? TransitionDetected;
    public event EventHandler<StatusSnapshotEventArgs>? SnapshotUpdated;

    public IReadOnlyDictionary<string, ContainerStatusKind> LastStatuses
    {
        get
        {
            lock (_gate)
                return new Dictionary<string, ContainerStatusKind>(_lastStatuses, StringComparer.Ordinal);
        }
    }

    public void Start()
    {
        lock (_gate)
        {
            if (_loop is not null)
                return;

            _pollSeconds = Math.Max(2, _monitor.PollSeconds);
            _cts = new CancellationTokenSource();
            _loop = Task.Run(() => RunLoopAsync(_cts.Token));
        }
    }

    public void Stop()
    {
        CancellationTokenSource? cts;
        Task? loop;
        lock (_gate)
        {
            cts = _cts;
            loop = _loop;
            _cts = null;
            _loop = null;
        }

        try
        {
            cts?.Cancel();
        }
        catch
        {
            // ignore
        }

        try
        {
            loop?.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // ignore
        }

        cts?.Dispose();
    }

    public void Dispose() => Stop();

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await PollOnceAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // keep polling
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(2, _pollSeconds)), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            _pollSeconds = Math.Max(2, _monitor.PollSeconds);
        }
    }

    public async Task PollOnceAsync(CancellationToken cancellationToken = default)
    {
        ServerPoll poll;
        try
        {
            poll = await _monitor.PollAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            poll = new ServerPoll
            {
                Connected = false,
                Statuses = RemoteCommands.Services.ToDictionary(
                    s => s.Id,
                    _ => ContainerStatusKind.Error,
                    StringComparer.Ordinal)
            };
        }

        foreach (var service in RemoteCommands.Services)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var next = poll.Statuses.TryGetValue(service.Id, out var st)
                ? st
                : ContainerStatusKind.Unknown;

            ContainerStatusKind? previous;
            lock (_gate)
                _baseline.TryGetValue(service.Id, out previous);

            var kind = StatusTransitionDetector.ShouldNotify(previous, next, managed: true, enabled: true);

            lock (_gate)
                _baseline[service.Id] = next;

            if (kind != TransitionNotifyKind.None)
            {
                TransitionDetected?.Invoke(this, new StatusNotifyEventArgs
                {
                    ServiceId = service.Id,
                    DisplayName = service.DisplayName,
                    Kind = kind,
                    Status = next
                });
            }
        }

        lock (_gate)
        {
            _lastStatuses.Clear();
            foreach (var service in RemoteCommands.Services)
            {
                _lastStatuses[service.Id] = poll.Statuses.TryGetValue(service.Id, out var st)
                    ? st
                    : ContainerStatusKind.Unknown;
            }
        }

        SnapshotUpdated?.Invoke(this, new StatusSnapshotEventArgs
        {
            Statuses = LastStatuses,
            Connected = poll.Connected
        });
    }
}

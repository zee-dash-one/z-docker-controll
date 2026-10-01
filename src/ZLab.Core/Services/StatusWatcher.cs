using ZLab.Models;

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
}

/// <summary>
/// Polls container status and raises transition-only notifications for managed+enabled items.
/// </summary>
public sealed class StatusWatcher : IDisposable
{
    private readonly ServiceCatalogStore _store;
    private readonly ContainerService _containers;
    private readonly Dictionary<string, ContainerStatusKind?> _baseline = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ContainerStatusKind> _lastStatuses = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private int _pollSeconds = 8;

    public StatusWatcher(ServiceCatalogStore store, ContainerService containers)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _containers = containers ?? throw new ArgumentNullException(nameof(containers));
    }

    public event EventHandler<StatusNotifyEventArgs>? TransitionDetected;
    public event EventHandler<StatusSnapshotEventArgs>? SnapshotUpdated;

    /// <summary>
    /// Last completed poll snapshot (empty until first poll finishes).
    /// </summary>
    public IReadOnlyDictionary<string, ContainerStatusKind> LastStatuses
    {
        get
        {
            lock (_gate)
                return new Dictionary<string, ContainerStatusKind>(_lastStatuses, StringComparer.Ordinal);
        }
    }

    public void Start(int? pollSeconds = null)
    {
        lock (_gate)
        {
            if (_loop is not null)
                return;

            _pollSeconds = ResolvePollSeconds(pollSeconds);
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

    public void ResetBaselineFor(string serviceId)
    {
        lock (_gate)
        {
            _baseline[serviceId] = null;
        }
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
                // keep polling; UI stays alive
            }

            var delay = TimeSpan.FromSeconds(Math.Max(2, _pollSeconds));
            try
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            // pick up pollSeconds changes after reload
            _pollSeconds = ResolvePollSeconds(null);
        }
    }

    public async Task PollOnceAsync(CancellationToken cancellationToken = default)
    {
        var doc = _store.Document;
        if (doc is null)
            return;

        var snapshot = new Dictionary<string, ContainerStatusKind>(StringComparer.Ordinal);

        foreach (var item in doc.Services)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ContainerStatusKind next;
            try
            {
                next = await _containers.GetStatusAsync(item, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                next = ContainerStatusKind.Error;
            }

            snapshot[item.Id] = next;

            ContainerStatusKind? previous;
            lock (_gate)
            {
                _baseline.TryGetValue(item.Id, out previous);
            }

            var kind = StatusTransitionDetector.ShouldNotify(
                previous,
                next,
                item.Managed,
                item.Enabled);

            lock (_gate)
            {
                _baseline[item.Id] = next;
            }

            if (kind != TransitionNotifyKind.None)
            {
                TransitionDetected?.Invoke(this, new StatusNotifyEventArgs
                {
                    ServiceId = item.Id,
                    DisplayName = item.DisplayName,
                    Kind = kind,
                    Status = next
                });
            }
        }

        // Drop baselines for removed ids
        lock (_gate)
        {
            foreach (var key in _baseline.Keys.Except(snapshot.Keys, StringComparer.Ordinal).ToList())
                _baseline.Remove(key);
        }

        lock (_gate)
        {
            _lastStatuses.Clear();
            foreach (var kv in snapshot)
                _lastStatuses[kv.Key] = kv.Value;
        }

        SnapshotUpdated?.Invoke(this, new StatusSnapshotEventArgs { Statuses = snapshot });
    }

    private int ResolvePollSeconds(int? overrideSeconds)
    {
        if (overrideSeconds is > 0)
            return overrideSeconds.Value;
        var fromSettings = _store.Document?.Settings.PollSeconds ?? 8;
        return fromSettings > 0 ? fromSettings : 8;
    }
}

namespace ZLab.Services;

/// <summary>
/// Polls zlab-stats.sh every 5 s while an SSH session is open.
/// </summary>
public sealed class StatsPoller : IDisposable
{
    private readonly SshSession _session;
    private readonly CpuUsageCalculator _cpu = new();
    private readonly object _gate = new();
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private int _running;
    private bool _failedOnce;
    private bool _disposed;
    private int _force;
    private readonly string _script;

    public StatsPoller(SshSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _script = EmbeddedScripts.ReadStatsScript();
    }

    public event Action<StatsSnapshot>? Snapshot;
    public event Action<string>? ErrorOnce;

    public CpuUsageCalculator Cpu => _cpu;

    public void Start()
    {
        lock (_gate)
        {
            if (_loop is not null)
                return;
            _failedOnce = false;
            _cpu.Reset();
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

        try { cts?.Cancel(); }
        catch { /* ignore */ }

        try { loop?.Wait(TimeSpan.FromSeconds(2)); }
        catch { /* ignore */ }

        cts?.Dispose();
        _cpu.Reset();
    }

    /// <summary>
    /// Asks the loop to run one poll as soon as the current delay ends (or immediately if idle).
    /// </summary>
    public void RequestImmediate()
    {
        Interlocked.Exchange(ref _force, 1);
        lock (_gate)
        {
            // Kick a parallel one-shot if the loop is running and not already polling.
            if (_loop is not null && _session.IsConnected && Interlocked.CompareExchange(ref _running, 1, 0) == 0)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await PollOnceAsync(CancellationToken.None).ConfigureAwait(false);
                    }
                    catch
                    {
                        // surfaced via ErrorOnce inside PollOnceAsync
                    }
                    finally
                    {
                        Interlocked.Exchange(ref _running, 0);
                        Interlocked.Exchange(ref _force, 0);
                    }
                });
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Stop();
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_session.IsConnected && Interlocked.CompareExchange(ref _running, 1, 0) == 0)
                {
                    try
                    {
                        await PollOnceAsync(cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        Interlocked.Exchange(ref _running, 0);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                if (!_failedOnce)
                {
                    _failedOnce = true;
                    ErrorOnce?.Invoke(ex.Message);
                }
            }

            if (Interlocked.Exchange(ref _force, 0) == 1)
                continue;

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        var result = await Task.Run(
            () => _session.RunScript(_script, TimeSpan.FromSeconds(20)),
            cancellationToken).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            if (!_failedOnce)
            {
                _failedOnce = true;
                var msg = result.CombinedText();
                ErrorOnce?.Invoke(msg.Length == 0 ? "Falha ao ler métricas do servidor." : msg);
            }
            return;
        }

        _failedOnce = false;
        var snap = StatsParser.Parse(result.StdOut);
        Snapshot?.Invoke(snap);
    }
}

using ZLab.Models;

namespace ZLab.Services;

public sealed class AutoBackupEventArgs : EventArgs
{
    public required bool Ok { get; init; }
    public required string Message { get; init; }
}

/// <summary>
/// Background daily auto backup into auto/{id} while the app is running.
/// </summary>
public sealed class ScheduledBackupService : IDisposable
{
    private readonly ServiceCatalogStore _store;
    private readonly BackupService _backups;
    private readonly Func<DateTimeOffset> _clock;
    private readonly TimeSpan _pollInterval;
    private readonly TimeSpan _retryCooldown;
    private readonly object _gate = new();
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private DateTimeOffset? _lastFailureAttempt;
    private bool _warnedMissingFolder;
    private bool _disposed;

    public ScheduledBackupService(
        ServiceCatalogStore store,
        BackupService backups,
        Func<DateTimeOffset>? clock = null,
        TimeSpan? pollInterval = null,
        TimeSpan? retryCooldown = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _backups = backups ?? throw new ArgumentNullException(nameof(backups));
        _clock = clock ?? (() => DateTimeOffset.Now);
        _pollInterval = pollInterval ?? TimeSpan.FromMinutes(1);
        _retryCooldown = retryCooldown ?? TimeSpan.FromMinutes(30);
    }

    public event EventHandler<AutoBackupEventArgs>? Completed;

    public void Start()
    {
        lock (_gate)
        {
            if (_loop is not null || _disposed)
                return;
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
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Stop();
    }

    /// <summary>
    /// Single evaluation tick (also used by tests).
    /// </summary>
    public async Task TickAsync(CancellationToken cancellationToken = default)
    {
        var doc = _store.Document;
        if (doc is null)
            return;

        var settings = doc.Settings;
        var now = _clock();

        if (!settings.BackupDailyEnabled)
            return;

        if (string.IsNullOrWhiteSpace(settings.BackupFolder))
        {
            if (!_warnedMissingFolder)
            {
                _warnedMissingFolder = true;
                Completed?.Invoke(this, new AutoBackupEventArgs
                {
                    Ok = false,
                    Message = "Backup diário pausado: pasta de backup não definida."
                });
            }

            return;
        }

        if (!DailyBackupScheduler.ShouldRunToday(
                settings.BackupDailyEnabled,
                settings.BackupFolder,
                settings.LastAutoBackupDate,
                settings.BackupDailyTime,
                now))
            return;

        if (_lastFailureAttempt is { } lastFail
            && now - lastFail < _retryCooldown
            && !string.Equals(settings.LastAutoBackupDate?.Trim(), now.ToString("yyyy-MM-dd"), StringComparison.Ordinal))
        {
            // Still retrying after a failure today — wait cooldown
            // But ShouldRunToday is true, so we only skip if we failed recently
            return;
        }

        // Mark attempt time before work to avoid stampede if Tick overlaps
        _lastFailureAttempt = now;

        var results = await _backups.BackupAllEligibleAsync(
            doc.Services,
            settings.BackupFolder,
            confirmColdFallback: null,
            namingMode: BackupNamingMode.AutoOverwrite,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var eligible = results.Where(r => !r.Skipped).ToList();
        var failed = eligible.Where(r => !r.Ok).ToList();
        var okCount = eligible.Count(r => r.Ok);
        var stamp = now.ToString("o");

        foreach (var ok in eligible.Where(r => r.Ok))
        {
            var svc = doc.Services.FirstOrDefault(s =>
                string.Equals(s.Id, ok.ServiceId, StringComparison.Ordinal));
            if (svc is not null)
                svc.LastAutoBackupAt = stamp;
        }

        if (eligible.Count == 0)
        {
            // Nothing to back up — still count as success for the day to avoid spinning
            PersistLastDate(doc, now);
            _lastFailureAttempt = null;
            Completed?.Invoke(this, new AutoBackupEventArgs
            {
                Ok = true,
                Message = "Backup diário: nenhum serviço elegível."
            });
            return;
        }

        if (failed.Count > 0)
        {
            // Persist timestamps of succeeded items even on partial failure.
            if (okCount > 0)
            {
                try { _store.Save(doc); }
                catch { /* ignore */ }
            }

            Completed?.Invoke(this, new AutoBackupEventArgs
            {
                Ok = false,
                Message = $"Backup diário parcial/falha. Ok: {okCount}, Falha: {failed.Count}."
            });
            return;
        }

        PersistLastDate(doc, now);
        _lastFailureAttempt = null;
        Completed?.Invoke(this, new AutoBackupEventArgs
        {
            Ok = true,
            Message = $"Backup diário ok ({okCount} serviço(s))."
        });
    }

    private void PersistLastDate(ServicesDocument doc, DateTimeOffset now)
    {
        doc.Settings.LastAutoBackupDate = now.ToString("yyyy-MM-dd");
        try
        {
            _store.Save(doc);
        }
        catch
        {
            // Keep in-memory date so we don't spam backups if disk write fails
        }
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // keep looping
            }

            try
            {
                await Task.Delay(_pollInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}

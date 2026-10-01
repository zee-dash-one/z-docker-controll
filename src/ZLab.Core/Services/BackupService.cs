using ZLab.Models;

namespace ZLab.Services;

public sealed class BackupItemResult
{
    public required string ServiceId { get; init; }
    public bool Ok { get; init; }
    public bool Skipped { get; init; }
    public string Message { get; init; } = "";
    public string? OutputPath { get; init; }
}

/// <summary>
/// volumeCopy backups into stamped, named, or auto/{id} folders under settings.backupFolder.
/// </summary>
public sealed class BackupService
{
    private readonly DockerCli _cli;
    private readonly ContainerService _containers;

    public BackupService(DockerCli cli, ContainerService containers)
    {
        _cli = cli ?? throw new ArgumentNullException(nameof(cli));
        _containers = containers ?? throw new ArgumentNullException(nameof(containers));
    }

    public static IEnumerable<ServiceItem> GetEligible(IEnumerable<ServiceItem> services) =>
        services.Where(s => s.Enabled && BackupFolderNaming.IsBackupEligible(s.Backup?.Mode));

    public async Task<BackupItemResult> BackupAsync(
        ServiceItem item,
        string backupRoot,
        DateTimeOffset? timestamp = null,
        bool attemptColdFallback = false,
        Func<Task<bool>>? confirmColdFallback = null,
        BackupNamingMode namingMode = BackupNamingMode.ManualStamp,
        string? customName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!BackupFolderNaming.IsBackupEligible(item.Backup?.Mode))
        {
            return new BackupItemResult
            {
                ServiceId = item.Id,
                Skipped = true,
                Message = "Backup não elegível (mode != volumeCopy)."
            };
        }

        if (string.IsNullOrWhiteSpace(backupRoot))
        {
            return new BackupItemResult
            {
                ServiceId = item.Id,
                Ok = false,
                Message = "Pasta de backup não configurada."
            };
        }

        if (namingMode == BackupNamingMode.ManualNamed
            && string.IsNullOrWhiteSpace(BackupFolderNaming.SanitizeCustomName(customName)))
        {
            return new BackupItemResult
            {
                ServiceId = item.Id,
                Ok = false,
                Message = "Nome de backup inválido."
            };
        }

        await _containers.EnsureDiscoveredAsync(cancellationToken).ConfigureAwait(false);
        if (!_cli.IsAvailable)
        {
            return new BackupItemResult
            {
                ServiceId = item.Id,
                Ok = false,
                Message = _cli.UnavailableReason ?? "Docker indisponível."
            };
        }

        var stamp = timestamp ?? DateTimeOffset.Now;
        string dest;
        try
        {
            dest = BackupFolderNaming.ResolveDestination(
                backupRoot, item.Id, namingMode, stamp, customName);
        }
        catch (ArgumentException ex)
        {
            return new BackupItemResult
            {
                ServiceId = item.Id,
                Ok = false,
                Message = ex.Message
            };
        }

        BackupFolderNaming.PrepareDestination(dest, namingMode);

        var statusBefore = await _containers.GetStatusAsync(item, cancellationToken).ConfigureAwait(false);
        var hot = await TryCopyAsync(item, dest, statusBefore, cancellationToken).ConfigureAwait(false);
        if (hot.Ok)
            return hot;

        // Cold fallback only makes sense when the container is running.
        // If already Down/Absent, the first attempt already used the stopped/volume path —
        // do not ask the operator to "stop" an already-stopped container.
        if (statusBefore != ContainerStatusKind.Up
            || !attemptColdFallback
            || confirmColdFallback is null)
            return hot;

        var confirmed = await confirmColdFallback().ConfigureAwait(false);
        if (!confirmed)
        {
            return new BackupItemResult
            {
                ServiceId = item.Id,
                Ok = false,
                Message = "Backup cancelado após falha a quente."
            };
        }

        var stop = await _containers.StopAsync(item, cancellationToken).ConfigureAwait(false);
        if (!stop.Ok)
        {
            return new BackupItemResult
            {
                ServiceId = item.Id,
                Ok = false,
                Message = "Falha ao parar para backup a frio: " + stop.Message
            };
        }

        var cold = await TryCopyAsync(item, dest, ContainerStatusKind.Down, cancellationToken)
            .ConfigureAwait(false);

        var start = await _containers.StartAsync(item, cancellationToken).ConfigureAwait(false);
        if (!start.Ok && cold.Ok)
        {
            return new BackupItemResult
            {
                ServiceId = item.Id,
                Ok = false,
                Message = "Backup ok, mas falha ao reiniciar: " + start.Message,
                OutputPath = dest
            };
        }

        return cold;
    }

    public async Task<IReadOnlyList<BackupItemResult>> BackupAllEligibleAsync(
        IEnumerable<ServiceItem> services,
        string backupRoot,
        Func<ServiceItem, Task<bool>>? confirmColdFallback = null,
        BackupNamingMode namingMode = BackupNamingMode.ManualStamp,
        string? customName = null,
        IProgress<(string id, string phase)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<BackupItemResult>();
        foreach (var item in GetEligible(services))
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report((item.Id, "Backup…"));
            var result = await BackupAsync(
                item,
                backupRoot,
                attemptColdFallback: confirmColdFallback is not null,
                confirmColdFallback: confirmColdFallback is null
                    ? null
                    : () => confirmColdFallback(item),
                namingMode: namingMode,
                customName: customName,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            results.Add(result);
        }

        return results;
    }

    private async Task<BackupItemResult> TryCopyAsync(
        ServiceItem item,
        string destDir,
        ContainerStatusKind status,
        CancellationToken cancellationToken)
    {
        var mounts = item.Volumes?
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList() ?? new List<string>();

        if (mounts.Count == 0)
        {
            await File.WriteAllTextAsync(
                Path.Combine(destDir, "README.txt"),
                $"Z-Lab backup de {item.Id}: sem volumes declarados no catálogo.",
                cancellationToken).ConfigureAwait(false);
            return new BackupItemResult
            {
                ServiceId = item.Id,
                Ok = true,
                Message = "Sem volumes; pasta criada.",
                OutputPath = destDir
            };
        }

        var errors = new List<string>();

        for (var i = 0; i < mounts.Count; i++)
        {
            var mount = mounts[i];
            var parts = mount.Split(':', 2);
            if (parts.Length < 2)
            {
                errors.Add($"Volume inválido: {mount}");
                continue;
            }

            var source = parts[0];
            var containerPath = parts[1].TrimEnd('/');
            var targetSub = Path.Combine(destDir, $"vol{i}");
            Directory.CreateDirectory(targetSub);

            // Prefer docker cp when the container exists (Up/Down) so data in the
            // container FS is captured even before a named volume is attached.
            // Use path/. so contents land in volN/ (not volN/data/).
            if (status is ContainerStatusKind.Up or ContainerStatusKind.Down)
            {
                var copy = await CopyFromContainerAsync(
                    item, containerPath, targetSub, status, cancellationToken).ConfigureAwait(false);
                if (!copy.Ok)
                    errors.Add(copy.Message);
                continue;
            }

            // Absent: named volume helper (alpine) when source is a volume name.
            if (IsNamedVolumeSource(source))
            {
                var copy = await CopyNamedVolumeAsync(
                    item.Id, i, source, targetSub, cancellationToken).ConfigureAwait(false);
                if (!copy.Ok)
                    errors.Add(copy.Message);
                continue;
            }

            errors.Add(
                $"Bind mount sem container ({source}): inicie o serviço ou use volume nomeado.");
        }

        if (errors.Count > 0)
        {
            return new BackupItemResult
            {
                ServiceId = item.Id,
                Ok = false,
                Message = string.Join("; ", errors),
                OutputPath = destDir
            };
        }

        return new BackupItemResult
        {
            ServiceId = item.Id,
            Ok = true,
            Message = "Backup concluído.",
            OutputPath = destDir
        };
    }

    /// <summary>
    /// Copies containerPath contents into targetSub. When Up, pauses briefly so SQLite WAL is consistent.
    /// </summary>
    private async Task<(bool Ok, string Message)> CopyFromContainerAsync(
        ServiceItem item,
        string containerPath,
        string targetSub,
        ContainerStatusKind status,
        CancellationToken cancellationToken)
    {
        var paused = false;
        if (status == ContainerStatusKind.Up)
        {
            var pause = await _cli.RunAsync(new[] { "pause", item.ContainerName }, cancellationToken)
                .ConfigureAwait(false);
            paused = pause.Succeeded;
        }

        try
        {
            var destForDocker = HostPathForDocker(targetSub);
            // Trailing "/." copies directory contents into dest (flat), including .sqlite-wal/-shm.
            var src = containerPath.EndsWith("/.", StringComparison.Ordinal)
                ? containerPath
                : containerPath + "/.";
            var args = new[] { "cp", $"{item.ContainerName}:{src}", destForDocker };
            var cp = await _cli.RunAsync(args, cancellationToken).ConfigureAwait(false);
            if (!cp.Succeeded)
                return (false, string.IsNullOrWhiteSpace(cp.StdErr) ? "Falha no docker cp." : cp.StdErr);

            return (true, "");
        }
        finally
        {
            if (paused)
            {
                try
                {
                    await _cli.RunAsync(new[] { "unpause", item.ContainerName }, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch
                {
                    // best-effort resume
                }
            }
        }
    }

    private async Task<(bool Ok, string Message)> CopyNamedVolumeAsync(
        string serviceId,
        int index,
        string volumeName,
        string targetSub,
        CancellationToken cancellationToken)
    {
        var helperName = $"zlab-bkp-{serviceId}-{index}-{Guid.NewGuid():N}";
        if (helperName.Length > 63)
            helperName = helperName[..63];

        var runArgs = new[]
        {
            "run", "--rm", "--name", helperName,
            "-v", $"{volumeName}:/from:ro",
            "-v", $"{HostPathForDocker(targetSub)}:/to",
            "alpine:3.20",
            "sh", "-c", "cp -a /from/. /to/ || true"
        };
        var run = await _cli.RunAsync(runArgs, cancellationToken).ConfigureAwait(false);
        if (run.Succeeded)
            return (true, "");

        return (false, string.IsNullOrWhiteSpace(run.StdErr) ? "Falha no backup do volume." : run.StdErr);
    }

    /// <summary>
    /// True for Docker named volumes (e.g. omniroute-data), false for bind mounts.
    /// </summary>
    public static bool IsNamedVolumeSource(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return false;
        var s = source.Trim();
        if (s.StartsWith('/'))
            return false;
        if (s.StartsWith('.'))
            return false;
        if (s.Length >= 2 && char.IsLetter(s[0]) && s[1] == ':')
            return false;
        return true;
    }

    /// <summary>
    /// Path visible to the docker daemon (WSL uses /mnt/&lt;drive&gt;/...).
    /// </summary>
    private string HostPathForDocker(string windowsOrUnixPath)
    {
        if (_cli.Mode == DockerInvocationMode.WslDocker)
            return ToDockerPath(windowsOrUnixPath);
        return windowsOrUnixPath;
    }

    /// <summary>
    /// Best-effort path for docker -v bind when CLI runs in WSL.
    /// </summary>
    public static string ToDockerPath(string windowsOrUnixPath)
    {
        if (windowsOrUnixPath.Length >= 3
            && char.IsLetter(windowsOrUnixPath[0])
            && windowsOrUnixPath[1] == ':'
            && (windowsOrUnixPath[2] == '\\' || windowsOrUnixPath[2] == '/'))
        {
            var drive = char.ToLowerInvariant(windowsOrUnixPath[0]);
            var rest = windowsOrUnixPath[2..].Replace('\\', '/');
            return $"/mnt/{drive}{rest}";
        }

        return windowsOrUnixPath.Replace('\\', '/');
    }
}

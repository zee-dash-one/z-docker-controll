using System.Diagnostics;
using ZLab.Models;

namespace ZLab.Services;

public sealed class OpResult
{
    public bool Ok { get; init; }
    public string Message { get; init; } = "";
    public ContainerStatusKind Status { get; init; } = ContainerStatusKind.Unknown;
    public string? Output { get; init; }

    public static OpResult Success(string message = "", ContainerStatusKind status = ContainerStatusKind.Unknown, string? output = null) =>
        new() { Ok = true, Message = message, Status = status, Output = output };

    public static OpResult Fail(string message, ContainerStatusKind status = ContainerStatusKind.Error) =>
        new() { Ok = false, Message = message, Status = status };
}

/// <summary>
/// Maps ServiceItem → docker start/stop/restart/update/stats/logs/run.
/// </summary>
public sealed class ContainerService
{
    private readonly DockerCli _cli;

    public ContainerService(DockerCli cli)
    {
        _cli = cli ?? throw new ArgumentNullException(nameof(cli));
    }

    public DockerCli Cli => _cli;

    public async Task EnsureDiscoveredAsync(CancellationToken cancellationToken = default)
    {
        if (!_cli.IsAvailable)
            await _cli.DiscoverAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<ContainerStatusKind> GetStatusAsync(ServiceItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        await EnsureDiscoveredAsync(cancellationToken).ConfigureAwait(false);
        if (!_cli.IsAvailable)
            return ContainerStatusKind.Error;

        var result = await _cli.RunAsync(
            DockerCliArgs.BuildInspectStatusArgs(item.ContainerName),
            cancellationToken,
            audit: false).ConfigureAwait(false);

        if (!result.Succeeded)
            return ContainerStatusKind.Absent;

        return DockerCliArgs.ParseEngineStatus(result.StdOut);
    }

    public async Task<OpResult> StartAsync(ServiceItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!item.Enabled)
            return OpResult.Fail("Serviço desativado. Ative antes de iniciar.");

        await EnsureDiscoveredAsync(cancellationToken).ConfigureAwait(false);
        if (!_cli.IsAvailable)
            return OpResult.Fail(_cli.UnavailableReason ?? "Docker indisponível.");

        var status = await GetStatusAsync(item, cancellationToken).ConfigureAwait(false);
        if (status == ContainerStatusKind.Up)
            return OpResult.Success("Já em execução.", ContainerStatusKind.Up);

        if (!string.IsNullOrWhiteSpace(item.EnvFile))
        {
            var authError = TailscaleAuthKeyCheck.Validate(item.EnvFile);
            if (authError is not null)
                return OpResult.Fail(authError);
        }

        if (status == ContainerStatusKind.Absent)
        {
            var runArgs = DockerCliArgs.BuildRunArgs(
                item.ContainerName,
                item.Image,
                item.HostPort,
                item.ContainerPort,
                item.Volumes,
                item.Env,
                item.NetworkMode,
                item.CapAdd,
                item.Hostname,
                item.EnvFile);
            var run = await _cli.RunAsync(runArgs, cancellationToken).ConfigureAwait(false);
            if (!run.Succeeded)
                return OpResult.Fail(PreferErr(run, "Falha ao criar/iniciar o container."));
            return OpResult.Success("Container criado e iniciado.", ContainerStatusKind.Up);
        }

        var start = await _cli.RunAsync(
            DockerCliArgs.BuildStartArgs(item.ContainerName),
            cancellationToken).ConfigureAwait(false);
        if (!start.Succeeded)
            return OpResult.Fail(PreferErr(start, "Falha ao iniciar o container."));
        return OpResult.Success("Container iniciado.", ContainerStatusKind.Up);
    }

    public async Task<OpResult> StopAsync(ServiceItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        await EnsureDiscoveredAsync(cancellationToken).ConfigureAwait(false);
        if (!_cli.IsAvailable)
            return OpResult.Fail(_cli.UnavailableReason ?? "Docker indisponível.");

        var status = await GetStatusAsync(item, cancellationToken).ConfigureAwait(false);
        if (status is ContainerStatusKind.Absent or ContainerStatusKind.Down)
            return OpResult.Success("Já parado ou ausente.", status);

        var timeout = item.StopTimeoutSeconds > 0 ? item.StopTimeoutSeconds : 15;
        var stop = await _cli.RunAsync(
            DockerCliArgs.BuildStopArgs(item.ContainerName, timeout),
            cancellationToken).ConfigureAwait(false);
        if (!stop.Succeeded)
            return OpResult.Fail(PreferErr(stop, "Falha ao parar o container."));
        return OpResult.Success("Container parado.", ContainerStatusKind.Down);
    }

    public async Task<OpResult> RestartAsync(ServiceItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!item.Enabled)
            return OpResult.Fail("Serviço desativado.");

        await EnsureDiscoveredAsync(cancellationToken).ConfigureAwait(false);
        if (!_cli.IsAvailable)
            return OpResult.Fail(_cli.UnavailableReason ?? "Docker indisponível.");

        var status = await GetStatusAsync(item, cancellationToken).ConfigureAwait(false);
        if (status == ContainerStatusKind.Absent)
            return await StartAsync(item, cancellationToken).ConfigureAwait(false);

        var restart = await _cli.RunAsync(
            DockerCliArgs.BuildRestartArgs(item.ContainerName),
            cancellationToken).ConfigureAwait(false);
        if (!restart.Succeeded)
            return OpResult.Fail(PreferErr(restart, "Falha ao reiniciar."));
        return OpResult.Success("Container reiniciado.", ContainerStatusKind.Up);
    }

    public async Task<OpResult> GetLogsAsync(ServiceItem item, int tailLines, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        await EnsureDiscoveredAsync(cancellationToken).ConfigureAwait(false);
        if (!_cli.IsAvailable)
            return OpResult.Fail(_cli.UnavailableReason ?? "Docker indisponível.");

        var logs = await _cli.RunAsync(
            DockerCliArgs.BuildLogsArgs(item.ContainerName, Math.Max(1, tailLines)),
            cancellationToken).ConfigureAwait(false);
        if (!logs.Succeeded && string.IsNullOrWhiteSpace(logs.StdOut))
            return OpResult.Fail(PreferErr(logs, "Falha ao obter logs."));

        // docker logs may write to stderr; prefer combined useful text
        var text = string.IsNullOrWhiteSpace(logs.StdOut) ? logs.StdErr : logs.StdOut;
        return OpResult.Success(output: text ?? "");
    }

    public async Task<OpResult> GetStatsAsync(ServiceItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        await EnsureDiscoveredAsync(cancellationToken).ConfigureAwait(false);
        if (!_cli.IsAvailable)
            return OpResult.Fail(_cli.UnavailableReason ?? "Docker indisponível.");

        var status = await GetStatusAsync(item, cancellationToken).ConfigureAwait(false);
        if (status != ContainerStatusKind.Up)
            return OpResult.Fail("Stats disponíveis apenas quando o container está Up.", status);

        var stats = await _cli.RunAsync(
            DockerCliArgs.BuildStatsArgs(item.ContainerName),
            cancellationToken).ConfigureAwait(false);
        if (!stats.Succeeded)
            return OpResult.Fail(PreferErr(stats, "Falha ao obter stats."), status);
        return OpResult.Success(status: status, output: stats.StdOut);
    }

    /// <summary>
    /// Pull image and recreate container preserving mounts/volumes from the item.
    /// Caller must confirm with the operator before invoking.
    /// </summary>
    public async Task<OpResult> UpdateAsync(ServiceItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!item.Enabled)
            return OpResult.Fail("Serviço desativado.");

        await EnsureDiscoveredAsync(cancellationToken).ConfigureAwait(false);
        if (!_cli.IsAvailable)
            return OpResult.Fail(_cli.UnavailableReason ?? "Docker indisponível.");

        var pull = await _cli.RunAsync(DockerCliArgs.BuildPullArgs(item.Image), cancellationToken)
            .ConfigureAwait(false);
        var usedLocalImage = false;
        if (!pull.Succeeded)
        {
            var err = PreferErr(pull, "Falha ao baixar a imagem.");
            var combined = ((pull.StdErr ?? "") + "\n" + (pull.StdOut ?? "") + "\n" + err);
            if (LooksLikeRegistryNetworkIssue(combined))
            {
                var local = await _cli.RunAsync(
                        DockerCliArgs.BuildImageInspectIdArgs(item.Image),
                        cancellationToken)
                    .ConfigureAwait(false);
                if (local.Succeeded && !string.IsNullOrWhiteSpace(local.StdOut))
                {
                    usedLocalImage = true;
                }
                else
                {
                    return OpResult.Fail(
                        err
                        + "\n\nDocker Hub/CloudFront falhou por IPv6 no WSL."
                        + "\nRode: wsl -u root -- bash /mnt/c/GIT/ZAPFCOORP/router-ctrl/scripts/fix-wsl-docker-ipv4.sh"
                        + "\nDepois tente Atualizar de novo.");
                }
            }
            else
            {
                return OpResult.Fail(err);
            }
        }

        var prior = await GetStatusAsync(item, cancellationToken).ConfigureAwait(false);
        if (prior != ContainerStatusKind.Absent)
        {
            if (prior == ContainerStatusKind.Up)
            {
                var stop = await StopAsync(item, cancellationToken).ConfigureAwait(false);
                if (!stop.Ok)
                    return stop;
            }

            var rm = await _cli.RunAsync(DockerCliArgs.BuildRmArgs(item.ContainerName), cancellationToken)
                .ConfigureAwait(false);
            if (!rm.Succeeded)
                return OpResult.Fail(PreferErr(rm, "Falha ao remover container antigo."));
        }

        var start = await StartAsync(item, cancellationToken).ConfigureAwait(false);
        if (!start.Ok)
            return start;

        if (usedLocalImage)
        {
            return OpResult.Success(
                "Container recriado com imagem local (pull falhou por rede/IPv6). "
                + "Corrija a rede e atualize de novo para pegar a versão mais recente.",
                ContainerStatusKind.Up);
        }

        return start;
    }

    public static bool TryOpenUrl(ServiceItem item, out string? error)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (string.IsNullOrWhiteSpace(item.OpenUrl))
        {
            error = "URL não configurada.";
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = item.OpenUrl,
                UseShellExecute = true
            });
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string PreferErr(CliResult result, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(result.StdErr))
            return result.StdErr.Trim();
        if (!string.IsNullOrWhiteSpace(result.StdOut))
            return result.StdOut.Trim();
        return fallback;
    }

    private static bool LooksLikeRegistryNetworkIssue(string text)
    {
        var t = text.ToLowerInvariant();
        return t.Contains("network is unreachable", StringComparison.Ordinal)
               || t.Contains("cannot assign requested address", StringComparison.Ordinal)
               || t.Contains("dial tcp [", StringComparison.Ordinal)
               || t.Contains("production.cloudfront.docker.com", StringComparison.Ordinal)
               || t.Contains("registry-1.docker.io", StringComparison.Ordinal)
               || t.Contains("i/o timeout", StringComparison.Ordinal)
               || t.Contains("no route to host", StringComparison.Ordinal)
               || t.Contains("failed to copy", StringComparison.Ordinal)
               || t.Contains("httpreadseeker", StringComparison.Ordinal);
    }
}

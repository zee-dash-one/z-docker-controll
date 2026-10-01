using System.Diagnostics;
using System.Text;
using ZLab.Models;

namespace ZLab.Services;

public sealed class LanExposeResult
{
    public bool Ok { get; init; }
    public bool NeedsElevation { get; init; }
    public string Message { get; init; } = "";
}

/// <summary>
/// netsh portproxy + firewall helper for exposeLan items.
/// </summary>
public sealed class LanExposeHelper
{
    private readonly string _wslDistro;
    private readonly Func<string, string[], bool, CancellationToken, Task<CliResult>> _runner;

    public LanExposeHelper(string wslDistro = DockerCliArgs.DefaultWslDistro)
        : this(wslDistro, RunProcessAsync)
    {
    }

    internal LanExposeHelper(
        string wslDistro,
        Func<string, string[], bool, CancellationToken, Task<CliResult>> runner)
    {
        _wslDistro = string.IsNullOrWhiteSpace(wslDistro)
            ? DockerCliArgs.DefaultWslDistro
            : wslDistro;
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    public async Task<string?> ResolveWslIpAsync(CancellationToken cancellationToken = default)
    {
        var result = await _runner(
            "wsl",
            new[] { "-d", _wslDistro, "hostname", "-I" },
            false,
            cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.StdOut))
            return null;

        return result.StdOut.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
    }

    public static string[] BuildPortProxyArgs(int listenPort, string connectAddress, int connectPort) =>
        new[]
        {
            "interface", "portproxy", "add", "v4tov4",
            $"listenport={listenPort}",
            "listenaddress=0.0.0.0",
            $"connectport={connectPort}",
            $"connectaddress={connectAddress}"
        };

    public static string[] BuildFirewallRuleArgs(string ruleName, int port) =>
        new[]
        {
            "advfirewall", "firewall", "add", "rule",
            $"name={ruleName}",
            "dir=in",
            "action=allow",
            "protocol=TCP",
            $"localport={port}"
        };

    public async Task<LanExposeResult> ExposeAsync(
        ServiceItem item,
        bool allowElevation = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!item.ExposeLan)
        {
            return new LanExposeResult
            {
                Ok = false,
                Message = "Este serviço não está marcado para exposição LAN."
            };
        }

        if (item.HostPort <= 0)
        {
            return new LanExposeResult
            {
                Ok = false,
                Message = "hostPort inválido."
            };
        }

        var wslIp = await ResolveWslIpAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(wslIp))
        {
            return new LanExposeResult
            {
                Ok = false,
                Message = "Não foi possível obter o IP do WSL. Verifique a distro Ubuntu-24.04."
            };
        }

        // Docker publishes hostPort on the WSL side.
        var connectPort = item.HostPort;
        var proxyArgs = BuildPortProxyArgs(item.HostPort, wslIp, connectPort);
        var proxy = await _runner("netsh", proxyArgs, false, cancellationToken)
            .ConfigureAwait(false);

        if (!proxy.Succeeded && LooksLikeElevationRequired(proxy) && allowElevation)
        {
            var elevated = await _runner("netsh", proxyArgs, true, cancellationToken)
                .ConfigureAwait(false);
            if (!elevated.Succeeded)
            {
                return new LanExposeResult
                {
                    Ok = false,
                    NeedsElevation = true,
                    Message = "É necessário Admin para configurar portproxy. Elevação negada ou falhou: "
                              + (string.IsNullOrWhiteSpace(elevated.StdErr) ? elevated.StdOut : elevated.StdErr)
                };
            }
        }
        else if (!proxy.Succeeded && LooksLikeElevationRequired(proxy))
        {
            return new LanExposeResult
            {
                Ok = false,
                NeedsElevation = true,
                Message = "É necessário executar como Administrador para expor na LAN."
            };
        }
        else if (!proxy.Succeeded)
        {
            // "already exists" — try delete+add is YAGNI; treat duplicate as soft-ok if message hints
            var proxyMsg = (proxy.StdErr + " " + proxy.StdOut).ToLowerInvariant();
            if (!proxyMsg.Contains("already", StringComparison.Ordinal)
                && !proxyMsg.Contains("exist", StringComparison.Ordinal))
            {
                return new LanExposeResult
                {
                    Ok = false,
                    Message = string.IsNullOrWhiteSpace(proxy.StdErr) ? proxy.StdOut : proxy.StdErr
                };
            }
        }

        if (!string.IsNullOrWhiteSpace(item.FirewallRuleName))
        {
            var fwArgs = BuildFirewallRuleArgs(item.FirewallRuleName, item.HostPort);
            var fw = await _runner("netsh", fwArgs, false, cancellationToken)
                .ConfigureAwait(false);
            if (!fw.Succeeded && LooksLikeElevationRequired(fw) && allowElevation)
            {
                fw = await _runner("netsh", fwArgs, true, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (!fw.Succeeded)
            {
                var fwMsg = (fw.StdErr + " " + fw.StdOut).ToLowerInvariant();
                if (!fwMsg.Contains("already", StringComparison.Ordinal)
                    && !fwMsg.Contains("exist", StringComparison.Ordinal))
                {
                    return new LanExposeResult
                    {
                        Ok = false,
                        NeedsElevation = LooksLikeElevationRequired(fw),
                        Message = "Portproxy ok, mas falha na regra de firewall: "
                                  + (string.IsNullOrWhiteSpace(fw.StdErr) ? fw.StdOut : fw.StdErr)
                    };
                }
            }
        }

        return new LanExposeResult
        {
            Ok = true,
            Message = $"Exposto na LAN: porta {item.HostPort} → {wslIp}:{connectPort}."
        };
    }

    private static bool LooksLikeElevationRequired(CliResult result)
    {
        var text = (result.StdErr + " " + result.StdOut).ToLowerInvariant();
        return result.ExitCode != 0 && (
            text.Contains("access is denied", StringComparison.Ordinal)
            || text.Contains("acesso negado", StringComparison.Ordinal)
            || text.Contains("requerida", StringComparison.Ordinal)
            || text.Contains("elevat", StringComparison.Ordinal)
            || text.Contains("administrator", StringComparison.Ordinal)
            || text.Contains("administrador", StringComparison.Ordinal));
    }

    private static async Task<CliResult> RunProcessAsync(
        string fileName,
        string[] args,
        bool elevate,
        CancellationToken cancellationToken)
    {
        if (elevate)
        {
            // Launch elevated netsh via cmd; UseShellExecute + Verb runas prompts UAC.
            var argLine = string.Join(" ", args.Select(QuoteIfNeeded));
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = argLine,
                UseShellExecute = true,
                Verb = "runas",
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            try
            {
                using var process = Process.Start(psi);
                if (process is null)
                {
                    return new CliResult
                    {
                        ExitCode = -1,
                        StdErr = "Não foi possível iniciar processo elevado."
                    };
                }

                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                return new CliResult { ExitCode = process.ExitCode };
            }
            catch (Exception ex)
            {
                return new CliResult { ExitCode = -1, StdErr = ex.Message };
            }
        }

        var info = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var a in args)
            info.ArgumentList.Add(a);

        try
        {
            using var process = new Process { StartInfo = info };
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null) stdout.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null) stderr.AppendLine(e.Data);
            };

            if (!process.Start())
                return new CliResult { ExitCode = -1, StdErr = $"Falha ao iniciar {fileName}." };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return new CliResult
            {
                ExitCode = process.ExitCode,
                StdOut = stdout.ToString().TrimEnd(),
                StdErr = stderr.ToString().TrimEnd()
            };
        }
        catch (Exception ex)
        {
            return new CliResult { ExitCode = -1, StdErr = ex.Message };
        }
    }

    private static string QuoteIfNeeded(string value)
    {
        if (value.Contains(' ') || value.Contains('"'))
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        return value;
    }
}

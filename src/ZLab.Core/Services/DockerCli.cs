using System.Diagnostics;
using System.Text;

namespace ZLab.Services;

public sealed class CliResult
{
    public int ExitCode { get; init; }
    public string StdOut { get; init; } = "";
    public string StdErr { get; init; } = "";
    public bool Succeeded => ExitCode == 0;
}

public sealed class CommandAuditEventArgs : EventArgs
{
    public required DateTimeOffset Timestamp { get; init; }
    public required string FileName { get; init; }
    public required IReadOnlyList<string> Args { get; init; }
    public required int ExitCode { get; init; }
    public required string StdOut { get; init; }
    public required string StdErr { get; init; }
    public required string ModeLabel { get; init; }

    public string FormatCommandLine()
    {
        var parts = new List<string> { FileName };
        foreach (var a in Args)
        {
            if (a.Length == 0)
                parts.Add("\"\"");
            else if (a.Contains(' ') || a.Contains('"'))
                parts.Add("\"" + a.Replace("\"", "\\\"") + "\"");
            else
                parts.Add(a);
        }

        return string.Join(' ', parts);
    }
}

/// <summary>
/// Sole process I/O boundary to docker.exe or wsl docker.
/// </summary>
public sealed class DockerCli
{
    private readonly string _wslDistro;
    private readonly Func<string, string[], CancellationToken, Task<CliResult>> _runner;

    public DockerCli(string wslDistro = DockerCliArgs.DefaultWslDistro)
        : this(wslDistro, RunProcessAsync)
    {
    }

    internal DockerCli(
        string wslDistro,
        Func<string, string[], CancellationToken, Task<CliResult>> runner)
    {
        _wslDistro = string.IsNullOrWhiteSpace(wslDistro)
            ? DockerCliArgs.DefaultWslDistro
            : wslDistro;
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    public DockerInvocationMode Mode { get; private set; } = DockerInvocationMode.WslDocker;

    public bool IsAvailable { get; private set; }

    public string? UnavailableReason { get; private set; }

    public string ModeLabel =>
        Mode == DockerInvocationMode.Docker ? "docker" : "wsl docker";

    /// <summary>Raised after each audited CLI invocation (stdout/stderr included).</summary>
    public event EventHandler<CommandAuditEventArgs>? CommandExecuted;

    public async Task DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var host = await _runner("docker", new[] { "version", "--format", "{{.Server.Version}}" }, cancellationToken)
            .ConfigureAwait(false);
        if (host.Succeeded)
        {
            Mode = DockerInvocationMode.Docker;
            IsAvailable = true;
            UnavailableReason = null;
            return;
        }

        var wslArgs = new[] { "-d", _wslDistro, "docker", "version", "--format", "{{.Server.Version}}" };
        var wsl = await _runner("wsl", wslArgs, cancellationToken).ConfigureAwait(false);
        if (wsl.Succeeded)
        {
            Mode = DockerInvocationMode.WslDocker;
            IsAvailable = true;
            UnavailableReason = null;
            return;
        }

        IsAvailable = false;
        UnavailableReason =
            "Docker indisponível no host e no WSL. Verifique docker.exe ou `wsl -d Ubuntu-24.04 docker`.";
        Mode = DockerInvocationMode.WslDocker;
    }

    public async Task<CliResult> RunAsync(
        string[] dockerArgs,
        CancellationToken cancellationToken = default,
        bool audit = true)
    {
        ArgumentNullException.ThrowIfNull(dockerArgs);
        if (!IsAvailable)
        {
            var unavailable = new CliResult
            {
                ExitCode = -1,
                StdErr = UnavailableReason ?? "Docker indisponível."
            };
            if (audit)
                RaiseAudit("docker", dockerArgs, unavailable);
            return unavailable;
        }

        string fileName;
        string[] args;
        if (Mode == DockerInvocationMode.Docker)
        {
            fileName = "docker";
            args = dockerArgs;
        }
        else
        {
            var combined = DockerCliArgs.CombineWslDocker(dockerArgs, _wslDistro);
            fileName = combined[0];
            args = combined.Skip(1).ToArray();
        }

        var result = await _runner(fileName, args, cancellationToken).ConfigureAwait(false);
        if (audit)
            RaiseAudit(fileName, args, result);
        return result;
    }

    private void RaiseAudit(string fileName, string[] args, CliResult result)
    {
        try
        {
            CommandExecuted?.Invoke(this, new CommandAuditEventArgs
            {
                Timestamp = DateTimeOffset.Now,
                FileName = fileName,
                Args = args,
                ExitCode = result.ExitCode,
                StdOut = result.StdOut,
                StdErr = result.StdErr,
                ModeLabel = ModeLabel
            });
        }
        catch
        {
            // UI listeners must not break CLI
        }
    }

    private static async Task<CliResult> RunProcessAsync(
        string fileName,
        string[] args,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var a in args)
            psi.ArgumentList.Add(a);

        using var process = new Process { StartInfo = psi };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                stdout.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                stderr.AppendLine(e.Data);
        };

        try
        {
            if (!process.Start())
            {
                return new CliResult
                {
                    ExitCode = -1,
                    StdErr = $"Não foi possível iniciar o processo '{fileName}'."
                };
            }

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
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
                // ignore
            }

            throw;
        }
        catch (Exception ex)
        {
            return new CliResult
            {
                ExitCode = -1,
                StdErr = ex.Message
            };
        }
    }
}

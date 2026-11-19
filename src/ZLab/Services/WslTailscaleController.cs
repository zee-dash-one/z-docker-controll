using System.Diagnostics;
using System.Text;

namespace ZLab.Services;

/// <summary>
/// Local WSL Docker Compose for nbpitang-wsl-tailscale. Does not talk to z-server-01.
/// </summary>
public sealed class WslTailscaleController
{
    public const string Distro = "Ubuntu-24.04";
    public const string ContainerName = "nbpitang-wsl-tailscale";
    public const string TaskName = "ZLab-WslTailscale";
    public const string AddressLabel = "127.0.0.1:1055 · :1056";

    public const string ComposeLinux =
        "/mnt/c/GIT/ZAPFCOORP/router-ctrl/wsl/tailscale-windows/docker-compose.yml";

    public static readonly string EnvPath =
        @"C:\GIT\ZAPFCOORP\router-ctrl\wsl\tailscale-windows\.env";

    public static readonly string ScriptPath =
        @"C:\GIT\ZAPFCOORP\router-ctrl\scripts\start-wsl-tailscale.ps1";

    public bool HasAuthKey()
    {
        if (!File.Exists(EnvPath))
            return false;
        foreach (var line in File.ReadLines(EnvPath))
        {
            if (line.StartsWith("TS_AUTHKEY=", StringComparison.Ordinal)
                && line.Length > "TS_AUTHKEY=".Length)
                return true;
        }

        return false;
    }

    public void SaveAuthKey(string key)
    {
        var trimmed = key.Trim();
        if (trimmed.Length == 0 || trimmed.IndexOfAny(new[] { '\r', '\n' }) >= 0)
            throw new InvalidOperationException("Chave inválida.");

        var dir = Path.GetDirectoryName(EnvPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(EnvPath, "TS_AUTHKEY=" + trimmed + Environment.NewLine, new UTF8Encoding(false));
    }

    public Task<int> StartAsync(CancellationToken cancellationToken) =>
        RunAsync(
            "powershell.exe",
            new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", ScriptPath },
            cancellationToken);

    public Task<int> StopAsync(CancellationToken cancellationToken) =>
        ComposeAsync(new[] { "stop" }, cancellationToken);

    public async Task RemoveAsync(CancellationToken cancellationToken)
    {
        await ComposeAsync(new[] { "down", "-v" }, cancellationToken).ConfigureAwait(false);
        if (File.Exists(EnvPath))
            File.Delete(EnvPath);
        await RunAsync(
            "schtasks.exe",
            new[] { "/Delete", "/TN", TaskName, "/F" },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<WslTailscaleSnapshot> ReadAsync(CancellationToken cancellationToken)
    {
        var inspect = await RunCaptureAsync(
            "wsl.exe",
            WslArgs("docker", "inspect", "-f", "{{.State.Status}}|{{.State.StartedAt}}|{{.RestartCount}}", ContainerName),
            cancellationToken).ConfigureAwait(false);
        if (inspect.Code != 0)
            return WslTailscaleSnapshot.Absent;

        var stats = await RunCaptureAsync(
            "wsl.exe",
            WslArgs("docker", "stats", "--no-stream", "--format", "{{.CPUPerc}}|{{.MemUsage}}|{{.MemPerc}}", ContainerName),
            cancellationToken).ConfigureAwait(false);
        return WslTailscaleParse.Parse(inspect.Text, stats.Code == 0 ? stats.Text : null);
    }

    private Task<int> ComposeAsync(IReadOnlyList<string> verb, CancellationToken cancellationToken)
    {
        var args = new List<string> { "docker", "compose", "-f", ComposeLinux };
        args.AddRange(verb);
        return RunAsync("wsl.exe", WslArgs(args.ToArray()), cancellationToken);
    }

    private static string[] WslArgs(params string[] command)
    {
        var args = new List<string> { "-d", Distro, "-u", "root", "--" };
        args.AddRange(command);
        return args.ToArray();
    }

    private static async Task<int> RunAsync(string file, IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        var result = await RunCaptureAsync(file, args, cancellationToken).ConfigureAwait(false);
        return result.Code;
    }

    private static async Task<(int Code, string Text)> RunCaptureAsync(
        string file,
        IReadOnlyList<string> args,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo(file)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Não foi possível iniciar " + file);
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        var text = string.IsNullOrWhiteSpace(stdout) ? stderr : stdout;
        return (process.ExitCode, text.Trim());
    }
}

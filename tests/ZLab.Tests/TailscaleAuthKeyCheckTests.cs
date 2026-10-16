using ZLab.Models;
using ZLab.Services;

namespace ZLab.Tests;

public sealed class TailscaleAuthKeyCheckTests : IDisposable
{
    private readonly string _dir;

    public TailscaleAuthKeyCheckTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ZLab.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public void ToWindowsPath_ConvertsWslMount()
    {
        Assert.Equal(
            @"C:\GIT\ZAPFCOORP\tailscale-9router\.env",
            TailscaleAuthKeyCheck.ToWindowsPath("/mnt/c/GIT/ZAPFCOORP/tailscale-9router/.env"));
    }

    [Fact]
    public void Validate_MissingFile_DoesNotEchoPathSecrets()
    {
        var error = TailscaleAuthKeyCheck.Validate("/mnt/z/nao-existe/.env");
        Assert.NotNull(error);
        Assert.Contains("não encontrado", error, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tskey-auth", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_EmptyOrPlaceholder_DoesNotEchoKey()
    {
        var empty = Path.Combine(_dir, "empty.env");
        File.WriteAllText(empty, "TS_AUTHKEY=\n");
        var emptyError = TailscaleAuthKeyCheck.Validate(empty);
        Assert.NotNull(emptyError);
        Assert.Contains("vazia ou ainda é o modelo", emptyError, StringComparison.Ordinal);

        var placeholder = Path.Combine(_dir, "placeholder.env");
        File.WriteAllText(placeholder, "TS_AUTHKEY=tskey-auth-XXXXXXXX\n");
        var placeholderError = TailscaleAuthKeyCheck.Validate(placeholder);
        Assert.NotNull(placeholderError);
        Assert.DoesNotContain("tskey-auth", placeholderError, StringComparison.Ordinal);
        Assert.DoesNotContain("XXXXXXXX", placeholderError, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_RealKey_ReturnsNull()
    {
        var file = Path.Combine(_dir, ".env");
        File.WriteAllText(file, "# comentario\nTS_AUTHKEY=\"tskey-auth-k1examplevalue\"\n");
        Assert.Null(TailscaleAuthKeyCheck.Validate(file));
    }
}

public sealed class TailscaleStartPreflightTests : IDisposable
{
    private readonly string _dir;

    public TailscaleStartPreflightTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ZLab.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public async Task StartAsync_RefusesEmptyAuthKey_WithoutCreatingContainer()
    {
        var envFile = Path.Combine(_dir, ".env");
        File.WriteAllText(envFile, "TS_AUTHKEY=\n");
        var calls = new List<string[]>();
        var cli = new DockerCli("Ubuntu-24.04", (file, args, _) =>
        {
            calls.Add(args);
            if (args.Contains("version"))
                return Task.FromResult(new CliResult { ExitCode = 0, StdOut = "27.0.0" });
            if (args.Length > 0 && args[0] == "inspect")
                return Task.FromResult(new CliResult { ExitCode = 1, StdErr = "No such object" });
            return Task.FromResult(new CliResult { ExitCode = 0 });
        });

        var result = await new ContainerService(cli).StartAsync(new ServiceItem
        {
            Id = "tailscale",
            Enabled = true,
            ContainerName = "tailscale-9router",
            Image = "tailscale/tailscale:latest",
            EnvFile = envFile
        });

        Assert.False(result.Ok);
        Assert.Contains("TS_AUTHKEY", result.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("tskey-auth", result.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(calls, args => args.Contains("run") || (args.Length > 0 && args[0] == "start"));
    }

    [Fact]
    public async Task StartAsync_WithRealKey_RunsUsingEnvFileOnly()
    {
        var envFile = Path.Combine(_dir, ".env");
        const string secret = "tskey-auth-k1examplevalue";
        File.WriteAllText(envFile, "TS_AUTHKEY=" + secret + "\n");
        string[]? runArgs = null;
        var cli = new DockerCli("Ubuntu-24.04", (file, args, _) =>
        {
            if (args.Contains("version"))
                return Task.FromResult(new CliResult { ExitCode = 0, StdOut = "27.0.0" });
            if (args.Length > 0 && args[0] == "inspect")
                return Task.FromResult(new CliResult { ExitCode = 1, StdErr = "No such object" });
            if (args.Length > 0 && args[0] == "run")
                runArgs = args;
            return Task.FromResult(new CliResult { ExitCode = 0 });
        });

        var item = ServiceCatalogStore.CreateTailscaleServiceItem();
        item.EnvFile = envFile;
        item.Env["TS_AUTHKEY"] = secret;

        var result = await new ContainerService(cli).StartAsync(item);

        Assert.True(result.Ok, result.Message);
        Assert.NotNull(runArgs);
        Assert.Contains("--env-file", runArgs);
        Assert.Contains(envFile, runArgs);
        Assert.DoesNotContain(runArgs!, a => a.Contains(secret, StringComparison.Ordinal));
    }
}

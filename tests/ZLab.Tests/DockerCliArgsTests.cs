using ZLab.Services;

namespace ZLab.Tests;

public sealed class DockerCliArgsTests
{
    [Fact]
    public void BuildStopArgs_IncludesTimeoutAndContainerName()
    {
        var args = DockerCliArgs.BuildStopArgs("omniroute", stopTimeoutSeconds: 40);

        Assert.Equal(new[] { "stop", "-t", "40", "omniroute" }, args);
    }

    [Fact]
    public void BuildStartArgs_UsesContainerName()
    {
        var args = DockerCliArgs.BuildStartArgs("9router");
        Assert.Equal(new[] { "start", "9router" }, args);
    }

    [Fact]
    public void BuildRestartArgs_UsesContainerName()
    {
        var args = DockerCliArgs.BuildRestartArgs("zlab-redis");
        Assert.Equal(new[] { "restart", "zlab-redis" }, args);
    }

    [Fact]
    public void BuildLogsArgs_IncludesTail()
    {
        var args = DockerCliArgs.BuildLogsArgs("9router", tailLines: 80);
        Assert.Equal(new[] { "logs", "--tail", "80", "9router" }, args);
    }

    [Fact]
    public void BuildStatsArgs_NoStream()
    {
        var args = DockerCliArgs.BuildStatsArgs("omniroute");
        Assert.Equal(new[] { "stats", "--no-stream", "omniroute" }, args);
    }

    [Fact]
    public void BuildPullArgs_UsesImage()
    {
        var args = DockerCliArgs.BuildPullArgs("postgres:16");
        Assert.Equal(new[] { "pull", "postgres:16" }, args);
    }

    [Fact]
    public void BuildWslDockerPrefix_UsesUbuntu2404Distro()
    {
        var args = DockerCliArgs.BuildWslDockerPrefix();
        Assert.Equal(new[] { "wsl", "-d", "Ubuntu-24.04", "docker" }, args);
    }

    [Fact]
    public void CombineWslDocker_PrefixesDockerSubArgs()
    {
        var combined = DockerCliArgs.CombineWslDocker(new[] { "stop", "-t", "15", "9router" });
        Assert.Equal(
            new[] { "wsl", "-d", "Ubuntu-24.04", "docker", "stop", "-t", "15", "9router" },
            combined);
    }

    [Theory]
    [InlineData("running", ContainerStatusKind.Up)]
    [InlineData("exited", ContainerStatusKind.Down)]
    [InlineData("created", ContainerStatusKind.Down)]
    [InlineData("", ContainerStatusKind.Absent)]
    [InlineData(null, ContainerStatusKind.Absent)]
    public void ParseEngineStatus_MapsKnownValues(string? engineStatus, ContainerStatusKind expected)
    {
        Assert.Equal(expected, DockerCliArgs.ParseEngineStatus(engineStatus));
    }

    [Fact]
    public void BuildRunArgs_IncludesRestartUnlessStopped()
    {
        var args = DockerCliArgs.BuildRunArgs(
            "9router",
            "decolua/9router:latest",
            hostPort: 20128,
            containerPort: 20128,
            volumes: new[] { "9router-data:/app/data" },
            env: new Dictionary<string, string> { ["INITIAL_PASSWORD"] = "123456" });

        Assert.Equal(
            new[]
            {
                "run", "-d",
                "--restart", "unless-stopped",
                "--name", "9router",
                "-p", "20128:20128",
                "-e", "INITIAL_PASSWORD=123456",
                "-v", "9router-data:/app/data",
                "decolua/9router:latest"
            },
            args);
    }

    [Fact]
    public void BuildRunArgs_HostNetwork_UsesCapsHostnameAndEnvFile_OmitsPortAndAuthKey()
    {
        var args = DockerCliArgs.BuildRunArgs(
            "tailscale-9router",
            "tailscale/tailscale:latest",
            hostPort: 20128,
            containerPort: 20128,
            volumes: new[]
            {
                "tailscale-9router-state:/var/lib/tailscale",
                "/mnt/c/GIT/ZAPFCOORP/tailscale-9router/config:/config:ro"
            },
            env: new Dictionary<string, string>
            {
                ["TS_USERSPACE"] = "true",
                ["TS_AUTHKEY"] = "tskey-auth-secret-should-not-appear"
            },
            networkMode: "host",
            capAdd: new[] { "NET_ADMIN", "NET_RAW" },
            hostname: "9router-serve",
            envFile: "/mnt/c/GIT/ZAPFCOORP/tailscale-9router/.env");

        Assert.Equal(
            new[]
            {
                "run", "-d",
                "--restart", "unless-stopped",
                "--name", "tailscale-9router",
                "--network", "host",
                "--hostname", "9router-serve",
                "--cap-add", "NET_ADMIN",
                "--cap-add", "NET_RAW",
                "--env-file", "/mnt/c/GIT/ZAPFCOORP/tailscale-9router/.env",
                "-e", "TS_USERSPACE=true",
                "-v", "tailscale-9router-state:/var/lib/tailscale",
                "-v", "/mnt/c/GIT/ZAPFCOORP/tailscale-9router/config:/config:ro",
                "tailscale/tailscale:latest"
            },
            args);
        Assert.DoesNotContain(args, a => a.Contains("tskey-auth", StringComparison.Ordinal));
        Assert.DoesNotContain("-p", args);
    }

    [Fact]
    public void PublicSurface_HasNoPruneOrVolumeDeleteHelpers()
    {
        var methods = typeof(DockerCliArgs).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(m => m.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain(methods, n => n.Contains("Prune", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methods, n => n.Contains("DeleteVolume", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methods, n => n.Contains("VolumeRm", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methods, n => n.Contains("SystemPrune", StringComparison.OrdinalIgnoreCase));
    }
}

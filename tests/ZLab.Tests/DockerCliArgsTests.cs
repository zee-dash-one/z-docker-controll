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

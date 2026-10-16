namespace ZLab.Services;

/// <summary>
/// Pure helpers for building docker CLI argv arrays.
/// </summary>
public static class DockerCliArgs
{
    public const string DefaultWslDistro = "Ubuntu-24.04";

    public static string[] BuildStopArgs(string containerName, int stopTimeoutSeconds) =>
        new[] { "stop", "-t", stopTimeoutSeconds.ToString(), containerName };

    public static string[] BuildStartArgs(string containerName) =>
        new[] { "start", containerName };

    public static string[] BuildRestartArgs(string containerName) =>
        new[] { "restart", containerName };

    public static string[] BuildLogsArgs(string containerName, int tailLines) =>
        new[] { "logs", "--tail", tailLines.ToString(), containerName };

    public static string[] BuildStatsArgs(string containerName) =>
        new[] { "stats", "--no-stream", containerName };

    public static string[] BuildPullArgs(string image) =>
        new[] { "pull", image };

    public static string[] BuildImageInspectIdArgs(string image) =>
        new[] { "image", "inspect", "-f", "{{.Id}}", image };

    public static string[] BuildInspectStatusArgs(string containerName) =>
        new[] { "inspect", "-f", "{{.State.Status}}", containerName };

    public static string[] BuildWslDockerPrefix(string distro = DefaultWslDistro) =>
        new[] { "wsl", "-d", distro, "docker" };

    public static string[] CombineWslDocker(string[] dockerArgs, string distro = DefaultWslDistro)
    {
        ArgumentNullException.ThrowIfNull(dockerArgs);
        var prefix = BuildWslDockerPrefix(distro);
        var combined = new string[prefix.Length + dockerArgs.Length];
        prefix.CopyTo(combined, 0);
        dockerArgs.CopyTo(combined, prefix.Length);
        return combined;
    }

    /// <summary>
    /// Parse a typical `docker inspect -f '{{.State.Status}}'` style status string into a coarse kind.
    /// </summary>
    public static ContainerStatusKind ParseEngineStatus(string? engineStatus)
    {
        if (string.IsNullOrWhiteSpace(engineStatus))
            return ContainerStatusKind.Absent;

        var s = engineStatus.Trim();
        if (s.Equals("running", StringComparison.OrdinalIgnoreCase))
            return ContainerStatusKind.Up;
        if (s.Equals("exited", StringComparison.OrdinalIgnoreCase)
            || s.Equals("created", StringComparison.OrdinalIgnoreCase)
            || s.Equals("paused", StringComparison.OrdinalIgnoreCase)
            || s.Equals("dead", StringComparison.OrdinalIgnoreCase))
            return ContainerStatusKind.Down;

        return ContainerStatusKind.Unknown;
    }

    public static string[] BuildRunArgs(
        string containerName,
        string image,
        int hostPort,
        int containerPort,
        IEnumerable<string>? volumes,
        IReadOnlyDictionary<string, string>? env,
        string? networkMode = null,
        IEnumerable<string>? capAdd = null,
        string? hostname = null,
        string? envFile = null)
    {
        var args = new List<string>
        {
            "run", "-d",
            "--restart", "unless-stopped",
            "--name", containerName
        };

        var hostNetwork = string.Equals(networkMode, "host", StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(networkMode))
            args.AddRange(new[] { "--network", networkMode.Trim() });

        if (!string.IsNullOrWhiteSpace(hostname))
            args.AddRange(new[] { "--hostname", hostname.Trim() });

        if (capAdd is not null)
        {
            foreach (var cap in capAdd)
            {
                if (string.IsNullOrWhiteSpace(cap))
                    continue;
                args.Add("--cap-add");
                args.Add(cap.Trim());
            }
        }

        if (!string.IsNullOrWhiteSpace(envFile))
            args.AddRange(new[] { "--env-file", envFile.Trim() });

        if (!hostNetwork && hostPort > 0 && containerPort > 0)
            args.AddRange(new[] { "-p", $"{hostPort}:{containerPort}" });

        if (env is not null)
        {
            foreach (var pair in env)
            {
                if (string.Equals(pair.Key, "TS_AUTHKEY", StringComparison.OrdinalIgnoreCase))
                    continue;
                args.Add("-e");
                args.Add($"{pair.Key}={pair.Value}");
            }
        }

        if (volumes is not null)
        {
            foreach (var vol in volumes)
            {
                if (string.IsNullOrWhiteSpace(vol))
                    continue;
                args.Add("-v");
                args.Add(vol);
            }
        }

        args.Add(image);
        return args.ToArray();
    }

    public static string[] BuildRmArgs(string containerName) =>
        new[] { "rm", "-f", containerName };
}

public enum ContainerStatusKind
{
    Unknown = 0,
    Up,
    Down,
    Absent,
    Busy,
    Error
}

public enum DockerInvocationMode
{
    Docker,
    WslDocker
}

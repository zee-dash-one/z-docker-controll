namespace ZLab.Models;

public sealed class ServiceItem
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Category { get; set; } = "";
    public bool Enabled { get; set; }
    public bool Managed { get; set; } = true;
    public string ContainerName { get; set; } = "";
    public string Image { get; set; } = "";
    public int HostPort { get; set; }
    public int ContainerPort { get; set; }
    public List<string> Volumes { get; set; } = new();
    public Dictionary<string, string> Env { get; set; } = new(StringComparer.Ordinal);
    /// <summary>
    /// Docker network mode. <c>host</c> skips published ports (<c>-p</c>).
    /// </summary>
    public string NetworkMode { get; set; } = "";
    public List<string> CapAdd { get; set; } = new();
    public string Hostname { get; set; } = "";
    /// <summary>
    /// Path passed to <c>docker run --env-file</c>. Linux path when the engine is WSL
    /// (for example <c>/mnt/c/...</c>). Secrets stay in that file, not in <see cref="Env"/>.
    /// </summary>
    public string EnvFile { get; set; } = "";
    public int StopTimeoutSeconds { get; set; } = 15;
    public string OpenUrl { get; set; } = "";
    /// <summary>
    /// Optional Windows hosts alias (e.g. 9router-local) for cookie-isolated local URLs.
    /// </summary>
    public string LocalHost { get; set; } = "";
    public HealthSpec Health { get; set; } = new();
    public BackupSpec Backup { get; set; } = new();
    public bool ExposeLan { get; set; }
    public string FirewallRuleName { get; set; } = "";
    public string Notes { get; set; } = "";

    /// <summary>
    /// Local timestamp of last successful daily auto backup (ISO 8601 round-trip), empty if never.
    /// </summary>
    public string LastAutoBackupAt { get; set; } = "";
}

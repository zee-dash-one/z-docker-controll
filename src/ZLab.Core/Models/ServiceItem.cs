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

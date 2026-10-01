namespace ZLab.Models;

public sealed class AppSettings
{
    public string BackupFolder { get; set; } = "";
    public int PollSeconds { get; set; } = 8;
    public int LogTailLines { get; set; } = 80;

    /// <summary>When true, daily auto backup runs while the app is open.</summary>
    public bool BackupDailyEnabled { get; set; } = true;

    /// <summary>Local time of day (HH:mm) when daily auto backup may run.</summary>
    public string BackupDailyTime { get; set; } = "03:00";

    /// <summary>yyyy-MM-dd of last successful auto backup cycle (empty if never).</summary>
    public string LastAutoBackupDate { get; set; } = "";
}

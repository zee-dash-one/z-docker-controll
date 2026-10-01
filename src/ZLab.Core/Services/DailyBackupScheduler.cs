namespace ZLab.Services;

/// <summary>
/// Pure scheduling rules for daily auto backup (injectable clock for tests).
/// </summary>
public static class DailyBackupScheduler
{
    public static bool TryParseDailyTime(string? hhmm, out TimeSpan timeOfDay)
    {
        timeOfDay = default;
        if (string.IsNullOrWhiteSpace(hhmm))
            return false;

        var parts = hhmm.Trim().Split(':', 2);
        if (parts.Length != 2)
            return false;
        if (!int.TryParse(parts[0], out var h) || !int.TryParse(parts[1], out var m))
            return false;
        if (h is < 0 or > 23 || m is < 0 or > 59)
            return false;

        timeOfDay = new TimeSpan(h, m, 0);
        return true;
    }

    /// <summary>
    /// True when daily backup should run now: enabled, folder set, not yet today, clock past daily time.
    /// </summary>
    public static bool ShouldRunToday(
        bool enabled,
        string? backupFolder,
        string? lastAutoBackupDate,
        string? backupDailyTime,
        DateTimeOffset now)
    {
        if (!enabled)
            return false;
        if (string.IsNullOrWhiteSpace(backupFolder))
            return false;
        if (!TryParseDailyTime(backupDailyTime, out var tod))
            return false;

        var today = now.ToString("yyyy-MM-dd");
        if (string.Equals(lastAutoBackupDate?.Trim(), today, StringComparison.Ordinal))
            return false;

        var localTime = now.TimeOfDay;
        return localTime >= tod;
    }
}

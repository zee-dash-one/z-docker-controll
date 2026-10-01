using System.Text.RegularExpressions;

namespace ZLab.Services;

/// <summary>
/// Backup subdirectory naming: stamped manual, custom manual, and overwritable auto/{id}.
/// </summary>
public static class BackupFolderNaming
{
    public const string StampFormat = "yyyy-MM-dd_HHmm";
    public const string AutoFolderName = "auto";

    private static readonly Regex InvalidNameChars = new(@"[^\w\-]+", RegexOptions.Compiled);

    /// <summary>
    /// Returns folder name segment under backup root, e.g. omniroute_2026-09-28_1719.
    /// </summary>
    public static string FormatStamp(string id, DateTimeOffset timestamp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return $"{id}_{timestamp.ToString(StampFormat)}";
    }

    /// <summary>
    /// Relative path for daily overwrite slot: auto/{id}.
    /// </summary>
    public static string FormatAutoRelative(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return Path.Combine(AutoFolderName, id);
    }

    /// <summary>
    /// Manual named folder: {id}_{sanitizedName}.
    /// </summary>
    public static string FormatCustom(string id, string customName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var sanitized = SanitizeCustomName(customName);
        if (string.IsNullOrWhiteSpace(sanitized))
            throw new ArgumentException("Nome de backup inválido após sanitização.", nameof(customName));
        return $"{id}_{sanitized}";
    }

    public static string SanitizeCustomName(string? customName)
    {
        if (string.IsNullOrWhiteSpace(customName))
            return "";

        var trimmed = customName.Trim();
        var cleaned = InvalidNameChars.Replace(trimmed, "_");
        cleaned = cleaned.Trim('_', '.', ' ');
        if (cleaned.Length > 64)
            cleaned = cleaned[..64].TrimEnd('_');
        return cleaned;
    }

    public static string CombineBackupPath(string backupRoot, string id, DateTimeOffset timestamp) =>
        Path.Combine(backupRoot, FormatStamp(id, timestamp));

    public static string CombineAutoPath(string backupRoot, string id) =>
        Path.Combine(backupRoot, FormatAutoRelative(id));

    public static string CombineCustomPath(string backupRoot, string id, string customName) =>
        Path.Combine(backupRoot, FormatCustom(id, customName));

    /// <summary>
    /// Resolves destination folder under backup root for the given naming mode.
    /// </summary>
    public static string ResolveDestination(
        string backupRoot,
        string id,
        BackupNamingMode mode,
        DateTimeOffset? timestamp = null,
        string? customName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupRoot);
        return mode switch
        {
            BackupNamingMode.AutoOverwrite => CombineAutoPath(backupRoot, id),
            BackupNamingMode.ManualNamed => CombineCustomPath(backupRoot, id, customName ?? ""),
            _ => CombineBackupPath(backupRoot, id, timestamp ?? DateTimeOffset.Now)
        };
    }

    /// <summary>
    /// Eligible for backup when mode is volumeCopy (not none / pgDump).
    /// </summary>
    public static bool IsBackupEligible(string? backupMode) =>
        string.Equals(backupMode, "volumeCopy", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// UI line for last auto backup: "Backup auto: dd/MM/yyyy - HH:mm" or "Backup auto: —".
    /// </summary>
    public static string FormatLastAutoBackupLabel(string? lastAutoBackupAtIso)
    {
        if (TryParseLastAutoBackupAt(lastAutoBackupAtIso, out var when))
            return "Backup auto: " + when.ToLocalTime().ToString("dd/MM/yyyy - HH:mm");
        return "Backup auto: —";
    }

    public static bool TryParseLastAutoBackupAt(string? value, out DateTimeOffset when)
    {
        when = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        return DateTimeOffset.TryParse(
            value,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out when);
    }

    /// <summary>
    /// Best-effort timestamp from auto/{id} folder mtime when JSON field is empty.
    /// </summary>
    public static DateTimeOffset? TryReadAutoSlotTimestamp(string? backupRoot, string serviceId)
    {
        if (string.IsNullOrWhiteSpace(backupRoot) || string.IsNullOrWhiteSpace(serviceId))
            return null;
        try
        {
            var dir = CombineAutoPath(backupRoot, serviceId);
            if (!Directory.Exists(dir))
                return null;
            return new DateTimeOffset(Directory.GetLastWriteTime(dir));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Ensures destination directory exists; for AutoOverwrite, clears previous contents.
    /// </summary>
    public static void PrepareDestination(string destDir, BackupNamingMode mode)
    {
        if (mode == BackupNamingMode.AutoOverwrite && Directory.Exists(destDir))
        {
            try
            {
                Directory.Delete(destDir, recursive: true);
            }
            catch (IOException)
            {
                // Fall through to recreate; partial clear still better than skip
                foreach (var entry in Directory.EnumerateFileSystemEntries(destDir))
                {
                    try
                    {
                        if (Directory.Exists(entry))
                            Directory.Delete(entry, recursive: true);
                        else
                            File.Delete(entry);
                    }
                    catch
                    {
                        // best-effort clear
                    }
                }
            }
        }

        Directory.CreateDirectory(destDir);
    }
}

public enum BackupNamingMode
{
    ManualStamp = 0,
    ManualNamed = 1,
    AutoOverwrite = 2
}

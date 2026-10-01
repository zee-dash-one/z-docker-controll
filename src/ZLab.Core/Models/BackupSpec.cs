namespace ZLab.Models;

public sealed class BackupSpec
{
    /// <summary>V1: "volumeCopy" | "none".</summary>
    public string Mode { get; set; } = "none";
}

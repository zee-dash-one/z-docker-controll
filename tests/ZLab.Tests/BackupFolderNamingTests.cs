using ZLab.Services;

namespace ZLab.Tests;

public sealed class BackupFolderNamingTests
{
    [Fact]
    public void FormatStamp_UsesIdAndYyyyMmDd_HHmm()
    {
        var stamp = BackupFolderNaming.FormatStamp(
            "omniroute",
            new DateTimeOffset(2026, 9, 28, 17, 19, 0, TimeSpan.FromHours(-3)));

        Assert.Equal("omniroute_2026-09-28_1719", stamp);
    }

    [Fact]
    public void FormatStamp_PadsHourAndMinute()
    {
        var stamp = BackupFolderNaming.FormatStamp(
            "9router",
            new DateTimeOffset(2026, 1, 5, 9, 7, 30, TimeSpan.Zero));

        Assert.Equal("9router_2026-01-05_0907", stamp);
    }

    [Fact]
    public void CombineBackupPath_JoinsRootAndStamp()
    {
        var root = Path.Combine(Path.GetTempPath(), "zlab-backups");
        var path = BackupFolderNaming.CombineBackupPath(
            root,
            "redis",
            new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal(Path.Combine(root, "redis_2026-09-28_1200"), path);
    }

    [Fact]
    public void FormatAutoRelative_UsesAutoId()
    {
        Assert.Equal(Path.Combine("auto", "9router"), BackupFolderNaming.FormatAutoRelative("9router"));
    }

    [Fact]
    public void CombineAutoPath_JoinsRootAutoId()
    {
        var root = Path.Combine(Path.GetTempPath(), "zlab-backups");
        Assert.Equal(
            Path.Combine(root, "auto", "omniroute"),
            BackupFolderNaming.CombineAutoPath(root, "omniroute"));
    }

    [Fact]
    public void FormatCustom_SanitizesName()
    {
        Assert.Equal("9router_pre_update", BackupFolderNaming.FormatCustom("9router", "pre update!"));
    }

    [Fact]
    public void ResolveDestination_Modes()
    {
        var root = @"D:\bkp";
        var stamp = new DateTimeOffset(2026, 9, 28, 15, 30, 0, TimeSpan.Zero);

        Assert.Equal(
            Path.Combine(root, "auto", "9router"),
            BackupFolderNaming.ResolveDestination(root, "9router", BackupNamingMode.AutoOverwrite));
        Assert.Equal(
            Path.Combine(root, "9router_2026-09-28_1530"),
            BackupFolderNaming.ResolveDestination(root, "9router", BackupNamingMode.ManualStamp, stamp));
        Assert.Equal(
            Path.Combine(root, "9router_manual"),
            BackupFolderNaming.ResolveDestination(root, "9router", BackupNamingMode.ManualNamed, customName: "manual"));
    }

    [Fact]
    public void PrepareDestination_AutoOverwrite_ClearsPrevious()
    {
        var root = Path.Combine(Path.GetTempPath(), "zlab-auto-" + Guid.NewGuid().ToString("N"));
        var dest = BackupFolderNaming.CombineAutoPath(root, "9router");
        try
        {
            Directory.CreateDirectory(dest);
            File.WriteAllText(Path.Combine(dest, "old.txt"), "stale");

            BackupFolderNaming.PrepareDestination(dest, BackupNamingMode.AutoOverwrite);

            Assert.True(Directory.Exists(dest));
            Assert.Empty(Directory.GetFileSystemEntries(dest));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("volumeCopy", true)]
    [InlineData("none", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("pgDump", false)]
    public void IsBackupEligible_OnlyVolumeCopy(string? mode, bool expected)
    {
        Assert.Equal(expected, BackupFolderNaming.IsBackupEligible(mode));
    }

    [Fact]
    public void FormatLastAutoBackupLabel_FormatsLocalDateTime()
    {
        var iso = new DateTimeOffset(2026, 9, 29, 3, 15, 0, TimeSpan.FromHours(-3)).ToString("o");
        Assert.Equal("Backup auto: 29/09/2026 - 03:15", BackupFolderNaming.FormatLastAutoBackupLabel(iso));
    }

    [Fact]
    public void FormatLastAutoBackupLabel_Empty_ShowsDash()
    {
        Assert.Equal("Backup auto: —", BackupFolderNaming.FormatLastAutoBackupLabel(null));
        Assert.Equal("Backup auto: —", BackupFolderNaming.FormatLastAutoBackupLabel(""));
    }

    [Fact]
    public void TryReadAutoSlotTimestamp_ReadsDirectoryMtime()
    {
        var root = Path.Combine(Path.GetTempPath(), "zlab-auto-mtime-" + Guid.NewGuid().ToString("N"));
        var dir = BackupFolderNaming.CombineAutoPath(root, "omniroute");
        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "marker.txt"), "x");
            var ts = BackupFolderNaming.TryReadAutoSlotTimestamp(root, "omniroute");
            Assert.NotNull(ts);
            Assert.True((DateTimeOffset.Now - ts.Value).Duration() < TimeSpan.FromMinutes(2));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}

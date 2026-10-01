using ZLab.Services;

namespace ZLab.Tests;

public sealed class DailyBackupSchedulerTests
{
    [Theory]
    [InlineData("03:00", true, 3, 0)]
    [InlineData("15:30", true, 15, 30)]
    [InlineData("24:00", false, 0, 0)]
    [InlineData("3:00", true, 3, 0)]
    [InlineData("", false, 0, 0)]
    [InlineData(null, false, 0, 0)]
    public void TryParseDailyTime(string? input, bool ok, int hour, int minute)
    {
        var parsed = DailyBackupScheduler.TryParseDailyTime(input, out var tod);
        Assert.Equal(ok, parsed);
        if (ok)
            Assert.Equal(new TimeSpan(hour, minute, 0), tod);
    }

    [Fact]
    public void ShouldRunToday_WhenPastTimeAndNotYetRun()
    {
        var now = new DateTimeOffset(2026, 9, 28, 3, 5, 0, TimeSpan.FromHours(-3));
        Assert.True(DailyBackupScheduler.ShouldRunToday(
            enabled: true,
            backupFolder: @"D:\bkp",
            lastAutoBackupDate: "",
            backupDailyTime: "03:00",
            now: now));
    }

    [Fact]
    public void ShouldRunToday_False_BeforeDailyTime()
    {
        var now = new DateTimeOffset(2026, 9, 28, 2, 59, 0, TimeSpan.FromHours(-3));
        Assert.False(DailyBackupScheduler.ShouldRunToday(
            enabled: true,
            backupFolder: @"D:\bkp",
            lastAutoBackupDate: "",
            backupDailyTime: "03:00",
            now: now));
    }

    [Fact]
    public void ShouldRunToday_False_AlreadyRanToday()
    {
        var now = new DateTimeOffset(2026, 9, 28, 10, 0, 0, TimeSpan.FromHours(-3));
        Assert.False(DailyBackupScheduler.ShouldRunToday(
            enabled: true,
            backupFolder: @"D:\bkp",
            lastAutoBackupDate: "2026-09-28",
            backupDailyTime: "03:00",
            now: now));
    }

    [Fact]
    public void ShouldRunToday_False_EmptyFolder()
    {
        var now = new DateTimeOffset(2026, 9, 28, 10, 0, 0, TimeSpan.FromHours(-3));
        Assert.False(DailyBackupScheduler.ShouldRunToday(
            enabled: true,
            backupFolder: "",
            lastAutoBackupDate: "",
            backupDailyTime: "03:00",
            now: now));
    }

    [Fact]
    public void ShouldRunToday_False_Disabled()
    {
        var now = new DateTimeOffset(2026, 9, 28, 10, 0, 0, TimeSpan.FromHours(-3));
        Assert.False(DailyBackupScheduler.ShouldRunToday(
            enabled: false,
            backupFolder: @"D:\bkp",
            lastAutoBackupDate: "",
            backupDailyTime: "03:00",
            now: now));
    }

    [Fact]
    public void ShouldRunToday_True_NextDayAfterPriorRun()
    {
        var now = new DateTimeOffset(2026, 9, 29, 3, 0, 0, TimeSpan.FromHours(-3));
        Assert.True(DailyBackupScheduler.ShouldRunToday(
            enabled: true,
            backupFolder: @"D:\bkp",
            lastAutoBackupDate: "2026-09-28",
            backupDailyTime: "03:00",
            now: now));
    }
}

using ZLab.Services;

namespace ZLab.Tests;

public sealed class BackupVolumeCopyTests
{
    [Theory]
    [InlineData("omniroute-data", true)]
    [InlineData("zlab-postgres-data", true)]
    [InlineData("/var/lib/data", false)]
    [InlineData("C:\\data", false)]
    [InlineData("D:/bind", false)]
    [InlineData("./relative", false)]
    [InlineData("", false)]
    public void IsNamedVolumeSource(string source, bool expected)
    {
        Assert.Equal(expected, BackupService.IsNamedVolumeSource(source));
    }

    [Fact]
    public void ToDockerPath_ConvertsWindowsDrive()
    {
        Assert.Equal(
            "/mnt/d/backups/omni/vol0",
            BackupService.ToDockerPath(@"D:\backups\omni\vol0"));
    }

    [Fact]
    public void ToDockerPath_LeavesUnixPath()
    {
        Assert.Equal("/home/user/bkp", BackupService.ToDockerPath("/home/user/bkp"));
    }
}

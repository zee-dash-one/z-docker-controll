using ZLab.Models;
using ZLab.Services;

namespace ZLab.Tests;

public sealed class LocalHostsHelperTests
{
    [Fact]
    public void CollectAliases_SkipsEmptyOrInvalid()
    {
        var services = new[]
        {
            new ServiceItem { Id = "9router", LocalHost = "9router-local", HostPort = 20128, DisplayName = "9Router" },
            new ServiceItem { Id = "bad", LocalHost = "bad host", HostPort = 1 },
            new ServiceItem { Id = "noport", LocalHost = "ok-local", HostPort = 0 },
            new ServiceItem { Id = "empty", LocalHost = "", HostPort = 80 }
        };

        var aliases = LocalHostsHelper.CollectAliases(services);
        var one = Assert.Single(aliases);
        Assert.Equal("9router-local", one.HostName);
        Assert.Equal("http://9router-local:20128", one.OpenUrl);
    }

    [Fact]
    public void EnsureHostsContent_AppendsMissing_Idempotent()
    {
        var aliases = LocalHostsHelper.CollectAliases(new[]
        {
            new ServiceItem { Id = "9router", LocalHost = "9router-local", HostPort = 20128 },
            new ServiceItem { Id = "omniroute", LocalHost = "omniroute-local", HostPort = 20129 }
        });

        var (once, changed1) = LocalHostsHelper.EnsureHostsContent("# seed\n", aliases);
        Assert.True(changed1);
        Assert.Contains("9router-local", once);
        Assert.Contains("omniroute-local", once);
        Assert.Contains(LocalHostsHelper.MarkerComment, once);

        var (twice, changed2) = LocalHostsHelper.EnsureHostsContent(once, aliases);
        Assert.False(changed2);
        Assert.Equal(once, twice);
    }

    [Fact]
    public void AreAliasesPresent_RecognizesLoopbackLines()
    {
        var aliases = LocalHostsHelper.CollectAliases(new[]
        {
            new ServiceItem { Id = "9router", LocalHost = "9router-local", HostPort = 20128 }
        });

        Assert.False(LocalHostsHelper.AreAliasesPresent("127.0.0.1 localhost\n", aliases));
        Assert.True(LocalHostsHelper.AreAliasesPresent(
            "127.0.0.1 localhost 9router-local\n", aliases));
        Assert.True(LocalHostsHelper.AreAliasesPresent(
            "127.0.0.1\t9router-local # comment\n", aliases));
    }

    [Fact]
    public void ApplyOpenUrls_UpdatesMatchingServices()
    {
        var services = new List<ServiceItem>
        {
            new() { Id = "9router", LocalHost = "9router-local", HostPort = 20128, OpenUrl = "http://localhost:20128" },
            new() { Id = "other", OpenUrl = "http://localhost:1" }
        };
        var aliases = LocalHostsHelper.CollectAliases(services);
        LocalHostsHelper.ApplyOpenUrls(services, aliases);
        Assert.Equal("http://9router-local:20128", services[0].OpenUrl);
        Assert.Equal("http://localhost:1", services[1].OpenUrl);
    }

    [Fact]
    public async Task EnsureAsync_WritesWhenPermitted()
    {
        var path = Path.Combine(Path.GetTempPath(), "zlab-hosts-test-" + Guid.NewGuid().ToString("N") + ".txt");
        await File.WriteAllTextAsync(path, "127.0.0.1 localhost\n");
        try
        {
            var helper = new LocalHostsHelper(
                path,
                (_, _, _) => Task.FromResult(new CliResult { ExitCode = -1, StdErr = "should not elevate" }));

            var services = new[]
            {
                new ServiceItem { Id = "9router", LocalHost = "9router-local", HostPort = 20128 },
                new ServiceItem { Id = "omniroute", LocalHost = "omniroute-local", HostPort = 20129 }
            };

            var r1 = await helper.EnsureAsync(services);
            Assert.True(r1.Ok);
            Assert.False(r1.AlreadyConfigured);
            var text = await File.ReadAllTextAsync(path);
            Assert.Contains("9router-local", text);
            Assert.Contains("omniroute-local", text);

            var r2 = await helper.EnsureAsync(services);
            Assert.True(r2.Ok);
            Assert.True(r2.AlreadyConfigured);
        }
        finally
        {
            try { File.Delete(path); } catch { /* ignore */ }
        }
    }
}

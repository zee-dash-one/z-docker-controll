using System.Security.Cryptography;
using System.Text;
using ZLab.Services;

namespace ZLab.Tests;

public class RemoteCommandCatalogTests
{
    [Fact]
    public void Services_are_the_closed_six()
    {
        Assert.Equal(
            new[] { "9router", "omniroute", "mausbot", "tailscale", "tailscale-9router", "tailscale-mausbot" },
            RemoteCommands.Services.Select(s => s.Id).ToArray());
    }

    [Fact]
    public void Start_9router_uses_absolute_compose_without_profile()
    {
        Assert.Equal(
            "sudo -n /usr/bin/docker compose --project-directory /opt/zlab -f /opt/zlab/docker-compose.yml start 9router",
            RemoteCommands.Start("9router"));
    }

    [Fact]
    public void Tailnet_verbs_place_profile_after_compose()
    {
        Assert.Equal(
            "sudo -n /usr/bin/docker compose --profile tailnet --project-directory /opt/zlab -f /opt/zlab/docker-compose.yml start tailscale-9router",
            RemoteCommands.Start("tailscale-9router"));
        Assert.Equal(
            "sudo -n /usr/bin/docker compose --profile tailnet --project-directory /opt/zlab -f /opt/zlab/docker-compose.yml pull tailscale",
            RemoteCommands.Pull("tailscale"));
        Assert.Equal(
            "sudo -n /usr/bin/docker compose --profile tailnet --project-directory /opt/zlab -f /opt/zlab/docker-compose.yml up -d tailscale-mausbot",
            RemoteCommands.Up("tailscale-mausbot"));
    }

    [Fact]
    public void Update_is_pull_then_up_as_two_strings()
    {
        Assert.Equal(
            "sudo -n /usr/bin/docker compose --project-directory /opt/zlab -f /opt/zlab/docker-compose.yml pull omniroute",
            RemoteCommands.Pull("omniroute"));
        Assert.Equal(
            "sudo -n /usr/bin/docker compose --project-directory /opt/zlab -f /opt/zlab/docker-compose.yml up -d omniroute",
            RemoteCommands.Up("omniroute"));
        Assert.NotEqual(RemoteCommands.Pull("omniroute"), RemoteCommands.Up("omniroute"));
    }

    [Fact]
    public void Ps_includes_all_and_json()
    {
        Assert.Equal(
            "sudo -n /usr/bin/docker compose --project-directory /opt/zlab -f /opt/zlab/docker-compose.yml ps -a --format json mausbot",
            RemoteCommands.Ps("mausbot"));
    }

    [Fact]
    public void Stats_is_outside_compose()
    {
        Assert.Equal(
            "sudo -n /usr/bin/docker stats --no-stream --format {{.CPUPerc}}/{{.MemUsage}}/{{.PIDs}} 9router",
            RemoteCommands.Stats("9router"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("postgres")]
    [InlineData("9router;rm")]
    [InlineData("tailscale-9router ")]
    public void Unknown_service_is_rejected(string id)
    {
        Assert.Throws<ArgumentException>(() => RemoteCommands.Start(id));
    }

    [Fact]
    public void Sudoers_lists_each_verb_once_per_service_plus_host_and_power()
    {
        var cmds = RemoteCommands.AllSudoersArgv();
        Assert.Equal((6 * 7) + 8, cmds.Count);
        Assert.Contains("/usr/sbin/shutdown -h now", cmds);
        Assert.Contains("/usr/sbin/reboot", cmds);
        Assert.Contains("/usr/bin/df -h /data", cmds);
        Assert.DoesNotContain(cmds, c => c.Contains("&&", StringComparison.Ordinal));
        Assert.DoesNotContain(cmds, c => c.Contains(" down", StringComparison.Ordinal));
        Assert.DoesNotContain(cmds, c => c.Contains("prune", StringComparison.OrdinalIgnoreCase));
        var doc = RemoteCommands.FormatSudoersDocument();
        foreach (var cmd in cmds)
            Assert.Contains(cmd, doc);
        Assert.DoesNotContain("NOPASSWD: ALL", doc, StringComparison.Ordinal);
        var sudoers = SiblingSudoersPath();
        if (sudoers is not null)
            Assert.Equal(Normalize(doc), Normalize(File.ReadAllText(sudoers)));
    }

    private static string? SiblingSudoersPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ZLab.sln")))
            {
                var path = Path.GetFullPath(Path.Combine(dir.FullName, "..", "z-server-01", "zlab", "sudoers-zlab-monitor"));
                return File.Exists(path) ? path : null;
            }

            dir = dir.Parent;
        }

        return null;
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n").Trim();
}

public class WakeOnLanTests
{
    [Fact]
    public void Magic_packet_is_102_bytes_with_mac_repeated()
    {
        var packet = WakeOnLan.BuildMagicPacket("FC:AA:14:F9:80:D4");
        Assert.Equal(102, packet.Length);
        Assert.All(packet.Take(6), b => Assert.Equal(0xFF, b));
        var mac = new byte[] { 0xFC, 0xAA, 0x14, 0xF9, 0x80, 0xD4 };
        for (var rep = 0; rep < 16; rep++)
            Assert.Equal(mac, packet.Skip(6 + (rep * 6)).Take(6).ToArray());
    }
}

public class ComposePsParserTests
{
    [Fact]
    public void Empty_output_is_absent()
    {
        Assert.Equal(ContainerStatusKind.Absent, ComposePsParser.Parse(""));
        Assert.Equal(ContainerStatusKind.Absent, ComposePsParser.Parse("   \n"));
    }

    [Fact]
    public void Running_json_line_is_up()
    {
        var json = "{\"Service\":\"9router\",\"State\":\"running\",\"Name\":\"9router\"}\n";
        Assert.Equal(ContainerStatusKind.Up, ComposePsParser.Parse(json));
    }

    [Fact]
    public void Exited_json_line_is_down()
    {
        Assert.Equal(ContainerStatusKind.Down, ComposePsParser.Parse("{\"State\":\"exited\"}"));
    }
}

public class KnownHostsTrustTests
{
    [Fact]
    public void Plain_line_trusts_matching_wire_key()
    {
        var key = new byte[] { 1, 2, 3, 4 };
        var wire = KnownHostsTrust.EncodeWireKey("ssh-ed25519", key);
        var line = "192.168.0.112 ssh-ed25519 " + Convert.ToBase64String(wire);
        Assert.True(KnownHostsTrust.IsTrusted(line, "192.168.0.112", 22, "ssh-ed25519", key));
    }

    [Fact]
    public void Different_key_is_refused()
    {
        var key = new byte[] { 1, 2, 3, 4 };
        var wire = KnownHostsTrust.EncodeWireKey("ssh-ed25519", key);
        var line = "192.168.0.112 ssh-ed25519 " + Convert.ToBase64String(wire);
        Assert.False(KnownHostsTrust.IsTrusted(line, "192.168.0.112", 22, "ssh-ed25519", new byte[] { 9, 9, 9, 9 }));
    }

    [Fact]
    public void Hashed_host_matches()
    {
        var key = new byte[] { 5, 6, 7 };
        var wire = KnownHostsTrust.EncodeWireKey("ssh-ed25519", key);
        var salt = new byte[] { 9, 8, 7, 6 };
        using var hmac = new HMACSHA1(salt);
        var hash = hmac.ComputeHash(Encoding.ASCII.GetBytes("192.168.0.112"));
        var marker = "|1|" + Convert.ToBase64String(salt) + "|" + Convert.ToBase64String(hash);
        var line = marker + " ssh-ed25519 " + Convert.ToBase64String(wire);
        Assert.True(KnownHostsTrust.IsTrusted(line, "192.168.0.112", 22, "ssh-ed25519", key));
        Assert.False(KnownHostsTrust.IsTrusted(line, "192.168.0.113", 22, "ssh-ed25519", key));
    }

    [Fact]
    public void Missing_file_trusts_nothing()
    {
        Assert.False(KnownHostsTrust.IsTrusted(null, "192.168.0.112", 22, "ssh-ed25519", new byte[] { 1 }));
    }
}

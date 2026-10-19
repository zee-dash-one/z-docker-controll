using System.Text;

namespace ZLab.Services;

/// <summary>
/// Closed argv for the z-server-01 monitor. Service names are concatenated only from <see cref="Services"/>.
/// </summary>
public static class RemoteCommands
{
    public const string Host = "192.168.0.112";
    public const int Port = 22;
    public const string StatsFormat = "{{.CPUPerc}}/{{.MemUsage}}/{{.PIDs}}";

    private const string ComposeFile = "/opt/zlab/docker-compose.yml";
    private const string ProjectDir = "/opt/zlab";

    public static IReadOnlyList<RemoteService> Services { get; } = new RemoteService[]
    {
        new("9router", "9Router", false, "http://192.168.0.112:20128", "https://9router-serve.mandrill-court.ts.net"),
        new("omniroute", "OmniRoute", false, "http://192.168.0.112:20129", "https://zlab.mandrill-court.ts.net"),
        new("mausbot", "MausBot", false, null, "https://mausbot-serve.mandrill-court.ts.net"),
        new("tailscale", "Tailscale zlab", true, null, "https://zlab.mandrill-court.ts.net"),
        new("tailscale-9router", "Tailscale 9Router", true, null, "https://9router-serve.mandrill-court.ts.net"),
        new("tailscale-mausbot", "Tailscale MausBot", true, null, "https://mausbot-serve.mandrill-court.ts.net")
    };

    public static RemoteService Require(string serviceId)
    {
        foreach (var service in Services)
        {
            if (string.Equals(service.Id, serviceId, StringComparison.Ordinal))
                return service;
        }

        throw new ArgumentException("Serviço fora da lista fechada: " + serviceId, nameof(serviceId));
    }

    public static string Start(string serviceId) => Compose(serviceId, "start " + Require(serviceId).Id);
    public static string Stop(string serviceId) => Compose(serviceId, "stop " + Require(serviceId).Id);
    public static string Pull(string serviceId) => Compose(serviceId, "pull " + Require(serviceId).Id);
    public static string Up(string serviceId) => Compose(serviceId, "up -d " + Require(serviceId).Id);
    public static string Ps(string serviceId) => Compose(serviceId, "ps -a --format json " + Require(serviceId).Id);
    public static string Logs(string serviceId) => Compose(serviceId, "logs --tail 80 " + Require(serviceId).Id);

    public static string Stats(string serviceId)
    {
        var id = Require(serviceId).Id;
        return Sudo("/usr/bin/docker stats --no-stream --format " + StatsFormat + " " + id);
    }

    public static string Uptime() => Sudo("/usr/bin/uptime");
    public static string LoadAvg() => Sudo("/usr/bin/cat /proc/loadavg");
    public static string Memory() => Sudo("/usr/bin/free -h");
    public static string DiskRoot() => Sudo("/usr/bin/df -h /");
    public static string DiskDocker() => Sudo("/usr/bin/df -h /var/lib/docker");
    public static string DiskData() => Sudo("/usr/bin/df -h /data");
    public static string Shutdown() => Sudo("/usr/sbin/shutdown -h now");
    public static string Reboot() => Sudo("/usr/sbin/reboot");

    public const string ShutdownConfirm = "Desligar o z-server-01 agora? O SSH cai e os containers param com o sistema.";
    public const string RebootConfirm = "Reiniciar o z-server-01 agora? O SSH cai. No boot o Docker sobe e os containers voltam por restart: unless-stopped.";
    public const string UpdateConfirm = "Atualizar este serviço no z-server-01? O pull baixa a imagem e o up -d recria o container se ela mudou. O volume permanece.";

    /// <summary>
    /// Commands as sudo will see them (no <c>sudo -n</c> prefix). One entry per allowed line.
    /// </summary>
    public static IReadOnlyList<string> AllSudoersArgv()
    {
        var list = new List<string>();
        foreach (var service in Services)
        {
            list.Add(StripSudo(Start(service.Id)));
            list.Add(StripSudo(Stop(service.Id)));
            list.Add(StripSudo(Pull(service.Id)));
            list.Add(StripSudo(Up(service.Id)));
            list.Add(StripSudo(Ps(service.Id)));
            list.Add(StripSudo(Logs(service.Id)));
            list.Add(StripSudo(Stats(service.Id)));
        }

        list.Add(StripSudo(Uptime()));
        list.Add(StripSudo(LoadAvg()));
        list.Add(StripSudo(Memory()));
        list.Add(StripSudo(DiskRoot()));
        list.Add(StripSudo(DiskDocker()));
        list.Add(StripSudo(DiskData()));
        list.Add(StripSudo(Shutdown()));
        list.Add(StripSudo(Reboot()));
        return list;
    }

    public static string FormatSudoersDocument()
    {
        var cmds = AllSudoersArgv();
        var sb = new StringBuilder();
        sb.AppendLine("# zlab-monitor - lista fechada do Z-Lab 2.0.");
        sb.AppendLine("# Aplicar com visudo, com outra sessao SSH ja aberta pela chave.");
        sb.AppendLine("# Sem ALL e sem curinga. Se /usr/sbin/reboot for link para systemctl, troque essa linha pelo caminho invocado.");
        sb.AppendLine("Defaults:melchior !requiretty");
        sb.AppendLine("Cmnd_Alias ZLAB_MONITOR = \\");
        for (var i = 0; i < cmds.Count; i++)
        {
            var comma = i < cmds.Count - 1 ? ", \\" : "";
            sb.Append("    ");
            sb.Append(cmds[i]);
            sb.Append(comma);
            sb.AppendLine();
        }

        sb.AppendLine("melchior ALL=(root) NOPASSWD: ZLAB_MONITOR");
        return sb.ToString();
    }

    private static string Compose(string serviceId, string verbAndArgs)
    {
        var service = Require(serviceId);
        var prefix = service.TailnetProfile
            ? "/usr/bin/docker compose --profile tailnet --project-directory " + ProjectDir + " -f " + ComposeFile
            : "/usr/bin/docker compose --project-directory " + ProjectDir + " -f " + ComposeFile;
        return Sudo(prefix + " " + verbAndArgs);
    }

    private static string Sudo(string argv) => "sudo -n " + argv;

    private static string StripSudo(string command)
    {
        const string prefix = "sudo -n ";
        if (!command.StartsWith(prefix, StringComparison.Ordinal))
            throw new InvalidOperationException("Comando sem sudo -n.");
        return command[prefix.Length..];
    }
}

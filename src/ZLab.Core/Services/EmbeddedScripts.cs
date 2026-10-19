using System.Reflection;
using System.Text;

namespace ZLab.Services;

public static class EmbeddedScripts
{
    public const string StatsLogicalName = "ZLab.Scripts.zlab-stats.sh";

    public static string ReadStatsScript()
    {
        var asm = typeof(EmbeddedScripts).Assembly;
        using var stream = asm.GetManifestResourceStream(StatsLogicalName)
            ?? throw new InvalidOperationException("Recurso embutido ausente: " + StatsLogicalName);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var text = reader.ReadToEnd();
        return text.Replace("\r\n", "\n").Replace('\r', '\n');
    }
}

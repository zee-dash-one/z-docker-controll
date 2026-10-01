using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using ZLab.Models;

namespace ZLab.Services;

public sealed class LocalHostsResult
{
    public bool Ok { get; init; }
    public bool NeedsElevation { get; init; }
    public bool AlreadyConfigured { get; init; }
    public string Message { get; init; } = "";
    public IReadOnlyList<string> OpenUrls { get; init; } = Array.Empty<string>();
}

public sealed class LocalHostAlias
{
    public string ServiceId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string HostName { get; init; } = "";
    public int HostPort { get; init; }
    public string OpenUrl => $"http://{HostName}:{HostPort}";
}

/// <summary>
/// Ensures Windows hosts aliases (e.g. 9router-local) so browser cookies do not clash across ports.
/// </summary>
public sealed class LocalHostsHelper
{
    public const string MarkerComment = "# Z-Lab local aliases (cookie isolation)";

    private readonly string _hostsPath;
    private readonly Func<string, bool, CancellationToken, Task<CliResult>> _elevatedWriter;

    public LocalHostsHelper()
        : this(DefaultHostsPath(), CreateDefaultElevatedWriter(DefaultHostsPath()))
    {
    }

    public LocalHostsHelper(
        string hostsPath,
        Func<string, bool, CancellationToken, Task<CliResult>> elevatedWriter)
    {
        _hostsPath = string.IsNullOrWhiteSpace(hostsPath)
            ? DefaultHostsPath()
            : hostsPath;
        _elevatedWriter = elevatedWriter ?? throw new ArgumentNullException(nameof(elevatedWriter));
    }

    public static string DefaultHostsPath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");

    internal static Func<string, bool, CancellationToken, Task<CliResult>> CreateDefaultElevatedWriter(string hostsPath) =>
        (content, elevate, ct) => RunElevatedWriteAsync(hostsPath, content, elevate, ct);

    public static IReadOnlyList<LocalHostAlias> CollectAliases(IEnumerable<ServiceItem> services)
    {
        ArgumentNullException.ThrowIfNull(services);
        var list = new List<LocalHostAlias>();
        foreach (var s in services)
        {
            if (string.IsNullOrWhiteSpace(s.LocalHost) || s.HostPort <= 0)
                continue;

            var host = s.LocalHost.Trim().ToLowerInvariant();
            if (!IsValidHostName(host))
                continue;

            list.Add(new LocalHostAlias
            {
                ServiceId = s.Id,
                DisplayName = string.IsNullOrWhiteSpace(s.DisplayName) ? s.Id : s.DisplayName,
                HostName = host,
                HostPort = s.HostPort
            });
        }

        return list;
    }

    public static bool IsValidHostName(string host)
    {
        if (string.IsNullOrWhiteSpace(host) || host.Length > 63)
            return false;
        // Simple DNS-label-ish: letters, digits, hyphen; must start with letter/digit.
        return Regex.IsMatch(host, @"^[a-z0-9]([a-z0-9\-]*[a-z0-9])?$", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Returns true when every alias hostname appears in hosts content mapped to a loopback address.
    /// </summary>
    public static bool AreAliasesPresent(string hostsContent, IEnumerable<LocalHostAlias> aliases)
    {
        ArgumentNullException.ThrowIfNull(aliases);
        hostsContent ??= "";
        var mapped = ParseLoopbackHostNames(hostsContent);
        foreach (var a in aliases)
        {
            if (!mapped.Contains(a.HostName))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Appends missing 127.0.0.1 alias lines (idempotent). Returns (content, changed).
    /// </summary>
    public static (string Content, bool Changed) EnsureHostsContent(
        string hostsContent,
        IEnumerable<LocalHostAlias> aliases)
    {
        ArgumentNullException.ThrowIfNull(aliases);
        var aliasList = aliases.ToList();
        if (aliasList.Count == 0)
            return (hostsContent ?? "", false);

        var content = hostsContent ?? "";
        var mapped = ParseLoopbackHostNames(content);
        var missing = aliasList
            .Select(a => a.HostName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(h => !mapped.Contains(h))
            .OrderBy(h => h, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (missing.Count == 0)
            return (content, false);

        var sb = new StringBuilder(content.TrimEnd());
        if (sb.Length > 0)
            sb.AppendLine();
        if (!content.Contains(MarkerComment, StringComparison.Ordinal))
            sb.AppendLine(MarkerComment);
        foreach (var host in missing)
            sb.Append("127.0.0.1").Append('\t').Append(host).AppendLine();

        return (sb.ToString(), true);
    }

    public static void ApplyOpenUrls(IEnumerable<ServiceItem> services, IEnumerable<LocalHostAlias> aliases)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(aliases);
        var byId = aliases.ToDictionary(a => a.ServiceId, StringComparer.Ordinal);
        foreach (var s in services)
        {
            if (!byId.TryGetValue(s.Id, out var alias))
                continue;
            s.OpenUrl = alias.OpenUrl;
        }
    }

    public LocalHostsResult Peek(IEnumerable<ServiceItem> services)
    {
        var aliases = CollectAliases(services);
        if (aliases.Count == 0)
        {
            return new LocalHostsResult
            {
                Ok = false,
                Message = "Nenhum serviço com localHost configurado no catálogo."
            };
        }

        string content;
        try
        {
            content = File.Exists(_hostsPath) ? File.ReadAllText(_hostsPath) : "";
        }
        catch (Exception ex)
        {
            return new LocalHostsResult
            {
                Ok = false,
                Message = "Não foi possível ler o arquivo hosts: " + ex.Message
            };
        }

        var present = AreAliasesPresent(content, aliases);
        var urls = aliases.Select(a => a.OpenUrl).ToArray();
        return new LocalHostsResult
        {
            Ok = true,
            AlreadyConfigured = present,
            OpenUrls = urls,
            Message = present
                ? "Aliases já presentes no hosts."
                : "Aliases ainda não configurados no hosts."
        };
    }

    public async Task<LocalHostsResult> EnsureAsync(
        IEnumerable<ServiceItem> services,
        bool allowElevation = true,
        CancellationToken cancellationToken = default)
    {
        var aliases = CollectAliases(services);
        if (aliases.Count == 0)
        {
            return new LocalHostsResult
            {
                Ok = false,
                Message = "Nenhum serviço com localHost configurado no catálogo."
            };
        }

        string current;
        try
        {
            current = File.Exists(_hostsPath) ? File.ReadAllText(_hostsPath) : "";
        }
        catch (Exception ex)
        {
            return new LocalHostsResult
            {
                Ok = false,
                Message = "Não foi possível ler o arquivo hosts: " + ex.Message
            };
        }

        var (next, changed) = EnsureHostsContent(current, aliases);
        var urls = aliases.Select(a => a.OpenUrl).ToArray();
        if (!changed)
        {
            return new LocalHostsResult
            {
                Ok = true,
                AlreadyConfigured = true,
                OpenUrls = urls,
                Message = "Aliases já estavam no hosts."
            };
        }

        try
        {
            File.WriteAllText(_hostsPath, next);
            return new LocalHostsResult
            {
                Ok = true,
                OpenUrls = urls,
                Message = "Hosts atualizado."
            };
        }
        catch (UnauthorizedAccessException) when (allowElevation)
        {
            var write = await _elevatedWriter(next, true, cancellationToken).ConfigureAwait(false);
            if (!write.Succeeded)
            {
                return new LocalHostsResult
                {
                    Ok = false,
                    NeedsElevation = true,
                    OpenUrls = urls,
                    Message = "É necessário Admin para gravar o hosts. Elevação negada ou falhou: "
                              + (string.IsNullOrWhiteSpace(write.StdErr) ? write.StdOut : write.StdErr)
                };
            }

            return new LocalHostsResult
            {
                Ok = true,
                OpenUrls = urls,
                Message = "Hosts atualizado (com elevação Admin)."
            };
        }
        catch (Exception ex)
        {
            return new LocalHostsResult
            {
                Ok = false,
                Message = "Falha ao gravar hosts: " + ex.Message
            };
        }
    }

    internal static HashSet<string> ParseLoopbackHostNames(string hostsContent)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var reader = new StringReader(hostsContent ?? "");
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;

            var commentIdx = trimmed.IndexOf('#');
            if (commentIdx >= 0)
                trimmed = trimmed[..commentIdx].TrimEnd();

            var parts = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                continue;

            var ip = parts[0];
            if (!IsLoopback(ip))
                continue;

            for (var i = 1; i < parts.Length; i++)
                set.Add(parts[i].ToLowerInvariant());
        }

        return set;
    }

    private static bool IsLoopback(string ip) =>
        ip is "127.0.0.1" or "::1" or "0:0:0:0:0:0:0:1";

    private static async Task<CliResult> RunElevatedWriteAsync(
        string hostsPath,
        string content,
        bool elevate,
        CancellationToken cancellationToken)
    {
        if (!elevate)
        {
            return new CliResult { ExitCode = -1, StdErr = "Elevação necessária." };
        }

        var temp = Path.Combine(Path.GetTempPath(), "zlab-hosts-" + Guid.NewGuid().ToString("N") + ".txt");
        await File.WriteAllTextAsync(temp, content, cancellationToken).ConfigureAwait(false);
        try
        {
            var src = temp.Replace("'", "''");
            var dst = hostsPath.Replace("'", "''");
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments =
                    "-NoProfile -ExecutionPolicy Bypass -Command \"Copy-Item -LiteralPath '"
                    + src + "' -Destination '" + dst + "' -Force\"",
                UseShellExecute = true,
                Verb = "runas",
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            try
            {
                using var process = Process.Start(psi);
                if (process is null)
                {
                    return new CliResult
                    {
                        ExitCode = -1,
                        StdErr = "Não foi possível iniciar PowerShell elevado."
                    };
                }

                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                return new CliResult
                {
                    ExitCode = process.ExitCode,
                    StdErr = process.ExitCode == 0 ? "" : "Copy-Item falhou (código " + process.ExitCode + ")."
                };
            }
            catch (Exception ex)
            {
                return new CliResult { ExitCode = -1, StdErr = ex.Message };
            }
        }
        finally
        {
            try { File.Delete(temp); } catch { /* ignore */ }
        }
    }
}

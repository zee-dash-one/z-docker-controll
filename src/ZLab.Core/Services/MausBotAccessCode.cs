using System.Globalization;
using System.Text.RegularExpressions;

namespace ZLab.Services;

/// <summary>
/// One-shot pairing code from the MausBot container. The code must never be persisted.
/// </summary>
public sealed record AccessCodeResult(bool Found, string? Code, DateTimeOffset? SeenAt, string? Problem)
{
    public override string ToString()
    {
        if (Found)
            return "Found=True SeenAt=" + (SeenAt?.ToString("o", CultureInfo.InvariantCulture) ?? "null");
        return "Found=False Problem=" + (Problem ?? "");
    }
}

public sealed class MausBotAccessCode
{
    public const string ContainerId = "mausbot";
    public const string PairUrl = "https://mausbot-serve.mandrill-court.ts.net";
    public const string StoppedProblem = "O MausBot está parado. Suba o container e tente de novo.";
    public const string NotFoundProblem = "Código não encontrado no log. O MausBot pode já estar configurado.";

    /// <summary>
    /// OpenMausBot CLI / serve line, e.g. <c>pairing code:  XXXX-XXXX-XXXX</c>.
    /// </summary>
    internal static readonly Regex PairingCodeRegex = new(
        @"pairing code:\s*(\S+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex DockerTimestampRegex = new(
        @"^(?<ts>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})?)\s+",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly SshSession _session;

    public MausBotAccessCode(SshSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public Task<AccessCodeResult> FetchAsync(CancellationToken cancellationToken) =>
        Task.Run(() => Fetch(cancellationToken), cancellationToken);

    private AccessCodeResult Fetch(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ps = _session.Run(RemoteCommands.Ps(ContainerId), TimeSpan.FromSeconds(30));
        var status = ps.Succeeded
            ? ComposePsParser.Parse(ps.StdOut)
            : ContainerStatusKind.Error;
        if (status != ContainerStatusKind.Up)
            return new AccessCodeResult(false, null, null, StoppedProblem);

        cancellationToken.ThrowIfCancellationRequested();
        var logs = _session.Run(
            "docker logs --timestamps --tail 2000 mausbot 2>&1",
            TimeSpan.FromSeconds(40));
        var fromLogs = Parse(logs.CombinedText());
        if (fromLogs.Found)
            return fromLogs;

        // Codes are minted by `pair` and are not kept in the volume; when the log has none,
        // mint once and parse the same line format from stdout (never persist the value).
        cancellationToken.ThrowIfCancellationRequested();
        var pair = _session.Run(
            "docker exec mausbot node dist-server/openmausbot.js pair --label Z-Lab",
            TimeSpan.FromSeconds(60));
        var minted = Parse(pair.CombinedText(), DateTimeOffset.UtcNow);
        if (minted.Found)
            return minted;

        return new AccessCodeResult(false, null, null, NotFoundProblem);
    }

    /// <summary>
    /// Parses docker log / pair stdout. Fake codes in tests only (e.g. ABC123).
    /// </summary>
    public static AccessCodeResult Parse(string text, DateTimeOffset? fallbackSeenAt = null)
    {
        if (string.IsNullOrEmpty(text))
            return new AccessCodeResult(false, null, null, NotFoundProblem);

        string? lastCode = null;
        DateTimeOffset? lastSeen = null;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            var match = PairingCodeRegex.Match(line);
            if (!match.Success)
                continue;

            var code = match.Groups[1].Value.Trim();
            if (code.Length == 0)
                continue;

            lastCode = code;
            lastSeen = TryParseDockerTimestamp(line) ?? fallbackSeenAt;
        }

        if (lastCode is null)
            return new AccessCodeResult(false, null, null, NotFoundProblem);

        return new AccessCodeResult(true, lastCode, lastSeen ?? fallbackSeenAt, null);
    }

    /// <summary>
    /// Display-only: alphanumeric characters in groups of two, spaced. Never used for copy.
    /// </summary>
    public static string FormatForDisplay(string code)
    {
        if (string.IsNullOrEmpty(code))
            return "";

        Span<char> buf = stackalloc char[code.Length];
        var n = 0;
        foreach (var ch in code)
        {
            if (char.IsAsciiLetterOrDigit(ch))
                buf[n++] = ch;
        }

        if (n == 0)
            return code;

        var sb = new System.Text.StringBuilder(n + n / 2);
        for (var i = 0; i < n; i++)
        {
            if (i > 0 && i % 2 == 0)
                sb.Append(' ');
            sb.Append(buf[i]);
        }

        return sb.ToString();
    }

    private static DateTimeOffset? TryParseDockerTimestamp(string line)
    {
        var m = DockerTimestampRegex.Match(line);
        if (!m.Success)
            return null;
        if (DateTimeOffset.TryParse(
                m.Groups["ts"].Value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var dto))
            return dto;
        return null;
    }
}

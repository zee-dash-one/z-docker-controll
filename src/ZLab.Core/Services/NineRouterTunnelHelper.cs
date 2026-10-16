using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ZLab.Models;

namespace ZLab.Services;

public sealed class NineRouterTunnelResult
{
    public bool Ok { get; init; }
    public string Message { get; init; } = "";
    public string? TunnelUrl { get; init; }
    public string? PublicUrl { get; init; }
    public bool Running { get; init; }
}

/// <summary>
/// Enables/queries 9Router Cloudflare tunnel using the dashboard local-only CLI token.
/// Token = sha256(machine-id + "9r-cli-auth" + cli-secret).hex[:16]
/// </summary>
public sealed class NineRouterTunnelHelper
{
    public const string CliAuthSalt = "9r-cli-auth";
    public const string DefaultVolumeName = "9router-data";
    public const string MachineIdRelPath = "machine-id";
    public const string CliSecretRelPath = "auth/cli-secret";

    private readonly DockerCli _cli;
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send;

    public NineRouterTunnelHelper(DockerCli cli)
        : this(cli, DefaultSendAsync)
    {
    }

    public NineRouterTunnelHelper(
        DockerCli cli,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
    {
        _cli = cli ?? throw new ArgumentNullException(nameof(cli));
        _send = send ?? throw new ArgumentNullException(nameof(send));
    }

    public static string ComputeCliToken(string machineId, string cliSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(machineId);
        ArgumentException.ThrowIfNullOrWhiteSpace(cliSecret);
        var input = machineId.Trim() + CliAuthSalt + cliSecret.Trim();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant()[..16];
    }

    public static string ResolveVolumeName(ServiceItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        foreach (var vol in item.Volumes ?? Enumerable.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(vol))
                continue;
            var idx = vol.IndexOf(':');
            if (idx <= 0)
                continue;
            var name = vol[..idx].Trim();
            if (name.Length > 0 && !name.StartsWith('/'))
                return name;
        }

        return DefaultVolumeName;
    }

    public async Task<NineRouterTunnelResult> EnableAsync(
        ServiceItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!string.Equals(item.Id, "9router", StringComparison.Ordinal))
        {
            return new NineRouterTunnelResult
            {
                Ok = false,
                Message = "Tunnel CLI só é suportado para o serviço 9router."
            };
        }

        if (item.HostPort <= 0)
        {
            return new NineRouterTunnelResult
            {
                Ok = false,
                Message = "hostPort inválido para 9router."
            };
        }

        if (!_cli.IsAvailable)
            await _cli.DiscoverAsync(cancellationToken).ConfigureAwait(false);
        if (!_cli.IsAvailable)
        {
            return new NineRouterTunnelResult
            {
                Ok = false,
                Message = _cli.UnavailableReason ?? "Docker indisponível."
            };
        }

        var volume = ResolveVolumeName(item);
        var (machineId, cliSecret, err) = await ReadSecretsAsync(volume, cancellationToken)
            .ConfigureAwait(false);
        if (machineId is null || cliSecret is null)
        {
            return new NineRouterTunnelResult
            {
                Ok = false,
                Message = err ?? "Não foi possível ler machine-id/cli-secret do volume 9router."
            };
        }

        string token;
        try
        {
            token = ComputeCliToken(machineId, cliSecret);
        }
        catch (Exception ex)
        {
            return new NineRouterTunnelResult { Ok = false, Message = "Falha ao calcular CLI token: " + ex.Message };
        }

        var baseUrl = $"http://127.0.0.1:{item.HostPort}";
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/api/tunnel/enable");
            req.Headers.TryAddWithoutValidation("x-9r-cli-token", token);
            req.Content = new StringContent("{}", Encoding.UTF8, "application/json");
            using var resp = await _send(req, cancellationToken).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                return new NineRouterTunnelResult
                {
                    Ok = false,
                    Message = $"Tunnel enable falhou HTTP {(int)resp.StatusCode}: {TrimBody(body)}"
                };
            }

            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            var root = doc.RootElement;
            var tunnelUrl = GetString(root, "tunnelUrl");
            var publicUrl = GetString(root, "publicUrl");
            var already = root.TryGetProperty("alreadyRunning", out var ar) && ar.ValueKind == JsonValueKind.True;

            var status = await GetStatusWithTokenAsync(item, token, cancellationToken).ConfigureAwait(false);
            return new NineRouterTunnelResult
            {
                Ok = true,
                Running = status.Running || !string.IsNullOrWhiteSpace(tunnelUrl),
                TunnelUrl = tunnelUrl ?? status.TunnelUrl,
                PublicUrl = publicUrl ?? status.PublicUrl,
                Message = already
                    ? "Tunnel 9Router já estava ativo."
                    : "Tunnel 9Router ativado."
            };
        }
        catch (Exception ex)
        {
            return new NineRouterTunnelResult
            {
                Ok = false,
                Message = "Falha ao chamar /api/tunnel/enable: " + ex.Message
            };
        }
    }

    public async Task<NineRouterTunnelResult> GetStatusAsync(
        ServiceItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!_cli.IsAvailable)
            await _cli.DiscoverAsync(cancellationToken).ConfigureAwait(false);
        if (!_cli.IsAvailable)
        {
            return new NineRouterTunnelResult
            {
                Ok = false,
                Message = _cli.UnavailableReason ?? "Docker indisponível."
            };
        }

        var volume = ResolveVolumeName(item);
        var (machineId, cliSecret, err) = await ReadSecretsAsync(volume, cancellationToken)
            .ConfigureAwait(false);
        if (machineId is null || cliSecret is null)
        {
            return new NineRouterTunnelResult
            {
                Ok = false,
                Message = err ?? "Não foi possível ler machine-id/cli-secret do volume 9router."
            };
        }

        var token = ComputeCliToken(machineId, cliSecret);
        return await GetStatusWithTokenAsync(item, token, cancellationToken).ConfigureAwait(false);
    }

    private async Task<NineRouterTunnelResult> GetStatusWithTokenAsync(
        ServiceItem item,
        string token,
        CancellationToken cancellationToken)
    {
        var baseUrl = $"http://127.0.0.1:{item.HostPort}";
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, baseUrl + "/api/tunnel/status");
            req.Headers.TryAddWithoutValidation("x-9r-cli-token", token);
            using var resp = await _send(req, cancellationToken).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                return new NineRouterTunnelResult
                {
                    Ok = false,
                    Message = $"Tunnel status HTTP {(int)resp.StatusCode}: {TrimBody(body)}"
                };
            }

            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            var root = doc.RootElement;
            JsonElement tunnel = default;
            var hasTunnel = root.TryGetProperty("tunnel", out tunnel);
            var running = hasTunnel
                && tunnel.TryGetProperty("running", out var run)
                && run.ValueKind == JsonValueKind.True;
            var tunnelUrl = hasTunnel ? GetString(tunnel, "tunnelUrl") : GetString(root, "tunnelUrl");
            var publicUrl = hasTunnel ? GetString(tunnel, "publicUrl") : GetString(root, "publicUrl");

            return new NineRouterTunnelResult
            {
                Ok = true,
                Running = running,
                TunnelUrl = tunnelUrl,
                PublicUrl = publicUrl,
                Message = running
                    ? "Tunnel em execução."
                    : "Tunnel parado / inativo."
            };
        }
        catch (Exception ex)
        {
            return new NineRouterTunnelResult
            {
                Ok = false,
                Message = "Falha ao consultar tunnel status: " + ex.Message
            };
        }
    }

    private async Task<(string? MachineId, string? CliSecret, string? Error)> ReadSecretsAsync(
        string volumeName,
        CancellationToken cancellationToken)
    {
        var args = new[]
        {
            "run", "--rm",
            "-v", $"{volumeName}:/data:ro",
            "alpine:3.20",
            "sh", "-c", "cat /data/machine-id; echo; cat /data/auth/cli-secret"
        };

        var result = await _cli.RunAsync(args, cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            var msg = string.IsNullOrWhiteSpace(result.StdErr) ? result.StdOut : result.StdErr;
            return (null, null, string.IsNullOrWhiteSpace(msg) ? "Falha ao ler volume 9router." : msg.Trim());
        }

        var parts = result.StdOut.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return (null, null, "machine-id/cli-secret ausentes no volume (container já iniciou uma vez?).");

        return (parts[0].Trim(), parts[1].Trim(), null);
    }

    private static string? GetString(JsonElement el, string name) =>
        el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString()
            : null;

    private static string TrimBody(string body)
    {
        body = body.Trim();
        return body.Length <= 400 ? body : body[..400] + "…";
    }

    private static readonly HttpClient SharedHttp = new()
    {
        Timeout = TimeSpan.FromSeconds(90)
    };

    private static Task<HttpResponseMessage> DefaultSendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        SharedHttp.SendAsync(request, cancellationToken);
}

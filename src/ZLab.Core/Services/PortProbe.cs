using System.Net.Sockets;

namespace ZLab.Services;

public static class PortProbe
{
    public static async Task<bool> IsOpenAsync(string host, int port, int timeoutMs, CancellationToken cancellationToken)
    {
        using var tcp = new TcpClient();
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(timeoutMs);
            await tcp.ConnectAsync(host, port, timeout.Token).ConfigureAwait(false);
            return tcp.Connected;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    /// <summary>
    /// Waits until TCP accepts a connection, or the budget ends. Pause is between attempts.
    /// </summary>
    public static async Task<bool> WaitUntilOpenAsync(
        string host,
        int port,
        TimeSpan budget,
        TimeSpan pause,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + budget;
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await IsOpenAsync(host, port, 5000, cancellationToken).ConfigureAwait(false))
                return true;

            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
                break;
            var delay = remaining < pause ? remaining : pause;
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }
}

using System.Net.Sockets;
using System.Text;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace ZLab.Services;

/// <summary>
/// One SSH session to z-server-01. Password stays in a char buffer until
/// <see cref="EndSession"/> or dispose. The SSH library keeps its own copy until disconnect.
/// </summary>
public sealed class SshSession : IDisposable
{
    private readonly object _gate = new();
    private SshClient? _client;
    private char[]? _password;
    private string? _user;
    private string? _knownHostsText;
    private string? _lastReject;
    private bool _disposed;

    public event EventHandler? AuthFailed;

    public bool IsConnected
    {
        get
        {
            lock (_gate)
                return _client is { IsConnected: true };
        }
    }

    public string? UserName
    {
        get
        {
            lock (_gate)
                return _user;
        }
    }

    public string? LastReject
    {
        get
        {
            lock (_gate)
                return _lastReject;
        }
    }

    public void Start(string user, char[] password, string? knownHostsText)
    {
        if (string.IsNullOrWhiteSpace(user))
            throw new ArgumentException("Usuário vazio.", nameof(user));
        if (password is null || password.Length == 0)
            throw new ArgumentException("Senha vazia.", nameof(password));

        EndSession();
        lock (_gate)
        {
            _user = user.Trim();
            _password = (char[])password.Clone();
            _knownHostsText = knownHostsText ?? "";
            _lastReject = null;
        }

        Array.Clear(password, 0, password.Length);
        try
        {
            ConnectLocked();
        }
        catch
        {
            EndSession();
            throw;
        }
    }

    public void EndSession()
    {
        SshClient? client;
        lock (_gate)
        {
            client = _client;
            _client = null;
            if (_password is not null)
            {
                Array.Clear(_password, 0, _password.Length);
                _password = null;
            }

            _user = null;
        }

        DisconnectQuiet(client);
    }

    public RemoteCommandResult Run(string command, TimeSpan timeout)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Comando vazio.", nameof(command));

        try
        {
            return Execute(command, timeout);
        }
        catch (Exception ex) when (IsSocketDrop(ex))
        {
            try
            {
                ConnectLocked();
                return Execute(command, timeout);
            }
            catch (SshAuthenticationException)
            {
                AuthFailed?.Invoke(this, EventArgs.Empty);
                throw;
            }
        }
    }

    /// <summary>
    /// Runs a local script on the remote host via <c>bash -s</c> (stdin). Nothing is written to disk.
    /// </summary>
    public RemoteCommandResult RunScript(string scriptText, TimeSpan timeout)
    {
        if (string.IsNullOrWhiteSpace(scriptText))
            throw new ArgumentException("Script vazio.", nameof(scriptText));

        var normalized = scriptText.Replace("\r\n", "\n").Replace('\r', '\n');
        try
        {
            return ExecuteScript(normalized, timeout);
        }
        catch (Exception ex) when (IsSocketDrop(ex))
        {
            try
            {
                ConnectLocked();
                return ExecuteScript(normalized, timeout);
            }
            catch (SshAuthenticationException)
            {
                AuthFailed?.Invoke(this, EventArgs.Empty);
                throw;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        EndSession();
    }

    private void ConnectLocked()
    {
        string user;
        char[] password;
        string knownHosts;
        lock (_gate)
        {
            if (_user is null || _password is null)
                throw new InvalidOperationException("Sessão sem senha.");
            user = _user;
            password = (char[])_password.Clone();
            knownHosts = _knownHostsText ?? "";
            _lastReject = null;
        }

        var passwordBytes = Encoding.UTF8.GetBytes(password);
        Array.Clear(password, 0, password.Length);
        SshClient? client = null;
        try
        {
            var method = new PasswordAuthenticationMethod(user, passwordBytes);
            var info = new ConnectionInfo(RemoteCommands.Host, RemoteCommands.Port, user, method)
            {
                Timeout = TimeSpan.FromSeconds(20)
            };
            client = new SshClient(info);
            client.HostKeyReceived += (_, e) =>
            {
                var trusted = KnownHostsTrust.IsTrusted(
                    knownHosts,
                    RemoteCommands.Host,
                    RemoteCommands.Port,
                    e.HostKeyName,
                    e.HostKey);
                e.CanTrust = trusted;
                if (!trusted)
                {
                    lock (_gate)
                    {
                        _lastReject =
                            "A chave de host de 192.168.0.112 não confere com known_hosts. " +
                            "Conecte uma vez com ssh para registrar a chave. O Z-Lab não aceita chave nova.";
                    }
                }
            };
            client.Connect();
        }
        catch
        {
            DisconnectQuiet(client);
            throw;
        }
        finally
        {
            Array.Clear(passwordBytes, 0, passwordBytes.Length);
        }

        SshClient? previous;
        lock (_gate)
        {
            previous = _client;
            _client = client;
        }

        DisconnectQuiet(previous);
    }

    private RemoteCommandResult Execute(string command, TimeSpan timeout)
    {
        SshClient client;
        lock (_gate)
        {
            if (_client is not { IsConnected: true })
                ConnectLocked();
            client = _client ?? throw new InvalidOperationException("Sem conexão SSH.");
        }

        using var cmd = client.CreateCommand(command);
        cmd.CommandTimeout = timeout;
        var stdout = cmd.Execute();
        return new RemoteCommandResult
        {
            ExitCode = cmd.ExitStatus ?? 1,
            StdOut = stdout ?? "",
            StdErr = cmd.Error ?? ""
        };
    }

    private RemoteCommandResult ExecuteScript(string scriptText, TimeSpan timeout)
    {
        SshClient client;
        lock (_gate)
        {
            if (_client is not { IsConnected: true })
                ConnectLocked();
            client = _client ?? throw new InvalidOperationException("Sem conexão SSH.");
        }

        using var cmd = client.CreateCommand("bash -s");
        cmd.CommandTimeout = timeout;
        var asyncResult = cmd.BeginExecute();
        using (var writer = new StreamWriter(cmd.CreateInputStream(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
        {
            writer.Write(scriptText);
            writer.Flush();
        }

        cmd.EndExecute(asyncResult);
        return new RemoteCommandResult
        {
            ExitCode = cmd.ExitStatus ?? 1,
            StdOut = cmd.Result ?? "",
            StdErr = cmd.Error ?? ""
        };
    }

    private static bool IsSocketDrop(Exception ex) =>
        ex is SshConnectionException or SocketException or ObjectDisposedException
        || (ex.InnerException is not null && IsSocketDrop(ex.InnerException));

    private static void DisconnectQuiet(SshClient? client)
    {
        if (client is null)
            return;
        try
        {
            if (client.IsConnected)
                client.Disconnect();
        }
        catch
        {
            // already gone
        }

        try
        {
            client.Dispose();
        }
        catch
        {
            // ignore
        }
    }
}

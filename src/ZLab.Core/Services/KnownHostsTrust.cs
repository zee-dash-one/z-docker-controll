using System.Security.Cryptography;
using System.Text;

namespace ZLab.Services;

/// <summary>
/// Accepts a server host key only when it matches an entry already in known_hosts.
/// Plain lines and OpenSSH |1| hashed hosts are both checked. New keys are refused.
/// </summary>
public static class KnownHostsTrust
{
    public static bool IsTrusted(string? knownHostsText, string host, int port, string keyType, byte[] hostKey)
    {
        if (string.IsNullOrWhiteSpace(knownHostsText) || hostKey is null || hostKey.Length == 0)
            return false;
        if (string.IsNullOrWhiteSpace(keyType))
            return false;

        var presented = new HashSet<string>(StringComparer.Ordinal)
        {
            Convert.ToBase64String(hostKey),
            Convert.ToBase64String(EncodeWireKey(keyType, hostKey))
        };

        var names = new[]
        {
            host,
            "[" + host + "]:" + port.ToString()
        };

        foreach (var raw in knownHostsText.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;
            if (line.StartsWith("@revoked", StringComparison.Ordinal))
                continue;
            if (line.StartsWith('@'))
            {
                var afterMarker = line.IndexOf(' ');
                if (afterMarker < 0)
                    continue;
                line = line[(afterMarker + 1)..].Trim();
            }

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
                continue;

            var marker = parts[0];
            var type = parts[1];
            var keyB64 = parts[2];
            if (!string.Equals(type, keyType, StringComparison.Ordinal))
                continue;
            if (!presented.Contains(keyB64))
                continue;
            if (HostMatches(marker, names))
                return true;
        }

        return false;
    }

    public static byte[] EncodeWireKey(string keyType, byte[] key)
    {
        var typeBytes = Encoding.ASCII.GetBytes(keyType);
        using var ms = new MemoryStream();
        WriteSshString(ms, typeBytes);
        WriteSshString(ms, key);
        return ms.ToArray();
    }

    private static bool HostMatches(string marker, IReadOnlyList<string> names)
    {
        if (marker.StartsWith("|1|", StringComparison.Ordinal))
        {
            var bits = marker.Split('|');
            if (bits.Length != 4)
                return false;
            foreach (var name in names)
            {
                if (HashedNameMatches(bits[2], bits[3], name))
                    return true;
            }

            return false;
        }

        foreach (var token in marker.Split(','))
        {
            foreach (var name in names)
            {
                if (string.Equals(token, name, StringComparison.Ordinal))
                    return true;
            }
        }

        return false;
    }

    private static bool HashedNameMatches(string saltB64, string hashB64, string hostname)
    {
        try
        {
            var salt = Convert.FromBase64String(saltB64);
            var expected = Convert.FromBase64String(hashB64);
            using var hmac = new HMACSHA1(salt);
            var actual = hmac.ComputeHash(Encoding.ASCII.GetBytes(hostname));
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static void WriteSshString(Stream stream, byte[] data)
    {
        var len = BitConverter.GetBytes(data.Length);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(len);
        stream.Write(len, 0, 4);
        stream.Write(data, 0, data.Length);
    }
}

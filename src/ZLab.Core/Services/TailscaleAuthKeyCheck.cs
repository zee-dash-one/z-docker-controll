namespace ZLab.Services;

/// <summary>
/// Reads TS_AUTHKEY from an env file without returning the secret.
/// </summary>
public static class TailscaleAuthKeyCheck
{
    public const string KeyName = "TS_AUTHKEY";
    public const string Placeholder = "tskey-auth-XXXXXXXX";

    public static string ToWindowsPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "";

        var normalized = path.Trim().Replace('\\', '/');
        if (normalized.StartsWith("/mnt/", StringComparison.OrdinalIgnoreCase)
            && normalized.Length > 7
            && normalized[6] == '/')
        {
            var drive = char.ToUpperInvariant(normalized[5]);
            if (drive is >= 'A' and <= 'Z')
            {
                var rest = normalized[7..].Replace('/', '\\');
                return drive + ":\\" + rest;
            }
        }

        return path.Trim();
    }

    /// <summary>
    /// Null when the file contains a real key. Otherwise a Portuguese message that does not include the key.
    /// </summary>
    public static string? Validate(string envFile)
    {
        if (string.IsNullOrWhiteSpace(envFile))
            return null;

        var windowsPath = ToWindowsPath(envFile);
        if (string.IsNullOrWhiteSpace(windowsPath) || !File.Exists(windowsPath))
        {
            return "Arquivo .env do Tailscale não encontrado. Siga 05-conta-e-chave.md e grave TS_AUTHKEY em C:\\GIT\\ZAPFCOORP\\tailscale-9router\\.env.";
        }

        string text;
        try
        {
            text = File.ReadAllText(windowsPath);
        }
        catch (Exception)
        {
            return "Não foi possível ler o .env do Tailscale.";
        }

        var value = ReadValue(text, KeyName);
        if (string.IsNullOrWhiteSpace(value) || IsPlaceholder(value))
        {
            return "TS_AUTHKEY vazia ou ainda é o modelo. Gere a chave no admin do Tailscale com a tag tag:9router e cole no .env. Não envie a chave no chat.";
        }

        return null;
    }

    private static bool IsPlaceholder(string value)
    {
        return value.Equals(Placeholder, StringComparison.Ordinal)
               || value.Contains("XXXXXXXX", StringComparison.Ordinal);
    }

    private static string? ReadValue(string text, string key)
    {
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim().TrimEnd('\r');
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var eq = line.IndexOf('=');
            if (eq <= 0)
                continue;

            var name = line[..eq].Trim();
            if (!name.Equals(key, StringComparison.OrdinalIgnoreCase))
                continue;

            var value = line[(eq + 1)..].Trim();
            if (value.Length >= 2
                && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            return value;
        }

        return null;
    }
}

using System.Globalization;
using System.Text;

namespace ZLab.Services;

public enum OutputLineKind
{
    Info,
    Warn,
    Error
}

public sealed record OutputLine(DateTime Timestamp, string Message, OutputLineKind Kind);

/// <summary>
/// Ring buffer for the Saída panel. Pure; no UI.
/// </summary>
public sealed class OutputBuffer
{
    public const int MaxLines = 1000;

    private readonly List<OutputLine> _lines = new();
    private int _unread;

    public IReadOnlyList<OutputLine> Lines => _lines;

    public int Count => _lines.Count;

    public int UnreadCount => _unread;

    public OutputLine? Last => _lines.Count == 0 ? null : _lines[^1];

    public void Append(string message, OutputLineKind? kind = null)
    {
        var text = (message ?? "").Replace("\r\n", "\n").TrimEnd();
        if (text.Length == 0)
            return;

        // Multi-line remote output → one buffer line per line of text.
        foreach (var part in text.Split('\n'))
        {
            var lineText = part.TrimEnd('\r');
            if (lineText.Length == 0)
                continue;
            var k = kind ?? Classify(lineText);
            _lines.Add(new OutputLine(DateTime.Now, lineText, k));
            _unread++;
            while (_lines.Count > MaxLines)
            {
                _lines.RemoveAt(0);
                if (_unread > _lines.Count)
                    _unread = _lines.Count;
            }
        }
    }

    public void MarkRead() => _unread = 0;

    public void Clear()
    {
        _lines.Clear();
        _unread = 0;
    }

    public string FormatLine(OutputLine line) =>
        line.Timestamp.ToString("HH:mm:ss", CultureInfo.GetCultureInfo("pt-BR")) + "  " + line.Message;

    public string FormatAll()
    {
        var sb = new StringBuilder();
        foreach (var line in _lines)
        {
            if (sb.Length > 0)
                sb.AppendLine();
            sb.Append(FormatLine(line));
        }

        return sb.ToString();
    }

    public static OutputLineKind Classify(string message)
    {
        if (string.IsNullOrEmpty(message))
            return OutputLineKind.Info;
        if (ContainsAny(message, "falhou", "falha", "erro", "error", "exception"))
            return OutputLineKind.Error;
        if (ContainsAny(message, "aviso", "warn", "atenção", "atencao"))
            return OutputLineKind.Warn;
        return OutputLineKind.Info;
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        foreach (var n in needles)
        {
            if (text.Contains(n, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}

namespace ZLab.Services;

public sealed class RemoteCommandResult
{
    public int ExitCode { get; init; }
    public string StdOut { get; init; } = "";
    public string StdErr { get; init; } = "";
    public bool Succeeded => ExitCode == 0;

    public string CombinedText()
    {
        var stderr = (StdErr ?? "").Trim();
        var stdout = (StdOut ?? "").Trim();
        if (stderr.Length == 0)
            return stdout;
        if (stdout.Length == 0)
            return stderr;
        return stdout + Environment.NewLine + stderr;
    }
}

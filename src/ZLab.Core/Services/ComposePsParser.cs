using System.Text.Json;

namespace ZLab.Services;

public static class ComposePsParser
{
    public static ContainerStatusKind Parse(string? stdout)
    {
        if (string.IsNullOrWhiteSpace(stdout))
            return ContainerStatusKind.Absent;

        ContainerStatusKind? found = null;
        foreach (var raw in stdout.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] != '{')
                continue;

            try
            {
                using var doc = JsonDocument.Parse(line);
                string? state = null;
                if (doc.RootElement.TryGetProperty("State", out var stateProp))
                    state = stateProp.GetString();
                found = Map(state);
            }
            catch (JsonException)
            {
                found = ContainerStatusKind.Error;
            }
        }

        return found ?? ContainerStatusKind.Absent;
    }

    private static ContainerStatusKind Map(string? state) => (state ?? "").ToLowerInvariant() switch
    {
        "running" => ContainerStatusKind.Up,
        "restarting" => ContainerStatusKind.Busy,
        "created" or "exited" or "paused" or "dead" or "removing" => ContainerStatusKind.Down,
        "" => ContainerStatusKind.Absent,
        _ => ContainerStatusKind.Down
    };
}

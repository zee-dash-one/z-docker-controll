namespace ZLab.Services;

public sealed class MonitorSettings
{
    public int PollSeconds { get; set; } = 8;
    public int LogTailLines { get; set; } = 80;
}

/// <summary>
/// Operator interval only. The six services are not stored here.
/// </summary>
public sealed class MonitorSettingsStore
{
    private readonly string _path;

    public MonitorSettingsStore(string directory)
    {
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "settings.json");
    }

    public string FilePath => _path;

    public MonitorSettings Current { get; private set; } = new();

    public void Load()
    {
        if (!File.Exists(_path))
        {
            Current = new MonitorSettings();
            Save();
            return;
        }

        try
        {
            var json = File.ReadAllText(_path);
            var parsed = System.Text.Json.JsonSerializer.Deserialize<MonitorSettings>(json);
            Current = parsed ?? new MonitorSettings();
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or IOException)
        {
            Current = new MonitorSettings();
        }

        if (Current.PollSeconds < 2)
            Current.PollSeconds = 8;
        if (Current.LogTailLines < 1)
            Current.LogTailLines = 80;
    }

    public void Save()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(Current, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(_path, json);
    }
}

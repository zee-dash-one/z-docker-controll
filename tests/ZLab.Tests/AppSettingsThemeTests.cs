using System.Text.Json;
using System.Text.Json.Serialization;
using ZLab.Models;
using ZLab.Services;

namespace ZLab.Tests;

public class AppSettingsThemeTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void AppSettings_theme_defaults_to_system()
    {
        Assert.Equal(ThemeMode.System, new AppSettings().Theme);
    }

    [Fact]
    public void AppSettings_theme_roundtrips_json()
    {
        var settings = new AppSettings { Theme = ThemeMode.Dark };
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        var parsed = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
        Assert.NotNull(parsed);
        Assert.Equal(ThemeMode.Dark, parsed!.Theme);
        Assert.Contains("\"Theme\": \"Dark\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void MonitorSettingsStore_persists_theme()
    {
        var dir = Path.Combine(Path.GetTempPath(), "zlab-theme-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new MonitorSettingsStore(dir);
            store.Load();
            store.Current.Theme = ThemeMode.Light;
            store.Save();

            var again = new MonitorSettingsStore(dir);
            again.Load();
            Assert.Equal(ThemeMode.Light, again.Current.Theme);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }
}

using System.Text.Json;
using ZLab.Services;

namespace ZLab.Tests;

public sealed class ServiceCatalogStoreTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _runtimeDir;
    private readonly string _seedPath;

    public ServiceCatalogStoreTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "ZLab.Tests", Guid.NewGuid().ToString("N"));
        _runtimeDir = Path.Combine(_tempRoot, "AppData", "ZLab");
        Directory.CreateDirectory(_tempRoot);

        var fixtureSeed = Path.Combine(AppContext.BaseDirectory, "Fixtures", "services.default.json");
        if (!File.Exists(fixtureSeed))
        {
            // Fallback: repo-relative path when running under debugger without copy
            fixtureSeed = FindRepoSeed();
        }

        _seedPath = Path.Combine(_tempRoot, "services.default.json");
        File.Copy(fixtureSeed, _seedPath, overwrite: true);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    private static string FindRepoSeed()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "config", "services.default.json");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new FileNotFoundException("Could not locate config/services.default.json");
    }

    private ServiceCatalogStore CreateStore() => new(_runtimeDir, _seedPath);

    [Fact]
    public void EnsureSeeded_WhenRuntimeMissing_CreatesRuntimeJsonFromSeed()
    {
        var store = CreateStore();
        Assert.False(File.Exists(store.RuntimeFilePath));

        store.EnsureSeeded();

        Assert.True(Directory.Exists(_runtimeDir));
        Assert.True(File.Exists(store.RuntimeFilePath));

        var json = File.ReadAllText(store.RuntimeFilePath);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(1, doc.RootElement.GetProperty("version").GetInt32());
        Assert.True(doc.RootElement.TryGetProperty("settings", out _));
        Assert.True(doc.RootElement.TryGetProperty("services", out var services));
        Assert.True(services.GetArrayLength() > 0);
    }

    [Fact]
    public void EnsureSeeded_DefaultEnabled_Only9routerAndOmniroute()
    {
        var store = CreateStore();
        store.EnsureSeeded();
        var loaded = store.Load();

        var enabledIds = loaded.Services.Where(s => s.Enabled).Select(s => s.Id).OrderBy(x => x).ToArray();
        Assert.Equal(new[] { "9router", "omniroute" }, enabledIds);

        var disabled = loaded.Services.Where(s => !s.Enabled).Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("postgres", disabled);
        Assert.Contains("redis", disabled);
        Assert.Contains("n8n", disabled);
    }

    [Fact]
    public void Load_RoundTrips_SettingsDefaultsFromSeed()
    {
        var store = CreateStore();
        store.EnsureSeeded();
        var doc = store.Load();

        Assert.Equal(1, doc.Version);
        Assert.Equal("", doc.Settings.BackupFolder);
        Assert.Equal(8, doc.Settings.PollSeconds);
        Assert.Equal(80, doc.Settings.LogTailLines);
        Assert.Contains(doc.Services, s => s.Id == "9router" && s.Managed && s.Enabled);
        Assert.Contains(doc.Services, s =>
            s.Id == "9router"
            && s.Volumes is not null
            && s.Volumes.Contains("9router-data:/app/data"));
        Assert.Contains(doc.Services, s =>
            s.Id == "9router"
            && s.Env.TryGetValue("INITIAL_PASSWORD", out var pwd)
            && pwd == "123456");
        Assert.Contains(doc.Services, s => s.Id == "omniroute" && s.StopTimeoutSeconds == 40);
    }

    [Fact]
    public void Load_MigratesEmpty9routerVolumes_ToNamedVolume()
    {
        var store = CreateStore();
        store.EnsureSeeded();
        var json = File.ReadAllText(store.RuntimeFilePath);

        var rewritten = System.Text.RegularExpressions.Regex.Replace(
            json,
            @"(""id""\s*:\s*""9router""[\s\S]*?""volumes""\s*:\s*)\[[^\]]*\]",
            "$1[]",
            System.Text.RegularExpressions.RegexOptions.Multiline);
        Assert.Contains("\"volumes\": []", rewritten);
        File.WriteAllText(store.RuntimeFilePath, rewritten);

        var doc = store.Load();
        var nine = Assert.Single(doc.Services, s => s.Id == "9router");
        Assert.Contains("9router-data:/app/data", nine.Volumes!);

        using var raw = JsonDocument.Parse(File.ReadAllText(store.RuntimeFilePath));
        var volumes = raw.RootElement.GetProperty("services").EnumerateArray()
            .Single(e => e.GetProperty("id").GetString() == "9router")
            .GetProperty("volumes");
        Assert.Equal("9router-data:/app/data", volumes[0].GetString());
    }

    [Fact]
    public void Load_MigratesOmnirouteOpenUrl_AwayFromLocalhostCookieClash()
    {
        var store = CreateStore();
        store.EnsureSeeded();

        var json = File.ReadAllText(store.RuntimeFilePath);
        using (var doc = JsonDocument.Parse(json))
        {
            using var stream = File.Create(store.RuntimeFilePath);
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            writer.WriteStartObject();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Name != "services")
                {
                    prop.WriteTo(writer);
                    continue;
                }

                writer.WritePropertyName("services");
                writer.WriteStartArray();
                foreach (var svc in prop.Value.EnumerateArray())
                {
                    writer.WriteStartObject();
                    foreach (var sp in svc.EnumerateObject())
                    {
                        if (sp.Name == "openUrl" && svc.GetProperty("id").GetString() == "omniroute")
                        {
                            writer.WriteString("openUrl", "http://localhost:20129");
                            continue;
                        }

                        sp.WriteTo(writer);
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        var loaded = store.Load();
        var omni = Assert.Single(loaded.Services, s => s.Id == "omniroute");
        Assert.Equal("http://127.0.0.1:20129", omni.OpenUrl);
    }

    [Fact]
    public void Load_MigratesMissingLocalHostAliases()
    {
        var store = CreateStore();
        store.EnsureSeeded();
        var json = File.ReadAllText(store.RuntimeFilePath);
        using (var doc = JsonDocument.Parse(json))
        {
            using var stream = File.Create(store.RuntimeFilePath);
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            writer.WriteStartObject();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Name != "services")
                {
                    prop.WriteTo(writer);
                    continue;
                }

                writer.WritePropertyName("services");
                writer.WriteStartArray();
                foreach (var svc in prop.Value.EnumerateArray())
                {
                    writer.WriteStartObject();
                    foreach (var sp in svc.EnumerateObject())
                    {
                        if (sp.Name == "localHost")
                            continue;
                        sp.WriteTo(writer);
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        var loaded = store.Load();
        Assert.Equal("9router-local", Assert.Single(loaded.Services, s => s.Id == "9router").LocalHost);
        Assert.Equal("omniroute-local", Assert.Single(loaded.Services, s => s.Id == "omniroute").LocalHost);
    }

    [Fact]
    public void Load_MigratesMissing9routerInitialPassword()
    {
        var store = CreateStore();
        store.EnsureSeeded();
        var json = File.ReadAllText(store.RuntimeFilePath);

        // Strip INITIAL_PASSWORD from 9router env (simulate older catalog).
        using (var doc = JsonDocument.Parse(json))
        {
            using var stream = File.Create(store.RuntimeFilePath);
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            writer.WriteStartObject();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Name != "services")
                {
                    prop.WriteTo(writer);
                    continue;
                }

                writer.WritePropertyName("services");
                writer.WriteStartArray();
                foreach (var svc in prop.Value.EnumerateArray())
                {
                    writer.WriteStartObject();
                    foreach (var sp in svc.EnumerateObject())
                    {
                        if (sp.Name == "env" && svc.GetProperty("id").GetString() == "9router")
                        {
                            writer.WritePropertyName("env");
                            writer.WriteStartObject();
                            writer.WriteEndObject();
                            continue;
                        }

                        sp.WriteTo(writer);
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        var loaded = store.Load();
        var nine = Assert.Single(loaded.Services, s => s.Id == "9router");
        Assert.Equal("123456", nine.Env["INITIAL_PASSWORD"]);
    }

    [Fact]
    public void SetEnabled_PersistsEnabledTrue_ForPostgres()
    {
        var store = CreateStore();
        store.EnsureSeeded();
        store.Load();

        var updated = store.SetEnabled("postgres", enabled: true);

        Assert.Contains(updated.Services, s => s.Id == "postgres" && s.Enabled);

        using var raw = JsonDocument.Parse(File.ReadAllText(store.RuntimeFilePath));
        var postgres = raw.RootElement.GetProperty("services").EnumerateArray()
            .Single(e => e.GetProperty("id").GetString() == "postgres");
        Assert.True(postgres.GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public void SetEnabled_PersistsEnabledFalse_ForOmniroute()
    {
        var store = CreateStore();
        store.EnsureSeeded();
        store.Load();

        var updated = store.SetEnabled("omniroute", enabled: false);

        Assert.Contains(updated.Services, s => s.Id == "omniroute" && !s.Enabled);

        using var raw = JsonDocument.Parse(File.ReadAllText(store.RuntimeFilePath));
        var item = raw.RootElement.GetProperty("services").EnumerateArray()
            .Single(e => e.GetProperty("id").GetString() == "omniroute");
        Assert.False(item.GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public void SetEnabled_DoesNotMutateSeedFile()
    {
        var seedBefore = File.ReadAllText(_seedPath);
        var store = CreateStore();
        store.EnsureSeeded();
        store.Load();
        store.SetEnabled("postgres", true);

        var seedAfter = File.ReadAllText(_seedPath);
        Assert.Equal(seedBefore, seedAfter);
    }

    [Fact]
    public void Reload_AfterManualJsonEdit_ReturnsNewValuesWithoutRestart()
    {
        var store = CreateStore();
        store.EnsureSeeded();
        store.Load();

        var path = store.RuntimeFilePath;
        var root = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        var services = root["services"]!.AsArray();
        var redis = services.Single(n => n!["id"]!.GetValue<string>() == "redis")!.AsObject();
        redis["enabled"] = true;
        File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        var ok = store.TryReload(out var error);
        Assert.True(ok, error);
        Assert.Null(error);
        Assert.NotNull(store.Document);
        Assert.Contains(store.Document!.Services, s => s.Id == "redis" && s.Enabled);
    }

    [Fact]
    public void TryReload_CorruptJson_KeepsLastGoodDocument_AndReportsError()
    {
        var store = CreateStore();
        store.EnsureSeeded();
        var good = store.Load();
        var serviceCount = good.Services.Count;
        Assert.True(serviceCount > 0);

        File.WriteAllText(store.RuntimeFilePath, "{ this is not valid json !!!");

        var ok = store.TryReload(out var error);

        Assert.False(ok);
        Assert.False(string.IsNullOrWhiteSpace(error));
        Assert.NotNull(store.Document);
        Assert.Equal(serviceCount, store.Document!.Services.Count);
        Assert.Contains(store.Document.Services, s => s.Id == "9router");
    }

    [Fact]
    public void Load_AfterRestartSimulation_ReflectsPersistedEnabled()
    {
        var store1 = CreateStore();
        store1.EnsureSeeded();
        store1.Load();
        store1.SetEnabled("n8n", true);

        var store2 = new ServiceCatalogStore(_runtimeDir, _seedPath);
        var reloaded = store2.Load();

        Assert.Contains(reloaded.Services, s => s.Id == "n8n" && s.Enabled);
    }

    [Fact]
    public void GetConfigDirectory_ReturnsRuntimeDirectory()
    {
        var store = CreateStore();
        Assert.Equal(_runtimeDir, store.GetConfigDirectory());
    }
}

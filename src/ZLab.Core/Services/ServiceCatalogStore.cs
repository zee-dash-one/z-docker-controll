using System.Text.Json;
using System.Text.Json.Serialization;
using ZLab.Models;

namespace ZLab.Services;

/// <summary>
/// Persistence boundary for %AppData%\ZLab\services.json.
/// </summary>
public sealed class ServiceCatalogStore
{
    public const string RuntimeFileName = "services.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly string _runtimeDirectory;
    private readonly string _seedFilePath;
    private readonly object _gate = new();

    public ServiceCatalogStore(string runtimeDirectory, string seedFilePath)
    {
        _runtimeDirectory = runtimeDirectory ?? throw new ArgumentNullException(nameof(runtimeDirectory));
        _seedFilePath = seedFilePath ?? throw new ArgumentNullException(nameof(seedFilePath));
    }

    public string RuntimeDirectory => _runtimeDirectory;

    public string RuntimeFilePath => Path.Combine(_runtimeDirectory, RuntimeFileName);

    public string SeedFilePath => _seedFilePath;

    public ServicesDocument? Document { get; private set; }

    public string GetConfigDirectory() => _runtimeDirectory;

    /// <summary>
    /// If runtime JSON is missing, create directory and copy seed bytes to runtime path.
    /// </summary>
    public void EnsureSeeded()
    {
        lock (_gate)
        {
            if (File.Exists(RuntimeFilePath))
                return;

            if (!File.Exists(_seedFilePath))
                throw new FileNotFoundException("Arquivo seed do catálogo não encontrado.", _seedFilePath);

            Directory.CreateDirectory(_runtimeDirectory);
            File.Copy(_seedFilePath, RuntimeFilePath, overwrite: false);
        }
    }

    /// <summary>
    /// Parse runtime JSON into in-memory document. Calls EnsureSeeded first when file missing.
    /// Applies light migrations (e.g. 9router volume) and persists when changed.
    /// </summary>
    public ServicesDocument Load()
    {
        lock (_gate)
        {
            EnsureSeededUnlocked();
            var doc = ReadDocumentFromDisk();
            if (ApplyMigrations(doc))
                WriteDocumentAtomic(doc);
            Document = doc;
            return doc;
        }
    }

    /// <summary>
    /// Persist document to runtime JSON (atomic write).
    /// </summary>
    public void Save(ServicesDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        lock (_gate)
        {
            Directory.CreateDirectory(_runtimeDirectory);
            WriteDocumentAtomic(document);
            Document = document;
        }
    }

    /// <summary>
    /// Toggle enabled for id, save, return updated document.
    /// </summary>
    public ServicesDocument SetEnabled(string id, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id do serviço é obrigatório.", nameof(id));

        lock (_gate)
        {
            var doc = Document ?? LoadUnlocked();
            var item = doc.Services.FirstOrDefault(s =>
                string.Equals(s.Id, id, StringComparison.Ordinal));
            if (item is null)
                throw new KeyNotFoundException($"Serviço '{id}' não encontrado no catálogo.");

            item.Enabled = enabled;
            WriteDocumentAtomic(doc);
            Document = doc;
            return doc;
        }
    }

    /// <summary>
    /// Re-read runtime file. On parse failure keep last good Document and return false with error.
    /// </summary>
    public bool TryReload(out string? error)
    {
        lock (_gate)
        {
            try
            {
                EnsureSeededUnlocked();
                var doc = ReadDocumentFromDisk();
                if (ApplyMigrations(doc))
                    WriteDocumentAtomic(doc);
                Document = doc;
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }

    /// <summary>
    /// Soft migrations for older runtime catalogs. Returns true when document was mutated.
    /// </summary>
    internal static bool ApplyMigrations(ServicesDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);
        doc.Services ??= new List<ServiceItem>();
        var changed = false;

        var nine = doc.Services.FirstOrDefault(s =>
            string.Equals(s.Id, "9router", StringComparison.Ordinal));
        if (nine is not null)
        {
            nine.Volumes ??= new List<string>();
            if (nine.Volumes.Count == 0)
            {
                nine.Volumes.Add("9router-data:/app/data");
                changed = true;
            }

            nine.Env ??= new Dictionary<string, string>(StringComparer.Ordinal);
            if (!nine.Env.ContainsKey("INITIAL_PASSWORD"))
            {
                nine.Env["INITIAL_PASSWORD"] = "123456";
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(nine.LocalHost))
            {
                nine.LocalHost = "9router-local";
                changed = true;
            }
        }

        // Both apps set cookie "auth_token" on host; browsers share cookies across ports
        // for the same host. Prefer 127.0.0.1 for OmniRoute until localHost aliases are applied.
        var omni = doc.Services.FirstOrDefault(s =>
            string.Equals(s.Id, "omniroute", StringComparison.Ordinal));
        if (omni is not null)
        {
            if (string.IsNullOrWhiteSpace(omni.LocalHost))
            {
                omni.LocalHost = "omniroute-local";
                changed = true;
            }

            if (string.Equals(omni.OpenUrl, "http://localhost:20129", StringComparison.OrdinalIgnoreCase))
            {
                omni.OpenUrl = "http://127.0.0.1:20129";
                changed = true;
            }
        }

        return changed;
    }

    private void EnsureSeededUnlocked()
    {
        if (File.Exists(RuntimeFilePath))
            return;

        if (!File.Exists(_seedFilePath))
            throw new FileNotFoundException("Arquivo seed do catálogo não encontrado.", _seedFilePath);

        Directory.CreateDirectory(_runtimeDirectory);
        File.Copy(_seedFilePath, RuntimeFilePath, overwrite: false);
    }

    private ServicesDocument LoadUnlocked()
    {
        EnsureSeededUnlocked();
        var doc = ReadDocumentFromDisk();
        if (ApplyMigrations(doc))
            WriteDocumentAtomic(doc);
        Document = doc;
        return doc;
    }

    private ServicesDocument ReadDocumentFromDisk()
    {
        var json = File.ReadAllText(RuntimeFilePath);
        var doc = JsonSerializer.Deserialize<ServicesDocument>(json, JsonOptions)
                  ?? throw new InvalidDataException("JSON do catálogo é nulo.");
        doc.Services ??= new List<ServiceItem>();
        doc.Settings ??= new AppSettings();
        return doc;
    }

    private void WriteDocumentAtomic(ServicesDocument document)
    {
        Directory.CreateDirectory(_runtimeDirectory);
        var json = JsonSerializer.Serialize(document, JsonOptions);
        var tempPath = RuntimeFilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllText(tempPath, json);
        if (File.Exists(RuntimeFilePath))
            File.Replace(tempPath, RuntimeFilePath, destinationBackupFileName: null);
        else
            File.Move(tempPath, RuntimeFilePath);
    }
}

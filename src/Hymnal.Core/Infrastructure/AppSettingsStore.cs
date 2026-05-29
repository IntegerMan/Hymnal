using System.Text.Json;
using System.Text.Json.Serialization;
using Hymnal.Core.Interfaces;

namespace Hymnal.Core.Infrastructure;

public class AppSettingsStore : IAppSettingsStore
{
    private readonly string _settingsPath;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AppSettingsStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _settingsPath = Path.Combine(appData, "Hymnal", "settings.json");
    }

    // Constructor overload for testing — allows injecting a custom path
    internal AppSettingsStore(string settingsPath)
    {
        _settingsPath = settingsPath;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        if (!File.Exists(_settingsPath))
            return default;

        var json = await File.ReadAllTextAsync(_settingsPath).ConfigureAwait(false);
        var doc = JsonSerializer.Deserialize<SettingsFile>(json, SerializerOptions);
        if (doc?.Values == null || !doc.Values.TryGetValue(key, out var element))
            return default;

        return element.Deserialize<T>(SerializerOptions);
    }

    public async Task SetAsync<T>(string key, T value)
    {
        SettingsFile doc;
        if (File.Exists(_settingsPath))
        {
            var existing = await File.ReadAllTextAsync(_settingsPath).ConfigureAwait(false);
            doc = JsonSerializer.Deserialize<SettingsFile>(existing, SerializerOptions) ?? new SettingsFile();
        }
        else
        {
            doc = new SettingsFile();
        }

        doc.Values ??= new Dictionary<string, JsonElement>();
        var serialized = JsonSerializer.SerializeToElement(value, SerializerOptions);
        doc.Values[key] = serialized;

        var dir = Path.GetDirectoryName(_settingsPath)!;
        Directory.CreateDirectory(dir);

        var tempPath = Path.Combine(dir, Path.GetRandomFileName());
        var jsonOut = JsonSerializer.Serialize(doc, SerializerOptions);
        await File.WriteAllTextAsync(tempPath, jsonOut).ConfigureAwait(false);
        File.Move(tempPath, _settingsPath, overwrite: true);
    }

    private sealed class SettingsFile
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, JsonElement>? Values { get; set; }
    }
}

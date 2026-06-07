using System.Text.Json;
using System.Text.Json.Serialization;
using Hymnal.Core.Interfaces;

namespace Hymnal.Core.Infrastructure;

public class AppSettingsStore : IAppSettingsStore, IDisposable
{
    private readonly string _settingsPath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Dictionary<string, JsonElement> _cache = new(StringComparer.Ordinal);
    private bool _cacheLoaded;
    private Timer? _flushTimer;
    private bool _dirty;

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
        await EnsureCacheLoadedAsync().ConfigureAwait(false);

        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!_cache.TryGetValue(key, out var element))
                return default;

            return element.Deserialize<T>(SerializerOptions);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SetAsync<T>(string key, T value)
    {
        await EnsureCacheLoadedAsync().ConfigureAwait(false);

        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            _cache[key] = JsonSerializer.SerializeToElement(value, SerializerOptions);
            _dirty = true;
            ScheduleFlush();
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        _flushTimer?.Dispose();
        _flushTimer = null;
        _lock.Dispose();
    }

    private async Task EnsureCacheLoadedAsync()
    {
        if (_cacheLoaded)
            return;

        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_cacheLoaded)
                return;

            _cache.Clear();
            if (File.Exists(_settingsPath))
            {
                var json = await File.ReadAllTextAsync(_settingsPath).ConfigureAwait(false);
                var doc = JsonSerializer.Deserialize<SettingsFile>(json, SerializerOptions);
                if (doc?.Values != null)
                {
                    foreach (var (key, value) in doc.Values)
                        _cache[key] = value;
                }
            }

            _cacheLoaded = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    private void ScheduleFlush()
    {
        _flushTimer?.Dispose();
        _flushTimer = new Timer(
            _ => _ = FlushAsync(),
            null,
            500,
            Timeout.Infinite);
    }

    internal Task FlushPendingAsync() => FlushAsync();

    private async Task FlushAsync()
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!_dirty)
                return;

            var doc = new SettingsFile { Values = new Dictionary<string, JsonElement>(_cache) };

            var dir = Path.GetDirectoryName(_settingsPath)!;
            Directory.CreateDirectory(dir);

            var tempPath = Path.Combine(dir, Path.GetRandomFileName());
            var jsonOut = JsonSerializer.Serialize(doc, SerializerOptions);
            await File.WriteAllTextAsync(tempPath, jsonOut).ConfigureAwait(false);
            File.Move(tempPath, _settingsPath, overwrite: true);
            _dirty = false;
        }
        finally
        {
            _lock.Release();
        }
    }

    private sealed class SettingsFile
    {
        public int SchemaVersion { get; set; } = 1;
        public Dictionary<string, JsonElement>? Values { get; set; }
    }
}

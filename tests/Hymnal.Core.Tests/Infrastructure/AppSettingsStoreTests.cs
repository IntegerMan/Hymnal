using Hymnal.Core.Infrastructure;

namespace Hymnal.Core.Tests.Infrastructure;

public sealed class AppSettingsStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _settingsPath;
    private readonly AppSettingsStore _store;

    public AppSettingsStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "HymnalTests_" + Guid.NewGuid().ToString("N"));
        _settingsPath = Path.Combine(_tempDir, "settings.json");
        _store = new AppSettingsStore(_settingsPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task RoundTrip_StringValue()
    {
        await _store.SetAsync("workspace", "/home/user/book");
        await _store.FlushPendingAsync();
        var result = await _store.GetAsync<string>("workspace");
        Assert.Equal("/home/user/book", result);
    }

    [Fact]
    public async Task RoundTrip_NullableType_MissingKey()
    {
        var result = await _store.GetAsync<string>("nonexistent");
        Assert.Null(result);
    }

    [Fact]
    public async Task AtomicWrite_DirectoryCreated()
    {
        Assert.False(Directory.Exists(_tempDir));
        await _store.SetAsync("key", "value");
        await _store.FlushPendingAsync();
        Assert.True(File.Exists(_settingsPath));
    }
}

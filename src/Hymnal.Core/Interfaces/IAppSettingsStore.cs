namespace Hymnal.Core.Interfaces;

public interface IAppSettingsStore
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value);
}

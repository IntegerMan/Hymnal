namespace Hymnal.Core.Interfaces;

public interface ICredentialStore
{
    Task StoreAsync(string key, string value);
    Task<string?> RetrieveAsync(string key);
    Task DeleteAsync(string key);
}

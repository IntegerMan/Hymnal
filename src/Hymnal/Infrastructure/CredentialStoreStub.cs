using System.Collections.Generic;
using System.Threading.Tasks;
using Hymnal.Core.Interfaces;

namespace Hymnal.Infrastructure;

/// <summary>
/// In-memory credential store stub. Real implementation deferred to a future milestone.
/// </summary>
public class CredentialStoreStub : ICredentialStore
{
    private readonly Dictionary<string, string> _store = new();

    public Task StoreAsync(string key, string value)
    {
        _store[key] = value;
        return Task.CompletedTask;
    }

    public Task<string?> RetrieveAsync(string key) =>
        Task.FromResult(_store.TryGetValue(key, out var value) ? value : null);

    public Task DeleteAsync(string key)
    {
        _store.Remove(key);
        return Task.CompletedTask;
    }
}

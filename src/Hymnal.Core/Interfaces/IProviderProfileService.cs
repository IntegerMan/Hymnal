using Hymnal.Core.Common;
using Hymnal.Core.Models.Ai;

namespace Hymnal.Core.Interfaces;

public interface IProviderProfileService
{
    Task<IReadOnlyList<ProviderProfile>> LoadAllAsync();
    Task<ProviderProfile?> GetActiveAsync();
    Task SaveAsync(ProviderProfile profile);
    Task DeleteAsync(string profileId);
    Task SetActiveAsync(string profileId);
    Task<Result<Unit>> TestConnectionAsync(ProviderProfile profile, string apiKey, CancellationToken ct);
}

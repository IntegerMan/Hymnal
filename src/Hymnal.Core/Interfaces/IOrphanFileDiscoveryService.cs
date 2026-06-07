using Hymnal.Core.Models;

namespace Hymnal.Core.Interfaces;

public interface IOrphanFileDiscoveryService
{
    /// <summary>
    /// Finds <c>.md</c> files under <paramref name="manuscriptRoot"/> that are not present in
    /// <paramref name="bookTxtEntries"/> (forward-slash normalized paths).
    /// </summary>
    Task<IReadOnlyList<OrphanFileInfo>> DiscoverAsync(
        string manuscriptRoot,
        IReadOnlyList<string> bookTxtEntries);
}

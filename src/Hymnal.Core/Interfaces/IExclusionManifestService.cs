using Hymnal.Core.Common;
using Hymnal.Core.Models;

namespace Hymnal.Core.Interfaces;

/// <summary>
/// Persists the workspace exclusion manifest under .hymnal-data/exclusions.json.
/// </summary>
public interface IExclusionManifestService
{
    /// <summary>
    /// Loads intentional exclusions. Missing manifests return an empty manifest; malformed manifests return failure.
    /// Stale entries whose files no longer exist are omitted without rewriting the manifest.
    /// </summary>
    Task<Result<ExclusionManifest>> LoadAsync(string workspaceRoot);

    /// <summary>
    /// Saves exclusions atomically, normalizing paths, de-duplicating case variants, and pruning stale entries.
    /// </summary>
    Task<Result<ExclusionManifest>> SaveAsync(string workspaceRoot, ExclusionManifest manifest);

    /// <summary>
    /// Adds a manuscript-relative path to the exclusion manifest, then atomically saves the pruned manifest.
    /// </summary>
    Task<Result<ExclusionManifest>> ExcludeAsync(string workspaceRoot, string relativePath);

    /// <summary>
    /// Removes a manuscript-relative path from the exclusion manifest, then atomically saves the pruned manifest.
    /// </summary>
    Task<Result<ExclusionManifest>> IncludeAsync(string workspaceRoot, string relativePath);
}

using Hymnal.Core.Common;
using Hymnal.Core.Models;

namespace Hymnal.Core.Interfaces;

/// <summary>
/// Loads and creates user-authored supplemental documents under a workspace's .hymnal-data/docs/ directory.
/// </summary>
public interface ISupplementalDocsService
{
    /// <summary>
    /// Derives the canonical supplemental docs root for a workspace.
    /// </summary>
    static string DeriveDocsRoot(string workspaceRoot)
        => Path.Combine(workspaceRoot, ".hymnal-data", "docs");

    /// <summary>
    /// Loads the supplemental docs tree, creating the docs root if it does not exist.
    /// </summary>
    Task<Result<IReadOnlyList<SupplementalDocNode>>> LoadTreeAsync(string workspaceRoot);

    /// <summary>
    /// Creates a folder below the docs root or below <paramref name="parentRelativePath"/> when provided.
    /// </summary>
    Task<Result<SupplementalDocNode>> CreateFolderAsync(string workspaceRoot, string? parentRelativePath, string folderName);

    /// <summary>
    /// Creates a file below the docs root or below <paramref name="parentRelativePath"/> when provided.
    /// Initial content is written through the atomic metadata store.
    /// </summary>
    Task<Result<SupplementalDocNode>> CreateFileAsync(string workspaceRoot, string? parentRelativePath, string fileName, string initialContent = "");
}

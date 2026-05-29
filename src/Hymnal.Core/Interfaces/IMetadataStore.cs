namespace Hymnal.Core.Interfaces;

/// <summary>
/// Provides an atomic text-write seam for saving chapter content and metadata.
/// Implementations must write via a temp-file-then-rename pattern to prevent partial writes.
/// </summary>
public interface IMetadataStore
{
    /// <summary>
    /// Writes <paramref name="content"/> to <paramref name="absolutePath"/> atomically
    /// using a temp-file-then-rename strategy. Parent directories are created automatically.
    /// </summary>
    Task WriteTextAtomicAsync(string absolutePath, string content);
}

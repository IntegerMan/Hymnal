using Hymnal.Core.Interfaces;

namespace Hymnal.Core.Infrastructure;

/// <summary>
/// File-backed atomic text writer. Writes via temp-file-then-rename so a crash
/// during the write never leaves a partial file at the target path.
/// </summary>
public sealed class MetadataStore : IMetadataStore
{
    /// <inheritdoc/>
    public async Task WriteTextAtomicAsync(string absolutePath, string content)
    {
        var dir = Path.GetDirectoryName(absolutePath)
            ?? throw new ArgumentException("absolutePath must include a directory component.", nameof(absolutePath));

        Directory.CreateDirectory(dir);

        var tempPath = Path.Combine(dir, Path.GetRandomFileName());
        try
        {
            await File.WriteAllTextAsync(tempPath, content).ConfigureAwait(false);
            File.Move(tempPath, absolutePath, overwrite: true);
        }
        catch
        {
            // Best-effort cleanup of the temp file on failure.
            try { File.Delete(tempPath); } catch { /* ignored */ }
            throw;
        }
    }
}

namespace Hymnal.Core.Interfaces;

/// <summary>
/// Read-only access to manuscript file content for context building.
/// Never writes to any file.
/// </summary>
public interface IManuscriptContextReader
{
    /// <summary>
    /// Reads the text of a single chapter file. Returns empty string if the file is missing.
    /// </summary>
    Task<string> ReadChapterTextAsync(string workspaceRoot, string chapterRelativePath, CancellationToken ct);

    /// <summary>
    /// Returns all (relativePath, title) entries from Book.txt, in document order.
    /// </summary>
    Task<IReadOnlyList<(string RelativePath, string Title, bool IsPart)>> ReadBookOrderAsync(
        string bookTxtPath, CancellationToken ct);
}

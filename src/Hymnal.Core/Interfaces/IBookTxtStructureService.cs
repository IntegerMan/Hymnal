using Hymnal.Core.Common;

namespace Hymnal.Core.Interfaces;

public interface IBookTxtStructureService
{
    Task<Result<IReadOnlyList<string>>> ReadNormalizedEntriesAsync(string bookTxtPath);

    Task<Result<Unit>> ReorderEntryAsync(string bookTxtPath, string chapterPath, int newIndex);

    Task<Result<Unit>> RenameEntryAsync(string bookTxtPath, string existingPath, string replacementPath);

    /// <summary>
    /// Moves an included chapter file to a replacement relative path, rewrites Book.txt at the requested entry index,
    /// and preserves chapter-registry identity for the moved chapter.
    /// </summary>
    Task<Result<Unit>> MoveEntryAsync(string bookTxtPath, string existingPath, string replacementPath, int newIndex)
        => throw new NotSupportedException("Moving entries is not supported by this implementation.");

    Task<Result<Unit>> AddExistingEntryAsync(string bookTxtPath, string chapterPath, int index);

    Task<Result<Unit>> AddExistingEntryAfterPartAsync(string bookTxtPath, string chapterPath, string partPath);

    /// <summary>
    /// Intentionally includes an existing excluded file by inserting it into Book.txt at the requested entry index
    /// and removing it from the exclusion manifest.
    /// </summary>
    Task<Result<Unit>> IncludeExistingEntryAsync(string bookTxtPath, string chapterPath, int index)
        => throw new NotSupportedException("Including excluded entries is not supported by this implementation.");

    /// <summary>
    /// Intentionally includes an existing excluded file by inserting it into Book.txt after the specified Part divider
    /// and removing it from the exclusion manifest.
    /// </summary>
    Task<Result<Unit>> IncludeExistingEntryAfterPartAsync(string bookTxtPath, string chapterPath, string partPath)
        => throw new NotSupportedException("Including excluded entries after a Part is not supported by this implementation.");

    Task<Result<Unit>> CreateNewChapterAsync(string bookTxtPath, string chapterPath, string content, int index);

    Task<Result<Unit>> CreateNewPartAsync(string bookTxtPath, string partPath, string title, int index);

    Task<Result<Unit>> RemoveEntryAsync(string bookTxtPath, string chapterPath);

    /// <summary>
    /// Intentionally excludes an included entry by removing it from Book.txt and adding it to the exclusion manifest.
    /// </summary>
    Task<Result<Unit>> ExcludeEntryAsync(string bookTxtPath, string chapterPath)
        => throw new NotSupportedException("Excluding entries is not supported by this implementation.");

    Task<Result<Unit>> DeleteChapterFileAsync(string bookTxtPath, string chapterPath);
}

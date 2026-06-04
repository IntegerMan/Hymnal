using Hymnal.Core.Common;

namespace Hymnal.Core.Interfaces;

public interface IBookTxtStructureService
{
    Task<Result<IReadOnlyList<string>>> ReadNormalizedEntriesAsync(string bookTxtPath);

    Task<Result<Unit>> ReorderEntryAsync(string bookTxtPath, string chapterPath, int newIndex);

    Task<Result<Unit>> RenameEntryAsync(string bookTxtPath, string existingPath, string replacementPath);

    Task<Result<Unit>> AddExistingEntryAsync(string bookTxtPath, string chapterPath, int index);

    Task<Result<Unit>> AddExistingEntryAfterPartAsync(string bookTxtPath, string chapterPath, string partPath);

    Task<Result<Unit>> CreateNewChapterAsync(string bookTxtPath, string chapterPath, string content, int index);

    Task<Result<Unit>> RemoveEntryAsync(string bookTxtPath, string chapterPath);

    Task<Result<Unit>> DeleteChapterFileAsync(string bookTxtPath, string chapterPath);
}

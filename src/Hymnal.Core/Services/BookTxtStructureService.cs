using Hymnal.Core.Common;
using Hymnal.Core.Infrastructure;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;

namespace Hymnal.Core.Services;

public sealed class BookTxtStructureService : IBookTxtStructureService
{
    private readonly IMetadataStore _metadataStore;
    private readonly IExclusionManifestService _exclusionManifestService;
    private readonly ChapterRegistryService _chapterRegistryService;

    public BookTxtStructureService(IMetadataStore metadataStore)
        : this(metadataStore, new ExclusionManifestService(metadataStore), new ChapterRegistryService(metadataStore))
    {
    }

    public BookTxtStructureService(IMetadataStore metadataStore, IExclusionManifestService exclusionManifestService)
        : this(metadataStore, exclusionManifestService, new ChapterRegistryService(metadataStore))
    {
    }

    public BookTxtStructureService(
        IMetadataStore metadataStore,
        IExclusionManifestService exclusionManifestService,
        ChapterRegistryService chapterRegistryService)
    {
        _metadataStore = metadataStore;
        _exclusionManifestService = exclusionManifestService;
        _chapterRegistryService = chapterRegistryService;
    }

    public async Task<Result<IReadOnlyList<string>>> ReadNormalizedEntriesAsync(string bookTxtPath)
    {
        var document = await LoadDocumentAsync(bookTxtPath).ConfigureAwait(false);
        if (!document.IsSuccess)
            return Result<IReadOnlyList<string>>.Fail(document.Error!);

        return Result<IReadOnlyList<string>>.Ok(document.Value!.Entries.AsReadOnly());
    }

    public async Task<Result<Unit>> ReorderEntryAsync(string bookTxtPath, string chapterPath, int newIndex)
    {
        var document = await LoadDocumentAsync(bookTxtPath).ConfigureAwait(false);
        if (!document.IsSuccess)
            return Result<Unit>.Fail(document.Error!);

        var doc = document.Value!;
        if (doc.Entries.Count == 0)
            return Result<Unit>.Fail($"Book.txt at '{doc.BookTxtPath}' does not contain any entries to reorder.");

        var normalized = NormalizeStructurePath(doc.ManuscriptRoot, chapterPath, nameof(chapterPath));
        if (!normalized.IsSuccess)
            return Result<Unit>.Fail(normalized.Error!);

        var sourceIndex = FindEntryIndex(doc.Entries, normalized.Value!.RelativePath);
        if (sourceIndex < 0)
            return Result<Unit>.Fail($"Entry '{normalized.Value.RelativePath}' was not found in '{doc.BookTxtPath}'.");

        if (newIndex < 0 || newIndex >= doc.Entries.Count)
            return Result<Unit>.Fail($"Requested reorder index {newIndex} is out of range for '{doc.BookTxtPath}'.");

        if (sourceIndex == newIndex)
            return Result<Unit>.Ok(Unit.Default);

        var lines = doc.RawLines.ToList();
        var sourceRawIndex = doc.EntryLineIndexes[sourceIndex];
        var movingLine = lines[sourceRawIndex];
        lines.RemoveAt(sourceRawIndex);

        var insertionIndex = FindRawInsertionIndex(lines, newIndex);
        lines.Insert(insertionIndex, movingLine);

        var write = await WriteBookTxtAsync(doc.BookTxtPath, lines).ConfigureAwait(false);
        return write.IsSuccess ? Result<Unit>.Ok(Unit.Default) : Result<Unit>.Fail(write.Error!);
    }

    public async Task<Result<Unit>> RenameEntryAsync(string bookTxtPath, string existingPath, string replacementPath)
    {
        var document = await LoadDocumentAsync(bookTxtPath).ConfigureAwait(false);
        if (!document.IsSuccess)
            return Result<Unit>.Fail(document.Error!);

        var doc = document.Value!;
        var sourcePath = NormalizeStructurePath(doc.ManuscriptRoot, existingPath, nameof(existingPath));
        if (!sourcePath.IsSuccess)
            return Result<Unit>.Fail(sourcePath.Error!);

        var replacement = NormalizeStructurePath(doc.ManuscriptRoot, replacementPath, nameof(replacementPath));
        if (!replacement.IsSuccess)
            return Result<Unit>.Fail(replacement.Error!);

        if (string.Equals(sourcePath.Value!.RelativePath, replacement.Value!.RelativePath, StringComparison.OrdinalIgnoreCase))
            return Result<Unit>.Ok(Unit.Default);

        var sourceIndex = FindEntryIndex(doc.Entries, sourcePath.Value.RelativePath);
        if (sourceIndex < 0)
            return Result<Unit>.Fail($"Entry '{sourcePath.Value.RelativePath}' was not found in '{doc.BookTxtPath}'.");

        if (FindEntryIndex(doc.Entries, replacement.Value.RelativePath) >= 0)
            return Result<Unit>.Fail($"Entry '{replacement.Value.RelativePath}' already exists in '{doc.BookTxtPath}'.");

        if (!File.Exists(replacement.Value.AbsolutePath))
            return Result<Unit>.Fail($"Replacement file '{replacement.Value.AbsolutePath}' does not exist.");

        var lines = doc.RawLines.ToList();
        lines[doc.EntryLineIndexes[sourceIndex]] = replacement.Value.RelativePath;

        var write = await WriteBookTxtAsync(doc.BookTxtPath, lines).ConfigureAwait(false);
        return write.IsSuccess ? Result<Unit>.Ok(Unit.Default) : Result<Unit>.Fail(write.Error!);
    }

    public async Task<Result<Unit>> MoveEntryAsync(string bookTxtPath, string existingPath, string replacementPath, int newIndex)
    {
        var document = await LoadDocumentAsync(bookTxtPath).ConfigureAwait(false);
        if (!document.IsSuccess)
            return Result<Unit>.Fail(document.Error!);

        var doc = document.Value!;
        var sourcePath = NormalizeStructurePath(doc.ManuscriptRoot, existingPath, nameof(existingPath));
        if (!sourcePath.IsSuccess)
            return Result<Unit>.Fail(sourcePath.Error!);

        var replacement = NormalizeStructurePath(doc.ManuscriptRoot, replacementPath, nameof(replacementPath));
        if (!replacement.IsSuccess)
            return Result<Unit>.Fail(replacement.Error!);

        if (string.Equals(sourcePath.Value!.RelativePath, replacement.Value!.RelativePath, StringComparison.Ordinal))
            return Result<Unit>.Ok(Unit.Default);

        if (string.Equals(sourcePath.Value.RelativePath, replacement.Value.RelativePath, StringComparison.OrdinalIgnoreCase))
            return Result<Unit>.Fail($"Move operation failed for '{sourcePath.Value.RelativePath}' to '{replacement.Value.RelativePath}' during path validation: case-only path moves are not supported.");

        var sourceIndex = FindEntryIndex(doc.Entries, sourcePath.Value.RelativePath);
        if (sourceIndex < 0)
            return Result<Unit>.Fail($"Move operation failed for '{sourcePath.Value.RelativePath}' to '{replacement.Value.RelativePath}' during Book.txt validation: source entry was not found in '{doc.BookTxtPath}'.");

        if (FindEntryIndex(doc.Entries, replacement.Value.RelativePath) >= 0)
            return Result<Unit>.Fail($"Move operation failed for '{sourcePath.Value.RelativePath}' to '{replacement.Value.RelativePath}' during Book.txt validation: target entry already exists in '{doc.BookTxtPath}'.");

        if (newIndex < 0 || newIndex >= doc.Entries.Count)
            return Result<Unit>.Fail($"Move operation failed for '{sourcePath.Value.RelativePath}' to '{replacement.Value.RelativePath}' during Book.txt validation: requested move index {newIndex} is out of range for '{doc.BookTxtPath}'.");

        if (!File.Exists(sourcePath.Value.AbsolutePath))
            return Result<Unit>.Fail($"Move operation failed for '{sourcePath.Value.RelativePath}' to '{replacement.Value.RelativePath}' during file move validation: source file '{sourcePath.Value.AbsolutePath}' does not exist.");

        if (File.Exists(replacement.Value.AbsolutePath))
            return Result<Unit>.Fail($"Move operation failed for '{sourcePath.Value.RelativePath}' to '{replacement.Value.RelativePath}' during file move validation: target file '{replacement.Value.AbsolutePath}' already exists.");

        var registryValidation = await BuildRegistryMoveAsync(
            doc.ManuscriptRoot,
            sourcePath.Value.RelativePath,
            replacement.Value.RelativePath).ConfigureAwait(false);
        if (!registryValidation.IsSuccess)
            return Result<Unit>.Fail($"Move operation failed for '{sourcePath.Value.RelativePath}' to '{replacement.Value.RelativePath}' during registry validation: {registryValidation.Error}");

        var originalLines = doc.RawLines.ToList();
        var updatedLines = BuildMoveLines(doc, sourceIndex, replacement.Value.RelativePath, newIndex);

        try
        {
            var targetDirectory = Path.GetDirectoryName(replacement.Value.AbsolutePath);
            if (!string.IsNullOrWhiteSpace(targetDirectory))
                Directory.CreateDirectory(targetDirectory);

            File.Move(sourcePath.Value.AbsolutePath, replacement.Value.AbsolutePath);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Fail($"Move operation failed for '{sourcePath.Value.RelativePath}' to '{replacement.Value.RelativePath}' during file move from '{sourcePath.Value.AbsolutePath}' to '{replacement.Value.AbsolutePath}': {ex.Message}");
        }

        var bookWrite = await WriteBookTxtAsync(doc.BookTxtPath, updatedLines).ConfigureAwait(false);
        if (!bookWrite.IsSuccess)
            return await RollBackMoveAsync(
                doc.BookTxtPath,
                sourcePath.Value,
                replacement.Value,
                restoreBookTxtLines: null,
                failurePhase: "Book.txt write",
                failureDetail: bookWrite.Error!).ConfigureAwait(false);

        try
        {
            await _chapterRegistryService.SaveAsync(doc.ManuscriptRoot, registryValidation.Value!).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return await RollBackMoveAsync(
                doc.BookTxtPath,
                sourcePath.Value,
                replacement.Value,
                originalLines,
                failurePhase: "registry update",
                failureDetail: ex.Message).ConfigureAwait(false);
        }

        var include = await _exclusionManifestService.IncludeAsync(doc.ManuscriptRoot, replacement.Value.RelativePath).ConfigureAwait(false);
        if (!include.IsSuccess)
            return Result<Unit>.Fail($"Move operation failed for '{sourcePath.Value.RelativePath}' to '{replacement.Value.RelativePath}' during manifest save after file move, Book.txt write, and registry update: {include.Error}");

        return Result<Unit>.Ok(Unit.Default);
    }

    public async Task<Result<Unit>> AddExistingEntryAsync(string bookTxtPath, string chapterPath, int index)
    {
        var document = await LoadDocumentAsync(bookTxtPath).ConfigureAwait(false);
        if (!document.IsSuccess)
            return Result<Unit>.Fail(document.Error!);

        var doc = document.Value!;
        var normalized = NormalizeStructurePath(doc.ManuscriptRoot, chapterPath, nameof(chapterPath));
        if (!normalized.IsSuccess)
            return Result<Unit>.Fail(normalized.Error!);

        if (FindEntryIndex(doc.Entries, normalized.Value!.RelativePath) >= 0)
            return Result<Unit>.Fail($"Entry '{normalized.Value.RelativePath}' already exists in '{doc.BookTxtPath}'.");

        if (!File.Exists(normalized.Value.AbsolutePath))
            return Result<Unit>.Fail($"Chapter file '{normalized.Value.AbsolutePath}' does not exist.");

        if (index < 0 || index > doc.Entries.Count)
            return Result<Unit>.Fail($"Requested insert index {index} is out of range for '{doc.BookTxtPath}'.");

        var lines = doc.RawLines.ToList();
        var insertionIndex = index == doc.Entries.Count ? lines.Count : doc.EntryLineIndexes[index];
        lines.Insert(insertionIndex, normalized.Value.RelativePath);

        var write = await WriteBookTxtAsync(doc.BookTxtPath, lines).ConfigureAwait(false);
        return write.IsSuccess ? Result<Unit>.Ok(Unit.Default) : Result<Unit>.Fail(write.Error!);
    }

    public async Task<Result<Unit>> AddExistingEntryAfterPartAsync(string bookTxtPath, string chapterPath, string partPath)
    {
        var document = await LoadDocumentAsync(bookTxtPath).ConfigureAwait(false);
        if (!document.IsSuccess)
            return Result<Unit>.Fail(document.Error!);

        var doc = document.Value!;
        var normalizedChapter = NormalizeStructurePath(doc.ManuscriptRoot, chapterPath, nameof(chapterPath));
        if (!normalizedChapter.IsSuccess)
            return Result<Unit>.Fail(normalizedChapter.Error!);

        if (FindEntryIndex(doc.Entries, normalizedChapter.Value!.RelativePath) >= 0)
            return Result<Unit>.Fail($"Entry '{normalizedChapter.Value.RelativePath}' already exists in '{doc.BookTxtPath}'.");

        if (!File.Exists(normalizedChapter.Value.AbsolutePath))
            return Result<Unit>.Fail($"Chapter file '{normalizedChapter.Value.AbsolutePath}' does not exist.");

        var normalizedPart = NormalizeStructurePath(doc.ManuscriptRoot, partPath, nameof(partPath));
        if (!normalizedPart.IsSuccess)
            return Result<Unit>.Fail(normalizedPart.Error!);

        var partIndex = FindEntryIndex(doc.Entries, normalizedPart.Value!.RelativePath);
        if (partIndex < 0)
            return Result<Unit>.Fail($"Part entry '{normalizedPart.Value.RelativePath}' was not found in '{doc.BookTxtPath}'.");

        var partValidation = ValidatePartDivider(doc.ManuscriptRoot, normalizedPart.Value.RelativePath);
        if (!partValidation.IsSuccess)
            return Result<Unit>.Fail(partValidation.Error!);

        var lines = doc.RawLines.ToList();
        var insertionIndex = doc.EntryLineIndexes[partIndex] + 1;
        while (insertionIndex < lines.Count && string.IsNullOrWhiteSpace(lines[insertionIndex]))
            insertionIndex++;

        lines.Insert(insertionIndex, normalizedChapter.Value.RelativePath);

        var write = await WriteBookTxtAsync(doc.BookTxtPath, lines).ConfigureAwait(false);
        return write.IsSuccess ? Result<Unit>.Ok(Unit.Default) : Result<Unit>.Fail(write.Error!);
    }

    public async Task<Result<Unit>> IncludeExistingEntryAsync(string bookTxtPath, string chapterPath, int index)
    {
        var document = await LoadDocumentAsync(bookTxtPath).ConfigureAwait(false);
        if (!document.IsSuccess)
            return Result<Unit>.Fail(document.Error!);

        var doc = document.Value!;
        var normalized = NormalizeStructurePath(doc.ManuscriptRoot, chapterPath, nameof(chapterPath));
        if (!normalized.IsSuccess)
            return Result<Unit>.Fail(normalized.Error!);

        var add = await AddExistingEntryAsync(bookTxtPath, normalized.Value!.RelativePath, index).ConfigureAwait(false);
        if (!add.IsSuccess)
            return Result<Unit>.Fail($"Include operation failed for '{normalized.Value.RelativePath}' during Book.txt write or validation: {add.Error}");

        var include = await _exclusionManifestService.IncludeAsync(doc.ManuscriptRoot, normalized.Value.RelativePath).ConfigureAwait(false);
        if (!include.IsSuccess)
            return Result<Unit>.Fail($"Include operation failed for '{normalized.Value.RelativePath}' during manifest save after Book.txt write: {include.Error}");

        return Result<Unit>.Ok(Unit.Default);
    }

    public async Task<Result<Unit>> IncludeExistingEntryAfterPartAsync(string bookTxtPath, string chapterPath, string partPath)
    {
        var document = await LoadDocumentAsync(bookTxtPath).ConfigureAwait(false);
        if (!document.IsSuccess)
            return Result<Unit>.Fail(document.Error!);

        var doc = document.Value!;
        var normalized = NormalizeStructurePath(doc.ManuscriptRoot, chapterPath, nameof(chapterPath));
        if (!normalized.IsSuccess)
            return Result<Unit>.Fail(normalized.Error!);

        var add = await AddExistingEntryAfterPartAsync(bookTxtPath, normalized.Value!.RelativePath, partPath).ConfigureAwait(false);
        if (!add.IsSuccess)
            return Result<Unit>.Fail($"Include operation failed for '{normalized.Value.RelativePath}' during Book.txt write or validation: {add.Error}");

        var include = await _exclusionManifestService.IncludeAsync(doc.ManuscriptRoot, normalized.Value.RelativePath).ConfigureAwait(false);
        if (!include.IsSuccess)
            return Result<Unit>.Fail($"Include operation failed for '{normalized.Value.RelativePath}' during manifest save after Book.txt write: {include.Error}");

        return Result<Unit>.Ok(Unit.Default);
    }

    public async Task<Result<Unit>> CreateNewChapterAsync(string bookTxtPath, string chapterPath, string content, int index)
    {
        var document = await LoadDocumentAsync(bookTxtPath).ConfigureAwait(false);
        if (!document.IsSuccess)
            return Result<Unit>.Fail(document.Error!);

        var doc = document.Value!;
        var normalized = NormalizeStructurePath(doc.ManuscriptRoot, chapterPath, nameof(chapterPath));
        if (!normalized.IsSuccess)
            return Result<Unit>.Fail(normalized.Error!);

        if (FindEntryIndex(doc.Entries, normalized.Value!.RelativePath) >= 0)
            return Result<Unit>.Fail($"Entry '{normalized.Value.RelativePath}' already exists in '{doc.BookTxtPath}'.");

        if (File.Exists(normalized.Value.AbsolutePath))
            return Result<Unit>.Fail($"Chapter file '{normalized.Value.AbsolutePath}' already exists.");

        if (index < 0 || index > doc.Entries.Count)
            return Result<Unit>.Fail($"Requested insert index {index} is out of range for '{doc.BookTxtPath}'.");

        try
        {
            await _metadataStore.WriteTextAtomicAsync(normalized.Value.AbsolutePath, content).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Fail($"Failed to create chapter file '{normalized.Value.AbsolutePath}': {ex.Message}");
        }

        var lines = doc.RawLines.ToList();
        var insertionIndex = index == doc.Entries.Count ? lines.Count : doc.EntryLineIndexes[index];
        lines.Insert(insertionIndex, normalized.Value.RelativePath);

        var write = await WriteBookTxtAsync(doc.BookTxtPath, lines).ConfigureAwait(false);
        if (write.IsSuccess)
            return Result<Unit>.Ok(Unit.Default);

        TryDeleteFile(normalized.Value.AbsolutePath);
        return Result<Unit>.Fail(write.Error!);
    }

    public async Task<Result<Unit>> CreateNewPartAsync(string bookTxtPath, string partPath, string title, int index)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result<Unit>.Fail("Part title is required.");

        var document = await LoadDocumentAsync(bookTxtPath).ConfigureAwait(false);
        if (!document.IsSuccess)
            return Result<Unit>.Fail(document.Error!);

        var doc = document.Value!;
        var normalized = NormalizeStructurePath(doc.ManuscriptRoot, partPath, nameof(partPath));
        if (!normalized.IsSuccess)
            return Result<Unit>.Fail(normalized.Error!);

        if (FindEntryIndex(doc.Entries, normalized.Value!.RelativePath) >= 0)
            return Result<Unit>.Fail($"Entry '{normalized.Value.RelativePath}' already exists in '{doc.BookTxtPath}'.");

        if (File.Exists(normalized.Value.AbsolutePath))
            return Result<Unit>.Fail($"Part file '{normalized.Value.AbsolutePath}' already exists.");

        if (index < 0 || index > doc.Entries.Count)
            return Result<Unit>.Fail($"Requested insert index {index} is out of range for '{doc.BookTxtPath}'.");

        var content = $"{{class: part}}\n\n# {title.Trim()}\n";
        try
        {
            var directory = Path.GetDirectoryName(normalized.Value.AbsolutePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            await _metadataStore.WriteTextAtomicAsync(normalized.Value.AbsolutePath, content).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Fail($"Failed to create part file '{normalized.Value.AbsolutePath}': {ex.Message}");
        }

        var lines = doc.RawLines.ToList();
        var insertionIndex = index == doc.Entries.Count ? lines.Count : doc.EntryLineIndexes[index];
        lines.Insert(insertionIndex, normalized.Value.RelativePath);

        var write = await WriteBookTxtAsync(doc.BookTxtPath, lines).ConfigureAwait(false);
        if (write.IsSuccess)
            return Result<Unit>.Ok(Unit.Default);

        TryDeleteFile(normalized.Value.AbsolutePath);
        return Result<Unit>.Fail(write.Error!);
    }

    public async Task<Result<Unit>> RemoveEntryAsync(string bookTxtPath, string chapterPath)
    {
        var document = await LoadDocumentAsync(bookTxtPath).ConfigureAwait(false);
        if (!document.IsSuccess)
            return Result<Unit>.Fail(document.Error!);

        var doc = document.Value!;
        var normalized = NormalizeStructurePath(doc.ManuscriptRoot, chapterPath, nameof(chapterPath));
        if (!normalized.IsSuccess)
            return Result<Unit>.Fail(normalized.Error!);

        var entryIndex = FindEntryIndex(doc.Entries, normalized.Value!.RelativePath);
        if (entryIndex < 0)
            return Result<Unit>.Fail($"Entry '{normalized.Value.RelativePath}' was not found in '{doc.BookTxtPath}'.");

        var lines = doc.RawLines.ToList();
        lines.RemoveAt(doc.EntryLineIndexes[entryIndex]);

        var write = await WriteBookTxtAsync(doc.BookTxtPath, lines).ConfigureAwait(false);
        return write.IsSuccess ? Result<Unit>.Ok(Unit.Default) : Result<Unit>.Fail(write.Error!);
    }

    public async Task<Result<Unit>> ExcludeEntryAsync(string bookTxtPath, string chapterPath)
    {
        var document = await LoadDocumentAsync(bookTxtPath).ConfigureAwait(false);
        if (!document.IsSuccess)
            return Result<Unit>.Fail(document.Error!);

        var doc = document.Value!;
        var normalized = NormalizeStructurePath(doc.ManuscriptRoot, chapterPath, nameof(chapterPath));
        if (!normalized.IsSuccess)
            return Result<Unit>.Fail(normalized.Error!);

        var remove = await RemoveEntryAsync(bookTxtPath, normalized.Value!.RelativePath).ConfigureAwait(false);
        if (!remove.IsSuccess)
            return Result<Unit>.Fail($"Exclude operation failed for '{normalized.Value.RelativePath}' during Book.txt write or validation: {remove.Error}");

        var exclude = await _exclusionManifestService.ExcludeAsync(doc.ManuscriptRoot, normalized.Value.RelativePath).ConfigureAwait(false);
        if (!exclude.IsSuccess)
            return Result<Unit>.Fail($"Exclude operation failed for '{normalized.Value.RelativePath}' during manifest save after Book.txt write: {exclude.Error}");

        return Result<Unit>.Ok(Unit.Default);
    }

    public async Task<Result<Unit>> DeleteChapterFileAsync(string bookTxtPath, string chapterPath)
    {
        var document = await LoadDocumentAsync(bookTxtPath).ConfigureAwait(false);
        if (!document.IsSuccess)
            return Result<Unit>.Fail(document.Error!);

        var doc = document.Value!;
        var normalized = NormalizeStructurePath(doc.ManuscriptRoot, chapterPath, nameof(chapterPath));
        if (!normalized.IsSuccess)
            return Result<Unit>.Fail(normalized.Error!);

        var entryIndex = FindEntryIndex(doc.Entries, normalized.Value!.RelativePath);
        if (entryIndex < 0)
            return Result<Unit>.Fail($"Entry '{normalized.Value.RelativePath}' was not found in '{doc.BookTxtPath}'.");

        if (!File.Exists(normalized.Value.AbsolutePath))
            return Result<Unit>.Fail($"Chapter file '{normalized.Value.AbsolutePath}' does not exist.");

        var originalLines = doc.RawLines.ToList();
        var updatedLines = doc.RawLines.ToList();
        updatedLines.RemoveAt(doc.EntryLineIndexes[entryIndex]);

        var write = await WriteBookTxtAsync(doc.BookTxtPath, updatedLines).ConfigureAwait(false);
        if (!write.IsSuccess)
            return Result<Unit>.Fail(write.Error!);

        try
        {
            File.Delete(normalized.Value.AbsolutePath);
            return Result<Unit>.Ok(Unit.Default);
        }
        catch (Exception ex)
        {
            var restore = await WriteBookTxtAsync(doc.BookTxtPath, originalLines).ConfigureAwait(false);
            if (!restore.IsSuccess)
                return Result<Unit>.Fail($"Failed to delete chapter file '{normalized.Value.AbsolutePath}' and failed to restore Book.txt at '{doc.BookTxtPath}': {ex.Message}; restore error: {restore.Error}");

            return Result<Unit>.Fail($"Failed to delete chapter file '{normalized.Value.AbsolutePath}': {ex.Message}");
        }
    }

    private async Task<Result<Dictionary<string, ChapterRegistryEntry>>> BuildRegistryMoveAsync(
        string manuscriptRoot,
        string sourceRelativePath,
        string replacementRelativePath)
    {
        Dictionary<string, ChapterRegistryEntry> registry;
        try
        {
            registry = await _chapterRegistryService.LoadAsync(manuscriptRoot).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return Result<Dictionary<string, ChapterRegistryEntry>>.Fail($"failed to load chapter registry: {ex.Message}");
        }

        var sourceMatches = registry
            .Where(pair => string.Equals(pair.Value.CurrentPath, sourceRelativePath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (sourceMatches.Count == 0)
            return Result<Dictionary<string, ChapterRegistryEntry>>.Fail($"no registry entry exists for source path '{sourceRelativePath}'; refusing to move because UUID continuity cannot be proven.");

        if (sourceMatches.Count > 1)
            return Result<Dictionary<string, ChapterRegistryEntry>>.Fail($"multiple registry entries exist for source path '{sourceRelativePath}'; refusing to move because UUID continuity is ambiguous.");

        var sourceUuid = sourceMatches[0].Key;
        var targetConflict = registry.Any(pair =>
            !string.Equals(pair.Key, sourceUuid, StringComparison.Ordinal) &&
            string.Equals(pair.Value.CurrentPath, replacementRelativePath, StringComparison.OrdinalIgnoreCase));
        if (targetConflict)
            return Result<Dictionary<string, ChapterRegistryEntry>>.Fail($"another registry entry already targets replacement path '{replacementRelativePath}'.");

        var updated = _chapterRegistryService.ReconcileRename(registry, sourceRelativePath, replacementRelativePath);
        return Result<Dictionary<string, ChapterRegistryEntry>>.Ok(updated);
    }

    private static List<string> BuildMoveLines(BookDocument doc, int sourceIndex, string replacementRelativePath, int newIndex)
    {
        var lines = doc.RawLines.ToList();
        var sourceRawIndex = doc.EntryLineIndexes[sourceIndex];
        lines[sourceRawIndex] = replacementRelativePath;

        if (sourceIndex == newIndex)
            return lines;

        var movingLine = lines[sourceRawIndex];
        lines.RemoveAt(sourceRawIndex);
        var insertionIndex = FindRawInsertionIndex(lines, newIndex);
        lines.Insert(insertionIndex, movingLine);
        return lines;
    }

    private async Task<Result<Unit>> RollBackMoveAsync(
        string bookTxtPath,
        (string RelativePath, string AbsolutePath) sourcePath,
        (string RelativePath, string AbsolutePath) replacementPath,
        IReadOnlyList<string>? restoreBookTxtLines,
        string failurePhase,
        string failureDetail)
    {
        var rollbackFailures = new List<string>();

        try
        {
            if (File.Exists(replacementPath.AbsolutePath))
                File.Move(replacementPath.AbsolutePath, sourcePath.AbsolutePath);
            else
                rollbackFailures.Add($"target file '{replacementPath.AbsolutePath}' was missing during rollback");
        }
        catch (Exception ex)
        {
            rollbackFailures.Add($"file rollback from '{replacementPath.AbsolutePath}' to '{sourcePath.AbsolutePath}' failed: {ex.Message}");
        }

        if (restoreBookTxtLines is not null)
        {
            var restoreBook = await WriteBookTxtAsync(bookTxtPath, restoreBookTxtLines).ConfigureAwait(false);
            if (!restoreBook.IsSuccess)
                rollbackFailures.Add($"Book.txt rollback failed: {restoreBook.Error}");
        }

        if (rollbackFailures.Count > 0)
        {
            return Result<Unit>.Fail(
                $"Move operation failed for '{sourcePath.RelativePath}' to '{replacementPath.RelativePath}' during {failurePhase}: {failureDetail}; rollback attempted but rollback failed: {string.Join("; ", rollbackFailures)}.");
        }

        return Result<Unit>.Fail(
            $"Move operation failed for '{sourcePath.RelativePath}' to '{replacementPath.RelativePath}' during {failurePhase}: {failureDetail}; rollback attempted and manuscript state was restored.");
    }

    private async Task<Result<BookDocument>> LoadDocumentAsync(string bookTxtPath)
    {
        var fullBookTxtPathResult = NormalizeAbsolutePath(bookTxtPath, nameof(bookTxtPath));
        if (!fullBookTxtPathResult.IsSuccess)
            return Result<BookDocument>.Fail(fullBookTxtPathResult.Error!);

        var fullBookTxtPath = fullBookTxtPathResult.Value!;
        if (!File.Exists(fullBookTxtPath))
            return Result<BookDocument>.Fail($"Book.txt not found: '{fullBookTxtPath}'.");

        var manuscriptRoot = Path.GetDirectoryName(fullBookTxtPath);
        if (string.IsNullOrWhiteSpace(manuscriptRoot))
            return Result<BookDocument>.Fail($"Could not determine manuscript root for '{fullBookTxtPath}'.");

        try
        {
            var rawLines = (await File.ReadAllLinesAsync(fullBookTxtPath).ConfigureAwait(false)).ToList();
            var entries = new List<string>();
            var entryLineIndexes = new List<int>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < rawLines.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(rawLines[i]))
                    continue;

                var normalizedEntry = NormalizeStructurePath(manuscriptRoot, rawLines[i], $"Book.txt entry line {i + 1}");
                if (!normalizedEntry.IsSuccess)
                    return Result<BookDocument>.Fail(normalizedEntry.Error!);

                if (!seen.Add(normalizedEntry.Value!.RelativePath))
                    return Result<BookDocument>.Fail($"Duplicate Book.txt entry '{normalizedEntry.Value.RelativePath}' found in '{fullBookTxtPath}'.");

                entries.Add(normalizedEntry.Value.RelativePath);
                entryLineIndexes.Add(i);
            }

            return Result<BookDocument>.Ok(new BookDocument(fullBookTxtPath, manuscriptRoot, rawLines, entryLineIndexes, entries));
        }
        catch (Exception ex)
        {
            return Result<BookDocument>.Fail($"Failed to read Book.txt at '{fullBookTxtPath}': {ex.Message}");
        }
    }

    private static Result<string> NormalizeAbsolutePath(string path, string label)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Result<string>.Fail($"{label} is required.");

        try
        {
            return Result<string>.Ok(Path.GetFullPath(path.Trim().Replace('\\', Path.DirectorySeparatorChar)));
        }
        catch (Exception ex)
        {
            return Result<string>.Fail($"Invalid {label} '{path}': {ex.Message}");
        }
    }

    private Result<(string RelativePath, string AbsolutePath)> NormalizeStructurePath(string manuscriptRoot, string path, string label)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Result<(string RelativePath, string AbsolutePath)>.Fail($"{label} is required.");

        var cleaned = path.Trim().Replace('\\', Path.DirectorySeparatorChar);
        string absolutePath;

        try
        {
            absolutePath = Path.IsPathRooted(cleaned)
                ? Path.GetFullPath(cleaned)
                : Path.GetFullPath(Path.Combine(manuscriptRoot, cleaned));
        }
        catch (Exception ex)
        {
            return Result<(string RelativePath, string AbsolutePath)>.Fail($"Invalid {label} '{path}': {ex.Message}");
        }

        var manuscriptRootFull = EnsureTrailingSeparator(Path.GetFullPath(manuscriptRoot));
        var absolutePathFull = Path.GetFullPath(absolutePath);
        if (!absolutePathFull.StartsWith(manuscriptRootFull, StringComparison.OrdinalIgnoreCase))
            return Result<(string RelativePath, string AbsolutePath)>.Fail($"{label} '{path}' is outside the manuscript root '{manuscriptRoot}'.");

        var relativePath = Path.GetRelativePath(manuscriptRoot, absolutePathFull).Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(relativePath) || relativePath == "." || relativePath.StartsWith("..", StringComparison.Ordinal))
            return Result<(string RelativePath, string AbsolutePath)>.Fail($"{label} '{path}' is outside the manuscript root '{manuscriptRoot}'.");

        if (string.Equals(Path.GetFileName(relativePath), "Book.txt", StringComparison.OrdinalIgnoreCase))
            return Result<(string RelativePath, string AbsolutePath)>.Fail($"{label} '{path}' cannot target Book.txt.");

        return Result<(string RelativePath, string AbsolutePath)>.Ok((relativePath, absolutePathFull));
    }

    private static int FindEntryIndex(IReadOnlyList<string> entries, string path)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            if (string.Equals(entries[i], path.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private static int FindRawInsertionIndex(IReadOnlyList<string> lines, int entryIndex)
    {
        var currentEntryIndex = 0;
        for (var rawIndex = 0; rawIndex < lines.Count; rawIndex++)
        {
            if (string.IsNullOrWhiteSpace(lines[rawIndex]))
                continue;

            if (currentEntryIndex == entryIndex)
                return rawIndex;

            currentEntryIndex++;
        }

        return lines.Count;
    }

    private static Result<Unit> ValidatePartDivider(string manuscriptRoot, string partRelativePath)
    {
        var partAbsolutePath = Path.Combine(manuscriptRoot, partRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(partAbsolutePath))
            return Result<Unit>.Fail($"Part file '{partAbsolutePath}' does not exist.");

        try
        {
            foreach (var line in File.ReadLines(partAbsolutePath))
            {
                if (!string.IsNullOrWhiteSpace(line) && line.Trim() == "{class: part}")
                    return Result<Unit>.Ok(Unit.Default);
            }
        }
        catch (Exception ex)
        {
            return Result<Unit>.Fail($"Failed to inspect part file '{partAbsolutePath}': {ex.Message}");
        }

        return Result<Unit>.Fail($"Entry '{partRelativePath}' is not a Part divider.");
    }

    private async Task<Result<Unit>> WriteBookTxtAsync(string bookTxtPath, IReadOnlyList<string> lines)
    {
        try
        {
            var content = string.Join(Environment.NewLine, lines);
            await _metadataStore.WriteTextAtomicAsync(bookTxtPath, content).ConfigureAwait(false);
            return Result<Unit>.Ok(Unit.Default);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Fail($"Failed to write Book.txt at '{bookTxtPath}': {ex.Message}");
        }
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
    }

    private static void TryDeleteFile(string absolutePath)
    {
        try
        {
            if (File.Exists(absolutePath))
                File.Delete(absolutePath);
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }

    private sealed record BookDocument(
        string BookTxtPath,
        string ManuscriptRoot,
        List<string> RawLines,
        List<int> EntryLineIndexes,
        List<string> Entries);
}

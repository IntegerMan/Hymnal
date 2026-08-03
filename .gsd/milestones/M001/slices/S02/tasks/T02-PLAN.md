---
estimated_steps: 13
estimated_files: 2
skills_used: []
---

# T02: ManuscriptService with FileSystemWatcher debounce implemented; 3 unit tests pass

Why: ManuscriptService composes BookTxtParser + ManuscriptModel + FileSystemWatcher to give the rest of the app a reactive view of the workspace. It is the only place that owns file I/O for the manuscript tree.

Do:
1. Create `src/Hymnal.Core/Services/ManuscriptService.cs`.
   - Constructor takes `INotificationService`.
   - `Task<Result<ManuscriptModel>> LoadWorkspaceAsync(string folderPath)`: reads `Book.txt` from `folderPath`; calls `BookTxtParser.Parse`; calls `model.Load(nodes)`; starts `FileSystemWatcher` on `folderPath` watching `Book.txt` with `NotifyFilters.FileName | NotifyFilters.LastWrite`; debounce 300ms (use a private `Timer` reset on each event); on debounce fire calls `notificationService.ShowInfo("Book.txt changed — reload?")` on the captured sync context. Returns `Result<ManuscriptModel>.Success(model)`. If `Book.txt` does not exist, returns `Result<ManuscriptModel>.Failure("Book.txt not found in folder")`.
   - `void Dispose()` — stops and disposes watcher and timer.
   - Register as `IDisposable` if using dispose pattern.
2. Create `tests/Hymnal.Core.Tests/Services/ManuscriptServiceTests.cs` with xUnit tests:
   - `LoadWorkspaceAsync_SimpleBook_ReturnsSuccessWithOneChapter`: uses simple-book fixture path → Result.IsSuccess, model.Nodes has 1 entry, IsMissing=false
   - `LoadWorkspaceAsync_MissingBookTxt_ReturnsFailure`: folder without Book.txt → Result.IsSuccess=false
   - `LoadWorkspaceAsync_MissingEntryFile_NodeIsMarkedMissing`: multi-part-book fixture (use real fixture path) → chapter-one node IsMissing=false (file exists in fixture)

Note: FileSystemWatcher tests are excluded to avoid flaky timing tests. The watcher code path is manually verified.

Done when: `dotnet test tests/Hymnal.Core.Tests/ --filter ManuscriptServiceTests` exits 0.

## Inputs

- `src/Hymnal.Core/Services/BookTxtParser.cs`
- `src/Hymnal.Core/Models/ManuscriptModel.cs`
- `src/Hymnal.Core/Models/ChapterNode.cs`
- `src/Hymnal.Core/Common/Result.cs`
- `src/Hymnal.Core/Interfaces/INotificationService.cs`
- `tests/Hymnal.Core.Tests/Fixtures/SampleManuscripts/simple-book/Book.txt`
- `tests/Hymnal.Core.Tests/Fixtures/SampleManuscripts/multi-part-book/Book.txt`

## Expected Output

- `src/Hymnal.Core/Services/ManuscriptService.cs`
- `tests/Hymnal.Core.Tests/Services/ManuscriptServiceTests.cs`

## Verification

dotnet test tests/Hymnal.Core.Tests/ --filter ManuscriptServiceTests

---
id: T02
parent: S02
milestone: M001
key_files:
  - src/Hymnal.Core/Services/ManuscriptService.cs
  - tests/Hymnal.Core.Tests/Services/ManuscriptServiceTests.cs
key_decisions:
  - FileSystemWatcher tests excluded to avoid flaky timing tests per plan spec
  - SynchronizationContext captured at LoadWorkspaceAsync call time so debounce fires on UI thread when available
duration: 
verification_result: passed
completed_at: 2026-05-29T03:30:30.967Z
blocker_discovered: false
---

# T02: ManuscriptService with FileSystemWatcher debounce implemented; 3 unit tests pass

**ManuscriptService with FileSystemWatcher debounce implemented; 3 unit tests pass**

## What Happened

Created `src/Hymnal.Core/Services/ManuscriptService.cs` — a sealed IDisposable class that takes INotificationService in its constructor. `LoadWorkspaceAsync` reads Book.txt from the folder, delegates to BookTxtParser.Parse, loads the ManuscriptModel, then starts a FileSystemWatcher watching Book.txt with NotifyFilters.FileName | NotifyFilters.LastWrite. Debounce is implemented via a private Timer reset on each event (300ms delay); on fire it calls notificationService.ShowInfo on the captured SynchronizationContext. Returns Result<ManuscriptModel>.Ok(model) on success or Result<ManuscriptModel>.Fail("Book.txt not found in folder") if Book.txt is absent. Dispose stops and disposes both the watcher and the timer. Created `tests/Hymnal.Core.Tests/Services/ManuscriptServiceTests.cs` with 3 xUnit tests: simple-book fixture returns success with 1 node, nonexistent folder returns failure, multi-part-book fixture returns success with chapter-one node not missing.

## Verification

Ran `dotnet test tests/Hymnal.Core.Tests/ --filter ManuscriptServiceTests` — 3 passed, 0 failed, exit code 0.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test tests/Hymnal.Core.Tests/ --filter ManuscriptServiceTests` | 0 | pass | 8000ms |

## Deviations

none

## Known Issues

none

## Files Created/Modified

- `src/Hymnal.Core/Services/ManuscriptService.cs`
- `tests/Hymnal.Core.Tests/Services/ManuscriptServiceTests.cs`

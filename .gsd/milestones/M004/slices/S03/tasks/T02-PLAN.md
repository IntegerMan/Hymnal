---
estimated_steps: 1
estimated_files: 2
skills_used: []
---

# T02: Add GitPanelViewModel refresh, watcher, and commit commands

Executor skills_used frontmatter: tdd, verify-before-complete. Why: the toolbar needs a single reactive state owner that can hide itself when Git/repo prerequisites fail, stay fresh after file saves, and avoid leaking FileSystemWatcher handles across workspace switches. Do: add `GitPanelViewModel` under `src/Hymnal/ViewModels/` with injected `WorkspaceViewModel`, `EditorViewModel`, `IGitService`, and `INotificationService`; expose `IsVisible`, `BranchName`, `UncommittedChangeCount`, `StatusText`, `IsBusy`, `LastError`, `RefreshCommand`, and public methods/commands for commit-only and commit-and-push with an explicit message string. Subscribe to `WorkspaceViewModel.WorkspaceChanged`, `EditorViewModel.Saved`, and a workspace-root `FileSystemWatcher` that watches file name, directory name, last write, and size changes; debounce all refresh triggers so Git status is not spammed during atomic saves or Git metadata writes. Hide/reset state when there is no workspace, system Git is unavailable, or `rev-parse --is-inside-work-tree` fails. Commit methods must use the default message from `CreateDefaultCommitMessage()` when the dialog supplies null/empty/whitespace; the format must start with `Hymnal: save progress ` followed by an ISO-8601 UTC timestamp. On Git failure, call `ShowError` with raw stderr when present, otherwise a useful fallback, and refresh afterward. Done when ViewModel tests prove visibility gates, debounce, save-trigger refresh, watcher-trigger refresh, default message formatting, successful commit refresh, failure notification stderr, and disposal of old watchers on workspace change.

## Inputs

- `src/Hymnal/ViewModels/ViewModelBase.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/EditorViewModel.cs`
- `src/Hymnal.Core/Interfaces/IGitService.cs`
- `src/Hymnal.Core/Interfaces/INotificationService.cs`
- `tests/Hymnal.Core.Tests/ViewModels/MainWindowSupplementalDocsTests.cs`

## Expected Output

- `src/Hymnal/ViewModels/GitPanelViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/GitPanelViewModelTests.cs`

## Verification

dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter FullyQualifiedName~GitPanelViewModelTests

## Observability Impact

Adds inspectable UI state for current Git availability, branch, change count, busy phase, and last failure, plus explicit notifications carrying raw stderr for failed commit/push operations.

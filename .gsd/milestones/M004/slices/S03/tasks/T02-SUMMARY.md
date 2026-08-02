---
id: T02
parent: S03
milestone: M004
key_files:
  - src/Hymnal/ViewModels/GitPanelViewModel.cs
  - tests/Hymnal.Core.Tests/ViewModels/GitPanelViewModelTests.cs
key_decisions:
  - Git status refreshes are queued through a throttled subject so workspace, save, and watcher bursts collapse into one status fetch.
  - Blank commit messages fall back to a timestamped `Hymnal: save progress <UTC ISO-8601>` message.
  - Git commit/push failures surface raw stderr through notifications, with fallback text only when stderr is empty.
duration: 
verification_result: passed
completed_at: 2026-06-04T13:38:59.746Z
blocker_discovered: false
---

# T02: Added a debounced Git toolbar ViewModel with branch/count status, workspace-root watching, and commit/push commands that refresh after saves and Git operations.

**Added a debounced Git toolbar ViewModel with branch/count status, workspace-root watching, and commit/push commands that refresh after saves and Git operations.**

## What Happened

Implemented `GitPanelViewModel` with injected workspace, editor, Git, and notification services. The VM now owns Git visibility and status state (`IsVisible`, `BranchName`, `UncommittedChangeCount`, `StatusText`, `IsBusy`, `LastError`), watches the workspace root with a `FileSystemWatcher`, and debounces refresh triggers from workspace changes, editor saves, and filesystem events so atomic saves and Git metadata writes collapse into one status refresh. Commit-only and commit-and-push commands normalize blank messages to a timestamped `Hymnal: save progress <UTC ISO-8601>` default, surface raw stderr on Git failure, and queue a follow-up refresh after both success and failure. Added targeted tests that prove hidden-state resets, visible status rendering, save-trigger refresh, watcher-trigger refresh, default message formatting, commit refresh behavior, raw-stderr notifications, and disposal of old workspace watchers on root switches.

## Verification

Ran the targeted GitPanel test suite: `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter FullyQualifiedName~GitPanelViewModelTests`. All 10 GitPanelViewModel tests passed, covering visibility gates, debounce behavior, save and watcher refreshes, default commit message formatting, successful commit/push refreshes, failure notifications, and watcher disposal across workspace changes.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter FullyQualifiedName~GitPanelViewModelTests` | 0 | ✅ pass | 8187ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/ViewModels/GitPanelViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/GitPanelViewModelTests.cs`

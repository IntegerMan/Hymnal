---
id: T03
parent: S03
milestone: M004
key_files:
  - src/Hymnal/App.axaml.cs
  - src/Hymnal/ViewModels/MainWindowViewModel.cs
  - src/Hymnal/Views/MainWindow.axaml
  - src/Hymnal/Views/MainWindow.axaml.cs
  - src/Hymnal/Views/GitCommitDialog.axaml
  - src/Hymnal/Views/GitCommitDialog.axaml.cs
  - tests/Hymnal.Core.Tests/ViewModels/MainWindowGitPanelTests.cs
  - tests/Hymnal.Core.Tests/Views/MainWindowGitPanelSmokeTests.cs
  - tests/Hymnal.Core.Tests/ViewModels/MainWindowSupplementalDocsTests.cs
  - tests/Hymnal.Core.Tests/Views/SupplementalDocsViewSmokeTests.cs
  - tests/Hymnal.Core.Tests/ViewModels/MainWindowPlanModeTests.cs
key_decisions:
  - Registered `IProcessRunner`, `IGitService`, and `GitPanelViewModel` in app startup so Git UI state comes from the existing service/ViewModel pipeline.
  - Kept dialog orchestration in MainWindow code-behind with a static routing helper, preserving ViewModel testability and avoiding a second notification path.
  - Used file-based smoke assertions for the Git dialog markup and code-behind because the test host cannot construct Avalonia windows without a windowing platform.
duration: 
verification_result: passed
completed_at: 2026-06-04T13:54:33.653Z
blocker_discovered: false
---

# T03: Added a Git toolbar with commit dialog wiring, DI registration, and Git status display in the main shell.

**Added a Git toolbar with commit dialog wiring, DI registration, and Git status display in the main shell.**

## What Happened

Registered the Git process runner, Git service, and Git panel ViewModel in App startup, then threaded GitPanelViewModel into MainWindowViewModel so the shell can bind to branch/count state. Added a top-right Git toolbar group in MainWindow.axaml that hides when Git is unavailable, shows branch and uncommitted-change count when visible, and exposes a Commit button. Implemented a small GitCommitDialog window with an initial commit-message value, Commit only / Commit & Push / Cancel actions, and code-behind routing from MainWindow to the GitPanelViewModel through a single static helper so the ViewModel remains testable. Updated the existing MainWindow construction tests to pass the new Git panel dependency and added focused VM and smoke tests for the toolbar bindings and dialog wiring.

## Verification

Ran `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter FullyQualifiedName~MainWindowGitPanel`, which built the app/test projects and passed all four targeted tests. The smoke coverage was adapted to file-based assertions against the XAML/code-behind because the test host does not provide an Avalonia windowing platform for constructing live Window instances.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter FullyQualifiedName~MainWindowGitPanel` | 0 | ✅ pass | 988ms |

## Deviations

Smoke coverage uses file-based XAML/code-behind assertions instead of instantiating the Avalonia dialog window in-process, because the test host lacks an `IWindowingPlatform`.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/App.axaml.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/Views/MainWindow.axaml`
- `src/Hymnal/Views/MainWindow.axaml.cs`
- `src/Hymnal/Views/GitCommitDialog.axaml`
- `src/Hymnal/Views/GitCommitDialog.axaml.cs`
- `tests/Hymnal.Core.Tests/ViewModels/MainWindowGitPanelTests.cs`
- `tests/Hymnal.Core.Tests/Views/MainWindowGitPanelSmokeTests.cs`
- `tests/Hymnal.Core.Tests/ViewModels/MainWindowSupplementalDocsTests.cs`
- `tests/Hymnal.Core.Tests/Views/SupplementalDocsViewSmokeTests.cs`
- `tests/Hymnal.Core.Tests/ViewModels/MainWindowPlanModeTests.cs`

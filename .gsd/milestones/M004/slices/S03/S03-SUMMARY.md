---
id: S03
parent: M004
milestone: M004
provides:
  - A reusable Git service contract and process runner seam for future tooling.
  - A debounced toolbar state owner that downstream UI can bind to without direct process calls.
  - A real local-repo Git workflow proof that the shell can commit and push through system Git.
requires:
  - slice: S01
    provides: Shell mode/navigation and corkboard wiring already existed and were reused.
  - slice: S02
    provides: Existing editor save path and workspace routing were reused for Git refresh triggers.
affects:
  []
key_files:
  - src/Hymnal.Core/Interfaces/IGitService.cs
  - src/Hymnal.Core/Interfaces/IProcessRunner.cs
  - src/Hymnal.Core/Models/GitCommandResult.cs
  - src/Hymnal.Core/Models/GitRepositoryStatus.cs
  - src/Hymnal.Core/Infrastructure/GitProcessRunner.cs
  - src/Hymnal.Core/Services/ProcessGitService.cs
  - src/Hymnal/ViewModels/GitPanelViewModel.cs
  - src/Hymnal/ViewModels/MainWindowViewModel.cs
  - src/Hymnal/App.axaml.cs
  - src/Hymnal/Views/MainWindow.axaml
  - src/Hymnal/Views/MainWindow.axaml.cs
  - src/Hymnal/Views/GitCommitDialog.axaml
  - src/Hymnal/Views/GitCommitDialog.axaml.cs
  - tests/Hymnal.Core.Tests/Services/ProcessGitServiceTests.cs
  - tests/Hymnal.Core.Tests/ViewModels/GitPanelViewModelTests.cs
  - tests/Hymnal.Core.Tests/ViewModels/MainWindowGitPanelTests.cs
  - tests/Hymnal.Core.Tests/Views/MainWindowGitPanelSmokeTests.cs
  - tests/Hymnal.Core.Tests/Services/ProcessGitServiceLocalRepoTests.cs
key_decisions:
  - Git commands run through an `IProcessRunner` seam and return `GitCommandResult` with raw stderr/exit-code diagnostics.
  - Repository commands use `git -C <workspaceRoot>` argument lists instead of a shell command string.
  - The commit dialog stays in MainWindow code-behind while the ViewModel owns the actual Git operations and notifications.
patterns_established:
  - Git status refreshes are debounced so save and watcher bursts collapse into one status fetch.
  - Raw Git stderr is intentionally surfaced to the user on commit/push failure.
  - MainWindow Git smoke assertions can be file-based when the test host lacks a live Avalonia windowing platform.
observability_surfaces:
  - GitPanelViewModel.IsVisible
  - GitPanelViewModel.BranchName
  - GitPanelViewModel.UncommittedChangeCount
  - GitPanelViewModel.StatusText
  - GitPanelViewModel.IsBusy
  - GitPanelViewModel.LastError
  - notification banner text
  - raw stderr in GitCommandResult
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-06-04T14:04:33.181Z
blocker_discovered: false
---

# S03: Git Toolbar Commit Workflow

**Added a Git-backed toolbar workflow that detects repositories, shows branch and dirty count, stages all changes, commits or commits-and-pushes, and surfaces raw Git stderr on failure.**

## What Happened

S03 completed the Git toolbar end-to-end on top of the shipped shell/editor surfaces from S01 and S02. T01 established the core system-Git contract: `IGitService`, `IProcessRunner`, `GitProcessRunner`, `GitCommandResult`, `GitRepositoryStatus`, and `ProcessGitService` now execute Git with `git -C <workspaceRoot>`, preserve stdout/stderr/exit code, detect repository visibility, count dirty files from porcelain output, and keep expected Git failures as structured results instead of exceptions. T02 added `GitPanelViewModel`, which owns Git visibility and status state, debounces refreshes from workspace changes/saves/watcher events, watches the workspace root with a disposable `FileSystemWatcher`, formats the default commit message, and refreshes after both successful and failed commit paths while surfacing raw stderr through notifications. T03 wired the Git service and panel into app startup, threaded the panel into `MainWindowViewModel`, rendered the toolbar group and commit button in `MainWindow.axaml`, and added a commit dialog surface whose actions route through the ViewModel rather than a second notification path. T04 closed the loop with a real temp local-repo integration test against the production Git runner plus a bare remote, proving branch detection, dirty-count refresh, commit-only cleanup, commit-and-push propagation, and the push-failure stderr path; it also confirmed the app shell compiles. Current closeout evidence is backed by the task summaries for T01-T04 plus the real local-repo and build verification from T04; the known gsd_exec/WSL dotnet limitation was avoided by using the Windows SDK evidence already captured in the task session.

## Verification

Verification evidence present across all tasks: `ProcessGitServiceTests` passed for the fake-runner/service contract; `GitPanelViewModelTests` passed for debounced refresh, watcher behavior, visibility gating, default message formatting, commit refresh, and stderr notifications; `MainWindowGitPanel` tests passed for DI/toolbar/dialog wiring; `ProcessGitServiceLocalRepoTests` passed against a real temp repo and bare remote; and `dotnet build src/Hymnal/Hymnal.csproj` passed. The gsd_exec verification surface was also used to confirm the four S03 task summary artifacts exist and report T02/T03/T04 as passed, with T01 recorded as mixed only because the required gsd_exec dotnet attempt hit the known environment issue while the task itself later passed through the Windows SDK.

## Requirements Advanced

None.

## Requirements Validated

- R008 — Validated by the T01-T04 task evidence: fake-runner service tests, debounced GitPanelViewModel tests, MainWindow toolbar/dialog wiring tests, real local-repo commit/push integration tests, and a successful app build.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

None.

## Known Limitations

None.

## Follow-ups

None.

## Files Created/Modified

- `src/Hymnal.Core/Interfaces/IGitService.cs` — Added the Git service contract.
- `src/Hymnal.Core/Interfaces/IProcessRunner.cs` — Added the process-runner seam used by Git integration.
- `src/Hymnal.Core/Models/GitCommandResult.cs` — Added structured Git process result data.
- `src/Hymnal.Core/Models/GitRepositoryStatus.cs` — Added toolbar-facing repository status projection.
- `src/Hymnal.Core/Infrastructure/GitProcessRunner.cs` — Added the production System.Diagnostics.Process runner.
- `src/Hymnal.Core/Services/ProcessGitService.cs` — Implemented Git availability, repository detection, status, commit, and push operations.
- `src/Hymnal/ViewModels/GitPanelViewModel.cs` — Implemented debounced toolbar state, watcher refresh, and commit commands.
- `src/Hymnal/ViewModels/MainWindowViewModel.cs` — Exposed the Git panel to the shell.
- `src/Hymnal/App.axaml.cs` — Registered Git services and the Git panel in DI.
- `src/Hymnal/Views/MainWindow.axaml` — Added the Git toolbar group and branch/count binding.
- `src/Hymnal/Views/MainWindow.axaml.cs` — Routed the commit dialog actions into the Git panel.
- `src/Hymnal/Views/GitCommitDialog.axaml` — Added the commit dialog UI.
- `src/Hymnal/Views/GitCommitDialog.axaml.cs` — Added commit dialog code-behind.
- `tests/Hymnal.Core.Tests/Services/ProcessGitServiceTests.cs` — Covered the Git service contract with a fake process runner.
- `tests/Hymnal.Core.Tests/ViewModels/GitPanelViewModelTests.cs` — Covered debounce, visibility, notification, and refresh behavior.
- `tests/Hymnal.Core.Tests/ViewModels/MainWindowGitPanelTests.cs` — Covered shell wiring for the Git panel.
- `tests/Hymnal.Core.Tests/Views/MainWindowGitPanelSmokeTests.cs` — Covered Git toolbar/dialog markup and routing expectations.
- `tests/Hymnal.Core.Tests/Services/ProcessGitServiceLocalRepoTests.cs` — Proved real repo commit-and-push behavior against a temp bare remote.

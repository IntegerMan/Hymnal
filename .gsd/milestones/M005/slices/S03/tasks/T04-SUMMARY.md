---
id: T04
parent: S03
milestone: M005
key_files:
  - src/Hymnal.Core/Services/BookTxtStructureService.cs
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal/Views/SidebarView.axaml
  - src/Hymnal/Views/SidebarView.axaml.cs
  - tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs
  - tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarRenameTests.cs
  - tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs
key_decisions:
  - Kept the rename ownership boundary intact: SidebarView prompts only, WorkspaceViewModel computes replacement paths and delegates, and BookTxtStructureService remains the sole structural mutation and rollback boundary.
duration: 
verification_result: passed
completed_at: 2026-06-18T12:09:25.147Z
blocker_discovered: false
---

# T04: Verified the sidebar rename slice end to end with targeted and full tests, a desktop build, and a boundary audit confirming rename mutations stay inside BookTxtStructureService.

**Verified the sidebar rename slice end to end with targeted and full tests, a desktop build, and a boundary audit confirming rename mutations stay inside BookTxtStructureService.**

## What Happened

I treated T04 as a closure-only verification unit and first inspected the rename call chain across `SidebarView.axaml`, `SidebarView.axaml.cs`, `WorkspaceViewModel.cs`, `BookTxtStructureService.cs`, and the rename-focused test files. The audit confirmed the intended ownership boundary: `SidebarView` only exposes the context-menu affordance, prompts for a new title, and executes commands; `WorkspaceViewModel` validates rename eligibility, derives safe replacement paths, suppresses the watcher, delegates to `IBookTxtStructureService.RenameEntryAsync`, reloads the workspace, and reselects the replacement node; `BookTxtStructureService` owns all structural mutation including file/folder moves, Book.txt rewrites, heading rewrites, registry reconciliation, exclusions manifest cleanup, and rollback/error reporting. I also confirmed `WorkspaceSidebarRenameTests.cs` contains the required chapter rename, Part folder rename, conflict handling, and UUID-backed metadata continuity-through-reload assertions. During verification I found the repo does not contain `Hymnal.sln`; it contains `Hymnal.slnx`, so I used the actual solution file. I also hit transient MSB3883/CS2012 file-lock failures when multiple native `dotnet test` commands were launched in parallel against shared obj/ref outputs; rerunning the suites serially with `--no-build --no-restore` after a successful build resolved the issue without any code change. Requirement note: this slice advances R013 as planned; the preloaded R010 ownership mapping was not advanced because AI editorial ownership is outside the approved S03 rename sketch.

## Verification

Verified the rename closure with native `dotnet` commands in the repo root using the real solution and desktop project. Targeted `BookTxtStructureServiceTests` passed, proving chapter rename, Part folder rename, conflict, rollback, and UUID continuity coverage at the Core boundary. Targeted `WorkspaceSidebarRenameTests` passed, proving ViewModel command integration, notification failures, workspace reload, selection continuity, and metadata continuity across reload. Targeted `SidebarViewSmokeTests` passed, proving the rename affordance and sidebar visibility rules remain wired in XAML and code-behind. `dotnet build src/Hymnal/Hymnal.csproj --nologo --verbosity minimal` succeeded, catching no XAML or DI compile failures; it emitted one pre-existing CS4014 warning at `src/Hymnal/ViewModels/WorkspaceViewModel.cs:1621` for an unawaited continuation. Finally, the full `dotnet test Hymnal.slnx --nologo --verbosity minimal --no-build --no-restore` suite passed with 324/324 tests green.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test Hymnal.slnx --nologo --verbosity minimal --filter "FullyQualifiedName~BookTxtStructureServiceTests" --no-restore` | 0 | ✅ pass | 1000ms |
| 2 | `dotnet test Hymnal.slnx --nologo --verbosity minimal --filter "FullyQualifiedName~WorkspaceSidebarRenameTests" --no-build --no-restore` | 0 | ✅ pass | 1000ms |
| 3 | `dotnet test Hymnal.slnx --nologo --verbosity minimal --filter "FullyQualifiedName~SidebarViewSmokeTests" --no-build --no-restore` | 0 | ✅ pass | 92ms |
| 4 | `dotnet build src/Hymnal/Hymnal.csproj --nologo --verbosity minimal` | 0 | ✅ pass (build succeeded; 1 existing warning) | 12740ms |
| 5 | `dotnet test Hymnal.slnx --nologo --verbosity minimal --no-build --no-restore` | 0 | ✅ pass | 5000ms |

## Deviations

Used `Hymnal.slnx` instead of the task-plan text’s `Hymnal.sln` because the repository only contains `Hymnal.slnx`. Initial parallel verification attempts produced Windows file-lock errors on shared `obj/ref` outputs, so the final verification was rerun serially with `--no-build --no-restore` after a successful build.

## Known Issues

`dotnet build` reports an existing CS4014 warning at `src/Hymnal/ViewModels/WorkspaceViewModel.cs:1621` because a continuation result is intentionally not awaited. This warning did not block the rename slice verification and no rename-specific failures remain.

## Files Created/Modified

- `src/Hymnal.Core/Services/BookTxtStructureService.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/Views/SidebarView.axaml.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`
- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarRenameTests.cs`
- `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs`

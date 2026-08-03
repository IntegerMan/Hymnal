---
id: T02
parent: S03
milestone: M005
key_files:
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarRenameTests.cs
  - src/Hymnal/ViewModels/CorkboardViewModel.cs
key_decisions:
  - Accepted the already-landed WorkspaceViewModel rename command surface as the canonical sidebar rename path because it already delegates only to `IBookTxtStructureService.RenameEntryAsync`, suppresses watchers during structural edits, reloads the workspace, and preserves UUID-backed metadata continuity under dedicated tests.
duration: 
verification_result: mixed
completed_at: 2026-06-18T12:17:03.480Z
blocker_discovered: false
---

# T02: Verified the existing Workspace sidebar rename command surface routes chapter and Part renames through BookTxtStructureService and preserves UUID-backed continuity on reload.

**Verified the existing Workspace sidebar rename command surface routes chapter and Part renames through BookTxtStructureService and preserves UUID-backed continuity on reload.**

## What Happened

Inspected `src/Hymnal/ViewModels/WorkspaceViewModel.cs` and confirmed the sidebar rename flow is already wired through `RenameChapterCommand`/`RenamePartCommand`, deterministic path builders, watcher-suppressed structural execution, workspace reload, and replacement-node reselection. Reviewed the dedicated integration coverage in `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarRenameTests.cs`, which already exercises chapter rename continuity, Part folder rename continuity, conflict notifications, invalid blank-title rejection, and duplicate-node avoidance. Because the code and tests already matched the task contract, no source edits were required; the task was closed by verification of the landed implementation.

## Verification

Ran the dedicated Workspace sidebar rename test suite with Windows `dotnet` via PowerShell. The first `gsd_exec` attempt failed due to the known WSL/NuGet `dotnet` path issue captured in project memory, so verification was repeated with a direct shell invocation that restored/build the projects and passed all four `WorkspaceSidebarRenameTests` cases.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `cd /mnt/c/Dev/Hymnal && dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarRenameTests --verbosity minimal` | 127 | ⚠️ environment limitation (WSL gsd_exec lacked dotnet) | 3350ms |
| 2 | `cd /mnt/c/Dev/Hymnal && cmd.exe /c "dotnet test tests\\Hymnal.Core.Tests\\Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarRenameTests --verbosity minimal"` | 1 | ⚠️ known WSL/NuGet path bug under gsd_exec | 2989ms |
| 3 | `powershell.exe -NoProfile -Command "dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarRenameTests --verbosity minimal"` | 0 | ✅ pass | 1000ms |

## Deviations

None. The implementation and tests were already present and satisfied the task contract, so the task completed via verification rather than new edits.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarRenameTests.cs`
- `src/Hymnal/ViewModels/CorkboardViewModel.cs`

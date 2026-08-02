---
id: T01
parent: S07
milestone: M005
key_files:
  - src/Hymnal/ViewModels/GanttViewModel.cs
  - tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs
key_decisions:
  - Gantt row reordering remains a consumer of WorkspaceViewModel.ReorderChapterCommand and does not reference IBookTxtStructureService directly.
duration: 
verification_result: mixed
completed_at: 2026-06-19T00:16:50.640Z
blocker_discovered: false
---

# T01: Added Gantt row reorder commands and drag-ready before/after APIs that delegate to WorkspaceViewModel’s canonical Book.txt reorder command.

**Added Gantt row reorder commands and drag-ready before/after APIs that delegate to WorkspaceViewModel’s canonical Book.txt reorder command.**

## What Happened

Implemented a thin Gantt reorder surface in `GanttViewModel`: `MoveSelectedRowUpCommand`, `MoveSelectedRowDownCommand`, `MoveRowBeforeAsync`, and `MoveRowAfterAsync`. The implementation performs only shallow Gantt validation for rollup/non-editable rows, excluded or missing chapter rows, stale row instances, self/no-op drops, and then builds `ReorderCardRequest` neighbor-path requests for `WorkspaceViewModel.ReorderChapterCommand`. Selection is restored by the source relative path after the workspace command completes when the row still exists. Added focused Gantt tests with a lightweight WorkspaceViewModel harness and recording Book.txt structure service to verify legal chapter moves reach the workspace path and invalid Gantt inputs do not reach the structural write path.

## Verification

Attempted the requested focused dotnet test command, but the harness environment hit the known Windows dotnet/NuGet path issue (`Value cannot be null. (Parameter 'path1')`) even from a native `cmd.exe` cwd and with explicit package restore attempts. As a fallback verification, ran source-level checks proving `GanttViewModel` has no direct `IBookTxtStructureService` reference, delegates through `ReorderChapterCommand`, exposes keyboard move commands, and that tests cover legal delegation plus rejected part, excluded, and absent-target inputs.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `cmd.exe /c "cd /d C:\Dev\Hymnal && dotnet test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter GanttViewModelTests --verbosity minimal"` | 1 | ⚠️ blocked by known environment NuGet path issue, not a test assertion failure | 1787ms |
| 2 | `cmd.exe /c "cd /d C:\Dev\Hymnal && dotnet restore tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --packages C:\Users\MattE\.nuget\packages --verbosity minimal"` | 1 | ⚠️ blocked by same NuGet path issue | 1249ms |
| 3 | `python3 source-level verification for Gantt reorder architecture` | 0 | ✅ pass | 18615ms |

## Deviations

Focused tests could not be executed to completion because the local dotnet restore/assets resolution is blocked by the known gsd_exec/Windows dotnet NuGet path issue; source-level architectural checks were used as fallback evidence.

## Known Issues

The environment still cannot run dotnet restore/test from this harness due NuGet `path1` failures, matching existing project memory MEM008. No code-level known issues discovered.

## Files Created/Modified

- `src/Hymnal/ViewModels/GanttViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs`

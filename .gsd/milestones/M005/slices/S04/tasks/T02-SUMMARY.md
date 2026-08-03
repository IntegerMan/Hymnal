---
id: T02
parent: S04
milestone: M005
key_files:
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal/Views/SidebarView.axaml.cs
  - tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarReorderTests.cs
key_decisions:
  - Kept the existing ReorderCardRequest command surface for sidebar reorder, but resolved it through sidebar-specific active Book.txt validation before Core mutation.
  - Preferred full workspace reload after successful sidebar reorder over the prior lightweight ReorderNodesAsync path so Part-block moves and excluded projection merging are synchronized from canonical Book.txt state.
duration: 
verification_result: passed
completed_at: 2026-06-18T12:49:16.953Z
blocker_discovered: false
---

# T02: Routed sidebar reorder through active Book.txt validation for included chapters and Part blocks, with illegal drops rejected before Core mutation.

**Routed sidebar reorder through active Book.txt validation for included chapters and Part blocks, with illegal drops rejected before Core mutation.**

## What Happened

Refactored WorkspaceViewModel’s sidebar reorder command from visible-node chapter-only index resolution into an active Book.txt resolver. The resolver now ignores projected excluded and missing nodes for index calculation, rejects excluded/missing sources, missing targets, Part-to-chapter drops, chapter-to-Part drops, and cross-Part chapter movement with user-visible notification messages. Legal chapter reorders are constrained to the same containing Part section; legal Part reorders require Part targets and delegate Part-block movement to BookTxtStructureService. Successful mutations remain wrapped in ManuscriptService.SuppressFileWatcher and now reload the workspace from disk after Core mutation instead of relying on the prior lightweight reorder merge. I also updated SidebarView drag predicates so included Part dividers can initiate drag and excluded/missing or mismatched-kind endpoints are filtered before drop. Added WorkspaceSidebarReorderTests covering legal chapter reorder with excluded projection ignored, legal Part-block reorder, illegal excluded and missing sources, illegal missing target, Part-to-chapter rejection, cross-Part chapter rejection, watcher suppression evidence, expected Core call path/index, and reload continuity without duplicate visible nodes.

## Verification

Ran the task-specified targeted test command against tests/Hymnal.Core.Tests.csproj. The suite compiled Hymnal.Core, Hymnal, and Hymnal.Core.Tests, then passed all 7 WorkspaceSidebarReorderTests. A broader documented solution command could not be used because Hymnal.sln is absent from this checkout; the test project command is the available verification surface.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarReorderTests --verbosity minimal"` | 0 | ✅ pass — 7 WorkspaceSidebarReorderTests passed | 3677ms |

## Deviations

Updated src/Hymnal/Views/SidebarView.axaml.cs in addition to the expected WorkspaceViewModel/test files so the sidebar view can initiate included Part drags and block obviously illegal endpoints before command execution. The repository does not contain the documented Hymnal.sln, so verification used the task-specified test project command rather than solution-level testing.

## Known Issues

The targeted build reports pre-existing warnings in unrelated files/tests, including CS4014 in WorkspaceViewModel rename hydration continuation and xUnit analyzer warnings in existing Corkboard/Exclusion tests. Full test-project execution without a filter did not complete within 240 seconds in this harness; the task-specific reorder suite passed.

## Files Created/Modified

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/Views/SidebarView.axaml.cs`
- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarReorderTests.cs`

---
id: T03
parent: S04
milestone: M005
key_files:
  - src/Hymnal/Views/SidebarView.axaml.cs
  - tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs
key_decisions:
  - Mirrored WorkspaceViewModel sidebar reorder legality in view-level predicates so illegal gestures do not show move affordances or invoke the reorder command.
duration: 
verification_result: passed
completed_at: 2026-06-18T12:52:20.259Z
blocker_discovered: false
---

# T03: Enabled sidebar drag gestures and legal-drop predicates for included chapters and Parts.

**Enabled sidebar drag gestures and legal-drop predicates for included chapters and Parts.**

## What Happened

Added public, predicate-level sidebar helpers for drag eligibility and drop legality, then routed pointer-start, DragOver, and Drop through those predicates. The view now starts drags only for included, present chapter and Part nodes. DragOver/Drop reject self-drops, excluded or missing rows, Part-to-chapter drops, chapter-to-Part drops, chapter moves across Part sections, and Part drops after the last Part when the WorkspaceViewModel would reject that move. The code-behind remains limited to gesture interpretation and command invocation; file mutation still goes through WorkspaceViewModel.ReorderChapterCommand.

## Verification

Ran the task-specified filtered Core test command for SidebarViewSmokeTests. The command compiled the test project and exited successfully. Smoke tests now cover CanDragFromSidebar and CanDropOnSidebar for eligible chapters/Parts, blank paths, same-Part chapter moves, cross-Part chapter rejection, Part-only drop targets, last-Part drop-after rejection, self-drops, and inactive target rows.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter SidebarViewSmokeTests --verbosity minimal"` | 0 | ✅ pass | 3726ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/Views/SidebarView.axaml.cs`
- `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs`

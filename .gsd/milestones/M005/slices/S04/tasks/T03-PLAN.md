---
estimated_steps: 9
estimated_files: 3
skills_used: []
---

# T03: Enable sidebar drag gestures for eligible chapters and Parts only

Why: the SidebarView currently has drag/drop wiring but only starts drags for chapters and does not expose testable legal-drop predicates. S04 needs the view to allow Part drags, suppress illegal visual drop affordances, and send only valid source/target intent to WorkspaceViewModel so UI gestures cannot generate malformed structure requests.

Expected skills_used frontmatter: tdd, verify-before-complete.

Do:
1. Extend `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs` with predicate-level smoke tests for new static helper methods such as `CanDragFromSidebar(ChapterNode node)` and `CanDropOnSidebar(ChapterNode source, ChapterNode target)` or equivalent names.
2. Update `src/Hymnal/Views/SidebarView.axaml.cs` so pointer drag can start for included present chapters and included present Parts, but never for excluded, missing, or unsupported node kinds.
3. Update DragOver/Drop logic to reject invalid targets before setting `DragEffects.Move` or showing an insertion line: no self-drop, no excluded or missing targets, no Part source onto chapter target, no chapter source across Part boundaries, and no drops that the ViewModel will reject as unsupported.
4. Preserve existing right-click/context-menu behavior and rename/include/exclude actions.
5. Keep code-behind limited to gesture interpretation and command invocation; it must not write files or mutate Book.txt directly.

Done when: smoke tests prove the view exposes the correct legal drag/drop predicates, Part rows can initiate drag requests, illegal rows cannot, and Row_Drop still routes through the WorkspaceViewModel reorder command only.

## Inputs

- `src/Hymnal/Views/SidebarView.axaml.cs`
- `src/Hymnal/Views/SidebarView.axaml`
- `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`

## Expected Output

- `src/Hymnal/Views/SidebarView.axaml.cs`
- `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs`

## Verification

powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter SidebarViewSmokeTests --verbosity minimal"

## Observability Impact

Improves failure visibility at the gesture layer by preventing illegal drops from showing a move cursor or insertion line; invalid structure attempts should be visibly impossible before command execution.

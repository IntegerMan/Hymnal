---
estimated_steps: 9
estimated_files: 8
skills_used: []
---

# T02: Fix integrated reload and watcher inconsistencies

Why: Prior slices repeatedly noted watcher suppression and reload synchronization as cross-surface risk. The S08 UAT replay should drive any necessary product fixes so successful structural edits do not cause duplicate nodes, stale selections, or re-entrant reloads.

Expected executor skills/frontmatter: `tdd`, `debug-like-expert`, `verify-before-complete`.

Do:
1. Run the T01 replay and inspect failures before changing production code.
2. If the replay shows stale state, duplicate projection, accidental watcher reload, lost selection, or metadata drift, repair the smallest owning layer while preserving decisions D023-D025: ViewModels initiate commands only, `BookTxtStructureService` owns structural mutation, and successful writes reload from canonical disk state.
3. Keep watcher suppression around intentional structural writes in `WorkspaceViewModel` and `CorkboardViewModel`; do not suppress unrelated external-change detection after reload.
4. If Gantt replay exposes accidental reorder sensitivity, add the minimum drag-distance threshold or blocked-path guard suggested by S07 only if the UAT failure proves it is needed.
5. Extend the focused UAT assertions or existing surface tests to pin every fix.

Done when: T01's integrated replay passes without weakening assertions, and no production code path outside the canonical structural service writes `Book.txt` or moves chapter files.

## Inputs

- `tests/Hymnal.Core.Tests/ViewModels/StructuralConsistencyUatTests.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `src/Hymnal/ViewModels/GanttViewModel.cs`
- `src/Hymnal/Views/GanttCanvas.cs`

## Expected Output

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `src/Hymnal/ViewModels/GanttViewModel.cs`
- `src/Hymnal/Views/GanttCanvas.cs`
- `tests/Hymnal.Core.Tests/ViewModels/StructuralConsistencyUatTests.cs`

## Verification

powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter StructuralConsistencyUatTests --verbosity minimal"

## Observability Impact

Strengthens failure localization for reload/watcher bugs by ensuring the UAT replay identifies whether state drift came from watcher re-entry, stale projection, duplicate excluded cards, or non-canonical persistence.

---
id: T02
parent: S08
milestone: M005
key_files:
  - tests/Hymnal.Core.Tests/ViewModels/StructuralConsistencyUatTests.cs
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal/ViewModels/CorkboardViewModel.cs
  - src/Hymnal/ViewModels/GanttViewModel.cs
  - src/Hymnal/Views/GanttCanvas.cs
  - src/Hymnal/ViewModels/CorkboardItemViewModel.cs
key_decisions:
  - No additional fix was applied because the focused integrated replay passed before edits and the existing code already satisfied the canonical structural write/reload contract.
duration: 
verification_result: passed
completed_at: 2026-06-19T04:49:38.273Z
blocker_discovered: false
---

# T02: Confirmed the integrated structural replay now passes and that watcher/reload structural paths remain canonical without adding new write paths.

**Confirmed the integrated structural replay now passes and that watcher/reload structural paths remain canonical without adding new write paths.**

## What Happened

Ran the focused `StructuralConsistencyUatTests` replay before changing production code; it passed, indicating the integrated stale-state/duplicate-projection issue found by T01 was already fixed. Inspected the UAT, T01 summary, `WorkspaceViewModel`, `CorkboardViewModel`, `GanttViewModel`, and `GanttCanvas` around structural operations, reload/reorder behavior, watcher suppression, selection restore, and Gantt row-reorder guards. The current implementation already preserves the intended ownership boundaries: ViewModels initiate commands, `IBookTxtStructureService` performs structural mutations, Workspace/Corkboard suppress the file watcher only while intentional structural writes are in flight, and successful writes reload/reorder from canonical disk state. Also audited direct write APIs under `src/` and found no extra production `Book.txt` write path outside the structural service; remaining direct writes are metadata/editor save services, not structural manuscript mutation. Because the replay did not expose stale state, duplicate projection, accidental watcher reload, lost selection, metadata drift, or Gantt reorder sensitivity, no additional production code or test changes were warranted for T02.

## Verification

Verified the integrated UAT replay with the required focused dotnet test command. Also ran a direct-write audit to confirm no new non-canonical production `Book.txt` write path needed repair; the audit only surfaced non-structural metadata/editor writes outside `BookTxtStructureService`.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter StructuralConsistencyUatTests --verbosity minimal"` | 0 | ✅ pass — pre-edit focused StructuralConsistencyUatTests replay passed | 3647ms |
| 2 | `python3 direct-write audit over src/**/*.cs for WriteAllText/WriteAllLines/WriteTextAtomicAsync/File.Write outside BookTxtStructureService` | 0 | ✅ pass — no non-canonical production Book.txt writer found outside the structural service | 3543ms |
| 3 | `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter StructuralConsistencyUatTests --verbosity minimal"` | 0 | ✅ pass — final focused StructuralConsistencyUatTests verification passed | 555ms |

## Deviations

No code edits were made because the authoritative first step passed and inspection showed T01 had already fixed the integrated duplicate-projection inconsistency targeted by this task.

## Known Issues

The gsd_exec PowerShell invocation returned exit code 0 with no captured stdout/stderr in this environment; exit status is still recorded as the verification signal. Existing unrelated analyzer warnings noted by T01 were not revisited.

## Files Created/Modified

- `tests/Hymnal.Core.Tests/ViewModels/StructuralConsistencyUatTests.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `src/Hymnal/ViewModels/GanttViewModel.cs`
- `src/Hymnal/Views/GanttCanvas.cs`
- `src/Hymnal/ViewModels/CorkboardItemViewModel.cs`

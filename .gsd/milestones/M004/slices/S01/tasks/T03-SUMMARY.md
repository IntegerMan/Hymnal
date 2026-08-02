---
id: T03
parent: S01
milestone: M004
key_files:
  - src/Hymnal/ViewModels/CorkboardViewModel.cs
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal/ViewModels/MainWindowViewModel.cs
  - src/Hymnal/App.axaml.cs
  - tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs
key_decisions:
  - Plan-mode structural edits must flow through IBookTxtStructureService, followed by a workspace reload seam rather than direct editor/file manipulation.
  - Corkboard selection is card-local state that must survive board rebuilds when possible and remain untouched on structural failure.
duration: 
verification_result: passed
completed_at: 2026-06-04T02:26:59.683Z
blocker_discovered: false
---

# T03: Implemented CorkboardViewModel selection, open-request, and structural edit workflow backed by Book.txt service calls and a workspace reload seam.

**Implemented CorkboardViewModel selection, open-request, and structural edit workflow backed by Book.txt service calls and a workspace reload seam.**

## What Happened

Added a dedicated CorkboardViewModel that projects the live WorkspaceViewModel node list into corkboard items, preserves selection state, ignores non-selectable Part dividers, and emits chapter-open requests instead of touching editor file APIs directly. Wired all structural actions through IBookTxtStructureService, including reorder, rename, create, include-existing, remove-from-book, and delete-with-confirmation, then refreshed the workspace through a small public reload seam on WorkspaceViewModel so the board stays in sync without bypassing editor save-before-switch behavior. Hooked the corkboard into the main shell container via DI and MainWindowViewModel so selecting a card can switch the shell back to Write mode and open the corresponding chapter. Added focused unit tests that cover mixed-item projection, live rebuilds, selection exclusivity, open emission, reorder calls, empty state, service failure handling, and delete confirmation gating.

## Verification

Verified with the slice’s focused test target: `dotnet test Hymnal.sln --filter FullyQualifiedName~CorkboardViewModelTests --no-restore` passed after implementation changes, including a rerun after the final cleanup tweak. The test suite exercised the new board view-model without the AXAML view and confirmed the structural workflow and failure surfaces behaved as expected.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `powershell.exe -NoProfile -Command "Set-Location 'C:\Dev\Hymnal'; & 'C:\Program Files\dotnet\dotnet.exe' test Hymnal.sln --filter FullyQualifiedName~CorkboardViewModelTests --no-restore"` | 0 | ✅ pass | 6632ms |
| 2 | `powershell.exe -NoProfile -Command "Set-Location 'C:\Dev\Hymnal'; & 'C:\Program Files\dotnet\dotnet.exe' test Hymnal.sln --filter FullyQualifiedName~CorkboardViewModelTests --no-restore"` | 0 | ✅ pass | 1409ms |

## Deviations

Added a public reload seam on WorkspaceViewModel (`ReloadCurrentWorkspaceAsync`) plus a `BookTxtPath` accessor so corkboard structural commands can refresh the workspace cleanly after service calls. This was the smallest safe surface needed to preserve the existing save-before-switch/editor coordination.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/App.axaml.cs`
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`

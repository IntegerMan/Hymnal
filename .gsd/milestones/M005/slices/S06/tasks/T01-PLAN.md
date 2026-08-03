---
estimated_steps: 10
estimated_files: 2
skills_used: []
---

# T01: Harden Corkboard include and exclude command contracts

Skills expected: tdd, verify-before-complete.

Why: S02 proved sidebar include/exclude and S05 proved Corkboard drag/drop, but S06 needs card-level inclusion toggles in Corkboard to advance R013 without adding a second structural write path. The existing CorkboardViewModel command surface already exposes IncludeExistingChapterCommand and RemoveFromBookCommand; this task should pin their semantics with focused tests and fill any gaps.

Do:
1. Add or extend focused CorkboardViewModel tests for an included chapter card invoking RemoveFromBookCommand and verify the fake IBookTxtStructureService receives ExcludeEntryAsync with the workspace Book.txt path and chapter relative path.
2. Add tests for an excluded projected card invoking IncludeExistingChapterCommand with an explicit index and with a PartPath, verifying IncludeExistingEntryAsync or IncludeExistingEntryAfterPartAsync respectively.
3. Assert successful include/exclude clears LastStructuralError, calls ReloadCurrentWorkspaceAsync, and restores or clears selection truthfully when the selected included card becomes excluded.
4. Assert invalid include requests with neither index nor PartPath fail locally, do not call Core, and populate LastStructuralError plus notification text.
5. Assert Core failure results for include/exclude leave the projection intact and surface operation/path/message through LastStructuralError.
6. Keep ViewModel logic as a thin consumer of IBookTxtStructureService; do not write Book.txt, `.hymnal-data/exclusions.json`, or chapter files directly from CorkboardViewModel.

Done when: Focused ViewModel tests prove Corkboard include/exclude operations call only the canonical Core methods, reload after success, and expose failures without silent partial state.

## Inputs

- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`
- `.gsd/milestones/M005/slices/S02/S02-SUMMARY.md`
- `.gsd/milestones/M005/slices/S05/S05-SUMMARY.md`

## Expected Output

- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`

## Verification

dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewModelTests --verbosity minimal

## Observability Impact

Extends failure diagnostics coverage for LastStructuralError and visible notification paths for include/exclude failures.

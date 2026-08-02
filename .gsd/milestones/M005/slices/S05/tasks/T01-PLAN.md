---
estimated_steps: 8
estimated_files: 3
skills_used: []
---

# T01: Added red Corkboard drop-contract tests plus a minimal DropCardCommand skeleton for same-Part reorder, cross-Part move, empty-Part drop, and invalid-drop failure visibility.

Executor skills_used frontmatter: `tdd`, `verify-before-complete`.

Why: The current Corkboard reorder path is textual/card-neighbor only: `ReorderCardRequest` resolves chapter-only visual moves into `ReorderEntryAsync`, which cannot prove cross-Part file movement or empty-Part drops. This task creates the red/contract tests that define the slice boundary before implementation.

Do:
1. Add focused failing tests in `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs` for Corkboard structural intent mapping using existing `TestContext`, `FakeBookTxtStructureService`, and `SpyWorkspaceViewModel` patterns.
2. Extend the fake structure service to record `MoveEntryAsync` calls separately from `ReorderEntryAsync` calls.
3. Cover at least these contract cases: same-Part card dropped after a sibling calls `ReorderEntryAsync` with an all-node Book.txt index; card dropped from `part-one/` to `part-two/` calls `MoveEntryAsync` with a replacement path under the target Part folder; card dropped into an empty Part resolves to the slot immediately after the Part divider; invalid missing/excluded/self drops do not call Core and set `LastStructuralError` plus notification.
4. If a small request type or helper signature is needed, add it in `src/Hymnal/ViewModels/CorkboardViewModel.cs` as a minimal skeleton only; do not implement full behavior here unless required to make tests compile.

Done when: the tests compile and fail for missing behavior, and their names clearly document the contract the next task must satisfy.

## Inputs

- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `src/Hymnal/ViewModels/CorkboardItemViewModel.cs`
- `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`
- `.gsd/milestones/M005/slices/S01/S01-SUMMARY.md`
- `.gsd/milestones/M005/slices/S04/S04-SUMMARY.md`

## Expected Output

- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`
- `src/Hymnal/ViewModels/CorkboardViewModel.cs`

## Verification

powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewModelTests --verbosity minimal"

## Observability Impact

Defines negative tests that require `LastStructuralError` and notification messages to expose rejected Corkboard drops and failed structural moves.

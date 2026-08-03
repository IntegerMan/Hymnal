---
estimated_steps: 11
estimated_files: 2
skills_used: []
---

# T02: Implemented Corkboard DropCardCommand orchestration for same-Part reorders, cross-Part MoveEntry moves, empty-Part drops, reloads, selection restoration, and visible failure reporting.

Executor skills_used frontmatter: `tdd`, `verify-before-complete`.

Why: S01 established `MoveEntryAsync` for cross-Part chapter moves and S04 established watcher-suppressed reload after successful structural writes. Corkboard must now consume those contracts directly instead of treating every drag as a textual `ReorderEntryAsync`.

Do:
1. Implement the request/helper shape introduced by T01 in `src/Hymnal/ViewModels/CorkboardViewModel.cs` so Corkboard can represent drops before/after a card, into a Part divider, into an empty Part hint, and optionally root/book-level slots.
2. Resolve source and targets against `_workspace.Nodes` (all Book.txt entries, Parts plus chapters), not only `board.Items` chapter cards.
3. Determine the source Part folder and target Part folder from the nearest owning Part/target Part. Same-Part moves must call `_structureService.ReorderEntryAsync(_workspace.BookTxtPath, sourcePath, newIndex)`.
4. Cross-Part moves must compute a replacement path in the target Part folder using the source file name, then call `_structureService.MoveEntryAsync(_workspace.BookTxtPath, sourcePath, replacementPath, newIndex)`. For root/book-level target, replacement path should be the source file name at manuscript root. Do not use `RenameEntryAsync` plus `ReorderEntryAsync`.
5. Wrap structural writes in `_manuscriptService.SuppressFileWatcher()` and reload with `_workspace.ReloadCurrentWorkspaceAsync()` after successful same-Part and cross-Part operations so the Corkboard reflects canonical disk state. Avoid `ReorderNodesAsync()` for S05 Corkboard moves because file paths and Part ownership can change.
6. Preserve or update selection after reload: same-Part keeps the same path; cross-Part restores selection to the replacement path.
7. Keep failure behavior phase-visible through `ReportStructuralFailure` and do not mutate visible projection on rejected requests.

Done when: T01 contract tests pass and prove same-Part vs cross-Part calls, empty-Part mapping, reload behavior, and failure visibility.

## Inputs

- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `src/Hymnal/ViewModels/CorkboardItemViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`
- `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`

## Expected Output

- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`

## Verification

powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewModelTests --verbosity minimal"

## Observability Impact

Maintains and expands `CorkboardStructuralError` coverage so future agents can inspect the last failed Corkboard structural operation and its Book.txt path.

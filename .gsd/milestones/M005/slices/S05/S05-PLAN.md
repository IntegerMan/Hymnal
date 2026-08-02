# S05: Corkboard drag reorder and cross-Part moves

**Goal:** Reuse the existing Corkboard projection and Avalonia drag/drop plumbing so dragging included chapter cards within or across Part regions updates the canonical Book.txt structure path, moves chapter files for cross-Part drops, preserves UUID-backed metadata continuity, reloads from disk, and leaves visible failure state when a move is rejected or fails.
**Demo:** After this, dragging a Corkboard chapter card within or across Part dividers updates Book.txt, moves files when needed, and the Corkboard reflects the new structure after reload.

## Must-Haves

- Corkboard drag intent maps visual positions against mixed board items (Part dividers, chapter cards, empty-Part hints, collapsed/expanded regions, and root/book-level area) to Book.txt entry indexes without using chapter-only indexes.
- Same-Part card reorder remains a thin call to `IBookTxtStructureService.ReorderEntryAsync` and reloads/reprojects the board from canonical workspace state.
- Cross-Part card movement calls `IBookTxtStructureService.MoveEntryAsync` with a replacement path in the target Part folder and a Book.txt all-entry insert index; it does not compose rename plus reorder in the ViewModel.
- Empty Part drops are supported by mapping to the slot immediately after the Part divider.
- Illegal or unsupported drops (missing/excluded source, self-drop, target path conflict, no workspace, Core failure, reload failure) surface `CorkboardStructuralError` and `INotificationService.ShowError` without changing the visible board projection.
- Automated tests cover position mapping around Part dividers, empty Part regions, same-Part reorder, cross-Part file movement, reload persistence, and unchanged state on failure.
- Desktop UAT checklist for actual pointer drag behavior is updated so S08 can replay the same Corkboard scenario end-to-end.

## Proof Level

- This slice proves: This slice proves integration-level behavior with real Core file movement covered by automated tests and actual Avalonia drag behavior covered by desktop UAT checklist. Human/UAT is required for pointer gesture confidence because Avalonia DragDrop.DoDragDropAsync is brittle to automate headlessly.

## Integration Closure

Consumes S01 `IBookTxtStructureService.MoveEntryAsync` for path-changing cross-Part moves and S04 `ReorderEntryAsync`/reload patterns. Introduces Corkboard-specific drop target mapping in `CorkboardViewModel` and `CorkboardView.axaml.cs`; no new Core write path is allowed. Remaining milestone work after this slice: S06 Corkboard include/insert, S07 Gantt reorder, and S08 cross-surface UAT polish.

## Verification

- Runtime diagnostics remain desktop-local: Corkboard structural failures populate `CorkboardViewModel.LastStructuralError` with operation/path/message/Book.txt path and emit `INotificationService.ShowError`. Tests must assert these failure surfaces for Core move failures, reload failures, and invalid drop requests.

## Tasks

- [x] **T01: Added red Corkboard drop-contract tests plus a minimal DropCardCommand skeleton for same-Part reorder, cross-Part move, empty-Part drop, and invalid-drop failure visibility.** `est:1h`
  Executor skills_used frontmatter: `tdd`, `verify-before-complete`.
  - Files: `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`, `src/Hymnal/ViewModels/CorkboardViewModel.cs`, `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`
  - Verify: powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewModelTests --verbosity minimal"

- [x] **T02: Implemented Corkboard DropCardCommand orchestration for same-Part reorders, cross-Part MoveEntry moves, empty-Part drops, reloads, selection restoration, and visible failure reporting.** `est:2h`
  Executor skills_used frontmatter: `tdd`, `verify-before-complete`.
  - Files: `src/Hymnal/ViewModels/CorkboardViewModel.cs`, `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`
  - Verify: powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewModelTests --verbosity minimal"

- [x] **T03: Wired Corkboard Part headers and empty-Part hints into drag/drop and routed all card drops through rich CorkboardDropRequest helpers.** `est:1.5h`
  Executor skills_used frontmatter: `tdd`, `verify-before-complete`.
  - Files: `src/Hymnal/Views/CorkboardView.axaml.cs`, `src/Hymnal/Views/CorkboardView.axaml`, `tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs`
  - Verify: powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewSmokeTests --verbosity minimal"

- [x] **T04: Restore Corkboard integration verification and re-prove real file movement** `est:1h`
  Fix the S05 integration test harness so the corkboard-focused regression suite compiles again, then re-run the real temp-workspace Corkboard integration coverage for same-Part reorder persistence, cross-Part file movement, reload continuity, and conflict failure visibility. Keep the harness aligned with actual ViewModel lifetimes rather than assuming WorkspaceViewModel is disposable.
  - Files: `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs`
  - Verify: powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter 'CorkboardViewModelIntegrationTests|CorkboardViewSmokeTests|CorkboardViewModelTests' --verbosity minimal"

- [x] **T05: Harden cross-Part move path for nested Parts and post-commit failure recovery** `est:2h`
  Correct cross-Part replacement-path derivation so moves into nested Part folders keep the full target folder path, and close the post-commit manifest failure gap so a manifest-save failure does not leave the corkboard showing stale state after a committed move. Add automated coverage for nested-Part drops and the chosen manifest-failure recovery behavior.
  - Files: `src/Hymnal/ViewModels/CorkboardViewModel.cs`, `src/Hymnal.Core/Services/BookTxtStructureService.cs`, `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs`, `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`
  - Verify: powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test Hymnal.slnx --nologo --verbosity minimal"

## Files Likely Touched

- tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs
- src/Hymnal/ViewModels/CorkboardViewModel.cs
- src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs
- src/Hymnal/Views/CorkboardView.axaml.cs
- src/Hymnal/Views/CorkboardView.axaml
- tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs
- tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs
- src/Hymnal.Core/Services/BookTxtStructureService.cs

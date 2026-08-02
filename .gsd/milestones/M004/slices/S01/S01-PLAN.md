# S01: Plan Mode Corkboard Cards

**Goal:** Deliver Plan mode as a compact corkboard board that projects Book.txt manuscript order into Part dividers plus live chapter cards, supports selection/open-to-Write, and performs explicit structural edits by atomically updating Book.txt without introducing a duplicate manuscript model.
**Demo:** Open a workspace, click Plan, see Part dividers plus cards for each chapter with status, word count, optional target percentage and phase dates, then click a card and verify Write mode opens that chapter.

## Must-Haves

- Plan tab is enabled and uses existing ShellMode.Plan. In a loaded workspace, Plan mode hides left and right chrome like Manage mode and shows a scrollable compact board with Part dividers, empty-Part hints, and one card per chapter in Book.txt order. Cards show title, status, word count, target/progress or No target, and phase dates where present; updates to chapter status, dates, targets, or word counts refresh while the board is alive. Single-click selects with an accent state; Enter or double-click opens the selected chapter through the existing WorkspaceViewModel/EditorViewModel switch path and activates Write mode. Drag reorder, rename, new chapter, include existing file, remove from Book, and delete file actions are explicit author actions; reorder/remove/include/new/rename update Book.txt atomically and delete requires confirmation. Verification includes Corkboard projection/VM tests, BookTxt structural service tests, shell-mode wiring tests, and a manual smoke note for opening Plan mode with a sample workspace.

## Proof Level

- This slice proves: integration: real ViewModels, Core structural service, DI, and AXAML composition are wired; automated tests prove projection, structural Book.txt edits, and shell navigation contracts. Real desktop UAT is still recommended for drag gestures and visual polish.

## Integration Closure

Consumes existing ShellMode, MainWindowViewModel shell navigation, WorkspaceViewModel.Nodes, ChapterViewModel status/word/target/phase state, EditorViewModel chapter opening through WorkspaceViewModel.SelectedNode, IMetadataStore atomic writes, BookTxtParser, and MainWindow chrome code-behind. Introduces CorkboardViewModel/CardViewModel/CorkboardItemViewModel, BookTxtStructureService/IBookTxtStructureService, CorkboardView, Plan tab wiring, and structural command UI. Later M004 slices still need supplemental docs and Git toolbar surfaces to coexist with the new Plan-mode full-board shell.

## Verification

- Failures in structural actions must surface via INotificationService with the target Book.txt/chapter path and raw exception message. CorkboardViewModel should expose LastStructuralError or equivalent testable state in addition to notifications so a future agent can inspect what command failed. No secrets or PII are involved; file paths may be user-local and should be shown only in app notifications/test output, not persisted to metadata.

## Tasks

- [x] **T01: Add atomic Book.txt structural edit service** `est:2h`
  Expected executor skills: tdd, verify-before-complete.
  - Files: `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`, `src/Hymnal.Core/Services/BookTxtStructureService.cs`, `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`
  - Verify: dotnet test Hymnal.sln --filter FullyQualifiedName~BookTxtStructureServiceTests

- [x] **T02: Project chapters into live corkboard card items** `est:2h`
  Expected executor skills: tdd, verify-before-complete.
  - Files: `src/Hymnal/ViewModels/CardViewModel.cs`, `src/Hymnal/ViewModels/CorkboardItemViewModel.cs`, `tests/Hymnal.Core.Tests/ViewModels/CorkboardProjectionTests.cs`
  - Verify: dotnet test Hymnal.sln --filter FullyQualifiedName~CorkboardProjectionTests

- [x] **T03: Implement CorkboardViewModel commands and structural workflow** `est:3h`
  Expected executor skills: tdd, verify-before-complete.
  - Files: `src/Hymnal/ViewModels/CorkboardViewModel.cs`, `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`
  - Verify: dotnet test Hymnal.sln --filter FullyQualifiedName~CorkboardViewModelTests

- [x] **T04: Wire Plan mode into DI, shell navigation, and full-board chrome** `est:2h`
  Expected executor skills: tdd, verify-before-complete.
  - Files: `src/Hymnal/App.axaml.cs`, `src/Hymnal/ViewModels/MainWindowViewModel.cs`, `src/Hymnal/Views/MainWindow.axaml.cs`, `tests/Hymnal.Core.Tests/Views/ShellModeConvertersTests.cs`, `tests/Hymnal.Core.Tests/ViewModels/MainWindowPlanModeTests.cs`
  - Verify: dotnet test Hymnal.sln --filter FullyQualifiedName~MainWindowPlanModeTests

- [x] **T05: Build CorkboardView and wire card interactions** `est:3h`
  Expected executor skills: make-interfaces-feel-better, verify-before-complete.
  - Files: `src/Hymnal/Views/CorkboardView.axaml`, `src/Hymnal/Views/CorkboardView.axaml.cs`, `src/Hymnal/Views/MainWindow.axaml`, `tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs`
  - Verify: dotnet test Hymnal.sln --filter FullyQualifiedName~CorkboardViewSmokeTests

## Files Likely Touched

- src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs
- src/Hymnal.Core/Services/BookTxtStructureService.cs
- tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs
- src/Hymnal/ViewModels/CardViewModel.cs
- src/Hymnal/ViewModels/CorkboardItemViewModel.cs
- tests/Hymnal.Core.Tests/ViewModels/CorkboardProjectionTests.cs
- src/Hymnal/ViewModels/CorkboardViewModel.cs
- tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs
- src/Hymnal/App.axaml.cs
- src/Hymnal/ViewModels/MainWindowViewModel.cs
- src/Hymnal/Views/MainWindow.axaml.cs
- tests/Hymnal.Core.Tests/Views/ShellModeConvertersTests.cs
- tests/Hymnal.Core.Tests/ViewModels/MainWindowPlanModeTests.cs
- src/Hymnal/Views/CorkboardView.axaml
- src/Hymnal/Views/CorkboardView.axaml.cs
- src/Hymnal/Views/MainWindow.axaml
- tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs

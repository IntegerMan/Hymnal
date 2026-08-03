---
estimated_steps: 12
estimated_files: 2
skills_used: []
---

# T03: Implement CorkboardViewModel commands and structural workflow

Expected executor skills: tdd, verify-before-complete.

Why: Plan mode needs a board coordinator that rebuilds items from `WorkspaceViewModel.Nodes`, owns card selection, emits open requests to the shell, and delegates all structural edits to the atomic Core service.

Do:
1. Add `CorkboardViewModel` that subscribes to `WorkspaceViewModel.Nodes` collection changes, builds mixed corkboard items from T02, and disposes/rebuilds item subscriptions safely.
2. Add selected-card state plus commands for select card, open selected card, open specific card, reorder card to an index/neighbor, rename/title/path update, create new chapter, include existing chapter file, remove from Book, and delete chapter file. Keep Part items non-selectable.
3. Opening a card should not call editor file APIs directly; expose an `OpenChapterRequested` observable/event or command result carrying the underlying `ChapterViewModel` so `MainWindowViewModel` can set `WorkspaceViewModel.SelectedNode` and switch to `ShellMode.Write`.
4. Structural commands call `IBookTxtStructureService`, then trigger workspace reload through an explicit public reload method if needed. If a public reload seam is missing, add the smallest public method to `WorkspaceViewModel` that reloads the current workspace from the existing model and preserves editor dirty-state behavior; do not bypass `_isSwitching` or editor save-before-switch logic.
5. Add tests for initial build order, live rebuild on workspace node collection changes, selection exclusivity, Enter/open request emission, reorder service call, non-selectable Parts, empty manuscript state, service failure notification/diagnostic, and delete requiring a confirmed command path.

Failure Modes (Q5): service failure should leave current selection intact, not mutate board items optimistically, and surface the error; editor dirty save failures remain handled by existing `WorkspaceViewModel.SelectedNode` switch path.
Load Profile (Q6): rebuilding 100+ cards should be one pass over `WorkspaceViewModel.Nodes`; avoid per-card filesystem reads or service calls during projection.
Negative Tests (Q7): open with no selected card, select Part divider, reorder unknown/missing card, service failure, empty workspace, and delete command invoked without confirmation token/flag.
Done when: CorkboardViewModel tests prove that the board can be operated without the AXAML view and all file mutations flow through `IBookTxtStructureService`.

## Inputs

- `src/Hymnal/ViewModels/CardViewModel.cs`
- `src/Hymnal/ViewModels/CorkboardItemViewModel.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/EditorViewModel.cs`
- `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`
- `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`

## Expected Output

- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`

## Verification

dotnet test Hymnal.sln --filter FullyQualifiedName~CorkboardViewModelTests

## Observability Impact

ViewModel commands notify failures and keep a `LastStructuralError`/similar diagnostic property with operation name and path so tests and future agents can inspect failed structural actions.

# S04: Chapter Notes Panel

**Goal:** Add a toggleable right-sidebar Notes panel that loads, auto-saves, and persists per-chapter freeform notes to .hymnal-data/notes/ using the existing IMetadataStore atomic-write infrastructure, completing milestone M001.
**Demo:** Toggling the Notes panel shows the active chapter's notes; writing a note and saving persists to .hymnal-data/notes/; reopening the chapter reloads the note

## Must-Haves

- dotnet build src/Hymnal/Hymnal.csproj -c Debug --nologo succeeds with 0 errors 0 warnings
- dotnet test tests/Hymnal.Core.Tests --filter "NotesService" exits 0 with all tests passing
- NotesView renders as a right-sidebar panel with chapter label and multiline TextBox
- Toggling the Notes button (or F4) shows/hides the notes panel
- Writing text in the panel auto-saves to .hymnal-data/notes/ after ~1.5s idle
- Reopening a chapter reloads its previously saved note text
- Panel is hidden when no chapter is active; appears on chapter open

## Proof Level

- This slice proves: integration — real file I/O via NotesService tested; UI composition verified by build; manual round-trip required for UAT

## Integration Closure

Upstream: EditorViewModel.ActiveNode (chapter lifecycle), ManuscriptModel.WorkspaceRoot (notes path base), IMetadataStore (atomic writes), INotificationService (error banners). New wiring: INotesService/NotesService registered in DI; NotesViewModel singleton observing EditorViewModel; NotesView column added to MainWindow shell. After this slice M001 is feature-complete.

## Verification

- Auto-save errors surfaced as INotificationService.ShowError banners. No new structured logging.

## Tasks

- [x] **T01: Added INotesService/NotesService for per-chapter note persistence and exposed WorkspaceRoot seam on WorkspaceViewModel** `est:45m`
  Why: Notes persistence requires a load/save abstraction and a public workspace-root seam that NotesViewModel can consume. Currently WorkspaceViewModel does not expose WorkspaceRoot and IMetadataStore has no read API.
  - Files: `src/Hymnal.Core/Interfaces/INotesService.cs`, `src/Hymnal.Core/Infrastructure/NotesService.cs`, `src/Hymnal/ViewModels/WorkspaceViewModel.cs`, `tests/Hymnal.Core.Tests/Infrastructure/NotesServiceTests.cs`
  - Verify: dotnet test tests/Hymnal.Core.Tests --filter "NotesService" --nologo

- [x] **T02: Added NotesViewModel with reactive chapter observation, throttled auto-save, chapter-switch CancellationToken safety, and ToggleCommand; wired into MainWindowViewModel and App DI** `est:45m`
  Why: The notes panel needs a ViewModel that observes the active chapter, loads notes on chapter open, clears on chapter close, and debounce-saves text changes without stale writes on chapter switch.
  - Files: `src/Hymnal/ViewModels/NotesViewModel.cs`, `src/Hymnal/ViewModels/MainWindowViewModel.cs`, `src/Hymnal/App.axaml.cs`
  - Verify: dotnet build src/Hymnal/Hymnal.csproj -c Debug --nologo

- [x] **T03: Added NotesView AXAML/code-behind, wired 5-column MainWindow shell with GridSplitter, added toolbar ToggleButton, and F4 KeyBinding — build passes 0 errors 0 warnings** `est:45m`
  Why: The ViewModel is useless without a visible panel. This task wires NotesView into the 4-column shell, adds the toolbar toggle button and F4 shortcut, and collapses the notes column properly when hidden.
  - Files: `src/Hymnal/Views/NotesView.axaml`, `src/Hymnal/Views/NotesView.axaml.cs`, `src/Hymnal/Views/MainWindow.axaml`, `src/Hymnal/Views/MainWindow.axaml.cs`
  - Verify: dotnet build src/Hymnal/Hymnal.csproj -c Debug --nologo

## Files Likely Touched

- src/Hymnal.Core/Interfaces/INotesService.cs
- src/Hymnal.Core/Infrastructure/NotesService.cs
- src/Hymnal/ViewModels/WorkspaceViewModel.cs
- tests/Hymnal.Core.Tests/Infrastructure/NotesServiceTests.cs
- src/Hymnal/ViewModels/NotesViewModel.cs
- src/Hymnal/ViewModels/MainWindowViewModel.cs
- src/Hymnal/App.axaml.cs
- src/Hymnal/Views/NotesView.axaml
- src/Hymnal/Views/NotesView.axaml.cs
- src/Hymnal/Views/MainWindow.axaml
- src/Hymnal/Views/MainWindow.axaml.cs

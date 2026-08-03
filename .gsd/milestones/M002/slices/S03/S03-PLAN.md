# S03: Chapter Info Pane and Validation Margin

**Goal:** Wire the Chapter Info pane (F3), refactor the right-rail shared host to hold both Chapter Info and Notes, and add an advisory ValidationMargin for two Markua patterns — completing M002's author-facing status/count/advisory surface.
**Demo:** Press F3: Chapter Info pane opens on the right rail showing the chapter's current status (Drafting), today as phase start date, live word count, and target if set. Change status to Editing in the pane: phase start date auto-fills with today. Open a Markua file with a blank line before a {sample: true} heading: an advisory dot appears in the editor gutter (no crash, no blocking). GridSplitter resizes both panes.

## Must-Haves

- dotnet build src/Hymnal/Hymnal.csproj -nologo exits 0; dotnet test Hymnal.sln -nologo passes all existing tests (57 baseline) with 0 failures; ValidationMargin registers without crash; ChapterInfoViewModel loads active chapter state on F3 toggle.

## Proof Level

- This slice proves: Final assembly — real Avalonia UI; verified by clean build + full solution test pass (no regressions). Desktop smoke pass confirms F3 pane opens with live chapter state and advisory gutter renders.

## Integration Closure

Consumes ChapterViewModel (S01: Uuid, Status, PhaseData, ChangeStatusCommand), PhaseDataService (S01), TargetsService (S02), ChapterViewModel.WordCount/Target/ProximityFill (S02), AppSettingsStore (M001), EditorViewModel.ActiveNode (M001/S01), WorkspaceViewModel.Nodes (S01), NotesViewModel.IsVisible (M001). Produces ChapterInfoViewModel + ChapterInfoView + ValidationMargin + refactored MainWindow right rail + DI wiring — completing the milestone's presentation layer.

## Verification

- None added. All exceptions in ValidationMargin are swallowed silently per spec. ChapterInfoViewModel surfacing errors via INotificationService (same pattern as NotesViewModel).

## Tasks

- [x] **T01: ChapterInfoViewModel, ApplyPhaseData mutator, and DI wiring** `est:2h`
  **Why:** ChapterInfoViewModel is the logic backbone for the F3 pane. It must mirror the NotesViewModel lifecycle pattern (observe ActiveNode, cancel-on-switch CancellationToken, gated ToggleCommand) while adding status-change, date-edit, and target-edit commands that persist via PhaseDataService/TargetsService with optional phase-date pre-fill. PhaseDataService.UpsertAsync must be called directly by ChapterInfoViewModel (not via ChapterViewModel.ChangeStatusCommand) so the pre-fill toggle can be honoured; ChapterViewModel then needs a public ApplyPhaseData() mutator to re-sync observable state.
  - Files: `src/Hymnal/ViewModels/ChapterViewModel.cs`, `src/Hymnal/ViewModels/ChapterInfoViewModel.cs`, `src/Hymnal/ViewModels/MainWindowViewModel.cs`, `src/Hymnal/App.axaml.cs`
  - Verify: dotnet build src/Hymnal/Hymnal.csproj -nologo

- [x] **T02: ChapterInfoView and right-rail MainWindow refactor** `est:2h`
  **Why:** ChapterInfoView is the visible F3 pane. MainWindow.axaml must be refactored so the right rail hosts both Chapter Info and Notes as independently toggleable stacked sections with a row GridSplitter when both are visible.
  - Files: `src/Hymnal/Views/ChapterInfoView.axaml`, `src/Hymnal/Views/ChapterInfoView.axaml.cs`, `src/Hymnal/Views/MainWindow.axaml`, `src/Hymnal/Views/Converters/NodeKindConverters.cs`
  - Verify: dotnet build src/Hymnal/Hymnal.csproj -nologo

- [x] **T03: ValidationMargin: AbstractMargin advisory gutter for two Markua patterns** `est:1.5h`
  **Why:** The ValidationMargin surfaces advisory Markua issues as a gutter dot without ever blocking the editor or throwing. It must extend `AbstractMargin` (AvaloniaEdit) and be registered in `EditorView.axaml.cs` against `PART_Editor.TextArea.LeftMargins`.
  - Files: `src/Hymnal/Views/Editor/ValidationMargin.cs`, `src/Hymnal/Views/EditorView.axaml.cs`
  - Verify: dotnet test Hymnal.sln -nologo

## Files Likely Touched

- src/Hymnal/ViewModels/ChapterViewModel.cs
- src/Hymnal/ViewModels/ChapterInfoViewModel.cs
- src/Hymnal/ViewModels/MainWindowViewModel.cs
- src/Hymnal/App.axaml.cs
- src/Hymnal/Views/ChapterInfoView.axaml
- src/Hymnal/Views/ChapterInfoView.axaml.cs
- src/Hymnal/Views/MainWindow.axaml
- src/Hymnal/Views/Converters/NodeKindConverters.cs
- src/Hymnal/Views/Editor/ValidationMargin.cs
- src/Hymnal/Views/EditorView.axaml.cs

---
id: S03
milestone: M002
status: ready
---

# S03: Chapter Info Pane and Validation Margin — Context

## Goal

Wire the Chapter Info pane (F3) into a refactored right-rail shared host showing status picker, Avalonia DatePicker phase dates, live word count, and editable word count target for the open chapter; and add a standalone ValidationMargin detecting two advisory Markua patterns.

## Why this Slice

S01 established ChapterViewModel wrappers with status and phase data; S02 established live word count and targets. S03 is the presentation layer that surfaces both to the author through a dedicated pane, completes the milestone's user-facing goal, and requires no further data-model changes. The right-rail refactor must happen here — it is the blocker for wiring ChapterInfoView alongside the existing NotesView.

## Scope

### In Scope

- Refactor right rail in `MainWindow.axaml`: replace single-pane Border with a shared host `Grid` whose rows hold Chapter Info (top) and Notes (bottom), separated by a row `GridSplitter` that appears only when both panes are expanded
- Collapsed state: 48px icon strip with F3 icon (Chapter Info) and F4 icon (Notes) stacked; both start collapsed when a chapter is opened; horizontal column GridSplitter appears when either pane is expanded
- `ChapterInfoView.axaml` / `ChapterInfoView.axaml.cs`: status ComboBox (colour swatch matching synthwave status colours from S01), Avalonia DatePicker for phase start and end dates, read-only live word count label, editable integer word count target field, proximity progress bar, phase-date pre-fill checkbox ("Auto-fill today on status change")
- `ChapterInfoViewModel`: mirrors NotesViewModel lifecycle (WhenAnyValue ActiveNode, cancel-on-chapter-switch CancellationTokenSource, ToggleCommand gated on hasActiveNode); loads/saves status + phase dates via PhaseDataService; loads/saves target via TargetsService; displays word count from ChapterViewModel.WordCount; reads and writes phase-date pre-fill preference via AppSettingsStore; applies pre-fill when status changes (if toggle is on)
- `MainWindowViewModel`: add `ChapterInfoViewModel` property; add right-rail coordination (both panes independently toggled; neither force-closes the other); horizontal GridSplitter visibility driven by `ChapterInfoViewModel.IsVisible || NotesViewModel.IsVisible`
- F3 KeyBinding in `MainWindow.axaml`; "Toggle Chapter Info" menu item under `_View`
- `ValidationMargin` (`AbstractMargin` primary, `IBackgroundRenderer` fallback) registered in `EditorView.axaml.cs`; detects two patterns; all exceptions swallowed silently
  - Pattern 1: blank line immediately before a `{sample: true}` attribute line that precedes a heading
  - Pattern 2: unrecognised Markua attribute key (hard-coded valid-key list in M002)
- Advisory dot in left gutter margin; no blocking behaviour; no crash on any document state
- DI registration of `ChapterInfoViewModel` in `App.axaml.cs`

### Out of Scope

- xUnit tests for `ChapterInfoViewModel` or `ValidationMargin` — UI layer; covered by desktop smoke pass only
- Avalonia DatePicker full custom theme beyond minimal synthwave colour alignment (full styling deferred)
- Min/max range target editing — single integer only; range variant deferred
- Progress chart or writing-velocity display — data accumulates in S02; chart deferred
- Sharing `ValidationMargin` infrastructure with M005 `IssueMargin` — deliberate non-goal per M002-CONTEXT
- Additional Markua validation patterns beyond the two above
- Persistence of right-pane column width or vertical split ratio across sessions

## Constraints

- `Hymnal.Core` must remain free of Avalonia references; `ChapterInfoViewModel` and `ValidationMargin` live in the `Hymnal` UI project
- `ValidationMargin` must swallow all exceptions silently — advisory hints must never crash the editor
- Right-rail refactor must preserve all existing Notes (F4) behaviour: auto-load on chapter switch, 1500ms debounce-save, visibility restored across chapter switches, ToggleCommand gated on hasActiveNode
- Phase-date pre-fill preference stored in `AppSettingsStore` (existing Core pattern); no new settings file
- `AbstractMargin` is untested in this codebase — verify the extension point in the first S03 task before adding patterns; fall back to `IBackgroundRenderer` if AbstractMargin causes build failures
- Avalonia DatePicker is new to this codebase — verify binding and minimal styling in the first S03 UI task; fallback is plain TextBox bound to an ISO date string

## Integration Points

### Consumes

- `ChapterViewModel` (S01) — status, phase start/end dates, UUID; provides the per-chapter mutable state ChapterInfoViewModel binds to
- `PhaseDataService` (S01) — load/save `PhaseData` (status + dates) keyed by chapter UUID
- `ChapterRegistryService` (S01) — UUID resolution for the open chapter
- `TargetsService` (S02) — load/save integer word count target keyed by chapter UUID
- `ChapterViewModel.WordCount` (S02) — live word count for display in the pane
- `AppSettingsStore` — read/write phase-date pre-fill preference (boolean)
- `EditorViewModel.ActiveNode` — chapter switch signal for ChapterInfoViewModel lifecycle (same as NotesViewModel)
- `NotesViewModel.IsVisible` — right-rail layout coordination (horizontal splitter visibility)
- `PART_Editor.TextArea.LeftMargins` — registration point for ValidationMargin in `EditorView.axaml.cs`

### Produces

- `src/Hymnal/ViewModels/ChapterInfoViewModel.cs` — bindable pane state; status/target/date persistence; pre-fill toggle logic
- `src/Hymnal/Views/ChapterInfoView.axaml` + `ChapterInfoView.axaml.cs` — Chapter Info pane UI
- `src/Hymnal/Views/Editor/ValidationMargin.cs` — AbstractMargin advisory gutter for two Markua patterns
- Updated `src/Hymnal/Views/MainWindow.axaml` — refactored right-rail shared host; F3 KeyBinding; View menu item; row GridSplitter; horizontal splitter visibility condition updated
- Updated `src/Hymnal/ViewModels/MainWindowViewModel.cs` — ChapterInfoViewModel property; right-rail coordination
- Updated `src/Hymnal/App.axaml.cs` — DI registration for ChapterInfoViewModel

## Open Questions

- **AbstractMargin vs IBackgroundRenderer**: AbstractMargin is the preferred extension point per M002-CONTEXT but is untested in this codebase. First S03 task must prove it compiles and renders; fall back to IBackgroundRenderer if needed. Current thinking: attempt AbstractMargin first.
- **Avalonia DatePicker synthwave styling**: DatePicker may need a ControlTheme override to match the dark synthwave palette. If styling cost is too high in M002, fall back to plain TextBox accepting ISO date string (yyyy-MM-dd). Decision during first UI task.
- **Right-rail icon strip order**: F3 (Chapter Info) above F4 (Notes) in the 48px collapsed strip — visually consistent with pane order (Chapter Info on top when both open). Confirm during UI implementation if author prefers reversed order.
- **Vertical split default ratio**: 50/50 when both panes are open simultaneously. No persistence in M002. Revisable in a later milestone if the author wants remembered ratios.

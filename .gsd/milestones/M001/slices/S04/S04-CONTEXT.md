---
id: S04
milestone: M001
status: ready
---

# S04: Chapter Notes Panel — Context

## Goal

Add a toggleable right-sidebar Notes panel that loads, auto-saves, and persists per-chapter freeform notes to `.hymnal-data/notes/` using the existing `IMetadataStore` atomic-write infrastructure.

## Why this Slice

S03 delivered the editor and atomic-write infrastructure. S04 is the final M001 slice: it completes the milestone's user-visible outcome ("toggle the Notes panel to read/write per-chapter notes without leaving the editor") and closes M001. Nothing depends on S04 within M001.

## Scope

### In Scope

- `NotesViewModel` (singleton) — reactive `Text` property, `IsVisible` bool (toggle state), auto-save debounce (1–2s idle after last keystroke), load-on-chapter-open, clear/hide on chapter close
- `NotesView.axaml` — right-sidebar panel containing a chapter title label, multi-line `TextBox` (word-wrap, no highlighting), and wired to `NotesViewModel`
- `INotesService` interface in `Hymnal.Core.Interfaces`; `NotesService` implementation in `Hymnal.Core.Infrastructure` — `LoadAsync(string notesPath)` / `SaveAsync(string notesPath, string content)` delegating to `IMetadataStore`
- Notes file path derivation: `{workspaceRoot}/.hymnal-data/notes/{relativePath-slashes-to-underscores}` where `relativePath` is `ChapterNode.RelativePath` with `/` and `\` replaced by `_` (e.g. `chapters/chapter-01.md` → `chapters_chapter-01.md`)
- MainWindow layout: add a 4th column to the main shell grid (fixed 280px) hosting `NotesView`; `GridSplitter` between editor and notes columns for manual resize
- Toolbar toggle button (`Notes` label or icon, toggling `NotesViewModel.IsVisible`) — wired in `MainWindow.axaml` toolbar row
- Keyboard shortcut F4 to toggle notes panel (bound in `MainWindow.axaml.cs`)
- Panel visibility: panel is **hidden** when no chapter is active; appears when a chapter opens, hides when the workspace closes or no chapter is selected
- Auto-save uses `Observable.Throttle` (1.5s) on `NotesViewModel.Text` changes; writes via `NotesService.SaveAsync`; errors surface via `INotificationService.ShowError`
- Notes file created on first save (parent directories created by `MetadataStore`)
- On chapter open: load notes file if it exists; if file absent, start with empty `Text`
- DI registration of `INotesService` → `NotesService` in `App.axaml.cs`; `NotesViewModel` as singleton

### Out of Scope

- Markua/syntax highlighting in the Notes `TextBox` — plain multi-line `TextBox` only
- Notes dirty-state indicator or explicit save button — auto-save only, no unsaved-change marker
- Notes word count, character count, or formatting toolbar
- Resizable notes panel beyond the `GridSplitter` (no persist of panel width in M001)
- Notes search or cross-chapter notes browsing
- Notes panel width persistence in `AppSettingsStore` (deferred post-M001)
- Notes panel toggle state persistence across sessions (panel starts hidden each launch)

## Constraints

- `Hymnal.Core.csproj` must retain zero Avalonia package references — `INotesService` and `NotesService` live in Core
- All notes file writes must go through `IMetadataStore.WriteTextAtomicAsync` — never `File.WriteAllText` directly
- Notes files stored at `.hymnal-data/notes/` relative to workspace root; workspace root comes from `ManuscriptModel.WorkspaceRoot` (set by `ManuscriptService`, established in D010)
- `ReactiveCommand.ThrownExceptions` subscribed in `NotesViewModel` (established pattern)
- Auto-save throttle must cancel on chapter switch to prevent a stale write landing after the new chapter loads

## Integration Points

### Consumes

- `EditorViewModel.ActiveNode` (ReactiveProperty `ChapterNode?`) — observed to trigger load/clear on chapter change
- `ManuscriptModel.WorkspaceRoot` (string) — used to construct the absolute `.hymnal-data/notes/` base path
- `IMetadataStore` (via `NotesService`) — atomic file write for every auto-save flush
- `INotificationService` — surfaces auto-save errors as error banners
- `SynthwaveTheme.axaml` named brushes — Notes panel styled consistently with sidebar

### Produces

- `INotesService` in `Hymnal.Core.Interfaces` — interface for loading/saving note text by absolute path
- `NotesService` in `Hymnal.Core.Infrastructure` — implementation wrapping `IMetadataStore`
- `NotesViewModel` in `src/Hymnal/ViewModels/` — toggle state, reactive text, throttled auto-save
- `NotesView.axaml` in `src/Hymnal/Views/` — right-sidebar panel bound to `NotesViewModel`
- Updated `MainWindow.axaml` — 4th column + `GridSplitter` + toolbar toggle button
- Updated `App.axaml.cs` — `INotesService` and `NotesViewModel` DI registrations

## Open Questions

- **`GridSplitter` behaviour in Avalonia 12:** confirm that `GridSplitter` between the editor column (`*`) and notes column (280px) resizes correctly without collapsing the editor — use `MinWidth` on both columns as a guard. Current thinking: `MinWidth=200` on editor column, `MinWidth=180` on notes column.
- **Auto-save throttle cancellation on chapter switch:** the `Observable.Throttle` subscription must be disposed or the pending write suppressed when `EditorViewModel.ActiveNode` changes to avoid a stale flush. Current thinking: subscribe to `ActiveNode` changes in `NotesViewModel` and reset the throttle subject on switch before loading the new chapter's notes.
- **Notes file absent on load:** if `.hymnal-data/notes/chapter-01.md` does not exist yet, `NotesService.LoadAsync` should return `Result<string>.Ok("")` rather than an error. Current thinking: catch `FileNotFoundException` and return empty string.

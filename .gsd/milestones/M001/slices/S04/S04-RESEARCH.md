# S04: Chapter Notes Panel — Research

**Date:** 2026-05-29

## Summary

S04 is a targeted Avalonia + ReactiveUI integration slice with one important architectural seam missing from the current code: the UI already knows the active chapter (`EditorViewModel.ActiveNode`), but no public object currently exposes the active workspace root needed to place notes under `{workspaceRoot}/.hymnal-data/notes/`. `WorkspaceViewModel` holds `ManuscriptModel` privately, and `EditorViewModel` exposes only `ActiveFilePath` + `ActiveNode`. That makes workspace-root propagation the first unblocker.

The existing save infrastructure is ready for notes writes. `IMetadataStore`/`MetadataStore` already provide atomic temp-file-then-rename writes and create parent directories automatically. That is enough for note persistence. However, `IMetadataStore` is write-only today, so the slice-context phrase "LoadAsync/SaveAsync delegating to IMetadataStore" is not literally possible unless the interface grows a read API. The simpler fit is: `NotesService` reads via `File.Exists`/`File.ReadAllTextAsync` and writes via `IMetadataStore.WriteTextAtomicAsync`.

The shell and editor are also already in good shape for composition. `MainWindow.axaml` has a 3-column sidebar/editor shell with a toolbar button pattern that can host a Notes toggle. `EditorViewModel` already owns chapter-open/chapter-close lifecycle and save-before-switch behavior, so `NotesViewModel` should observe `EditorViewModel.ActiveNode` rather than inventing a parallel selection source. S03’s existing equality-guard pattern in `EditorView.axaml.cs` is a good local precedent for avoiding reactive write loops.

One planning discrepancy needs to be resolved explicitly: `.gsd/state-manifest.json` still says S04 produces "panel toggle state preserved per session," but the slice context says toggle-state persistence is out of scope and the panel starts hidden each launch. Use the slice context as source of truth unless product says otherwise.

## Recommendation

Build S04 in three tasks:

1. **Core seam + pathing** — add `INotesService`/`NotesService`, keep reads simple, and expose workspace-root information through a public VM seam the notes layer can consume.
2. **Reactive notes behavior** — add singleton `NotesViewModel` that watches `EditorViewModel.ActiveNode`, loads note text on chapter switch, clears on no chapter/workspace, and auto-saves with `Throttle` + switch-safe cancellation.
3. **Shell composition** — add `NotesView`, a toolbar toggle, F4 binding, and a truly collapsible right panel in `MainWindow`.

First proof should be the workspace-root/pathing seam plus a unit test around note-path derivation and file round-trip. Without that seam, the UI work can render but cannot save to the required `.hymnal-data/notes/` location correctly.

## Implementation Landscape

### Key Files

- `src/Hymnal/ViewModels/EditorViewModel.cs` — authoritative active-chapter state (`ActiveNode`, `ActiveFilePath`, `CloseChapter()`); best event source for note load/clear behavior.
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs` — owns the loaded `ManuscriptModel` privately and therefore currently hides `WorkspaceRoot`; this is the integration gap for notes path derivation.
- `src/Hymnal/ViewModels/MainWindowViewModel.cs` — already composes shell-level VMs and is the natural host for a `NotesViewModel` property if the view is added beside editor/sidebar.
- `src/Hymnal/Views/MainWindow.axaml` — current 3-column shell (`240,Auto,*`) with toolbar button styling; needs the notes column/splitter/toggle wiring.
- `src/Hymnal/Views/MainWindow.axaml.cs` — currently minimal; available if F4 handling must be done in code-behind, though a command + `KeyBinding` is preferable if possible.
- `src/Hymnal/App.axaml.cs` — DI registration point; must register `INotesService` and singleton `NotesViewModel`.
- `src/Hymnal.Core/Interfaces/IMetadataStore.cs` — write-only atomic persistence seam; sufficient for save, not load.
- `src/Hymnal.Core/Infrastructure/MetadataStore.cs` — creates parent directories and atomically overwrites files; exactly what notes writes need.
- `src/Hymnal.Core/Models/ManuscriptModel.cs` — already stores both `WorkspaceRoot` and `ManuscriptRoot`; the data exists, it just is not surfaced publicly to the notes layer.
- `src/Hymnal.Core/Models/ChapterNode.cs` — provides `RelativePath`; required for stable note filename derivation.
- `tests/Hymnal.Core.Tests/Infrastructure/MetadataStoreTests.cs` — existing proof that atomic writes work; can be extended or complemented by NotesService tests.

### Files to Create

- `src/Hymnal.Core/Interfaces/INotesService.cs` — focused load/save abstraction for per-chapter notes.
- `src/Hymnal.Core/Infrastructure/NotesService.cs` — file-backed implementation using `File.ReadAllTextAsync` for load and `IMetadataStore.WriteTextAtomicAsync` for save.
- `src/Hymnal/ViewModels/NotesViewModel.cs` — singleton notes state, visibility toggle, active-chapter observation, throttled auto-save.
- `src/Hymnal/Views/NotesView.axaml` — plain multiline notes editor surface with chapter label and empty/placeholder state.
- `src/Hymnal/Views/NotesView.axaml.cs` — likely minimal or unnecessary unless a local text-sync workaround is needed.
- `tests/Hymnal.Core.Tests/Infrastructure/NotesServiceTests.cs` — load-missing-as-empty, save-creates-parent-dir, save-then-reload, relative-path filename mapping.

### Likely Files to Modify

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs` — expose public `WorkspaceRoot`/`ManuscriptRoot` or another stable seam for notes path derivation.
- `src/Hymnal/ViewModels/MainWindowViewModel.cs` — add `NotesViewModel` property and possibly shell-level command exposure if F4/toggle are bound here.
- `src/Hymnal/Views/MainWindow.axaml` — add toolbar Notes button, splitter, and collapsible notes column.
- `src/Hymnal/Views/MainWindow.axaml.cs` — only if MainWindow-level key handling is easier than XAML binding.
- `src/Hymnal/App.axaml.cs` — DI registrations.
- `tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj` — no package changes expected, but new test file will live here.

### Build Order

1. **Public workspace-root seam**
   - Expose the loaded workspace root somewhere public (`WorkspaceViewModel.WorkspaceRoot` is the smallest change).
   - Decide whether `NotesViewModel` depends on `WorkspaceViewModel`, `EditorViewModel`, or receives a small context object from `MainWindowViewModel`.
2. **`INotesService` / `NotesService`**
   - Keep filename derivation deterministic: `ChapterNode.RelativePath` with `/` and `\\` replaced by `_`, stored under `.hymnal-data/notes/`.
   - Prefer a helper method for note path derivation so it is easy to test.
3. **`NotesViewModel` reactive lifecycle**
   - Observe `EditorViewModel.ActiveNode` changes.
   - On chapter switch: cancel pending save, load note or empty string, suppress auto-save during programmatic text assignment.
   - On chapter close / workspace close: clear text and hide panel.
4. **Shell UI**
   - Add `NotesView` and a toolbar toggle.
   - Implement a genuinely collapsible notes column; `IsVisible=false` alone is not enough if the column width remains 280.
5. **Verification**
   - `dotnet test tests/Hymnal.Core.Tests --filter "NotesService|MetadataStore"`
   - `dotnet build src/Hymnal/Hymnal.csproj`
   - Manual round-trip with real manuscript fixture/workspace.

### Verification Approach

- **Contract**
  - Unit test note-path derivation from chapter relative paths, including nested chapter paths.
  - Unit test save creates missing `.hymnal-data/notes/` parents via `MetadataStore`.
  - Unit test missing note file returns empty content without surfacing an error.
- **Integration / manual**
  - Open a workspace and a chapter.
  - Toggle Notes open.
  - Type a note, wait > debounce interval, verify `.hymnal-data/notes/<derived-name>.md` exists.
  - Switch chapters and confirm previous note loads back when returning.
  - Close workspace and verify the panel hides/clears.

## Don’t Hand-Roll

| Problem | Existing Solution | Why Use It |
|---------|------------------|------------|
| Atomic note writes | `IMetadataStore` / `MetadataStore` | Already proven in S03; creates directories and temp-renames safely |
| Active chapter lifecycle | `EditorViewModel.ActiveNode` + `CloseChapter()` | Single source of truth already used by the editor shell |
| Reactive debounce | `System.Reactive` / `Observable.Throttle` | Already in the stack; matches the slice-context requirement |
| Toolbar button styling | `Button.toolbar-icon` in `MainWindow.axaml` | Keeps notes toggle visually consistent with Save |
| Notes text input styling | existing `TextBox` `ControlTheme` in `src/Hymnal/Themes/ControlStyles.axaml` | Gives the synthwave look without custom control work |

## Constraints

- `Hymnal.Core` must stay Avalonia-free; `INotesService` and `NotesService` belong in Core, while `NotesViewModel` and `NotesView` belong in the UI project.
- All note writes must go through `IMetadataStore.WriteTextAtomicAsync`; do not introduce direct `File.WriteAllText*` usage for saves.
- The slice context, not stale manifest text, is the current scope authority: no cross-session persistence of panel width or toggle state in M001.
- Notes are plain multiline text; no AvaloniaEdit, no syntax highlighting, no dirty badge, no explicit save button.
- Auto-save must not land a stale write after switching chapters; pending debounce work must be cancelled or guarded by the current chapter identity.

## Common Pitfalls

- **Workspace root vs manuscript root confusion** — note files live under `WorkspaceRoot/.hymnal-data/notes`, not `ManuscriptRoot`. If `Book.txt` lives in `workspace/manuscript`, saving under manuscript root would be wrong.
- **Using `IsVisible` without collapsing the column** — a hidden `NotesView` in a fixed 280px column still leaves empty space. The column width itself must collapse to 0 when hidden/no chapter.
- **Auto-save firing during programmatic loads** — setting `Text` after a chapter switch can trigger the throttle pipeline unless there is a suppression flag or a “loaded note snapshot” comparison.
- **Stale debounce after chapter switch** — if chapter A text is pending save and the user switches to chapter B, the A save must be cancelled or identity-checked.
- **Needless widening of `IMetadataStore`** — a read method is not required for safety. Keep the interface focused unless multiple consumers now need a read abstraction.
- **Hidden coupling to private `_model`** — notes code should not reach into `WorkspaceViewModel` internals; expose a public property or a small context seam instead.
- **F4 binding placement** — binding F4 only inside `NotesView` will fail when focus is in the main editor. The shortcut should live at `Window` level.

## Open Risks

- The interrupted previous session had started eliciting note UX decisions, but the current slice context already fixes the main ones: auto-save only, one note per chapter file, panel hidden when no chapter is active. Do not block planning on that earlier incomplete questionnaire unless product explicitly reopens it.
- There is a mild design choice around ownership: `NotesViewModel` can depend directly on `EditorViewModel` + a public workspace-root seam, or `MainWindowViewModel` can orchestrate chapter changes into notes. The direct `NotesViewModel -> EditorViewModel` observation path is simpler and matches S03’s existing ownership.
- Avalonia layout may need one small helper property (`GridLength NotesColumnWidth`) if binding directly to `ColumnDefinition.Width` is awkward with compiled bindings.

## Skill Discovery

No directly installed skill in this project targets Avalonia/ReactiveUI desktop work. External skill discovery surfaced Avalonia-oriented options if the team wants extra framework-specific guidance later:

- `npx skills add sickn33/antigravity-awesome-skills@avalonia-layout-zafiro` — highest install count for Avalonia layout-focused help.
- `npx skills add sickn33/antigravity-awesome-skills@avalonia-viewmodels-zafiro` — strongest VM-oriented Avalonia result.
- `npx skills add sickn33/antigravity-awesome-skills@avalonia-zafiro-development` — broader Avalonia development guidance.

ReactiveUI-specific discovery did not surface a clearly better specialized skill than the Avalonia-focused results above.

## Sources

- `src/Hymnal/ViewModels/EditorViewModel.cs` — confirmed active chapter is exposed publicly and chapter close lifecycle already exists.
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs` — confirmed `ManuscriptModel` is private and currently hides `WorkspaceRoot` from notes consumers.
- `src/Hymnal/ViewModels/MainWindowViewModel.cs` — confirmed shell composition pattern and current VM ownership.
- `src/Hymnal/Views/MainWindow.axaml` — confirmed toolbar button styling and current 3-column layout.
- `src/Hymnal/App.axaml.cs` — confirmed DI pattern and current singleton/transient registrations.
- `src/Hymnal.Core/Interfaces/IMetadataStore.cs` and `src/Hymnal.Core/Infrastructure/MetadataStore.cs` — confirmed atomic write behavior and lack of read API.
- `src/Hymnal.Core/Models/ManuscriptModel.cs` and `src/Hymnal.Core/Models/ChapterNode.cs` — confirmed required path data already exists in the model layer.
- `src/Hymnal/Themes/SynthwaveTheme.axaml` and `src/Hymnal/Themes/ControlStyles.axaml` — confirmed existing theme/style resources suitable for the notes panel.
- `tests/Hymnal.Core.Tests/Infrastructure/MetadataStoreTests.cs` — confirmed existing contract coverage for atomic writes.
- `.gsd/STATE.md` and `.gsd/state-manifest.json` — confirmed S04 is active and exposed the manifest/context scope mismatch around toggle-state persistence.

# S03 Research — Markua Editor with Save

## Summary

S02 leaves the shell in a good state for navigation, but almost all S03 editor behavior is still missing. The right-hand pane in `src/Hymnal/Views/MainWindow.axaml:39-46` is still a placeholder border, the File menu has no Save action, `SidebarView` renders nodes but does not expose selection/open behavior, and the notification path is still debug-log-only rather than banner UI.

The biggest structural gap is path ownership. `ManuscriptService` computes `manuscriptRoot` when it finds `Book.txt` (`src/Hymnal.Core/Services/ManuscriptService.cs:30`) but throws that information away before returning `ManuscriptModel`; `ChapterNode` only carries `RelativePath` (`src/Hymnal.Core/Models/ChapterNode.cs:5`) and `ManuscriptModel` only wraps the `SourceCache` (`src/Hymnal.Core/Models/ManuscriptModel.cs:5-13`). S03 needs stable chapter-file resolution for both `{workspace}/Book.txt` and `{workspace}/manuscript/Book.txt` layouts before chapter open/save/restore can work reliably.

The second key mismatch is write safety. The slice and roadmap require atomic chapter saves, but the architecture draft still says editor saves may use standard `File.WriteAllText` (`_bmad-output/planning-artifacts/architecture.md:80`). S03 should follow the stronger slice rule: write-temp-then-rename for chapter files too. `AppSettingsStore` already shows the local atomic pattern (`src/Hymnal.Core/Infrastructure/AppSettingsStore.cs:62-65`).

## Active Requirements / Constraints

- **R002 — Markua Editor:** S03 owns the editor surface, chapter loading, dirty tracking, syntax highlighting, and explicit save affordances.
- **R012 — Atomic Writes and Data Safety:** chapter saves must be temp-write + rename; no direct overwrite path.
- **Supports R001 continuity:** last-workspace restore already exists; S03 extends it to restore the last edited chapter.
- **Supports R014 boundary/perf constraints:** AvaloniaEdit and all UI/editor control concerns stay in `src/Hymnal/`; file I/O and save infrastructure should stay Core-friendly.
- Slice-specific constraints from context: one active chapter only, no background autosave, missing chapter entries remain non-editable, and save-on-switch must block the switch if the save fails.

## Recommendation

1. **Add manuscript-root awareness before building editor behavior.** Either enrich `ManuscriptModel` with `WorkspaceRoot`, `ManuscriptRoot`, and optionally `BookTxtPath`, or introduce an editor-facing model that carries resolved chapter paths. Without this, last-chapter restore and atomic saves will be fragile whenever `Book.txt` lives in `manuscript/`.
2. **Use a Core atomic-write seam, not ad hoc file writes in the Avalonia layer.** The roadmap names `IMetadataStore`/`MetadataStore`; the safest interpretation is a reusable Core service that exposes atomic text writes and can later serve notes/metadata too. Reusing the `AppSettingsStore` temp-write + `File.Move(..., overwrite: true)` pattern keeps behavior consistent.
3. **Keep editor state single-buffer and explicit.** `EditorViewModel` should own: active node/path, current text, original text, `IsDirty`, `CanSave`, and the active chapter watcher. Avoid multi-buffer abstractions; the slice explicitly scopes them out.
4. **Treat external-change choice as editor state, not as a generic message string.** The current notification API cannot represent actionable choices. For S03, a small editor-local conflict strip / infobar is lower risk than over-generalizing the global notification system.
5. **Bind the window title to the existing VM property instead of inventing another title mechanism.** `MainWindowViewModel` already has `Title` (`src/Hymnal/ViewModels/MainWindowViewModel.cs:10-14`), but `MainWindow.axaml` still hardcodes `Title="Hymnal"` (`src/Hymnal/Views/MainWindow.axaml:11`). S03 can reuse that seam for the unsaved bullet indicator.

## Implementation Landscape

### Existing files that matter

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
  - Provides workspace open/init and the sidebar node collection.
  - Persists only `lastWorkspacePath` (`:89`, `:100`); there is no active chapter key, selected node, or chapter-open command.
  - `BindModel` only binds nodes (`:117-124`); no editor/session handoff exists.

- `src/Hymnal/Views/SidebarView.axaml`
  - Renders the `ListBox` of `Nodes` (`:48-82`) and shows missing-file warning icons.
  - No `SelectedItem` binding, no item activation command, and no disabled styling for `IsMissing`/`NodeKind.Part`.
  - This is the main integration seam for chapter switching and save-before-switch.

- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
  - Has only `Title`, `WorkspaceViewModel`, and `ExitCommand`.
  - No editor child VM, no save command aggregation, and no title recomputation beyond the default string.

- `src/Hymnal/Views/MainWindow.axaml`
  - File menu exposes only Open Workspace and Exit (`:29-35`).
  - The editor column is still a placeholder border (`:39-46`).
  - No visible Save button, no content host for notifications, and the window title is hardcoded.

- `src/Hymnal/Views/MainWindow.axaml.cs`
  - Notification subscription currently writes to Debug only and explicitly comments that banner UI is still pending (`:15-18`).

- `src/Hymnal.Core/Services/ManuscriptService.cs`
  - Only watches `Book.txt` (`:41-60`), not the active chapter file.
  - Returns a `ManuscriptModel` but does not preserve the resolved manuscript root, which S03 needs.

- `src/Hymnal.Core/Infrastructure/AppSettingsStore.cs`
  - Already implements the desired atomic-write pattern (`:62-65`).
  - Good template for `lastChapterPath` persistence and any reusable save infrastructure.

### Current gaps that block S03

- No `EditorView`, `EditorViewModel`, `MarkuaHighlighting.xshd`, `IMetadataStore`, or `MetadataStore` exists yet.
- No chapter-content load/save service exists.
- No chapter selection state exists in the view-model layer.
- No global banner UI exists; `INotificationService` is currently message-only (`src/Hymnal.Core/Interfaces/INotificationService.cs:3-7`).
- No external chapter-file watcher exists.
- No title binding from window to VM exists.

### Architecture alignment / drift

The architecture draft is directionally useful but not fully current:

- It expects `EditorView`, `EditorViewModel`, `MarkuaHighlighting.xshd`, and `IMetadataStore`/`MetadataStore` (`_bmad-output/planning-artifacts/architecture.md:480, 508, 549, 561`), which matches S03 needs.
- It assumes nested `Views/Editor/` and `ViewModels/Editor/` folders, while the existing codebase is still flat. Either is fine, but planners should choose once and keep it consistent.
- It conflicts with slice safety requirements on editor writes (`architecture.md:80` vs slice requirement for atomic chapter saves). The slice rule should win.

## Markua Highlighting Targets

The roadmap minimum is heading, bold, italic, code block, and attribute list highlighting. The local Markua research suggests a low-risk first-pass grammar should cover:

- Headings: `# Chapter`, `## Section`, and part headings using `# Part ... #`.
- Attribute lists: `{sample: true}`, `{id: ch-intro, sample: true}`, and code/image attribute lines like `{title: "Bubble sort", format: python, line-numbers: true}`.
- Inline emphasis/strong and code spans.
- Fenced code blocks and their language/attribute prelude.
- Markua directives worth cheap extra support: `{mainmatter}`, `{backmatter}`.
- Blurb prefixes `A>`, `B>`, `D>`, `E>`, `I>`, `Q>`, `T>`, `W>`, `X>`, `C>` from `docs/research/leanpub_markua_manuscript_research.md:13, 128-154`.

If time is tight, land the roadmap-minimum tokens first and treat blurbs/directives as follow-on rules inside the same XSHD file.

## Risks / Watch-outs

1. **Path resolution risk:** current models do not remember where chapter files live. This is the first thing to fix.
2. **Notification capability mismatch:** `INotificationService` only supports `ShowError/ShowInfo/ShowSuccess`; there is no action model, persistence model, or dismiss state. S03 explicit-reload choice needs either a richer notification contract or an editor-local UI strip.
3. **Static title wiring:** the VM already has a `Title` property, but the view does not bind it. Easy to miss and then wonder why the unsaved indicator never shows.
4. **Missing-file/part-node behavior:** sidebar items are currently always renderable/selectable. S03 must prevent opening missing chapter nodes and probably part nodes.
5. **Watcher scope:** `ManuscriptService` already owns a `Book.txt` watcher. Adding active-chapter watching there could overgrow its responsibility; an editor-scoped watcher is probably cleaner.
6. **Build verification limitation:** dotnet build/test from `gsd_exec` remains blocked by the known WSL/.NET sandbox issue (MEM012). Final compile/test evidence must come from task-session Windows terminal runs or previously produced task evidence.

## Natural Seams for Planning

### Seam 1 — Core save/path infrastructure
Likely files:
- `src/Hymnal.Core/Models/ManuscriptModel.cs`
- `src/Hymnal.Core/Models/ChapterNode.cs` (only if more path metadata is needed here)
- `src/Hymnal.Core/Interfaces/IMetadataStore.cs` (new)
- `src/Hymnal.Core/Infrastructure/MetadataStore.cs` or `src/Hymnal.Core/Stores/MetadataStore.cs` (new; choose one convention)
- `tests/Hymnal.Core.Tests/Infrastructure/MetadataStoreTests.cs` (new)

Goal: preserve manuscript root information and provide atomic text writes usable by chapter saves now and notes later.

### Seam 2 — Editor/session view-model state
Likely files:
- `src/Hymnal/ViewModels/EditorViewModel.cs` (new)
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/App.axaml.cs`

Goal: selected chapter/open/save/switch lifecycle, last-edited chapter persistence, title updates, and DI wiring.

### Seam 3 — UI composition and commands
Likely files:
- `src/Hymnal/Views/EditorView.axaml` + `.cs` (new)
- `src/Hymnal/Views/MarkuaHighlighting.xshd` (new)
- `src/Hymnal/Views/MainWindow.axaml`
- `src/Hymnal/Views/SidebarView.axaml`

Goal: replace the placeholder editor pane, wire Save affordances (Ctrl+S, menu, button), bind title, and ensure missing/part nodes do not enter edit flow.

### Seam 4 — External change / banner UX
Likely files:
- `src/Hymnal/ViewModels/EditorViewModel.cs`
- `src/Hymnal/Infrastructure/NotificationService.cs` and/or `src/Hymnal/Core/Interfaces/INotificationService.cs`
- `src/Hymnal/Views/MainWindow.axaml.cs` / `src/Hymnal/Views/EditorView.axaml`

Goal: clean-buffer auto-reload, dirty-buffer explicit choice, and persistent save failure surfacing.

## First Proof

Prove the end-to-end chapter lifecycle before polishing syntax rules:

1. Select a real chapter node from the sidebar.
2. Resolve the correct on-disk file path regardless of whether `Book.txt` is at workspace root or in `manuscript/`.
3. Load the file into an `AvaloniaEdit` editor surface.
4. Modify text, show dirty state in the title, and save via temp-write-then-rename.
5. Attempt a chapter switch and verify save-before-switch semantics.

Once that works, syntax-color refinement and external-change conflict UX are incremental rather than foundational.

## Verification

### Core/unit verification
Run in a Windows terminal outside `gsd_exec` because of MEM012:

- `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter "FullyQualifiedName~MetadataStoreTests"`
- `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter "FullyQualifiedName~AppSettingsStoreTests|FullyQualifiedName~ManuscriptServiceTests"`

Recommended new unit assertions:
- atomic overwrite preserves the final file and leaves no temp file behind
- simulated save failure preserves original content
- last edited chapter round-trips through `AppSettingsStore`
- workspace/manuscript root resolution works for both supported `Book.txt` locations

### Structural/readback verification
- `rg -n 'Title="\{Binding Title\}"|InputGesture="Ctrl\+S"|SaveCommand|EditorView|MarkuaHighlighting\.xshd' src/Hymnal`
- Confirm `MainWindow.axaml` no longer contains the placeholder editor border.
- Confirm sidebar items for missing files / part nodes do not route into editor open.

### Manual smoke verification
- Launch app, restore prior workspace, and confirm last edited chapter opens.
- Edit text, verify title changes to a bullet-prefixed filename, press Ctrl+S, confirm file contents changed on disk.
- Make external file change while clean → editor reloads.
- Make external file change while dirty → explicit choice is required; unsaved buffer is not silently discarded.

## Skill Discovery

No directly relevant installed skill in the current inventory targets Avalonia, ReactiveUI, DynamicData, or AvaloniaEdit specifically. For this slice, local project architecture notes plus the existing S02 patterns are more valuable than generic frontend/web-oriented skills.

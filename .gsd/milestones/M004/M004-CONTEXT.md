# M004: Corkboard, Supplemental Docs, and Git Panel

**Gathered:** 2026-06-01
**Status:** Ready for planning

## Project Description

Hymnal is a cross-platform desktop manuscript editor for authors using the Book.txt / Markua workflow. M004 completes Early V1 by adding three features that close the author's full writing loop: a corkboard view for at-a-glance chapter status, supplemental document management in the sidebar, and persistent Git commit/push controls in the toolbar.

## Why This Milestone

M004 is the final Early V1 milestone. After it ships, the author can write, track chapter status, review the corkboard, and commit their work entirely within Hymnal — no terminal, no file manager, no switching context. Each of the three features was deliberately deferred from earlier milestones until the core write/manage loop (M001–M003) was stable.

## User-Visible Outcome

### When this milestone is complete, the user can:

- Switch to the "Plan" nav mode and see every chapter as a card showing its title, status badge, word count, word-count % toward target (if set), and current-phase start/end dates; click any card to open that chapter in the editor
- Open the sidebar "DOCS" section, create a new file or subfolder in `.hymnal-data/docs/`, and edit it in the same editor used for chapters — opening a doc file automatically switches to Write mode
- See a persistent Git status chip (branch name + uncommitted change count) in the toolbar; click "Commit…" to stage all, enter a commit message (pre-filled with a timestamp default), and push in a single dialog — the entire Git area is hidden if no Git binary or repo is found

### Entry point / environment

- Entry point: `dotnet run --project src/Hymnal/Hymnal.csproj` (or installed binary)
- Environment: Local desktop, Windows 10+ (Linux stretch goal)
- Live dependencies involved: System Git binary (optional — feature hidden if absent), local file system

## Completion Class

- Contract complete means: unit tests cover `CardViewModel` data projection (word count, status, phase dates), `ProcessGitService` operations behind a fake process runner, and `EditorViewModel.OpenArbitraryFileAsync` dirty-state transitions
- Integration complete means: Corkboard cards reflect live `ChapterViewModel` data; supplemental doc saves round-trip through `EditorViewModel` → `IMetadataStore`; Git commit workflow runs against a real local repo (manual smoke test)
- Operational complete means: FileSystemWatcher for Git change count debounces correctly and does not fire excessive processes during Hymnal's own atomic writes; app launches cleanly with no repo detected (Git UI hidden)

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- Open a workspace with a Git repo, navigate to Plan mode, verify cards show accurate status/word-count for a known chapter, click a card, confirm the editor opens that chapter
- Create a new file in the DOCS sidebar section, type content, save, close and reopen the workspace, verify the file is still present and its content is intact
- With uncommitted changes in the workspace, click "Commit…", confirm the pre-filled message is present, click "Commit & Push", and verify the commit appears in `git log` on the real repo

## Architectural Decisions

### ShellMode.Plan = Corkboard (not a new enum value)

**Decision:** Reuse the existing `ShellMode.Plan` enum value for the Corkboard view; no new enum values are added in M004.

**Rationale:** The `ShellMode` enum already reserved `Plan` for exactly this purpose. Adding a new `Corkboard` value would grow the enum without benefit and would require updating the converter, nav bar, and MainWindow AXAML. The nav button label is "Plan" (user-facing) regardless of the enum value name.

**Alternatives Considered:**
- Add `ShellMode.Corkboard` — rejected; unnecessary enum growth, reserved values exist
- Add both `Corkboard` and `Git` modes — rejected; Git is toolbar-level, not a center-panel mode

---

### Git is a persistent toolbar widget, not a ShellMode

**Decision:** Git controls (branch chip, change count, Commit… button) live at the right edge of the top toolbar and are visible in all shell modes. There is no `GitPanelView` center-panel mode.

**Rationale:** The author directed this explicitly: "Commit and Push buttons on the toolbar that apply to ALL modes." This avoids forcing the user to navigate to a separate view just to commit, and keeps the Git surface minimal — a single dialog triggered by one button covers the full stage-all → commit → push workflow.

**Alternatives Considered:**
- `ShellMode.Edit` = Git Panel (center-panel view) — rejected per user direction
- Git as a right-panel tab alongside Notes — rejected per user direction
- Three separate Stage / Commit / Push toolbar buttons — rejected in favor of the single combined Commit… dialog

---

### Supplemental docs use EditorViewModel.OpenArbitraryFileAsync

**Decision:** Opening a supplemental doc calls a new `OpenArbitraryFileAsync(string absolutePath)` method on the existing singleton `EditorViewModel`. This sets `_activeNode = null`, loads the file, and sets `ActiveFilePath`. The shell automatically activates `ShellMode.Write` so the editor is visible.

**Rationale:** `EditorViewModel` already tracks `ActiveFilePath` independently of the chapter node. The save, watcher, and dirty-state logic all operate on `ActiveFilePath` — they work identically for supplemental docs. A separate `DocEditorViewModel` would duplicate all of that code for no gain.

**Alternatives Considered:**
- Separate `SupplementalEditorViewModel` — rejected; code duplication, no meaningful isolation benefit
- Direct `File.*` calls from a new sidebar ViewModel — rejected; violates the service/interface layer rule

---

### Git change count uses a debounced FileSystemWatcher

**Decision:** `GitPanelViewModel` attaches a `FileSystemWatcher` to the workspace root and debounces `Changed` events (2-second throttle) before spawning `git status --porcelain` to update the change count. Count also refreshes after each Git operation completes.

**Rationale:** The user explicitly chose the reactive file-watcher path for its live feedback. The debounce window (2 s) prevents excessive process spawning during Hymnal's own atomic writes (write-temp → rename produces two events).

**Alternatives Considered:**
- On-user-action refresh only — rejected; user preferred reactive updates

## Error Handling Strategy

- **Git binary not found or no repo detected:** entire Git toolbar group (`IsVisible = false`); no error shown — the feature simply doesn't appear
- **Git operation failure:** in-app notification banner (using `INotificationService`) with the raw stderr included, so the author can diagnose the issue without opening a terminal
- **Supplemental doc open/save failure:** existing `Result<T>` propagation from `IMetadataStore`; notification banner on failure
- **FileSystemWatcher exceptions:** caught and logged; change count shows last known value; no crash
- **Commit with no changes:** "Commit…" button disabled (count = 0); no dialog shown

## Risks and Unknowns

- **EditorViewModel dirty-state with `_activeNode = null`** — the `_isSwitching` guard and chapter-switch flow must be audited to ensure they handle `_activeNode == null` gracefully; a supplemental doc open mid-chapter-switch could race
- **Git binary detection on Windows vs. Linux** — PATH lookup (`where git` / `which git`) and WSL boundary; the Windows build must find `git.exe` via the Windows PATH, not through the WSL mount
- **FileSystemWatcher firing on `.hymnal-data/` atomic writes** — Hymnal's own saves (write-temp → rename) will trigger the git-count watcher; debounce mitigates this but should be verified not to produce stale counts
- **Corkboard performance with large manuscripts (100+ chapters)** — `WrapPanel` with many cards in a `ScrollViewer`; no virtualization by default; if scrolling stutters, a `VirtualizingStackPanel` or `VirtualizingWrapPanel` may be needed

## Existing Codebase / Prior Art

- `src/Hymnal/ViewModels/EditorViewModel.cs` — handles `ActiveFilePath`, dirty state, atomic save, file watcher; `OpenArbitraryFileAsync` extends this
- `src/Hymnal/ViewModels/ChapterViewModel.cs` — has `WordCount`, `Status`, `PhaseData`, `Target`; `CardViewModel` will read from these
- `src/Hymnal/ViewModels/MainWindowViewModel.cs` — owns `ShellMode ActiveMode`; needs `IsCorkboardVisible` derived property and `SelectPlanCommand`
- `src/Hymnal/ViewModels/ShellMode.cs` — `Plan` value already present; no changes needed to the enum
- `src/Hymnal/Views/SidebarView.axaml` — existing chapter tree; DOCS section added below it
- `src/Hymnal/Views/MainWindow.axaml` — existing nav `ToggleButton` strip; "Plan" button wired up; Git toolbar widget added to the right edge of the top bar
- `src/Hymnal.Core/Services/WordCountService.cs`, `PhaseDataService.cs`, `TargetsService.cs` — data sources for corkboard cards
- `src/Hymnal.Core/Models/ChapterNode.cs` — `NodeKind { Part, Chapter }`; will need `SupplementalDoc` and `SupplementalFolder` kinds (or a parallel doc-tree model)
- `src/Hymnal/Views/GanttCanvas.cs` — prior art for a custom canvas renderer; Corkboard uses a simpler `WrapPanel` layout, not a canvas
- `src/Hymnal.Core/Interfaces/INotificationService.cs` — used for Git error banners

## Relevant Requirements

- R006 (Corkboard Early V1) — primary; this milestone delivers the read-only card grid with status, word count, and phase dates
- R007 (Supplemental Docs) — primary; sidebar DOCS section + editor integration
- R008 (Git Panel) — primary; toolbar-level commit workflow

## Scope

### In Scope

- `CorkboardView` + `CorkboardViewModel` + `CardViewModel` (data projected from existing `ChapterViewModel`s)
- Part nodes rendered as labeled section dividers in the corkboard (not as cards)
- `SidebarView` DOCS section showing `.hymnal-data/docs/` tree; `FileSystemWatcher` on that folder
- Context menu on DOCS section: "New File…" and "New Folder…"
- `EditorViewModel.OpenArbitraryFileAsync(string absolutePath)` + `ShellMode.Write` auto-activation
- Git toolbar widget: branch chip, change count badge, "Commit…" button
- Inline commit dialog: pre-filled message, "Commit & Push" / "Commit only" / Cancel
- `ProcessGitService` in `Hymnal.Core` with `GetStatusAsync`, `StageAllAsync`, `CommitAsync`, `PushAsync`, `GetCurrentBranchAsync`
- `IGitService` interface in `Hymnal.Core.Interfaces`
- `GitOperationResult { bool Success, string Output, string Error }`
- Git binary detection; feature hidden if not found or no repo detected
- Git error notification banners (raw stderr surfaced via `INotificationService`)
- FileSystemWatcher for git change count (debounced 2 s, workspace root)
- "Plan" nav button wired to `ShellMode.Plan` / `CorkboardView`
- Unit tests: `CardViewModel`, `ProcessGitService`, `EditorViewModel` arbitrary-file open path

### Out of Scope / Non-Goals

- Drag-to-reorder on the Corkboard (deferred to M005)
- Individual file staging (stage-all only)
- Git history / log view
- Pull / fetch / branch management
- Markua syntax highlighting (separate feature track)
- AI editorial features
- Windows Credential Manager / libsecret integration for Git auth (deferred; assumes push works with existing credential helper / SSH key)

## Technical Constraints

- Avalonia 12 / .NET 10 / ReactiveUI + DynamicData — all existing dependencies
- No new NuGet packages for Git (use `System.Diagnostics.Process` to invoke system Git binary)
- Supplemental docs must use `IMetadataStore.WriteTextAtomicAsync()` for saves — no raw `File.WriteAllText` in ViewModels
- `ProcessGitService` must be testable behind `IGitService`; unit tests use a fake implementation
- FileSystemWatcher in `GitPanelViewModel` must be disposed via `DisposeWith(Disposables)` on deactivation

## Integration Points

- System Git binary — invoked via `System.Diagnostics.Process`; detected at workspace open via PATH lookup
- `IMetadataStore` — supplemental doc saves route through the same atomic-write abstraction used by chapters and notes
- `INotificationService` — Git error banners use the same notification surface as chapter load errors
- `ManuscriptModel` (DynamicData `SourceCache`) — `CorkboardViewModel` subscribes to the same observable cache as `WorkspaceViewModel`; no duplicate data fetching

## Testing Requirements

- **Unit tests (Hymnal.Core.Tests):**
  - `CardViewModel` correctly projects `ChapterViewModel` data (status, word count, target %, phase dates; null-safe when target not set)
  - `ProcessGitService` operations against a `FakeGitProcess` stub: success path, failure path (non-zero exit), stderr propagation
  - `EditorViewModel.OpenArbitraryFileAsync`: dirty-prompt triggered when dirty, `_activeNode` set to null, `ActiveFilePath` updated, watcher started
- **Manual smoke tests (acceptance):**
  - Corkboard renders cards for a known sample manuscript; clicking a card opens that chapter
  - New supplemental doc created, edited, saved, visible after workspace re-open
  - Commit…  dialog completes successfully against a real local Git repo; change count drops to 0 after commit
  - Git toolbar hidden when workspace has no `.git` directory

## Acceptance Criteria

**Corkboard:**
- Plan mode shows one card per chapter and one divider per Part, in manuscript order
- Card shows: title, status badge (synthwave palette, consistent with sidebar), word count, % toward target (hidden if no target set), phase start/end dates (hidden if no phase data)
- Click any chapter card → shell activates Write mode and opens that chapter in the editor
- Corkboard renders correctly for a manuscript with 0 chapters (empty state text)

**Supplemental Docs:**
- DOCS section appears in sidebar when workspace is open (even if `.hymnal-data/docs/` doesn't exist yet — created on first file creation)
- "New File…" and "New Folder…" context menu items work; new file opens immediately in editor (Write mode activated)
- Supplemental doc saves are atomic; file content survives a forced app restart
- Switching from a supplemental doc to a chapter (via sidebar click) prompts to save if dirty

**Git:**
- Git toolbar group is visible only when a Git binary is found AND the workspace (or a parent directory) contains a `.git` directory
- Branch name and change count display correctly for a real repo
- Change count updates within 3 seconds of a file save in the workspace
- "Commit…" button is disabled when change count = 0
- Commit dialog pre-fills message as `"Hymnal: save progress {ISO-8601}"`; user can edit before confirming
- "Commit & Push" completes successfully and change count drops to 0
- "Commit only" commits without pushing; change count drops to 0
- Git operation failure (e.g., no remote configured) shows a notification banner with raw stderr
- Entire Git area remains hidden when no Git binary found; no exception thrown

## Open Questions

- **Corkboard virtualization threshold** — if scrolling a 100+ chapter manuscript proves sluggish, switch from `WrapPanel` to `VirtualizingWrapPanel` (or a `UniformGrid`-based approach); worth prototyping in the first Corkboard slice before committing to a layout container
- **Git auth for push** — the implementation assumes the system Git credential helper or SSH key handles auth silently; if push fails with an auth prompt (which can't be shown in a process subprocess), the error banner will surface the stderr, but there is no credential-entry UX in M004
- **DOCS section empty-state UX** — when `.hymnal-data/docs/` is empty (no files yet), the DOCS section should show a subtle "No docs yet — right-click to create" hint rather than being blank

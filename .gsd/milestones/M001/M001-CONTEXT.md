# M001: Scaffold, Workspace, and Editor

**Gathered:** 2026-05-28
**Status:** Ready for planning

## Project Description

Hymnal is a cross-platform .NET 10 desktop writing application for a solo Markua/LeanPub author (Matt Eland, writing *A Choir of Minds*). It opens any Markua manuscript folder, parses `Book.txt` as the authoritative chapter manifest, and provides a focused writing environment alongside project management and Git integration. All Hymnal metadata lives in `.hymnal-data/` at the workspace root.

M001 delivers the runnable foundation: the solution scaffold, the synthwave dark theme, workspace opening and `Book.txt` parsing, the sidebar chapter/part tree, the Markua editor with save/load and syntax highlighting, the chapter notes panel, atomic write infrastructure, and the `Hymnal.Core` / `Hymnal` compile-enforced boundary. By the end of M001, the author can open their manuscript folder and write prose in a focused Markua-aware editor.

## Why This Milestone

M001 must exist before everything else. Without the scaffold, workspace model, and editor, no subsequent milestone can build on working code. The "write files early" priority means M001 must ship a genuinely usable writing experience — not just a scaffold with stubs.

## User-Visible Outcome

### When this milestone is complete, the user can:

- Open `C:/Dev/EliAndGraceMakeAGame` (or any Markua manuscript folder) and see the Chapter/Part tree in the sidebar
- Click a chapter in the sidebar and write prose in the editor with Markua syntax highlighting active
- Save with Ctrl+S and have the file written atomically to disk
- Toggle the Notes panel to read/write per-chapter notes without leaving the editor
- See the synthwave dark theme (purple primary, JetBrains Mono in editor, Inter for UI) across the whole app

### Entry point / environment

- Entry point: `Hymnal` desktop application (double-click or `dotnet run`)
- Environment: local Windows dev machine; Linux compatibility verified by CI matrix
- Live dependencies involved: filesystem only (no network, no Git, no AI in M001)

## Completion Class

- Contract complete means: solution builds (`dotnet build`), tests in `Hymnal.Core.Tests` pass, `Book.txt` is parsed correctly for sample fixtures, editor saves atomically
- Integration complete means: sidebar tree, editor, and notes panel are wired through the real DI container on real manuscript files
- Operational complete means: app cold-starts in under 5s; `Book.txt` with 100 chapters parses in under 2s

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- Open `C:/Dev/EliAndGraceMakeAGame` (a real Markua manuscript): sidebar renders the Part/Chapter tree in `Book.txt` order
- Click a chapter, write a word, Ctrl+S: the chapter `.md` file is updated on disk and word count is not yet tracked (M002)
- Open the Notes panel: a notes file is created/read from `.hymnal-data/notes/` and editable inline
- All `Hymnal.Core.Tests` pass with no failures
- CI matrix build passes for win-x64 and linux-x64

## Architectural Decisions

### Two-project solution with compile-enforced layer boundary

**Decision:** Split into `src/Hymnal/` (Avalonia UI layer) and `src/Hymnal.Core/` (pure .NET 10, zero Avalonia reference)

**Rationale:** Any agent that accidentally imports an Avalonia type into a service gets a compile error, not a code-review note. This is the strongest enforcement mechanism available.

**Alternatives Considered:**
- Single project with naming conventions only — relies on convention, not enforcement; rejected

---

### Avalonia UI 12.0 + ReactiveUI MVVM

**Decision:** Avalonia UI 12.0 with Skia renderer; ReactiveUI for MVVM; DynamicData for reactive collections

**Rationale:** Desktop-first, MIT licensed, Linux first-class, Skia gives pixel-level control for custom Gantt/Corkboard rendering. ReactiveUI + DynamicData are designed as a pair with first-class integration. 30k+ stars, production use at JetBrains and Devolutions.

**Alternatives Considered:**
- Uno Platform — mobile/web breadth unused for V1; commercial tooling tier creates friction for solo MIT project; rejected

---

### AvaloniaEdit for the editor component

**Decision:** `TextEditor` from the `AvaloniaEdit` NuGet package with a custom XSHD syntax definition for Markua 0.30

**Rationale:** Purpose-built code editor control for Avalonia; supports custom `IHighlightingDefinition`, `AbstractMargin` for inline issue indicators (needed in M005), and integrates cleanly with the ReactiveUI binding model.

**Alternatives Considered:**
- Plain `TextBox` — no syntax highlighting extension point; rejected
- Custom text editor — prohibitive scope; rejected

---

### Metadata storage: JSON files in .hymnal-data/

**Decision:** All Hymnal metadata stored as JSON in `.hymnal-data/` at the workspace root; `System.Text.Json`; every file carries `"schemaVersion": 1`; camelCase properties; atomic writes (write-temp-then-rename)

**Rationale:** Human-readable, Git-diffable, consistent with Hymnal's plaintext/Git philosophy. LeanQuill (a real parallel implementation) confirmed schema versioning is non-negotiable — it underwent a painful migration at `schema_version: "2"`.

**Alternatives Considered:**
- SQLite — harder to inspect/script outside the app; overkill for single-author scope; deferred post-V1 if needed
- XML — verbose; less scriptable; rejected

---

### Result<T> for all async service returns

**Decision:** `readonly record struct Result<T>` in `Hymnal.Core/Common/`; all service methods return `Task<Result<T>>`; ViewModels surface errors via `INotificationService`

**Rationale:** Errors propagate as values, not exceptions; avoids silent swallowing; forces explicit error handling at the ViewModel boundary.

**Alternatives Considered:**
- Exceptions — easy to swallow accidentally; rejected

## Error Handling Strategy

- All service methods return `Result<T>`; failures are surfaced as `INotificationService.ShowError()` banners (red, persist until dismissed)
- File writes: atomic (write-temp-then-rename); a crash mid-write leaves the original file intact
- Missing `Book.txt` reference: entry shown in sidebar with ⚠ icon; does not prevent other chapters from loading
- External `Book.txt` change: `FileSystemWatcher` triggers an info banner with Reload/Dismiss options
- `ReactiveCommand.ThrownExceptions` subscribed in every ViewModel as a safety net for unexpected throws
- No modal dialogs for non-destructive errors; no silent exception swallowing; no raw exception type names shown to users

## Risks and Unknowns

- Avalonia `TextEditor` (AvaloniaEdit) XSHD definition authoring — the token grammar for Markua 0.30 needs to be hand-authored; no existing Markua XSHD exists. Low risk: XSHD is declarative XML; a working definition can be iteratively refined.
- `FileSystemWatcher` on Windows vs Linux — behavior differences around rename events and buffering are known gotchas. Mitigate: use `NotifyFilter.FileName | NotifyFilter.LastWrite`; test on both platforms in CI.
- Avalonia 12.0 + .NET 10 compatibility — Avalonia 12 targets .NET 8+ minimum; .NET 10 should be supported. Verify at scaffold time.

## Existing Codebase / Prior Art

- `C:/Dev/EliAndGraceMakeAGame/.leanquill/` — real working Markua manuscript tool; same problem space. Key patterns: derived structural index, `schema_version`, AI write isolation, editorial persona config. Used as architectural reference throughout.
- `_bmad-output/planning-artifacts/architecture.md` — complete architecture spec; the directory tree, naming conventions, DynamicData patterns, and anti-patterns are all taken from this document.
- `docs/research/leanpub_markua_manuscript_research.md` — Markua 0.30 token reference; input for the XSHD definition.

## Relevant Requirements

- R001 — Workspace and Manuscript Model (primary owner M001/S02)
- R002 — Markua Editor (primary owner M001/S03)
- R009 — Chapter Notes Panel (primary owner M001/S04)
- R011 — Synthwave Theme and Focused Layout (primary owner M001/S01)
- R012 — Atomic Writes and Data Safety (primary owner M001/S03)
- R014 — Platform, Performance, and Portability (primary owner M001/S01)

## Scope

### In Scope

- Solution scaffold: `Hymnal.sln`, `src/Hymnal/`, `src/Hymnal.Core/`, `tests/Hymnal.Core.Tests/`, `.github/workflows/release.yml`
- Synthwave dark theme: `SynthwaveTheme.axaml`, `ControlStyles.axaml`, named brush resources, Inter + JetBrains Mono fonts
- DI container wiring in `App.axaml.cs`; `ICredentialStore` platform-conditional registration (stub implementations for M001 — no AI yet)
- `ManuscriptService`: `Book.txt` parse → `ManuscriptModel`; `FileSystemWatcher` on `Book.txt`; Part recognition from folder structure; missing-file handling
- `SidebarView` + `WorkspaceViewModel`: Chapter/Part tree in `Book.txt` order; ⚠ indicator for missing files; context menu to remove missing entries
- `EditorView` + `EditorViewModel`: AvaloniaEdit `TextEditor`, Markua XSHD syntax highlighting, Ctrl+S save, unsaved-change tracking
- `NotesView` + `NotesViewModel`: notes panel accessible from editor; reads/writes `.hymnal-data/notes/{filename}.md`
- `MetadataStore`: atomic write infrastructure (write-temp-then-rename) for `.hymnal-data/`
- `Result<T>` + `Unit` in `Hymnal.Core/Common/`
- `INotificationService` + `NotificationService` banner implementation (error/info/success)
- `AppSettingsStore`: last-opened workspace path, window state; stored at `%APPDATA%\Hymnal\settings.json` / `~/.config/hymnal/settings.json`
- xUnit test project: `ManuscriptServiceTests`, `WordCountServiceTests` (stub — word count not yet live), `MetadataStoreTests`, `BookTxtParserTests`; sample manuscript fixtures

### Out of Scope / Non-Goals

- Chapter Status, word count, targets (M002)
- Gantt view (M003)
- Corkboard, supplemental docs, Git panel (M004)
- AI features (M005)
- Markua inline validation (FR-9) — deferred to M002 when the editor is stable
- Settings UI panel — `AppSettingsStore` is wired, but no settings screen in M001

## Technical Constraints

- `Hymnal.Core.csproj` must have zero Avalonia package references — compile-enforced boundary
- All `.hymnal-data/` writes: write-temp-then-rename (atomic); never `File.WriteAllText(path, ...)` directly in store implementations
- JSON: camelCase properties, `"schemaVersion": 1` in every file, ISO 8601 dates, `JsonIgnoreCondition.WhenWritingNull`
- ViewModel naming: `XxxViewModel` / `XxxView` pair (ViewLocator convention)
- `ReactiveCommand.ThrownExceptions` subscribed in every ViewModel

## Integration Points

- Filesystem — `Book.txt` and chapter `.md` files are read; `.hymnal-data/notes/` is written
- `AppSettingsStore` — persists last workspace path; read on launch for auto-restore (FR-1)
- `FileSystemWatcher` — watches `Book.txt` for external changes (FR-6)

## Testing Requirements

- xUnit in `Hymnal.Core.Tests`; NSubstitute for mocks
- `BookTxtParserTests`: parse simple-book and multi-part-book fixtures; assert correct Chapter/Part tree structure
- `MetadataStoreTests`: verify atomic write (temp file created then renamed; original preserved on simulated failure)
- `ManuscriptServiceTests`: missing-file entry marked correctly; external change detection fires
- No UI tests in M001 — Avalonia UI is not testable in `Hymnal.Core.Tests`
- CI matrix must pass on win-x64 and linux-x64

## Acceptance Criteria

- **S01 (Scaffold + Theme):** `dotnet build` succeeds; synthwave palette resources load; app launches to a windowed shell with dark background and purple accents
- **S02 (Workspace):** Opening the `EliAndGraceMakeAGame` folder renders its Chapter/Part tree in sidebar in `Book.txt` order; last-opened workspace auto-restores on relaunch
- **S03 (Editor):** Clicking a chapter opens it in AvaloniaEdit with Markua syntax highlighting; Ctrl+S saves atomically; unsaved-change indicator visible in title
- **S04 (Notes):** Notes panel toggles open; writing a note persists to `.hymnal-data/notes/{filename}.md`; reopening the chapter reloads the note

## Open Questions

- Avalonia 12.0 + .NET 10 compatibility: verify at scaffold time with `dotnet new avalonia.mvvm --framework net10.0`
- XSHD token color assignments for Markua constructs: reference `docs/research/leanpub_markua_manuscript_research.md` and the UX design doc color palette during S03

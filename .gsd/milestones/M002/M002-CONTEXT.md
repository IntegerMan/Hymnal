# M002: Status Tracking and Word Count

**Gathered:** 2026-05-30
**Status:** Ready for planning

## Project Description

Hymnal is a cross-platform .NET 10 desktop writing application for solo Markua/LeanPub author Matt Eland (writing *A Choir of Minds*). M002 builds directly on the working editor from M001 to add the project management layer that replaces the author's spreadsheet: chapter status lifecycle tracking, phase date capture, live word count with Part/book rollup, optional word count targets with proximity indicators, and advisory inline Markua validation.

## Why This Milestone

Status tracking must start accumulating historical data while the author is actively writing. Once early drafting phases pass unrecorded, the timeline is gone. Word count is the author's primary day-to-day progress signal. Both must land in M002 — not a later milestone — so the data accumulates from the start.

## User-Visible Outcome

### When this milestone is complete, the user can:

- See a colored status dot next to every chapter in the sidebar (Outlining / Drafting / Editing / Polishing / Reviewing / Done)
- Open the Chapter Info pane (F3) to set the chapter's status, edit phase start/end dates, see live word count, and set a word count target — all without leaving the editor
- Watch the word count update in the editor as they type; see Part and book totals update on save
- See an advisory margin indicator in the editor when a known Markua issue pattern is detected (e.g. blank line before `{sample: true}` heading)
- Know their tracking data survives chapter renames, reorders, and Book.txt inclusions/exclusions

### Entry point / environment

- Entry point: Hymnal desktop application (double-click or `dotnet run`)
- Environment: local Windows dev machine; Linux compatibility verified by CI matrix
- Live dependencies involved: filesystem only (`.hymnal-data/` JSON files; no network)

## Completion Class

- Contract complete means: `dotnet build` succeeds; all `Hymnal.Core.Tests` pass; `ChapterRegistryService` tests cover UUID assignment, path update on rename, orphan flagging; `WordCountService` tests cover tokenisation with Markua directive exclusion; `PhaseDataService` round-trips `phases.json`
- Integration complete means: status changes persist across app restarts; word count rollup is correct for a real manuscript; Chapter Info pane wired through real DI container
- Operational complete means: word count updates within 500ms of keystroke on a 10,000-word chapter; workspace load with 100 chapters (all requiring word count recalculation) completes within the existing < 5s cold-start budget

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- Change a chapter's status from `Outlining` to `Drafting`: sidebar dot updates immediately; reopening the app restores the status correctly; the phase start date is pre-filled with today's date
- Rename a chapter's `.md` file and update `Book.txt`: relaunch the app; the status and word count for the renamed chapter are intact
- Open a chapter, write 100 words, Ctrl+S: word count updates in the Chapter Info pane; Part and book totals in the sidebar reflect the new count; `wordcount-history.json` contains a new entry for today
- Set a word count target of 4,000 on a chapter at 2,000 words: proximity bar in sidebar shows ~50% fill

## Architectural Decisions

### Stable chapter identity via UUID registry

**Decision:** All tracking data (`phases.json`, `targets.json`, `wordcount-history.json`) is keyed by a stable UUID, not by file path. A `chapter-registry.json` in `.hymnal-data/` maps `currentPath → UUID`. The registry is reconciled at every workspace load.

**Rationale:** Chapter file paths change over time (renames, reorders, Book.txt include/exclude). Keying by path would orphan tracking data on any rename. UUID keys decouple identity from location. The LeanQuill parallel implementation confirmed this pattern is non-negotiable — it uses UUID-keyed `outline-index.json` for exactly this reason.

**Behaviour on change:**
- Rename: registry updates `currentPath`; UUID-keyed data in all tracking files survives unchanged
- Remove from `Book.txt`: entry flagged `orphaned: true` in registry; data preserved for potential re-link
- Re-include: if path matches a registered entry, UUID is restored from registry

**Alternatives Considered:**
- Relative path as key — orphans data on rename; rejected
- Chapter title as key — title edits orphan data; collision risk for same-title chapters; rejected
- Path key with rename heuristic — heuristic can be wrong; adds complex reconciliation logic; rejected

---

### Word count for closed chapters: persist-on-save + lazy recalculate

**Decision:** Word count is persisted to `phases.json` on every `Ctrl+S`. At workspace load, any chapter with no saved count is recalculated from its `.md` file in a background task (does not block cold start). Open chapter count is always live from `EditorViewModel` (debounced 300ms).

**Rationale:** Eager full-load on workspace open conflicts with the < 5s cold-start target. Lazy background recalculation is a one-time cost per chapter until counts are cached, and is invisible to the author.

**Alternatives Considered:**
- Eager load all chapters on open — violates cold-start target for large manuscripts; rejected
- Live count for open chapter only, no rollup — defers core feature; rejected

---

### Chapter Info pane: toggleable right-side panel

**Decision:** A toggleable Chapter Info pane on the right side rail (F3 keyboard shortcut), using the same resizable-column pattern as the Notes pane. Shows: status picker, phase start/end dates (editable), live word count, and target for the open chapter. Phase-date pre-fill toggle lives here and inline in any status-change interaction.

**Rationale:** The author needs status and date editing without leaving the editor context. A toggleable panel mirrors the established Notes pane pattern, keeps the API surface consistent, and avoids a separate settings screen for the phase-date preference.

**Alternatives Considered:**
- Always-visible narrow strip — less real estate, always present; but dates not directly visible; rejected in favour of the richer pane
- Status popover only — sufficient for status change but gives no overview of phase dates or word count at a glance; rejected

---

### Inline Markua validation: standalone ValidationMargin, no M005 coupling

**Decision:** FR-9 advisory validation (blank line before `{sample: true}` heading, unrecognised attribute key, etc.) implemented as a standalone `ValidationMargin` class (`AbstractMargin` / `IBackgroundRenderer`) in M002. No coupling to M005's `IssueMargin` design.

**Rationale:** Sharing infrastructure now commits M005's interface shape prematurely. A standalone margin can be merged into `IssueMargin` in M005 when that design is settled, at lower risk than designing it now. M002 ships advisory hints; M005 ships AI issues — the use cases are different enough to justify independent evolution.

**Alternatives Considered:**
- Shared `IssueMargin` stub now — locks in M005 interface early; over-scopes M002; rejected
- Defer FR-9 to M005 — author won't see validation hints until M005; rejected given FR-9 value during active drafting

---

### Word count history for future progress charts

**Decision:** On every `Ctrl+S`, append one entry to `wordcount-history.json` (chapter UUID + date + word count). Duplicates for the same chapter+date are deduplicated (last-write wins). Progress chart visualisation deferred to a future milestone.

**Rationale:** The data must accumulate from the author's first writing sessions in M002 so future milestones have meaningful history to display. Writing and displaying are separate concerns.

## Error Handling Strategy

- All service methods return `Result<T>`; failures surface via `INotificationService.ShowError()` banners — consistent with M001 pattern
- `phases.json` / `targets.json` / `chapter-registry.json` writes: atomic (write-temp-then-rename); crash mid-write leaves original file intact
- Registry reconciliation on load: unknown schema version → surface error banner and abort load, preserving existing data; do not silently migrate or discard
- Orphaned registry entries (chapter removed from `Book.txt`): flagged `orphaned: true`; data preserved silently; no error banner (expected workflow)
- Word count background recalculation: per-chapter failures are silent (chapter contributes 0 to rollup until resolved); not worth surfacing as a banner for a missing count
- `ValidationMargin` errors: swallow silently — advisory hints must never crash the editor

## Risks and Unknowns

- **Reactive rollup pipeline performance** — `CombineLatest` across ~100 `ChapterViewModel` instances may have non-trivial subscription overhead. Mitigate: profile with a 100-chapter fixture; fall back to periodic rollup recalculation if reactive graph is too heavy.
- **ChapterViewModel wrappers** — M001 binds `ChapterNode` records directly in the sidebar. M002 needs per-chapter mutable state (status, word count). Introducing `ChapterViewModel` wrappers is the right call but requires touching the sidebar binding established in M001.
- **ValidationMargin API surface** — `AbstractMargin` in AvaloniaEdit has not been exercised in the codebase yet. Verify the extension point during the first S03 task; fall back to `IBackgroundRenderer` if needed.
- **wordcount-history.json growth** — for an active author, this file could accumulate thousands of entries over months. M002 writes without pruning; a compaction/rollup strategy is a future concern.

## Existing Codebase / Prior Art

- `src/Hymnal/ViewModels/EditorViewModel.cs` — singleton editor; `TextEditor.Document.Text` on `TextChanged` is the source for live word count; `OriginalText` / `Text` diff already tracked
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs` — owns `ManuscriptModel`, `SelectedNode`, chapter switching; M002 adds `ChapterViewModel` wrappers and word count rollup here
- `src/Hymnal.Core/Models/ChapterNode.cs` — `record ChapterNode(...)` — M002 adds `ChapterStatus.cs`, `PhaseData.cs`, `WordCountTarget.cs` as companion models
- `src/Hymnal.Core/Infrastructure/AppSettingsStore.cs` — `SetAsync`/`GetAsync` atomic JSON pattern used by all new stores
- `src/Hymnal.Core/Infrastructure/MetadataStore.cs` — atomic write infrastructure; all new `.hymnal-data/` writes use this pattern
- `src/Hymnal/Views/Editor/NotesView.axaml` — toggleable right-pane pattern to replicate for Chapter Info pane
- `C:/Dev/EliAndGraceMakeAGame/.leanquill/` — UUID-keyed `outline-index.json` and `chapter-order.json`; direct inspiration for `chapter-registry.json`

## Relevant Requirements

- R003 — Chapter Status Lifecycle (primary owner M002); FR-14 status enum, FR-15 phase date pre-fill
- R004 — Live Word Count and Targets (primary owner M002); FR-16 live count, FR-17 rollup, FR-18 targets, FR-19 proximity indicators
- R002 — Markua Editor; FR-9 inline Markua validation (advisory margin) lands here

## Scope

### In Scope

- `ChapterStatus` enum (Outlining, Drafting, Editing, Polishing, Reviewing, Done) in `Hymnal.Core/Models/`
- `PhaseData` model (status, phaseStartDate, phaseEndDate) in `Hymnal.Core/Models/`
- `WordCountTarget` model (single value or min/max range) in `Hymnal.Core/Models/`
- `chapter-registry.json` and `ChapterRegistryService` — UUID assignment, path reconciliation, orphan flagging
- `phases.json` and `PhaseDataService` — per-chapter status + phase dates, keyed by UUID; atomic write
- `targets.json` and `TargetsService` — per-chapter/Part/book word count targets, keyed by UUID; atomic write
- `wordcount-history.json` and `WordCountHistoryService` — append-on-save, date + UUID + count; last-write-wins dedup per chapter+date
- `WordCountService` — whitespace tokenizer excluding Markua directive lines and attribute list lines
- Live word count in `EditorViewModel` (debounced 300ms on `TextChanged`)
- Background word count recalculation for chapters with no saved count at workspace load
- `ChapterViewModel` wrappers around `ChapterNode` in `WorkspaceViewModel` for per-chapter mutable state
- Reactive Part and book word count rollup via DynamicData / `CombineLatest`
- Status dots in `SidebarView` with synthwave status colour palette
- Proximity progress bar in sidebar for chapters with a target
- Toggleable Chapter Info pane (F3): status picker, editable phase dates, live word count, target display; phase-date pre-fill toggle
- Status-change interaction pre-fills today's date into phase start (toggle respects AppSettingsStore preference)
- `ValidationMargin` (`AbstractMargin`) with advisory indicators for known Markua error patterns (FR-9)
- xUnit tests: `ChapterRegistryServiceTests`, `WordCountServiceTests`, `PhaseDataServiceTests`, `TargetsServiceTests`

### Out of Scope / Non-Goals

- Progress chart / writing velocity visualisation — data is recorded, display deferred
- Settings screen / Settings panel UI — phase-date pre-fill toggle lives in Chapter Info pane only
- Gantt phase timeline renderer (M003)
- Corkboard cards (M004)
- AI features (M005)
- Markua validation beyond advisory hints: no blocking errors, no LeanPub preview integration
- `IssueMargin` shared with M005 AI issues — deliberate non-goal in M002

## Technical Constraints

- `Hymnal.Core.csproj` must have zero Avalonia package references — compile-enforced boundary
- All `.hymnal-data/` writes: write-temp-then-rename (atomic); never `File.WriteAllText` directly in store implementations
- JSON: camelCase properties, `"schemaVersion": 1` in every file, ISO 8601 dates, `JsonIgnoreCondition.WhenWritingNull`, enum values as strings (`JsonStringEnumConverter`)
- Chapter identity keys in all tracking files: UUID strings from `chapter-registry.json`, never raw file paths
- `ReactiveCommand.ThrownExceptions` subscribed in every ViewModel
- Word count update latency: ≤ 500ms from keystroke to displayed count

## Integration Points

- `EditorViewModel` → `WordCountService` — live count driven by `TextChanged` with 300ms debounce
- `WorkspaceViewModel` → `ChapterRegistryService` — reconcile registry on workspace load; assign UUIDs to new chapters
- `WorkspaceViewModel` → `PhaseDataService` + `TargetsService` — load phase data and targets on workspace open; save on status/target change
- `WorkspaceViewModel` → `WordCountHistoryService` — append entry on each `Ctrl+S`
- `SidebarView` — consumes `ChapterViewModel.Status` for dot colour; consumes `ChapterViewModel.WordCount` + `ChapterViewModel.Target` for proximity bar
- `ChapterInfoView` (new) — bound to `ChapterInfoViewModel`; wired into main shell's right-side resizable column
- `ValidationMargin` — registered with `TextEditor` in `EditorView.axaml.cs`; reads `Document` on change

## Testing Requirements

- xUnit in `Hymnal.Core.Tests`; NSubstitute for mocks
- `ChapterRegistryServiceTests`: assign UUID on first encounter; preserve UUID on path update; flag orphan on removal from manifest; round-trip `chapter-registry.json`
- `WordCountServiceTests`: basic word count; exclude lines starting with `{` (attribute lists); exclude Markua directive lines (e.g. `{sample: true}`); handle empty/whitespace-only content
- `PhaseDataServiceTests`: round-trip `phases.json`; unknown schema version returns `Result.Fail`
- `TargetsServiceTests`: single value target; min/max range target; missing target returns null gracefully
- No UI tests — AvaloniaEdit and reactive binding verification remains desktop smoke pass only

## Acceptance Criteria

- **S01 (Status + Registry):** Status changes persist across restarts; UUID survives chapter rename; orphaned entry flagged when chapter removed from `Book.txt`; `phases.json` round-trips correctly; all `ChapterRegistryServiceTests` and `PhaseDataServiceTests` pass
- **S02 (Word Count + Targets):** Live word count updates ≤ 500ms; Part and book totals correct; word count history appended on save; proximity bar reflects target; all `WordCountServiceTests` and `TargetsServiceTests` pass
- **S03 (Chapter Info Pane + Validation):** F3 toggles Chapter Info pane; status picker and phase dates editable inline; phase-date pre-fill toggle works; `ValidationMargin` shows advisory dots for at least two known Markua error patterns; no editor crash on any validation path

## Open Questions

- **Word count rollup under ChapterViewModel**: Should `ChapterViewModel` hold a `ReactiveObject` with `WordCount` as a `BehaviorSubject` / `ObservableAsPropertyHelper<int>`, or should `WorkspaceViewModel` maintain a flat `Dictionary<string, int>` updated on save? The reactive graph is cleaner; the dictionary is simpler for the rollup. Lean toward `ChapterViewModel` with `OAPH` to stay consistent with the architecture doc's described pattern — confirm during S02 planning.
- **wordcount-history.json dedup strategy**: Last-write-wins per `(UUID, date)` pair means today's count is always the latest save. Alternatively, record every save and take max for the day. Last-write-wins is simpler and sufficient for daily progress tracking — confirm before S02 implementation.
- **ValidationMargin error patterns in M002 scope**: The PRD references "blank line before sample heading" and "unrecognized attribute key" as the two examples. Confirm the full list of advisory patterns to ship in M002 vs defer during S03 planning.

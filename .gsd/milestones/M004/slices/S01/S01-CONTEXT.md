---
id: S01
milestone: M004
status: ready
---

# S01: Plan Mode Corkboard Cards — Context

<!-- Slice-scoped context. Milestone-only sections (acceptance criteria, completion class,
     milestone sequence) do not belong here — those live in the milestone context. -->

## Goal

Deliver a Plan-mode corkboard that gives the author a compact, full-width manuscript overview with live chapter cards, selectable/openable cards, and pulled-forward structural editing through drag-reorder and context actions.

## Why this Slice

This slice is first because Plan mode is currently reserved but unused, and proving the shell mode, card projection, and manuscript-order editing surface establishes the planning board that later supplemental-doc and Git toolbar work must coexist with. The user also chose to pull the Late V1 Corkboard structural-editing behavior forward into this slice, so S01 now becomes the main proof point for visual manuscript review plus direct restructuring.

## Scope

### In Scope

- Plan mode uses the existing `ShellMode.Plan` value and presents the corkboard as a full-board focus surface: top navigation remains, while the sidebar and right panes collapse like Manage mode.
- Corkboard displays manuscript structure in Book.txt order with Part dividers and one card per chapter.
- Part dividers remain visible even when a Part has no chapters; empty Parts show a subtle empty-Part hint.
- Card layout defaults to a compact scan style: many chapters visible, concise metadata, and quick triage over decorative index-card styling.
- Each card shows chapter title, status, word count, target-progress area, and current phase dates when available.
- Target-progress space is reserved on cards; chapters without a target show “No target” rather than hiding the row.
- Card data should be live while Plan mode is visible, reflecting status, word count, target, and phase-date updates through the existing reactive chapter data source.
- Single-click selects a card; double-click opens that chapter and switches to Write mode.
- Selected card receives a clear accent border/glow; pressing Enter opens the selected chapter.
- Drag-reorder is in scope and may move cards anywhere in the board, including across Part boundaries; Book.txt order is authoritative after the drop.
- Right-click context options are in scope for the full structural set: Open, rename/title edit, new chapter, include/exclude, move to Part if useful, and delete/remove.
- Structural actions use a minimal-prompt safety model: reorder and non-destructive changes apply immediately with clear feedback; destructive delete/remove actions require confirmation.
- Large-manuscript posture starts with a simple wrapping grid/scroll layout and only revisits virtualization if smoke testing around 100+ chapters shows stutter.
- Empty-manuscript state is explicit and useful rather than a blank panel.

### Out of Scope

- Git toolbar/status/commit workflow; this remains S03.
- Supplemental docs sidebar/editor path; this remains S02.
- AI editorial features, Markua syntax highlighting, and Git history/branch management.
- Heavy virtualization or a custom canvas renderer unless the simple corkboard grid demonstrably stutters during S01 verification.
- A staged “Apply Changes” model for structural edits; the chosen UX is immediate application with selective confirmation for destructive operations.
- Broad keyboard grid navigation is not required for S01 beyond selected-card Enter-to-open, unless implementation can add it cheaply without delaying core behavior.

## Constraints

- Reuse `ShellMode.Plan`; do not introduce a new Corkboard enum value.
- The original roadmap treated S01 as read-only R006, but the user explicitly chose to pull full Corkboard structural editing forward from R013/M005; execution planning must reconcile this scope expansion before implementation.
- Structural writes to `Book.txt` must be atomic and must respect the project rule that manuscript files are never modified except through explicit author actions.
- Part nodes are dividers, not selectable chapter cards.
- Card data should project from existing `ChapterViewModel` / manuscript data rather than creating a duplicate manuscript model.
- Visual styling must remain consistent with the existing dark synthwave theme and status palette.
- Plan mode should not regress Write or Manage navigation, existing sidebar behavior, or editor dirty-state behavior.

## Integration Points

### Consumes

- `src/Hymnal/ViewModels/ShellMode.cs` — existing `Plan` mode value to activate the corkboard.
- `src/Hymnal/ViewModels/MainWindowViewModel.cs` — active shell mode, navigation commands, and visibility properties that need Plan-mode wiring.
- `src/Hymnal/Views/MainWindow.axaml` — top navigation and center-panel composition where CorkboardView is shown.
- `src/Hymnal/Views/MainWindow.axaml.cs` — shell chrome width/visibility behavior; Plan should use full-board focus chrome similar to Manage.
- `src/Hymnal/ViewModels/ChapterViewModel.cs` — source for title, status, word count, target/proximity, and phase data displayed on cards.
- `src/Hymnal.Core/Models/ChapterNode.cs` / manuscript order from Book.txt — source of Part/chapter ordering and Part divider placement.
- Existing status/target/phase services — backing data for live card projection.

### Produces

- `src/Hymnal/ViewModels/CorkboardViewModel.cs` — Plan-mode board projection and commands for selecting/opening/restructuring cards.
- `src/Hymnal/ViewModels/CardViewModel.cs` — per-card projection of chapter metadata and display state.
- `src/Hymnal/ViewModels/CorkboardItemViewModel.cs` or equivalent — representation for mixed Part dividers, empty-Part hints, and chapter cards if needed.
- `src/Hymnal/Views/CorkboardView.axaml` — compact wrapping card grid, Part dividers, empty states, selection state, and context menu surface.
- Structural-edit service or ViewModel commands — Book.txt reorder/insert/include/exclude/delete behavior, using atomic write patterns and explicit user actions.
- Tests for card projection and, after scope reconciliation, structural Book.txt edit behavior.

## Open Questions

- How exactly should include/exclude be represented in storage? — Current requirements note this as unresolved for R013; pulling R013 into S01 means execution must decide or ask before implementing include/exclude.
- What does delete/remove mean? — Current thinking: destructive file deletion/removal must require confirmation; execution should clarify whether delete removes the chapter file, removes only the Book.txt entry, or offers both.
- Should cross-Part drag physically move chapter files between Part folders? — Current thinking follows the milestone context for R013: drag-reorder updates Book.txt and may move files between Part folders when moving across Parts, but this increases risk and must be made explicit in the S01 plan.
- Should the M004 roadmap and requirement mapping be updated because R013/M005 was pulled forward? — Current thinking: yes, planning artifacts should be reconciled before execution so S01 is not implemented against a stale read-only slice contract.
- What exact context menu labels and confirmation copy should be used? — Current thinking can be resolved during execution, but destructive copy must be clear and non-destructive actions should stay low-friction.

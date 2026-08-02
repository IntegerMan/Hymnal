# M005: Manuscript Structure

**Gathered:** 2026-06-07
**Status:** Ready for planning
**Depends on:** M004

## Project Description

Hymnal is a cross-platform desktop manuscript editor for authors using the Book.txt / Markua workflow. M005 gives authors full structural control over their manuscript from within the app: sidebar chapter management (show, include/exclude, rename, reorder), corkboard drag-to-reorder with cross-Part file moves, corkboard inclusion toggle and chapter insertion, and Gantt row drag-to-reorder.

This milestone absorbs the work that was previously planned as the separate `M007-trfeo8: Sidebar Chapter Management` milestone, combining it with the Late V1 corkboard structural editing scope (FR-31–33) and Gantt drag-reorder (FR-27 Late V1 extension) into a single coherent structural milestone.

## Why This Milestone

M004 completed the Early V1 author loop. M005 opens the second dimension: authorial control over the manuscript structure itself. Before M005, the only way to add, remove, rename, or reorder chapters is by manually editing Book.txt in a text editor. After M005, every structural operation has a first-class UI surface in Hymnal.

M005 ships before AI milestones (M006+) because AI-mode features — summaries, issues, review — are scoped to chapters. Stable, UUID-anchored chapter identity across rename/reorder/include-exclude is a prerequisite for those features to work correctly without losing chapter-level data.

## User-Visible Outcome

### When this milestone is complete, the user can:

- Open the sidebar and see chapters that exist on disk but are absent from Book.txt, shown with a distinct "excluded" appearance
- Toggle a chapter in/out of Book.txt from the sidebar with a single click; the change persists across restarts
- Rename a chapter or Part from the sidebar; the rename updates the underlying file/folder and preserves notes, phases, and word count history (UUID-stable)
- Drag chapters or Parts to a new position in the sidebar; Book.txt is rewritten atomically and the new order survives reload
- Drag Corkboard cards to reorder chapters, or drag across Part dividers to restructure the manuscript
- Toggle a chapter's inclusion state from its Corkboard card
- Insert a new chapter between existing Corkboard cards
- Drag Gantt rows within a Part to reorder chapters (same underlying Book.txt write)

### Entry point / environment

- Entry point: `dotnet run --project src/Hymnal/Hymnal.csproj` (or installed binary)
- Environment: Local desktop, Windows 10+ (Linux stretch goal)
- Live dependencies: local file system (Book.txt, chapter .md files, `.hymnal-data/`)

## Completion Class

- Contract complete means: unit tests cover `ChapterRegistryService` UUID stability across rename/reorder, `ManuscriptService` Book.txt atomic rewrite, inclusion state round-trip, and corkboard card cross-Part move logic
- Integration complete means: sidebar + Corkboard + Gantt all reflect the same chapter order; structural changes survive workspace reload without data loss or duplicate UUIDs
- Operational complete means: file-move on Corkboard cross-Part drag is atomic and no partial state is visible if the write fails

## Architectural Decisions

### OQ-3 Resolution: exclusions.json storage format

**Decision:** Excluded chapters are stored in `.hymnal-data/exclusions.json` as a JSON array of relative file paths. The file has a `schemaVersion` field. Excluded chapters remain in their on-disk location and are not deleted.

**Rationale:** A separate manifest is cleanest for the data model. Commenting Book.txt lines is fragile and couples exclusion state to the manuscript file itself. A sidecar exclusions.json keeps Book.txt as a pure ordered include-list.

### UUID stability on rename/reorder

**Decision:** `ChapterRegistryService.ReconcileAsync` must handle a path change without treating it as a new chapter. The reconcile path uses title matching as the tiebreaker when a UUID's stored path is no longer present on disk but a file with the same title exists.

**Rationale:** Chapter notes, phase dates, word count history, and (future) AI summaries are all keyed by UUID. Losing the UUID on rename would silently orphan all per-chapter metadata.

### Gantt drag uses same Book.txt write path

**Decision:** Gantt row drag-to-reorder calls the same `ManuscriptService.ReorderChapterAsync` method as sidebar drag. No separate Gantt-specific write path.

**Rationale:** Single write path = single test surface + single atomic guarantee.

## Requirements Covered

- R013 (Corkboard structural editing: FR-31–33) — primary
- R005 (Gantt view) — supporting slice S07 adds Late V1 drag-reorder
- Sidebar chapter management (M007 absorbed) — primary for S01–S04, S08

## Open Questions

- OQ-1: Should Hymnal offer to add `.hymnal-data/exclusions.json` to `.gitignore`? Recommend deciding in S02 implementation. Notes and phase-date files should remain tracked; exclusions.json is debatable.
- Cross-Part drag with file move: what is the rollback strategy if the file rename succeeds but the Book.txt write fails?

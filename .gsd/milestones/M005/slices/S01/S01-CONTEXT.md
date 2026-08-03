---
id: S01
milestone: M005
status: ready
---

# S01: Atomic structure core and exclusion manifest — Context

<!-- Slice-scoped context. Milestone-only sections (acceptance criteria, completion class,
     milestone sequence) do not belong here — those live in the milestone context. -->

## Goal

Deliver the Core storage and operation contract for persistent exclusions, atomic Book.txt structural writes, rollback-aware cross-Part file moves, and UUID-safe path-changing operations.

## Why this Slice

This slice is first because every later sidebar, Corkboard, and Gantt structural affordance depends on one trustworthy Core path for changing manuscript structure. It retires the highest data-loss risk before UI work begins: keeping Book.txt, markdown file locations, `.hymnal-data/exclusions.json`, and UUID-backed metadata consistent when operations fail or paths change.

## Scope

### In Scope

- Add a schema-versioned `.hymnal-data/exclusions.json` persistence path for intentional user exclusions.
- Treat exclusions as user intent: removing a chapter from Book.txt through Hymnal records it; random orphan `.md` files remain discoverable but are not automatically persisted as intentional exclusions.
- Keep stale exclusion entries load-tolerant when files are deleted externally: do not show phantom files, and prune missing paths only when the manifest is next saved.
- Treat `exclusions.json` as manuscript structure metadata worth tracking in Git, like other `.hymnal-data` JSON files.
- Extend or introduce Core structural operations so include, exclude, reorder, rename/path-change, and cross-Part move all flow through the canonical BookTxtStructureService contract.
- For cross-Part moves, update the file location and Book.txt path/order together from the caller's perspective.
- If a file move succeeds but the Book.txt update fails, attempt rollback and still return an explicit failure so the UI can say the move failed and the manuscript was restored.
- If rollback cannot fully restore the previous state, return a clear recoverable failure that identifies the affected path and phase.
- If a target file path already exists, fail without auto-suffixing, overwriting, or attempting title-based merge.
- Preserve UUID-backed metadata identity during path-changing operations, or fail when the operation would be ambiguous.
- Add automated Core tests for exclusion manifest round-trip, stale entry tolerance, include/exclude semantics, cross-Part move success, target conflict failure, Book.txt write failure with rollback, and UUID continuity expectations.

### Out of Scope

- Sidebar UI for showing excluded chapters or toggling include/exclude; that is S02.
- Sidebar rename and drag-reorder affordances; those are S03 and S04.
- Corkboard drag/drop, inclusion controls, and chapter insertion UI; those are S05 and S06.
- Gantt row drag-reorder; that is S07.
- A manual repair UI for unresolved structural failures; S01 should return actionable failures, while integrated polish belongs in S08 unless a minimal Core recovery artifact becomes unavoidable.
- Automatically rewriting `.gitignore` or prompting the user about Git tracking.
- Automatically adopting every orphan markdown file into `exclusions.json` on workspace load.
- Auto-renaming, overwriting, or merging target-path conflicts.

## Constraints

- Core remains free of Avalonia dependencies and should expose Result-returning service methods rather than UI behavior.
- All persistent writes must use existing atomic-write patterns through `IMetadataStore.WriteTextAtomicAsync()` or equivalent Core infrastructure.
- Book.txt remains the authoritative ordered include-list; exclusions.json records intentional excluded paths and does not replace Book.txt parsing.
- Path handling must normalize to forward-slash relative paths and use case-insensitive comparison where the existing manuscript structure code does.
- `.hymnal-data/` remains the only location for non-manuscript metadata written outside explicit editor saves.
- ViewModels and Views must not perform raw structural file writes; later slices consume the Core contract produced here.
- Structural failures must be explicit enough for later UI surfaces to show actionable notification text.
- UUID continuity is higher priority than completing an ambiguous move; preserve identity or fail.
- Existing watcher suppression and workspace reload orchestration should remain the higher-level integration path in later UI slices.

## Integration Points

### Consumes

- `src/Hymnal.Core/Services/BookTxtStructureService.cs` — existing canonical Book.txt structure writer to extend for exclusion-aware operations and cross-Part file moves.
- `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs` — service contract consumed by WorkspaceViewModel and CorkboardViewModel; update only in ways later surfaces can share.
- `src/Hymnal.Core/Interfaces/IMetadataStore.cs` — atomic text persistence for Book.txt-adjacent and `.hymnal-data` writes.
- `src/Hymnal.Core/Services/ChapterRegistryService.cs` — UUID continuity anchor that must remain compatible with rename and move semantics.
- `src/Hymnal.Core/Services/OrphanFileDiscoveryService.cs` — existing orphan discovery remains useful for random on-disk files that are not yet intentional exclusions.
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs` — current structure-service coverage to extend for rollback, manifest, and move semantics.

### Produces

- `.hymnal-data/exclusions.json` — schema-versioned JSON manifest of intentionally excluded chapter relative paths.
- Core exclusion manifest service or equivalent structure-service methods — load/save/add/remove normalized excluded paths with stale-entry tolerance.
- Extended `IBookTxtStructureService` operations — canonical include/exclude/path-changing operations for later sidebar, Corkboard, and Gantt slices.
- Core failure messages/results — user-actionable errors for write failures, rollback failures, target conflicts, and ambiguous identity preservation.
- Automated Core tests — executable proof that Book.txt, chapter files, exclusions, and UUID continuity stay consistent across success and failure cases.

## Open Questions

- Should S01 persist an explicit recovery-state file when rollback fails? — Current thinking: avoid unless implementation shows it is necessary; return a precise failure first, and leave user-facing repair workflow to S08.
- What exact method shape should represent cross-Part movement: extend `ReorderEntryAsync`, add a dedicated move method, or compose rename plus reorder internally? — Current thinking: choose the smallest Core API that lets later UI surfaces request one atomic user action and receive one Result.
- Should stale exclusion pruning happen in the manifest service automatically on save or only when include/exclude operations mutate the list? — Current thinking: prune when saving after a user-driven exclusion change, not during passive workspace load.
- How should title-based UUID preservation handle multiple plausible matches? — Current thinking: fail ambiguous operations rather than guess or create a new UUID silently.

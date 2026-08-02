# M005: Late V1 — Research

**Date:** 2026-06-07

## Summary

M005 is mostly an orchestration milestone around existing structural primitives rather than a brand-new manuscript model. The codebase already has atomic Book.txt operations, workspace reload/reorder plumbing, corkboard projection for parts/excluded cards/inline creation, sidebar drag-reorder, and a chapter registry that preserves UUIDs across path changes by title match.

The main risks are consistency and rollback, not discovery: cross-surface actions must share one canonical write path, and any move across Part folders needs a compensating rollback strategy so file moves and Book.txt updates cannot diverge. The other notable gap is persistence for excluded files: code can detect orphans and show excluded cards, but there is no exclusions.json load/save path yet, so the resolved OQ-3 decision still needs implementation.

## Recommendation

Implement M005 around the canonical Book.txt structure service and reuse the same path from sidebar, corkboard, and Gantt. Build and verify identity/storage first (registry continuity, exclusion manifest, atomic write semantics), then wire the UI entry points, then add Gantt row drag-reorder as a thin consumer of the same reorder primitive.

## Implementation Landscape

### Key Files

- `src/Hymnal.Core/Services/BookTxtStructureService.cs` — Canonical Book.txt writer. It already supports reorder, rename, add-existing, create-new, remove, and delete operations with atomic Book.txt writes; current reorder semantics are textual only and do **not** move files between Part folders.
- `src/Hymnal.Core/Services/ChapterRegistryService.cs` — UUID registry that preserves chapter identity across renames and title changes during workspace reconciliation; this is the key anchor for keeping notes/phases/history stable through structural edits.
- `src/Hymnal.Core/Services/OrphanFileDiscoveryService.cs` — Discovers unregistered `*.md` files and groups them by part folder so the corkboard can render excluded cards; it ignores `.hymnal-data` but does not persist exclusion state.
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs` — Workspace reload/reorder orchestrator. It reloads the manuscript model after Book.txt writes, reorders in-memory nodes after text changes, suppresses the Book.txt watcher during structural edits, and performs registry reconciliation during load.
- `src/Hymnal/ViewModels/CorkboardViewModel.cs` — Plan-mode structural coordinator. It already exposes reorder, rename, create chapter/part, include existing, remove, and delete commands and reuses the shared structure service; this is the cleanest place to keep corkboard actions thin.
- `src/Hymnal/ViewModels/CorkboardItemViewModel.cs` — Corkboard projection layer that already renders part dividers, empty-part hints, chapter cards, excluded cards, and transient inline-create items.
- `src/Hymnal/Views/CorkboardView.axaml.cs` — UI wiring for drag/drop, context menus, insertion-line hints, inline create, include existing, and delete/rename flows.
- `src/Hymnal/Views/SidebarView.axaml.cs` — Sidebar drag/drop wiring already forwards reorder gestures into `WorkspaceViewModel.ReorderChapterCommand`; only chapters are draggable.
- `src/Hymnal/ViewModels/GanttViewModel.cs` and `src/Hymnal/Views/GanttCanvas.cs` — Current Gantt surface is a projection/editing view for dates and status only. I found no drag-reorder command or drag/drop handling, so Late V1 row reorder still needs a dedicated surface implementation.
- `src/Hymnal/Views/GanttView.axaml.cs` — Contains inline edit and clipboard handling, but no row drag logic.
- `src/Hymnal/Views/SidebarView.axaml` — Sidebar item template for the chapter tree, including the existing drag handles and context menu surfaces used by the plan-mode list.

### What Already Exists vs. What Is Still Open

| Need | Existing code | Gap |
|---|---|---|
| Reorder within Book.txt | `BookTxtStructureService.ReorderEntryAsync`, `WorkspaceViewModel.ReorderNodesAsync`, sidebar/corkboard reorder commands | Cross-Part drag still rewrites only Book.txt; it does not move chapter files between folders |
| Include/exclude chapter | Orphan discovery, excluded corkboard cards, `IncludeExistingChapterCommand`, `RemoveFromBookCommand` | No exclusions.json persistence path exists yet |
| Rename chapter | Corkboard rename command, registry title-aware reconcile | Sidebar rename wiring was not obvious in the current code I inspected; file move semantics for rename are also absent |
| Insert new chapter | Corkboard inline-create and add menu flows | Mostly covered; needs to stay aligned with the same structural service |
| Gantt row reorder | None found in Gantt UI/view model | Needs new drag/drop or row-move surface that still routes through the same Book.txt reorder primitive |

### Build Order

1. **Prove the storage contract first.** Extend the structure service for cross-Part move semantics, define the failure/rollback story, and add persistence for exclusions if that remains in scope. This is the highest-risk area because it can leave Book.txt and the filesystem out of sync.
2. **Lock identity continuity next.** Keep the ChapterRegistryService title-aware reconcile path as the canonical source of UUID stability so rename/move does not orphan notes, phases, or history.
3. **Wire sidebar and corkboard against the same primitives.** These surfaces can stay thin if they call the same structure service methods and rely on WorkspaceViewModel reload/reorder behavior.
4. **Add Gantt row drag-reorder last.** It should be a thin consumer of the same reorder primitive, not a second structural write path.

### Verification Approach

- Add/extend `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs` for cross-Part reorder/move, rollback on partial failure, and any exclusions manifest round-trip.
- Extend `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs` for reorder, include, insert, rename, and selection preservation after structural writes.
- Extend `tests/Hymnal.Core.Tests/ViewModels/MainWindowPlanModeTests.cs` or sidebar tests for cross-surface consistency after drag-reorder.
- Add Gantt-specific tests under `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs` once row drag-reorder exists.
- Verify the visible behavior with a workspace reload: ordering, inclusion state, and UUID-backed data should survive restart without duplication or loss.

## Don't Hand-Roll

| Problem | Existing Solution | Why Use It |
|---|---|---|
| Atomic manuscript writes | `IMetadataStore.WriteTextAtomicAsync()` | Keeps manuscript data durable and matches the project-wide atomic-write contract. |
| Workspace refresh after structure edits | `WorkspaceViewModel.ReloadCurrentWorkspaceAsync()` / `ReorderNodesAsync()` | Prevents duplicate reload logic and keeps registry/phase/target hydration centralized. |
| Excluded-file detection | `OrphanFileDiscoveryService` | Reuses existing `.md` scanning and part-folder grouping instead of inventing another file walker. |

## Constraints

- All manuscript file writes must remain atomic; the editor shell should not write raw manuscript files directly.
- `.hymnal-data/` is the only directory intended for non-explicit-editor persistence.
- Structural edits need file-watcher suppression or a comparable guard to avoid reload loops.
- Path handling is normalized to forward-slash relative paths and case-insensitive comparisons for cross-platform stability.
- The current Gantt surface is ordered from the workspace node list and has no drag/drop reordering mechanism yet.

## Common Pitfalls

- **Moving the file before the Book.txt update is proven** — this can leave a chapter file orphaned if the text write fails; plan for rollback or a compensating restore path.
- **Treating rename as a simple path swap** — UUID continuity depends on the registry reconcile path, so rename/move must preserve or intentionally remap identity.
- **Duplicating reorder logic in three surfaces** — sidebar, corkboard, and Gantt should all call the same canonical structural service to avoid divergent semantics.
- **Assuming orphans are the same thing as exclusions** — the code can detect missing Book.txt entries today, but persistence still needs a deliberate exclusions manifest.

## Open Risks

- The rollback strategy for cross-Part drag is still the biggest unknown: if file move succeeds but Book.txt write fails, what is the recovery path and how visible is the partial state?
- Exclusions storage was resolved conceptually as `exclusions.json`, but the implementation path is still absent from the inspected code.
- The sidebar/plan surface split may need a deliberate ownership decision for rename and include/exclude actions so users do not encounter two inconsistent structural affordances.

## Candidate Requirement / Scope Notes

- Cross-Part drag should be treated as a single atomic user-visible action even if it is implemented as multiple filesystem operations internally.
- If a structure change moves a chapter into a different Part folder, the move should preserve the chapter's UUID-backed history and notes.
- Exclusion state should round-trip across restart instead of being inferred only from orphan discovery.
- Gantt row drag-reorder should not introduce a second Book.txt write path; it should reuse the same reorder primitive as sidebar/corkboard.

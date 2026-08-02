# M005: Late V1 Manuscript Structure

**Vision:** Give authors full structural control over a Book.txt manuscript from inside Hymnal: sidebar chapter management, persistent inclusion state, UUID-stable rename and reorder, Corkboard structural editing including cross-Part moves, and Gantt row reorder, all routed through one atomic Book.txt structure path.

## Success Criteria

- Excluded markdown files are visible as excluded chapters and can be included or excluded with state surviving workspace reload.
- Sidebar rename and reorder operations update the filesystem and Book.txt without orphaning UUID-backed notes, phases, targets, or word-count history.
- Corkboard drag reorder, cross-Part movement, inclusion toggle, and chapter insertion operate through the same canonical structure service and survive reload.
- Gantt row drag reorder is a thin consumer of the same Book.txt reorder primitive and does not introduce a second write path.
- Failure paths for structural edits leave no silent partial state: Book.txt, chapter files, registry, and exclusion manifest remain recoverable and user-visible errors are surfaced.

## Slices

- [x] **S01: Atomic structure core and exclusion manifest** `risk:Highest risk: this slice contains the multi-resource consistency boundary where Book.txt, filesystem paths, exclusions, and UUID-backed registry state can diverge.` `depends:[]`
  > After this: After this, automated Core tests can exclude and include files, move a chapter across Part folders, force a Book.txt write failure, and prove the filesystem and Book.txt recover without silent partial state.

- [x] **S02: Sidebar excluded chapters and include toggle** `risk:Medium: user-facing state can easily desynchronize between orphan discovery, exclusions.json, Book.txt, and the sidebar tree.` `depends:[S01]`
  > After this: After this, a chapter absent from Book.txt appears in the sidebar with excluded styling, can be included or excluded, and remains correct after reload.

- [x] **S03: Sidebar rename with UUID continuity** `risk:High: renames touch filesystem paths, Book.txt text, display titles, registry reconciliation, and metadata continuity.` `depends:[S01,S02]`
  > After this: After this, renaming a chapter or Part from the sidebar updates files and Book.txt, reloads the workspace, and preserves notes, phases, targets, and history under the same chapter UUID.

- [x] **S04: Sidebar drag reorder for chapters and Parts** `risk:Medium: drag gestures need watcher suppression, reload consistency, and clear limits around legal moves.` `depends:[S01,S02,S03]`
  > After this: After this, dragging chapters or Parts in the sidebar updates Book.txt and the new order survives reload without duplicate nodes.

- [x] **S05: Corkboard drag reorder and cross-Part moves** `risk:Highest UI integration risk: drag-drop must map visual card positions to the canonical structure operation and prove real file movement.` `depends:[S01,S04]`
  > After this: After this, dragging a Corkboard chapter card within or across Part dividers updates Book.txt, moves files when needed, and the Corkboard reflects the new structure after reload.

- [x] **S06: Corkboard inclusion toggle and chapter insertion** `risk:Medium: insertion and inclusion must share order semantics with drag reorder and avoid duplicate files or duplicate registry entries.` `depends:[S01,S02,S05]`
  > After this: After this, a Corkboard card can be included or excluded and a new chapter can be inserted between cards with persisted Book.txt order.

- [x] **S07: Gantt row drag reorder** `risk:Medium: the Gantt surface currently has no drag-reorder handling, so the risk is UI gesture implementation and keeping it a thin consumer.` `depends:[S01,S04]`
  > After this: After this, dragging a Gantt row within a Part reorders chapters using the same Book.txt write path as sidebar and Corkboard.

- [x] **S08: Structural consistency UAT and failure polish** `risk:Medium: individual slices can pass while cross-surface reload, watcher suppression, and failure messaging still feel inconsistent.` `depends:[S02,S03,S04,S05,S06,S07]`
  > After this: After this, a desktop UAT script performs sidebar, Corkboard, and Gantt structure changes on one workspace, restarts the app, and verifies one consistent manuscript state with no UUID or metadata loss.

## Boundary Map

## Horizontal Checklist

- Requirements: R013 primary, R005 supporting, sidebar chapter management absorbed from former M007; R006 and R012 are constraints or existing surfaces rather than new primary outcomes.
- Decisions: Exclusions persist in `.hymnal-data/exclusions.json`; all surfaces use the canonical BookTxtStructureService path; Gantt remains a thin consumer.
- Shutdown: No long-running services are introduced. Existing file watchers must be suppressed during intentional structure writes and resume after reload.
- Revenue: No billing, payments, or monetization surfaces are touched.
- Auth: No authentication or credential flow is touched.
- Shared resources: Book.txt, chapter markdown files, `.hymnal-data/registry.json`, `.hymnal-data/exclusions.json`, notes, phase data, and target or history files must remain consistent.
- Reconnection: Workspace reload after structural edits is the reconnection path; restart persistence is part of acceptance.

## Runtime Boundaries

- Avalonia UI Views and ViewModels initiate commands only.
- Hymnal.Core structure services own Book.txt, filesystem moves, exclusions, and rollback semantics.
- Metadata stores provide atomic sidecar writes under `.hymnal-data/`.
- The local filesystem is the only external dependency; no API keys or SaaS services are required.

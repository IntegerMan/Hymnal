# M007-trfeo8: M007-trfeo8: Sidebar Chapter Management

**Vision:** Let authors manage chapter structure directly from the sidebar while keeping UUID-backed metadata stable and Book.txt as the canonical manuscript structure source.

## Success Criteria

- Chapters that exist on disk in a part folder but are not listed in Book.txt are visible in the sidebar with a distinct excluded state.
- The author can add or remove a chapter from the book from the sidebar, and the inclusion state persists across reloads and restarts.
- The author can rename a sidebar chapter or part without losing UUID-backed metadata or breaking chapter recovery.
- The author can drag and drop chapters or parts to a new order in the sidebar, and the order persists back to Book.txt.
- dotnet build exits 0; targeted tests around chapter reconciliation, rename/inclusion persistence, and sidebar ordering pass.

## Slices

- [ ] **S01: Show unlisted disk chapters** `risk:high` `depends:[]`
  > After this: After this: chapters that exist on disk in a part folder but are absent from Book.txt appear in the sidebar with a distinct excluded state and survive reloads.

- [ ] **S02: Add and remove chapters from the book** `risk:high` `depends:[S01]`
  > After this: After this: the author can include or exclude a chapter from Book.txt from the sidebar, and the inclusion state persists after restart.

- [ ] **S03: Rename chapters from the sidebar** `risk:medium` `depends:[S01,S02]`
  > After this: After this: the author can rename a chapter or part from the sidebar, and the rename updates the underlying structure while preserving UUID-backed metadata.

- [ ] **S04: Drag reorder sidebar items** `risk:high` `depends:[S01,S02]`
  > After this: After this: the author can drag chapters or parts to a new position in the sidebar and the new order is restored after reload.

- [ ] **S05: Sidebar chapter editing integration** `risk:medium` `depends:[S02,S03,S04]`
  > After this: After this: the sidebar can reconcile disk-only chapters, include or exclude them, rename them, reorder them, and reload without drift between the sidebar and Book.txt.

## Boundary Map

## Boundary Map

```text
┌─ Hymnal.Core ──────────────────────────────────────────────────────────┐
│ ChapterRegistryService: UUID/path reconciliation, include/exclude state │
│ BookTxtParser / ManuscriptService: canonical book structure read/write  │
│ Models: ChapterNode, ManuscriptModel                                   │
└────────────────────────────────────────────────────────────────────────┘
            │ stable chapter identity + atomic structure writes
┌─ Hymnal (UI / Avalonia) ───────────────────────────────────────────────┐
│ WorkspaceViewModel: chapter collection, selection, edit commands        │
│ ChapterViewModel: per-chapter state used by sidebar rows                │
│ SidebarView: excluded/disk-only display, rename, reorder, toggle flows  │
└────────────────────────────────────────────────────────────────────────┘
            │ persists back to canonical files
┌─ Workspace root + .hymnal-data/ ───────────────────────────────────────┐
│ Book.txt and workspace chapter files remain source of truth for order   │
│ chapter-registry.json continues to anchor UUID identity across changes  │
└────────────────────────────────────────────────────────────────────────┘
```

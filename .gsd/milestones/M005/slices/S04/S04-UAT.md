# S04: Sidebar drag reorder for chapters and Parts — UAT

**Milestone:** M005
**Written:** 2026-06-18T13:15:49.882Z

## UAT Type

Manual desktop UAT deferred to S08 integrated structural consistency UAT; this slice provides an executable test-backed UAT script for sidebar behavior.

## Preconditions

- Use a workspace with a root or `manuscript/Book.txt` containing at least two `{class: part}` Part divider files and multiple included chapter entries under each Part.
- Include at least one markdown file discoverable as excluded/missing projection state from prior S02 behavior.
- Launch Hymnal and open the workspace.

## Steps and Expected Outcomes

1. Drag an included chapter row to another position within the same Part section.
   - Expected: the drop affordance appears only for legal same-Part chapter targets; `Book.txt` order changes; the sidebar reloads in the new order; no duplicate chapter nodes appear.
2. Restart or reload the workspace.
   - Expected: the chapter order from step 1 persists from `Book.txt`.
3. Drag an included Part divider relative to another Part divider.
   - Expected: the Part divider and all of its child chapter entries move as a single block; no child chapters are split from their Part; `Book.txt` reflects the moved block.
4. Restart or reload the workspace again.
   - Expected: the Part-block order persists and the sidebar has one node per active Book.txt entry plus expected excluded projections, with no duplicates.
5. Attempt to drag an excluded or missing row.
   - Expected: no drag starts, or the drop is rejected; no Book.txt mutation occurs.
6. Attempt to drop a chapter onto a target in a different Part.
   - Expected: the UI rejects the unsupported cross-Part move before Core mutation and surfaces a user-visible error.
7. Attempt to drop a Part onto a chapter, or a chapter onto a Part divider.
   - Expected: the UI rejects the mismatched-kind drop; Book.txt remains unchanged.
8. Simulate or induce a downstream Core reorder failure after a legal drag intent.
   - Expected: the user is notified with the Core failure context, watcher suppression is released, Book.txt remains unchanged, and the sidebar order remains unchanged without duplicates.

## Edge Cases

- Self-drops are ignored/rejected.
- Blank or inactive paths are not valid drag sources or targets.
- Excluded projections are ignored for active Book.txt index calculation.
- Part moves after an invalid final target are rejected before mutation.

## Acceptance

Accept S04 when legal same-Part chapter reorder and Part-block reorder persist after reload, illegal endpoints never mutate Book.txt, user-visible errors appear for unsupported moves, and automated tests remain green.

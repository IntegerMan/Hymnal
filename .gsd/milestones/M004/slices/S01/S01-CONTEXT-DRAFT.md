---
id: S01
milestone: M004
status: draft
---

# S01: Plan Mode Corkboard Cards — Context Draft

## Signals Captured So Far

- Plan mode should feel like a full-board focus surface: keep top navigation, collapse sidebars/right panes like Manage mode, and give the corkboard maximum width.
- Default card density should be compact scan: many chapters visible, concise metadata, quick triage over decorative index-card styling.
- Part structure should remain visible: show Part dividers in manuscript order, including a subtle empty-Part hint when a Part has no chapters.
- Card activation: single click selects; double-click opens the chapter and switches to Write mode. Selected card should have clear accent border/glow, and Enter should open the selected chapter. Arrow-key grid navigation is nice-to-have only unless execution finds it cheap.
- Target display should reserve space and show “No target” when no target exists, rather than hiding the target row.
- Large manuscript posture: start with a simple wrapping card grid and only revisit virtualization after evidence of stutter around 100+ chapters.
- Corkboard data should be live while Plan is visible, reflecting chapter status, word count, targets, and phase dates through the same reactive projection rather than a stale snapshot.
- Scope change requested: pull M005-style structural Corkboard editing into S01, including drag-reorder and right-click structural options. This materially expands S01 beyond the original read-only M004/R006 slice and will need plan/requirement alignment before execution.

## Follow-up Needed

- Define the exact structural edit set for right-click: reorder only, insert chapter, include/exclude, move between Parts, delete/remove, or subset.
- Define failure/confirmation behavior for Book.txt rewrites and any file moves.
- Confirm whether implementing structural editing in S01 should update the milestone roadmap and requirement mapping for R013.

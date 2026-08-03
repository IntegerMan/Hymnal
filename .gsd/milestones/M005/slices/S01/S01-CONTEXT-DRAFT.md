---
id: S01
milestone: M005
status: draft
---

# S01: Atomic structure core and exclusion manifest — Context Draft

## Interview Signals So Far

- Cross-Part move failure behavior: always surface an error when the requested move fails, even if rollback succeeds. Preferred wording/semantics: “move failed; manuscript was restored” rather than pretending nothing happened.
- Exclusions source of truth: `.hymnal-data/exclusions.json` tracks user intent only. Random orphan markdown files may still be discovered by existing orphan discovery, but should not be automatically persisted as intentional exclusions until the user acts.
- Path conflicts: if a target path already exists during a cross-Part move, fail without auto-renaming or merging. Avoid surprising manuscript path changes.
- Stale exclusion entries: tolerate stale manifest entries for deleted files. Do not show phantom cards; prune later when exclusions are next saved rather than rewriting metadata on load.
- Git tracking: treat exclusions.json as manuscript structure intent worth tracking in Git, like other `.hymnal-data` JSON metadata.
- UUID continuity: preserve metadata identity or fail. If path/title evidence is ambiguous, stop instead of silently creating a new UUID and orphaning notes, phases, history, or future AI data.

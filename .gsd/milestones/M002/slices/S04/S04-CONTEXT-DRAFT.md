---
id: S04
milestone: M002
status: draft
---

# S04: Runtime Stabilization and Chapter Info Wiring — Context (DRAFT)

## Goal

Fix the startup crash caused by OAPH initialization order in ChapterInfoViewModel, wire ProximityFill to live ChapterViewModel data, and complete a defensive sweep of the ChapterInfo/MainWindow ViewModels to confirm no other stubs or null-unsafe patterns exist.

## Why this Slice

S03 completed the Chapter Info pane and validation margin but left two known issues: a NullReferenceException that prevents the app from launching (OAPH ordering bug in ChapterInfoViewModel), and a ProximityFill value stubbed at 0.0. S04 must resolve both before S05 can run the full desktop UAT.

## Scope

### In Scope

- Fix OAPH initialization order in ChapterInfoViewModel constructor (`_hasTarget` must be initialized before `_proximityFill`)
- Replace ProximityFill stub with live subscription to `ChapterViewModel.ProximityFill` in the per-chapter block in `OnActiveNodeChanged`
- Defensive sweep of ChapterInfoViewModel and MainWindowViewModel for other stubs or null-unsafe patterns
- Full build + test run to confirm no regressions

### Out of Scope

- New UI features or layout changes
- CredentialStoreStub (intentionally deferred to a future milestone)
- ThrownExceptions subscriptions on synchronous commands (ExitCommand, ToggleSidebarCommand) — low-risk
- S05 UAT checks

## Constraints

- Must not change the public API of ChapterInfoViewModel (view bindings must remain intact)
- All writes to .hymnal-data must continue through MetadataStore atomic path
- Hymnal.Core must remain free of Avalonia references

## Integration Points

### Consumes

- `ChapterViewModel.ProximityFill` — live 0.0–1.0 fill fraction subscribed in OnActiveNodeChanged per-chapter block
- `ChapterViewModel.HasTarget` — may replace TargetDisplay-derived OAPH if simpler

### Produces

- `ChapterInfoViewModel` — crash-free startup; live ProximityFill driven from ChapterViewModel
- Passing build and test suite

## Open Questions

- Whether to keep the `_hasTarget` OAPH (derived from TargetDisplay) or simplify it to a backing field set from `chapterVm.WhenAnyValue(x => x.HasTarget)` — current thinking: simplify to match the WordCount/Status/TargetDisplay property pattern already used in the per-chapter subscription block.

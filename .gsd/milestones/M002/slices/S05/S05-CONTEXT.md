---
id: S05
milestone: M002
status: ready
---

# S05: Desktop UAT and Operational Verification — Context

## Goal

Execute the full M002 acceptance scenario against the real manuscript, record pass/fail evidence and latency numbers in the S05-SUMMARY checklist, and fix any defects found before signing off on M002.

## Why this Slice

S01–S04 established all the code; S05 is the only gate between "builds and passes unit tests" and "actually works for the author." It must run last because it validates the integrated behaviour of every prior slice together — status persistence, UUID rename continuity, word count rollup, Chapter Info pane, and the ValidationMargin — in a live desktop session, not a unit test harness.

## Scope

### In Scope

- Run all four M002 milestone acceptance scenarios end-to-end:
  1. Change chapter status → sidebar dot updates → restart → dot and phase-start date intact
  2. Rename chapter file + update Book.txt → relaunch → status and word count intact (UUID preserved)
  3. Open chapter → write 100 words → Ctrl+S → word count in Chapter Info and sidebar/totals updated → `wordcount-history.json` gains today's entry
  4. Set word count target of 4,000 on a 2,000-word chapter → proximity bar shows ~50% fill
- Operational latency evidence: word count update ≤ 500 ms from keystroke; cold-start time observed and recorded
- F3 / Chapter Info pane usability pass: status picker, editable phase dates, live word count display, target display
- ValidationMargin visual check: temporarily add a blank-line-before-`{sample: true}` snippet and/or an unrecognised attribute key (`{foobarbaz: true}`) to a chapter, confirm amber advisory dot appears in the gutter, then undo/remove the snippet
- Defect fixing — if any scenario fails, a fix task (T02) is added within S05, the fix is applied, and the failing scenario re-runs before M002 is closed

### Out of Scope

- New features or enhancements beyond closing identified defects
- Automated UI testing (AvaloniaEdit/ReactiveUI binding layer has no headless test path)
- Structural refactoring unrelated to a discovered defect
- Validation of Notes pane (F4) beyond confirming F3 does not break it
- Formal screenshot archive or standalone verification document

## Constraints

- **Real manuscript workspace** — UAT must be run against the actual *A Choir of Minds* workspace (not the `simple-book` fixture) to produce meaningful latency and rollup evidence with real chapter count and real `.hymnal-data/` state accumulated from S01–S04.
- **Rename test uses an isolated copy** — copy the workspace to a temp directory (`Copy-Item -Recurse`), open Hymnal against the copy, run rename + `Book.txt` edit + relaunch there, then discard the copy. The original workspace must never be modified by the rename test.
- **ValidationMargin test is non-destructive** — temporarily type or paste the advisory snippet into an open chapter, visually confirm the dot, then undo with Ctrl+Z before saving. No permanent change to manuscript files.
- **Evidence goes in S05-SUMMARY** — the Observable Truths table in the summary is the verification record; no separate milestone artifact is needed.
- All prior M002 decisions (D010–D014) are frozen; S05 must not introduce new architectural patterns.

## Integration Points

### Consumes

- `src/Hymnal/ViewModels/ChapterViewModel.cs` — authoritative status, word count, proximity fill for sidebar
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs` — registry reconciliation and UUID-keyed metadata load on workspace open
- `src/Hymnal/ViewModels/ChapterInfoViewModel.cs` — F3 pane state: status picker, phase dates, live count, target display, phase-start prefill
- `src/Hymnal/ViewModels/EditorViewModel.cs` — debounced live word count, Saved observable that drives history append
- `src/Hymnal/Views/Editor/ValidationMargin.cs` — advisory gutter dots for the two Markua patterns
- `.hymnal-data/chapter-registry.json`, `phases.json`, `targets.json`, `wordcount-history.json` — the actual persisted state that the restart and rename tests validate

### Produces

- `S05-SUMMARY.md` with an Observable Truths checklist covering all four milestone acceptance criteria plus latency evidence — this is the M002 closure record
- Any defect fixes applied during T02 (if needed), verified by re-running the failed scenario

## Open Questions

- **Real manuscript chapter count** — number of chapters in the workspace determines how meaningful the cold-start evidence is; the milestone target is < 5 s for 100 chapters, but the real book may have fewer. Record the actual count and observed start time; no action needed unless the observed time exceeds 5 s.
- **Existing `.hymnal-data/` state in the real manuscript** — if `chapter-registry.json` already exists from earlier manual testing, the UAT starts from that persisted state. This is intentional (validates restart continuity) but should be noted in the summary if the registry has unexpected orphans that affect the dot display.
- **T02 scope cap** — if UAT uncovers a defect that requires significant architectural change (not a simple fix), escalate to a new S06 rather than expanding S05 indefinitely. The heuristic is: if the fix touches more than two files or requires planning, open a new slice.

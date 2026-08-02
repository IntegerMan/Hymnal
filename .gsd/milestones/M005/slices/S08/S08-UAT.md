# S08: Structural consistency UAT and failure polish — UAT

**Milestone:** M005
**Written:** 2026-06-19T05:31:08.314Z

## UAT Type

Desktop/manual end-to-end structural consistency replay, backed by automated ViewModel/Core replay.

## Preconditions

- Use a throwaway local workspace; no secrets or PII are required.
- Open/build with `Hymnal.slnx` using native Windows .NET from PowerShell when running outside the agent verification lane.
- Prepare a workspace with `Book.txt`, Part files under `front/`, `middle/`, `middle/act-one/`, and `back/`, included chapter files, and an orphan `middle/act-one/orphan.md` absent from `Book.txt`.
- Where possible, pre-create `.hymnal-data` registry, exclusions, notes, phase, target, and word-count history data so UUID continuity can be inspected.

## Main Replay Steps

1. Launch Hymnal and open the temp workspace.
2. Verify sidebar order matches `Book.txt`; Part nodes are visible but not normal selectable chapter documents; orphan/excluded files are styled as excluded where shown.
3. In the sidebar: remove `front/beta.md`, confirm the file remains on disk and appears excluded, include it again, rename `front/alpha.md` to `Renamed Alpha`, reorder `front/beta.md` after the renamed chapter, and move the Back Part before the Middle Part.
4. In Corkboard: reorder the renamed Alpha card within Front, move it into the Back section, remove `middle/act-one/delta.md`, verify Delta and Orphan appear as excluded cards without duplicates, include Orphan after Gamma, and inline-create `Inserted Scene` after Orphan.
5. In Gantt: verify row order matches the current manuscript projection and move `middle/act-one/gamma.md` after `middle/act-one/inserted-scene.md` using the supported row movement affordance.
6. Inspect persisted state: `Book.txt` has the final expected order, `back/renamed-alpha.md` exists, old Alpha paths are gone, Delta remains on disk but excluded, Inserted Scene exists, `exclusions.json` contains Delta only, registry UUIDs moved with their chapters, and UUID-keyed metadata sidecars remain readable with no duplicate identities.
7. Quit and relaunch Hymnal, reopen the same workspace, and verify sidebar, Corkboard, and Gantt all show one consistent state matching `Book.txt`, with excluded Delta appearing once and no duplicate paths.

## Controlled Failure Steps

1. Create a target filename conflict, then attempt a Corkboard cross-Part move into that conflicting path.
2. Expected: the move is rejected, user-facing error text is actionable, `CorkboardViewModel.LastStructuralError` contains operation/path/message/Book.txt context when inspected, and Book.txt/files/registry/exclusions/metadata remain unchanged after reload.
3. Attempt a Gantt cross-Part chapter reorder.
4. Expected: the operation is rejected because Gantt row reorder is same-Part only; notification copy explains the invalid move and points authors to Corkboard for cross-Part movement; all surfaces remain unchanged.

## Expected Outcomes

- Sidebar, Corkboard, and Gantt are thin consumers of one canonical Book.txt structure path.
- Restart/reload persistence preserves final structure and UUID-backed metadata continuity.
- Watcher suppression/reload behavior does not create duplicate nodes, stale selections, or re-entrant reload artifacts.
- Controlled failures are user-visible and non-destructive.
- Manual script details are maintained in `docs/uat/M005-S08-structural-consistency.md`.

---
id: M004
title: "Corkboard, Supplemental Docs, and Git Panel"
status: complete
completed_at: 2026-06-18T02:29:37.478Z
key_decisions:
  - Project the corkboard directly from existing chapter/view-model state instead of creating a second manuscript model.
  - Keep supplemental docs in a separate `.hymnal-data/docs` tree and reuse the shared editor lifecycle with `ActiveNode == null` for arbitrary files.
  - Run system Git through `IProcessRunner` and `GitCommandResult` so stderr and exit-code diagnostics stay structured and user-visible.
  - Keep the commit dialog in MainWindow code-behind while the ViewModel owns Git operations, notifications, and refresh logic.
key_files:
  - src/Hymnal/ViewModels/CorkboardViewModel.cs
  - src/Hymnal/ViewModels/CardViewModel.cs
  - src/Hymnal/ViewModels/EditorViewModel.cs
  - src/Hymnal/ViewModels/SupplementalDocsViewModel.cs
  - src/Hymnal/ViewModels/GitPanelViewModel.cs
  - src/Hymnal/ViewModels/MainWindowViewModel.cs
  - src/Hymnal.Core/Services/SupplementalDocsService.cs
  - src/Hymnal.Core/Services/ProcessGitService.cs
  - src/Hymnal/Views/CorkboardView.axaml
  - src/Hymnal/Views/SupplementalDocsView.axaml
  - src/Hymnal/Views/GitCommitDialog.axaml
  - tests/Hymnal.Core.Tests/Services/ProcessGitServiceLocalRepoTests.cs
lessons_learned:
  - Native Avalonia UAT-sensitive flows still need explicit desktop validation when headless/browser-style harnesses only provide partial confidence.
  - The WSL-backed `gsd_exec` lane is not authoritative for .NET verification in this repo; closeout should rely on task-session Windows SDK evidence when restore/build hit the known path-resolution failure.
  - The existing single-buffer editor can support supplemental documents safely by driving arbitrary-file state through `ActiveFilePath`, `ActiveNode == null`, and the shared atomic save path.
---

# M004: Corkboard, Supplemental Docs, and Git Panel

**Delivered the Early V1 author loop by shipping a Plan-mode corkboard, supplemental project docs in the shared editor, and an integrated Git commit workflow that all compose through Hymnal's existing shell/editor/workspace surfaces.**

## What Happened

M004 closed the Early V1 author loop by adding three integrated capabilities to the existing workspace/editor shell: a Plan-mode corkboard surface, a DOCS sidebar with arbitrary-file editing under `.hymnal-data/docs`, and a persistent Git toolbar with commit/push workflow. The closeout review started by confirming the milestone is still active and that all three slices are complete. Code-change verification passed by diffing `HEAD` against `origin/main` merge-base `cdef6cc9f060604e82188421e444ab8128dd1b5e`, which showed non-`.gsd/` implementation changes in `src/Hymnal/ViewModels/GitPanelViewModel.cs` and `src/Hymnal/Views/MainWindow.axaml`.

The passing validation artifact remained authoritative after a freshness check because it still matched the current slice summaries and milestone status: S01 delivers Plan mode as part dividers plus live chapter cards with click-to-open Write routing; S02 adds a separate supplemental-docs tree and routes arbitrary files through the shared editor dirty/save lifecycle; S03 adds system-Git detection, branch/change-count visibility, stage-all commit flows, debounced status refresh, and raw stderr propagation. Together they compose cleanly across the shared shell/editor/workspace surfaces. The only earlier gap was native desktop UAT for S01 and S02, and that gap is now explicitly closed by direct human validation already recorded in `M004-VALIDATION.md`.

## Decision Re-evaluation

| Decision | What shipped | Revisit? | Evidence |
|---|---|---|---|
| Corkboard projects from existing chapter/view-model state rather than a second manuscript model | Held. The shipped corkboard is a live projection over existing manuscript state and mixed item rows. | No | `S01-SUMMARY.md` What Happened + Patterns established |
| Structural Plan edits route through Book.txt service + workspace reload seam | Held. The seam kept structural edits synchronized without pushing file/editor mutations into the board. | Revisit in M005 only for expanded structural editing breadth, not for the seam itself | `S01-SUMMARY.md` What Happened / Deviations |
| Supplemental docs stay separate from chapter models and reuse the shared editor save path | Held. The shipped DOCS tree is isolated under `.hymnal-data/docs` and opens in the existing editor with arbitrary-file state. | No | `S02-SUMMARY.md` What Happened |
| Git runs through `IProcessRunner` with structured results and raw stderr | Held. The shipped toolbar and tests depend on structured process results and surfaced stderr on failure. | No | `S03-SUMMARY.md` What Happened / Verification |
| Commit dialog remains in MainWindow code-behind while the ViewModel owns operations | Held. Prompting stayed thin while the workflow stayed testable in the ViewModel. | No | `S03-SUMMARY.md` Key decisions |

The only notable limitations are milestone-local rather than blocking: S02's broad solution-test lane still carries unrelated existing Gantt/Corkboard failures, and S01's drag/drop plus dialog flows remain the most brittle UI surface and should be treated as a focus area in M005. Requirement status transitions were validated before closeout: R006 now moves from active to validated based on S01 projection/routing evidence plus milestone-level desktop confirmation; R007, R008, and R012 already had evidence-backed validated status and remain unchanged. Project state and requirements were refreshed before milestone completion, and M004 learnings were captured both as an audit file and as durable memories for future milestones.

## Success Criteria Results

- **Plan mode renders manuscript order as Part dividers plus chapter cards showing title, status, word count, optional target percentage, and optional phase dates** — PASS. Validation artifact cites S01 summary/test evidence plus direct desktop confirmation.
- **Clicking a corkboard card opens the selected chapter in the existing editor and activates Write mode** — PASS. Validation artifact cites S01 routing evidence plus direct desktop confirmation.
- **DOCS section supports creating folders/files under `.hymnal-data/docs`, opens docs in the existing editor, and preserves content through atomic saves and workspace reopen** — PASS. Validation artifact cites S02 service/view-model evidence plus direct desktop confirmation.
- **Git toolbar appears only when Git + repo are detected, displays branch + uncommitted count, and refreshes after saves/Git ops** — PASS. Validation artifact cites S03 assessment PASS with runtime and local-repo evidence.
- **Commit dialog stages all changes, uses the default timestamped message, supports Commit only and Commit & Push, and surfaces raw Git stderr on failure** — PASS. Validation artifact cites S03 assessment PASS.
- **Unit and manual smoke evidence cover CardViewModel projection, EditorViewModel arbitrary-file dirty transitions, ProcessGitService behavior, and a real local-repo Git workflow** — PASS. Automated evidence exists across S01-S03 and the remaining native desktop smoke gap was closed by direct human validation.

## Definition of Done Results

- All roadmap slices are complete: S01 5/5 tasks, S02 4/4 tasks, S03 4/4 tasks (`gsd_milestone_status`).
- Slice summaries exist for S01, S02, and S03 under `.gsd/milestones/M004/slices/*/*-SUMMARY.md`.
- Cross-slice integration is coherent per the passing validation artifact: Plan mode routes chapter opens into Write mode, supplemental docs reuse the same shell/editor path, and the Git panel refreshes against the shared editor/workspace lifecycle.
- Validation artifact `M004-VALIDATION.md` remains internally consistent with current artifacts after closeout checks, despite the earlier freshness warning.
- Horizontal Checklist: none present in `M004-ROADMAP.md`.
- Structured learnings were extracted to `.gsd/milestones/M004/M004-LEARNINGS.md` and durable project memories were captured for reusable M004 patterns, conventions, gotchas, and architecture decisions.

## Requirement Outcomes

- **R006** — Updated from **active** to **validated** in `.gsd/REQUIREMENTS.md`. Evidence: S01 summary proves live card projection, Part dividers, Plan-shell routing, and click-to-open Write behavior; milestone validation closes the remaining native desktop UAT gap with direct human confirmation.
- **R007** — Remains **validated**. Evidence in `M004-VALIDATION.md` and `S02-SUMMARY.md` confirms the DOCS tree, arbitrary-file editor path, atomic save, and reopen persistence.
- **R008** — Remains **validated**. Evidence in `M004-VALIDATION.md` and `S03-SUMMARY.md` confirms Git visibility gating, branch/change counts, default commit message, commit-only and commit-and-push flows, refresh behavior, and raw stderr surfacing.
- **R012** — Remains **validated**. M004 continues to rely on the shared atomic metadata-store path for supplemental docs and explicit author-driven write paths for manuscript-affecting operations.

## Deviations

S01 used a dedicated corkboard view plus a small workspace reload seam for structural refreshes instead of trying to reuse existing center-panel surfaces. S02 could not close native desktop smoke in the headless lane, so the milestone relied on automated slice evidence first and then closed the remaining UAT gap with direct human desktop validation recorded in M004 validation.

## Follow-ups

M005 should revisit the brittle drag-and-drop / confirmation-dialog paths from the corkboard while preserving the shared shell/editor contracts established here. Future native-Avalonia milestone closeouts should continue treating desktop validation as the final proof for UAT-sensitive flows when the harness cannot exercise them faithfully.

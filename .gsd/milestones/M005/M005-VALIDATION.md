---
verdict: needs-attention
remediation_round: 0
---

# Milestone Validation: M005

## Success Criteria Checklist
- [x] Excluded markdown files are visible as excluded chapters and can be included or excluded with state surviving workspace reload. Evidence: S02 SUMMARY/ASSESSMENT pass excluded-node projection, include/exclude commands, manifest persistence, and fresh reload checks; S08 integrated replay consumes the same path.
- [x] Sidebar rename and reorder operations update the filesystem and Book.txt without orphaning UUID-backed notes, phases, targets, or word-count history. Evidence: S03 ASSESSMENT passes chapter/Part rename plus UUID continuity; S04 ASSESSMENT passes chapter/Part reorder and reload persistence; S08 replay re-checks metadata continuity after restart.
- [x] Corkboard drag reorder, cross-Part movement, inclusion toggle, and chapter insertion operate through the same canonical structure service and survive reload. Evidence: S05 and S06 SUMMARY/ASSESSMENT pass canonical Corkboard reorder, cross-Part move, include/exclude, insertion, and reload persistence; S08 replay proves the combined flow.
- [x] Gantt row drag reorder is a thin consumer of the same Book.txt reorder primitive and does not introduce a second write path. Evidence: S07 SUMMARY/ASSESSMENT pass same-Part Gantt reorder, canonical-path delegation, and reload persistence; S08 replay includes final same-Part Gantt move.
- [ ] Failure paths for structural edits leave no silent partial state: Book.txt, chapter files, registry, and exclusion manifest remain recoverable and user-visible errors are surfaced. Artifact evidence is strong: S01 rollback coverage, S05 controlled move-conflict coverage, and S08 controlled failure replay all pass. However, the milestone-level live/native desktop UAT evidence remains partial because S08-ASSESSMENT is PARTIAL and records only artifact-backed verification plus manual human follow-up for visible failure copy and restart replay.

## Slice Delivery Audit
| Slice | SUMMARY.md | Assessment verdict | Audit |
|---|---|---|---|
| S01 | present | PASS | Delivered canonical structure core, exclusions manifest, rollback coverage, and UUID continuity foundations. |
| S02 | present | PASS | Delivered sidebar excluded-node projection, include/exclude commands, and reload persistence. |
| S03 | present | PASS | Delivered sidebar rename path through canonical service with UUID continuity preserved. |
| S04 | present | PASS | Delivered sidebar chapter/Part reorder with reload persistence and illegal-move rejection. |
| S05 | present | PASS | Delivered Corkboard reorder and cross-Part moves with failure visibility. |
| S06 | present | PASS | Delivered Corkboard include/exclude and chapter insertion with manifest consistency. |
| S07 | present | PASS | Delivered Gantt same-Part reorder as a thin consumer of the canonical structure path. |
| S08 | present | PARTIAL | Summary is complete and slice status is complete, but the recorded assessment verdict is PARTIAL because this validation lane could not re-run focused native .NET UAT or live desktop restart checks; manual script follow-up remains. |

Overall: all roadmap slices have SUMMARY.md artifacts, but the milestone does not have an all-pass assessment set because S08 remains PARTIAL rather than PASS.

## Cross-Slice Integration
Reviewer B found the producer/consumer contracts honored end-to-end across all planned slice boundaries: S01 provides the canonical structure service and exclusion manifest consumed by S02-S07; S02/S03/S04 feed the sidebar state and command semantics consumed later by S08; S05/S06/S07 deliver Corkboard and Gantt consumers that S08 replays together.

Integrated evidence exists in S08-SUMMARY.md through StructuralConsistencyUatTests, which drives sidebar include/exclude, rename, chapter reorder, Part reorder, Corkboard same-Part reorder, Corkboard cross-Part move, Corkboard inclusion toggle, chapter insertion, and Gantt same-Part reorder on one temp workspace, then verifies restart/reload persistence and UUID-backed metadata continuity.

Boundary mismatch: none in the delivered producer/consumer contracts.

Cross-slice attention point: the integration proof is artifact-backed but not fully re-executed in this verification lane. S08-ASSESSMENT.md is PARTIAL because the lane could not truthfully rerun focused native .NET UAT and still requires manual desktop confirmation for excluded-row styling, notification wording, and live quit/relaunch replay.

## Requirement Coverage
| Requirement / scope | Status | Evidence |
|---|---|---|
| R013 — Corkboard structural editing | COVERED | S05 and S06 deliver the Corkboard move/include/insert behaviors; S08 validates them end-to-end with restart/reload persistence, metadata continuity, and controlled failure visibility. REQUIREMENTS.md already marks R013 validated by M005/S08. |
| R005 — Gantt view supporting drag-reorder extension | COVERED FOR M005 SCOPE | S07 delivers same-Part Gantt row reorder through the canonical structure service; S08 replays the same path on the integrated workspace. This milestone advances only the Late V1 reorder extension, not the full base Gantt requirement. |
| Absorbed sidebar chapter-management scope (former M007) | COVERED | S01-S04 deliver exclusions manifest, excluded sidebar projection, include/exclude, rename, and reorder; S08 integrates these flows with restart persistence. |
| R012 — structural-write safety constraint | COVERED FOR TOUCHED PATHS | S01 rollback-aware structure operations, S03 canonical rename path, S05 controlled conflict handling, and S08 controlled failure replay all show no silent partial state on touched structural edit paths. |
| UUID-backed metadata continuity (supports R009/R003/R004 continuity expectations) | COVERED FOR TOUCHED PATHS | S03 and S08 explicitly preserve notes, phases, targets, and word-count history across rename, reorder, and restart by keeping registry UUID continuity. |
| R010 ownership mapping in REQUIREMENTS.md | PARTIAL / COHERENCE GAP | REQUIREMENTS.md still lists R010 (AI editorial assistance) with primary owning slice M005/S03, but S03-SUMMARY.md explicitly says that R010 mapping was out of scope for this milestone and the slice advanced R013 instead. Validation should keep M005 as needs-attention until requirement traceability is realigned. |

Overall: the milestone roadmap scope itself is covered, but requirement traceability is not fully coherent because REQUIREMENTS.md still maps R010 to M005/S03 while the delivered milestone did not implement AI editorial work.

## Verification Class Compliance
| Class | Planned Check | Evidence | Verdict |
|---|---|---|---|
| Contract | Core and ViewModel tests must cover the canonical structure service, exclusion manifest persistence, registry continuity, and each UI command path. | S01 passes include/exclude, move, rollback, and registry continuity coverage; S03 passes rename plus UUID continuity; S05 passes Corkboard cross-Part move; S06 passes inclusion round-trip; S07 passes Gantt thin-consumer reorder behavior. | PASS |
| Integration | Open a fixture workspace, execute structure changes through sidebar, Corkboard, and Gantt entry points, reload the workspace, and verify Book.txt order, file locations, inclusion state, registry UUIDs, notes, phases, targets, and word-count history remain consistent. | S08 StructuralConsistencyUatTests replay the full cross-surface flow and assert restart/reload state, but S08-ASSESSMENT.md remains PARTIAL because this validation lane could not re-run the focused native .NET UAT. | NEEDS-ATTENTION |
| Operational | Inject or simulate write and move failures in the structure service and verify rollback or explicit recoverable error reporting; watcher-safe structural writes remain recoverable. | S01 passes rollback and rollback-failure behavior; S05 passes controlled cross-Part conflict handling; S08 passes controlled Corkboard and Gantt failure replay with unchanged post-reload state. | PASS |
| UAT | Run the desktop app against a sample workspace and demonstrate excluded chapters, sidebar rename and reorder, Corkboard cross-Part drag and insert, Gantt row reorder, restart persistence, and error surfacing for impossible moves or conflicting file names. | Manual desktop script exists at docs/uat/M005-S08-structural-consistency.md and slice task summaries record native Windows focused-test/build evidence, but S08-ASSESSMENT.md is PARTIAL and still calls for human/native-desktop confirmation of visible styling, notification copy, and live quit/relaunch replay. | NEEDS-ATTENTION |


## Verdict Rationale
M005's planned slices are all complete and the milestone scope is strongly supported by slice summaries, passing assessments, and S08's integrated artifact-backed replay. The verdict is needs-attention, not pass, because milestone validation still has two coherence gaps: S08's assessment remains PARTIAL rather than PASS due missing fresh native desktop/runtime UAT in this lane, and REQUIREMENTS.md still maps R010 to M005/S03 even though the slice explicitly treated AI editorial work as out of scope for this milestone.

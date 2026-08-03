# S05: Desktop UAT and Operational Verification

**Goal:** Produce the M002 closure record by running automated build/test verification, performing R004 code-inspection evidence collection, and writing S05-SUMMARY.md with an Observable Truths checklist covering all four milestone acceptance criteria and latency expectations. Human-observable desktop steps are documented and flagged so the operator can complete the manual pass.
**Demo:** Run the desktop verification flow end-to-end: status survives restart and rename, word count save updates pane/sidebar/history, target shows expected proximity, validation gutter appears, and latency/cold-start evidence is recorded for validation.

## Must-Haves

- dotnet build src/Hymnal/Hymnal.csproj -nologo exits 0 with 0 errors and 0 warnings
- dotnet test Hymnal.sln -nologo passes all 57+ tests with 0 failures
- Code inspection confirms ChapterInfoViewModel has no OAPH, no stub, and no fallback for ProximityFill/HasTarget; both values come exclusively from ChapterViewModel subscriptions (R004 satisfied)
- Code inspection confirms WorkspaceViewModel rename-continuity logic (orphan marking + title-match UUID preservation) is present
- Code inspection confirms ValidationMargin has triggers for blank-line-before-{sample:true} and unrecognised-attribute-key patterns
- Code inspection confirms EditorViewModel debounces word count at 300 ms and exposes a Saved observable
- S05-SUMMARY.md written with Observable Truths table covering: status-dot persistence, rename UUID survival, live count + history, target/proximity (R004); automated evidence rows populated; human-verification items flagged explicitly

## Proof Level

- This slice proves: operational — final-assembly verification combining automated code-inspection evidence with a structured human-verification checklist for the desktop scenarios that have no headless automation path in this harness

## Integration Closure

Consumes: ChapterInfoViewModel.cs, ChapterViewModel.cs, WorkspaceViewModel.cs, EditorViewModel.cs, ValidationMargin.cs, SidebarView.axaml, ChapterInfoView.axaml, and the four .hymnal-data JSON schemas. Produces: S05-SUMMARY.md as the sole new artifact. No new wiring or architectural changes are introduced; this slice only observes and records.

## Verification

- None — no new log points, metrics, or diagnostics are added. The S05-SUMMARY.md is the sole evidence artifact.

## Tasks

- [x] **T01: Run automated baseline and collect R004 code-inspection evidence** `est:45m`
  **Why:** Establish a clean build and test baseline for M002 and gather the code-inspection evidence needed to satisfy R004 and the remaining milestone acceptance criteria before writing the closure record in T02.
  - Verify: powershell.exe -NoProfile -Command "dotnet test Hymnal.sln -nologo"

- [x] **T02: Produce S05-SUMMARY with Observable Truths evidence record** `est:30m`
  **Why:** S05-SUMMARY.md is the M002 closure record. It must present structured, traceable evidence for every milestone acceptance criterion so that M002 can be completed without ambiguity. Items verifiable via code inspection are reported as confirmed; items that require the desktop (GUI dot colour, cold-start timing, gutter marker visibility) are flagged as Human Verification Pending with exact operator steps.
  - Files: `.gsd/milestones/M002/slices/S05/S05-SUMMARY.md`
  - Verify: test -f .gsd/milestones/M002/slices/S05/S05-SUMMARY.md

## Files Likely Touched

- .gsd/milestones/M002/slices/S05/S05-SUMMARY.md

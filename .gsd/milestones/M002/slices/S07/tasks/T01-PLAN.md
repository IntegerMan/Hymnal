---
estimated_steps: 9
estimated_files: 1
skills_used: []
---

# T01: Confirm ProximityFill fix and update R003 + R014 to validated

Why: R003 (Chapter Status Lifecycle) and R014 (platform/quality-attribute) both remain 'unmapped' in REQUIREMENTS.md despite S01–S06 producing sufficient evidence. This task closes the traceability gap.

Do:
1. Read `.gsd/milestones/M002/slices/S01/S01-SUMMARY.md` — extract: 6 ChapterRegistryServiceTests + 4 PhaseDataServiceTests pass; status dot rendering confirmed; UUID continuity confirmed via ReconcileAsync.
2. Read `.gsd/milestones/M002/slices/S06/S06-SUMMARY.md` — extract: `[S06] Live word-count latency: 1 ms` (threshold <500ms); `[S06] Cold-start recalculation: 82 ms` (threshold <5s); 59/59 full suite pass.
3. Read `.gsd/milestones/M001/M001-VALIDATION.md` — extract platform/publish evidence (Windows + Linux CI matrix, self-contained dotnet publish, Book.txt parse <2s) for the R014 combined note.
4. Inspect `src/Hymnal/ViewModels/ChapterInfoViewModel.cs` — confirm `_proximityFill` and `_hasTarget` are plain backing fields (not OAPH), `WhenAnyValue` live subscriptions are present in the per-chapter subscription block, and `SyncFromChapterVm` copies both values on switch. This rebuts S03's 'stubbed 0.0' known limitation.
5. Call `gsd_requirement_update` for R003: set status=validated, add validation note: 'Validated by S01 evidence: 6 ChapterRegistryServiceTests + 4 PhaseDataServiceTests pass (dotnet test exit 0); sidebar status dot renders from ChapterViewModel.Status; phase-date prefill wired in WorkspaceViewModel; UUID continuity confirmed by code inspection of ChapterRegistryService.ReconcileAsync and the title-aware orphan matching path in WorkspaceViewModel (lines ~441-470).'  
6. Call `gsd_requirement_update` for R014: set status=validated, add validation note: 'Validated by combined M001 + M002 evidence. Platform/publish: M001-VALIDATION.md records Windows win-x64 and Linux linux-x64 CI matrix passing, self-contained dotnet publish verified, and Book.txt parse <2s confirmed. Performance: S06 benchmark (WordCountPerformanceTests) measured [S06] Live word-count latency: 1 ms for 10,000 words (threshold <500ms) and [S06] Cold-start recalculation: 82 ms for 100 chapters (threshold <5s); full suite 59/59 pass.'

Done when: Both gsd_requirement_update calls succeed without error; REQUIREMENTS.md no longer shows R003 or R014 as unmapped.

## Inputs

- `.gsd/milestones/M002/slices/S01/S01-SUMMARY.md`
- `.gsd/milestones/M002/slices/S06/S06-SUMMARY.md`
- `.gsd/milestones/M001/M001-VALIDATION.md`
- `src/Hymnal/ViewModels/ChapterInfoViewModel.cs`
- `.gsd/REQUIREMENTS.md`

## Expected Output

- `.gsd/REQUIREMENTS.md`

## Verification

test -s .gsd/REQUIREMENTS.md

## Observability Impact

None — REQUIREMENTS.md update is the only side effect.

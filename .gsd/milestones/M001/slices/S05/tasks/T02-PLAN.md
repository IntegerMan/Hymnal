---
estimated_steps: 11
estimated_files: 3
skills_used: []
---

# T02: Desktop smoke pass, timing evidence, and milestone assessment artifacts

Run the five-scenario integrated smoke pass against the real manuscript workspace (C:/Dev/EliAndGraceMakeAGame), capture cold-start time and Book.txt parse time, confirm CI matrix evidence, and produce the milestone closure artifacts:
1. S05-SMOKE-PASS.md — tabular pass/fail for all five scenarios plus timing
2. Updated S01-ASSESSMENT.md with real observed results
3. Updated S04-ASSESSMENT.md with smoke-pass evidence
4. Confirm M001-ROADMAP.md reflects all slices complete

Five scenarios:
1. Open workspace → sidebar renders with chapters in order
2. Click chapter → editor loads with syntax highlighting active
3. Edit text + Ctrl+S → file saved to disk
4. Toggle Notes panel → write note → reopen chapter → note persists
5. Close app → relaunch → last chapter restores silently in editor

## Inputs

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/EditorViewModel.cs`
- `src/Hymnal/ViewModels/NotesViewModel.cs`
- `.gsd/milestones/M001/slices/S01/S01-SUMMARY.md`
- `.gsd/milestones/M001/slices/S04/S04-SUMMARY.md`
- `.github/workflows/release.yml`

## Expected Output

- `.gsd/milestones/M001/slices/S05/S05-SMOKE-PASS.md`
- `.gsd/milestones/M001/slices/S01/S01-ASSESSMENT.md`
- `.gsd/milestones/M001/slices/S04/S04-ASSESSMENT.md`

## Verification

S05-SMOKE-PASS.md exists with all five scenarios marked pass/fail and timing captured. S01-ASSESSMENT.md and S04-ASSESSMENT.md exist with real evidence (not stubs). dotnet test tests/Hymnal.Core.Tests --nologo exits 0.

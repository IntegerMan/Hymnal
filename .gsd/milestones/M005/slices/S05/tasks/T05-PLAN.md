---
estimated_steps: 1
estimated_files: 4
skills_used: []
---

# T05: Harden cross-Part move path for nested Parts and post-commit failure recovery

Correct cross-Part replacement-path derivation so moves into nested Part folders keep the full target folder path, and close the post-commit manifest failure gap so a manifest-save failure does not leave the corkboard showing stale state after a committed move. Add automated coverage for nested-Part drops and the chosen manifest-failure recovery behavior.

## Inputs

- `.gsd/milestones/M005/slices/S05/S05-PLAN.md`
- `.gsd/milestones/M005/slices/S05/tasks/T02-SUMMARY.md`
- `.gsd/milestones/M005/slices/S05/tasks/T04-SUMMARY.md`
- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `src/Hymnal.Core/Services/BookTxtStructureService.cs`

## Expected Output

- `Nested-Part cross-Part moves preserve the full target folder path`
- `Manifest-save failures either roll back or reload to a truthful workspace state`
- `Passing full-solution verification evidence for S05`

## Verification

powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test Hymnal.slnx --nologo --verbosity minimal"

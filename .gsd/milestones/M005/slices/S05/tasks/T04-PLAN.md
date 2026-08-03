---
estimated_steps: 1
estimated_files: 1
skills_used: []
---

# T04: Restore Corkboard integration verification and re-prove real file movement

Fix the S05 integration test harness so the corkboard-focused regression suite compiles again, then re-run the real temp-workspace Corkboard integration coverage for same-Part reorder persistence, cross-Part file movement, reload continuity, and conflict failure visibility. Keep the harness aligned with actual ViewModel lifetimes rather than assuming WorkspaceViewModel is disposable.

## Inputs

- `.gsd/milestones/M005/slices/S05/S05-PLAN.md`
- `.gsd/milestones/M005/slices/S05/tasks/T04-SUMMARY.md`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`

## Expected Output

- `Compiling Corkboard integration test harness`
- `Passing focused corkboard regression evidence`

## Verification

powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter 'CorkboardViewModelIntegrationTests|CorkboardViewSmokeTests|CorkboardViewModelTests' --verbosity minimal"

## Observability Impact

Verifies the real failure path preserves inspectable `LastStructuralError` and notification text while leaving filesystem and Book.txt state unchanged.

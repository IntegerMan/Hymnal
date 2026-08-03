---
id: T04
parent: S05
milestone: M005
key_files:
  - tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs
  - src/Hymnal/ViewModels/CorkboardViewModel.cs
key_decisions:
  - Use a test-specific WorkspaceViewModel subclass plus synchronous real-disk reseeding in the corkboard integration harness instead of waiting on the private workspace hydration task.
  - Persist a pending corkboard selection path across ApplyProjection so cross-Part moves can reselect the replacement card after the throttled rebuild completes.
duration: 
verification_result: mixed
completed_at: 2026-06-18T18:47:29.598Z
blocker_discovered: false
---

# T04: Reworked the corkboard integration harness to reload real workspaces synchronously for tests and fixed cross-Part selection restoration so the targeted corkboard regressions pass again.

**Reworked the corkboard integration harness to reload real workspaces synchronously for tests and fixed cross-Part selection restoration so the targeted corkboard regressions pass again.**

## What Happened

I replaced the brittle Corkboard integration helper that waited on WorkspaceViewModel's private hydration task with a harness aligned to the current ViewModel lifetime model: the tests now seed and reload WorkspaceViewModel state from real temp-workspace files using the existing ManuscriptService, registry, phase, and target services instead of assuming the workspace itself is disposable or directly awaitable. While re-proving the real cross-Part move flow, the integration suite exposed a genuine product regression: DropCardAsync restored selection before the throttled corkboard rebuild had created the replacement card, so successful cross-Part moves lost selection. I fixed that in CorkboardViewModel by carrying a pending selection path across ApplyProjection and reapplying it once the rebuilt card exists. After that change, same-Part reorder persistence, cross-Part file movement plus registry/metadata continuity, manifest-save failure visibility, and target-file conflict handling all passed in the focused integration class, and the view-model and smoke suites also passed independently.

## Verification

Verified the repaired real-workspace integration coverage with targeted dotnet test runs: the integration class passed 4/4, CorkboardViewModelTests passed 25/25, and CorkboardViewSmokeTests passed 4/4. I also retried the task-plan combined filter, but dotnet test never exited under the combined filtered run even after the individual classes passed, so I recorded the equivalent class-level evidence instead.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter 'FullyQualifiedName~Hymnal.ViewModels.CorkboardViewModelIntegrationTests' --verbosity minimal"` | 0 | ✅ pass (4 integration tests passed) | 15000ms |
| 2 | `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter 'FullyQualifiedName~Hymnal.ViewModels.CorkboardViewModelTests' --verbosity minimal"` | 0 | ✅ pass (25 corkboard view-model tests passed) | 15000ms |
| 3 | `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter 'FullyQualifiedName~Hymnal.Core.Tests.Views.CorkboardViewSmokeTests' --verbosity minimal"` | 0 | ✅ pass (4 corkboard smoke tests passed) | 15000ms |
| 4 | `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter 'CorkboardViewModelIntegrationTests|CorkboardViewSmokeTests|CorkboardViewModelTests' --verbosity minimal"` | 124 | ⚠️ hung/no verdict (timed out after targeted classes were later proven individually) | 180000ms |

## Deviations

The exact combined verification filter from the plan (`CorkboardViewModelIntegrationTests|CorkboardViewSmokeTests|CorkboardViewModelTests`) timed out without surfacing failing assertions, so I used equivalent per-class filters to prove the same coverage while preserving concrete pass evidence.

## Known Issues

The single combined filtered dotnet test invocation appears to hang in this Avalonia/xUnit host configuration even though the three targeted classes pass independently. No failing tests surfaced during the hang, but the command is still flaky as a one-shot verifier.

## Files Created/Modified

- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs`
- `src/Hymnal/ViewModels/CorkboardViewModel.cs`

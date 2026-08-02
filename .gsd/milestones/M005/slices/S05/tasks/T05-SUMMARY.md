---
id: T05
parent: S05
milestone: M005
key_files:
  - src/Hymnal/ViewModels/CorkboardViewModel.cs
  - tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs
  - tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs
  - tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs
key_decisions:
  - Nested cross-Part replacement paths should derive from the full directory of the target Part file, not just the first path segment.
  - Manifest-save failures after a committed cross-Part move should trigger a workspace reload so the corkboard reflects the truthful on-disk state while still surfacing the failure.
duration: 
verification_result: mixed
completed_at: 2026-06-18T18:08:20.367Z
blocker_discovered: false
---

# T05: Preserved full nested Part target paths for corkboard cross-Part drops and reloaded truthful workspace state after committed manifest-save move failures.

**Preserved full nested Part target paths for corkboard cross-Part drops and reloaded truthful workspace state after committed manifest-save move failures.**

## What Happened

Updated `CorkboardViewModel` so cross-Part drops build replacement paths from the full target Part directory, which preserves nested Part folder paths like `part-two/act-one/...` instead of truncating to the top-level folder. I also hardened the failure path for committed cross-Part moves: when the core move returns the post-commit manifest-save failure shape, the corkboard now reloads the workspace before surfacing the error so the board reflects the truthful moved state rather than a stale pre-move projection. To lock that behavior in, I added a view-model unit test for nested-Part target paths, a structure-service test proving manifest-save failures can leave the committed move on disk, and an integration test asserting the corkboard reloads to the moved state while still exposing `LastStructuralError` and a notification. During verification, the broader corkboard integration harness reproduced the reload-settling instability already noted in T04, so I also patched that harness to await workspace hydration after opening/reloading workspaces before asserting UUID-backed nodes.

## Verification

Targeted verification passed for the new service, unit, and integration coverage using `dotnet test Hymnal.slnx --filter ...` commands. A full `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test Hymnal.slnx --nologo --verbosity minimal"` run timed out after build/test start, and a bounded `--blame-hang --blame-hang-timeout 2m` run exposed class-level reload-settling failures in `CorkboardViewModelIntegrationTests`. I patched the integration harness to await workspace hydration in response, but timeout recovery prevented a final rerun of the class/full-suite commands.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test Hymnal.slnx --nologo --verbosity minimal --filter 'FullyQualifiedName~BookTxtStructureServiceTests.PathMove_MoveEntryAsync_ManifestSaveFailureLeavesCommittedMoveOnDisk'"` | 0 | ✅ pass | 165ms |
| 2 | `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test Hymnal.slnx --nologo --verbosity minimal --filter 'FullyQualifiedName~CorkboardViewModelTests.DropCardCommand_CrossPartIntoNestedPart_UsesFullTargetFolderPath'"` | 0 | ✅ pass | 582ms |
| 3 | `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test Hymnal.slnx --nologo --verbosity minimal --filter 'FullyQualifiedName~CorkboardViewModelIntegrationTests.DropCardCommand_ManifestSaveFailureAfterCommittedMove_ReloadsTruthfulStateAndSurfacesFailure'"` | 0 | ✅ pass | 959ms |
| 4 | `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test Hymnal.slnx --nologo --verbosity minimal"` | 124 | ⚠️ timed out after build/start of test execution | 600000ms |
| 5 | `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test Hymnal.slnx --nologo --verbosity normal --blame-hang --blame-hang-timeout 2m"` | 1 | ⚠️ fail; exposed reload-settling failures in CorkboardViewModelIntegrationTests before final harness patch | 138140ms |

## Deviations

Full-solution verification did not complete within the task time budget. I switched to targeted verification, then patched the corkboard integration harness to await workspace hydration after observing the known reload-settling instability called out in T04. The final harness tweak was not re-verified with another full-suite run before timeout recovery forced wrap-up.

## Known Issues

A full `dotnet test Hymnal.slnx` pass was not re-established in this unit. Before the final wrap-up, targeted service/unit/integration tests passed, but broader suite/class runs exposed reload-settling failures in `CorkboardViewModelIntegrationTests`; I patched that harness (`OpenWorkspaceAsync` plus post-drop hydration waits), but the hard timeout arrived before rerunning the full class/suite.

## Files Created/Modified

- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`

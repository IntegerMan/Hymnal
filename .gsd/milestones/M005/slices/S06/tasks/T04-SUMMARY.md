---
id: T04
parent: S06
milestone: M005
key_files:
  - src/Hymnal/ViewModels/CorkboardItemViewModel.cs
  - tests/Hymnal.Core.Tests/ViewModels/CorkboardProjectionTests.cs
key_decisions:
  - Project manifest-excluded chapters directly from `WorkspaceViewModel` nodes and treat orphan discovery as a supplemental source rather than the primary excluded-card source.
  - Deduplicate orphan-discovery results against excluded workspace node paths so reloads cannot show the same excluded chapter twice.
duration: 
verification_result: mixed
completed_at: 2026-06-18T23:50:22.084Z
blocker_discovered: false
---

# T04: Fixed corkboard excluded-card projection to render manifest-excluded chapters immediately after reload and added focused projection coverage alongside the real-workspace integration proofs for include/exclude and inline-create persistence.

**Fixed corkboard excluded-card projection to render manifest-excluded chapters immediately after reload and added focused projection coverage alongside the real-workspace integration proofs for include/exclude and inline-create persistence.**

## What Happened

Reviewed the existing S05/S06 corkboard integration harness and confirmed the real-workspace include/exclude/inline-create scenarios were already present in `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs`. The remaining product gap was in `CorkboardItemViewModel.Project(...)`: excluded `WorkspaceViewModel` nodes were being skipped entirely, so the board depended on slower orphan-file rediscovery to show excluded cards after reload. I updated the projection to emit `ExcludedChapterCardItemViewModel` instances directly from `Node.IsExcluded` chapters, preserve their owning Part context, and de-duplicate matching orphan-discovery results so the same path cannot appear twice. I then extended `CorkboardProjectionTests` with focused coverage proving excluded workspace nodes appear immediately without waiting for orphan discovery and that duplicate orphan rows are suppressed when the workspace already carries the excluded node.

## Verification

Verified the real-workspace corkboard integration class passes, covering exclude/include persistence and inline-create reload scenarios on temp workspaces. Verified the new projection-focused tests pass, proving immediate excluded-card projection and orphan de-duplication. Also confirmed an attempted parallelized dotnet-test run can fail with Avalonia `obj/.../resources` file locking, so final verification was re-run serially with native `bash`/`dotnet test` instead of `gsd_exec` per the existing WSL dotnet gotcha.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `cmd.exe /c "dotnet test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewModelIntegrationTests --verbosity minimal"` | 1 | ❌ fail (known gsd_exec WSL dotnet/NuGet path issue, not product failure) | 7416ms |
| 2 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter "CorkboardViewModelIntegrationTests" --verbosity minimal` | 0 | ✅ pass | 0ms |
| 3 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter CorkboardProjectionTests --verbosity minimal` | 1 | ❌ fail (parallel Avalonia build file-lock contention) | 0ms |
| 4 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --no-restore --filter CorkboardProjectionTests --verbosity minimal` | 0 | ✅ pass | 0ms |

## Deviations

Did not modify `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs` because the required real-workspace coverage was already present locally; focused the code change on the remaining projection bug and added targeted projection tests. Verification used native `bash` `dotnet test` instead of `gsd_exec` because the known WSL-hosted dotnet restore issue reproduced immediately.

## Known Issues

`dotnet test` commands that build the Avalonia app in parallel can contend on `src/Hymnal/obj/Debug/net10.0/Avalonia/resources` or `ref/Hymnal.dll`; serial reruns pass.

## Files Created/Modified

- `src/Hymnal/ViewModels/CorkboardItemViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardProjectionTests.cs`

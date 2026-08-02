---
id: T01
parent: S08
milestone: M005
key_files:
  - tests/Hymnal.Core.Tests/ViewModels/StructuralConsistencyUatTests.cs
  - src/Hymnal/ViewModels/CorkboardItemViewModel.cs
key_decisions:
  - Use one integrated temp-workspace UAT to replay sidebar, Corkboard, and Gantt operations through actual ViewModel/Core paths, then assert disk-backed state after a fresh ViewModel stack restart.
  - Fix Corkboard projection duplication by treating Workspace excluded nodes as authoritative before orphan-file projection adds excluded cards.
duration: 
verification_result: passed
completed_at: 2026-06-19T04:47:16.181Z
blocker_discovered: false
---

# T01: Added an integrated structural UAT replay across sidebar, Corkboard, and Gantt, plus fixed duplicate excluded Corkboard projection on reload.

**Added an integrated structural UAT replay across sidebar, Corkboard, and Gantt, plus fixed duplicate excluded Corkboard projection on reload.**

## What Happened

Created `StructuralConsistencyUatTests` with a real temp workspace containing nested Parts, included chapters, a manifest-excluded orphan chapter, registry UUIDs, notes, phase data, targets, and word-count history. The test drives sidebar commands for remove/include, rename, chapter reorder, and Part-block reorder; Corkboard commands for same-Part reorder, cross-Part move, remove/include, and inline create; and Gantt row move for a same-Part reorder. It then simulates a restart with a fresh ViewModel stack and asserts the final Book.txt order, file locations, exclusions manifest, registry paths, UUID-keyed sidecar continuity, watcher suppression evidence, no duplicate workspace/card/row projections, no stale Corkboard structural error, and no user-facing errors. During verification the new UAT exposed duplicate Corkboard excluded-card projection after restart; fixed `CorkboardItemViewModel.Project` by pre-seeding excluded workspace paths before appending orphan cards so manifest-excluded nodes discovered later in nested Part traversal are not also rendered as orphan cards.

## Verification

Ran the targeted UAT verification using the required Windows dotnet SDK path with explicit working directory: `powershell.exe -NoProfile -Command "Set-Location 'C:\Dev\Hymnal'; & 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter StructuralConsistencyUatTests --verbosity minimal; if (\$LASTEXITCODE -ne 0) { exit \$LASTEXITCODE }"`. The final run restored/build/tested successfully and reported Passed: 1, Failed: 0. Existing analyzer warnings remain in older Corkboard/WorkspaceSidebarExclusion tests; the new test's own analyzer warning was cleaned up.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `powershell.exe -NoProfile -Command "Set-Location 'C:\Dev\Hymnal'; & 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter StructuralConsistencyUatTests --verbosity minimal; if (\$LASTEXITCODE -ne 0) { exit \$LASTEXITCODE }"` | 0 | ✅ pass — targeted StructuralConsistencyUatTests passed (1 passed, 0 failed) | 0ms |

## Deviations

The UAT uncovered and required a small production projection fix in `CorkboardItemViewModel.Project`; this was necessary for the new final no-duplicate assertion to pass and remained within the slice contract of not adding new structural write paths.

## Known Issues

Existing xUnit analyzer warnings remain in older tests (`CorkboardViewModelTests.cs`, `WorkspaceSidebarExclusionTests.cs`); they are unrelated to this task. `gsd_exec` still has a Windows PowerShell/WSL output and exit-code propagation quirk for direct dotnet invocations, so the successful verification evidence is from the reliable `bash` tool invocation with explicit PowerShell working directory.

## Files Created/Modified

- `tests/Hymnal.Core.Tests/ViewModels/StructuralConsistencyUatTests.cs`
- `src/Hymnal/ViewModels/CorkboardItemViewModel.cs`

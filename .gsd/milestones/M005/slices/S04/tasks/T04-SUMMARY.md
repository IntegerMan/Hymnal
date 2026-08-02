---
id: T04
parent: S04
milestone: M005
key_files:
  - tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarReorderTests.cs
key_decisions:
  - (none)
duration: 
verification_result: mixed
completed_at: 2026-06-18T13:12:38.041Z
blocker_discovered: false
---

# T04: Added integration proof that sidebar reorder Core failures notify the user and leave Book.txt/sidebar order unchanged, alongside existing reload-persistence coverage for chapter and Part moves.

**Added integration proof that sidebar reorder Core failures notify the user and leave Book.txt/sidebar order unchanged, alongside existing reload-persistence coverage for chapter and Part moves.**

## What Happened

Added a focused integration test to `WorkspaceSidebarReorderTests` that exercises the real WorkspaceViewModel reorder path through active Book.txt validation, then simulates a downstream Core `Result.Fail` from `IBookTxtStructureService.ReorderEntryAsync`. The test asserts the legal drag-style request reaches Core under watcher suppression, the user-visible notification includes the Core failure context, Book.txt remains unchanged, the visible sidebar order remains unchanged, and no duplicate sidebar nodes are present. Existing tests in the same file already cover chapter reorder reload persistence, Part block reorder reload persistence, excluded projection presence without duplication, and pre-Core illegal-drop rejection, so the added case closes the remaining integration failure path without expanding production code. No S04 UAT markdown was added because the repository has prior `.gsd` UAT artifacts for S01-S03 but no existing tracked S04 checklist or suitable tracked UAT artifact to append for this slice.

## Verification

Focused reorder-related suites were run with native Windows dotnet through PowerShell and passed: BookTxtStructureServiceTests, WorkspaceSidebarReorderTests, and SidebarViewSmokeTests reported 73 passed, 0 failed. Full solution verification was run with native Windows dotnet through PowerShell using `--no-build --blame-hang` after the minimal-output full command twice exceeded the harness timeout despite no failures; the blame-hang run completed the entire `Hymnal.slnx` test suite with 348 passed, 0 failed. The expected pre-existing CS4014 warning in WorkspaceViewModel appeared during the focused build and was not changed. A `gsd_exec` native-dotnet attempt reproduced the known WSL NuGet `Value cannot be null (Parameter 'path1')` environment issue recorded in project memory, so final pass evidence used the working Windows PowerShell shell.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Set-Location -LiteralPath 'C:\Dev\Hymnal'; & 'C:\Program Files\dotnet\dotnet.exe' test Hymnal.slnx --nologo --verbosity minimal --filter 'FullyQualifiedName~BookTxtStructureServiceTests|FullyQualifiedName~WorkspaceSidebarReorderTests|FullyQualifiedName~SidebarViewSmokeTests'"` | 0 | ✅ pass — 73 focused tests passed; existing CS4014 and xUnit analyzer warnings observed | 10000ms |
| 2 | `powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Set-Location -LiteralPath 'C:\Dev\Hymnal'; & 'C:\Program Files\dotnet\dotnet.exe' test Hymnal.slnx --no-build --nologo --verbosity normal --blame-hang --blame-hang-timeout 60s --blame-hang-dump-type none"` | 0 | ✅ pass — full solution suite passed: 348 passed, 0 failed | 8970ms |
| 3 | `gsd_exec: "/mnt/c/Program Files/dotnet/dotnet.exe" test Hymnal.slnx --nologo --verbosity minimal --filter 'FullyQualifiedName~BookTxtStructureServiceTests|FullyQualifiedName~WorkspaceSidebarReorderTests|FullyQualifiedName~SidebarViewSmokeTests'` | 1 | ⚠️ environment caveat — reproduced known WSL dotnet.exe NuGet restore failure: Value cannot be null (Parameter 'path1') | 1953ms |

## Deviations

Used Windows PowerShell via the interactive shell for final test verification because `gsd_exec` runs dotnet.exe from a WSL-style path in this environment and hits the known NuGet `path1` restore failure. Did not add UAT markdown because no suitable tracked S04 UAT artifact exists; prior UAT artifacts are `.gsd` planning outputs for earlier slices.

## Known Issues

The unrelated existing CS4014 warning in `src/Hymnal/ViewModels/WorkspaceViewModel.cs` still appears when building; per task plan it was inspected but not fixed. The plain minimal full-suite command timed out in the harness after test-run startup with no failures; `--blame-hang` showed all 348 tests complete successfully in about 9 seconds.

## Files Created/Modified

- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarReorderTests.cs`

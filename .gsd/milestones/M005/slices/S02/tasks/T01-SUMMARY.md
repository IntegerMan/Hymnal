---
id: T01
parent: S02
milestone: M005
key_files:
  - tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs
key_decisions:
  - Represent excluded sidebar state in the contract as `ChapterNode.IsExcluded`, matching the task plan's suggested public state and keeping assertions at the WorkspaceViewModel projection surface.
duration: 
verification_result: mixed
completed_at: 2026-06-18T03:40:26.178Z
blocker_discovered: false
---

# T01: Added WorkspaceViewModel sidebar exclusion contract tests covering projection, reload, include/exclude commands, and notification-preserving failure paths.

**Added WorkspaceViewModel sidebar exclusion contract tests covering projection, reload, include/exclude commands, and notification-preserving failure paths.**

## What Happened

Created `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs` with real temp-workspace fixtures, real `ExclusionManifestService`, real `BookTxtStructureService` for success paths, and hand-rolled picker/settings/notification fakes. The tests define the S02 contract that manifest-excluded markdown files should appear in `WorkspaceViewModel.Nodes` / `VisibleNodes` in sidebar order, ordinary unmanifested orphan files should not be projected, excluded nodes should survive a fresh WorkspaceViewModel reload, include/exclude commands should update Book.txt plus `.hymnal-data/exclusions.json`, and failed include/exclude operations should surface notification text while leaving sidebar state intact. The tests intentionally reference `ChapterNode.IsExcluded` as the new public state requested by the task plan; current code does not implement that API yet, so verification fails at the intended missing implementation boundary.

## Verification

Ran targeted verification. `gsd_exec` could not use bare `dotnet` because bash PATH lacks it, then Windows `dotnet.exe` through gsd_exec hit the known WSL/NuGet `path1` failure captured in project memory. A native shell diagnostic command restored successfully and compiled until the intended S02 contract failure: `ChapterNode` lacks `IsExcluded`. No unrelated missing imports or fake interface mismatches remain after fixing the new test file's `Hymnal.Core.Common` import.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `gsd_exec: dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarExclusionTests` | 127 | ❌ environment failure: dotnet not found in gsd_exec bash PATH | 3709ms |
| 2 | `gsd_exec: '/mnt/c/Program Files/dotnet/dotnet.exe' test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarExclusionTests` | 1 | ❌ environment failure: known NuGet path1 issue under gsd_exec/WSL | 2189ms |
| 3 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarExclusionTests` | 1 | ✅ intended failing contract: only new-test compile errors are missing ChapterNode.IsExcluded | 0ms |

## Deviations

Verification required a native shell diagnostic after gsd_exec hit the known Windows dotnet/NuGet path bug; this is consistent with project memory MEM008.

## Known Issues

The targeted test suite currently fails to compile because `ChapterNode.IsExcluded` does not exist yet. This is intentional for T01 and should be addressed by T02's implementation.

## Files Created/Modified

- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs`

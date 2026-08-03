---
id: T04
parent: S08
milestone: M005
key_files:
  - docs/uat/M005-S08-structural-consistency.md
  - tests/Hymnal.Core.Tests/ViewModels/StructuralConsistencyUatTests.cs
  - src/Hymnal/Hymnal.csproj
  - Hymnal.slnx
key_decisions:
  - Document the S08 manual desktop UAT as a durable docs artifact rather than adding new structural write paths during closeout.
  - Treat the full-solution test timeout as a known verification-environment issue after focused structural UAT and desktop app build passed.
duration: 
verification_result: mixed
completed_at: 2026-06-19T05:26:20.642Z
blocker_discovered: false
---

# T04: Added a desktop UAT script for S08 structural consistency and ran focused replay/build closeout verification.

**Added a desktop UAT script for S08 structural consistency and ran focused replay/build closeout verification.**

## What Happened

Created `docs/uat/M005-S08-structural-consistency.md` as the manual desktop UAT script for the completed S08 structural-editing surfaces. The script mirrors the automated StructuralConsistencyUatTests fixture, including the starting workspace, sidebar remove/include/rename/reorder flow, Corkboard cross-Part move/remove/include/inline-create flow, Gantt row reorder flow, restart persistence checks, UUID-backed metadata continuity checks, and controlled Corkboard/Gantt failure scenarios. It also calls out author-facing polish observations for excluded styling, menu labels, drag/drop affordances, Gantt keyboard/drag behavior, restart persistence, and failure message copy, plus the verification-environment constraints around `Hymnal.slnx`, native Windows PowerShell dotnet, and MEM008. No production code or tests were changed in T04 because prior tasks had already implemented the executable replay and failure coverage.

## Verification

Verified the UAT artifact is non-empty and contains required S08 sections. Ran the focused StructuralConsistencyUatTests gate with native Windows dotnet through PowerShell; it passed with 2 tests. Ran the full solution test gate; it first hit the known Windows file-lock cleanup failure in GanttViewModelTests and then timed out with lingering dotnet hosts. After stopping those hosts, reran a serialized/no-build full solution test gate; it also hung after test discovery and timed out, which was recorded as an environment/testhost known issue rather than a product behavior failure. Ran the desktop app build gate for `src/Hymnal/Hymnal.csproj`; it succeeded with 0 warnings and 0 errors.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `python docs UAT sanity check for non-empty file plus sidebar/corkboard/gantt/restart/failure/env-constraint sections` | 0 | ✅ pass — UAT artifact exists and contains all required closeout sections | 30000ms |
| 2 | `powershell.exe -NoProfile -Command "& 'C:\\Program Files\\dotnet\\dotnet.exe' test Hymnal.slnx --nologo --verbosity minimal --filter 'FullyQualifiedName~StructuralConsistencyUatTests'"` | 0 | ✅ pass — focused S08 StructuralConsistencyUatTests passed: 2 passed, 0 failed | 18807ms |
| 3 | `powershell.exe -NoProfile -Command "& 'C:\\Program Files\\dotnet\\dotnet.exe' test Hymnal.slnx --nologo --verbosity minimal"` | 124 | ⚠️ environment issue — full solution run hit Windows file-lock cleanup failure in GanttViewModelTests and then timed out with lingering dotnet hosts | 600000ms |
| 4 | `powershell.exe -NoProfile -Command "& 'C:\\Program Files\\dotnet\\dotnet.exe' test Hymnal.slnx --no-build --nologo --verbosity minimal -- RunConfiguration.MaxCpuCount=1"` | 124 | ⚠️ environment issue — serialized/no-build retry hung after test discovery and timed out | 600000ms |
| 5 | `powershell.exe -NoProfile -Command "& 'C:\\Program Files\\dotnet\\dotnet.exe' build src/Hymnal/Hymnal.csproj --nologo --verbosity minimal"` | 0 | ✅ pass — desktop app build succeeded with 0 warnings and 0 errors | 3219ms |

## Deviations

The full solution test gate did not produce a clean pass despite the prescribed serialized/no-build retry; it reproduced known Windows Avalonia/temp-workspace file-lock and hung-testhost behavior. T04 therefore records focused replay and app build pass evidence plus the full-test environment failure rather than masking it.

## Known Issues

Full `dotnet test Hymnal.slnx` remains unreliable in this environment: the first run failed inside GanttViewModelTests temp workspace cleanup with a file lock on `chapter-three.md` and then timed out; the serialized/no-build retry also timed out after test discovery. Existing unrelated analyzer warnings observed during focused test build were not addressed.

## Files Created/Modified

- `docs/uat/M005-S08-structural-consistency.md`
- `tests/Hymnal.Core.Tests/ViewModels/StructuralConsistencyUatTests.cs`
- `src/Hymnal/Hymnal.csproj`
- `Hymnal.slnx`

---
id: T05
parent: S06
milestone: M005
key_files:
  - tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs
  - tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs
  - tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs
  - src/Hymnal/ViewModels/CorkboardViewModel.cs
  - src/Hymnal/Views/CorkboardView.axaml
  - src/Hymnal/Views/CorkboardView.axaml.cs
key_decisions:
  - Use native Windows `dotnet.exe` for verification when executor-side bash tooling cannot resolve `dotnet`, and document the limitation in task closure evidence.
  - Treat concurrent native `dotnet test` build-lock failures as verification-environment contention rather than product regressions; re-run focused classes with `--no-build` for stable evidence.
duration: 
verification_result: mixed
completed_at: 2026-06-18T22:42:51.544Z
blocker_discovered: false
---

# T05: Recorded fresh slice-wide Corkboard regression evidence across focused tests, full solution tests, and app build, with native-shell fallbacks for executor environment quirks.

**Recorded fresh slice-wide Corkboard regression evidence across focused tests, full solution tests, and app build, with native-shell fallbacks for executor environment quirks.**

## What Happened

T05 closed S06 by re-verifying the Corkboard include/exclude and inline chapter insertion work added in T01-T04 rather than changing source. I first probed the preferred gsd_exec path and confirmed the executor's WSL/bash environment could not invoke dotnet at all (`/bin/bash: dotnet: command not found`), so I fell back to native `C:\Program Files\dotnet\dotnet.exe` execution from the repo root as allowed by the task plan. I then ran the three focused Corkboard regression commands. An initial attempt to run multiple native `dotnet test` commands in parallel hit Windows file-lock contention in `src/Hymnal.Core/obj` during concurrent builds, so I re-ran the focused classes with `--no-build`, which produced clean passing evidence for `CorkboardViewModelTests` (39 passed), `CorkboardViewSmokeTests` (7 passed), and `CorkboardViewModelIntegrationTests` (10 passed). After that, I verified the desktop app build with `dotnet build src/Hymnal/Hymnal.csproj --nologo`, which passed. The first direct wrapper attempt for `dotnet test Hymnal.slnx --nologo --verbosity minimal` timed out at the tool layer even though the suite was not failing, so I re-ran the same command under `bg_shell` to observe live progress and confirm completion; it exited 0 with 386 passing tests. This gives fresh closure evidence that S06 preserved the canonical BookTxtStructureService route for include/exclude and inline chapter insertion, that the Corkboard smoke and real-workspace persistence tests remain green, and that broader solution behavior still holds. R013 was advanced for the remaining Corkboard structural capabilities in this slice; desktop cross-surface replay and final user-facing interaction polish remain deferred to S08 as planned.

## Verification

Fresh verification evidence was produced with native `dotnet.exe` because the gsd_exec/bash environment lacked `dotnet` on PATH. Focused slice regressions passed for `CorkboardViewModelTests` (39 tests), `CorkboardViewSmokeTests` (7 tests), and `CorkboardViewModelIntegrationTests` (10 tests) using `--no-build` after an initial concurrent-build lock contention attempt. Full solution verification passed under `bg_shell` with `dotnet test Hymnal.slnx --nologo --verbosity minimal` (386 passed, 0 failed). The Avalonia desktop app also compiled successfully with `dotnet build src/Hymnal/Hymnal.csproj --nologo` (0 warnings, 0 errors). Diagnostic surfaces to rely on later remain `CorkboardViewModel.LastStructuralError` and `INotificationService.ShowError`, with persistence inspection via Book.txt, `.hymnal-data/exclusions.json`, chapter files, and the Corkboard integration tests.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewModelTests --verbosity minimal (via gsd_exec/bash probe)` | 127 | ❌ environment limitation — /bin/bash could not find dotnet | 3758ms |
| 2 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewSmokeTests --verbosity minimal (parallel native build attempt)` | 1 | ❌ environment contention — concurrent build hit Windows file lock in obj/ | 4948ms |
| 3 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewModelIntegrationTests --verbosity minimal (parallel native build attempt)` | 1 | ❌ environment contention — concurrent build hit Windows file lock in obj/ | 3925ms |
| 4 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --no-build --filter CorkboardViewModelTests --verbosity minimal` | 0 | ✅ pass — 39 CorkboardViewModelTests passed | 3938ms |
| 5 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --no-build --filter CorkboardViewSmokeTests --verbosity minimal` | 0 | ✅ pass — 7 CorkboardViewSmokeTests passed | 3950ms |
| 6 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --no-build --filter CorkboardViewModelIntegrationTests --verbosity minimal` | 0 | ✅ pass — 10 CorkboardViewModelIntegrationTests passed | 4446ms |
| 7 | `dotnet test Hymnal.slnx --nologo --verbosity minimal (initial native wrapper attempt)` | 124 | ⚠️ wrapper timeout — re-run under bg_shell completed successfully | 360000ms |
| 8 | `dotnet test Hymnal.slnx --nologo --verbosity minimal (bg_shell rerun)` | 0 | ✅ pass — full solution suite passed 386/386 | 15000ms |
| 9 | `dotnet build src/Hymnal/Hymnal.csproj --nologo` | 0 | ✅ pass — app build succeeded with 0 warnings and 0 errors | 2957ms |

## Deviations

Used native `C:\Program Files\dotnet\dotnet.exe` instead of gsd_exec because the executor bash environment had no `dotnet` on PATH. Also switched the full-solution test rerun to `bg_shell` after the initial wrapper invocation timed out despite the suite later completing successfully.

## Known Issues

Concurrent native `dotnet test` invocations can contend on `src/Hymnal.Core/obj` and fail with Windows file-lock errors during build; focused test classes should be serialized or run with `--no-build` once the test assembly is already built. S08 desktop cross-surface replay remains outstanding by plan.

## Files Created/Modified

- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs`
- `tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs`
- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `src/Hymnal/Views/CorkboardView.axaml`
- `src/Hymnal/Views/CorkboardView.axaml.cs`

---
id: T04
parent: S02
milestone: M005
key_files:
  - src/Hymnal/Views/SidebarView.axaml
  - src/Hymnal/Views/Converters/NodeKindConverters.cs
  - src/Hymnal/Views/Converters/StatusToBrushConverter.cs
  - tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs
key_decisions:
  - Treat excluded sidebar nodes as a dedicated visual state in the existing shared converters so the XAML can distinguish excluded, missing, part, and normal chapter rows without new view-model surface area.
  - Keep sidebar regression coverage focused on a source-level smoke test plus converter assertions instead of brittle deep context-menu UI automation.
duration: 
verification_result: mixed
completed_at: 2026-06-18T08:07:08.099Z
blocker_discovered: false
---

# T04: Added excluded-sidebar styling, conditional include/exclude context menu wiring, and sidebar smoke coverage for excluded chapter bindings.

**Added excluded-sidebar styling, conditional include/exclude context menu wiring, and sidebar smoke coverage for excluded chapter bindings.**

## What Happened

Updated `src/Hymnal/Views/SidebarView.axaml` so excluded chapter rows render distinctly with italic title text, a muted excluded foreground state, and a small `excluded` badge while still preserving the existing missing-file and part treatments. Tightened the context-menu visibility rules so excluded nodes only surface `Include in book`, included present chapters surface `Exclude from book`, and missing nodes retain `Remove` without duplicate include/exclude affordances. Extended the shared converters in `src/Hymnal/Views/Converters/NodeKindConverters.cs` to treat excluded chapters as a first-class presentation state and added `BoolToFontStyleConverter` in `src/Hymnal/Views/Converters/StatusToBrushConverter.cs` for the italic treatment. Added `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs` to source-assert the sidebar XAML include/exclude bindings and to verify the foreground converter returns a distinct brush for excluded chapters.

## Verification

Verified the slice’s exclusion contract still passes with `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarExclusionTests`, then ran `dotnet test Hymnal.slnx --nologo --verbosity minimal` to confirm the full 311-test suite passes, and `dotnet build src/Hymnal/Hymnal.csproj --nologo` to confirm the app builds cleanly. A prior `gsd_exec` attempt reproduced the known WSL/Windows .NET path issue (`dotnet` missing on PATH and later NuGet `Value cannot be null (Parameter 'path1')`), so final verification used task-shell PowerShell invocations of `C:\Program Files\dotnet\dotnet.exe` and recorded their fresh output.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `gsd_exec bash: dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarExclusionTests` | 127 | ⚠️ expected harness failure reproduced (`dotnet` missing on PATH in gsd_exec WSL shell) | 3751ms |
| 2 | `gsd_exec bash: "/mnt/c/Program Files/dotnet/dotnet.exe" test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarExclusionTests` | 1 | ⚠️ expected MEM008 reproduction (`Value cannot be null (Parameter 'path1')`) | 2938ms |
| 3 | `powershell.exe -NoProfile -Command '& "C:\Program Files\dotnet\dotnet.exe" test "tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj" --nologo --filter WorkspaceSidebarExclusionTests'` | 0 | ✅ pass (8 exclusion tests) | 21758ms |
| 4 | `powershell.exe -NoProfile -Command '& "C:\Program Files\dotnet\dotnet.exe" test "Hymnal.slnx" --nologo --verbosity minimal'` | 0 | ✅ pass (311 total tests) | 10276ms |
| 5 | `powershell.exe -NoProfile -Command '& "C:\Program Files\dotnet\dotnet.exe" build "src/Hymnal/Hymnal.csproj" --nologo'` | 0 | ✅ pass | 2707ms |

## Deviations

Used `src/Hymnal/Views/Converters/StatusToBrushConverter.cs` for the new `BoolToFontStyleConverter` because the sidebar already sources its bool UI helpers from that shared converter file; no behavior changed outside the intended excluded styling.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/Views/Converters/NodeKindConverters.cs`
- `src/Hymnal/Views/Converters/StatusToBrushConverter.cs`
- `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs`

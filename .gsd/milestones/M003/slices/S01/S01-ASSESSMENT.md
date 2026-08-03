---
sliceId: S01
uatType: browser-executable
verdict: FAIL
date: 2026-06-01T18:53:13.363Z
---

# UAT Result — S01

## Checks

| Check | Mode | Result | Notes |
|-------|------|--------|-------|
| Build the app project and run the Gantt view-model tests | runtime | FAIL | `cmd.exe /c dotnet test Hymnal.sln --filter GanttViewModelTests -nologo` failed during restore with `NuGet.targets(782,5): error : Value cannot be null. (Parameter 'path1')` for `src/Hymnal.Core/Hymnal.Core.csproj`, `tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj`, and `src/Hymnal/Hymnal.csproj`. |
| Read-only chapter timeline renders | human-follow-up | NEEDS-HUMAN | The slice summary and prior verification artifacts indicate this behavior passed earlier, but I could not live-verify the Gantt UI in this session because the build/test verification path is currently blocked by the restore error above and no browser/runtime UI tool is available here. |
| Missing or invalid dates stay visible | human-follow-up | NEEDS-HUMAN | Prior slice evidence says fallback rows and muted placeholders were implemented, but this session could not re-open the UI to confirm the rendering directly. |
| In-session phase changes refresh the projection | human-follow-up | NEEDS-HUMAN | Prior slice evidence says `PhaseData` changes trigger projection refreshes, but the live UI path could not be exercised in this session. |
| All chapters lack valid dates still shows a usable axis and muted rows | human-follow-up | NEEDS-HUMAN | Prior slice evidence says the renderer falls back to a sensible axis for all-invalid data, but this was not live-tested here. |

## Overall Verdict

FAIL — the required build/test smoke verification could not complete in this environment because `dotnet restore` fails with a `path1` null error, so the slice cannot be UAT-passed from this session.

## Notes

- Existing slice artifacts already record a previous passing verification run for the Gantt feature, including `dotnet test Hymnal.sln --filter GanttViewModelTests -nologo`, `dotnet test Hymnal.sln -nologo`, and `dotnet build src/Hymnal/Hymnal.csproj -nologo`.
- This UAT session attempted fresh verification via `cmd.exe /c dotnet test Hymnal.sln --filter GanttViewModelTests -nologo` and `cmd.exe /c dotnet build src/Hymnal/Hymnal.csproj -nologo`, but both hit the same restore failure.
- The failure appears environmental rather than feature-specific, but the UAT contract requires fresh verification evidence in this session, so the overall result remains FAIL.

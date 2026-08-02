---
estimated_steps: 9
estimated_files: 5
skills_used: []
---

# T04: Document desktop UAT script and close verification

Why: Automated ViewModel/Core replay is necessary but the sketch specifically calls for a desktop UAT script that an author or reviewer can perform across sidebar, Corkboard, and Gantt after app restart. This task captures the exact manual scenario and runs the full closeout gate.

Expected executor skills/frontmatter: `write-docs`, `verify-before-complete`.

Do:
1. Create a concise manual UAT script at `docs/uat/M005-S08-structural-consistency.md` (or the nearest existing docs/UAT folder if one exists) using the same fixture story as T01: starting workspace layout, sidebar operations, Corkboard operations, Gantt operations, app restart, expected final state, and controlled failure case.
2. Include explicit observations for author-facing polish: excluded styling, menu labels, drag/drop affordances, Gantt keyboard/drag behavior, restart persistence, and failure message copy.
3. State assumptions and known verification-environment constraints: use `Hymnal.slnx`, prefer native Windows dotnet via PowerShell, and do not rely on WSL `gsd_exec` dotnet restore when it reproduces MEM008.
4. Run focused S08 verification, full solution tests, and desktop app build. If full solution tests hit known concurrent Avalonia obj locks, rerun serialized/no-build as prior slices did and record the fresh command evidence in the task summary.
5. Do not change scope beyond final UAT polish; if manual script uncovers a feature request outside the sketch, record it as follow-up rather than implementing it in S08.

Done when: the UAT script is non-empty and reviewable, focused replay passes, full solution verification passes, and app build succeeds.

## Inputs

- `tests/Hymnal.Core.Tests/ViewModels/StructuralConsistencyUatTests.cs`
- `src/Hymnal/Hymnal.csproj`
- `Hymnal.slnx`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `src/Hymnal/ViewModels/GanttViewModel.cs`

## Expected Output

- `docs/uat/M005-S08-structural-consistency.md`
- `tests/Hymnal.Core.Tests/ViewModels/StructuralConsistencyUatTests.cs`

## Verification

powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test Hymnal.slnx --nologo --verbosity minimal"

## Observability Impact

Adds a human inspection artifact for final assembly and records the verification commands future agents should repeat to distinguish product failures from the known WSL/dotnet environment issue.

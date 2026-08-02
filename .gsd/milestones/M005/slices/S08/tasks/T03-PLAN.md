---
estimated_steps: 9
estimated_files: 5
skills_used: []
---

# T03: Polish controlled failure visibility across surfaces

Why: The sketch explicitly requires at least one simulated or controlled failure case plus final error copy polish. Prior slices already have Core rollback messages, sidebar notifications, and Corkboard `LastStructuralError`; S08 must verify that integrated failure states are user-visible and non-destructive across the real surfaces.

Expected executor skills/frontmatter: `tdd`, `verify-before-complete`.

Do:
1. Add or extend focused tests in `StructuralConsistencyUatTests` to trigger a controlled failure after some successful cross-surface operations. Prefer deterministic local failures already supported by prior slices: target-file conflict on a Corkboard cross-Part move, invalid include path, illegal sidebar/Gantt cross-Part reorder, or a fake structure-service failure injected through existing test seams.
2. Assert failure leaves `Book.txt`, moved/renamed files, registry, exclusions manifest, and surface projection recoverable and unchanged where rollback is expected; if a prior accepted committed-move failure is tested, assert the truthful reloaded state instead of pretending rollback occurred.
3. Assert user-facing failure copy is actionable and consistent enough for an author: operation name, affected path or row, and a clear reason. Use existing notification fakes and `CorkboardViewModel.LastStructuralError` where available.
4. Polish message text only where tests reveal vague or inconsistent copy. Avoid broad redesign of notification UX.
5. Keep failure messages free of secrets and OS-specific temp-root noise except where a file path is necessary to identify the manuscript-relative target.

Done when: the controlled failure path is part of the executable S08 gate and proves both no silent partial state and visible author-facing diagnostics.

## Inputs

- `tests/Hymnal.Core.Tests/ViewModels/StructuralConsistencyUatTests.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `src/Hymnal/ViewModels/GanttViewModel.cs`
- `src/Hymnal.Core/Services/BookTxtStructureService.cs`

## Expected Output

- `tests/Hymnal.Core.Tests/ViewModels/StructuralConsistencyUatTests.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `src/Hymnal/ViewModels/GanttViewModel.cs`
- `src/Hymnal.Core/Services/BookTxtStructureService.cs`

## Verification

powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter StructuralConsistencyUatTests --verbosity minimal"

## Observability Impact

Makes structural failures inspectable through notification fakes, `LastStructuralError`, manuscript-relative paths, and persisted file-state assertions instead of relying on silent command failures.

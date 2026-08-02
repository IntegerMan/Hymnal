---
estimated_steps: 10
estimated_files: 4
skills_used: []
---

# T01: Add cross-surface UAT replay test

Why: S02-S07 proved individual surfaces, but the S08 sketch requires one integrated replay across sidebar, Corkboard, and Gantt on the same workspace with restart persistence and UUID continuity. This task owns R013's final cross-surface replay and R005's Gantt participation.

Expected executor skills/frontmatter: `tdd`, `verify-before-complete`.

Do:
1. Add a focused test class such as `StructuralConsistencyUatTests` under `tests/Hymnal.Core.Tests/ViewModels/` or an equivalent existing test namespace.
2. Build a real temp workspace fixture with nested Parts, included chapters, one excluded/orphan chapter, notes, phase/target/history metadata, and registry UUIDs for chapters that will be renamed, moved, excluded, included, and reordered.
3. Drive the actual ViewModel/Core paths rather than editing files directly: sidebar commands through `WorkspaceViewModel` for include/exclude, rename, chapter reorder, and Part-block reorder; Corkboard commands through `CorkboardViewModel` for same-Part reorder, cross-Part move, include/exclude, and inline create; Gantt commands through `GanttViewModel` for same-Part row reorder.
4. Simulate restart persistence by constructing a fresh workspace/ViewModel stack after the replay and asserting the final projection from disk, not just in-memory collections.
5. Assert final `Book.txt` order, file locations, `.hymnal-data/exclusions.json`, `.hymnal-data/registry.json`, note/phase/target/history continuity by UUID, no duplicate nodes/cards/rows, and no stale structural errors after successful operations.
6. Do not introduce a second structural write path; if a helper is needed, keep it in tests or reuse existing fixture helpers.

Done when: the new focused UAT replay fails before any missing fixes and then passes with all assertions proving one coherent post-restart manuscript state.

## Inputs

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `src/Hymnal/ViewModels/GanttViewModel.cs`
- `src/Hymnal.Core/Services/BookTxtStructureService.cs`
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs`
- `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs`
- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarReorderTests.cs`

## Expected Output

- `tests/Hymnal.Core.Tests/ViewModels/StructuralConsistencyUatTests.cs`

## Verification

powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter StructuralConsistencyUatTests --verbosity minimal"

## Observability Impact

Adds the primary inspection surface for final assembly: a single test can localize inconsistencies to Book.txt, file layout, registry, exclusions, metadata continuity, or surface projection after reload.

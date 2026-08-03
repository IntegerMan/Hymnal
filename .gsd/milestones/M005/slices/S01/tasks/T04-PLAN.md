---
estimated_steps: 7
estimated_files: 4
skills_used: []
---

# T04: Close Core contract integration

Expected executor skills: verify-before-complete.

Why: S01 changes a public Core service contract used by Avalonia ViewModels and later slices. The final task must prove existing consumers still compile and that the full Core contract is tested together rather than only by narrow filters.

Do: Update any DI registration or existing callers affected by constructor or interface changes, especially `src/Hymnal/App.axaml.cs` and ViewModels that consume `IBookTxtStructureService`. Keep UI behavior unchanged. Add or adjust broad integration-style Core tests that exercise a realistic workspace with Book.txt, Part folders, exclusions, registry, include, exclude, move, and reload-style reads. Confirm no tests read `.gsd`, `.planning`, or other gitignored planning directories. If `Hymnal.sln` remains absent, use the actual project files for verification and do not recreate a solution file as part of this slice.

Done when: The full Core test project passes and the desktop app project compiles against the updated Core interface.

Failure modes: stale DI constructor registration, interface method additions breaking ViewModels, tests passing narrowly while app compile fails, accidental reliance on planning files.
Load profile: no new runtime load; this is compile and test closure.
Negative tests: full suite includes the failure paths from T01 through T03 and app compile catches consumer mismatches.

## Inputs

- `src/Hymnal/App.axaml.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`
- `src/Hymnal.Core/Services/BookTxtStructureService.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`

## Expected Output

- `src/Hymnal/App.axaml.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`

## Verification

dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj && dotnet build src/Hymnal/Hymnal.csproj

## Observability Impact

Confirms all new failure diagnostics remain available through the Core Result contract and that no UI layer introduces a second structural write path.

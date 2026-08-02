---
estimated_steps: 10
estimated_files: 3
skills_used: []
---

# T03: Wired Corkboard Part headers and empty-Part hints into drag/drop and routed all card drops through rich CorkboardDropRequest helpers.

Executor skills_used frontmatter: `tdd`, `verify-before-complete`.

Why: The view already starts card drags and handles drops onto chapter-card buttons, but the sketch specifically requires position mapping around Part dividers and empty Part regions. Actual user drags must reach the ViewModel request shape from T02 without introducing file writes in code-behind.

Do:
1. Update `src/Hymnal/Views/CorkboardView.axaml.cs` so drops can target chapter cards, Part divider headers, and `EmptyPartHintItemViewModel` regions. Code-behind must only map UI events to `CorkboardViewModel` commands/helpers; it must not write files or call Core services.
2. Add or expose small testable helpers for drop eligibility and request creation, similar to S04 SidebarView predicates. Included present chapter cards can be drag sources; excluded/missing cards, inline create items, and non-chapter items cannot be drag sources.
3. For card targets, keep before/after indicator behavior but base the request on the richer T02 target model rather than chapter-only list indexes.
4. For Part divider and empty-Part targets, request a drop into that Part. Empty Part drops must not require a child card. Collapsed Part behavior should still allow dropping into the Part header because hidden child cards are not available as targets.
5. Update AXAML if necessary to attach `Loaded`/drop handlers to Part divider and empty hint templates while preserving existing collapse/expand and add-button behavior.
6. Add or extend `tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs` to assert XAML loads with the new target handlers/helpers and to document the manual desktop smoke steps for real drag: same-Part reorder, cross-Part move, drop into an empty Part, reload/restart verification, and invalid drop feedback.

Done when: view smoke tests pass, code-behind remains a command mapper, and the manual checklist explicitly covers actual pointer drag behavior for S05.

## Inputs

- `src/Hymnal/Views/CorkboardView.axaml.cs`
- `src/Hymnal/Views/CorkboardView.axaml`
- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `src/Hymnal/ViewModels/CorkboardItemViewModel.cs`
- `tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs`

## Expected Output

- `src/Hymnal/Views/CorkboardView.axaml.cs`
- `src/Hymnal/Views/CorkboardView.axaml`
- `tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs`

## Verification

powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewSmokeTests --verbosity minimal"

## Observability Impact

Adds UI-level negative coverage for ignored drag sources/targets and documents the user-visible error checks required during desktop UAT.

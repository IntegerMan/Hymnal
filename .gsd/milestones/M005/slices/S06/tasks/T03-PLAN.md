---
estimated_steps: 10
estimated_files: 3
skills_used: []
---

# T03: Wire Corkboard menus and excluded styling smoke coverage

Skills expected: verify-before-complete.

Why: R013 is user-facing: authors must see excluded cards, invoke Include in Book, and start insertion from card/Part/board controls. This task keeps UI code-behind thin while proving the XAML and helper methods expose the ViewModel capabilities delivered in T01 and T02.

Do:
1. Update CorkboardView.axaml only as needed so excluded cards have a distinct excluded visual treatment and an Include in Book action, while included chapter cards expose Remove from Book and New Chapter actions without making Part or inline-create items draggable.
2. Update CorkboardView.axaml.cs only as needed so context menu handlers compute insertion indexes using CorkboardViewModel.GetInsertIndexAfterChapter, GetInsertIndexAfterPart, or GetBookChapterInsertIndex and execute the existing ViewModel commands.
3. Add or extend CorkboardViewSmokeTests to assert XAML contains the include/exclude/inline-create affordances and that drag helpers reject ExcludedChapterCardItemViewModel and InlineCreateItemViewModel as drag sources.
4. Add smoke/helper tests for IncludeExcludedCard routing: excluded cards owned by a Part use IncludeExistingChapterRequest with PartPath; book-level excluded cards use an explicit index.
5. Add smoke/helper tests for card and Part new-chapter menu routing where practical without opening real dialogs; helper methods should remain deterministic and testable.
6. Do not move business logic into code-behind; code-behind may translate UI events into ViewModel request records only.

Done when: Smoke tests prove the Corkboard UI exposes inclusion toggle and insertion affordances, excluded styling exists, and helper routing remains a thin command adapter.

## Inputs

- `src/Hymnal/Views/CorkboardView.axaml`
- `src/Hymnal/Views/CorkboardView.axaml.cs`
- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs`

## Expected Output

- `src/Hymnal/Views/CorkboardView.axaml`
- `src/Hymnal/Views/CorkboardView.axaml.cs`
- `tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs`

## Verification

dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewSmokeTests --verbosity minimal

## Observability Impact

Ensures visible UI state and actions expose excluded-card status and structural failure pathways rather than hiding unsupported operations.

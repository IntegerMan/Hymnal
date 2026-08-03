---
estimated_steps: 11
estimated_files: 3
skills_used: []
---

# T02: Project chapters into live corkboard card items

Expected executor skills: tdd, verify-before-complete.

Why: The corkboard must display live metadata from existing `ChapterViewModel` instances rather than introduce a second manuscript model. A focused projection layer makes cards testable before the full board UI exists.

Do:
1. Add `CardViewModel` wrapping a chapter `ChapterViewModel` and exposing title, relative path, status, status display/brush key if useful, word count display, target display, proximity fill, phase start/end display, missing state, and selected state. Subscribe to the exact source properties used (`Node`, `Status`, `WordCount`, `WordCountKnown`, `Target`, `PhaseData`) and dispose subscriptions through `ViewModelBase.Disposables`.
2. Add `CorkboardItemViewModel` or equivalent discriminated item for mixed board rows: Part divider item, empty-Part hint item, and chapter card item. Part items wrap Part `ChapterViewModel`s but are not selectable cards.
3. Preserve empty Part visibility by generating a hint item when a Part is followed by another Part or end-of-list with no intervening chapters.
4. Add tests using the existing ReactiveUI test initialization pattern from `GanttViewModelTests`: projection maps title/status/word count/target/date fields, `No target` is shown when target is absent, Part divider items are not selectable cards, empty-Part hints are emitted, and changes to `ChapterViewModel.ApplyPhaseData`, `UpdateWordCount`, `UpdateNode`, and target updates refresh the card.

Failure Modes (Q5): if a chapter has null/missing phase or target data, show safe fallback text rather than hiding rows; missing chapter files remain visible with a missing-state marker.
Load Profile (Q6): one lightweight subscription set per card; verify no rebuild loop is required for metadata updates and dispose old card subscriptions when board items are rebuilt.
Negative Tests (Q7): no chapters, consecutive Parts, missing chapter node, absent target, invalid/missing phase dates.
Done when: card/item projection tests prove display completeness and live refresh without touching Book.txt or UI AXAML.

## Inputs

- `src/Hymnal/ViewModels/ChapterViewModel.cs`
- `src/Hymnal.Core/Models/ChapterNode.cs`
- `src/Hymnal.Core/Models/PhaseData.cs`
- `src/Hymnal.Core/Models/WordCountTarget.cs`
- `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs`

## Expected Output

- `src/Hymnal/ViewModels/CardViewModel.cs`
- `src/Hymnal/ViewModels/CorkboardItemViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardProjectionTests.cs`

## Verification

dotnet test Hymnal.sln --filter FullyQualifiedName~CorkboardProjectionTests

## Observability Impact

Projection objects expose explicit display properties and item kind so UI failures can be localized to card projection versus shell wiring.

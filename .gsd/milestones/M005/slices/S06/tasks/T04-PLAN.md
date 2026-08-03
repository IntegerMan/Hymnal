---
estimated_steps: 11
estimated_files: 3
skills_used: []
---

# T04: Prove real workspace persistence for Corkboard inclusion and insertion

Skills expected: tdd, verify-before-complete.

Why: The slice demo is not complete until the canonical Core path updates actual Book.txt, chapter files, exclusions manifest, and the reloaded Corkboard projection. S05 established the real temp-workspace integration harness; this task extends it to the two remaining Corkboard structural capabilities.

Do:
1. Add integration tests using real temp workspaces and the real BookTxtStructureService, ExclusionManifestService, ChapterRegistryService, ManuscriptService, WorkspaceViewModel, and CorkboardViewModel composition pattern from S05.
2. Test excluding an included Corkboard card: Book.txt no longer lists the chapter, `.hymnal-data/exclusions.json` lists the normalized path, the reloaded board shows an ExcludedChapterCardItemViewModel with excluded styling state, and the card cannot be dragged/opened as an included chapter.
3. Test including that excluded Corkboard card back at a selected index between two chapters: Book.txt order matches the requested visual position, the manifest no longer lists the path, and a fresh WorkspaceViewModel/CorkboardViewModel reload projects the chapter as included.
4. Test creating a new chapter between two existing cards: the markdown file is created with the expected heading content, Book.txt inserts the new relative path at the exact all-entry index, registry reconciliation assigns one stable UUID, and a fresh reload preserves the card order.
5. Test creating a new chapter around Part dividers, including an empty Part and a nested Part folder: Book.txt order and on-disk path should match the target Part folder semantics from S05.
6. Test a duplicate create or invalid include failure over the real services: Book.txt, target files, registry, and exclusions manifest remain recoverable, LastStructuralError is populated, and INotificationService.ShowError is called.
7. Run the full focused Corkboard integration class. If combined Avalonia/xUnit filtering hangs as noted in S05, run the individual new test methods or per-class commands and record equivalent evidence.

Done when: Real-workspace tests prove include/exclude and insertion survive reload/restart with correct Book.txt order, filesystem state, exclusion manifest, and failure diagnostics.

## Inputs

- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs`
- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `src/Hymnal.Core/Services/BookTxtStructureService.cs`
- `src/Hymnal.Core/Services/ExclusionManifestService.cs`
- `src/Hymnal.Core/Services/ChapterRegistryService.cs`
- `.gsd/milestones/M005/slices/S01/S01-SUMMARY.md`
- `.gsd/milestones/M005/slices/S02/S02-SUMMARY.md`
- `.gsd/milestones/M005/slices/S05/S05-SUMMARY.md`

## Expected Output

- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs`
- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `src/Hymnal.Core/Services/BookTxtStructureService.cs`

## Verification

dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewModelIntegrationTests --verbosity minimal

## Observability Impact

Provides real failure-path proof that LastStructuralError, notifications, and disk artifacts localize include/create failures without silent partial state.

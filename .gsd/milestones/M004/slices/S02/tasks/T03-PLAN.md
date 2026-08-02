---
estimated_steps: 11
estimated_files: 6
skills_used: []
---

# T03: Wire SupplementalDocsViewModel into workspace and shell navigation

Why: after Core and editor support exist, the slice must make docs available as a user-facing sidebar section with create/edit/folder support and route selection into Write mode. This task closes most of R007 at ViewModel/DI level without depending on final AXAML polish. Executor skills frontmatter: `bmad-quick-dev`, `tdd`, `verify-before-complete`.

Do:
1. Add `SupplementalDocsViewModel` in `src/Hymnal/ViewModels/` that depends on `WorkspaceViewModel`, `ISupplementalDocsService`, `EditorViewModel`, and `INotificationService`.
2. Expose an observable tree/list suitable for binding in a DOCS sidebar section. Use the separate `SupplementalDocNode` model from T01; do not add docs to `WorkspaceViewModel.Nodes`, `ChapterNode`, or `NodeKind`.
3. Refresh the docs tree when a workspace opens/reloads/closes. If `WorkspaceViewModel` lacks a clean workspace-state observable/event, add the narrowest public event or property notification needed; keep existing chapter reload behavior intact.
4. Add commands for create folder, create file, refresh, and select/open doc. Create commands should call `ISupplementalDocsService`, update the tree, and surface `Result.Fail` through notifications.
5. Selection/open should save the current editor buffer first if `EditorViewModel.IsDirty`; if save fails, abort the doc switch and keep the previous editor state. On success, call `EditorViewModel.OpenArbitraryFileAsync`, clear manuscript chapter selection without triggering unwanted chapter open, and request/activate `ShellMode.Write` through `MainWindowViewModel` wiring.
6. Extend `MainWindowViewModel` to own the docs ViewModel and subscribe to a doc-open request or command outcome so docs always open in Write mode, matching the S01 corkboard open-to-Write pattern.
7. Register `ISupplementalDocsService`, `SupplementalDocsViewModel`, and any required factories in `src/Hymnal/App.axaml.cs`, preserving the existing DI ordering constraint that `EditorViewModel` is registered before `WorkspaceViewModel`.
8. Add tests in `tests/Hymnal.Core.Tests/ViewModels/SupplementalDocsViewModelTests.cs` and/or `MainWindowSupplementalDocsTests.cs` covering workspace load tree refresh, create folder/file, failed create notification, doc open sets editor arbitrary-file state and Write mode, dirty chapter save before doc open, and dirty doc save before chapter switch.

Done when: ViewModel tests prove docs are separate from chapters, commands create and reload tree entries under `.hymnal-data/docs/`, opening a doc uses `OpenArbitraryFileAsync`, failed saves abort switches, and MainWindow routing activates Write mode.

## Inputs

- `src/Hymnal.Core/Models/SupplementalDocNode.cs`
- `src/Hymnal.Core/Interfaces/ISupplementalDocsService.cs`
- `src/Hymnal.Core/Services/SupplementalDocsService.cs`
- `src/Hymnal/ViewModels/EditorViewModel.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/App.axaml.cs`

## Expected Output

- `src/Hymnal/ViewModels/SupplementalDocsViewModel.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/App.axaml.cs`
- `tests/Hymnal.Core.Tests/ViewModels/SupplementalDocsViewModelTests.cs`
- `tests/Hymnal.Core.Tests/ViewModels/MainWindowSupplementalDocsTests.cs`

## Verification

dotnet test Hymnal.sln --filter SupplementalDocs

## Observability Impact

Surfaces docs load/create/open failures through `INotificationService` and keeps tree state inspectable from `SupplementalDocsViewModel`, reducing ambiguity when the sidebar does not show an expected file.

# S02: Supplemental Docs Sidebar and Editor Path

**Goal:** Add a separate supplemental-docs tree under `.hymnal-data/docs/` that appears as a DOCS sidebar section, supports creating folders and files, opens documents in the existing editor with `ActiveNode == null`, and preserves edits through the existing `IMetadataStore.WriteTextAtomicAsync` save path across workspace reopen.
**Demo:** Create a docs file from the DOCS section, type content, save it through the editor, reopen the workspace, and verify the doc remains visible with intact content.

## Must-Haves

- Demo is true when a user can open a workspace, use the DOCS sidebar section to create a folder and markdown/text file under `.hymnal-data/docs/`, click that doc to open it in Write mode through the existing editor, type content, save, switch between chapter and doc files without losing dirty edits, close/reopen the workspace, and see the doc still listed with the saved content intact. R007 is advanced by every task in this slice. The plan stays within D018: supplemental docs use a separate docs model/tree and do not add docs to `ChapterNode` or `NodeKind`. Executor skills expected in task frontmatter: `bmad-quick-dev`, `tdd`, and `verify-before-complete`.

## Proof Level

- This slice proves: This slice proves integration: Core filesystem/document-tree behavior, EditorViewModel arbitrary-file lifecycle, Workspace/MainWindow shell routing, and Avalonia sidebar smoke construction are covered by automated tests. Real runtime required: yes for final manual smoke. Human/UAT required: yes, one desktop smoke after tests because this is a UI sidebar/editor workflow.

## Integration Closure

S02 composes a new Core docs service, editor arbitrary-file path, docs ViewModel, DI registration, and sidebar UI. It consumes S01's first-class shell mode pattern by activating Write mode when docs open, but does not modify corkboard behavior.

## Verification

- Runtime signals are existing user-facing notification banners and editor state (`ActiveFilePath`, `ActiveNode`, `IsDirty`, `HasConflict`, `IsBookSelected`). New failure paths should surface clear `INotificationService.ShowError` messages for invalid doc names, create/read/write failures, and failed switch-save operations; do not silently swallow docs-service errors. Inspection surfaces are the DOCS sidebar tree, `.hymnal-data/docs/` on disk, and focused unit/smoke test names. No secrets are handled; document contents are user-authored local files.

## Tasks

- [x] **T01: Implement supplemental docs tree service with filesystem-safe create and load behavior** `est:1.5h`
  Why: R007 needs a separate docs tree rooted at `.hymnal-data/docs/` and D018 explicitly forbids expanding `ChapterNode` or `NodeKind` for docs. This task establishes the Core contract and test coverage before UI wiring. Executor skills frontmatter: `bmad-quick-dev`, `tdd`, `verify-before-complete`.
  - Files: `src/Hymnal.Core/Models/SupplementalDocNode.cs`, `src/Hymnal.Core/Interfaces/ISupplementalDocsService.cs`, `src/Hymnal.Core/Services/SupplementalDocsService.cs`, `tests/Hymnal.Core.Tests/Services/SupplementalDocsServiceTests.cs`
  - Verify: dotnet test Hymnal.sln --filter SupplementalDocsServiceTests

- [x] **T02: Extend EditorViewModel with arbitrary-file lifecycle and dirty-switch tests** `est:2h`
  Why: the sketch's highest-risk contract is that supplemental docs open in the existing single-buffer editor while `ActiveNode` is null, without bypassing dirty-state handling, watcher conflict behavior, or `IMetadataStore.WriteTextAtomicAsync`. This task makes the editor path explicit before sidebar integration. Executor skills frontmatter: `bmad-quick-dev`, `tdd`, `verify-before-complete`.
  - Files: `src/Hymnal/ViewModels/EditorViewModel.cs`, `src/Hymnal/ViewModels/WorkspaceViewModel.cs`, `src/Hymnal/ViewModels/MainWindowViewModel.cs`, `tests/Hymnal.Core.Tests/ViewModels/EditorViewModelArbitraryFileTests.cs`, `tests/Hymnal.Core.Tests/ViewModels/WorkspaceViewModelTests.cs`
  - Verify: dotnet test Hymnal.sln --filter EditorViewModelArbitraryFileTests

- [x] **T03: Wire SupplementalDocsViewModel into workspace and shell navigation** `est:2.5h`
  Why: after Core and editor support exist, the slice must make docs available as a user-facing sidebar section with create/edit/folder support and route selection into Write mode. This task closes most of R007 at ViewModel/DI level without depending on final AXAML polish. Executor skills frontmatter: `bmad-quick-dev`, `tdd`, `verify-before-complete`.
  - Files: `src/Hymnal/ViewModels/SupplementalDocsViewModel.cs`, `src/Hymnal/ViewModels/WorkspaceViewModel.cs`, `src/Hymnal/ViewModels/MainWindowViewModel.cs`, `src/Hymnal/App.axaml.cs`, `tests/Hymnal.Core.Tests/ViewModels/SupplementalDocsViewModelTests.cs`, `tests/Hymnal.Core.Tests/ViewModels/MainWindowSupplementalDocsTests.cs`
  - Verify: dotnet test Hymnal.sln --filter SupplementalDocs

- [x] **T04: Render DOCS sidebar section and smoke-test create-open-save-reopen path** `est:2h`
  Why: the slice demo is user-facing, not only service-level. This task adds the actual Avalonia sidebar surface beside CHAPTERS and verifies the real view can construct and bind against the new docs ViewModel. Executor skills frontmatter: `bmad-quick-dev`, `tdd`, `make-interfaces-feel-better`, `verify-before-complete`.
  - Files: `src/Hymnal/Views/SupplementalDocsView.axaml`, `src/Hymnal/Views/SupplementalDocsView.axaml.cs`, `src/Hymnal/Views/MainWindow.axaml`, `src/Hymnal/Views/MainWindow.axaml.cs`, `src/Hymnal/ViewModels/MainWindowViewModel.cs`, `tests/Hymnal.Core.Tests/Views/SupplementalDocsViewSmokeTests.cs`, `tests/Hymnal.Core.Tests/Views/MainWindowViewSmokeTests.cs`
  - Verify: dotnet test Hymnal.sln

## Files Likely Touched

- src/Hymnal.Core/Models/SupplementalDocNode.cs
- src/Hymnal.Core/Interfaces/ISupplementalDocsService.cs
- src/Hymnal.Core/Services/SupplementalDocsService.cs
- tests/Hymnal.Core.Tests/Services/SupplementalDocsServiceTests.cs
- src/Hymnal/ViewModels/EditorViewModel.cs
- src/Hymnal/ViewModels/WorkspaceViewModel.cs
- src/Hymnal/ViewModels/MainWindowViewModel.cs
- tests/Hymnal.Core.Tests/ViewModels/EditorViewModelArbitraryFileTests.cs
- tests/Hymnal.Core.Tests/ViewModels/WorkspaceViewModelTests.cs
- src/Hymnal/ViewModels/SupplementalDocsViewModel.cs
- src/Hymnal/App.axaml.cs
- tests/Hymnal.Core.Tests/ViewModels/SupplementalDocsViewModelTests.cs
- tests/Hymnal.Core.Tests/ViewModels/MainWindowSupplementalDocsTests.cs
- src/Hymnal/Views/SupplementalDocsView.axaml
- src/Hymnal/Views/SupplementalDocsView.axaml.cs
- src/Hymnal/Views/MainWindow.axaml
- src/Hymnal/Views/MainWindow.axaml.cs
- tests/Hymnal.Core.Tests/Views/SupplementalDocsViewSmokeTests.cs
- tests/Hymnal.Core.Tests/Views/MainWindowViewSmokeTests.cs

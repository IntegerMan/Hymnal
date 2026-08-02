---
estimated_steps: 11
estimated_files: 7
skills_used: []
---

# T04: Render DOCS sidebar section and smoke-test create-open-save-reopen path

Why: the slice demo is user-facing, not only service-level. This task adds the actual Avalonia sidebar surface beside CHAPTERS and verifies the real view can construct and bind against the new docs ViewModel. Executor skills frontmatter: `bmad-quick-dev`, `tdd`, `make-interfaces-feel-better`, `verify-before-complete`.

Do:
1. Add a `SupplementalDocsView` AXAML/code-behind pair or extend the existing sidebar composition with a clearly separated DOCS section. Keep manuscript chapters and docs visually distinct; the existing CHAPTERS section must continue to bind only to `WorkspaceViewModel.Nodes`.
2. Provide UI affordances for create folder and create file under the DOCS section. Use code-behind dialogs/prompts if consistent with S01 corkboard prompt patterns; route all actions to `SupplementalDocsViewModel` commands.
3. Bind doc tree items so folders are expandable/non-editor targets and files are selectable/openable. Opening a file should activate Write mode and show the existing editor; folder clicks should expand/collapse or no-op without editor file changes.
4. Preserve the existing no-workspace/loading sidebar states and keep the collapsed sidebar icon behavior reasonable; do not regress Plan-mode hidden chrome from S01.
5. Update `MainWindow.axaml` to pass `SupplementalDocsViewModel` into the new DOCS view and keep right-pane behavior unchanged.
6. Add smoke tests in `tests/Hymnal.Core.Tests/Views/SupplementalDocsViewSmokeTests.cs` or extend existing view smoke tests to construct MainWindow with the docs section, bind sample docs, verify the DOCS header exists, verify create/open commands are reachable, and verify no binding exceptions are thrown.
7. Run the full solution test command after focused tests. Because prior GSD memory notes `gsd_exec`/WSL can break .NET restore, executors should run `dotnet test Hymnal.sln` directly in the task shell and record fresh evidence rather than relying on `gsd_exec` for closure.
8. Perform a manual desktop smoke if possible: create `.hymnal-data/docs/research/notes.md` through the UI, type content, save, close/reopen workspace, and verify the doc remains visible and content loads intact. If desktop smoke cannot be run in the environment, record that limitation and include the automated integration evidence.

Done when: the DOCS sidebar is visible in a real MainWindow, create/open actions are wired to the docs ViewModel, editor save/reopen behavior satisfies the slice demo, smoke tests pass, and full solution tests pass.

## Inputs

- `src/Hymnal/ViewModels/SupplementalDocsViewModel.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/Views/MainWindow.axaml`
- `tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs`
- `tests/Hymnal.Core.Tests/Views/ShellModeConvertersTests.cs`

## Expected Output

- `src/Hymnal/Views/SupplementalDocsView.axaml`
- `src/Hymnal/Views/SupplementalDocsView.axaml.cs`
- `src/Hymnal/Views/MainWindow.axaml`
- `src/Hymnal/Views/MainWindow.axaml.cs`
- `tests/Hymnal.Core.Tests/Views/SupplementalDocsViewSmokeTests.cs`

## Verification

dotnet test Hymnal.sln

## Observability Impact

Adds a visible DOCS tree as the primary runtime inspection surface; errors from create/open commands should appear through existing notification banners rather than disappearing into binding failures.

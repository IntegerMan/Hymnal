---
estimated_steps: 9
estimated_files: 5
skills_used: []
---

# T02: Extend EditorViewModel with arbitrary-file lifecycle and dirty-switch tests

Why: the sketch's highest-risk contract is that supplemental docs open in the existing single-buffer editor while `ActiveNode` is null, without bypassing dirty-state handling, watcher conflict behavior, or `IMetadataStore.WriteTextAtomicAsync`. This task makes the editor path explicit before sidebar integration. Executor skills frontmatter: `bmad-quick-dev`, `tdd`, `verify-before-complete`.

Do:
1. Add `EditorViewModel.OpenArbitraryFileAsync(string absolutePath)` or an equivalent public API that mirrors `OpenChapterAsync` for plain files: stop any existing watcher, read content, set `Text` and `OriginalText`, set `ActiveFilePath`, set `ActiveNode = null`, clear missing-chapter/book/conflict state, set `IsBookSelected = false`, and start the file watcher.
2. Update derived editor visibility so arbitrary files are visible in the editor even though `ActiveNode` is null and `IsBookSelected` is false. Preserve the existing workspace/no-chapter prompt behavior so a workspace with no selected file still prompts, but an arbitrary doc file shows the editor.
3. Keep `SaveAsync` unchanged in spirit: it must write the arbitrary doc through `_metadataStore.WriteTextAtomicAsync(savePath, Text)`, update `OriginalText`, emit `Saved`, clear conflict state, and restart the watcher.
4. Ensure `AcceptExternalCommand` and watcher conflict text work for arbitrary docs as well as chapters; no chapter-only assumptions may leak into the doc path.
5. Add or update focused tests, preferably `tests/Hymnal.Core.Tests/ViewModels/EditorViewModelArbitraryFileTests.cs`, covering: opening a doc sets `ActiveNode` null and `ActiveFilePath`; `ShowEditor` is true for docs; modifying and saving uses a fake `IMetadataStore`; opening a chapter after dirty doc state triggers save-before-switch through the Workspace path; opening a doc after dirty chapter state triggers save-before-switch through the planned docs ViewModel path or a small test seam; external change on clean doc reloads or dirty doc sets conflict.
6. If existing title logic in `MainWindowViewModel` assumes `ActiveNode` for file titles, adjust it in this task or leave a failing test expectation for T03 only if the title needs docs ViewModel context. Do not make doc files appear as chapters.

Done when: a supplemental doc can be the active editor buffer with `ActiveNode == null`, save uses the same atomic metadata store abstraction as chapters, dirty switches between chapters and docs are covered by automated tests, and existing chapter/Book.txt tests still pass.

## Inputs

- `src/Hymnal/ViewModels/EditorViewModel.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/MainWindowPlanModeTests.cs`
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`

## Expected Output

- `src/Hymnal/ViewModels/EditorViewModel.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/EditorViewModelArbitraryFileTests.cs`

## Verification

dotnet test Hymnal.sln --filter EditorViewModelArbitraryFileTests

## Observability Impact

Extends existing editor state signals so future agents can inspect `ActiveFilePath`, `ActiveNode`, `IsDirty`, `HasConflict`, and notification text to localize document open/save failures.

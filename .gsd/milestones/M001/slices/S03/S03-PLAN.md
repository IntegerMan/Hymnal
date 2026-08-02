# S03: Markua Editor with Save

**Goal:** Deliver a focused single-chapter Markua editor with syntax highlighting, explicit save affordances (Ctrl+S, File>Save, visible Save button), atomic temp-write-then-rename saves, dirty-state title indicator, save-before-switch semantics, last-edited-chapter restore on relaunch, and external-change conflict handling.
**Demo:** Clicking a chapter in the sidebar opens it in the editor with heading, bold, italic, code block, and attribute list highlighting; Ctrl+S saves atomically; title bar shows unsaved-change indicator

## Must-Haves

- Clicking a chapter node in the sidebar opens the chapter text in AvaloniaEdit with Markua syntax highlighting (headings, bold, italic, inline code, fenced code blocks, attribute lists, directives, blurb prefixes)
- Editing text sets the title bar to "• filename — Hymnal" (unsaved indicator)
- Ctrl+S, File>Save, and Save button all trigger an atomic temp-write-then-rename save and clear the dirty indicator
- Clicking a second chapter triggers save-before-switch; if save fails the switch is blocked and an error banner appears
- Relaunching the app with an existing workspace restores the last edited chapter
- External file change on a clean buffer auto-reloads; on a dirty buffer shows an in-editor conflict strip offering keep-edits or reload-from-disk
- Part nodes and missing-file chapter nodes in the sidebar are non-selectable / non-editable
- dotnet build src/Hymnal/Hymnal.csproj succeeds; MetadataStore unit tests pass

## Proof Level

- This slice proves: integration — end-to-end chapter lifecycle (open → edit → save → switch) exercised in the running app; unit tests cover MetadataStore atomic write

## Integration Closure

Consumes: ManuscriptModel (S02) for chapter nodes and root paths; AppSettingsStore (S02) for last-chapter persistence; INotificationService (S01) for error/info banners; DI container (S01) for service registration. Produces: EditorViewModel, EditorView, MarkuaHighlighting.xshd, IMetadataStore/MetadataStore — all consumed by S04 (notes panel needs active-chapter context, save lifecycle, and the MetadataStore atomic write seam).

## Verification

- Notification banner in MainWindow surfaces save failures and info events from INotificationService. Editor conflict strip shows external-change state. Title bar reflects dirty/clean editor state. All error paths route through INotificationService.ShowError with descriptive messages.

## Tasks

- [x] **T01: Added WorkspaceRoot/ManuscriptRoot to ManuscriptModel, wired SetRoots in ManuscriptService, and delivered IMetadataStore/MetadataStore atomic-write implementation with 3/3 passing unit tests registered in DI.** `est:45m`
  Why: ManuscriptService already computes manuscriptRoot when locating Book.txt but discards it before returning. Without root paths in the model, EditorViewModel cannot resolve absolute chapter file paths for load/save/watch operations regardless of whether Book.txt is at workspace root or in manuscript/. IMetadataStore/MetadataStore provides the reusable atomic text-write seam used by chapter saves in S03 and notes persistence in S04.
  - Files: `src/Hymnal.Core/Models/ManuscriptModel.cs`, `src/Hymnal.Core/Services/ManuscriptService.cs`, `src/Hymnal.Core/Interfaces/IMetadataStore.cs`, `src/Hymnal.Core/Infrastructure/MetadataStore.cs`, `tests/Hymnal.Core.Tests/Infrastructure/MetadataStoreTests.cs`, `src/Hymnal/App.axaml.cs`
  - Verify: dotnet test tests/Hymnal.Core.Tests/ --filter "MetadataStore"

- [x] **T02: EditorViewModel (single-buffer, atomic save, FileSystemWatcher conflict detection), updated WorkspaceViewModel (SelectedNode, TrySwitchChapterAsync, session restore), updated MainWindowViewModel (reactive title, 5s auto-dismiss banner), and updated App.axaml.cs DI wiring all implemented and structurally verified.** `est:1h 30m`
  Why: The behavioral core of S03 — single-buffer chapter management, dirty state, atomic save, save-before-switch, last-chapter restore, and reactive title updates — must live in view-model land before the UI shell can be assembled. This task creates EditorViewModel and extends WorkspaceViewModel and MainWindowViewModel to wire the full chapter lifecycle.
  - Files: `src/Hymnal/ViewModels/EditorViewModel.cs`, `src/Hymnal/ViewModels/WorkspaceViewModel.cs`, `src/Hymnal/ViewModels/MainWindowViewModel.cs`, `src/Hymnal/App.axaml.cs`
  - Verify: dotnet build src/Hymnal/Hymnal.csproj

- [x] **T03: Composed EditorView (AvaloniaEdit + Markua XSHD highlighting + conflict strip), wired MainWindow shell (title binding, Save menu, notification banner), and added SelectedItem binding to SidebarView — dotnet build passes 0 errors 0 warnings.** `est:1h 30m`
  Why: T02 delivers the behavioral core in view-model land. T03 closes the visual loop: replace the placeholder editor pane with a real AvaloniaEdit surface, load Markua syntax rules, bind the window title, activate the notification banner, wire Ctrl+S and File>Save, and update SidebarView to activate chapter selection with guard rails on Part/missing nodes.
  - Files: `src/Hymnal/Views/MarkuaHighlighting.xshd`, `src/Hymnal/Views/EditorView.axaml`, `src/Hymnal/Views/EditorView.axaml.cs`, `src/Hymnal/Views/MainWindow.axaml`, `src/Hymnal/Views/MainWindow.axaml.cs`, `src/Hymnal/Views/SidebarView.axaml`, `src/Hymnal/Hymnal.csproj`
  - Verify: dotnet build src/Hymnal/Hymnal.csproj

## Files Likely Touched

- src/Hymnal.Core/Models/ManuscriptModel.cs
- src/Hymnal.Core/Services/ManuscriptService.cs
- src/Hymnal.Core/Interfaces/IMetadataStore.cs
- src/Hymnal.Core/Infrastructure/MetadataStore.cs
- tests/Hymnal.Core.Tests/Infrastructure/MetadataStoreTests.cs
- src/Hymnal/App.axaml.cs
- src/Hymnal/ViewModels/EditorViewModel.cs
- src/Hymnal/ViewModels/WorkspaceViewModel.cs
- src/Hymnal/ViewModels/MainWindowViewModel.cs
- src/Hymnal/Views/MarkuaHighlighting.xshd
- src/Hymnal/Views/EditorView.axaml
- src/Hymnal/Views/EditorView.axaml.cs
- src/Hymnal/Views/MainWindow.axaml
- src/Hymnal/Views/MainWindow.axaml.cs
- src/Hymnal/Views/SidebarView.axaml
- src/Hymnal/Hymnal.csproj

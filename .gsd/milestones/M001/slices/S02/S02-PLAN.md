# S02: Workspace Open and Book.txt Parsing

**Goal:** Open a folder as a Workspace, parse Book.txt into an ordered Part/Chapter tree backed by DynamicData SourceCache, persist last-opened workspace path, render the tree in a SidebarView, and auto-restore on relaunch.
**Demo:** Opening C:/Dev/EliAndGraceMakeAGame renders the full Part/Chapter tree in Book.txt order; last-opened workspace auto-restores on relaunch; external Book.txt change triggers reload banner

## Must-Haves

- dotnet test tests/Hymnal.Core.Tests/ exits 0 with BookTxtParser, ManuscriptService, and AppSettingsStore tests passing; dotnet build src/Hymnal/ exits 0; manually opening C:/Dev/EliAndGraceMakeAGame renders Parts and Chapters in Book.txt order; relaunching auto-restores last workspace.

## Proof Level

- This slice proves: integration — tests cover Core logic; manual UAT covers end-to-end workspace open and auto-restore

## Integration Closure

Upstream: DI container + INotificationService + Result&lt;T&gt; from S01. This slice wires ManuscriptService, AppSettingsStore, WorkspaceViewModel, SidebarView, and FolderPickerService into the running shell. S03 consumes ManuscriptModel chapter file paths exposed by WorkspaceViewModel.

## Verification

- ManuscriptService fires INotificationService.ShowInfo on external Book.txt change and ShowError on load failure — banner is the only runtime signal needed for this slice.

## Tasks

- [x] **T01: BookTxtParser implemented with Part/Chapter detection; 4 unit tests pass** `est:45m`
  Why: BookTxtParser is the highest-risk piece — it must correctly distinguish Parts from Chapters by reading the first non-blank line of each .md file for `{class: part}`. The multi-part-book fixture Book.txt currently only lists `part-one/chapter-one.md` but the real manuscript shows part.md files appear inline in Book.txt; the fixture must be corrected to include `part-one/part.md` so part-detection tests are meaningful.
  - Files: `src/Hymnal.Core/Services/BookTxtParser.cs`, `tests/Hymnal.Core.Tests/Fixtures/SampleManuscripts/multi-part-book/Book.txt`, `tests/Hymnal.Core.Tests/Services/BookTxtParserTests.cs`
  - Verify: dotnet test tests/Hymnal.Core.Tests/ --filter BookTxtParserTests

- [x] **T02: ManuscriptService with FileSystemWatcher debounce implemented; 3 unit tests pass** `est:45m`
  Why: ManuscriptService composes BookTxtParser + ManuscriptModel + FileSystemWatcher to give the rest of the app a reactive view of the workspace. It is the only place that owns file I/O for the manuscript tree.
  - Files: `src/Hymnal.Core/Services/ManuscriptService.cs`, `tests/Hymnal.Core.Tests/Services/ManuscriptServiceTests.cs`
  - Verify: dotnet test tests/Hymnal.Core.Tests/ --filter ManuscriptServiceTests

- [x] **T03: IFolderPickerService interface and AppSettingsStore (atomic JSON settings store) implemented with 3 passing tests** `est:40m`
  Why: AppSettingsStore persists last-opened workspace path across launches. IFolderPickerService is a thin Core interface that lets WorkspaceViewModel trigger a folder picker without taking an Avalonia dependency in Core.
  - Files: `src/Hymnal.Core/Interfaces/IFolderPickerService.cs`, `src/Hymnal/Infrastructure/AppSettingsStore.cs`, `tests/Hymnal.Core.Tests/Infrastructure/AppSettingsStoreTests.cs`
  - Verify: dotnet test tests/Hymnal.Core.Tests/ --filter AppSettingsStoreTests

- [x] **T04: WorkspaceViewModel, FolderPickerService, SidebarView, and NodeKindConverters implemented; DynamicData Bind pattern wires ManuscriptModel.Nodes to ReadOnlyObservableCollection for AXAML** `est:60m`
  Why: WorkspaceViewModel bridges ManuscriptService + AppSettingsStore + IFolderPickerService for the UI. SidebarView renders the Part/Chapter tree. FolderPickerService wraps Avalonia's IStorageProvider.
  - Files: `src/Hymnal/Infrastructure/FolderPickerService.cs`, `src/Hymnal/ViewModels/WorkspaceViewModel.cs`, `src/Hymnal/Views/SidebarView.axaml`, `src/Hymnal/Views/SidebarView.axaml.cs`
  - Verify: dotnet build src/Hymnal/ --no-incremental

- [x] **T05: Wired DI registrations (AppSettingsStore, ManuscriptService, WorkspaceViewModel, FolderPickerService), updated MainWindowViewModel with WorkspaceViewModel injection, and replaced MainWindow placeholder with two-panel Grid hosting SidebarView** `est:30m`
  Why: Everything built in T01–T04 is dead code until it is wired into the running application via DI and the MainWindow layout is updated to host the sidebar.
  - Files: `src/Hymnal/App.axaml.cs`, `src/Hymnal/ViewModels/MainWindowViewModel.cs`, `src/Hymnal/Views/MainWindow.axaml`
  - Verify: dotnet build Hymnal.sln --no-incremental && dotnet test tests/Hymnal.Core.Tests/

## Files Likely Touched

- src/Hymnal.Core/Services/BookTxtParser.cs
- tests/Hymnal.Core.Tests/Fixtures/SampleManuscripts/multi-part-book/Book.txt
- tests/Hymnal.Core.Tests/Services/BookTxtParserTests.cs
- src/Hymnal.Core/Services/ManuscriptService.cs
- tests/Hymnal.Core.Tests/Services/ManuscriptServiceTests.cs
- src/Hymnal.Core/Interfaces/IFolderPickerService.cs
- src/Hymnal/Infrastructure/AppSettingsStore.cs
- tests/Hymnal.Core.Tests/Infrastructure/AppSettingsStoreTests.cs
- src/Hymnal/Infrastructure/FolderPickerService.cs
- src/Hymnal/ViewModels/WorkspaceViewModel.cs
- src/Hymnal/Views/SidebarView.axaml
- src/Hymnal/Views/SidebarView.axaml.cs
- src/Hymnal/App.axaml.cs
- src/Hymnal/ViewModels/MainWindowViewModel.cs
- src/Hymnal/Views/MainWindow.axaml

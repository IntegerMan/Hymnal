# S02: Workspace Open and Book.txt Parsing — Research

**Date:** 2026-05-28

## Summary

S02 wires workspace opening and `Book.txt` parsing into a live sidebar tree. The work spans three concerns: (1) parsing `Book.txt` into an ordered `ManuscriptModel` backed by DynamicData's `SourceCache`, (2) persisting the last-opened workspace path via an `AppSettingsStore` implementation, and (3) rendering the Chapter/Part tree in a `SidebarView`/`WorkspaceViewModel` pair that the existing shell can host.

The real manuscript at `C:/Dev/EliAndGraceMakeAGame` reveals the key structural rule: a Part is signaled by `{class: part}` at the top of its `.md` file — not by folder name or file name convention. `Book.txt` itself is a flat ordered list of relative paths from the manuscript folder root; Parts and Chapters are distinguished only by reading the first lines of each `.md` file. Blank lines in `Book.txt` are present between parts and must be ignored.

The DI container (App.axaml.cs) needs two additions for S02: `AppSettingsStore` registered as `IAppSettingsStore`, and `ManuscriptService` + `WorkspaceViewModel` registered so the sidebar can be resolved. The shell (`MainWindow.axaml`) needs its placeholder replaced with a two-panel `Grid` (sidebar + editor placeholder) that binds `WorkspaceViewModel` via the existing `ViewLocator`. S01 established every prerequisite; this is a pure addition slice.

## Recommendation

Build in this order: (1) `BookTxtParser` pure static/service in Core parsing flat-file → `ChapterNode` list, (2) `ManuscriptModel` as thin wrapper over `SourceCache<ChapterNode, string>`, (3) `ManuscriptService` with `LoadWorkspaceAsync` + `FileSystemWatcher`, (4) `AppSettingsStore` writing JSON to platform settings path, (5) `WorkspaceViewModel` + `SidebarView` rendering the tree, (6) wire everything into DI and replace the MainWindow placeholder. Tests in `Hymnal.Core.Tests` can cover steps 1–3 in isolation.

Part detection must parse the opening of each `.md` file to check for `{class: part}`. This is the non-obvious structural rule — Book.txt has no structural markers, only the file content does.

## Implementation Landscape

### Key Files

- `src/Hymnal.Core/Interfaces/IAppSettingsStore.cs` — Already defined (`GetAsync<T>` / `SetAsync<T>`); just needs implementation
- `src/Hymnal.Core/Interfaces/INotificationService.cs` — Used by ManuscriptService to surface load errors and FSW change banners
- `src/Hymnal.Core/Common/Result.cs` — All service returns must be `Task<Result<T>>` per established pattern
- `src/Hymnal/App.axaml.cs` — DI wiring; currently registers NotificationService, CredentialStoreStub, MainWindowViewModel. Needs: AppSettingsStore, ManuscriptService, WorkspaceViewModel
- `src/Hymnal/Views/MainWindow.axaml` — Placeholder `TextBlock` to be replaced with sidebar+editor-area Grid
- `src/Hymnal/ViewModels/MainWindowViewModel.cs` — Add `WorkspaceViewModel` property; wire open-folder command
- `tests/Hymnal.Core.Tests/Fixtures/SampleManuscripts/simple-book/Book.txt` — Contains `chapter-one.md` (single entry, no parts)
- `tests/Hymnal.Core.Tests/Fixtures/SampleManuscripts/multi-part-book/Book.txt` — Contains `part-one/chapter-one.md` (one-level path, needs part.md to signal part)

### Files to Create

- `src/Hymnal.Core/Models/ChapterNode.cs` — `record ChapterNode(string Key, string RelativePath, string Title, NodeKind Kind, bool IsMissing)` where `NodeKind` is `{ Part, Chapter }`
- `src/Hymnal.Core/Models/ManuscriptModel.cs` — Wraps `SourceCache<ChapterNode, string>`; exposes `IObservableCache<ChapterNode, string> Nodes`
- `src/Hymnal.Core/Services/BookTxtParser.cs` — Pure static parse: reads lines, strips blanks, checks first lines of each file for `{class: part}`, produces `IReadOnlyList<ChapterNode>`
- `src/Hymnal.Core/Services/ManuscriptService.cs` — `LoadWorkspaceAsync(string folderPath)`, `FileSystemWatcher` on `Book.txt`, fires `INotificationService.ShowInfo` on external change
- `src/Hymnal/Infrastructure/AppSettingsStore.cs` — Implements `IAppSettingsStore`; stores JSON at `%APPDATA%/Hymnal/settings.json` (Windows) / `~/.config/hymnal/settings.json` (Linux); atomic write-temp-rename
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs` — Exposes `ReadOnlyObservableCollection<ChapterNode>` bound from `ManuscriptModel`; `OpenWorkspaceCommand`
- `src/Hymnal/Views/SidebarView.axaml` + `.axaml.cs` — `TreeView` or `ListBox` rendering the Part/Chapter node list with ⚠ indicator for missing files
- `tests/Hymnal.Core.Tests/Services/BookTxtParserTests.cs` — Tests: simple-book parses to 1 Chapter, multi-part-book parses with correct Part/Chapter split, blank lines ignored, missing-file entry flagged
- `tests/Hymnal.Core.Tests/Services/ManuscriptServiceTests.cs` — Tests: missing file entry marked `IsMissing=true`, external change fires notification
- `tests/Hymnal.Core.Tests/Infrastructure/AppSettingsStoreTests.cs` — Tests: roundtrip Get/Set, atomic write (temp file created, original preserved on failure)

### Build Order

1. **`ChapterNode` + `ManuscriptModel`** — Pure models, no dependencies; unblocks everything
2. **`BookTxtParser`** — Static parse logic; can be fully unit-tested immediately with fixtures; highest-risk because part detection requires file I/O
3. **`AppSettingsStore`** — Needs platform path; implement + test; register in DI
4. **`ManuscriptService`** — Composes `BookTxtParser` + `ManuscriptModel` + `FileSystemWatcher`; `LoadWorkspaceAsync` returns `Result<ManuscriptModel>`
5. **`WorkspaceViewModel` + `SidebarView`** — Binds DynamicData cache to `ReadOnlyObservableCollection`; triggers `ManuscriptService.LoadWorkspaceAsync` via `OpenWorkspaceCommand`
6. **Wire MainWindow** — Replace placeholder with sidebar+editor-area Grid; `MainWindowViewModel` gets `WorkspaceViewModel`; DI registrations added to App.axaml.cs; last-workspace auto-restore on launch

### Verification Approach

- `dotnet test` in `Hymnal.Core.Tests` — BookTxtParserTests + ManuscriptServiceTests + AppSettingsStoreTests all pass
- Manual: open `C:/Dev/EliAndGraceMakeAGame` via the Open Workspace command; sidebar renders 7 parts + chapters in Book.txt order
- Close and relaunch the app; verify the workspace auto-restores (last-workspace path persisted in settings.json)
- Manually edit `Book.txt` in an editor while app is running; verify the info banner "Book.txt changed — Reload?" appears

## Don't Hand-Roll

| Problem | Existing Solution | Why Use It |
|---------|------------------|------------|
| Reactive ordered collection | `DynamicData.SourceCache<ChapterNode, string>` (already a project dependency) | Purpose-built for observable CRUD with key-based lookup; `Bind()` to `ReadOnlyObservableCollection` is one line |
| Platform settings path | `Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)` / `Environment.SpecialFolder.UserProfile` | Cross-platform; no extra package needed |
| JSON settings serialization | `System.Text.Json` (already used) | Zero new dependency; `JsonSerializerOptions` already established in codebase patterns |
| Open-folder dialog | `Avalonia.Platform.Storage.IStorageProvider.OpenFolderPickerAsync` | First-class Avalonia 12 API; no third-party needed |

## Constraints

- `Hymnal.Core.csproj` must have zero Avalonia package references — `BookTxtParser`, `ManuscriptModel`, `ManuscriptService`, `ChapterNode` are all Core; no Avalonia types may appear in them
- `AppSettingsStore` writes must use write-temp-then-rename (per R012 and established pattern) — never `File.WriteAllText` directly
- JSON settings file: camelCase properties, `"schemaVersion": 1`, `JsonIgnoreCondition.WhenWritingNull` (established convention)
- `DynamicData` is pinned to 9.4.31; do not upgrade — version floor is set by ReactiveUI 23.2.1 transitive constraint
- `IAppSettingsStore` is already defined; do not change the interface signature — it was part of the S01 contract

## Common Pitfalls

- **Part detection by filename/folder** — Folder name (`part1/`) is not the Part signal; `{class: part}` on the first non-blank line of the `.md` file is. The real manuscript uses `part1/part.md` but a file named anything could carry the `{class: part}` directive. Read the first 3 lines of each entry to detect.
- **Blank lines in Book.txt** — The real `Book.txt` has blank lines between part groups. These must be stripped/ignored during parse; they are not entries.
- **FileSystemWatcher cross-platform** — On Linux, `FileSystemWatcher` rename events differ from Windows. Use `NotifyFilter.FileName | NotifyFilter.LastWrite`; debounce with a short timer (~300ms) to avoid double-firing on Windows write-flush-rename sequences.
- **DynamicData `SourceCache` key** — Key must be the `RelativePath` string (unique per entry). Using filename alone would collide across parts (`part1/chapter.md` vs `part2/chapter.md`).
- **`OpenFolderPickerAsync` returns nullable** — User can cancel; check for null before proceeding.
- **Settings path on Linux** — `Environment.SpecialFolder.ApplicationData` maps to `~/.config` on Linux (via .NET runtime), not `~/.local/share`. This is correct for config files.
- **`ReactiveCommand.ThrownExceptions` subscription** — Every ViewModel must subscribe (per established pattern in S01); `WorkspaceViewModel.OpenWorkspaceCommand.ThrownExceptions.Subscribe(...)` is required.

## Open Risks

- Fixture files for multi-part-book only have `part-one/chapter-one.md` listed; the fixture directory may need a `part-one/part.md` with `{class: part}` content for part-detection tests to be meaningful. Check fixture contents and add if missing.
- `IStorageProvider` access in `WorkspaceViewModel` — Avalonia's storage provider is accessed via `TopLevel.GetTopLevel(view)?.StorageProvider`. ViewModels should receive it via constructor injection or a dedicated `IDialogService` abstraction to keep Core clean. A thin `IFolderPickerService` interface in Core is the right seam.

## Sources

- Real manuscript `Book.txt` at `C:/Dev/EliAndGraceMakeAGame/manuscript/Book.txt` — confirmed flat ordered list, blank lines between parts, `part.md` files mark parts
- `C:/Dev/EliAndGraceMakeAGame/manuscript/part1/part.md` — confirmed `{class: part}` directive on line 1 is the Part signal
- `docs/research/leanpub_markua_manuscript_research.md` — Markua 0.30 `Book.txt` spec reference
- S01 summary — DI patterns, DynamicData pins, Result<T> pattern, atomic write convention

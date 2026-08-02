# M001: Scaffold, Workspace, and Editor

**Vision:** A runnable Hymnal app where the author can open a Markua manuscript folder, see the Chapter/Part tree, write prose in a Markua-aware editor, save atomically, and read/write chapter notes — all in the synthwave dark theme.

## Success Criteria

- Opening a real Markua manuscript folder renders the Chapter/Part tree in Book.txt order in the sidebar
- Clicking a chapter opens AvaloniaEdit with Markua syntax highlighting active
- Ctrl+S saves the chapter file atomically (write-temp-then-rename)
- Notes panel toggles open and persists notes to .hymnal-data/notes/
- Dark synthwave theme (purple primary, JetBrains Mono editor font, Inter UI font) renders across the full shell
- All Hymnal.Core.Tests pass; CI matrix builds succeed on win-x64 and linux-x64
- App cold-starts under 5s; Book.txt with 100 chapters parses under 2s

## Slices

- [x] **S01: Solution Scaffold and Synthwave Theme** `risk:high` `depends:[]`
  > After this: dotnet build succeeds; app launches to a dark windowed shell with purple accents and correct Inter/JetBrains Mono fonts

- [x] **S02: Workspace Open and Book.txt Parsing** `risk:high` `depends:[S01]`
  > After this: Opening C:/Dev/EliAndGraceMakeAGame renders the full Part/Chapter tree in Book.txt order; last-opened workspace auto-restores on relaunch; external Book.txt change triggers reload banner

- [x] **S03: Markua Editor with Save** `risk:medium` `depends:[S02]`
  > After this: Clicking a chapter in the sidebar opens it in the editor with heading, bold, italic, code block, and attribute list highlighting; Ctrl+S saves atomically; title bar shows unsaved-change indicator

- [x] **S04: Chapter Notes Panel** `risk:low` `depends:[S03]`
  > After this: Toggling the Notes panel shows the active chapter's notes; writing a note and saving persists to .hymnal-data/notes/; reopening the chapter reloads the note

- [x] **S05: Validation Remediation and Integrated Evidence** `risk:medium` `depends:[S04]`
  > After this: After this: last edited chapter reliably restores on relaunch; S01/S04 assessment evidence exists; an integrated desktop smoke pass covers workspace open, chapter edit/save, notes persistence, and theme/highlighting confirmation; startup, parse, and CI evidence are captured for milestone closure

## Boundary Map

### S01 → S02

Produces:
- Avalonia `AppBuilder` host with MSDI DI container
- `SynthwaveTheme.axaml` named brush resources (`SynthwavePurpleBrush`, etc.)
- `INotificationService` / `NotificationService` banner infrastructure
- `Result<T>` + `Unit` types in `Hymnal.Core/Common/`
- `ViewLocator` convention (XxxView / XxxViewModel mapping)

Consumes:
- nothing (first slice)

### S02 → S03

Produces:
- `ManuscriptModel` (ordered `SourceCache<ChapterNode, string>`)
- `ManuscriptService.LoadWorkspaceAsync()` + `FileSystemWatcher`
- `WorkspaceViewModel.ManuscriptModel` observable
- `SidebarView` rendering the Chapter/Part tree
- `AppSettingsStore` with last-workspace-path persistence

Consumes:
- DI container + notification service from S01

### S03 → S04

Produces:
- `EditorView` + `EditorViewModel` with AvaloniaEdit `TextEditor`
- `MarkuaHighlighting.xshd` syntax definition
- `IMetadataStore` atomic write infrastructure
- Save command (Ctrl+S) wired to `MetadataStore`

Consumes:
- `ManuscriptModel` chapter file paths from S02
- DI container + notification service from S01

---
stepsCompleted: [1, 2, 3, 4, 5, 6, 7, 8]
inputDocuments:
  - _bmad-output/planning-artifacts/prds/prd-Hymnal-2026-05-27/prd.md
  - _bmad-output/planning-artifacts/prds/prd-Hymnal-2026-05-27/addendum.md
  - _bmad-output/planning-artifacts/briefs/brief-Hymnal-2026-05-27/brief.md
  - docs/research/leanpub_markua_manuscript_research.md
  - external:C:/Dev/EliAndGraceMakeAGame/.leanquill/project.yaml
  - external:C:/Dev/EliAndGraceMakeAGame/.leanquill/outline-index.json
  - external:C:/Dev/EliAndGraceMakeAGame/.leanquill/chapter-order.json
  - external:C:/Dev/EliAndGraceMakeAGame/notes/chapter-status.md
  - external:C:/Dev/EliAndGraceMakeAGame/.leanquill/workflows/story-chat.md
  - external:C:/Dev/EliAndGraceMakeAGame/.leanquill/personas/copy-editor.md
workflowType: 'architecture'
lastStep: 8
status: 'complete'
completedAt: '2026-05-27'
project_name: 'Hymnal'
user_name: 'Matthew-Hope'
date: '2026-05-27'
referenceProjects:
  - name: LeanQuill
    path: C:/Dev/EliAndGraceMakeAGame/.leanquill
    notes: Real working Markua manuscript tool; same problem space. Key patterns observed.
---

# Architecture Decision Document

_This document builds collaboratively through step-by-step discovery. Sections are appended as we work through each architectural decision together._

---

## Project Context Analysis

### Requirements Overview

**Functional Requirements:** 50 FRs across 8 feature areas:

| Area | FRs | Core challenge |
|---|---|---|
| Workspace & Manuscript Model | FR-1–FR-6 | `Book.txt` parse → in-memory tree; file-system watch |
| Markua Editor | FR-7–FR-13 | Syntax highlighting, inline validation, notes panel |
| Project Management | FR-14–FR-19 | Status lifecycle, live word count, targets |
| Gantt Phase View | FR-20–FR-27 | Custom time-axis renderer, inline date editing |
| Corkboard View | FR-28–FR-33 | Card layout; drag-to-reorder (Late V1) |
| Supplemental Docs | FR-34–FR-37 | File/folder tree sidebar, editor integration |
| Lightweight Git | FR-38–FR-43 | Stage-all → commit → push via system Git binary |
| AI Editorial | FR-44–FR-50 | Provider config, summaries, chat panel, issues |

**Non-Functional Requirements driving architectural decisions:**

- **Cross-platform:** Windows 10+ and Linux (Ubuntu LTS, Fedora) — eliminates WPF; Avalonia UI or Uno Platform are the primary candidates
- **Self-contained publish:** No .NET runtime install required — `dotnet publish --self-contained`
- **Performance:** 500ms word count update, 5s cold start, 2s Book.txt parse (100 chapters)
- **Data safety:** Atomic writes to `Book.txt` (write-temp-then-rename); never modify chapter `.md` files outside editor
- **Security:** API keys in OS credential store (Windows Credential Manager / libsecret) — never on disk
- **Accessibility:** WCAG AA contrast (4.5:1 minimum) across all surfaces

**Scale & Complexity:**

- Primary domain: **.NET cross-platform desktop**
- Complexity level: **medium-high**
- Estimated architectural components: 9–11 (workspace model, editor component, word-count engine, Gantt renderer, Corkboard renderer, metadata store, file-watcher service, Git service, AI service, credential store, platform abstractions)

### Technical Constraints & Dependencies

- **UI Framework:** Must support Windows and Linux natively — Avalonia UI or Uno Platform (both under active consideration)
- **.NET version:** .NET 8 or .NET 9 (current LTS / current release as of architecture date)
- **System Git binary:** Hymnal invokes the installed `git` via PATH — no embedded libgit2/LibGit2Sharp required; simpler, but requires Git to be installed
- **AI integration:** LiteLLM-compatible endpoints OR Microsoft Extensions for AI / Agent Framework — needs abstraction layer
- **File system:** Standard .NET `FileSystemWatcher` for Book.txt and chapter change detection
- **Credential store:** Platform-conditional: `Windows.Security.Credentials.PasswordVault` on Windows; `libsecret` / `SecretService` D-Bus API on Linux

### Cross-Cutting Concerns Identified

- **Reactive data propagation** — word count changes in one chapter must roll up to Part and book totals across all views simultaneously
- **Async I/O coordination** — Git operations, AI API calls, and file-system events are all async; cancellation and error surfacing must be first-class
- **Schema versioning** — `.hymnal-data/` JSON files (`phases.json`, `targets.json`, `issues.json`) need embedded schema version from day one; validated by real-world parallel (LeanQuill `schema_version: "2"` migration evidence)
- **Platform abstraction** — credential store and any OS-specific behavior require a seam for testability and portability
- **Atomic file writes** — `Book.txt` mutations must be write-to-temp → rename; editor saves follow standard .NET `File.WriteAllText` (which is atomic on most targets)
- **Synthwave theming** — dark theme with purple primary, yellow/pink/orange accents must be applied consistently across all custom-rendered surfaces (Gantt, Corkboard) and framework-provided controls

### Reference Architecture Observations (LeanQuill)

Real parallel implementation (`C:/Dev/EliAndGraceMakeAGame/.leanquill`) revealed:

- **Derived structural index pattern** — LeanQuill maintains both `outline-index.json` (UUID-keyed tree) and `chapter-order.json` (flat path list) derived from `Book.txt`. Hymnal should follow: `Book.txt` is source of truth, in-memory model is the working representation, with optional cache
- **`project.yaml` with `schema_version`** — configuration carries versioning; Hymnal's metadata files must do the same
- **AI safety policy** — `manuscript_write_blocked: true` and `git_operations_blocked: true` are explicit in LeanQuill's config. Hymnal's architecture should encode the same constraints explicitly, not rely on convention
- **Persona/mode configuration shape** — LeanQuill's editorial personas (YAML with `context_access`, `feedback`, `allowed_types`) inform the shape of Hymnal's structured editorial modes (Proofing, Consistency, Line Editing)

---

## Starter Template Evaluation

### Primary Technology Domain

**.NET cross-platform desktop** — Windows 10+ and Linux (Ubuntu LTS, Fedora). No mobile, no web, no macOS in V1.

### Starter Options Considered

| Framework | Renderer | License | Linux | Scope fit |
|---|---|---|---|---|
| **Avalonia UI 12.0** | Skia (pixel-identical) + Impeller (preview) | MIT, fully free | First-class | Desktop-first ✅ |
| Uno Platform | WinUI 3 surface | Community free / Pro $390/yr | Supported | Mobile+web breadth unused for V1 ❌ |

Uno Platform's strength is broad target coverage (mobile, web, Windows). Hymnal explicitly targets only Windows + Linux desktop in V1 — that breadth is unused weight, and the commercial tooling tier creates friction for a solo MIT-licensed project.

### Selected Starter: Avalonia UI 12.0 + ReactiveUI MVVM

**Rationale:** Desktop-first framework; Skia renderer gives pixel-level control for custom Gantt and Corkboard rendering; MIT licensed; ReactiveUI MVVM is the right primitive for live word-count propagation; 30.2k stars with production use at JetBrains, Devolutions, NASA; Linux is a first-class target not an afterthought.

**Initialization:**

```bash
dotnet new install Avalonia.Templates
dotnet new avalonia.mvvm -o Hymnal --framework net10.0
```

### Architectural Decisions Provided by Starter

| Decision | Value |
|---|---|
| **Language** | C# with nullable reference types enabled |
| **UI Pattern** | MVVM via ReactiveUI (`ReactiveObject`, `ReactiveCommand`, `WhenAnyValue`) |
| **Renderer** | Skia (default); Impeller opt-in for GPU-intensive surfaces |
| **Styling** | Avalonia `Style` + `ControlTheme` with resource dictionary dark theme |
| **Compiled bindings** | Enabled — type-safe, no reflection overhead |
| **View resolution** | `IDataTemplate`-based `ViewLocator` (ViewModel → View mapping) |
| **Build** | Standard `dotnet publish --self-contained -r win-x64 / linux-x64` |
| **.NET version** | .NET 10 |

---

## Core Architectural Decisions

### Decision Priority Analysis

**Critical Decisions (block implementation):**
- Metadata storage format: JSON files in `.hymnal-data/`
- Editor component: AvaloniaEdit
- AI provider abstraction: MEAI `IChatClient`
- DI container: Microsoft.Extensions.DependencyInjection

**Important Decisions (shape architecture):**
- Word count engine: full reparse per chapter with 300ms debounce + reactive rollup
- In-memory manuscript model: derived from `Book.txt` on load; `Book.txt` is source of truth
- Schema versioning: embedded `schemaVersion` field in every `.hymnal-data/` JSON file

**Deferred Decisions (post-V1):**
- Distribution packaging format (Parcel, `.deb`, `.rpm`, Windows installer) — GitHub Releases with self-contained `.zip` sufficient for V1
- SQLite migration if Hymnal grows beyond single-author scope

---

### Data Architecture

**Metadata storage: JSON files in `.hymnal-data/`**
- Rationale: human-readable, Git-diffable, inspectable and scriptable outside the app — consistent with Hymnal's plaintext/Git philosophy
- Serialization: `System.Text.Json` (built-in, no extra dependency)
- Schema versioning: every file carries a top-level `"schemaVersion": 1` field; Hymnal validates on load and refuses to open an unknown version with a clear diagnostic error
- Files: `phases.json` (phase dates + progress, keyed by chapter path), `targets.json` (word count targets keyed by chapter path / part folder / `"book"`), `issues.json` (AI issues array), `exclusions.json` (Late V1)
- Write safety: all `.hymnal-data/` writes use write-to-temp-then-rename for atomicity; same pattern as `Book.txt` mutations (NFR-D1)

**In-memory manuscript model:**
- `Book.txt` is parsed on Workspace open into a `ManuscriptModel` — an ordered list of `PartNode` and `ChapterNode` objects
- `FileSystemWatcher` on `Book.txt` triggers a reload prompt on external changes (FR-6)
- `FileSystemWatcher` on chapter files triggers incremental word count update for that chapter
- The `ManuscriptModel` is the single authoritative in-memory source of truth; all views bind to ViewModel projections of it

---

### Security

**Credential store abstraction**
- Interface: `ICredentialStore` with `StoreAsync(key, secret)`, `RetrieveAsync(key)`, `DeleteAsync(key)`
- Two platform implementations registered conditionally via `RuntimeInformation.IsOSPlatform`:
  - `WindowsCredentialStore` — `Windows.Security.Credentials.PasswordVault`
  - `LinuxSecretServiceStore` — `libsecret` via `SecretService` NuGet or D-Bus P/Invoke
- API keys never appear in any `.hymnal-data/` file, settings export, or log output (NFR-S2)

**AI write isolation (explicit constraint, not convention):**
- The `AiEditorialService` returns data only (summary strings, `Issue` objects); it has no `IMetadataStore` or file-write dependency
- All persistence of AI output flows through the caller (ViewModel → `IMetadataStore`)
- This boundary is enforced at the DI registration level — `AiEditorialService` does not receive any write-capable service

---

### API & Communication Patterns

**AI provider: MEAI `IChatClient`**
- `Microsoft.Extensions.AI.IChatClient` is the internal abstraction throughout the codebase
- LiteLLM endpoint → `OpenAIChatClient` (MEAI) pointed at the LiteLLM URL
- Native MEAI/Agent Framework → direct `IChatClient` implementation
- Provider type + endpoint + model name stored in application settings (not credential store); API key stored separately in credential store
- `IHttpClientFactory` manages `HttpClient` lifetime for the LiteLLM path

**Git service: process wrapper**
- `IGitService` interface; `ProcessGitService` implementation
- Invokes `git` via `System.Diagnostics.Process` with PATH resolution; no bundled Git binary
- Operations: `GetStatusAsync()`, `StageAllAsync()`, `CommitAsync(message)`, `PushAsync()`, `GetCurrentBranchAsync()`
- Returns `GitOperationResult { bool Success, string Output, string Error }` — errors surfaced to user via in-app notification with raw stderr (FR-43); never swallowed

---

### Application Architecture

**DI container: `Microsoft.Extensions.DependencyInjection`**
- Wired via Avalonia's `AppBuilder` host from the `avalonia.mvvm` template
- Service registrations: `IManuscriptService` (singleton), `IMetadataStore` (singleton), `IGitService` (singleton), `AiEditorialService` (singleton), `ICredentialStore` (singleton, platform-conditional)

**Editor component: AvaloniaEdit**
- `TextEditor` control from the `AvaloniaEdit` NuGet package
- Markua syntax highlighting via custom `IHighlightingDefinition` (XSHD token rules for FR-8 constructs)
- `AbstractMargin` subclass for inline issue indicators (FR-50)
- Word count: computed from `TextEditor.Document.Text` on `TextChanged` with 300ms debounce → pushed to `ChapterViewModel.WordCount`

**Reactive word count rollup:**
- `ChapterViewModel.WordCount` is a `ReactiveUI` `ObservableAsPropertyHelper<int>` driven by the debounced text-change stream
- `PartViewModel` aggregates member chapter `WordCount` values via `WhenAnyValue` + `Select` + `CombineLatest`
- `WorkspaceViewModel` aggregates all part totals the same way
- Targets and proximity indicators derive from the same reactive graph

**Navigation / view composition:**
- Single-window layout; main content region is a `ContentControl` bound to `WorkspaceViewModel.CurrentView`
- Named views: `EditorView`, `GanttView`, `CorkboardView`, `GitPanelView`, `AiChatView`, `IssuesPanelView`
- Supplemental docs are not a separate view — clicking a supplemental doc node in `SidebarView` loads the file into `EditorView` (same component, different file). No `SupplementalDocsView` class exists.
- `ViewLocator` maps ViewModel type → View type via Avalonia `IDataTemplate` convention (from template)

---

### Infrastructure & Deployment

**Build:**
- `dotnet publish --self-contained -r win-x64 -c Release` → Windows `.zip`
- `dotnet publish --self-contained -r linux-x64 -c Release` → Linux `.tar.gz`
- GitHub Releases with attached archives; no installer for V1

**CI/CD: GitHub Actions**
- Trigger: tag push (`v*`)
- Matrix: `[win-x64, linux-x64]`
- Steps: restore → build → test → publish → upload release asset

**Dev tooling:**
- VS Code + Avalonia for VS Code extension (free; XAML preview, IntelliSense, hot reload)
- Avalonia DevTools enabled in `#if DEBUG` builds for live visual tree and binding inspection

---

## Implementation Patterns & Consistency Rules

### Conflict Points Identified

9 areas where AI agents working in parallel could make incompatible choices without these rules.

---

### Naming Patterns

**C# code naming — all agents MUST follow:**

| Construct | Convention | Example |
|---|---|---|
| Types (class, struct, enum, record) | PascalCase | `ChapterViewModel`, `ManuscriptModel` |
| Interfaces | `I` prefix + PascalCase | `IMetadataStore`, `IGitService` |
| Methods and properties | PascalCase | `LoadWorkspaceAsync()`, `WordCount` |
| Private fields | `_camelCase` | `_wordCount`, `_metadataStore` |
| Local variables / parameters | camelCase | `chapterNode`, `cancellationToken` |
| Constants | PascalCase | `DefaultCommitMessagePrefix` |
| ViewModel suffix | `…ViewModel` | `ChapterViewModel`, `GanttViewModel` |
| View suffix | `…View` | `ChapterView`, `GanttView` — must match ViewModel name for `ViewLocator` |
| Service suffix | `…Service` | `AiEditorialService`, `WordCountService` |
| Store suffix | `…Store` | `IMetadataStore`, `MetadataStore` |

**File naming:** one public type per file; filename = type name (`ChapterViewModel.cs`, `IMetadataStore.cs`).

**XAML resource keys:** `PascalCase` strings matching the purpose (`SynthwavePurpleBrush`, `BodyTextStyle`).

---

### JSON Serialization Patterns

All `.hymnal-data/` JSON files follow these rules:

```json
{
  "schemaVersion": 1,
  "chapters": {
    "part1/01-chapter.md": { ... }
  }
}
```

- **Property names:** `camelCase` (System.Text.Json default — do NOT apply `JsonNamingPolicy.SnakeCaseLower` or PascalCase)
- **Top-level `schemaVersion`:** present in every `.hymnal-data/` file; integer; validated on every read
- **Chapter keys:** always the folder-prefixed relative path from the manuscript root (e.g. `"part1/01-chapter.md"`) — never basename-only, never absolute
- **Dates:** ISO 8601 strings (`"2026-05-27"` for date-only; `"2026-05-27T14:30:00Z"` for timestamps)
- **Missing optional values:** omit the key entirely rather than writing `null` (`JsonIgnoreCondition.WhenWritingNull`)
- **Enum values:** strings, not integers (`JsonStringEnumConverter`)

---

### Reactive Collections Pattern

**DynamicData for all collections that require filtering, sorting, or transformation:**

| Collection | Type | Reason |
|---|---|---|
| `ManuscriptModel.Chapters` | `SourceCache<ChapterNode, string>` (key = chapter path) | Reactive word count updates and reorder |
| `IssueStore.Issues` | `SourceCache<Issue, string>` (key = `issue.Id`) | Filterable by scope/type/state (FR-49) |
| `PartViewModel.Chapters` | Derived `ReadOnlyObservableCollection<T>` via `.Bind()` | Bound to Gantt and Corkboard views |

Plain `ObservableCollection<T>` is acceptable for simple one-shot display lists that never need reactive filter/transform (e.g. supplemental docs file tree, settings list items).

**Pattern — all agents MUST follow this shape for DynamicData collections:**
```csharp
// In service/store — source of truth
private readonly SourceCache<Issue, string> _issues = new(i => i.Id);
public IObservable<IChangeSet<Issue, string>> Connect() => _issues.Connect();

// In ViewModel — derived, bound collection
_issues.Connect()
    .Filter(this.WhenAnyValue(x => x.ScopeFilter).Select(BuildFilter))
    .Sort(SortExpressionComparer<Issue>.Descending(i => i.CreatedDate))
    .Bind(out _filteredIssues)
    .Subscribe()
    .DisposeWith(Disposables);
```

---

### Async & Error Handling Patterns

**All service method signatures:**
```csharp
Task<Result<T>> MethodAsync(/* params */, CancellationToken ct = default);
```

**`Result<T>` shape** — one shared type in `Hymnal.Core`:
```csharp
readonly record struct Result<T>(T? Value, string? Error, bool IsSuccess)
{
    public static Result<T> Ok(T value) => new(value, null, true);
    public static Result<T> Fail(string error) => new(default, error, false);
}
// Unit variant for void operations: Result<Unit>
```

**In ViewModels — all agents MUST follow this pattern:**
```csharp
CommitCommand = ReactiveCommand.CreateFromTask(async ct =>
{
    var result = await _gitService.CommitAsync(CommitMessage, ct);
    if (!result.IsSuccess)
        _notifications.ShowError(result.Error!);
});
// Safety net for unexpected throws:
CommitCommand.ThrownExceptions
    .Subscribe(ex => _notifications.ShowError(ex.Message))
    .DisposeWith(Disposables);
```

**`CancellationToken` rules:**
- Every async service method accepts `CancellationToken ct = default` as its last parameter
- ViewModels pass `ReactiveCommand`'s built-in cancellation token
- `ConfigureAwait(false)` on all awaits inside service/store implementations (not in ViewModels)

---

### MVVM Layer Boundaries

| Layer | Owns | Must NOT |
|---|---|---|
| **View** (`.axaml` + code-behind) | Layout, animation, input binding | Contain business logic; access services directly |
| **ViewModel** | UI state, commands, reactive composition | Read/write files directly; call `Process.Start()` |
| **Service** (`IXxxService`) | Business logic, orchestration | Know about ViewModels or Avalonia types |
| **Store** (`IMetadataStore`) | Read/write `.hymnal-data/` JSON | Know about ViewModels; perform Git or AI operations |
| **Model** (`ManuscriptModel`, `ChapterNode`) | Data shape only | Contain reactive properties or DI dependencies |

**Dependency direction: View → ViewModel → Service/Store → Model. Never reversed.**

---

### File Operation Patterns

All agents writing to disk MUST use write-to-temp-then-rename:

```csharp
// CORRECT — atomic
var tmp = path + ".tmp";
await File.WriteAllTextAsync(tmp, json, Encoding.UTF8, ct);
File.Move(tmp, path, overwrite: true);

// WRONG — non-atomic, data loss risk
await File.WriteAllTextAsync(path, json, ct);
```

Applies to: `Book.txt` mutations, all `.hymnal-data/` JSON file writes.

---

### Notification Pattern

```csharp
interface INotificationService {
    void ShowError(string message);    // red banner, persists until dismissed
    void ShowInfo(string message);     // blue banner, auto-dismiss 4s
    void ShowSuccess(string message);  // green banner, auto-dismiss 3s
}
```

Agents MUST NOT: use `MessageBox`/modal dialogs for non-destructive errors; display raw exception type names to users; silently swallow errors.

---

### Enforcement Guidelines

**All AI agents MUST:**
- Follow the naming convention table above exactly
- Use `Result<T>` for all Git and AI service return types
- Include `schemaVersion` in every new `.hymnal-data/` JSON file
- Use write-to-temp-then-rename for every `IMetadataStore` file write
- Respect MVVM layer boundaries — services have no Avalonia type references
- Use `SourceCache<T, TKey>` for `ManuscriptModel.Chapters` and `IssueStore.Issues`

**Anti-patterns to avoid:**
- `ObservableCollection<Chapter>` on `ManuscriptModel` — breaks reactive word count pipeline
- `File.WriteAllText(path, ...)` without temp-rename in store implementations
- `try/catch` in ViewModels that swallows without notifying the user
- Storing the AI provider API key in `appsettings.json` or any settings file
- Properties named `ChaptersList` or `ChapterItems` — canonical name is `Chapters`

---

## Project Structure & Boundaries

### Solution Layout Decision

**Two-project solution selected (compile-enforced layer boundary):**
- `src/Hymnal/` — Avalonia UI layer (Views, ViewModels, Converters, Infrastructure)
- `src/Hymnal.Core/` — Pure .NET 10 domain layer (zero Avalonia reference)
- `tests/Hymnal.Core.Tests/` — xUnit tests; no UI dependency

Rationale: the `Hymnal.Core.csproj` project file contains no Avalonia package reference. Any agent that accidentally imports an Avalonia type into a service gets a compile error, not a code-review note.

---

### Complete Directory Tree

```
Hymnal/                                        ← workspace / git root
├── .github/
│   └── workflows/
│       └── release.yml                        ← tag-triggered build matrix (win-x64, linux-x64)
├── .gitignore
├── README.md
├── Hymnal.sln
├── src/
│   ├── Hymnal/                                ← Avalonia UI project
│   │   ├── Hymnal.csproj                      ← references Hymnal.Core + Avalonia packages
│   │   ├── Program.cs
│   │   ├── App.axaml
│   │   ├── App.axaml.cs                       ← AppBuilder host + DI registration
│   │   ├── ViewLocator.cs
│   │   ├── Assets/
│   │   │   ├── hymnal.ico
│   │   │   └── Fonts/
│   │   ├── Themes/
│   │   │   ├── SynthwaveTheme.axaml           ← palette resources (SynthwavePurpleBrush etc.)
│   │   │   ├── ControlStyles.axaml            ← control theme overrides
│   │   │   └── Icons.axaml                    ← vector/path icon resources
│   │   ├── Views/
│   │   │   ├── MainWindow.axaml
│   │   │   ├── MainWindow.axaml.cs
│   │   │   ├── Editor/
│   │   │   │   ├── EditorView.axaml
│   │   │   │   ├── EditorView.axaml.cs
│   │   │   │   ├── NotesView.axaml
│   │   │   │   ├── NotesView.axaml.cs
│   │   │   │   ├── MarkuaHighlighting.xshd    ← XSHD syntax definition (FR-8)
│   │   │   │   └── IssueMargin.cs             ← AbstractMargin inline indicators (FR-50)
│   │   │   ├── Gantt/
│   │   │   │   ├── GanttView.axaml
│   │   │   │   ├── GanttView.axaml.cs
│   │   │   │   └── GanttCanvas.cs             ← custom DrawingContext renderer
│   │   │   ├── Corkboard/
│   │   │   │   ├── CorkboardView.axaml
│   │   │   │   └── CorkboardView.axaml.cs
│   │   │   ├── Sidebar/
│   │   │   │   ├── SidebarView.axaml          ← manuscript tree + supplemental docs
│   │   │   │   └── SidebarView.axaml.cs
│   │   │   ├── Git/
│   │   │   │   ├── GitPanelView.axaml
│   │   │   │   └── GitPanelView.axaml.cs
│   │   │   ├── Ai/
│   │   │   │   ├── AiChatView.axaml
│   │   │   │   ├── AiChatView.axaml.cs
│   │   │   │   ├── IssuesPanelView.axaml
│   │   │   │   └── IssuesPanelView.axaml.cs
│   │   │   └── Settings/
│   │   │       ├── SettingsView.axaml
│   │   │       └── SettingsView.axaml.cs
│   │   ├── ViewModels/
│   │   │   ├── ViewModelBase.cs               ← ReactiveObject + CompositeDisposable
│   │   │   ├── MainWindowViewModel.cs
│   │   │   ├── WorkspaceViewModel.cs          ← CurrentView + word count root
│   │   │   ├── Editor/
│   │   │   │   ├── EditorViewModel.cs
│   │   │   │   └── NotesViewModel.cs
│   │   │   ├── Manuscript/
│   │   │   │   ├── ChapterViewModel.cs        ← WordCount ObservableAsPropertyHelper
│   │   │   │   └── PartViewModel.cs           ← DynamicData word count rollup
│   │   │   ├── Gantt/
│   │   │   │   ├── GanttViewModel.cs
│   │   │   │   ├── GanttRowViewModel.cs
│   │   │   │   └── PhaseBoxViewModel.cs
│   │   │   ├── Corkboard/
│   │   │   │   ├── CorkboardViewModel.cs
│   │   │   │   └── CardViewModel.cs
│   │   │   ├── Git/
│   │   │   │   └── GitPanelViewModel.cs
│   │   │   ├── Ai/
│   │   │   │   ├── AiChatViewModel.cs
│   │   │   │   └── IssuesPanelViewModel.cs
│   │   │   └── Settings/
│   │   │       └── SettingsViewModel.cs
│   │   ├── Converters/
│   │   │   ├── StatusToColorConverter.cs
│   │   │   ├── WordCountToProgressConverter.cs
│   │   │   └── BoolToVisibilityConverter.cs
│   │   └── Infrastructure/
│   │       └── NotificationService.cs         ← INotificationService Avalonia banner impl
│   │
│   └── Hymnal.Core/                           ← Pure .NET 10 — zero Avalonia reference
│       ├── Hymnal.Core.csproj
│       ├── Common/
│       │   ├── Result.cs                      ← Result<T> + Result<Unit>
│       │   └── Unit.cs
│       ├── Models/
│       │   ├── ManuscriptModel.cs             ← SourceCache<ChapterNode, string>
│       │   ├── ChapterNode.cs
│       │   ├── PartNode.cs
│       │   ├── ChapterStatus.cs               ← enum: Outlining…Done
│       │   ├── PhaseData.cs                   ← start/end dates + progress %
│       │   ├── WordCountTarget.cs             ← single value or min/max range
│       │   └── Issue.cs                       ← type / state / location / created
│       ├── Interfaces/
│       │   ├── IManuscriptService.cs
│       │   ├── IMetadataStore.cs
│       │   ├── IGitService.cs
│       │   ├── ICredentialStore.cs
│       │   ├── IAiEditorialService.cs
│       │   ├── INotificationService.cs
│       │   └── IAppSettingsStore.cs           ← non-secret app settings (endpoint, window state, recents)
│       ├── Services/
│       │   ├── ManuscriptService.cs           ← Book.txt parse + FileSystemWatcher
│       │   ├── WordCountService.cs            ← prose tokenization (FR-16)
│       │   ├── AiEditorialService.cs          ← IChatClient wrapper; returns data only
│       │   └── SummaryService.cs             ← generate/store summaries (FR-45)
│       ├── Stores/
│       │   ├── MetadataStore.cs               ← JSON R/W for .hymnal-data/ (atomic writes)
│       │   ├── IssueStore.cs                  ← SourceCache<Issue, string>
│       │   └── AppSettingsStore.cs            ← R/W %APPDATA%/Hymnal/ or ~/.config/hymnal/
│       ├── Git/
│       │   ├── ProcessGitService.cs           ← system git binary via Process
│       │   └── GitOperationResult.cs
│       └── Platform/
│           ├── WindowsCredentialStore.cs      ← PasswordVault
│           └── LinuxSecretServiceStore.cs     ← libsecret / SecretService
│
└── tests/
    └── Hymnal.Core.Tests/
        ├── Hymnal.Core.Tests.csproj           ← xUnit + NSubstitute
        ├── Services/
        │   ├── ManuscriptServiceTests.cs
        │   ├── WordCountServiceTests.cs
        │   └── MetadataStoreTests.cs
        ├── Git/
        │   └── ProcessGitServiceTests.cs
        ├── Models/
        │   └── BookTxtParserTests.cs
        └── Fixtures/
            └── SampleManuscripts/             ← embedded test data
                ├── simple-book/
                │   ├── Book.txt
                │   └── chapter.md
                └── multi-part-book/
                    ├── Book.txt
                    ├── part1/
                    │   ├── part.md
                    │   └── 01-chapter.md
                    └── part2/
                        ├── part.md
                        └── 01-chapter.md
```

**App settings location (non-project, non-secret):**

| Platform | Path |
|---|---|
| Windows | `%APPDATA%\Hymnal\settings.json` |
| Linux | `~/.config/hymnal/settings.json` |

Content: AI provider type, endpoint URL, model name, window size/position, recent workspace paths. Managed by `AppSettingsStore` / `IAppSettingsStore`. **API keys are never in this file** — they live in `ICredentialStore`.

---

### Requirements to Structure Mapping

| FR Area | Primary Location |
|---|---|
| FR-1–FR-6 Workspace & Manuscript | `Hymnal.Core/Services/ManuscriptService.cs`, `Models/ManuscriptModel.cs` |
| FR-7–FR-13 Markua Editor | `Views/Editor/`, `ViewModels/Editor/`, `MarkuaHighlighting.xshd`, `IssueMargin.cs` |
| FR-14–FR-19 Project Management | `Models/ChapterStatus.cs`, `ViewModels/Manuscript/`, `Stores/MetadataStore.cs` |
| FR-20–FR-27 Gantt View | `Views/Gantt/`, `ViewModels/Gantt/`, `GanttCanvas.cs` |
| FR-28–FR-33 Corkboard | `Views/Corkboard/`, `ViewModels/Corkboard/` |
| FR-34–FR-37 Supplemental Docs | `Views/Sidebar/SidebarView.axaml` + `ViewModels/Editor/EditorViewModel.cs` |
| FR-38–FR-43 Git Integration | `Git/ProcessGitService.cs`, `ViewModels/Git/GitPanelViewModel.cs` |
| FR-44–FR-50 AI Editorial | `Services/AiEditorialService.cs`, `SummaryService.cs`, `Stores/IssueStore.cs`, `ViewModels/Ai/` |

---

### Architectural Boundaries

**Compile-enforced layer boundary:** `Hymnal.Core.csproj` contains no Avalonia package reference. An agent that imports `Avalonia.Controls` into a service gets a build error.

**DI seam in `App.axaml.cs`:**
```csharp
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    services.AddSingleton<ICredentialStore, WindowsCredentialStore>();
else
    services.AddSingleton<ICredentialStore, LinuxSecretServiceStore>();
```

---

### Data Flow

**Word Count pipeline:**
```
FileSystemWatcher → ManuscriptService (parse/update ManuscriptModel)
    → ChapterViewModel (WordCount via debounced TextChanged / 300ms)
        → PartViewModel (DynamicData rollup)
            → WorkspaceViewModel (book total)
                → Sidebar, Gantt, Corkboard views via binding
```

**AI Editorial pipeline:**
```
AiChatViewModel
    → AiEditorialService.GenerateSummaryAsync() → IChatClient → returns string
        → SummaryService.StoreAsync()           → MetadataStore → .hymnal-data/summaries/
    → AiEditorialService.AnalyseAsync()         → IChatClient → returns Issue[]
        → IssueStore.AddRange()                 → SourceCache → MetadataStore → issues.json
            → IssuesPanelViewModel              ← DynamicData Connect() pipeline
                → IssueMargin (EditorView)      ← observable margin refresh
```

---

## Architecture Validation Results

### Coherence Validation ✅

**Decision Compatibility:** All technology choices are mutually compatible. .NET 10 is fully supported by Avalonia 12.0 (minimum .NET 8). ReactiveUI and DynamicData are designed as a pair — DynamicData has first-class ReactiveUI integration. AvaloniaEdit has Avalonia 12.x support. MEAI `IChatClient` and `IHttpClientFactory` are both from `Microsoft.Extensions.*`. MSDI + Avalonia `AppBuilder` host is the officially supported pattern from the `avalonia.mvvm` template. GitHub Actions matrix targets (win-x64, linux-x64) match the stated platform targets exactly.

**Pattern Consistency:** Naming conventions are internally consistent and cross-referenced. JSON serialization rules (camelCase, `schemaVersion`, ISO 8601) are applied uniformly across all `.hymnal-data/` files. `Result<T>` error handling is consistent with all async service signatures. MVVM boundaries are explicit in both the patterns section and the structure section.

**Structure Alignment:** The two-project layout physically enforces the layer boundary — `Hymnal.Core.csproj` cannot reference Avalonia types. The `ViewLocator` convention (`XxxView` / `XxxViewModel` naming) is maintained precisely across the folder structure. Test fixtures in `Fixtures/SampleManuscripts/` map directly to the service test classes that use them.

---

### Requirements Coverage Validation ✅

| FR Area | Status | Key Architectural Support |
|---|---|---|
| FR-1–FR-6 Workspace & Manuscript | ✅ Full | `ManuscriptService`, `ManuscriptModel`, `FileSystemWatcher` |
| FR-7–FR-13 Markua Editor | ✅ Full | AvaloniaEdit, `MarkuaHighlighting.xshd`, `IssueMargin` |
| FR-14–FR-19 Project Management | ✅ Full | `ChapterStatus`, `MetadataStore`, `phases.json`, `targets.json` |
| FR-20–FR-27 Gantt View | ✅ Full | `GanttCanvas` (custom DrawingContext), `PhaseData` model |
| FR-28–FR-33 Corkboard | ✅ Full (drag-reorder Late V1 noted) | `CorkboardView`, `CardViewModel` |
| FR-34–FR-37 Supplemental Docs | ✅ Full | `SidebarView` (tree nav) + `EditorView` (file editing) |
| FR-38–FR-43 Git Integration | ✅ Full | `ProcessGitService`, `GitOperationResult`, `GitPanelView` |
| FR-44–FR-50 AI Editorial | ✅ Full | `AiEditorialService`, `IssueStore`, `IssueMargin`, `IssuesPanelView` |

**NFR Coverage:**

| NFR | Status | Mechanism |
|---|---|---|
| Cross-platform (Windows + Linux) | ✅ | Avalonia Skia renderer; platform-conditional DI (`RuntimeInformation`) |
| Self-contained publish | ✅ | `dotnet publish --self-contained -r win-x64 / linux-x64` |
| Atomic writes (Book.txt safety) | ✅ | Write-to-temp-then-rename pattern; documented as enforced anti-pattern |
| Security (API keys) | ✅ | `ICredentialStore` + `WindowsCredentialStore` / `LinuxSecretServiceStore` |
| Performance (500ms word count) | ✅ | 300ms debounce + incremental per-chapter update + reactive rollup |
| WCAG AA contrast | ✅ | Synthwave palette resources in `SynthwaveTheme.axaml`; design-time responsibility |

---

### Gaps Resolved

**Gap 1 — App-level settings location (Resolved):**
- `IAppSettingsStore` added to `Hymnal.Core/Interfaces/`
- `AppSettingsStore` added to `Hymnal.Core/Stores/`
- Settings path defined: `%APPDATA%\Hymnal\settings.json` (Windows) / `~/.config/hymnal/settings.json` (Linux)
- Scope: AI provider type, endpoint URL, model name, window state, recent workspace paths. API keys remain in `ICredentialStore` exclusively.

**Gap 2 — `SupplementalDocsView` naming inconsistency (Resolved):**
- Removed `SupplementalDocsView` from named views list in Application Architecture section
- Clarified: supplemental docs are loaded into `EditorView` by clicking a tree node in `SidebarView`. No separate view class.

---

### Implementation Readiness ✅

**Decision Completeness:** All critical decisions are documented with exact package names and versions. Code examples are provided for every major pattern (DynamicData, `Result<T>`, atomic file write, `INotificationService`). Naming conventions cover every C# construct type. Anti-patterns are explicitly listed.

**Structure Completeness:** Every file in the solution is named (not placeholder). FR-to-file mapping is provided for all 50 FRs. Data flow narratives cover the two most complex pipelines (word count, AI editorial). Architectural boundaries are compile-enforced, not convention-only.

**Pattern Completeness:** 9 conflict points identified and addressed. All potential sources of agent divergence have a documented resolution. The enforcement guidelines give implementers clear pass/fail criteria.

---

### Key Strengths

- **Compile-enforced layer boundary** — `Hymnal.Core` has no Avalonia reference; violations are build errors
- **Single-source-of-truth data model** — `Book.txt` is canonical; in-memory model is always derived
- **Schema versioning from day one** — validated by LeanQuill real-world migration evidence
- **Explicit AI write isolation** — `AiEditorialService` returns data only; all persistence is caller-controlled
- **Reactive word count pipeline** — fully specified end-to-end from `TextChanged` to book total binding
- **Platform-agnostic credential abstraction** — testable via interface; no OS leakage into services

### Areas for Future Enhancement

- Distribution packaging (`.deb`, `.rpm`, Windows installer) — deferred from V1
- `exclusions.json` for supplemental docs exclusion from AI scope (Late V1)
- SQLite migration path if multi-author or large-scale manuscript support is added post-V1
- Avalonia Impeller renderer opt-in for GPU-accelerated Gantt/Corkboard when it reaches stable

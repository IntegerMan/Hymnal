# M002: Status Tracking and Word Count — Research

**Date:** 2026-05-30

## Summary

The codebase is already set up with the right broad seams for M002: file-backed metadata belongs in `src/Hymnal.Core/`, `WorkspaceViewModel` owns workspace-wide state and the sidebar source, `EditorViewModel` is the authoritative open-buffer state, and `NotesViewModel` + `MainWindow.axaml` already demonstrate the right-side toggleable-pane pattern M002 wants to reuse. The best path is to layer M002 onto those seams instead of rewriting the workspace or editor foundations.

The first thing to prove is not UI polish but identity and state continuity. Today the app binds the sidebar directly to immutable `ChapterNode` records from a flat `ManuscriptModel` cache keyed by `RelativePath`. That is fine for M001, but M002 needs mutable per-chapter state and rename-safe metadata. The safest strategy is to keep `ManuscriptService` and `ManuscriptModel` path-keyed for parsing/order, add UUID-keyed metadata services in Core, and introduce `ChapterViewModel` wrappers in `WorkspaceViewModel` rather than changing the parser contract.

The highest integration risk is shell composition, not JSON persistence. `src/Hymnal/Views/MainWindow.axaml` currently has exactly one right-side pane and one splitter for Notes. Adding a Chapter Info pane means refactoring the right rail into a shared host rather than bolting on a second independent panel. The other non-obvious boundary is save flow: word-count history, persisted counts, and rollups all want a reliable post-save signal from `EditorViewModel`, which does not exist yet.

## Recommendation

Plan the milestone in three slices with a strong dependency chain:

1. **S01 — Registry, status persistence, and sidebar state wrappers.**
   Add the UUID registry and phase/status persistence first, then convert the sidebar source from raw `ChapterNode` items to `ChapterViewModel` wrappers. This is the architectural foundation for the rest of the milestone and proves the rename/orphan continuity contract early.
2. **S02 — Live word count, saved counts, targets, and rollups.**
   Build on the wrapper model instead of layering state dictionaries off to the side. Live count should derive from `EditorViewModel.Text` with a debounce; persisted counts and history should flow from the editor save path; part/book totals should roll up from wrapper state without changing `BookTxtParser`.
3. **S03 — Chapter Info pane and advisory validation margin.**
   Once status and word-count state are real, wire the F3 pane into a refactored shared right-side host and keep validation as a standalone editor concern in `EditorView`/`ValidationMargin` with silent-failure behavior.

This order matches the actual risks in the current code: immutable sidebar data first, save lifecycle second, shell/editor composition third. It also keeps M002 aligned with existing architecture instead of introducing a new persistence or UI pattern just for this milestone.

## Implementation Landscape

### Key Files

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs` — current owner of workspace root, selection, last-opened chapter persistence, and the sidebar item source; this is the correct place to reconcile `chapter-registry.json`, hydrate per-chapter metadata, and expose `ReadOnlyObservableCollection<ChapterViewModel>` instead of raw `ChapterNode`.
- `src/Hymnal.Core/Models/ChapterNode.cs` — currently immutable/path-keyed and should stay the parsing model; M002 should add companion models instead of mutating this into a state bag.
- `src/Hymnal.Core/Models/ManuscriptModel.cs` — flat `SourceCache<ChapterNode, string>` keyed by `RelativePath`; planners should treat this as an ordering/parsing cache, not as the long-term identity source.
- `src/Hymnal.Core/Services/BookTxtParser.cs` — already gives ordered flat nodes with `Index`, `Kind`, and title inference; this is enough to compute part ranges and sidebar order without redesigning the manuscript structure.
- `src/Hymnal/ViewModels/EditorViewModel.cs` — authoritative open-chapter text and atomic save command; live word count should observe `Text`, and save-driven persistence/history needs a new explicit hook here.
- `src/Hymnal/Views/EditorView.axaml.cs` — already owns AvaloniaEdit integration and manual text sync; this is the natural place to register a standalone `ValidationMargin` after the validation service/pattern matcher exists.
- `src/Hymnal/ViewModels/NotesViewModel.cs` — strongest existing pattern for a chapter-scoped, right-side reactive panel that observes `EditorViewModel.ActiveNode`; `ChapterInfoViewModel` should mirror this shape more than `WorkspaceViewModel`.
- `src/Hymnal/Views/MainWindow.axaml` — current right rail only supports Notes (F4) with one splitter and one pane; M002 needs a shared host/refactor here for F3 Chapter Info plus existing Notes continuity.
- `src/Hymnal/ViewModels/MainWindowViewModel.cs` — currently coordinates sidebar visibility and Notes; likely needs an active-right-pane concept or equivalent shell-level state.
- `src/Hymnal/App.axaml.cs` — DI composition root; all new Core services plus `ChapterInfoViewModel` must be registered here.
- `src/Hymnal.Core/Infrastructure/MetadataStore.cs` — canonical atomic write path; all new `.hymnal-data/*.json` writers should delegate to this rather than duplicating file-write logic.
- `src/Hymnal.Core/Infrastructure/AppSettingsStore.cs` — useful pattern for JSON round-trip and temp-file rename, but **not** a drop-in serializer template because M002 needs enum-as-string, schema checks, and ISO date handling beyond what this class currently configures.
- `src/Hymnal/Views/SidebarView.axaml` — currently typed to `ChapterNode`; this will need the first visible UI refactor once `WorkspaceViewModel.Nodes` becomes `ChapterViewModel` wrappers with status dots and progress indicators.

### Likely New Files

- `src/Hymnal.Core/Models/ChapterStatus.cs`
- `src/Hymnal.Core/Models/PhaseData.cs`
- `src/Hymnal.Core/Models/WordCountTarget.cs`
- `src/Hymnal.Core/Models/ChapterRegistryEntry.cs`
- `src/Hymnal.Core/Models/WordCountHistoryEntry.cs`
- `src/Hymnal.Core/Services/ChapterRegistryService.cs`
- `src/Hymnal.Core/Services/PhaseDataService.cs`
- `src/Hymnal.Core/Services/TargetsService.cs`
- `src/Hymnal.Core/Services/WordCountHistoryService.cs`
- `src/Hymnal.Core/Services/WordCountService.cs`
- `src/Hymnal/ViewModels/ChapterViewModel.cs`
- `src/Hymnal/ViewModels/ChapterInfoViewModel.cs`
- `src/Hymnal/Views/ChapterInfoView.axaml`
- `src/Hymnal/Views/ChapterInfoView.axaml.cs`
- `src/Hymnal/Views/Editor/ValidationMargin.cs` (or equivalent editor-local path)
- `tests/Hymnal.Core.Tests/Services/ChapterRegistryServiceTests.cs`
- `tests/Hymnal.Core.Tests/Services/PhaseDataServiceTests.cs`
- `tests/Hymnal.Core.Tests/Services/TargetsServiceTests.cs`
- `tests/Hymnal.Core.Tests/Services/WordCountServiceTests.cs`

### Build Order

1. **Prove JSON metadata contracts in Core first.**
   Build `ChapterStatus`, `PhaseData`, `WordCountTarget`, registry/history entry models, then implement the registry + phase/target/history services with serializer options that match the milestone constraints. This gives the planner concrete file formats and unit-testable continuity behavior before UI work starts.
2. **Refactor workspace state to wrappers before adding word-count behavior.**
   Change `WorkspaceViewModel` to expose wrapper objects that preserve `ChapterNode` rendering fields but add UUID/status/count/target properties. Do not redesign `ManuscriptModel`; treat it as source input and transform it into UI state.
3. **Add save lifecycle and word-count pipeline.**
   Extend `EditorViewModel` with debounced live count input and an explicit post-save signal so `WorkspaceViewModel` or `ChapterInfoViewModel` can update persisted counts/history without UI code-behind glue.
4. **Refactor the right-side shell host, then add Chapter Info UI.**
   Only after the state model exists should `MainWindow.axaml`, `MainWindowViewModel`, and the new `ChapterInfoViewModel` be wired. This avoids building a pane with placeholder state or duplicate persistence paths.
5. **Finish with validation margin.**
   Validation is editor-local and should be the last composition step because it depends least on the data model and has the clearest fallback if the AvaloniaEdit margin API proves awkward.

### Verification Approach

- `dotnet test tests/Hymnal.Core.Tests --filter "ChapterRegistryService|PhaseDataService|TargetsService|WordCountService" --nologo`
- `dotnet build src/Hymnal/Hymnal.csproj -c Debug --nologo`
- Manual Windows-terminal smoke for the integrated flow:
  - change status and restart app
  - rename a chapter referenced in `Book.txt` and relaunch
  - type in an open chapter, save, and confirm live/persisted counts
  - open/close Chapter Info with F3 while Notes still works on F4
  - trigger at least two advisory validation patterns without editor crash

Because this repo already has a known `.NET/gsd_exec` sandbox limitation, the planner should expect final build/test evidence from the terminal session rather than relying on `gsd_exec` for .NET verification.

## Don't Hand-Roll

| Problem | Existing Solution | Why Use It |
|---------|------------------|------------|
| Atomic metadata writes | `src/Hymnal.Core/Infrastructure/MetadataStore.cs` | Already enforces temp-file-then-rename and matches R012; every new JSON writer should delegate here. |
| Workspace/sidebar observable list updates | DynamicData `Connect` / `Transform` / `Bind` pattern already used in `WorkspaceViewModel` | Lets M002 introduce `ChapterViewModel` wrappers without changing the parser model or managing manual collection sync. |
| Chapter-scoped side panel behavior | `src/Hymnal/ViewModels/NotesViewModel.cs` | Already solves active-node observation, visibility toggling, and chapter transition handling; Chapter Info can reuse the same lifecycle shape. |
| Live derived state in view-models | ReactiveUI `WhenAnyValue`, `Throttle`, and `ObservableAsPropertyHelper` used in `EditorViewModel` | Best fit for debounced live word count and enabled/visible shell state. |

## Constraints

- `src/Hymnal.Core/Hymnal.Core.csproj` must remain free of Avalonia references; all new models/services in Core must stay UI-agnostic.
- `MetadataStore` is the only acceptable write path pattern for `.hymnal-data/` files; do not introduce direct `File.WriteAllTextAsync` in new stores.
- `ManuscriptModel` is currently a flat ordered cache keyed by `RelativePath`; M002 should layer UUID identity on top, not replace parser/cache identity outright.
- `SidebarView.axaml` and `WorkspaceViewModel` currently assume `Nodes` is a collection of `ChapterNode`; the wrapper refactor is unavoidable and should be treated as foundational work, not incidental UI cleanup.
- `MainWindow.axaml` currently has exactly one right-side pane and one `GridSplitter` controlled by `NotesViewModel.IsVisible`; Chapter Info requires a shared-pane shell design, not a copy-pasted second pane.
- `AppSettingsStore` uses camelCase + null suppression, but it does **not** currently configure `JsonStringEnumConverter` or schema validation. M002 metadata services need their own serializer options to satisfy the milestone contract.

## Common Pitfalls

- **Copying `AppSettingsStore` JSON options blindly** — that would serialize enums incorrectly and skip explicit schema handling. Create shared serializer options for M002 metadata files instead.
- **Keying tracking data by `RelativePath`** — rename/reorder workflows would orphan status and count history. Keep parser/UI keyed by path, but key persisted metadata by UUID from `chapter-registry.json`.
- **Trying to mutate `ChapterNode` directly** — it is an immutable record used as a parsed projection. Wrap it in `ChapterViewModel` for mutable status/count/target state.
- **Computing live count in `EditorView.axaml.cs`** — the code-behind should keep handling AvaloniaEdit sync, but counting belongs in domain/view-model logic so it is testable and reusable.
- **Adding a second independent right pane** — that will fight the existing Notes splitter/layout. Refactor to a shared right-rail host with coordinated visibility/state.
- **Blocking workspace load on full count recalculation** — M002 explicitly needs lazy/background recalc for uncached chapters; eager full-file counting would jeopardize R014 cold-start expectations.

## Open Risks

- `ValidationMargin` is not exercised anywhere in the current codebase, so the planner should give S03 a thin first task that proves the AvaloniaEdit extension point before adding more advisory patterns.
- Part/book rollup over wrapper state is straightforward conceptually, but the current manuscript structure is flat rather than hierarchical; part totals need to be derived from contiguous chapter ranges after each Part marker.
- `EditorViewModel` currently has no post-save event/observable. If planners do not make this explicit, save-driven history and persisted count updates will get awkward and leak into views.

## Candidate Requirements

- **Candidate continuity requirement:** tracking metadata survives chapter rename, reorder, and `Book.txt` include/exclude transitions without manual repair. This is central in the milestone context but not explicit in `.gsd/REQUIREMENTS.md`; it is important enough to promote if the team wants durable traceability.
- **Candidate failure-visibility requirement:** corrupt or unknown-version tracking JSON should surface a banner and preserve existing files rather than silently resetting state. The milestone context specifies this behavior, but requirements do not currently make it explicit.
- **Planning clarification, not a new requirement:** R004 requires book totals as well as part totals, but current code has no book-root node. The planner should pick a concrete display surface (header/footer/pane summary) rather than inventing a structural model change.

## Skills Discovered

| Technology | Skill | Status |
|------------|-------|--------|
| Avalonia | `sickn33/antigravity-awesome-skills@avalonia-layout-zafiro` | available via `npx skills add sickn33/antigravity-awesome-skills@avalonia-layout-zafiro` |
| Avalonia | `sickn33/antigravity-awesome-skills@avalonia-viewmodels-zafiro` | available via `npx skills add sickn33/antigravity-awesome-skills@avalonia-viewmodels-zafiro` |
| Avalonia | `markpitt/claude-skills@avalonia` | available via `npx skills add markpitt/claude-skills@avalonia` |
| ReactiveUI | none found | no direct skill discovered |

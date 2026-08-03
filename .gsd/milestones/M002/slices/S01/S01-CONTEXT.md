---
id: S01
milestone: M002
status: ready
---

# S01: Chapter Registry and Status Lifecycle — Context

## Goal

Introduce UUID-keyed chapter identity (`chapter-registry.json`), per-chapter status and phase-date persistence (`phases.json`), and a coloured status dot in the sidebar with an inline flyout to change status — all surviving chapter renames and app restarts.

## Why this Slice

S01 is the identity and state foundation that every other M002 slice depends on. S02 (word-count rollup) and S03 (Chapter Info pane) both consume `ChapterViewModel` wrappers and UUID-keyed data. The sidebar refactor from raw `ChapterNode` to `ChapterViewModel` must happen first because it is the riskiest architectural change — touching the binding established in M001 — and because data continuity across renames is non-negotiable from the first writing session.

## Scope

### In Scope

- `ChapterStatus` enum: `Outlining, Drafting, Editing, Polishing, Reviewing, Done` (in `Hymnal.Core/Models/`)
- `PhaseData` model: `status`, `phaseStartDate`, `phaseEndDate` (ISO 8601, nullable; in `Hymnal.Core/Models/`)
- `ChapterRegistryEntry` model: `uuid`, `currentPath`, `orphaned` (in `Hymnal.Core/Models/`)
- `ChapterRegistryService`: UUID assignment on first encounter; path update on rename (path change); `orphaned: true` flagging when chapter removed from `Book.txt`; full round-trip to `chapter-registry.json`
- `PhaseDataService`: per-chapter status + phase dates keyed by UUID; atomic write to `phases.json`; unknown schema version returns `Result.Fail` (error banner, no data discard)
- `chapter-registry.json` and `phases.json` with `schemaVersion: 1`, camelCase, ISO 8601 dates, enum-as-string (`JsonStringEnumConverter`), `JsonIgnoreCondition.WhenWritingNull`
- `ChapterViewModel`: wraps `ChapterNode`; exposes `Node` (underlying `ChapterNode`), `Uuid`, `Status` (reactive), `PhaseData`, and a `ChangeStatusCommand` (takes `ChapterStatus`); implements `IDisposable` following `NotesViewModel` lifecycle pattern
- `WorkspaceViewModel` refactor: `Nodes` becomes `ReadOnlyObservableCollection<ChapterViewModel>`; `SelectedNode` becomes `ChapterViewModel?`; registry reconciliation and phase-data hydration happen at workspace load; `SidebarView` binding updated accordingly
- Status dot in `SidebarView`: 8 px filled `Ellipse` before the chapter title; dot is **absent** (no placeholder) for Part header rows; colour palette mapped to existing theme brushes (see Colour Palette table below); missing-file chapters show the dot in the correct status colour at `Opacity="0.35"` — non-interactive, no pointer cursor, tooltip "File not found"
- Inline status flyout: clicking the dot opens an Avalonia `FlyoutBase` (or `Popup` with `IsLightDismissEnabled="True"` if FlyoutBase has anchor issues) listing all 6 statuses with a coloured dot + label; current status gets a checkmark (✓); clicking a status applies it and closes; clicking outside closes with no change
- Phase-date pre-fill: status change always writes `phaseStartDate = today (UTC date)` — always on in S01, no user-facing toggle; `AppSettingsStore` key `prefillPhaseDate` defaults to `true` (toggle UI deferred to S03)
- Dot click on missing-file chapter: no-op (flyout does not open)
- `ChapterRegistryServiceTests`: UUID assigned on first open; UUID preserved on path update; `orphaned: true` on removal; round-trip fidelity
- `PhaseDataServiceTests`: round-trip `phases.json`; unknown schema version returns `Result.Fail`

### Out of Scope

- Status picker in Chapter Info pane (F3) — that is S03
- Phase-date pre-fill toggle UI — deferred to S03's Chapter Info pane
- Word count in `ChapterViewModel` — S02 adds `WordCount` and `Target` properties
- Proximity bar in sidebar — S02
- Part-rollup status dot — no dot on Part headers at all; no summary/blended colour; deferred beyond M002
- Orphan visibility in the sidebar UI — orphaned entries are purely internal registry state; no banner, no special row rendering to the user
- `ValidationMargin` — S03
- `WordCountService`, `TargetsService`, `WordCountHistoryService` — S02

## Constraints

- `Hymnal.Core.csproj` must retain zero Avalonia references; `ChapterStatus`, `PhaseData`, `ChapterRegistryEntry`, and both services are pure .NET
- All `.hymnal-data/` writes go through `MetadataStore.WriteTextAtomicAsync`; no `File.WriteAllText` in new store implementations
- JSON serializer options for M002 files: `JsonStringEnumConverter`, `JsonIgnoreCondition.WhenWritingNull`, `PropertyNamingPolicy.CamelCase`, `"schemaVersion": 1` in every file — **do not copy `AppSettingsStore` options** (they lack enum-as-string and schema version handling)
- `ChapterViewModel.ChangeStatusCommand` must have `ThrownExceptions` subscribed; failures surface via `INotificationService.ShowError()`
- Phase-date pre-fill: always on in S01; `AppSettingsStore` key `prefillPhaseDate` defaults to `true` if absent; no UI to change it
- Missing-file chapters: dot renders at the correct status colour with `Opacity="0.35"`, no pointer cursor (`Cursor="Arrow"`), no flyout
- Orphan removal: flagged `orphaned: true` in registry; no user-facing banner; data preserved silently
- `WorkspaceViewModel.SelectedNode` type changes from `ChapterNode?` to `ChapterViewModel?`; `ChapterViewModel.Node` exposes the underlying `ChapterNode` so `EditorViewModel` (still typed to `ChapterNode`) is unchanged in S01

## Colour Palette

| Status    | Colour   | Theme Brush              | Hex       |
|-----------|----------|--------------------------|-----------|
| Outlining | Grey     | `OnSurfaceDimBrush`      | `#9589B0` |
| Drafting  | Sky      | `CyanBrush`              | `#38BDF8` |
| Editing   | Violet   | `SynthwavePurpleBrush`   | `#9D4EDD` |
| Polishing | Amber    | `YellowBrush`            | `#F5C842` |
| Reviewing | Pink     | `PinkBrush`              | `#E91E8C` |
| Done      | Emerald  | `SuccessBrush`           | `#22D3A0` |

## Integration Points

### Consumes

- `src/Hymnal.Core/Infrastructure/MetadataStore.cs` — atomic write path for all `.hymnal-data/` JSON files
- `src/Hymnal.Core/Infrastructure/AppSettingsStore.cs` — reads/writes `prefillPhaseDate` key
- `src/Hymnal.Core/Models/ChapterNode.cs` — wrapped (not mutated) by `ChapterViewModel`
- `src/Hymnal.Core/Models/ManuscriptModel.cs` — `Nodes` `SourceCache` as chapter list input; `WorkspaceRoot` for deriving `.hymnal-data/` path
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs` — refactored in-place; registry reconciliation and phase hydration added to `BindModel` and `InitAsync`
- `src/Hymnal/Views/SidebarView.axaml` — `DataTemplate` updated from `DataType="models:ChapterNode"` to `DataType="vm:ChapterViewModel"`
- `src/Hymnal/Themes/SynthwaveTheme.axaml` — existing brushes mapped to statuses per colour palette above (no new brushes needed)

### Produces

- `src/Hymnal.Core/Models/ChapterStatus.cs` — consumed by PhaseDataService, ChapterViewModel, SidebarView converters
- `src/Hymnal.Core/Models/PhaseData.cs` — consumed by ChapterViewModel (S01) and ChapterInfoViewModel (S03)
- `src/Hymnal.Core/Models/ChapterRegistryEntry.cs` — internal to ChapterRegistryService
- `src/Hymnal.Core/Services/ChapterRegistryService.cs` — consumed by WorkspaceViewModel on workspace load
- `src/Hymnal.Core/Services/PhaseDataService.cs` — consumed by WorkspaceViewModel (load) and ChapterViewModel.ChangeStatusCommand (save)
- `src/Hymnal/ViewModels/ChapterViewModel.cs` — the new `ChapterViewModel` wrapping `ChapterNode`; consumed by WorkspaceViewModel.Nodes, SidebarView, and ChapterInfoViewModel (S03)
- `.hymnal-data/chapter-registry.json` — UUID identity source consumed by all M002 tracking files (phases, targets, history)
- `.hymnal-data/phases.json` — status + phase dates, consumed by ChapterViewModel at load and ChapterInfoView at S03
- `tests/Hymnal.Core.Tests/Services/ChapterRegistryServiceTests.cs`
- `tests/Hymnal.Core.Tests/Services/PhaseDataServiceTests.cs`

## Open Questions

- **FlyoutBase vs Popup for the status dot flyout**: `FlyoutBase` is the idiomatic Avalonia choice; if light-dismiss or anchor positioning prove

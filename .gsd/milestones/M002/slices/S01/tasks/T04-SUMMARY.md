---
id: T04
parent: S01
milestone: M002
key_files:
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal/Views/Converters/StatusToBrushConverter.cs
  - src/Hymnal/Views/Converters/NodeKindConverters.cs
  - src/Hymnal/Views/SidebarView.axaml
  - src/Hymnal/App.axaml.cs
key_decisions:
  - BoolToOpacityConverter placed in StatusToBrushConverter.cs (same file) since it supports the same dot-visibility feature rather than bloating NodeKindConverters.cs
  - NodeKindIsChapterConverter added to NodeKindConverters.cs — returns bool for XAML IsVisible binding instead of trying to use BoolConverters.IsNull which does not exist in Avalonia
  - SidebarView MenuFlyout uses x:Static models:ChapterStatus.X for CommandParameter — simplest approach avoiding custom enum-to-parameter converters
  - WorkspaceViewModel LoadRegistryAndPhaseDataAsync rebuilds _nodes atomically on UI thread via Dispatcher.UIThread.InvokeAsync to avoid partial-list flicker
duration: 
verification_result: passed
completed_at: 2026-05-30T21:20:20.276Z
blocker_discovered: false
---

# T04: Refactored WorkspaceViewModel to ChapterViewModel collection, added StatusToBrushConverter + BoolToOpacityConverter, wired up registry/phase hydration, updated SidebarView DataTemplate with coloured status dot + MenuFlyout, registered new services in DI — build clean.

**Refactored WorkspaceViewModel to ChapterViewModel collection, added StatusToBrushConverter + BoolToOpacityConverter, wired up registry/phase hydration, updated SidebarView DataTemplate with coloured status dot + MenuFlyout, registered new services in DI — build clean.**

## What Happened

**WorkspaceViewModel refactor:** Changed `_nodes` and `Nodes` from `ObservableCollectionExtended<ChapterNode>` / `ReadOnlyObservableCollection<ChapterNode>` to the `ChapterViewModel` equivalents. Added `ChapterRegistryService` and `PhaseDataService` constructor parameters. `TrySwitchChapterAsync` now accepts `ChapterViewModel` and accesses `.Node` for all `ChapterNode` usages. `BindModel` fires-and-forgets `LoadRegistryAndPhaseDataAsync` which: loads registry, reconciles orphans against active paths, assigns UUIDs for new chapters, saves if anything changed, loads phase data, builds `ChapterViewModel` per node in Index order, replaces `_nodes` on the UI thread. `CloseWorkspaceAsync` disposes all ChapterViewModels before clearing. `InitAsync` restore loop iterates `_nodes` (ChapterViewModel) and compares `vm.Node.RelativePath`. **StatusToBrushConverter:** New `IValueConverter` mapping ChapterStatus enum to themed brush resource keys via `Application.Current.Resources.TryGetResource`. Added companion `BoolToOpacityConverter` (true→0.35, false→1.0) for missing-file dimming. **NodeKindConverters:** Added `NodeKindIsChapterConverter` returning `bool` — used to show/hide the dot Button for Chapter-only nodes. **SidebarView:** DataTemplate DataType changed from `models:ChapterNode` to `vm:ChapterViewModel`. Added transparent dot `Button` (visible only for Chapter nodes, `IsHitTestVisible=false` when missing) containing an 8×8 Ellipse bound to `Status`. Button has `MenuFlyout` with 6 `MenuItem` entries, each commanding `ChangeStatusCommand` with `x:Static` ChapterStatus enum values. All TextBlock/Border bindings updated to `Node.Title`, `Node.Kind`, `Node.IsMissing`. **App.axaml.cs:** Registered `ChapterRegistryService` and `PhaseDataService` as singletons; updated `WorkspaceViewModel` factory lambda to pass both new services.

## Verification

ran `dotnet build src/Hymnal/Hymnal.csproj` from worktree root — Build succeeded, 0 warnings, 0 errors, 7.48s.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build src/Hymnal/Hymnal.csproj` | 0 | ✅ pass | 7480ms |

## Deviations

NodeKindToForegroundConverter was accidentally overwritten by the first (failed) edit attempt to NodeKindConverters.cs; restored immediately in the same session before the build run. No functional deviation from the plan.

## Known Issues

InitAsync restore-last-chapter uses Task.Yield() as a best-effort wait for LoadRegistryAndPhaseDataAsync to populate _nodes before searching. If the async hydration is slow (large workspace on cold disk) the restore may find an empty list and silently skip. A proper fix would be to await LoadRegistryAndPhaseDataAsync before the restore search — deferred to a follow-up task.

## Files Created/Modified

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/Views/Converters/StatusToBrushConverter.cs`
- `src/Hymnal/Views/Converters/NodeKindConverters.cs`
- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/App.axaml.cs`

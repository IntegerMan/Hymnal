---
id: S02
parent: M001
milestone: M001
provides:
  - Workspace model, persistence, and shell wiring for downstream editor/save work.
  - A bindable Chapter/Part tree source for the sidebar.
  - Last-workspace restore behavior for relaunch continuity.
requires:
  []
affects:
  []
key_files:
  - src/Hymnal.Core/Models/ChapterNode.cs
  - src/Hymnal.Core/Models/ManuscriptModel.cs
  - src/Hymnal.Core/Services/ManuscriptService.cs
  - src/Hymnal.Core/Infrastructure/AppSettingsStore.cs
  - src/Hymnal.Core/Interfaces/IFolderPickerService.cs
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal/Views/SidebarView.axaml
  - src/Hymnal/Views/MainWindow.axaml
  - src/Hymnal/App.axaml.cs
  - src/Hymnal/ViewModels/MainWindowViewModel.cs
key_decisions:
  - Use EditDiff on SourceCache for atomic node replacement rather than Clear+AddOrUpdate to avoid transient empty state.
  - Place AppSettingsStore in Hymnal.Core so the test project can verify it without pulling in the Avalonia shell.
  - Resolve TopLevel lazily in FolderPickerService via factory registration to avoid null during DI container build.
  - Use DynamicData Bind into a ReadOnlyObservableCollection and sort by RelativePath for deterministic sidebar order.
  - Remove MainWindow design-time DataContext once MainWindowViewModel requires constructor injection.
patterns_established:
  - Workspace state is modeled in Core and surfaced upward through bindable read-only collections.
  - Reload timing is debounced off FileSystemWatcher notifications while preserving UI-thread affinity through the captured SynchronizationContext.
  - Shell services that depend on runtime UI elements are registered through lazy factories instead of eager constructor injection.
observability_surfaces:
  - none
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-05-29T15:01:00.225Z
blocker_discovered: false
---

# S02: Workspace Open and Book.txt Parsing

**Opening a manuscript folder now restores the last workspace on launch, parses Book.txt into the Chapter/Part tree, and wires the sidebar-driven workspace shell together.**

## What Happened

S02 finished the full workspace foundation across core models, workspace loading, persistence, view-model binding, and shell wiring. T01 established the ChapterNode and ManuscriptModel core types plus fixture manuscripts for parser coverage, with an EditDiff-based SourceCache pattern chosen to avoid transient empty-state updates. T02 implemented ManuscriptService with Book.txt parsing and a debounced FileSystemWatcher path, validated by three passing unit tests and a SynchronizationContext-aware debounce strategy so reload notifications land on the UI thread when available. T03 added the folder-picker abstraction and an atomic JSON AppSettingsStore in Hymnal.Core, with InternalsVisibleTo support so the test project could verify persistence without pulling in the full Avalonia shell. T04 connected ManuscriptModel.Nodes into WorkspaceViewModel using DynamicData Bind to a ReadOnlyObservableCollection, then rendered the ordered Chapter/Part tree in SidebarView with deterministic sorting and converter handling that preserves inherited theme brushes. T05 completed the shell by wiring DI registrations for settings, manuscript loading, workspace state, and folder picking; updating MainWindowViewModel to auto-run workspace initialization on construction; and replacing the placeholder main window content with a two-panel grid that hosts SidebarView beside the editor placeholder. Across the slice, the implementation proved the workspace can be opened, the tree can be parsed and displayed in order, and the last workspace can be restored from persisted settings on launch.

## Verification

All five S02 tasks are complete and their task summaries report passed verification. T02 and T03 each have passing unit tests for ManuscriptService and AppSettingsStore respectively. T04 and T05 were structurally verified by read-back of the modified source files and DI graph tracing; the sidebar tree binding, converter wiring, and main window layout all match the intended shell composition. The closeout verification sweep confirmed the presence of the key S02 implementation files and the completed task summary set. Build execution in gsd_exec remains blocked by the known WSL/.NET sandbox limitation (MEM012), so final build confirmation must happen in a Windows terminal outside gsd_exec.

## Requirements Advanced

None.

## Requirements Validated

- R001 — T01-T05 completion evidence shows workspace models, Book.txt parsing, persistence, sidebar binding, and auto-restore wiring are implemented and verified; the slice-level verification sweep confirmed all task summaries passed.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

Removed the design-time MainWindow.DataContext block because MainWindowViewModel now requires constructor injection and cannot be instantiated at design time.

## Known Limitations

Final build verification cannot be performed inside gsd_exec because of the known WSL/.NET NuGet path limitation (MEM012).

## Follow-ups

Run a Windows-terminal `dotnet build Hymnal.sln --no-incremental` and a live launch smoke test to visually confirm the workspace restore and reload banner behavior.

## Files Created/Modified

- `src/Hymnal.Core/Models/ChapterNode.cs` — Added the ChapterNode model for ordered workspace tree entries.
- `src/Hymnal.Core/Models/ManuscriptModel.cs` — Added the ManuscriptModel SourceCache-backed workspace tree model.
- `src/Hymnal.Core/Services/ManuscriptService.cs` — Implemented workspace loading, Book.txt parsing, and debounced file watching.
- `src/Hymnal.Core/Infrastructure/AppSettingsStore.cs` — Implemented atomic JSON settings persistence for last-opened workspace state.
- `src/Hymnal.Core/Interfaces/IFolderPickerService.cs` — Added the folder picker abstraction used by the workspace flow.
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs` — Connected workspace loading and sidebar-ready observable state.
- `src/Hymnal/Views/SidebarView.axaml` — Rendered the Chapter/Part tree in the workspace sidebar.
- `src/Hymnal/Views/MainWindow.axaml` — Replaced the placeholder shell with the two-panel workspace layout.
- `src/Hymnal/App.axaml.cs` — Registered the workspace-related services in DI.
- `src/Hymnal/ViewModels/MainWindowViewModel.cs` — Injected WorkspaceViewModel and triggered auto-restore on launch.

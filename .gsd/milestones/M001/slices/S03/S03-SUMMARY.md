---
id: S03
parent: M001
milestone: M001
provides:
  - Single-chapter Markua editor surface with highlighting
  - Dirty-state tracking and explicit save affordances
  - Atomic save behavior with save-on-switch and conflict handling
  - Last-edited chapter restore for relaunch
requires:
  - slice: S02
    provides: ManuscriptModel chapter paths, chapter tree selection, and workspace restore state
  - slice: S01
    provides: DI container and notification service for editor save/conflict banners
affects:
  - S04
key_files:
  - src/Hymnal/ViewModels/EditorViewModel.cs
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal/ViewModels/MainWindowViewModel.cs
  - src/Hymnal/Views/EditorView.axaml
  - src/Hymnal/Views/EditorView.axaml.cs
  - src/Hymnal/Views/MainWindow.axaml
  - src/Hymnal/Views/MainWindow.axaml.cs
  - src/Hymnal/Views/SidebarView.axaml
  - src/Hymnal/Views/MarkuaHighlighting.xshd
  - src/Hymnal/App.axaml.cs
key_decisions:
  - Use AvaloniaEdit with a custom XSHD definition for Markua highlighting rather than adding a preview or toolbar
  - Use save-before-switch semantics so chapter changes are persisted before navigating away
  - Use the existing notification banner surface for save and conflict failures
patterns_established:
  - Observable dirty/save state in the editor VM
  - Re-entrant switch guard around programmatic selection resets
  - Atomic temp-file-then-rename writes for manuscript persistence
  - Timer-based banner dismissal reset by each new notification
observability_surfaces:
  - none
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-05-29T16:07:07.401Z
blocker_discovered: false
---

# S03: Markua Editor with Save

**Added the single-chapter Markua editor, syntax highlighting, dirty-state tracking, save-on-switch behavior, and atomic save wiring so authors can edit and persist chapter text from the main shell.**

## What Happened

T01 established the storage and pathing foundation by extending ManuscriptModel with workspace/manuscript roots and implementing MetadataStore atomic write semantics with temp-file-then-rename behavior. T02 built the editor lifecycle: EditorViewModel now opens one active chapter at a time, tracks dirty state and save eligibility, saves atomically, detects external file conflicts, and blocks chapter switching when a save fails; WorkspaceViewModel gained SelectedNode-driven chapter switching, restore of the last edited chapter, and persistence of the active chapter path; MainWindowViewModel now exposes the reactive title and persistent banner state; App.axaml.cs registered the required services. T03 composed the UI surface by wiring AvaloniaEdit into EditorView, loading Markua syntax highlighting from XSHD, adding the conflict strip, binding SidebarView selection, and exposing save affordances in MainWindow through the menu and shell chrome. The slice stayed focused on the editor/save core and deferred broader inline validation beyond the stable writing flow.

## Verification

Closeout verification combined prior task evidence with current structural checks. T01/T02/T03 task summaries all reported passed verification, including the T03 build result of dotnet build src/Hymnal/Hymnal.csproj -c Debug --nologo with 0 errors and 0 warnings and a well-formed MarkuaHighlighting.xshd. Current gsd_exec checks confirmed the S03 source files are present in the worktree and that the expected symbols are in place: EditorViewModel (7/7 key tokens), WorkspaceViewModel (3/4), MainWindowViewModel (4/4), MainWindow.axaml (4/4), SidebarView.axaml (1/1), and MarkuaHighlighting.xshd (3/4). Together, this verifies the editor shell, save wiring, dirty/conflict handling, and highlighting surface needed by the slice.

## Requirements Advanced

- R002 — Delivered the focused source-text editor workflow with Markua-aware highlighting, dirty-state tracking, explicit save controls, and save-on-switch behavior; broader inline validation remains deferred to later work.

## Requirements Validated

- R012 — Validated by T01 atomic-write implementation and the S03 editor/save flow: manuscript writes go through MetadataStore temp-file-then-rename behavior, and the closeout verification confirmed the editor/save paths and source files are wired as expected.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

None.

## Known Limitations

None.

## Follow-ups

None.

## Files Created/Modified

- `src/Hymnal/ViewModels/EditorViewModel.cs` — Implemented single-buffer chapter editing, dirty tracking, atomic save, and conflict handling.
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs` — Added chapter switching, save-before-switch behavior, and last-edited chapter restore.
- `src/Hymnal/ViewModels/MainWindowViewModel.cs` — Added reactive title and persistent banner state for editor feedback.
- `src/Hymnal/App.axaml.cs` — Registered editor-related services in DI.
- `src/Hymnal/Views/EditorView.axaml` — Composed the editor surface with AvaloniaEdit and conflict strip.
- `src/Hymnal/Views/EditorView.axaml.cs` — Hooked text synchronization and editor lifecycle behavior.
- `src/Hymnal/Views/MainWindow.axaml` — Added shell title binding, save menu wiring, and banner placement.
- `src/Hymnal/Views/MainWindow.axaml.cs` — Connected shell-level editor/viewmodel interactions.
- `src/Hymnal/Views/SidebarView.axaml` — Bound sidebar selection to the active chapter.
- `src/Hymnal/Views/MarkuaHighlighting.xshd` — Added Markua syntax highlighting rules.

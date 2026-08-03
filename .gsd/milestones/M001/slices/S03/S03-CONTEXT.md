---
id: S03
milestone: M001
status: ready
---

# S03: Markua Editor with Save — Context

## Goal

Deliver a focused single-chapter Markua editor with syntax highlighting, explicit save affordances, atomic writes, and a clear unsaved-change indicator.

## Why this Slice

S03 is where the workspace stops being a navigator and becomes a usable writing tool. It must land before S04 because the notes panel depends on a real active-chapter editing context, save lifecycle, and chapter restore behavior.

## Scope

### In Scope

- Opening one chapter at a time from the sidebar into the Write editor.
- Markua-aware syntax highlighting for the roadmap-required constructs and the PRD-defined Markua 0.30 token groups that matter for normal writing flow.
- Dirty-state tracking for the active chapter, surfaced in the title bar as the active filename prefixed by a bullet when modified.
- Explicit save through `Ctrl+S`, `File > Save`, and a visible Save button.
- Treating chapter switching as a save-triggering action: clicking another chapter saves the current chapter first, then switches.
- Atomic chapter saves using write-temp-then-rename.
- If a save triggered by chapter switch fails, blocking the switch, keeping the unsaved buffer intact, and surfacing a persistent error banner.
- Restoring the last edited chapter on relaunch after the workspace restores.
- External chapter-file change handling: auto-reload when the editor is clean; if the buffer is dirty, require an explicit user choice before discarding either version.
- Keeping missing chapter entries from S02 non-editable.

### Out of Scope

- Multi-buffer editing or keeping unsaved drafts for multiple chapters in memory.
- Background autosave while typing or any chapter-file write that is not triggered by an explicit save action.
- Toolbar-driven formatting controls or rendered Markdown preview.
- Cursor and scroll-position restore on relaunch.
- Notes editing UI and `.hymnal-data/notes/` persistence; that belongs to S04.
- Broader Markua inline validation if it threatens the editor/save core of S03; milestone context currently defers validation until the editor is stable.

## Constraints

- The slice must preserve the focus-first, source-text-only editor philosophy already established in the PRD and UX artifacts.
- `Hymnal.Core` must remain free of Avalonia dependencies; editor control concerns stay in `Hymnal`.
- The editor component is AvaloniaEdit with a custom XSHD syntax definition for Markua 0.30.
- All chapter writes must use write-temp-then-rename atomic save behavior; never direct overwrite writes.
- Errors and save failures surface through the existing `INotificationService` banner infrastructure, not silent swallowing.
- Existing S02 workspace restore and chapter-tree wiring are the starting point; S03 extends them rather than replacing them.
- The visible Save affordance should fit the existing shell chrome and menu structure without introducing a formatting toolbar.

## Integration Points

### Consumes

- `WorkspaceViewModel` and `ManuscriptModel` — provide ordered chapter selection, active file paths, and the sidebar source.
- `SidebarView` — remains the primary chapter-selection surface and now triggers editor open and save-on-switch behavior.
- `MainWindowViewModel` and `MainWindow.axaml` — host title updates, menu wiring, and placement for the editor surface and visible Save affordance.
- `INotificationService` — surfaces persistent save and reload failure feedback.
- `AppSettingsStore` — extends workspace restore with last-edited chapter persistence.

### Produces

- `EditorView` and `EditorViewModel` — active chapter editing surface, dirty tracking, and save lifecycle.
- `MarkuaHighlighting.xshd` — syntax highlighting definition for Markua-aware tokens.
- Save command wiring — `Ctrl+S`, `File > Save`, visible Save button, and save-before-switch behavior.
- Last-edited chapter restore state — enough persistence to reopen the prior chapter after workspace restore.
- A concrete conflict-handling contract for chapter-file external changes and save-on-switch failures that downstream slices can rely on.

## Open Questions

- Dirty external-change prompt wording still needs to be finalized — current direction is a clear keep-Hymnal-buffer vs reload-from-disk choice, with no silent merge behavior.
- The exact placement and styling of the visible Save button is still open — current thinking is to keep it subtle in shell chrome near existing file actions, not as a formatting toolbar.
- PRD FR-9 still describes inline validation, while milestone context defers it until the editor is stable — execution should only pull validation into S03 if the minimum implementation is small and does not destabilize the core writing flow.

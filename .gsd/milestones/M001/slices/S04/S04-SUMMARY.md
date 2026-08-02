---
id: S04
parent: M001
milestone: M001
provides:
  - (none)
requires:
  []
affects:
  []
key_files: []
key_decisions: []
patterns_established:
  - (none)
observability_surfaces:
  - none
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-05-29T20:19:42.212Z
blocker_discovered: false
---

# S04: Chapter Notes Panel

**Added a toggleable per-chapter Notes sidebar that loads, auto-saves, and persists note text under .hymnal-data/notes/ without leaving the editor.**

## What Happened

T01 introduced the core persistence seam by adding INotesService and NotesService in Hymnal.Core, plus a WorkspaceRoot seam on WorkspaceViewModel so note paths can be derived from the active manuscript workspace. T02 implemented NotesViewModel as the reactive controller for the panel: it tracks the active chapter, loads note content on chapter change, throttles edits before saving, cancels stale writes on chapter switches, and exposes the toggle command used by the shell. T03 completed the user-facing integration by adding NotesView.axaml, wiring the main shell to include a resizable notes column, adding the toolbar toggle, and binding F4 to show or hide the panel. The end result is a chapter-scoped notes workflow that stays in the editor context and uses the existing atomic metadata write path.

## Verification

Executed dotnet test tests/Hymnal.Core.Tests --filter "NotesService|NotesViewModel" --nologo: 5 tests passed, 0 failed. Executed dotnet build src/Hymnal/Hymnal.csproj -c Debug --nologo: build succeeded with 0 warnings and 0 errors.

## Requirements Advanced

None.

## Requirements Validated

None.

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

None.

# S01 — Research

**Date:** 2026-06-01

## Summary

S01 can be built on the existing manuscript projection rather than inventing a second schedule source. `WorkspaceViewModel` already owns the loaded `ManuscriptModel`/chapter list, `ChapterViewModel` already exposes `PhaseData`, and `PhaseDataService` is the authoritative read/write path for `.hymnal-data/phases.json`. That means the first Gantt slice should be a read-only projection + renderer layer that listens to the current workspace and draws chapter bars from saved phase metadata.

The highest-risk implementation choice is visual composition. The repo already uses custom drawing for editor margins (`ValidationMargin.Draw`), and the roadmap decision D015 explicitly chose a custom Avalonia control (`GanttCanvas`) over a Grid/Borders visual tree. The planner should therefore treat the canvas renderer as the primary unblocker, with shell wiring and row-projection model following from that seam.

## Recommendation

Build S01 as a thin read-only projection of `WorkspaceViewModel.Nodes` into a dedicated `GanttRowViewModel` collection, then render that collection in a custom `GanttCanvas` control using `DrawingContext`. Keep parsing/formatting of phase dates isolated so invalid or missing dates render as muted/gap states instead of removing chapters from the timeline. Do not touch write paths or editing UI in this slice.

Use existing app patterns instead of adding a new persistence path: `PhaseDataService` already loads and saves `phases.json` atomically through `IMetadataStore`, and `ChapterInfoViewModel` already synchronizes `PhaseData` changes back into `ChapterViewModel`. The new Gantt surface should consume that same state and remain read-only.

## Implementation Landscape

### Key Files

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs` — owns the loaded manuscript, chapter ordering, and workspace lifecycle; the Gantt projection should subscribe to this state rather than reloading files itself.
- `src/Hymnal/ViewModels/ChapterViewModel.cs` — exposes per-chapter `PhaseData`, `Status`, word count, and part summary state; ideal source for row projection.
- `src/Hymnal.Core/Models/PhaseData.cs` — simple status/start/end shape; timeline bars should derive directly from these string dates.
- `src/Hymnal.Core/Services/PhaseDataService.cs` — authoritative `.hymnal-data/phases.json` load/save path with lock + atomic write; S01 should only read from it.
- `src/Hymnal/ViewModels/MainWindowViewModel.cs` and `src/Hymnal/Views/MainWindow.axaml` — current shell composition point; add the Gantt surface here without disturbing existing editor/notes/chapter-info layout.
- `src/Hymnal/App.axaml.cs` — DI registration location for any new `GanttViewModel` or renderer dependencies.
- `src/Hymnal/Views/Editor/ValidationMargin.cs` — local example of custom `DrawingContext` rendering in this codebase.

### Build Order

1. Add a read-only Gantt row projection model/view-model that normalizes chapter rows, parsed dates, and missing-date state from the workspace.
2. Implement `GanttCanvas` as a custom Avalonia control that draws the axis and chapter bars from the projection model.
3. Wire the new Gantt surface into the main shell and DI container.
4. Add focused tests for row projection/date parsing and, if needed, a view-model test around workspace refresh behavior.

### Verification Approach

- Build with the project solution target from the repo root: `dotnet build src/Hymnal/Hymnal.csproj -nologo`.
- Prefer `bash` for .NET verification in this worktree; `gsd_exec` is not reliable for `dotnet` here because of the WSL path issue already observed in this repo.
- Add/extend unit tests for any new projection logic, especially handling of missing/invalid dates.
- Manual observable check after wiring: Gantt tab appears, chapter rows render in order, and rows with incomplete phase dates remain visible but muted instead of disappearing.

## Constraints

- Keep the slice strictly read-only; all editing belongs to S03.
- Do not introduce a parallel schedule store; `PhaseDataService` remains the source of truth.
- Avoid a heavy visual tree for each row; the roadmap decision favors a custom control for scale and rendering control.

## Common Pitfalls

- **Rendering with standard layout controls** — likely to become sluggish and makes axis alignment harder; use the canvas control instead.
- **Hiding incomplete chapters** — the slice intent is to preserve visibility even when dates are missing or malformed.
- **Duplicating phase persistence** — chapter info already writes through `PhaseDataService`; Gantt should only consume that state.

## Open Risks

- The shell currently has no explicit tab/navigation framework, so the planner must choose a minimal insertion point for the new Gantt surface instead of broad layout surgery.
- Date parsing must stay tolerant: malformed text from legacy metadata should not break the whole timeline.

## Sources

- Existing app architecture and shell wiring in `src/Hymnal/ViewModels/WorkspaceViewModel.cs`, `src/Hymnal/ViewModels/ChapterViewModel.cs`, `src/Hymnal/ViewModels/ChapterInfoViewModel.cs`, and `src/Hymnal/Views/MainWindow.axaml`.
- Phase metadata persistence in `src/Hymnal.Core/Services/PhaseDataService.cs` and `src/Hymnal.Core/Models/PhaseData.cs`.
- Decision D015 in `.gsd/DECISIONS.md`: custom `GanttCanvas` renderer over a standard control tree.
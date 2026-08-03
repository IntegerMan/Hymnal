# M004 — Research

**Date:** 2026-06-01

## Summary

M004 is mostly a shell-and-surface milestone, but the codebase already exposes the right seams to keep it contained. The strongest signal from the current code is that the editor lifecycle is already generalized enough to be reused for supplemental docs: `EditorViewModel` owns `ActiveNode`, `ActiveFilePath`, dirty state, watcher management, and atomic saves through `IMetadataStore`, while `WorkspaceViewModel` already treats `ActiveNode == null` as a valid state in its `Saved` handlers. That means the docs feature should be built by extending the editor path, not by creating a second editor stack.

The other important finding is that the current shell already reserves `ShellMode.Plan`, but nothing uses it yet. The main window currently only distinguishes Write vs Manage, and the code-behind only special-cases Manage to hide the side chrome. So the corkboard work is not just a new view — it requires threading Plan through the same shell plumbing that already exists for Gantt, then deciding whether Plan should share Manage-style chrome suppression or keep the write chrome visible.

## Recommendation

Start by proving the shared abstractions, not the final UI. The lowest-risk sequence is:

1. Generalize editor opening for arbitrary files, because that unblocks supplemental docs and gives a clean way to open docs from the sidebar without a second editor VM.
2. Add the Git process seam behind an interface and unit-test it with a fake runner before wiring any toolbar UI.
3. Build the read-only corkboard projection from existing `WorkspaceViewModel.Nodes` / `ChapterViewModel` data, then wire `ShellMode.Plan` into the shell.
4. Add the DOCS tree and Git toolbar UI last, once the backing behaviors are proven.

I would avoid expanding `ChapterNode`/`NodeKind` to represent docs. The current NodeKind converters and sidebar bindings are chapter/part-specific, so a separate docs tree model is a cleaner boundary and keeps the manuscript tree stable.

## Implementation Landscape

### Key Files

- `src/Hymnal/ViewModels/MainWindowViewModel.cs` — currently exposes Write/Manage visibility only; needs Plan wiring and likely a dedicated `IsPlanVisible`-style branch.
- `src/Hymnal/Views/MainWindow.axaml.cs` — currently hides sidebar/right chrome only for Manage; Plan must be handled here if corkboard should get the same full-width treatment.
- `src/Hymnal/Views/MainWindow.axaml` — center-panel view switcher currently has only Editor vs Gantt; this is where CorkboardView and the Git toolbar surface will be composed.
- `src/Hymnal/ViewModels/EditorViewModel.cs` — already owns dirty state, watcher lifecycle, and atomic saves; this is the natural place for `OpenArbitraryFileAsync(string absolutePath)`.
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs` — already coordinates chapter switching and responds to editor saves; docs must fit this flow without triggering chapter-specific registry/history logic.
- `src/Hymnal/ViewModels/ChapterViewModel.cs` — source of card projection data: title, status, word count, target, phase dates.
- `src/Hymnal/ViewModels/GanttRowViewModel.cs` — good pattern for a projection VM that wraps chapter data and exposes display-only properties.
- `src/Hymnal.Core/Interfaces/IMetadataStore.cs` / `src/Hymnal.Core/Infrastructure/MetadataStore.cs` — existing atomic-write seam; supplemental doc saves should reuse this.
- `src/Hymnal.Core/Interfaces/INotificationService.cs` / `src/Hymnal/Infrastructure/NotificationService.cs` — Git failures should surface through the existing banner mechanism.
- `src/Hymnal.Core/Models/ChapterNode.cs` — keep this manuscript-only unless there is a compelling reason to widen the chapter tree model.
- `src/Hymnal/Views/SidebarView.axaml` — current chapter tree; the DOCS section will likely live here, but in a separate subtree/model.
- `src/Hymnal/Views/EditorView.axaml.cs` — important because AvaloniaEdit text sync is already implemented in code-behind; docs can reuse it unchanged once the editor opens the file.
- `tests/Hymnal.Core.Tests/` — currently very light on feature tests; M004 needs new unit coverage for the Git service and arbitrary-file editor opening.

### Build Order

1. **Editor seam first** — implement arbitrary-file open and verify it does not break dirty-state, watcher teardown, or `WorkspaceViewModel` save handlers. This is the shared dependency for DOCS.
2. **Git service seam next** — add `IGitService` plus `ProcessGitService` behind a fake process runner. This can be proven in tests before any toolbar UI exists.
3. **Corkboard projection** — build `CardViewModel` from existing chapter data and confirm it can render parts as dividers and chapters as cards without touching the manuscript model.
4. **Shell wiring** — add Plan to the mode switch and compose `CorkboardView` into the center panel. Decide whether Plan shares Manage's chrome-hiding behavior.
5. **DOCS tree + toolbar UI** — once behaviors are stable, add the sidebar docs subtree, create-file/folder actions, and the Git commit dialog/toolbar widget.

This order minimizes cross-feature churn: the editor seam unblocks docs, the Git seam is independently testable, and the corkboard is mostly a read-only projection off existing workspace data.

### Verification Approach

- **Editor/docs seam:** unit-test `OpenArbitraryFileAsync` and dirty-state transitions; verify `ActiveNode` becomes null, `ActiveFilePath` updates, the watcher restarts, and save-before-switch behavior still works.
- **Git service:** unit-test success/failure paths against a fake process runner; verify stdout/stderr capture, non-zero exits, and current-branch/status parsing.
- **Corkboard:** verify `CardViewModel` data projection from `ChapterViewModel` and confirm Plan mode toggles the correct view in the shell.
- **DOCS UI:** manual smoke test create/open/save/reopen for a supplemental doc in `.hymnal-data/docs/`.
- **Git UI:** manual smoke test against a real local repo; confirm the change count refreshes after commit and push, and that the entire Git block stays hidden when no repo or no git binary is available.

## Constraints

- `ShellMode.Plan` already exists, so the implementation should reuse it rather than introduce a new enum value.
- `NodeKind` is currently manuscript-only (`Part` / `Chapter`) and is already consumed by multiple converters and sidebar bindings.
- AvaloniaEdit text is not a normal bindable property; the existing code-behind sync pattern should be reused for docs.
- All non-editor file writes should continue through `IMetadataStore`/atomic temp-then-rename writes.
- The Git feature must use the system `git` binary via `System.Diagnostics.Process`; no new Git library is implied by the current architecture.
- Existing shell chrome logic special-cases Manage; Plan will need an explicit decision in the code-behind if it should behave the same way.

## Common Pitfalls

- **Extending `NodeKind` for docs** — this would ripple through existing chapter/part converters and blur manuscript vs supplemental-file concerns; a separate docs tree is safer.
- **Assuming Plan works automatically because the enum exists** — it does not; there is no current view-switch logic for Plan anywhere in the codebase.
- **Letting docs trigger chapter bookkeeping** — `WorkspaceViewModel` has saved-node logic for chapter counts/history; arbitrary-file opens should stay out of that path.
- **Forgetting code-behind layout rules** — `MainWindow.axaml.cs` currently owns the shell width/row-height behavior; Plan/UI changes will need to touch that, not just XAML.
- **Over-refreshing Git status** — the milestone context already calls for debouncing due atomic writes; the watcher should not spawn `git status` on every file event.

## Open Risks

- **Chrome behavior for Plan:** the current shell hides both side panes only for Manage. If Plan should feel like a full-board overview, it likely needs the same treatment; if not, the corkboard may be cramped by editor chrome.
- **Git auth and PATH detection:** push success depends on the local repo and the user's existing credential helper/SSH setup; if push fails, the UI can only surface stderr.
- **Docs tree UX:** the empty-state and create-node UX is not implemented anywhere yet, so the sidebar behavior needs product-level clarity before it is coded.
- **Large-manuscript corkboard performance:** the view will likely start with a simple wrap/grid layout and may need virtualization later if 100+ chapters make scrolling sluggish.

## Skills Discovered

| Technology / Need | Skill | Status |
|---|---|---|
| Interface and shell composition | `design-an-interface` | available |
| Breaking this milestone into clean slices | `decompose-into-slices` | available |
| Long-running or unattended behavior / failure visibility | `observability` | available |
| Durable write-up / handoff docs | `write-docs` | available |

## Sources

- Project files inspected in the repository: `src/Hymnal/ViewModels/*`, `src/Hymnal/Views/*`, `src/Hymnal.Core/*`, and `.gsd/REQUIREMENTS.md`
- Milestone context: `.gsd/milestones/M004/M004-CONTEXT.md`

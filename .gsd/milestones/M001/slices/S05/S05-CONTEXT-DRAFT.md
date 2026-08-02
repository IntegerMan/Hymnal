---
id: S05
milestone: M001
status: draft
---

# S05: Validation Remediation and Integrated Evidence — Context (DRAFT)

## Goal

Fix the last-edited-chapter restore-on-relaunch defect, resolve any other milestone-AC-blocking issues surfaced by a core desktop smoke pass, and capture the markdown evidence needed to close M001.

## Why this Slice

All four prior slices (S01–S04) were built and verified in isolation; no integrated evidence spans the full workflow, the restore-on-relaunch defect was found during S04 assessment validation, and milestone closure requires timing, CI, and smoke-pass evidence. S05 is the final gate before M001 is called complete.

## Scope

### In Scope

- Diagnose and fix the restore-on-relaunch defect (suspected: case-sensitive path comparison in WorkspaceViewModel.InitAsync; fix = case-insensitive, Path.GetFullPath-normalised compare)
- Desktop smoke pass covering five core scenarios: workspace open → tree renders → click chapter → type → Ctrl+S → toggle Notes → close → relaunch → chapter restores silently
- Fix any additional defects that cause milestone acceptance criteria to fail during the smoke pass (scope is open to AC-blocking issues discovered in the pass)
- Capture evidence as markdown: `S05-SMOKE-PASS.md` recording pass/fail per scenario with observed results
- Update `S01-ASSESSMENT.md` with real evidence (currently auto-stub); update `S04-ASSESSMENT.md` with verified smoke-pass evidence
- Capture cold-start time (target < 5s) and Book.txt parse time for a 100-chapter fixture (target < 2s)
- Confirm CI matrix passes for win-x64 and linux-x64

### Out of Scope

- New features of any kind (word count, status tracking, settings UI, Git panel — all M002+)
- Evidence for non-AC scenarios (external Book.txt change banner, conflict strip, close-workspace command) — these may be noted as pass/fail but are not required for milestone closure
- Markua inline validation beyond what already exists in the XSHD definition
- Any changes to the notes panel behaviour, editor highlighting rules, or sidebar tree beyond fixing AC-blocking defects

## Constraints

- Restore experience must be silent: no banner, no spinner, no prompt — last chapter appears in the editor as if the app never closed; if restore fails, editor opens empty with no error
- All code fixes must respect the established patterns: Result<T> returns, atomic writes, compile-enforced Core/UI boundary (zero Avalonia refs in Hymnal.Core)
- Evidence format: markdown only — no screenshots required, tabular pass/fail format
- Do not widen the restore fix beyond the minimum needed to make it reliable; over-engineering the path-matching logic adds risk with no user-visible payoff
- The smoke pass must use the real manuscript folder: `C:/Dev/EliAndGraceMakeAGame`

## Integration Points

### Consumes

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs` — InitAsync restore path, path comparison logic
- `src/Hymnal.Core/Infrastructure/AppSettingsStore.cs` — lastChapterPath / lastWorkspacePath persistence
- `src/Hymnal/Views/EditorView.axaml` + `EditorViewModel.cs` — editor load/save/dirty state
- `src/Hymnal/Views/NotesView.axaml` + `NotesViewModel.cs` — notes toggle and persistence
- `.gsd/milestones/M001/slices/S01/S01-ASSESSMENT.md` — to update with real evidence
- `.gsd/milestones/M001/slices/S04/S04-ASSESSMENT.md` — to update with real evidence
- `tests/Hymnal.Core.Tests/` — 22 existing passing tests; new restore test to be added

### Produces

- Restore defect fix in `WorkspaceViewModel.InitAsync` (and possibly AppSettingsStore path normalisation)
- New unit test confirming restore path comparison is case-insensitive
- `S05-SMOKE-PASS.md` — tabular evidence for five core scenarios + timing + CI
- Updated `S01-ASSESSMENT.md` and `S04-ASSESSMENT.md` with real observed results

## Open Questions

- Is the restore defect a path-comparison issue (case-sensitive `==`) or a DynamicData timing issue (nodes not yet in `_nodes` when foreach runs)? — Current thinking: path comparison, because SourceCache.Connect().Bind() processes the initial snapshot synchronously; confirm by adding a diagnostic trace or a targeted unit test before writing the fix
- Does the Avalonia folder picker on Windows return paths with forward slashes that differ from Path.Combine backslash output? — Check during smoke pass by inspecting settings.json after opening a workspace; normalise with Path.GetFullPath if needed
- CI evidence: does the existing GitHub Actions ci.yml workflow have a recent green run? — Check CI status before closing; if not, trigger and wait; if CI is blocked by the WSL/.NET limitation, document that explicitly

# S05: Validation Remediation and Integrated Evidence

**Goal:** Fix the last-edited-chapter restore-on-relaunch defect, resolve any other milestone-AC-blocking issues surfaced by a core desktop smoke pass, and capture the markdown evidence needed to close M001.
**Demo:** After this: last edited chapter reliably restores on relaunch; S01/S04 assessment evidence exists; an integrated desktop smoke pass covers workspace open, chapter edit/save, notes persistence, and theme/highlighting confirmation; startup, parse, and CI evidence are captured for milestone closure

## Must-Haves

- dotnet test tests/Hymnal.Core.Tests --nologo exits 0 with all tests passing (including PathHelperTests)
- restore-on-relaunch is silent and reliable: last chapter appears in editor on relaunch with no banner, no prompt, no spinner
- S05-SMOKE-PASS.md records pass/fail for all five core scenarios with cold-start and parse timing captured
- S01-ASSESSMENT.md and S04-ASSESSMENT.md updated with real observed evidence
- Milestone roadmap and validation artifacts present and accurate in worktree

## Proof Level

- This slice proves: integration — restore defect fix verified by unit tests; integrated smoke pass required for AC evidence; milestone closure artifacts produced as markdown

## Integration Closure

Consumes: WorkspaceViewModel.InitAsync (restore logic), AppSettingsStore (lastChapterPath persistence), EditorViewModel (chapter open/close), NotesViewModel (notes toggle), tests/Hymnal.Core.Tests (31 passing baseline). Produces: PathHelper.IsSamePath in Hymnal.Core.Common (extracted restore-path helper), new PathHelperTests, S05-SMOKE-PASS.md, updated S01-ASSESSMENT.md and S04-ASSESSMENT.md, milestone closure evidence.

## Verification

- No new runtime observability surfaces. Existing INotificationService banners and IAppSettingsStore atomic-write patterns preserved. Evidence artifacts (smoke pass, assessments) are the observable output of this slice.

## Tasks

- [x] **T01: Extracted PathHelper.IsSamePath, fixed WorkspaceViewModel restore to scan authoritative SourceCache and normalize paths, added 9 passing unit tests** `est:60m`
  Three bugs compounded the restore-on-relaunch defect:
  1. Wrong collection scanned: InitAsync iterated _nodes (UI-bound) instead of _model.Nodes.Items (authoritative SourceCache snapshot).
  2. Raw string path comparison: == comparison fails on Windows with mixed casing or separator style. Created PathHelper.cs in Hymnal.Core.Common with IsSamePath(string? a, string? b) using Path.GetFullPath + OrdinalIgnoreCase.
  3. SelectedNode assigned before OpenChapterAsync: fixed to assign only after successful reopen.
  4. Non-canonical path stored on switch: changed to Path.GetFullPath(absolutePath) before persisting.
  Added 9 unit tests in Hymnal.Core.Tests/Common/PathHelperTests.cs.
  - Files: `src/Hymnal.Core/Common/PathHelper.cs`, `src/Hymnal/ViewModels/WorkspaceViewModel.cs`, `tests/Hymnal.Core.Tests/Common/PathHelperTests.cs`
  - Verify: dotnet test tests/Hymnal.Core.Tests --filter PathHelper --nologo
dotnet build src/Hymnal/Hymnal.csproj -c Debug --nologo
dotnet test tests/Hymnal.Core.Tests --nologo

- [x] **T02: Desktop smoke pass, timing evidence, and milestone assessment artifacts** `est:45m`
  Run the five-scenario integrated smoke pass against the real manuscript workspace (C:/Dev/EliAndGraceMakeAGame), capture cold-start time and Book.txt parse time, confirm CI matrix evidence, and produce the milestone closure artifacts:
  1. S05-SMOKE-PASS.md — tabular pass/fail for all five scenarios plus timing
  2. Updated S01-ASSESSMENT.md with real observed results
  3. Updated S04-ASSESSMENT.md with smoke-pass evidence
  4. Confirm M001-ROADMAP.md reflects all slices complete
  - Files: `.gsd/milestones/M001/slices/S05/S05-SMOKE-PASS.md`, `.gsd/milestones/M001/slices/S01/S01-ASSESSMENT.md`, `.gsd/milestones/M001/slices/S04/S04-ASSESSMENT.md`
  - Verify: S05-SMOKE-PASS.md exists with all five scenarios marked pass/fail and timing captured. S01-ASSESSMENT.md and S04-ASSESSMENT.md exist with real evidence (not stubs). dotnet test tests/Hymnal.Core.Tests --nologo exits 0.

## Files Likely Touched

- src/Hymnal.Core/Common/PathHelper.cs
- src/Hymnal/ViewModels/WorkspaceViewModel.cs
- tests/Hymnal.Core.Tests/Common/PathHelperTests.cs
- .gsd/milestones/M001/slices/S05/S05-SMOKE-PASS.md
- .gsd/milestones/M001/slices/S01/S01-ASSESSMENT.md
- .gsd/milestones/M001/slices/S04/S04-ASSESSMENT.md

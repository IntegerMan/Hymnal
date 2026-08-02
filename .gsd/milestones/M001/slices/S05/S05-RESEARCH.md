# S05: Validation Remediation and Integrated Evidence — Research

## Summary
- The highest-confidence remediation target is `src/Hymnal/ViewModels/WorkspaceViewModel.cs`: restore currently scans the UI-bound `_nodes` collection and compares `ResolveAbsolutePath(n) == lastChapterPath` as a raw string before silently opening the file. That is brittle for case/separator/full-path differences and also leaves `SelectedNode` set even if the silent reopen fails.
- The codebase already has the right seams for a minimal fix: `ManuscriptModel` carries `WorkspaceRoot` and `ManuscriptRoot`, `EditorViewModel.OpenChapterAsync()` is the single reopen path, and `IAppSettingsStore` already persists `lastWorkspacePath` / `lastChapterPath` atomically.
- Milestone evidence is materially incomplete in the worktree snapshot: `.gsd/milestones/M001/M001-VALIDATION.md` is missing on disk, `.gsd/milestones/M001/slices/S01/S01-ASSESSMENT.md` is missing, `.gsd/milestones/M001/slices/S04/S04-ASSESSMENT.md` is missing, and the checked-in `.gsd/milestones/M001/M001-ROADMAP.md` is stale (still shows S02/S04 incomplete and has no S05 entry).
- There is no existing UI/headless test project for `WorkspaceViewModel`; `tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj` references only `src/Hymnal.Core/Hymnal.Core.csproj`. If the slice still requires a restore-path test in `Hymnal.Core.Tests`, the comparison/normalization logic should be extracted into a small Core helper rather than testing the Avalonia VM directly.
- CI matrix configuration already exists in `.github/workflows/release.yml` for `win-x64` and `linux-x64`; the missing piece is evidence capture, not workflow authoring.

## Recommendation
1. **Fix restore determinism in `WorkspaceViewModel` with the smallest possible change.**
   - Normalize both stored and candidate paths with `Path.GetFullPath(...)`.
   - Compare using OS-aware semantics (`OrdinalIgnoreCase` on Windows, `Ordinal` elsewhere).
   - Prefer looking up the candidate chapter from the loaded model snapshot (`model.Nodes.Items`) instead of the UI-bound `_nodes` collection so restore does not depend on DynamicData binding timing.
   - Only set `SelectedNode` after `_editor.OpenChapterAsync(...)` succeeds; if reopen fails silently, leave the editor empty and selection null to satisfy the slice constraint.
2. **Extract the path-compare logic into Core if a new test must live under `tests/Hymnal.Core.Tests/`.** A tiny helper in `src/Hymnal.Core/Common/` is lower-risk than adding a new Avalonia-capable test project mid-remediation slice.
3. **Treat evidence generation as a first-class deliverable.** After the code fix, produce/update:
   - `.gsd/milestones/M001/slices/S05/S05-SMOKE-PASS.md`
   - `.gsd/milestones/M001/slices/S01/S01-ASSESSMENT.md`
   - `.gsd/milestones/M001/slices/S04/S04-ASSESSMENT.md`
   - milestone validation/roadmap artifacts if they are still missing from the worktree after execution.
4. **Do not spend time changing CI config.** Just capture the existing workflow/run evidence for the already-defined win/linux matrix.

## Implementation Landscape

### 1) Restore-on-relaunch hotspot
**Files:**
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/ViewModels/EditorViewModel.cs`
- `src/Hymnal.Core/Infrastructure/AppSettingsStore.cs`

**What exists now**
- `MainWindowViewModel` fire-and-forgets `workspaceViewModel.InitAsync()` on construction.
- `WorkspaceViewModel.TrySwitchChapterAsync()` persists `lastChapterPath` after a successful chapter open.
- `WorkspaceViewModel.InitAsync()` reloads `lastWorkspacePath`, binds the model, then tries to restore the prior chapter.
- The restore match is currently raw-string equality against `ResolveAbsolutePath(n) == lastChapterPath`.

**Why this is fragile**
- String equality is sensitive to path casing and path-shape differences even when both point to the same file.
- Restore depends on `_nodes`, which is the UI-bound projection, not the authoritative model snapshot.
- `SelectedNode` is assigned before `OpenChapterAsync()` completes; if file reopen fails, the UI can still look selected even though the editor is empty.

**Minimal change shape**
- Add a tiny path-normalization/match helper.
- Normalize before persisting `lastChapterPath` and before comparing during restore.
- Resolve the candidate from the loaded model snapshot, then call `_editor.OpenChapterAsync(match, absolutePath)`; assign `SelectedNode` only on success.

### 2) Best testing seam
**Files:**
- `tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj`
- `tests/Hymnal.Core.Tests/Infrastructure/AppSettingsStoreTests.cs`
- likely new: `tests/Hymnal.Core.Tests/Common/PathRestoreTests.cs`
- likely new: `src/Hymnal.Core/Common/<path helper>.cs`

**Constraint discovered**
- The existing test project cannot reference `WorkspaceViewModel` because it only references `Hymnal.Core`.

**Practical implication**
- If the slice requires “at least one new restore-path test” without adding a new UI test project, the testable unit must move into Core.

**Recommended test cases**
- Same file matches when one path contains mixed separators.
- Same file matches when stored path differs only by casing on Windows-shaped paths.
- Different chapter paths do not match.
- `Path.GetFullPath` normalization collapses relative segments before compare.

### 3) Evidence/documentation gaps already visible
**Missing or stale in this worktree**
- Missing: `.gsd/milestones/M001/M001-VALIDATION.md`
- Missing: `.gsd/milestones/M001/slices/S01/S01-ASSESSMENT.md`
- Missing: `.gsd/milestones/M001/slices/S04/S04-ASSESSMENT.md`
- Missing: `.gsd/milestones/M001/slices/S05/` contents despite the recovery brief referring to S05 artifacts
- Stale: `.gsd/milestones/M001/M001-ROADMAP.md` still shows S02/S04 unchecked and does not list S05

**Interpretation**
- Previous `gsd_validate_milestone` / `gsd_reassess_roadmap` work is not fully materialized in the current worktree snapshot. Planner/executor should not assume those files already exist locally.
- Because the recovery brief explicitly says not to redo completed work, treat this as an artifact-sync discrepancy to verify at the end of execution rather than the first coding step.

### 4) Smoke-pass surface area
**Files involved in the integrated pass**
- `src/Hymnal/Views/MainWindow.axaml`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/EditorViewModel.cs`
- `src/Hymnal/ViewModels/NotesViewModel.cs`
- `src/Hymnal.Core/Infrastructure/NotesService.cs`
- real manuscript workspace: `C:/Dev/EliAndGraceMakeAGame`

**Scenarios to cover in one pass**
1. Open workspace and confirm sidebar order.
2. Open a real chapter and confirm editor loads/highlighting is visibly active.
3. Edit + `Ctrl+S` and verify on-disk file changed.
4. Toggle notes, edit, and verify `.hymnal-data/notes/` round-trip.
5. Close/relaunch and confirm the last chapter restores silently.

**Important watch-out**
- `NotesViewModel` listens to `EditorViewModel.ActiveNode`, not `WorkspaceViewModel.SelectedNode`. This is good: it means successful restore must end in a real editor open, not just a sidebar selection.

### 5) Operational/CI evidence seams
**Files:**
- `.github/workflows/release.yml`
- `src/Hymnal/Program.cs`
- `src/Hymnal.Core/Services/ManuscriptService.cs`

**Current state**
- Release workflow already defines `windows-latest`/`win-x64` and `ubuntu-latest`/`linux-x64` matrix entries.
- There is no existing benchmark or startup telemetry harness in the repo.

**Low-risk evidence approach**
- For the 100-chapter parse target, add a one-off evidence harness or focused timing test around `ManuscriptService`/`BookTxtParser` rather than inventing a reusable benchmark subsystem.
- For cold start, capture manual timed evidence during the desktop smoke pass instead of adding instrumentation unless execution proves manual timing is too noisy.

## Risks / Watch-outs
- **Do not over-engineer path matching.** A tiny helper + normalized compare is enough.
- **Do not set `SelectedNode` before successful restore open.** Silent restore failure should leave the UI empty, not half-restored.
- **Avoid coupling the fix to UI binding order.** Lookup should come from the authoritative model snapshot.
- **Be explicit about artifact discrepancies.** The worktree does not currently reflect the recovery brief’s validation/roadmap claims.
- **WSL sandbox gotcha remains active** (see memory MEM008): use terminal-session evidence for `dotnet` verification rather than assuming `gsd_exec` can run .NET successfully.

## Skill Discovery
- No installed skill in the current environment is directly about Avalonia/ReactiveUI.
- External Avalonia-focused candidates found via `npx skills find "Avalonia"`:
  - `npx skills add sickn33/antigravity-awesome-skills@avalonia-layout-zafiro` (505 installs)
  - `npx skills add sickn33/antigravity-awesome-skills@avalonia-viewmodels-zafiro` (466 installs)
  - `npx skills add sickn33/antigravity-awesome-skills@avalonia-zafiro-development` (459 installs)
- `npx skills find "ReactiveUI"` returned no results.
- These are optional; the current slice looks implementable without installing extra skills.

## Verification Plan
- Add/adjust a Core-level unit test for restore path normalization/matching.
- Run the smallest relevant automated verification possible for the touched code.
- Produce fresh markdown evidence for the integrated smoke pass and the missing S01/S04 assessments.
- Before milestone closure, re-verify that validation/roadmap artifacts now exist in the worktree and match the intended post-remediation state.

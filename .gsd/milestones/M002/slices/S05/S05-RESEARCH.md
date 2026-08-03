# S05 Research — Desktop UAT and Operational Verification

## Summary
- `gsd_milestone_status` shows **S05 is still pending with 0 tasks planned**; this research should drive the first task plan.
- The S04 wiring changes on disk look coherent: `ChapterInfoViewModel` now mirrors live `ChapterViewModel` target/proximity state directly, `MainWindowViewModel` aggregates right-rail visibility, `MainWindow.axaml.cs` owns the row/column collapse behavior, and `SidebarView.axaml` renders both the status affordance and proximity bar.
- This slice is mostly **human-assisted desktop verification plus file inspection**. In this harness there is no Windows UI automation tool comparable to the browser/mac toolchain, so the agent can prepare steps, inspect persisted JSON, and verify follow-on fixes, but the actual desktop observations (dot color, gutter marker, perceived latency, cold start) need an operator-run pass.
- Primary requirement support: **R004** acceptance proof for authoritative live target/proximity wiring. Secondary support: status persistence / rename continuity / validation-gutter acceptance from M002.

## Recommendation
Plan S05 as **two tasks max**:
1. **T01 — Run the manual desktop verification flow and collect evidence** in one pass against the real manuscript plus an isolated copied workspace for the rename scenario.
2. **T02 — Defect fix only if T01 fails**, scoped tightly to the observed failing path. If the fix crosses more than ~2 files or changes architecture, stop and spawn a follow-up slice instead of expanding S05.

The first proof should be the **rename continuity scenario** because it is the least unit-test-covered integrated behavior: it depends on registry hydration, orphan reconciliation, rename matching, persisted app settings, and restart restore all cooperating.

## Active Requirements / Acceptance Focus
- **R004** — prove `ChapterInfoViewModel` is driven by authoritative live `ChapterViewModel` state, not any stub/fallback path. Observable proof: set a 4,000-word target on a ~2,000-word chapter and confirm both **Chapter Info** and **sidebar** show the same ~50% proximity state.
- **R003 support** — prove status changes update immediately, persist to `phases.json`, and survive restart.
- **R002 support** — prove `ValidationMargin` shows an advisory marker for a known pattern and disappears when undone.

## Implementation Landscape

### Files to observe, not proactively change
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
  - Persists `lastWorkspacePath` / `lastChapterPath` on open and selection.
  - On startup `InitAsync()` restores the last workspace and then the last chapter.
  - On editor save, recalculates the active chapter count, updates totals, and appends history.
  - Rename continuity lives in `LoadRegistryAndPhaseDataAsync()` via orphan reconciliation + title-based rename matching before `AssignUuid`.
- `src/Hymnal/ViewModels/ChapterViewModel.cs`
  - Authoritative live state for `Status`, `WordCount`, `Target`, `HasTarget`, and `ProximityFill`.
  - Proximity is `min(wordCount / (MaxWords ?? MinWords), 1.0)`.
- `src/Hymnal/ViewModels/ChapterInfoViewModel.cs`
  - Mirrors the active `ChapterViewModel` on chapter switch.
  - `F3` toggle is gated on `ActiveNode != null`.
  - Persists `chapterInfoVisible`; status changes optionally prefill today via `prefillPhaseDate`.
  - `SetTargetCommand` in this pane sets a **min-only** target from `PendingTarget`.
- `src/Hymnal/Views/ChapterInfoView.axaml` and `src/Hymnal/Views/ChapterInfoView.axaml.cs`
  - Status ComboBox forwards user changes to `SetStatusCommand` from code-behind.
  - Pane shows live word count, target display, and proximity bar.
- `src/Hymnal/Views/SidebarView.axaml`
  - Sidebar shows chapter status flyout, word counts, and the proximity `ProgressBar` when `HasTarget` is true.
- `src/Hymnal/ViewModels/EditorViewModel.cs`
  - Live word count is debounced with `.Throttle(TimeSpan.FromMilliseconds(300))`.
  - Sidebar totals and `wordcount-history.json` update only after successful save (`Saved`).
- `src/Hymnal/Views/Editor/ValidationMargin.cs` and `src/Hymnal/Views/EditorView.axaml.cs`
  - Advisory marker is refreshed on every text change; no save is needed.
  - Supported triggers are: blank line before `{sample: true}` block, or any attribute block with an unrecognised key.
- `src/Hymnal.Core/Infrastructure/AppSettingsStore.cs`
  - Settings live outside the repo in AppData; S05 should rely on normal app restart behavior rather than repo-local config edits.
- Persisted evidence files in the opened workspace:
  - `.hymnal-data/chapter-registry.json`
  - `.hymnal-data/phases.json`
  - `.hymnal-data/targets.json`
  - `.hymnal-data/wordcount-history.json`

## Natural Seams for Task Planning

### Seam A — Restart and status persistence
Run on the **real manuscript** because it proves the actual operator workflow and app-settings restore path:
- open a real chapter first (F3 does nothing when no chapter is active)
- change status to `Drafting`
- verify sidebar dot changes immediately
- confirm phase start date auto-fills today if `PrefillPhaseDate` is enabled
- close and relaunch Hymnal
- confirm the same workspace/chapter reopens and status/date persist

### Seam B — Rename continuity on an isolated copy
Use a **copied workspace** only:
- choose a chapter with a **unique title**
- rename only the file path and matching `Book.txt` entry; do **not** change the chapter heading/title text
- reopen/relaunch against the copied workspace
- inspect `chapter-registry.json` / `phases.json` / `wordcount-history.json` to confirm the UUID-backed data survived

### Seam C — Live count, save-side effects, and target/proximity
Run on a normal chapter:
- observe `ChapterInfoViewModel.WordCountDisplay` moving after ~300 ms of typing
- save with Ctrl+S
- verify sidebar totals update and today's history entry appears
- set target `4000` in Chapter Info (min-only target is sufficient)
- confirm both pane and sidebar show ~0.5 fill on a ~2,000-word chapter

### Seam D — Validation gutter and no-regression usability pass
- paste a minimal advisory snippet, verify amber dot, undo without saving
- quick F3/F4 interaction pass: Chapter Info and Notes should coexist; right-rail collapse/expand should still behave

## First Proof / Highest-Risk Check
**Run the rename continuity scenario first.**

Why it is riskiest:
- it depends on `WorkspaceViewModel.LoadRegistryAndPhaseDataAsync()` marking orphans first, then matching exactly one orphan by **stored title** before `ReconcileRename()`
- it depends on `lastWorkspacePath` / `lastChapterPath` restore on relaunch
- it is the easiest place for a "looks fine in-session, breaks after restart" defect to hide

Critical watch-out:
- the rename heuristic is **title-sensitive**. For reliable proof, choose a chapter whose title is unique among orphanable chapters and keep the H1/title unchanged while renaming the file path.

## Verification Notes for the Planner
- **Manual UI evidence is required.** The agent can inspect files after the run, but a human/operator must observe the desktop UI states.
- **Live count vs persisted totals are different checkpoints:**
  - `EditorViewModel.LiveWordCount` should move after the 300 ms debounce while typing.
  - Sidebar totals + `wordcount-history.json` update only after save, because `WorkspaceViewModel` listens to `EditorViewModel.Saved`.
- **Best validation-margin probe:** use an unknown-key attribute block like `{foobarbaz: true}` on a line. It is simpler and less contextual than the blank-line-before-`{sample: true}` case, and it should draw the amber advisory marker immediately on text change.
- **Cold-start timing:** startup restore waits for registry/phase/target hydration before reopening the last chapter, but background per-chapter word counts continue afterward. Record at least:
  - time until workspace + last chapter are visible/interactive
  - whether totals visibly continue settling afterward
- **If a defect is found:** re-run only the failed scenario after the fix, then optionally run `dotnet build src/Hymnal/Hymnal.csproj -nologo` and `dotnet test Hymnal.sln -nologo` from the normal terminal execution path. Prior project memory says not to use `gsd_exec` for dotnet verification in this repo.

## Risks / Watch-outs
- `F3` is gated by active chapter selection, so trying to verify Chapter Info while Book.txt or nothing is open will look like a failure even though it is expected behavior.
- `chapterInfoVisible`, `sidebarExpanded`, and `prefillPhaseDate` persist across sessions. Existing user state can change the starting posture of the app; record it instead of assuming defaults.
- Rename continuity is not a pure path rename; duplicate titles among orphaned chapters can defeat the heuristic.
- `ChapterInfoViewModel` target entry is single-value/min-only, while the sidebar flyout supports min/max. For milestone acceptance, the pane is enough; do not overcomplicate T01 by using ranges.
- Validation markers are advisory-only and intentionally swallow exceptions; a missing marker is the signal to investigate, not a crash/log expectation.

## Concrete Planning Output Suggested
Create these tasks:
- **T01: Run desktop UAT and capture evidence**
  - manual step list for real manuscript + copied-workspace rename pass
  - file-inspection checklist for `.hymnal-data/*`
  - latency/cold-start notes section
- **T02: Fix any defect found in T01 and re-run the failing scenario**
  - only created/executed if T01 surfaces a real defect

## Sources Consulted
- `.gsd/milestones/M002/slices/S05/S05-CONTEXT.md`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/ChapterViewModel.cs`
- `src/Hymnal/ViewModels/ChapterInfoViewModel.cs`
- `src/Hymnal/ViewModels/EditorViewModel.cs`
- `src/Hymnal/Views/ChapterInfoView.axaml`
- `src/Hymnal/Views/ChapterInfoView.axaml.cs`
- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/Views/Editor/ValidationMargin.cs`
- `src/Hymnal/Views/EditorView.axaml.cs`
- `src/Hymnal/Views/MainWindow.axaml`
- `src/Hymnal/Views/MainWindow.axaml.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal.Core/Infrastructure/AppSettingsStore.cs`
- `src/Hymnal.Core/Services/WordCountHistoryService.cs`
- `tests/Hymnal.Core.Tests/Services/ChapterRegistryServiceTests.cs`
- `tests/Hymnal.Core.Tests/Services/PhaseDataServiceTests.cs`
- `tests/Hymnal.Core.Tests/Services/TargetsServiceTests.cs`

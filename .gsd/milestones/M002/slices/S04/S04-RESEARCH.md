# S04 Research — Runtime Stabilization and Chapter Info Wiring

## Slice Status / Resume Check

- `S04-CONTEXT.md` and `S04-CONTEXT-DRAFT.md` both exist and match the known scope: fix the Chapter Info startup crash, wire live proximity state, and do a small defensive VM sweep.
- `S04-PLAN.md` does **not** exist yet.
- `gsd_milestone_status` shows `S04` is still **pending** with **0 tasks planned**, so the planner should start from decomposition rather than execution.

## Requirement Focus

This slice directly supports:
- **R003** — Chapter Info pane status/date editing must remain usable at runtime.
- **R004** — Chapter Info pane must show live target/proximity state instead of the S03 stub.

The runtime crash is currently the gate for both requirements because S05 UAT cannot proceed until the app launches and F3 works reliably.

## Summary

This is **targeted research**, not deep architecture work. The implementation surface is narrow and already localized.

The main defect is in `src/Hymnal/ViewModels/ChapterInfoViewModel.cs`:
- `_proximityFill` is declared as an `ObservableAsPropertyHelper<double>` and built from `WhenAnyValue(x => x.HasTarget, x => x.WordCount, x => x.TargetDisplay, ...)` around lines **190-192**.
- `_hasTarget` is another OAPH built later from `TargetDisplay` around lines **196-197**.
- Because `WhenAnyValue` can evaluate immediately, `ProximityFill` depends on `HasTarget` before `_hasTarget` is initialized, matching the reported startup null/OAPH-ordering hazard.

The second defect is also in the same file:
- `TargetDisplay` is updated from the active chapter subscription at lines **300-301**.
- The file still contains comments near lines **451-453** explicitly admitting that the local fill calculation is only approximate and that `ChapterViewModel.ProximityFill` is the authoritative source.
- S03 task summaries confirm `ComputeProximityFill` was intentionally stubbed at `0.0` and deferred to S04.

The good news: the authoritative data already exists in `src/Hymnal/ViewModels/ChapterViewModel.cs`.
- `ChapterViewModel` exposes OAPH-backed `ProximityFill` at lines **199-208**.
- It also exposes OAPH-backed `HasTarget` at lines **211-215**.
- `SidebarView.axaml` already binds directly to those properties (`HasTarget` around line **202**, `ProximityFill` around line **205**), so the sidebar is the working reference implementation for expected behavior.

The right-rail hosting work from S03 looks stable and low-risk:
- `MainWindowViewModel.cs` only aggregates pane visibility via `IsAnyRightPaneOpen` and `IsBothRightPanesOpen` (lines **104-114** from the scan).
- `MainWindow.axaml.cs` drives right-pane row heights from `WhenAnyValue(x => x.IsVisible)` subscriptions, which matches MEM035 and the S03 summary.
- I did not find another obvious runtime blocker there; the sweep should stay focused rather than expand into layout refactoring.

## Recommendation

Use the smallest-risk fix:

1. **Remove `ChapterInfoViewModel`'s local OAPH chain for `ProximityFill`.**
   - Replace it with a simple backing field/property updated from the active chapter subscription.
   - Subscribe to `chapterVm.WhenAnyValue(x => x.ProximityFill)` inside `OnActiveNodeChanged`, alongside the existing `WordCount`, `Status`, `PhaseData`, and `Target` subscriptions.

2. **Strongly consider removing the local OAPH for `HasTarget` too.**
   - Current implementation derives `HasTarget` from the formatted `TargetDisplay` string, which is brittle and creates the ordering problem.
   - Prefer subscribing directly to `chapterVm.WhenAnyValue(x => x.HasTarget)` and storing a backing bool.
   - This aligns Chapter Info with the existing per-chapter subscription model already used for `WordCount`, `Status`, and `TargetDisplay`.

3. **Keep the public binding surface unchanged.**
   - `ChapterInfoView.axaml` already binds to `ProximityFill`, `HasTarget`, and `TargetDisplay`.
   - The fix should be internal to `ChapterInfoViewModel`; avoid changing the view contract or right-rail structure.

4. **Do only a narrow defensive sweep in `MainWindowViewModel`.**
   - Confirm no similar OAPH self-dependency/null-init patterns exist.
   - Current evidence suggests no broader rewrite is needed.

## Implementation Landscape

### 1) `src/Hymnal/ViewModels/ChapterInfoViewModel.cs` — primary change site

Known current structure from the scan and prior slice summaries:
- `WordCountDisplay` is OAPH-backed from `WordCount` (**184-186**).
- `ProximityFill` is OAPH-backed from `HasTarget`, `WordCount`, and `TargetDisplay` (**190-192**).
- `HasTarget` is OAPH-backed from `TargetDisplay` string presence (**196-197**).
- `OnActiveNodeChanged` already subscribes to active chapter properties:
  - `WordCount` (**284**)
  - `Status` (**288**)
  - `PhaseData` (**292**)
  - `Target` / `TargetDisplay` (**300-301**)
- Initial sync occurs around line **317** for `TargetDisplay`.

This gives a natural edit seam:
- leave `WordCountDisplay` OAPH alone;
- convert `HasTarget` and `ProximityFill` to direct chapter-driven state;
- extend reset logic when no active chapter is selected so both properties clear safely.

### 2) `src/Hymnal/ViewModels/ChapterViewModel.cs` — authoritative source, probably no behavioral change needed

Existing properties already do the right work:
- `ProximityFill` OAPH: lines **199-208**
- `HasTarget` OAPH: lines **211-215**

This file is likely reference-only unless compilation reveals an edge case. Planner should treat it as validation context, not the primary modification target.

### 3) `src/Hymnal/Views/ChapterInfoView.axaml` — binding contract to preserve

The view currently expects:
- `ProgressBar Value="{Binding ProximityFill}"` around line **132**
- `IsVisible="{Binding HasTarget}"` around line **135**
- `TargetDisplay` text bound around lines **149-150**

Because the contract is already correct, S04 should avoid view edits unless the VM reset behavior exposes a display glitch.

### 4) `src/Hymnal/ViewModels/MainWindowViewModel.cs` and `src/Hymnal/Views/MainWindow.axaml.cs` — defensive sweep only

MainWindow findings:
- visibility aggregation only in VM;
- row-height subscriptions only in code-behind;
- no sign from the scan of another `HasTarget`/`ProximityFill` style ordering issue.

This supports a planner choice to make MainWindow a short review task, not a full implementation task.

## Risks / Constraints

- **Do not derive truth from presentation strings.** `HasTarget` currently comes from `TargetDisplay`, which is both fragile and unnecessary.
- **Do not change public API names.** `ChapterInfoView.axaml` and `MainWindow.axaml` already bind to the current property names.
- **Keep disposal aligned with local conventions.** S03 established `CompositeDisposable.Add(...)` rather than `DisposeWith(...)` for this codebase.
- **Respect MEM035.** Right-pane row heights belong in `MainWindow.axaml.cs`; do not try to “clean up” into AXAML binding during S04.
- **Avoid widening scope into Core services.** This is a UI/viewmodel stabilization slice; nothing in the evidence points to `TargetsService`, `PhaseDataService`, or `MetadataStore` as the blocker.

## Natural Task Seams

### Task 1 — Fix `ChapterInfoViewModel` runtime state wiring
- Remove/replace the local `HasTarget` and `ProximityFill` OAPH implementation.
- Subscribe directly to `chapterVm.HasTarget` and `chapterVm.ProximityFill` in `OnActiveNodeChanged`.
- Ensure reset/clear paths set both properties to safe defaults when there is no active chapter.

### Task 2 — Defensive sweep + compile cleanup
- Check `MainWindowViewModel` and any touched code for similar initialization-order hazards or stale comments.
- Keep this task narrow: clean comments, null-safety, and any compilation fallout from Task 1.

### Task 3 — Verification
- Run build and full solution tests.
- If runtime smoke verification is needed before S05, minimally launch the app and confirm startup/F3 no longer crash, but the hard acceptance UAT still belongs to S05.

## First Proof

The first proof should be **ChapterInfoViewModel constructor + active-chapter subscription behavior**, because that is the highest-risk unblocker and likely fixes both symptoms at once.

If Task 1 is correct, the following should become true immediately:
- app startup no longer depends on `_hasTarget` being initialized before `_proximityFill`;
- Chapter Info progress bar visibility follows real target presence;
- Chapter Info progress bar value mirrors the same live chapter state already used by the sidebar.

## Verification Plan

Minimum required:
- `dotnet build src/Hymnal/Hymnal.csproj -nologo`
- `dotnet test Hymnal.sln -nologo`

Recommended runtime smoke after code change, before handing to S05:
- `dotnet run --project src/Hymnal/Hymnal.csproj`
- Open a workspace and press **F3**.
- Confirm no startup crash and no Chapter Info crash when a chapter with a target is active.
- Confirm Chapter Info progress bar is no longer permanently zero when the sidebar shows non-zero proximity for the same chapter.

## Skill Discovery

No directly installed skill in the current environment targets **Avalonia** or **ReactiveUI** specifically.

External `npx skills find` results worth noting for future work (not required for S04):
- `npx skills add sickn33/antigravity-awesome-skills@avalonia-viewmodels-zafiro`
- `npx skills add sickn33/antigravity-awesome-skills@avalonia-zafiro-development`
- `npx skills add markpitt/claude-skills@avalonia`
- `npx skills add abdssamie/quater@avalonia-mvvm`

`npx skills find "ReactiveUI"` returned no dedicated skill.

## Key Prior Art / Decisions

- **MEM035:** right-rail row heights must be managed from `MainWindow.axaml.cs`, not AXAML RowDefinition bindings.
- **S03 / T01:** `ChapterInfoViewModel.ProximityFill` was knowingly stubbed, and `ChapterViewModel.ProximityFill` was always intended as the authoritative value.
- **S03 / T01:** use `CompositeDisposable.Add(...)` rather than `DisposeWith(...)` in this area.
- **S03 / T02:** keep the `ChapterInfoView` binding contract intact; status selection is already manually forwarded in code-behind because `Status` has a private setter.

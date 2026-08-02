# S04: Runtime Stabilization and Override Alignment

**Goal:** Align S04 with the user-provided `ChapterInfoViewModel` implementation, treat that file as the authoritative fix for Chapter Info startup safety and live target/proximity wiring, and preserve clean build/test verification as the handoff into S05 desktop UAT.
**Demo:** Launch the app successfully, open a workspace, use F3 without crashing, and see Chapter Info target/proximity state driven by live `ChapterViewModel` subscriptions in the user-provided `ChapterInfoViewModel` implementation while the full solution still builds and tests cleanly.

## Must-Haves

- The user-provided `src/Hymnal/ViewModels/ChapterInfoViewModel.cs` implementation is the authoritative S04 baseline
- `dotnet build src/Hymnal/Hymnal.csproj -nologo` exits 0 with 0 errors
- `dotnet test Hymnal.sln -nologo` passes all 57+ tests with 0 failures
- `ChapterInfoViewModel._proximityFill` and `._hasTarget` are plain backing fields, not OAPHs
- `ProximityFill` and `HasTarget` are fed from live `ChapterViewModel` subscriptions, not stubbed or display-string-derived state
- App launch does not produce a `NullReferenceException` in the `ChapterInfoViewModel` constructor
- T02 defensive sweep confirms adjacent `MainWindowViewModel` and `ChapterViewModel` OAPH chains are safe and need no code changes

## Proof Level

- This slice proves: integration — compile-verified and unit-test-verified; full runtime UAT deferred to S05

## Integration Closure

The override locks in `src/Hymnal/ViewModels/ChapterInfoViewModel.cs` as the canonical implementation for S04. `ChapterInfoView.axaml` bindings remain unchanged and continue to consume `Status`, `PhaseStartDate`, `PhaseEndDate`, `WordCountDisplay`, `TargetDisplay`, `HasTarget`, `ProximityFill`, `PendingTarget`, and `PrefillPhaseDate` from the view model. `MainWindowViewModel` right-pane visibility OAPHs and `ChapterViewModel` target/proximity OAPHs were reviewed as non-self-referential and require no follow-on code changes in this slice.

## Verification

- Fresh `dotnet build` and `dotnet test` evidence remains the required verification signal for S04
- Desktop/UAT confirmation of the F3 workflow, restart persistence, rename survival, and latency evidence remains explicitly deferred to S05

## Tasks

- [x] **T01: Replace OAPH-based ProximityFill and HasTarget with live per-chapter subscriptions** `est:30m`
  **Why**: ChapterInfoViewModel crashes on startup because the `_proximityFill` OAPH calls `WhenAnyValue(x => x.HasTarget, ...)` — which evaluates immediately — before `_hasTarget` is assigned, producing a NullReferenceException. Additionally, `ProximityFill` is permanently stubbed at 0.0 via `ComputeProximityFill`. Both defects are fixed by dropping the OAPHs in favour of plain backing fields driven from the existing per-chapter subscription pattern.
  - Files: `src/Hymnal/ViewModels/ChapterInfoViewModel.cs`
  - Verify: dotnet build src/Hymnal/Hymnal.csproj -nologo

- [x] **T02: Defensive sweep and full solution build + test verification** `est:20m`
  **Why**: Confirm that the T01 changes produce a clean build and do not regress any of the 57 existing unit tests. Also perform a brief defensive sweep of MainWindowViewModel to confirm no similar OAPH self-dependency or null-init ordering hazards exist there.
  - Files: `src/Hymnal/ViewModels/MainWindowViewModel.cs`
  - Verify: dotnet build src/Hymnal/Hymnal.csproj -nologo && dotnet test Hymnal.sln -nologo

## Files Likely Touched

- src/Hymnal/ViewModels/ChapterInfoViewModel.cs
- src/Hymnal/ViewModels/MainWindowViewModel.cs
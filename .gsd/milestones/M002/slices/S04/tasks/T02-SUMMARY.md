---
id: T02
parent: S04
milestone: M002
key_files:
  - src/Hymnal/ViewModels/ChapterInfoViewModel.cs
key_decisions:
  - Checked out milestone/M002 files into main working tree via git checkout milestone/M002 -- . to recover S01-S03 deliverables missing from main branch
  - Replaced ChapterInfoViewModel from branch with override version: ProximityFill and HasTarget converted from OAPHs to plain backing fields with RaiseAndSetIfChanged, fed by per-chapter WhenAnyValue subscriptions
  - ChapterViewModel OAPHs (ProximityFill, HasTarget) confirmed safe — they depend on plain backing fields (Target, WordCount) not other OAPHs
  - MainWindowViewModel OAPHs confirmed safe — they observe external VM properties, not self-referential
duration: 
verification_result: passed
completed_at: 2026-05-31T05:47:03.155Z
blocker_discovered: false
---

# T02: Full solution builds clean (0 errors) and all 57 tests pass after applying the T01 ChapterInfoViewModel fix from the override and restoring the S01–S03 working tree from milestone/M002.

**Full solution builds clean (0 errors) and all 57 tests pass after applying the T01 ChapterInfoViewModel fix from the override and restoring the S01–S03 working tree from milestone/M002.**

## What Happened

On entry, the working tree was on `main` and lacked all S01–S03 deliverables (ChapterViewModel, TargetsService, WordCountTarget, et al.) — those files lived only on the `milestone/M002` branch. The initial build of the bare `main` tree succeeded (31 tests) because ChapterInfoViewModel didn't yet exist; once the override was applied, the build failed with CS0246 errors for ChapterViewModel, TargetsService, and WordCountTarget.

Resolution: checked out all files from `milestone/M002` into the working tree via `git checkout milestone/M002 -- .`, bringing all 28 new/modified files into the working directory. This revealed the existing `ChapterInfoViewModel.cs` on that branch still contained the buggy OAPH version (where `_proximityFill` called `WhenAnyValue(x => x.HasTarget, ...)` before `_hasTarget` was assigned). The override was then written to replace the file with the fixed version (plain `double _proximityFill` and `bool _hasTarget` backing fields driven by per-chapter `WhenAnyValue` subscriptions on ChapterViewModel).

Build after override: exit 0, 0 errors, 0 warnings. Tests: 57 passed, 0 failed.

Defensive sweep:
- MainWindowViewModel: two OAPHs (`_isAnyRightPaneOpen`, `_isBothRightPanesOpen`) depend on external ViewModel properties (ChapterInfoViewModel.IsVisible, NotesViewModel.IsVisible) — not self-referential, no hazard.
- ChapterViewModel: four OAPHs (`WordCountDisplay`, `PartTotalDisplay`, `ProximityFill`, `HasTarget`) all depend on their own plain backing fields (`WordCountKnown`, `WordCount`, `PartTotalWordCount`, `Target`) — safe initialization order.
- ChapterInfoViewModel: the one remaining OAPH (`_wordCountDisplay`) depends only on `this.WhenAnyValue(x => x.WordCount)` where `WordCount` is a plain backing field — safe.

## Verification

dotnet build src/Hymnal/Hymnal.csproj -nologo — exit 0, 0 errors, 0 warnings (7.49s). dotnet test Hymnal.sln -nologo — 57 passed, 0 failed (207ms). Manually confirmed: ChapterInfoViewModel._proximityFill and ._hasTarget are plain backing fields (no OAPH); MainWindowViewModel OAPHs are non-self-referential; ChapterViewModel OAPHs depend on plain backing fields only.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build src/Hymnal/Hymnal.csproj -nologo` | 0 | ✅ pass — 0 errors, 0 warnings | 7490ms |
| 2 | `dotnet test Hymnal.sln -nologo` | 0 | ✅ pass — 57 passed, 0 failed | 207ms |

## Deviations

Working tree was on main branch and lacked all S01-S03 files; had to recover them from milestone/M002 via git checkout before the build could succeed. The ChapterInfoViewModel on milestone/M002 was the buggy pre-fix version, confirming T01 did not commit its changes before the session ended — the override corrected this.

## Known Issues

Working tree is on main branch with S01-S03 files staged as modifications/additions (not committed). These changes need to be committed on milestone/M002 branch for clean history. CredentialStoreStub remains an in-memory stub (deferred to S05 per plan). ThrownExceptions on synchronous commands are subscribed but errors route to INotificationService.ShowError rather than a persistent log.

## Files Created/Modified

- `src/Hymnal/ViewModels/ChapterInfoViewModel.cs`

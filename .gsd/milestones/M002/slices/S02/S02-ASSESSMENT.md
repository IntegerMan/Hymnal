---
sliceId: S02
uatType: browser-executable
verdict: PARTIAL
date: 2026-05-30T23:13:10-04:00
---

# UAT Result — S02

## Checks

| Check | Mode | Result | Notes |
|-------|------|--------|-------|
| Open a chapter and type 50 words; wait 300 ms; save the chapter with Ctrl+S | runtime | NEEDS-HUMAN | I could not drive the live desktop UI in this browser-only environment. Source inspection shows `EditorViewModel` debounces `LiveWordCount` with a 300 ms throttle and `SaveAsync` publishes `Saved` after atomic persistence, but the end-to-end UI interaction itself was not executable here. |
| Observe the chapter row, Part row, and CHAPTERS header in the sidebar after the debounce/save flow | human-follow-up | NEEDS-HUMAN | No browser-accessible Hymnal runtime was available to visually confirm sidebar refresh behavior. The underlying word-count logic was exercised via tests, but the actual rendered row/header update still needs a human UI pass. |
| Right-click a chapter row, choose `Set Target…`, enter `minWords = 1800` and `maxWords = 4000`, then confirm | human-follow-up | NEEDS-HUMAN | The target popup flow is implemented in `ChapterViewModel` (`OpenTargetFlyoutCommand`, `ConfirmTargetCommand`), and target persistence round-trips are covered by tests, but the live popup interaction could not be executed here. |
| Reopen the popup and clear the target | human-follow-up | NEEDS-HUMAN | `TargetsService` and `MetadataStore` tests confirm save/load/removal and atomic file writes, but the live clear-target UI action was not observable in this environment. |
| Word-count calculation excludes Markua directive lines and treats empty/whitespace-only content as 0 | runtime | PASS | `WordCountServiceTests` passed, including directive exclusion, whitespace-only zero-count, and mixed-content cases. |
| Target persistence round-trips min/max values, overwrites existing targets, removes null targets, and atomic metadata writes create/overwrite files | runtime | PASS | `TargetsServiceTests` and `MetadataStoreTests` passed: 19/19 targeted tests succeeded with `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --no-restore --filter FullyQualifiedName~WordCountServiceTests|FullyQualifiedName~TargetsServiceTests|FullyQualifiedName~MetadataStoreTests -v normal`. |

## Overall Verdict

PARTIAL — the underlying word-count, target, and atomic-persistence contracts are verified, but the live browser/UI flow for this desktop app could not be exercised in this environment.

## Notes

- Verified via `dotnet test` on `tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj` with `--no-restore`; all targeted tests passed (19/19).
- The solution restore path fails under the installed preview SDK if restore is attempted fresh (`Value cannot be null. (Parameter 'path1')` from NuGet restore), so `--no-restore` was required.
- Relevant implementation points inspected: `EditorViewModel.LiveWordCount` uses a 300 ms throttle and `ChapterViewModel` exposes target/proximity state plus popup commands.
- No browser-accessible Hymnal runtime was available, so the visual sidebar and popup checks remain pending human UI verification.

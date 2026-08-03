---
sliceId: S03
uatType: browser-executable
verdict: FAIL
date: 2026-05-31T03:45:00Z
---

# UAT Result — S03

## Checks

| Check | Mode | Result | Notes |
|-------|------|--------|-------|
| Press `F3` to open the Chapter Info pane. | runtime | FAIL | The app could not be launched to a usable UI state. `dotnet run --project src/Hymnal/Hymnal.csproj` crashed during startup with `NullReferenceException` in `ChapterInfoViewModel.HasTarget` (`src/Hymnal/ViewModels/ChapterInfoViewModel.cs:123`), surfaced through `ChapterInfoViewModel..ctor` (`line 189`) and `App.OnFrameworkInitializationCompleted()` (`src/Hymnal/App.axaml.cs:108`). |
| Confirm the pane shows the current chapter title, status, phase start/end date fields, live word count, and target controls. | runtime | FAIL | Startup crash prevented opening the UI, so the Chapter Info pane could not be inspected. |
| Change the status to `Editing` and confirm the phase start date auto-fills to today when prefill is enabled. | runtime | FAIL | Startup crash prevented interacting with status/date controls. |
| Toggle the right rail so both Chapter Info and Notes are visible and confirm the row splitter is present and resizes the panes. | runtime | FAIL | Startup crash prevented reaching the right-rail layout. |
| Open a Markua file that contains a blank line immediately before a `{sample: true}` heading and confirm an advisory dot appears in the editor gutter. | runtime | FAIL | Startup crash prevented loading the editor to verify gutter markers. |
| Add or remove the right-rail panes and confirm notes behavior still works as before. | runtime | FAIL | Startup crash prevented exercising notes visibility behavior. |

## Overall Verdict

FAIL — the application crashes on startup before the UI can be exercised, so none of the interactive UAT checks could be completed.

## Notes

- Browser navigation to `http://localhost:3000` returned `ERR_CONNECTION_REFUSED`, indicating no local browser target was available.
- No console or network activity was captured because the target app never reached a runnable browser UI.
- The failure stack trace points to `ChapterInfoViewModel.HasTarget` being evaluated during view-model construction.

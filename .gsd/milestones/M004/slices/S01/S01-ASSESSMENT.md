---
sliceId: S01
uatType: browser-executable
verdict: PARTIAL
date: 2026-06-04T00:00:00Z
---

# UAT Result — S01

## Checks

| Check | Mode | Result | Notes |
|-------|------|--------|-------|
| Click the Plan tab. | human-follow-up | NEEDS-HUMAN | Browser navigation to `http://localhost:3000` failed with `ERR_CONNECTION_REFUSED`, so the desktop app could not be reached from browser automation. |
| Verify the shell switches to a full-board corkboard layout with left/right chrome collapsed like Manage mode. | human-follow-up | NEEDS-HUMAN | Could not access the live UI; needs direct desktop verification. |
| Confirm the board shows Part dividers in Book.txt order and a card for each chapter. | human-follow-up | NEEDS-HUMAN | Could not access the live UI; needs direct desktop verification. |
| Inspect one card and verify it shows chapter title, status, word count, target/progress area or `No target`, and phase dates when present. | human-follow-up | NEEDS-HUMAN | Could not access the live UI; needs direct desktop verification. |
| Click a card once and confirm it becomes the selected card. | human-follow-up | NEEDS-HUMAN | Could not access the live UI; needs direct desktop verification. |
| Press Enter or double-click the selected card. | human-follow-up | NEEDS-HUMAN | Could not access the live UI; needs direct desktop verification. |
| Verify the editor opens that chapter and the shell returns to Write mode. | human-follow-up | NEEDS-HUMAN | Could not access the live UI; needs direct desktop verification. |
| Use a structural action from the card context menu, such as rename, new chapter, include existing, remove from book, or delete with confirmation. | human-follow-up | NEEDS-HUMAN | Could not access the live UI; needs direct desktop verification. |
| Verify the change is reflected in Book.txt order and the board refreshes afterward. | human-follow-up | NEEDS-HUMAN | Could not access the live UI; needs direct desktop verification. |

## Overall Verdict

PARTIAL — the browser automation target was unreachable, so none of the live desktop behaviors could be verified in this harness.

## Notes

- Attempted to launch the app with `cmd.exe /c dotnet run --project src/Hymnal/Hymnal.csproj`.
- Attempted to open `http://localhost:3000` in the browser, but navigation failed with `ERR_CONNECTION_REFUSED`.
- A screenshot captured the browser error page (“This site can’t be reached”).
- Manual desktop verification is required to complete this UAT in the intended environment.

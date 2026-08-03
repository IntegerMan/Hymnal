---
sliceId: S01
uatType: browser-executable
verdict: FAIL
date: 2026-05-31T01:01:43Z
---

# UAT Result — S01

## Checks

| Check | Mode | Result | Notes |
|-------|------|--------|-------|
| 1. Open the workspace. | runtime | FAIL | App did not stay up long enough to open a workspace. `dotnet run --project src/Hymnal/Hymnal.csproj` crashed on startup with `System.Collections.Generic.KeyNotFoundException: Static resource 'BoolToOpacityConverter' not found.` |
| 2. Confirm every chapter row in the sidebar shows a status dot and every Part row does not. | runtime | FAIL | Could not reach the sidebar because the app crashed during startup before the UI became usable. |
| 3. Click a chapter status dot and choose `Drafting`. | runtime | FAIL | Blocked by startup crash; no interactive UI was available. |
| 4. Observe that the dot changes colour immediately and the chapter state persists after relaunch. | runtime | FAIL | Blocked by startup crash; no status interaction or relaunch verification was possible. |
| 5. Rename a chapter file and update `Book.txt` to match the new path. | runtime | FAIL | Blocked by startup crash; no workspace interaction was possible. |
| 6. Relaunch the app and confirm the chapter keeps the same status/UUID-backed history after rename. | runtime | FAIL | Blocked by startup crash; no relaunchable runtime session reached the workspace UI. |
| 7. Open a missing chapter entry and confirm the dot appears dimmed and non-interactive. | runtime | FAIL | Blocked by startup crash; no sidebar interaction was possible. |
| 8. Change status on two different chapters in quick succession and confirm both persist after reload. | runtime | FAIL | Blocked by startup crash; no UI interaction or persistence verification was possible. |

## Overall Verdict

FAIL — the app currently crashes on startup because `BoolToOpacityConverter` is referenced in `SidebarView.axaml` but not registered as a static resource, so the UAT flow could not be exercised.

## Notes

- Verified by launching the app with `dotnet run --project src/Hymnal/Hymnal.csproj`; it exited with `KeyNotFoundException: Static resource 'BoolToOpacityConverter' not found`.
- `src/Hymnal/Views/SidebarView.axaml` references `BoolToOpacityConverter` in the chapter-dot button opacity binding.
- `src/Hymnal/Views/Converters/StatusToBrushConverter.cs` defines `BoolToOpacityConverter`, but `SidebarView.axaml` only registers `StatusToBrushConverter`, `BoolNotConverter`, `StatusEqualsConverter`, and `StatusDotTooltipConverter` in `UserControl.Resources`.
- `dotnet test Hymnal.sln` passed 41/41 tests, but that does not validate the blocked UAT flow.
- A browser check against `http://localhost:3000` returned `ERR_CONNECTION_REFUSED`, confirming there is no web UI endpoint to use for this desktop UAT.

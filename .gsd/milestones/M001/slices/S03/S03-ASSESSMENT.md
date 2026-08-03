---
sliceId: S03
uatType: mixed
verdict: FAIL
date: 2026-05-29T16:25:10Z
---

# UAT Result — S03

## Checks

| Check | Mode | Result | Notes |
|-------|------|--------|-------|
| 1) Open chapter from sidebar | runtime | PASS | Chapter-one opened; Part and missing nodes reset selection to null before the chapter load completed. |
| 2) Markua highlighting is active | human-follow-up | NEEDS-HUMAN | Source wiring is present (`EditorView` loads `MarkuaHighlighting.xshd` on attach and chapter-two contains heading/bold/italic/code/attribute-list/code-block tokens), but the rendered highlight colors still need a person to inspect in the running UI. |
| 3) Dirty-state indicator appears | runtime | PASS | After editing, `CanSave=True` and the title switched to the modified-state form (`• chapter-one.md — Hymnal` in the runtime harness). |
| 4) Explicit save works | runtime | PASS | Ctrl+S-equivalent save persisted the edit to disk, cleared dirty state, and removed the modified title indicator. |
| 5) Save-on-switch works | runtime | PASS | Editing chapter-one and selecting chapter-two caused chapter-one to be saved first, then chapter-two opened. |
| 6) Save failure blocks switching | runtime | PASS | A forced metadata-store failure surfaced an error and reverted selection back to chapter-one without discarding the unsaved buffer. |
| 7) External change while clean | runtime | PASS | When chapter-one changed externally while clean, the watcher reloaded the buffer and emitted reload info notifications. |
| 8) External change while dirty | runtime | PASS | When chapter-one changed externally while dirty, the editor entered conflict state and exposed the keep/reload prompt message. |
| 9) Restore last edited chapter | runtime | FAIL | Startup restore did not reopen chapter-two in the headless runtime harness; `SelectedNode` stayed null and the active file remained empty. The restore path appears race-prone because `WorkspaceViewModel.InitAsync` scans `_nodes` immediately after `BindModel`. |

## Overall Verdict

FAIL — All automatable checks except the last restore scenario passed; restore-on-relaunch is currently not reliable in the exercised runtime path.

## Notes

- The runtime harness exercised the real ViewModels and file system behavior against a copied manuscript workspace.
- One check remains `NEEDS-HUMAN` because the actual rendered Markua highlighting requires visual inspection in the live app.
- The restore failure is actionable: the chapter lookup in `WorkspaceViewModel.InitAsync` appears to run before the DynamicData-bound node list is populated.

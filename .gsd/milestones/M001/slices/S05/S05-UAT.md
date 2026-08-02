# S05: Validation Remediation and Integrated Evidence — UAT

**Milestone:** M001
**Written:** 2026-05-30T03:02:06.969Z

## UAT — S05: Validation Remediation and Integrated Evidence

### Scenario 1: Restore on Relaunch (Primary Fix)

**Given** the user has opened a chapter and closed the app  
**When** the app relaunches  
**Then** the last chapter opens silently in the editor — no banner, no prompt, no spinner — the app appears as if it never closed

**Evidence:** PathHelper.IsSamePath unit tests (9/9 pass); WorkspaceViewModel.InitAsync scans _model.Nodes.Items with GetFullPath-normalized OrdinalIgnoreCase comparison

---

### Scenario 2: Path Matching Reliability

**Given** the OS folder picker returns a path with mixed casing or mixed separators  
**When** the stored lastChapterPath is compared to the candidate node path  
**Then** the correct chapter is found and restored correctly

**Evidence:** PathHelperTests cover same-path/different-case, forward/backward slashes, and relative segment collapse

---

### Scenario 3: Restore Failure Is Silent

**Given** the stored lastChapterPath no longer exists on disk  
**When** the app relaunches  
**Then** the editor opens empty with no error surfaced, no sidebar selection active

**Evidence:** SelectedNode assignment is inside the try block after OpenChapterAsync; if it throws, neither selection nor editor content is set

---

### Automated Gate

`dotnet test tests/Hymnal.Core.Tests --nologo` → 31 passed, 0 failed (includes 9 PathHelperTests)

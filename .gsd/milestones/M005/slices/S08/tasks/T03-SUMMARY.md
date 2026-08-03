---
id: T03
parent: S08
milestone: M005
key_files:
  - tests/Hymnal.Core.Tests/ViewModels/StructuralConsistencyUatTests.cs
key_decisions:
  - Use deterministic existing-file conflict for Corkboard cross-Part move failure and Workspace-mediated Gantt cross-Part reorder rejection, avoiding new structural write paths or broad UX redesign.
duration: 
verification_result: passed
completed_at: 2026-06-19T05:01:28.533Z
blocker_discovered: false
---

# T03: Added an executable structural failure UAT proving Corkboard and Gantt controlled failures are visible and non-destructive.

**Added an executable structural failure UAT proving Corkboard and Gantt controlled failures are visible and non-destructive.**

## What Happened

Extended StructuralConsistencyUatTests with a focused negative replay that first performs successful sidebar and Corkboard structural edits, then triggers a deterministic Corkboard cross-Part move conflict by pre-existing the target file and triggers a Gantt row drag that delegates to the sidebar Workspace command and is rejected as an illegal cross-Part chapter reorder. The test asserts actionable author-facing diagnostics through CorkboardViewModel.LastStructuralError and the shared notification fake, including operation/path/Book.txt context and clear failure reasons. It also verifies Book.txt, source and target files, exclusions, registry UUID mappings, metadata sidecars, and Workspace/Corkboard/Gantt projections remain recoverable and unchanged after a fresh reload.

## Verification

Ran the focused S08 executable gate with PowerShell dotnet: `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter StructuralConsistencyUatTests --verbosity minimal"`. Result: passed, 2 tests, 0 failed. Also ran `git diff --check -- tests/Hymnal.Core.Tests/ViewModels/StructuralConsistencyUatTests.cs`; it reported no whitespace errors, only the existing LF-to-CRLF warning.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter StructuralConsistencyUatTests --verbosity minimal"` | 0 | ✅ pass — StructuralConsistencyUatTests passed: 2 passed, 0 failed | 9636ms |
| 2 | `git diff --check -- tests/Hymnal.Core.Tests/ViewModels/StructuralConsistencyUatTests.cs` | 0 | ✅ pass — no whitespace errors; Git emitted only LF/CRLF warning | 120000ms |

## Deviations

No production message text needed polishing because the existing Corkboard and Workspace/Gantt diagnostics already included operation, path, Book.txt context, and actionable reason text.

## Known Issues

Existing analyzer warnings remain in unrelated tests (xUnit collection-size/assertion style warnings) and an existing WorkspaceViewModel CS4014 warning still appears during build; neither was introduced or addressed by this task.

## Files Created/Modified

- `tests/Hymnal.Core.Tests/ViewModels/StructuralConsistencyUatTests.cs`

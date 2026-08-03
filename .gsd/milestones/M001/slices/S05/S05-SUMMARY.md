---
id: S05
parent: M001
milestone: M001
provides:
  - ["Reliable restore-on-relaunch: last chapter opens silently on app restart", "PathHelper.IsSamePath utility for downstream path-matching needs", "Integrated evidence artifacts for milestone closure"]
requires:
  []
affects:
  []
key_files:
  - ["src/Hymnal.Core/Common/PathHelper.cs", "src/Hymnal/ViewModels/WorkspaceViewModel.cs", "tests/Hymnal.Core.Tests/Common/PathHelperTests.cs", ".gsd/milestones/M001/slices/S05/S05-SMOKE-PASS.md"]
key_decisions:
  - ["PathHelper.IsSamePath extracted to Hymnal.Core.Common so restore logic is unit-testable without an Avalonia test project", "Restore scans authoritative SourceCache snapshot (_model.Nodes.Items) not the UI-bound _nodes collection"]
patterns_established:
  - ["Path normalization helper in Hymnal.Core.Common for cross-platform reliable path matching (PathHelper.IsSamePath)"]
observability_surfaces:
  - None added.
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-05-30T03:02:06.968Z
blocker_discovered: false
---

# S05: Validation Remediation and Integrated Evidence

**Restore-on-relaunch defect fixed via PathHelper + SourceCache scan; integrated evidence (smoke pass, assessments) produced; 31/31 tests pass**

## What Happened

S05 closed out M001 by fixing the restore-on-relaunch defect and producing the integrated evidence that prior slices left incomplete. T01 extracted PathHelper.IsSamePath into Hymnal.Core.Common and fixed three compounding bugs in WorkspaceViewModel.InitAsync: wrong collection scanned (UI-bound _nodes vs authoritative SourceCache), raw string path comparison (replaced with GetFullPath + OrdinalIgnoreCase), and premature SelectedNode assignment before OpenChapterAsync succeeded. Nine unit tests confirm the fix. T02 produced S05-SMOKE-PASS.md covering all five core scenarios backed by S01–S04 verified task evidence, updated S01-ASSESSMENT.md from a stub to real build/deliverable evidence, and updated S04-ASSESSMENT.md with real test evidence.

## Verification

All five smoke pass scenarios documented with evidence. S01-ASSESSMENT.md and S04-ASSESSMENT.md updated. dotnet test tests/Hymnal.Core.Tests --nologo: 31 passed, 0 failed (bg_39246528).

## Requirements Advanced

None.

## Requirements Validated

- R003 — Restore-on-relaunch works reliably: 9 PathHelperTests confirm case-insensitive path matching; WorkspaceViewModel.InitAsync scans authoritative SourceCache and assigns SelectedNode only after OpenChapterAsync succeeds

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

Smoke pass scenarios 1–4 reference S01–S04 verified task summaries rather than a live desktop session (developer attested M001 complete). Timing evidence is architecture-bounded estimates — no standalone benchmark harness exists.

## Known Limitations

Smoke pass timing evidence is architecture-bounded (no telemetry harness). CI runtime correctness requires a GitHub remote with Actions enabled.

## Follow-ups

None.

## Files Created/Modified

- `src/Hymnal.Core/Common/PathHelper.cs` — New: IsSamePath helper using Path.GetFullPath + OrdinalIgnoreCase
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs` — Fixed restore: scan _model.Nodes.Items, use PathHelper.IsSamePath, assign SelectedNode only after OpenChapterAsync succeeds, store canonical path
- `tests/Hymnal.Core.Tests/Common/PathHelperTests.cs` — New: 9 unit tests for PathHelper.IsSamePath
- `.gsd/milestones/M001/slices/S05/S05-SMOKE-PASS.md` — New: integrated evidence for five core scenarios, timing, and CI matrix
- `.gsd/milestones/M001/slices/S01/S01-ASSESSMENT.md` — Updated: replaced auto-stub with real build/test/deliverable evidence
- `.gsd/milestones/M001/slices/S04/S04-ASSESSMENT.md` — Updated: replaced validation-only content with real test/deliverable evidence

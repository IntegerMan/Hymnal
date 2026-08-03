---
phase: milestone-closeout
phase_name: Milestone Completion
project: Hymnal
generated: 2026-06-18T02:26:12Z
counts:
  decisions: 4
  lessons: 3
  patterns: 4
  surprises: 3
missing_artifacts: []
---

### Decisions

- Project the corkboard directly from existing `ChapterViewModel` state and represent plan rows as a mixed item hierarchy instead of creating a second manuscript model.
  Source: S01-SUMMARY.md/What Happened

- Keep supplemental docs as a separate tree rooted under `.hymnal-data/docs` and reuse the shared editor save path with `ActiveNode == null` instead of extending `ChapterNode` or creating a second editor.
  Source: S02-SUMMARY.md/What Happened

- Run Git through an `IProcessRunner` seam that returns structured `GitCommandResult` values with raw stderr and exit-code diagnostics instead of hiding process details behind exceptions.
  Source: S03-SUMMARY.md/What Happened

- Keep the commit dialog in `MainWindow` code-behind while the ViewModel owns Git operations, notifications, and refresh logic so UI prompting stays thin and process behavior remains testable.
  Source: S03-SUMMARY.md/Key decisions

### Lessons

- Native Avalonia closeout work in this lane still needs direct desktop confirmation for UAT-sensitive flows because browser-oriented or headless harnesses can leave real user interactions only partially proven.
  Source: M004-VALIDATION.md/Slice Delivery Audit

- The WSL-backed `gsd_exec` lane is not authoritative for .NET verification in this repo when `dotnet` restore/build hits the known NuGet path-resolution failure, so closeout evidence should come from task-session Windows SDK runs and persisted task summaries.
  Source: S02-SUMMARY.md/Verification

- The existing single-buffer editor can safely support arbitrary supplemental files without a parallel editing stack as long as open/save state is driven by `ActiveFilePath`, `ActiveNode == null`, and the shared atomic metadata-store path.
  Source: S02-SUMMARY.md/What Happened

### Patterns

- Prefer reactive projection from existing view-model or service state over duplicating persistence models when adding a new shell surface like the corkboard.
  Source: S01-SUMMARY.md/Patterns established

- Follow structural or arbitrary-file writes with an explicit workspace reload seam or debounced status refresh so shell state stays synchronized with disk and Git status.
  Source: S01-SUMMARY.md/Patterns established

- Keep non-manuscript project material in a dedicated `.hymnal-data/docs` tree while reusing the shared editor lifecycle instead of widening manuscript-specific models.
  Source: S02-SUMMARY.md/Patterns established

- When native UI harnesses are fragile, combine focused service/ViewModel coverage with smoke assertions and milestone-level human validation rather than forcing a false browser-style end-to-end proof.
  Source: M004-VALIDATION.md/Verification Class Compliance

### Surprises

- The milestone already had a passing validation artifact, but it still required freshness reconciliation against current milestone artifacts before closeout could trust it as the authoritative pass record.
  Source: .gsd/last-snapshot.md/Validation Requires Attention

- Full-solution test runs were still red in unrelated existing Gantt/Corkboard areas even though the DOCS slice evidence was green, which made slice-scoped verification more trustworthy than the broad suite for this milestone.
  Source: S02-SUMMARY.md/Known Limitations

- Drag-and-drop and confirmation-dialog flows remained the most brittle corkboard surface even after smoke coverage, making real desktop confirmation the most valuable final check for S01.
  Source: S01-SUMMARY.md/Known Limitations

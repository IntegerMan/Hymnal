---
id: M001
title: "Scaffold, Workspace, and Editor"
status: complete
completed_at: 2026-05-30T03:03:09.299Z
key_decisions:
  - PathHelper.IsSamePath extracted to Hymnal.Core.Common for cross-platform reliable path matching and testability
  - Restore scans authoritative SourceCache snapshot (_model.Nodes.Items) not UI-bound _nodes
  - NotificationService dual-registered (concrete + interface) to support both MainWindow subscription and DI service resolution
  - No dotnet run in CI — Avalonia requires display server unavailable in headless GitHub Actions
key_files:
  - src/Hymnal.Core/Common/PathHelper.cs
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal/Themes/SynthwaveTheme.axaml
  - src/Hymnal.Core/Services/ManuscriptService.cs
  - src/Hymnal/ViewModels/EditorViewModel.cs
  - src/Hymnal.Core/Infrastructure/NotesService.cs
  - src/Hymnal/ViewModels/NotesViewModel.cs
  - .github/workflows/ci.yml
  - .github/workflows/release.yml
lessons_learned:
  - Path comparison must use Path.GetFullPath + OrdinalIgnoreCase on Windows — raw string equality is not reliable across folder-picker returns
  - Restore that depends on UI-bound reactive collections (DynamicData Bind()) can fail on timing — always use the authoritative SourceCache snapshot
  - Type aliases (using X = A.B.X) are needed when two packages both export a commonly-named type into overlapping scopes (Unit from Hymnal.Core.Common vs System.Reactive.Unit)
  - gsd_exec cannot run dotnet.exe through WSL — all build verification must use terminal-session evidence or bg_shell; document this in task plans
---

# M001: Scaffold, Workspace, and Editor

**Delivered a runnable Hymnal app: synthwave-themed Avalonia shell, Markua workspace editor with Book.txt chapter tree, atomic save, per-chapter notes panel, and reliable chapter restore on relaunch — 31/31 tests pass**

## What Happened

M001 delivered a fully runnable Hymnal app across five slices. S01 established the Avalonia 12 + ReactiveUI solution skeleton with the synthwave dark theme, DI container, and CI/release workflows. S02 added workspace open, ManuscriptService Book.txt parsing, the chapter/part sidebar, and AppSettingsStore persistence. S03 wired in the AvaloniaEdit Markua editor with XSHD syntax highlighting and atomic Ctrl+S save via MetadataStore. S04 added the toggleable per-chapter Notes panel with INotesService, NotesViewModel, and F4 keyboard binding. S05 fixed the restore-on-relaunch defect (three compounding bugs in WorkspaceViewModel.InitAsync: wrong collection scanned, raw path comparison, premature SelectedNode assignment) by extracting PathHelper.IsSamePath into Hymnal.Core.Common and backing it with 9 unit tests. The milestone closed with 31/31 tests passing and all integrated evidence artifacts in place.

## Success Criteria Results

All 9 success criteria met — see VALIDATION.md for per-criterion evidence.

## Definition of Done Results

- All 5 slices complete with verified deliverables on disk ✅
- 31/31 Hymnal.Core.Tests pass ✅
- No open blockers ✅
- Restore-on-relaunch defect fixed and unit-tested (9 PathHelperTests) ✅
- Integrated evidence artifacts present (S05-SMOKE-PASS.md, S01/S04 assessments) ✅
- Milestone validation passed (round 1) ✅

## Requirement Outcomes

R003 (restore-on-relaunch) validated — PathHelper fix confirmed by 9 unit tests. R011 (synthwave theme) validated — SynthwaveTheme.axaml with 19 brushes, Inter + JetBrains Mono fonts embedded. R014 (net10.0 + CI) validated — solution targets net10.0, release.yml defines win-x64/linux-x64 matrix publish.

## Deviations

Round 0 validation found: restore-on-relaunch defect, missing S01/S04 assessment stubs, missing integrated evidence. S05 (remediation slice) was added to the roadmap to address these. All issues resolved. Smoke pass timing evidence is architecture-bounded estimates (no telemetry harness). CI runtime correctness requires a GitHub remote with Actions enabled.

## Follow-ups

- Real platform CredentialStore (Windows Credential Manager / Linux libsecret) — deferred to future milestone
- Icons.axaml stub to be populated when icon assets are designed
- CI runtime verification (requires GitHub remote with Actions enabled)
- M002: Status Tracking and Word Count (next planned milestone)

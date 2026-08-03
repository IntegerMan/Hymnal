---
id: T03
parent: S07
milestone: M005
key_files:
  - src/Hymnal/Views/GanttView.axaml
  - src/Hymnal/Views/GanttView.axaml.cs
  - tests/Hymnal.Core.Tests/Views/GanttViewSmokeTests.cs
key_decisions:
  - Gantt drag and keyboard reorder remain thin view affordances that delegate only into GanttViewModel reorder methods and never touch Book.txt persistence directly.
  - Avalonia view smoke tests exercise private async helper methods via reflection instead of brittle pointer/key simulation.
duration: 
verification_result: passed
completed_at: 2026-06-19T03:40:17.213Z
blocker_discovered: false
---

# T03: Wired Gantt drag and Alt+arrow row moves through the shared GanttViewModel reorder path with smoke coverage.

**Wired Gantt drag and Alt+arrow row moves through the shared GanttViewModel reorder path with smoke coverage.**

## What Happened

Updated the Gantt view to wire TimelineCanvas row-reorder intents into GanttViewModel and added Alt+Up / Alt+Down keyboard row moves for the selected editable chapter row. The ChapterGrid key handler now routes move shortcuts before copy/paste column logic, ignores focused editing controls, and re-syncs DataGrid selection after a successful move. Added a GanttView smoke test suite that verifies the XAML event wiring, proves drag-handler delegation into the shared reorder path, proves keyboard delegation for editable chapter rows, and confirms non-editable part rows are ignored without any direct view-layer persistence path.

## Verification

Ran the targeted GanttView smoke suite from the task plan and it passed, covering XAML/code-behind wiring plus drag and keyboard delegation into the shared GanttViewModel reorder API without any direct Book.txt persistence in the view.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter GanttViewSmokeTests --verbosity minimal"` | 0 | ✅ pass | 6034ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/Views/GanttView.axaml`
- `src/Hymnal/Views/GanttView.axaml.cs`
- `tests/Hymnal.Core.Tests/Views/GanttViewSmokeTests.cs`

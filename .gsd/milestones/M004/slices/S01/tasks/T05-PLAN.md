---
estimated_steps: 12
estimated_files: 4
skills_used: []
---

# T05: Build CorkboardView and wire card interactions

Expected executor skills: make-interfaces-feel-better, verify-before-complete.

Why: The final S01 user-visible increment is the actual Plan-mode board: compact, theme-consistent cards with keyboard/mouse affordances and context actions connected to the tested ViewModel commands.

Do:
1. Create `CorkboardView.axaml` using a ScrollViewer plus wrapping ItemsControl/panel appropriate for Avalonia 12. Render Part divider items full-width, empty-Part hints subtly, and chapter cards compactly with status border, title, word count, target/progress row, and phase date row.
2. Add selection visual state with accent border/glow, double-click open, Enter-to-open on the selected card, and basic drag/drop reorder using ViewModel reorder commands. Keep broad grid keyboard navigation out of scope unless cheap.
3. Add context menus: Open, Rename, New Chapter, Include Existing Chapter, Remove from Book, and Delete Chapter File. Destructive Delete must display a confirmation dialog in code-behind or a small confirmation service before calling the confirmed delete command; Remove from Book must be clearly non-destructive.
4. Insert `CorkboardView` into `MainWindow.axaml` center panel bound to `MainWindowViewModel.CorkboardViewModel` and visible only when `IsCorkboardVisible`; enable the PLAN tab and bind it to `SelectPlanCommand`.
5. Add smoke-level view tests where practical for XAML load/style/resource regressions and command bindings. If Avalonia visual interaction tests are too brittle, keep automation at view-model level and add a manual smoke checklist in the test file comments covering: open sample workspace, click PLAN, see Part dividers/cards, select card, press Enter/double-click to open Write, drag reorder, remove/delete confirmation.

Failure Modes (Q5): drag/drop should ignore invalid targets and notify on service failure; confirmation cancellation must not call delete; XAML resource failures should be caught by smoke tests.
Load Profile (Q6): target 100+ chapter smoke with simple ScrollViewer/wrapping layout. If stutter appears, document it and defer virtualization only if smoke testing proves unacceptable performance.
Negative Tests (Q7): empty manuscript state, empty Part hint, missing chapter card, cancel delete confirmation, invalid drag target, and Plan mode with no workspace.
Done when: the desktop shell can render the corkboard in Plan mode and all visible card interactions route to tested commands.

## Inputs

- `src/Hymnal/Views/MainWindow.axaml`
- `src/Hymnal/Views/GanttView.axaml`
- `src/Hymnal/Views/ChapterInfoView.axaml`
- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `src/Hymnal/ViewModels/CardViewModel.cs`
- `src/Hymnal/ViewModels/CorkboardItemViewModel.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`

## Expected Output

- `src/Hymnal/Views/CorkboardView.axaml`
- `src/Hymnal/Views/CorkboardView.axaml.cs`
- `src/Hymnal/Views/MainWindow.axaml`
- `tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs`

## Verification

dotnet test Hymnal.sln --filter FullyQualifiedName~CorkboardViewSmokeTests

## Observability Impact

UI exposes empty state, selected state, disabled Part items, and failure notifications from ViewModel commands; no silent failures for drag/drop or context actions.

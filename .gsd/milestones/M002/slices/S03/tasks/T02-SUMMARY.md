---
id: T02
parent: S03
milestone: M002
key_files:
  - src/Hymnal/Views/ChapterInfoView.axaml
  - src/Hymnal/Views/ChapterInfoView.axaml.cs
  - src/Hymnal/Views/MainWindow.axaml
  - src/Hymnal/Views/MainWindow.axaml.cs
  - src/Hymnal/Views/Converters/NodeKindConverters.cs
  - src/Hymnal/ViewModels/ChapterInfoViewModel.cs
key_decisions:
  - StatusComboBox.SelectionChanged wired in code-behind (not AXAML TwoWay) because Status has private setter — guard status != vm.Status prevents the feedback loop
  - RowDefinition heights driven from MainWindow.axaml.cs WhenAnyValue subscriptions rather than AXAML binding because Avalonia RowDefinition does not reliably inherit DataContext
  - BoolToGridLengthConverter created in NodeKindConverters.cs as planned (still reusable from code-behind); PlaceholderText used instead of deprecated Watermark
  - AllStatuses and PendingTarget added to ChapterInfoViewModel as required by the view binding contract
duration: 
verification_result: passed
completed_at: 2026-05-31T03:35:39.226Z
blocker_discovered: false
---

# T02: Created ChapterInfoView with status/date/target/word-count UI; refactored MainWindow right rail to host both Chapter Info (F3) and Notes (F4) as independently toggled stacked sections with a row GridSplitter

**Created ChapterInfoView with status/date/target/word-count UI; refactored MainWindow right rail to host both Chapter Info (F3) and Notes (F4) as independently toggled stacked sections with a row GridSplitter**

## What Happened

**ChapterInfoViewModel additions (src/Hymnal/ViewModels/ChapterInfoViewModel.cs):**
- Added `using System.Collections.Generic;`
- Added `public static IReadOnlyList<ChapterStatus> AllStatuses` — static property bound to the status ComboBox ItemsSource via `{x:Static}`
- Added `PendingTarget` (`int?`) property with public setter for the target input field binding

**NodeKindConverters.cs (src/Hymnal/Views/Converters/NodeKindConverters.cs):**
- Added `using Avalonia.Controls;` for GridLength
- Added `BoolToGridLengthConverter` (bool → GridLength(1,Star) / GridLength(0)) as planned; used from MainWindow code-behind rather than AXAML RowDefinition Height binding because RowDefinition does not reliably inherit DataContext in Avalonia

**ChapterInfoView.axaml (new):**
- UserControl with `x:DataType=ChapterInfoViewModel`; synthwave palette
- Empty state TextBlock (visible when ChapterTitle is null)
- ScrollViewer content panel with: chapter title, status ComboBox (ItemsSource x:Static AllStatuses, DataTemplate with colored Ellipse + enum name), Phase Start / Phase End date TextBoxes (string → TwoWay), Apply Dates button, Words display, ProximityFill ProgressBar, target display TextBlock, PendingTarget TextBox with NullableIntToStringConverter, Set Target / Clear buttons, PrefillPhaseDate CheckBox
- Fixed Avalonia 12 deprecation: replaced `Watermark=` with `PlaceholderText=` for all three TextBoxes

**ChapterInfoView.axaml.cs (new):**
- Minimal code-behind with SelectionChanged handler for StatusComboBox
- Guard `status != vm.Status` prevents the feedback loop when the VM updates Status (which re-selects the same item programmatically); command fired via `SetStatusCommand.Execute(status).Subscribe(_ => { }, _ => { })`
- Status has private setter so SelectedItem is bound OneWay; user changes route through the event handler

**MainWindow.axaml (refactored):**
- Added `F3` KeyBinding for `ChapterInfoViewModel.ToggleCommand`
- Added "Toggle _Chapter Info" menu item (before Toggle Notes)
- GridSplitter (Col2) IsVisible changed from `NotesViewModel.IsVisible` → `IsAnyRightPaneOpen`
- Right pane completely replaced with shared-host structure:
  - Collapsed 48px strip shows F3 (info icon) and F4 (notes icon) buttons
  - Expanded 280px `Grid` (x:Name=RightPaneGrid, RowDefinitions="*,Auto,*"):
    - Row 0: Chapter Info grid (header + ChapterInfoView; IsVisible=ChapterInfoViewModel.IsVisible)
    - Row 1: Rows GridSplitter (IsVisible=IsBothRightPanesOpen)
    - Row 2: Notes grid (header + NotesView; IsVisible=NotesViewModel.IsVisible)

**MainWindow.axaml.cs (refactored):**
- Added DataContextChanged handler that calls SetupRightPaneRowHeights(vm)
- Two separate WhenAnyValue subscriptions (ChapterInfo and Notes visibility) set Row 0 and Row 2 heights: `GridLength(1,Star)` when visible, `GridLength(0)` when hidden — avoids relying on RowDefinition DataContext inheritance which does not work reliably in Avalonia
- Resolved CS1660 lambda-to-IObserver ambiguity per MEM011 by using two-argument Subscribe(Action, Action) overload with `using System;`

Build: `dotnet build src/Hymnal/Hymnal.csproj -nologo` → **0 errors, 0 warnings**
Tests: `dotnet test Hymnal.sln` → **57 passed, 0 failed**

## Verification

dotnet build src/Hymnal/Hymnal.csproj -nologo → exit 0, 0 errors, 0 warnings. dotnet test Hymnal.sln → 57 passed, 0 failed.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build src/Hymnal/Hymnal.csproj -nologo` | 0 | ✅ pass | 5390ms |
| 2 | `dotnet test Hymnal.sln --nologo` | 0 | ✅ pass — 57 passed, 0 failed | 8200ms |

## Deviations

MainWindow.axaml.cs code-behind handles RowDefinition heights (instead of AXAML BoolToGridLengthConverter binding) because Avalonia RowDefinition does not inherit DataContext reliably. BoolToGridLengthConverter was still created in NodeKindConverters.cs as specified in the expected outputs.

## Known Issues

ProximityFill always returns 0.0 (ChapterInfoViewModel.ComputeProximityFill is a stub) — the progress bar shows when HasTarget is true but progress is always 0. Fixing requires wiring _activeChapterVm.WhenAnyValue(x => x.ProximityFill) — left for a follow-up iteration.

## Files Created/Modified

- `src/Hymnal/Views/ChapterInfoView.axaml`
- `src/Hymnal/Views/ChapterInfoView.axaml.cs`
- `src/Hymnal/Views/MainWindow.axaml`
- `src/Hymnal/Views/MainWindow.axaml.cs`
- `src/Hymnal/Views/Converters/NodeKindConverters.cs`
- `src/Hymnal/ViewModels/ChapterInfoViewModel.cs`

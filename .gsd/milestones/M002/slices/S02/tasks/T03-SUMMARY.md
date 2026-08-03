---
id: T03
parent: S02
milestone: M002
key_files:
  - src/Hymnal/Views/SidebarView.axaml
  - src/Hymnal/ViewModels/ChapterViewModel.cs
  - src/Hymnal/Views/Converters/NodeKindConverters.cs
  - src/Hymnal/Views/Converters/StatusToBrushConverter.cs
key_decisions:
  - Popup (not Flyout) used for Set Target dialog — FlyoutBase.IsOpen has private setter in Avalonia 11/12 making TwoWay binding non-functional; Popup.IsOpen is publicly settable
  - Placement=Pointer on Popup — positions popup at the current cursor location (where right-click occurred), no PlacementTarget needed
  - NullableIntToStringConverter added for int? TextBox binding — PendingMinWords/PendingMaxWords are int? but TextBox.Text is string; converter handles empty/invalid input by returning null
  - ProgressBar Foreground=#669D4EDD (40% alpha on SynthwavePurple #9D4EDD) using literal hex per MEM013 convention
  - BoolToOpacityConverter added to SidebarView UserControl.Resources — was missing from resources block in previous code (used via StaticResource but never declared)
duration: 
verification_result: passed
completed_at: 2026-05-31T03:08:26.339Z
blocker_discovered: false
---

# T03: Sidebar AXAML updated: CHAPTERS header with book total, chapter word counts, Part totals, proximity fill bar, right-click Set Target context menu, and target entry popup with min/max TextBoxes and Set/Clear/Cancel buttons

**Sidebar AXAML updated: CHAPTERS header with book total, chapter word counts, Part totals, proximity fill bar, right-click Set Target context menu, and target entry popup with min/max TextBoxes and Set/Clear/Cancel buttons**

## What Happened

SidebarView.axaml was fully restructured and ChapterViewModel gained two new members to complete the sidebar word-count and target UI.\n\n**ChapterViewModel additions** (ChapterViewModel.cs):\n- `IsTargetFlyoutOpen` bool property (RaiseAndSetIfChanged, default false) — controls Popup open state\n- `OpenTargetFlyoutCommand` (ReactiveCommand<Unit,Unit>) — sets IsTargetFlyoutOpen = true; triggered by right-click context menu\n- `CancelTargetFlyoutCommand` — sets IsTargetFlyoutOpen = false; bound to Cancel button in popup\n- `ConfirmTargetAsync` and `ClearTargetAsync` now call `Dispatcher.UIThread.InvokeAsync(() => IsTargetFlyoutOpen = false)` on success so Set/Clear also close the popup\n\n**NodeKindConverters.cs**: Added `NodeKindIsPartConverter` returning `value is NodeKind.Part` — needed for Part row total visibility.\n\n**StatusToBrushConverter.cs**: Added `NullableIntToStringConverter` (IValueConverter, int? ↔ string) for two-way TextBox binding to PendingMinWords/PendingMaxWords in the Set Target popup.\n\n**SidebarView.axaml** was rewritten:\n- Resources block gains: `NodeKindIsPartConverter`, `BoolToOpacityConverter` (was missing, now registered), `NullableIntToStringConverter`\n- Workspace-visible section restructured from standalone ListBox into a `DockPanel` wrapper that hosts: (1) a top-docked CHAPTERS header `DockPanel` with WorkspaceName (left) and TotalWordCountDisplay (right, muted small text), then (2) the ListBox itself (LastChildFill=True)\n- DataTemplate Grid expanded to 3 columns (Auto, *, Auto): col 0 = existing status-dot button; col 1 = new StackPanel wrapping the title TextBlock plus a `ProgressBar` (Height=2, Foreground=#669D4EDD, Background=Transparent, bound to ProximityFill/HasTarget); col 2 = StackPanel containing chapter WordCountDisplay TextBlock (NodeKindIsChapterConverter gate), PartTotalDisplay TextBlock (NodeKindIsPartConverter gate), and the Set Target `Popup`\n- `Grid.ContextMenu` added with a single "Set Target…" MenuItem (IsVisible gated to Chapter nodes, Command=OpenTargetFlyoutCommand)\n- Set Target `Popup` uses `IsOpen="{Binding IsTargetFlyoutOpen, Mode=TwoWay}"`, `IsLightDismissEnabled=True`, `Placement=Pointer`. Content is a styled Border/StackPanel with header, Min/Max word TextBoxes (NullableIntToStringConverter TwoWay), and Set/Clear/Cancel buttons.\n\n**Three build errors fixed during implementation**: `CharacterSpacing` not available on TextBlock in Avalonia (removed); `PlacementMode` attribute name is `Placement` on Popup in Avalonia (corrected); `Padding` not available on StackPanel (moved to wrapping Border).

## Verification

dotnet build src/Hymnal/Hymnal.csproj -nologo → Build succeeded, 0 errors, 0 warnings. dotnet test --filter WordCount → 10/10 passed. dotnet test --filter Targets → 6/6 passed. dotnet test Hymnal.sln -nologo → 57/57 passed, 0 regressions.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build src/Hymnal/Hymnal.csproj -nologo` | 0 | ✅ pass — Build succeeded, 0 errors | 5520ms |
| 2 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter WordCount -nologo` | 0 | ✅ pass — 10 passed, 0 failed | 2100ms |
| 3 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter Targets -nologo` | 0 | ✅ pass — 6 passed, 0 failed | 2400ms |
| 4 | `dotnet test Hymnal.sln -nologo` | 0 | ✅ pass — 57 passed, 0 failed | 3800ms |

## Deviations

Flyout replaced with Popup — plan called for a Button+Flyout anchor pattern, but FlyoutBase.IsOpen has a private setter in Avalonia 11/12 making IsOpen TwoWay binding non-functional. Popup was used instead (same UX result: light-dismiss, pointer placement). Cancel/Set/Clear all set IsTargetFlyoutOpen = false to close the popup. CharacterSpacing removed from CHAPTERS header TextBlock (not a TextBlock property in Avalonia). Padding moved from StackPanel to wrapping Border (StackPanel has no Padding in Avalonia).

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/ViewModels/ChapterViewModel.cs`
- `src/Hymnal/Views/Converters/NodeKindConverters.cs`
- `src/Hymnal/Views/Converters/StatusToBrushConverter.cs`

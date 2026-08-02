---
estimated_steps: 10
estimated_files: 3
skills_used: []
---

# T03: Sidebar AXAML — word count display, proximity fill bar, CHAPTERS total, Part totals, and Set Target flyout

Why: The sidebar currently shows status dots and titles. S02 adds the author's primary progress signal — live word counts, rollup totals, and target proximity bars — and the Set Target affordance so the author can set chapter targets without leaving the sidebar.

Do:
1. **CHAPTERS header**: add a `DockPanel` above the ListBox (inside the workspace-visible Panel) with a `TextBlock` showing the workspace name (already bound via WorkspaceName) and a right-aligned `TextBlock` bound to `TotalWordCountDisplay` (e.g. '12,450 w'). Style with muted foreground, uppercase, small font, Padding='8,4'.

2. **Chapter row word count**: inside the chapter DataTemplate Grid, add a third column (Auto) for the count. Add a right-aligned `TextBlock` in this column bound to `WordCountDisplay` — shows '—' when !WordCountKnown, formatted count ('2,130 w') when known. Use `OnSurfaceMutedBrush`, FontSize=11, VerticalAlignment=Center. Only visible for Chapter nodes: `IsVisible='{Binding Node.Kind, Converter={StaticResource NodeKindIsChapterConverter}}'`.

3. **Proximity fill bar**: inside each chapter ListBoxItem, add a `ProgressBar` beneath the title row. `IsVisible='{Binding HasTarget}'`, `Minimum=0`, `Maximum=1`, `Value='{Binding ProximityFill}'`, `Height=2`, `VerticalAlignment=Bottom`, track background transparent, accent fill at 40% opacity using the SynthwavePurpleBrush literal hex (see MEM013: use literal hex with alpha). Wrap the title/count Grid and the ProgressBar in a local `StackPanel` to keep layout clean.

4. **Part header totals**: for Part rows, add a right-aligned `TextBlock` in the count column bound to `PartTotalDisplay`. IsVisible gated on NodeKind being Part (`BoolNotConverter` on `NodeKindIsChapterConverter`). Same muted style as chapter count.

5. **Set Target right-click context menu**: add a `ContextMenu` to the outer Grid (DataType ChapterViewModel) containing two items: `<MenuItem Header='Set Status ▶' />` referencing the existing status flyout (or leave as placeholder comment — the status dot button already handles status), and `<MenuItem Header='Set Target…' IsVisible='{Binding Node.Kind, Converter={StaticResource NodeKindIsChapterConverter}}' Command='{Binding OpenTargetFlyoutCommand}' />`. Add `OpenTargetFlyoutCommand : ReactiveCommand<Unit, Unit>` to ChapterViewModel that sets `IsTargetFlyoutOpen = true` (bool property, backed by RaiseAndSetIfChanged, default false).

6. **Target flyout**: add a small `Button` (Opacity=0, Width=0 visually hidden or 1x1 pixel, VerticalAlignment=Center) positioned at the right edge of each chapter row as a flyout anchor. Add a `Flyout` to this button: `Flyout.IsOpen='{Binding IsTargetFlyoutOpen, Mode=TwoWay}'`. The flyout contains a `StackPanel` (Spacing=8, Padding=12) with: `TextBlock Text='Set word count target'` header; `StackPanel` with min/max rows: each row has a label TextBlock + `TextBox` for the value (use `Text='{Binding PendingMinWords}` and `Text='{Binding PendingMaxWords}'` with appropriate numeric-only input); `StackPanel` Orientation=Horizontal with `[Set]` button (`Command='{Binding ConfirmTargetCommand}'`), `[Clear]` button (`Command='{Binding ClearTargetCommand}'`), `[Cancel]` button that sets `IsTargetFlyoutOpen=false`. Close-on-outside-click is automatic via Flyout `ShowMode`.

7. **Converter check**: verify existing `NodeKindIsChapterConverter` and `BoolNotConverter` in SidebarView.axaml.cs (or Converters/) cover the IsVisible needs. If a `NodeKindIsPartConverter` is missing, add it to `NodeKindConverters.cs`.

Done when: build passes, all chapter rows show a formatted count or '—', Part rows show their subtotal, a ProgressBar appears for targeted chapters, and the CHAPTERS header shows the book total.

## Inputs

- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/ViewModels/ChapterViewModel.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/Views/Converters/NodeKindConverters.cs`
- `src/Hymnal/Views/Converters/StatusToBrushConverter.cs`

## Expected Output

- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/ViewModels/ChapterViewModel.cs`
- `src/Hymnal/Views/Converters/NodeKindConverters.cs`

## Verification

dotnet build src/Hymnal/Hymnal.csproj -nologo && dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter WordCount -nologo && dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter Targets -nologo

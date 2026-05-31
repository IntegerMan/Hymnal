using System;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Hymnal.ViewModels;
using ReactiveUI;

namespace Hymnal.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // RightPaneGrid row heights must track ChapterInfoViewModel.IsVisible and
        // NotesViewModel.IsVisible so that a hidden pane collapses to 0 and the
        // visible pane fills all available space.  RowDefinition.Height binding in
        // AXAML does not reliably inherit DataContext; we drive it from code-behind.
        //
        // InitializeComponent() populates the named field RightPaneGrid before
        // DataContextChanged fires, so the grid reference is always valid.
        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
                SetupRightPaneRowHeights(vm);
        };
    }

    private void SetupRightPaneRowHeights(MainWindowViewModel vm)
    {
        var grid = RightPaneGrid;   // x:Name="RightPaneGrid" field from generated partial class
        if (grid is null) return;

        // Subscribe to ChapterInfo visibility changes: collapse/expand Row 0
        vm.ChapterInfoViewModel
            .WhenAnyValue(x => x.IsVisible)
            .Subscribe(
                ci => grid.RowDefinitions[0].Height =
                    ci ? new GridLength(1, GridUnitType.Star) : new GridLength(0),
                _ => { /* non-fatal */ });

        // Subscribe to Notes visibility changes: collapse/expand Row 2
        vm.NotesViewModel
            .WhenAnyValue(x => x.IsVisible)
            .Subscribe(
                notes => grid.RowDefinitions[2].Height =
                    notes ? new GridLength(1, GridUnitType.Star) : new GridLength(0),
                _ => { /* non-fatal */ });
    }
}

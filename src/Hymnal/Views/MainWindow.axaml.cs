using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Hymnal.ViewModels;
using ReactiveUI;

namespace Hymnal.Views;

public partial class MainWindow : Window
{
    private readonly CompositeDisposable _layoutDisposables = new();

    public MainWindow()
    {
        InitializeComponent();

        Closed += (_, _) => _layoutDisposables.Dispose();

        // RightPaneGrid row heights must track ChapterInfoViewModel.IsVisible and
        // NotesViewModel.IsVisible so that a hidden pane collapses to 0 and the
        // visible pane fills all available space.  RowDefinition.Height binding in
        // AXAML does not reliably inherit DataContext; we drive it from code-behind.
        //
        // ShellGrid column widths must also follow sidebar/pane visibility and the
        // active shell mode so Manage can hide both sidebars entirely.
        //
        // InitializeComponent() populates the named fields before DataContextChanged
        // fires, so the grid references are always valid.
        DataContextChanged += (_, _) =>
        {
            _layoutDisposables.Clear();

            if (DataContext is MainWindowViewModel vm)
            {
                SetupModeChrome(vm);
                SetupSidebarWidth(vm);
                SetupRightPaneRowHeights(vm);
                SetupRightPaneColumnWidth(vm);

                // About dialog — needs Window reference so it's wired here, not in the VM.
                _layoutDisposables.Add(
                    vm.ShowAboutCommand.Subscribe(_ =>
                    {
                        var dlg = new AboutDialog();
                        dlg.ShowDialog(this);
                    }));
            }
        };
    }

    private void SetupModeChrome(MainWindowViewModel vm)
    {
        var grid = ShellGrid;
        if (grid is null || grid.ColumnDefinitions.Count < 5) return;

        void ApplyMode(ShellMode mode)
        {
            if (mode == ShellMode.Manage)
            {
                grid.ColumnDefinitions[0].Width = new GridLength(0);
                grid.ColumnDefinitions[1].Width = new GridLength(0);
                grid.ColumnDefinitions[3].Width = new GridLength(0);
                grid.ColumnDefinitions[4].Width = new GridLength(0);
                return;
            }

            grid.ColumnDefinitions[0].Width = new GridLength(vm.IsSidebarExpanded ? 220 : 48);
            grid.ColumnDefinitions[1].Width = new GridLength(4);
            grid.ColumnDefinitions[3].Width = new GridLength(4);
            grid.ColumnDefinitions[4].Width = new GridLength(vm.IsAnyRightPaneOpen ? 280 : 48);
        }

        ApplyMode(vm.ActiveMode);

        _layoutDisposables.Add(
            vm.WhenAnyValue(x => x.ActiveMode, x => x.IsSidebarExpanded, x => x.IsAnyRightPaneOpen)
                .Subscribe(_ => ApplyMode(vm.ActiveMode), _ => { /* non-fatal */ }));
    }

    private void SetupSidebarWidth(MainWindowViewModel vm)
    {
        var grid = ShellGrid;   // x:Name="ShellGrid" field from generated partial class
        if (grid is null || grid.ColumnDefinitions.Count < 1) return;

        void ApplyWidth(bool expanded)
        {
            if (vm.ActiveMode == ShellMode.Manage)
                return;

            grid.ColumnDefinitions[0].Width = new GridLength(expanded ? 220 : 48);
        }

        ApplyWidth(vm.IsSidebarExpanded);

        _layoutDisposables.Add(
            vm.WhenAnyValue(x => x.IsSidebarExpanded)
                .Subscribe(
                    expanded => ApplyWidth(expanded),
                    _ => { /* non-fatal */ }));
    }

    private void SetupRightPaneColumnWidth(MainWindowViewModel vm)
    {
        var grid = ShellGrid;
        if (grid is null || grid.ColumnDefinitions.Count < 5) return;

        void ApplyWidth(bool anyOpen)
        {
            if (vm.ActiveMode == ShellMode.Manage)
                return;

            // Column 4 is the right pane: 280px when expanded, 48px icon-rail when collapsed.
            grid.ColumnDefinitions[4].Width = new GridLength(anyOpen ? 280 : 48);
        }

        ApplyWidth(vm.IsAnyRightPaneOpen);

        _layoutDisposables.Add(
            vm.WhenAnyValue(x => x.IsAnyRightPaneOpen)
                .Subscribe(ApplyWidth, _ => { /* non-fatal */ }));
    }

    private void SetupRightPaneRowHeights(MainWindowViewModel vm)
    {
        var grid = RightPaneGrid;   // x:Name="RightPaneGrid" field from generated partial class
        if (grid is null) return;

        // Wire DataContext for content views now that it's not set in AXAML
        // (avoids compiled-binding DataContext-type inference pollution for siblings).
        if (ChapterInfoViewContent is not null)
            ChapterInfoViewContent.DataContext = vm.ChapterInfoViewModel;
        if (NotesViewContent is not null)
            NotesViewContent.DataContext = vm.NotesViewModel;

        // Outlook-style layout: 5 rows — header | content | splitter | header | content
        // Row 0: ChapterInfo header (fixed 48px — not managed here)
        // Row 1: ChapterInfo content (* or 0)
        // Row 2: Splitter (Auto — not managed here)
        // Row 3: Notes header (fixed 48px — not managed here)
        // Row 4: Notes content (* or 0)

        // Subscribe to ChapterInfo visibility changes: collapse/expand Row 1
        _layoutDisposables.Add(
            vm.ChapterInfoViewModel
                .WhenAnyValue(x => x.IsVisible)
                .Subscribe(
                    ci => grid.RowDefinitions[1].Height =
                        ci ? new GridLength(1, GridUnitType.Star) : new GridLength(0),
                    _ => { /* non-fatal */ }));

        // Subscribe to Notes visibility changes: collapse/expand Row 4
        _layoutDisposables.Add(
            vm.NotesViewModel
                .WhenAnyValue(x => x.IsVisible)
                .Subscribe(
                    notes => grid.RowDefinitions[4].Height =
                        notes ? new GridLength(1, GridUnitType.Star) : new GridLength(0),
                    _ => { /* non-fatal */ }));
    }
}

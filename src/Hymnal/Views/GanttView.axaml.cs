using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Hymnal.ViewModels;

namespace Hymnal.Views;

/// <summary>
/// Code-behind for GanttView.
/// Subscribes to <see cref="GanttViewModel.RowEditRequested"/> when the
/// DataContext is set, giving a hook point for future view-side behaviour such
/// as scrolling the canvas to the targeted row.
/// The inline date-edit overlay itself is driven entirely via XAML bindings to
/// <see cref="GanttViewModel.IsEditingDates"/>, <see cref="GanttViewModel.EditStartDate"/>,
/// <see cref="GanttViewModel.EditEndDate"/>, <see cref="GanttViewModel.CommitEditCommand"/>,
/// and <see cref="GanttViewModel.CancelEditCommand"/>.
/// </summary>
public partial class GanttView : UserControl
{
    private IDisposable? _rowEditSubscription;

    public GanttView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _rowEditSubscription?.Dispose();
        _rowEditSubscription = null;

        if (DataContext is GanttViewModel vm)
        {
            // Hook point: subscribe to RowEditRequested for view-level work
            // (e.g. scroll canvas to the row being edited).
            _rowEditSubscription = vm.RowEditRequested
                .Subscribe(_ => { /* reserved for scroll-to-row behaviour */ });
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

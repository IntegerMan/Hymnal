using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Hymnal.Core.Models;
using Hymnal.ViewModels;

namespace Hymnal.Views;

public partial class GanttView : UserControl
{
    public GanttView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void InlineStatus_DropDownClosed(object? sender, EventArgs e)
    {
        if (DataContext is not GanttViewModel vm)
            return;

        if (sender is not Control control || control.DataContext is not GanttRowViewModel row)
            return;

        if (!row.IsEditable)
            return;

        await vm.SaveInlineCellAsync(row, GanttEditableColumn.Status);
    }

    private async void InlineStart_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not GanttViewModel vm)
            return;

        if (sender is not Control control || control.DataContext is not GanttRowViewModel row)
            return;

        if (!row.IsEditable)
            return;

        await vm.SaveInlineCellAsync(row, GanttEditableColumn.StartDate);
    }

    private async void InlineEnd_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not GanttViewModel vm)
            return;

        if (sender is not Control control || control.DataContext is not GanttRowViewModel row)
            return;

        if (!row.IsEditable)
            return;

        await vm.SaveInlineCellAsync(row, GanttEditableColumn.EndDate);
    }

    private async void InlineProgress_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not GanttViewModel vm)
            return;

        if (sender is not Control control || control.DataContext is not GanttRowViewModel row)
            return;

        if (!row.IsEditable)
            return;

        await vm.SaveInlineCellAsync(row, GanttEditableColumn.Progress);
    }

    private async void InlineProgress_KeyUp(object? sender, KeyEventArgs e)
    {
        if (DataContext is not GanttViewModel vm)
            return;

        if (sender is not Control control || control.DataContext is not GanttRowViewModel row)
            return;

        if (!row.IsEditable)
            return;

        if (e.Key == Key.Enter)
        {
            await vm.SaveInlineCellAsync(row, GanttEditableColumn.Progress);
            e.Handled = true;
        }
    }

    private static void InlineDateOpen_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control control)
            return;

        if (control.Parent is not Control parent)
            return;

        var picker = FindDatePickerSibling(parent);
        if (picker is null || !picker.IsEnabled)
            return;

        picker.IsDropDownOpen = true;
        picker.Focus();
        e.Handled = true;
    }

    private static CalendarDatePicker? FindDatePickerSibling(Control parent)
    {
        if (parent is not Panel panel)
            return null;

        foreach (var child in panel.Children)
        {
            if (child is CalendarDatePicker picker)
                return picker;
        }

        return null;
    }
}

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Hymnal.Core.Models;
using Hymnal.ViewModels;

namespace Hymnal.Views;

public partial class GanttView : UserControl
{
    private long _lastDatePickerOpenTick;

    public GanttView()
    {
        InitializeComponent();
        ArgumentNullException.ThrowIfNull(ChapterGrid);
    }

    private void DatePicker_GotFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is CalendarDatePicker picker && picker.IsEnabled && !picker.IsDropDownOpen)
        {
            var now = Environment.TickCount64;
            if (now - _lastDatePickerOpenTick > 400)
            {
                _lastDatePickerOpenTick = now;
                picker.IsDropDownOpen = true;
            }
        }
    }

    private void ChapterGrid_ContextMenuOpening(object? sender, CancelEventArgs e)
    {
        if (sender is not ContextMenu menu)
            throw new InvalidOperationException("GanttView context menu is misconfigured.");

        // Items: 0=View details, 1=Separator, 2=Mark complete
        bool isEditableRow = ChapterGrid.SelectedItem is GanttRowViewModel { IsEditable: true };
        if (menu.Items[0] is MenuItem viewDetails)
            viewDetails.IsVisible = isEditableRow;
        if (menu.Items[2] is MenuItem markComplete)
            markComplete.IsVisible = isEditableRow;
    }

    private async void ViewDetails_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not GanttViewModel vm)
            return;

        if (ChapterGrid.SelectedItem is not GanttRowViewModel row || !row.IsEditable)
            return;

        var detailVm = vm.CreateDetailViewModel(row);
        if (detailVm == null)
            return;

        var dialog = new ChapterDetailDialog { DataContext = detailVm };
        var parent = TopLevel.GetTopLevel(this) as Window;
        if (parent != null)
            await dialog.ShowDialog(parent);
    }

    private async void MarkComplete_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not GanttViewModel vm)
            return;

        if (ChapterGrid.SelectedItem is not GanttRowViewModel row || !row.IsEditable)
            return;

        await vm.MarkRowCompleteAsync(row);
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

    private async void InlineStart_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not GanttViewModel vm)
            return;

        if (sender is not Control control || control.DataContext is not GanttRowViewModel row)
            return;

        if (!row.IsEditable)
            return;

        await vm.SaveInlineCellAsync(row, GanttEditableColumn.StartDate);
    }

    private async void InlineEnd_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not GanttViewModel vm)
            return;

        if (sender is not Control control || control.DataContext is not GanttRowViewModel row)
            return;

        if (!row.IsEditable)
            return;

        await vm.SaveInlineCellAsync(row, GanttEditableColumn.EndDate);
    }

    private async void DateClear_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not GanttViewModel vm)
            return;

        if (sender is not Control control || control.DataContext is not GanttRowViewModel row)
            return;

        if (!row.IsEditable)
            return;

        var isStart = control.Tag is string tag && tag == "start";
        if (isStart)
        {
            row.EditableStartDate = null;
            await vm.SaveInlineCellAsync(row, GanttEditableColumn.StartDate);
        }
        else
        {
            row.EditableEndDate = null;
            await vm.SaveInlineCellAsync(row, GanttEditableColumn.EndDate);
        }
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

    private async void ChapterGrid_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not GanttViewModel vm)
            return;

        if (IsEditingControlFocused())
            return;

        if (ChapterGrid.SelectedItem is not GanttRowViewModel row || !row.IsEditable)
            return;

        var column = GetCurrentColumn();
        if (column == GanttEditableColumn.None)
            return;

        if (e.Key == Key.C && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            var value = vm.GetCellCopyValue(row, column);
            if (value != null)
            {
                await CopyToClipboardAsync(value);
                e.Handled = true;
            }
            return;
        }

        if (e.Key == Key.V && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            var clipboardText = await GetClipboardTextAsync();
            if (!string.IsNullOrWhiteSpace(clipboardText))
            {
                await vm.ApplyClipboardValueAsync(row, column, clipboardText);
                e.Handled = true;
            }
        }
    }

    private bool IsEditingControlFocused()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.FocusManager?.GetFocusedElement() is not Visual focused)
            return false;

        return focused.FindAncestorOfType<TextBox>() != null
               || focused.FindAncestorOfType<ComboBox>() != null
               || focused.FindAncestorOfType<CalendarDatePicker>() != null
               || focused.FindAncestorOfType<Calendar>() != null;
    }

    private GanttEditableColumn GetCurrentColumn()
    {
        var index = ChapterGrid.CurrentColumn?.DisplayIndex ?? -1;
        return index switch
        {
            2 => GanttEditableColumn.StartDate,
            3 => GanttEditableColumn.EndDate,
            4 => GanttEditableColumn.Progress,
            _ => GanttEditableColumn.None
        };
    }

    private async Task CopyToClipboardAsync(string text)
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            await clipboard.SetTextAsync(text);
    }

    private async Task<string?> GetClipboardTextAsync()
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            return await clipboard.TryGetTextAsync();

        return null;
    }
}

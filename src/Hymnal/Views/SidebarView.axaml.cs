using System;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Hymnal.ViewModels;

namespace Hymnal.Views;

public partial class SidebarView : UserControl
{
    private ChapterViewModel? _dragSource;
    private string? _dragSourcePath;
    private PointerPressedEventArgs? _dragPressArgs;
    private Point _dragStart;
    private bool _dragOperationStarted;
    private Panel? _lastDropIndicator;

    public SidebarView()
    {
        InitializeComponent();
    }

    private void RemoveFromBook_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not WorkspaceViewModel vm) return;
        if (sender is MenuItem { DataContext: ChapterViewModel chapter })
            vm.RemoveFromBookCommand.Execute(chapter.Node.RelativePath)
                .Subscribe(Observer.Create<Unit>(_ => { }));
    }

    private void ExcludeFromBook_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not WorkspaceViewModel vm) return;
        if (sender is MenuItem { DataContext: ChapterViewModel chapter })
            vm.RemoveFromBookCommand.Execute(chapter.Node.RelativePath)
                .Subscribe(Observer.Create<Unit>(_ => { }));
    }

    // ── Drag-and-drop ─────────────────────────────────────────────────────────

    private void ChapterRow_Loaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Grid grid)
            return;

        if (DragDrop.GetAllowDrop(grid))
            return;

        DragDrop.SetAllowDrop(grid, true);
        DragDrop.AddDragOverHandler(grid, Row_DragOver);
        DragDrop.AddDragLeaveHandler(grid, Row_DragLeave);
        DragDrop.AddDropHandler(grid, Row_Drop);

        grid.AddHandler(PointerPressedEvent, Row_PointerPressed,
            RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: true);
        grid.AddHandler(PointerMovedEvent, Row_PointerMoved,
            RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: true);
        grid.AddHandler(PointerReleasedEvent, Row_PointerReleased,
            RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private void Row_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control || control.DataContext is not ChapterViewModel chapter)
            return;

        var point = e.GetCurrentPoint(control);

        if (point.Properties.IsRightButtonPressed)
        {
            e.Handled = true;
            if (control.ContextMenu is ContextMenu menu)
                menu.Open(control);
            return;
        }

        if (!point.Properties.IsLeftButtonPressed)
            return;

        // Only chapters are draggable, not parts
        if (chapter.Node.Kind != Hymnal.Core.Models.NodeKind.Chapter)
            return;

        _dragSource = chapter;
        _dragPressArgs = e;
        _dragStart = e.GetPosition(this);
        _dragOperationStarted = false;
        e.Pointer.Capture(control);
    }

    private async void Row_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragSource is null || _dragOperationStarted)
            return;

        var point = e.GetPosition(this);
        if (Math.Abs(point.X - _dragStart.X) < 4 && Math.Abs(point.Y - _dragStart.Y) < 4)
            return;

        _dragOperationStarted = true;
        _dragSourcePath = _dragSource.Node.RelativePath;
        e.Pointer.Capture(null);

        try
        {
            var data = new DataTransfer();
            data.Add(DataTransferItem.CreateText(_dragSource.Node.RelativePath));

            if (_dragPressArgs is not null)
                await DragDrop.DoDragDropAsync(_dragPressArgs, data, DragDropEffects.Move);
        }
        finally
        {
            ClearDropIndicator();
            ClearDragState();
        }
    }

    private void Row_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_dragOperationStarted)
            ClearDragState();
        e.Pointer.Capture(null);
    }

    private void Row_DragOver(object? sender, DragEventArgs e)
    {
        if (sender is not Grid grid || grid.DataContext is not ChapterViewModel target)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        if (string.IsNullOrWhiteSpace(_dragSourcePath) ||
            string.Equals(_dragSourcePath, target.Node.RelativePath, StringComparison.OrdinalIgnoreCase))
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;

        if (grid.Parent is not Panel panel)
            return;

        ClearDropIndicator();

        var dropBefore = ShouldDropBefore(target.Node.RelativePath);
        SetInsertionLine(panel, dropBefore);
        _lastDropIndicator = panel;
    }

    private void Row_DragLeave(object? sender, DragEventArgs e)
    {
        ClearDropIndicator();
    }

    private async void Row_Drop(object? sender, DragEventArgs e)
    {
        ClearDropIndicator();

        if (DataContext is not WorkspaceViewModel vm)
            return;

        if (sender is not Grid grid || grid.DataContext is not ChapterViewModel target)
            return;

        var draggedPath = _dragSourcePath;
        if (string.IsNullOrWhiteSpace(draggedPath) ||
            string.Equals(draggedPath, target.Node.RelativePath, StringComparison.OrdinalIgnoreCase))
            return;

        var visibleNodes = vm.VisibleNodes.Select(n => n.Node.RelativePath).ToList();
        var sourceIndex = visibleNodes.FindIndex(p => string.Equals(p, draggedPath, StringComparison.OrdinalIgnoreCase));
        var targetIndex = visibleNodes.FindIndex(p => string.Equals(p, target.Node.RelativePath, StringComparison.OrdinalIgnoreCase));
        if (sourceIndex < 0 || targetIndex < 0)
            return;

        var request = sourceIndex < targetIndex
            ? new ReorderCardRequest(draggedPath, AfterRelativePath: target.Node.RelativePath)
            : new ReorderCardRequest(draggedPath, BeforeRelativePath: target.Node.RelativePath);

        await ExecuteCommandAsync(vm.ReorderChapterCommand.Execute(request));
    }

    private static async Task ExecuteCommandAsync(IObservable<Unit> execution)
    {
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = execution.Subscribe(
            _ => { },
            ex => completion.TrySetException(ex),
            () => completion.TrySetResult(null));
        await completion.Task;
    }

    private bool ShouldDropBefore(string targetRelativePath)
    {
        if (DataContext is not WorkspaceViewModel vm || string.IsNullOrWhiteSpace(_dragSourcePath))
            return true;

        var paths = vm.VisibleNodes.Select(n => n.Node.RelativePath).ToList();
        var sourceIndex = paths.FindIndex(p => string.Equals(p, _dragSourcePath, StringComparison.OrdinalIgnoreCase));
        var targetIndex = paths.FindIndex(p => string.Equals(p, targetRelativePath, StringComparison.OrdinalIgnoreCase));
        return sourceIndex > targetIndex;
    }

    private void ClearDropIndicator()
    {
        if (_lastDropIndicator != null)
        {
            SetInsertionLine(_lastDropIndicator, null);
            _lastDropIndicator = null;
        }
    }

    private static void SetInsertionLine(Panel panel, bool? dropBefore)
    {
        foreach (var child in panel.Children)
        {
            if (child is not Border border)
                continue;
            if (border.Name == "InsertBefore")
                border.IsVisible = dropBefore == true;
            else if (border.Name == "InsertAfter")
                border.IsVisible = dropBefore == false;
        }
    }

    private void ClearDragState()
    {
        _dragSource = null;
        _dragSourcePath = null;
        _dragPressArgs = null;
        _dragOperationStarted = false;
    }
}

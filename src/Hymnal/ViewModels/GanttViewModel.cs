using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;
using Hymnal.Core.Services;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace Hymnal.ViewModels;

/// <summary>
/// Read-only projection of the current workspace's chapter list into Gantt rows.
/// Observes <see cref="WorkspaceViewModel.Nodes"/> and rebuilds the row list whenever
/// chapters are added, removed, or their phase metadata changes.
///
/// PhaseData is already loaded onto each <see cref="ChapterViewModel"/> by the time
/// Nodes is populated; <see cref="GanttProjection"/> is used for the actual mapping.
/// </summary>
public sealed class GanttViewModel : ViewModelBase
{
    private readonly WorkspaceViewModel _workspace;
    // PhaseDataService retained for future refresh/filter operations.
    private readonly PhaseDataService _phaseDataService;
    private readonly INotificationService _notificationService;

    private readonly ObservableCollection<GanttRowViewModel> _rows = new();
    private readonly SerialDisposable _phaseSubscriptions = new();

    // Subject that emits the chapter row the user wants to edit.
    private readonly Subject<GanttRowViewModel> _rowEditRequested = new();

    /// <summary>Projected Gantt rows in manuscript order (Parts + Chapters).</summary>
    public ReadOnlyObservableCollection<GanttRowViewModel> Rows { get; }

    /// <summary>
    /// Emits a <see cref="GanttRowViewModel"/> whenever the user triggers
    /// <see cref="GanttRowViewModel.EditDatesCommand"/> on a chapter row.
    /// Only chapter rows (not Part rows) are forwarded.
    /// Subscribers should open a date-picker popup bound to the emitted row.
    /// </summary>
    public IObservable<GanttRowViewModel> RowEditRequested => _rowEditRequested;

    // ── Inline date-editing state ─────────────────────────────────────────────

    private GanttRowViewModel? _editingRow;
    /// <summary>The chapter row currently being edited, or null when the overlay is closed.</summary>
    public GanttRowViewModel? EditingRow
    {
        get => _editingRow;
        private set => this.RaiseAndSetIfChanged(ref _editingRow, value);
    }

    private bool _isEditingDates;
    /// <summary>True while the inline date-edit overlay is open.</summary>
    public bool IsEditingDates
    {
        get => _isEditingDates;
        private set => this.RaiseAndSetIfChanged(ref _isEditingDates, value);
    }

    private DateTime? _editStartDate;
    /// <summary>
    /// Start date staged for editing. Settable so CalendarDatePicker can write back.
    /// Null means the user has cleared the field.
    /// </summary>
    public DateTime? EditStartDate
    {
        get => _editStartDate;
        set => this.RaiseAndSetIfChanged(ref _editStartDate, value);
    }

    private DateTime? _editEndDate;
    /// <summary>
    /// End date staged for editing. Settable so CalendarDatePicker can write back.
    /// Null means the user has cleared the field.
    /// </summary>
    public DateTime? EditEndDate
    {
        get => _editEndDate;
        set => this.RaiseAndSetIfChanged(ref _editEndDate, value);
    }

    // ── Edit commands ─────────────────────────────────────────────────────────

    /// <summary>
    /// Persists <see cref="EditStartDate"/> and <see cref="EditEndDate"/> to the
    /// underlying <see cref="ChapterViewModel"/> and closes the overlay.
    /// Errors are surfaced via <see cref="INotificationService"/>.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CommitEditCommand { get; }

    /// <summary>Closes the date-edit overlay without saving.</summary>
    public ReactiveCommand<Unit, Unit> CancelEditCommand { get; }

    // ── Constructor ───────────────────────────────────────────────────────────

    public GanttViewModel(
        WorkspaceViewModel workspace,
        PhaseDataService phaseDataService,
        INotificationService notificationService)
    {
        _workspace           = workspace;
        _phaseDataService    = phaseDataService;
        _notificationService = notificationService;

        Rows = new ReadOnlyObservableCollection<GanttRowViewModel>(_rows);

        // ReadOnlyObservableCollection implements INotifyCollectionChanged explicitly —
        // cast to subscribe to collection changes.
        var nodesAsNotify = (INotifyCollectionChanged)workspace.Nodes;

        NotifyCollectionChangedEventHandler handler = (_, _) => RebuildRows();
        nodesAsNotify.CollectionChanged += handler;
        Disposables.Add(Disposable.Create(() => nodesAsNotify.CollectionChanged -= handler));
        Disposables.Add(_phaseSubscriptions);
        Disposables.Add(Disposable.Create(() => _rowEditRequested.OnCompleted()));

        // ── Editing commands ───────────────────────────────────────────────────

        // Subscribe to RowEditRequested to populate the inline edit overlay.
        // Called on the same thread as EditDatesCommand.Execute() (UI thread in app,
        // test thread in unit tests) — no dispatch needed.
        Disposables.Add(
            _rowEditRequested.Subscribe(OpenEditForRow));

        CommitEditCommand = ReactiveCommand.CreateFromTask(CommitEditAsync);
        Disposables.Add(
            CommitEditCommand.ThrownExceptions
                .Subscribe(ex => _notificationService.ShowError(ex.Message)));

        CancelEditCommand = ReactiveCommand.Create(CancelEdit);
        Disposables.Add(
            CancelEditCommand.ThrownExceptions
                .Subscribe(ex => _notificationService.ShowError(ex.Message)));

        // Build initial rows immediately.
        RebuildRows();
    }

    // ── Editing helpers ───────────────────────────────────────────────────────

    private void OpenEditForRow(GanttRowViewModel row)
    {
        EditingRow     = row;
        EditStartDate  = row.StartDate.HasValue
            ? row.StartDate.Value.ToDateTime(TimeOnly.MinValue)
            : null;
        EditEndDate    = row.EndDate.HasValue
            ? row.EndDate.Value.ToDateTime(TimeOnly.MinValue)
            : null;
        IsEditingDates = true;
    }

    private void CancelEdit()
    {
        IsEditingDates = false;
        EditingRow     = null;
        EditStartDate  = null;
        EditEndDate    = null;
    }

    private async Task CommitEditAsync()
    {
        var row = EditingRow;
        if (row == null) return;

        // Find the live ChapterViewModel by forward-slash-normalized relative path.
        var chapterVm = _workspace.Nodes
            .FirstOrDefault(n => n.Node.RelativePath == row.RelativePath);

        if (chapterVm == null)
        {
            _notificationService.ShowError(
                $"Could not find chapter '{row.Title}' to save dates.");
            return;
        }

        var startStr = EditStartDate.HasValue
            ? DateOnly.FromDateTime(EditStartDate.Value).ToString("yyyy-MM-dd")
            : null;
        var endStr = EditEndDate.HasValue
            ? DateOnly.FromDateTime(EditEndDate.Value).ToString("yyyy-MM-dd")
            : null;

        await chapterVm.UpdateDatesAsync(startStr, endStr).ConfigureAwait(false);

        // Clear editing state on the UI thread (mirrors ApplyPhaseData pattern).
        if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            CancelEdit();
        else
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(CancelEdit);
    }

    // ── Projection ────────────────────────────────────────────────────────────

    /// <summary>
    /// Rebuilds the <see cref="Rows"/> collection from the current workspace nodes.
    /// Part rows are rendered as rollup summaries whose dates and completion percentage
    /// are derived from the child Chapter rows that follow them.
    /// Must be called on the UI thread.
    /// </summary>
    private void RebuildRows()
    {
        _rows.Clear();

        var phaseSubscriptions = new CompositeDisposable();
        var nodes = _workspace.Nodes.ToList();

        for (int i = 0; i < nodes.Count; i++)
        {
            var vm = nodes[i];

            PropertyChangedEventHandler handler = (_, e) =>
            {
                if (e.PropertyName == nameof(ChapterViewModel.PhaseData))
                    RebuildRows();
            };
            vm.PropertyChanged += handler;
            phaseSubscriptions.Add(Disposable.Create(() => vm.PropertyChanged -= handler));

            GanttRowData rowData;
            if (vm.Node.Kind == NodeKind.Part)
            {
                // Collect child chapters: nodes after this Part until the next Part (or end).
                var children = new List<ChapterViewModel>();
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    if (nodes[j].Node.Kind == NodeKind.Part) break;
                    children.Add(nodes[j]);
                }
                rowData = BuildPartRollup(vm.Node, children);
            }
            else
            {
                rowData = GanttProjection.Project(vm.Node, vm.PhaseData);
            }

            var rowVm = new GanttRowViewModel(rowData);

            // Wire EditDatesCommand: only chapter rows route to RowEditRequested.
            if (rowVm.IsChapter)
            {
                var captured = rowVm;
                phaseSubscriptions.Add(
                    rowVm.EditDatesCommand
                         .Subscribe(_ => _rowEditRequested.OnNext(captured)));
            }

            _rows.Add(rowVm);
        }

        _phaseSubscriptions.Disposable = phaseSubscriptions;
    }

    /// <summary>
    /// Aggregates child chapter data into a rollup <see cref="GanttRowData"/> for a Part node.
    /// StartDate = min of child StartDates; EndDate = max of child EndDates.
    /// CompletionPercentage = fraction of children with <see cref="ChapterStatus.Done"/>.
    /// IsMissingDates is true when no child has both a start and end date.
    /// </summary>
    private static GanttRowData BuildPartRollup(ChapterNode partNode, IReadOnlyList<ChapterViewModel> children)
    {
        DateOnly? minStart = null;
        DateOnly? maxEnd   = null;
        int done  = 0;
        int total = children.Count;

        foreach (var child in children)
        {
            var childRow = GanttProjection.Project(child.Node, child.PhaseData);

            if (childRow.StartDate.HasValue &&
                (minStart is null || childRow.StartDate.Value < minStart.Value))
                minStart = childRow.StartDate;

            if (childRow.EndDate.HasValue &&
                (maxEnd is null || childRow.EndDate.Value > maxEnd.Value))
                maxEnd = childRow.EndDate;

            if (childRow.Status == ChapterStatus.Done)
                done++;
        }

        double completion = total > 0 ? (double)done / total : 0.0;
        bool isMissing    = minStart is null || maxEnd is null;

        // Status is not meaningful for a Part rollup row; use Outlining as a neutral default.
        return new GanttRowData(
            RelativePath:        partNode.RelativePath,
            Title:               partNode.Title,
            Kind:                partNode.Kind,
            Status:              ChapterStatus.Outlining,
            StartDate:           minStart,
            EndDate:             maxEnd,
            IsMissingDates:      isMissing,
            CompletionPercentage: completion);
    }
}

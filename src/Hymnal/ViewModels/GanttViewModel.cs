using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
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

    private readonly ObservableCollection<GanttRowViewModel> _rows = new();
    private readonly SerialDisposable _phaseSubscriptions = new();

    /// <summary>Projected Gantt rows in manuscript order (Parts + Chapters).</summary>
    public ReadOnlyObservableCollection<GanttRowViewModel> Rows { get; }

    // ── Constructor ───────────────────────────────────────────────────────────

    public GanttViewModel(WorkspaceViewModel workspace, PhaseDataService phaseDataService)
    {
        _workspace        = workspace;
        _phaseDataService = phaseDataService;

        Rows = new ReadOnlyObservableCollection<GanttRowViewModel>(_rows);

        // ReadOnlyObservableCollection implements INotifyCollectionChanged explicitly —
        // cast to subscribe to collection changes.
        var nodesAsNotify = (INotifyCollectionChanged)workspace.Nodes;

        NotifyCollectionChangedEventHandler handler = (_, _) => RebuildRows();
        nodesAsNotify.CollectionChanged += handler;
        Disposables.Add(Disposable.Create(() => nodesAsNotify.CollectionChanged -= handler));
        Disposables.Add(_phaseSubscriptions);

        // Build initial rows immediately.
        RebuildRows();
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

            _rows.Add(new GanttRowViewModel(rowData));
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

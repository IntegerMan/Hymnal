using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Hymnal.Core.Services;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace Hymnal.ViewModels;

/// <summary>
/// Read-only projection of the current workspace's chapter list into Gantt rows.
/// Observes <see cref="WorkspaceViewModel.Nodes"/> and rebuilds the row list whenever
/// chapters are added, removed, or the workspace changes.
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

        // Build initial rows immediately.
        RebuildRows();
    }

    // ── Projection ────────────────────────────────────────────────────────────

    /// <summary>
    /// Rebuilds the <see cref="Rows"/> collection from the current workspace nodes.
    /// Must be called on the UI thread.
    /// </summary>
    private void RebuildRows()
    {
        _rows.Clear();

        foreach (var chapterVm in _workspace.Nodes)
        {
            var rowData = GanttProjection.Project(chapterVm.Node, chapterVm.PhaseData);
            _rows.Add(new GanttRowViewModel(rowData));
        }
    }
}

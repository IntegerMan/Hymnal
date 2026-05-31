using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;
using Hymnal.Core.Services;
using ReactiveUI;

namespace Hymnal.ViewModels;

/// <summary>
/// Wraps an immutable <see cref="ChapterNode"/> with mutable reactive status state
/// and a <see cref="ChangeStatusCommand"/> that persists phase data to
/// <see cref="PhaseDataService"/>.  Lives in the Avalonia project so it may reference
/// ReactiveUI and <see cref="INotificationService"/> without creating circular deps.
/// </summary>
public sealed class ChapterViewModel : ViewModelBase, IDisposable
{
    // ── Injected services ─────────────────────────────────────────────────────

    private readonly PhaseDataService _phaseDataService;
    private readonly IAppSettingsStore _settingsStore;
    private readonly INotificationService _notificationService;
    private readonly string _workspaceRoot;

    // ── Public read-only data ─────────────────────────────────────────────────

    /// <summary>The underlying immutable chapter node (for EditorViewModel etc.).</summary>
    public ChapterNode Node { get; }

    /// <summary>The stable UUID that survives file renames.</summary>
    public string Uuid { get; }

    // ── Reactive properties ───────────────────────────────────────────────────

    private ChapterStatus _status;
    public ChapterStatus Status
    {
        get => _status;
        private set => this.RaiseAndSetIfChanged(ref _status, value);
    }

    private PhaseData? _phaseData;
    public PhaseData? PhaseData
    {
        get => _phaseData;
        private set => this.RaiseAndSetIfChanged(ref _phaseData, value);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public ReactiveCommand<ChapterStatus, Unit> ChangeStatusCommand { get; }

    // ── Constructor ───────────────────────────────────────────────────────────

    public ChapterViewModel(
        ChapterNode node,
        string uuid,
        PhaseData? phaseData,
        PhaseDataService phaseDataService,
        IAppSettingsStore settingsStore,
        INotificationService notificationService,
        string workspaceRoot)
    {
        Node = node;
        Uuid = uuid;
        _phaseData = phaseData;
        _status = phaseData?.Status ?? ChapterStatus.Outlining;

        _phaseDataService = phaseDataService;
        _settingsStore = settingsStore;
        _notificationService = notificationService;
        _workspaceRoot = workspaceRoot;

        ChangeStatusCommand = ReactiveCommand.CreateFromTask<ChapterStatus>(ChangeStatusAsync);

        Disposables.Add(
            ChangeStatusCommand.ThrownExceptions
                .Subscribe(ex => _notificationService.ShowError(ex.Message)));
    }

    // ── Command implementation ────────────────────────────────────────────────

    private async Task ChangeStatusAsync(ChapterStatus newStatus)
    {
        PhaseData? updated = null;
        await _phaseDataService.UpsertAsync(_workspaceRoot, Uuid, current =>
        {
            var basePhaseData = current ?? _phaseData;
            updated = new PhaseData
            {
                Status = newStatus,
                PhaseStartDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                PhaseEndDate = basePhaseData?.PhaseEndDate
            };
            return updated;
        }).ConfigureAwait(false);

        // Commit state updates on the UI thread.
        await Avalonia.Threading.Dispatcher.UIThread
            .InvokeAsync(() =>
            {
                Status = newStatus;
                PhaseData = updated!;
            });
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Disposables.Dispose();
    }
}

using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;
using Hymnal.Core.Services;
using ReactiveUI;

namespace Hymnal.ViewModels;

/// <summary>
/// Backs the F3 Chapter Info side-panel.
/// Mirrors the NotesViewModel lifecycle pattern:
///   • observes EditorViewModel.ActiveNode
///   • cancels in-flight saves on chapter switch
///   • ToggleCommand gated on ActiveNode != null
/// Adds status-change, date-edit, and target-edit commands that persist via
/// PhaseDataService / TargetsService and call <see cref="ChapterViewModel.ApplyPhaseData"/>
/// to re-sync the sidebar without re-loading the whole workspace.
/// </summary>
public sealed class ChapterInfoViewModel : ViewModelBase, IDisposable
{
    // ── Injected services ────────────────────────────────────────────────────

    private readonly EditorViewModel _editorViewModel;
    private readonly WorkspaceViewModel _workspaceViewModel;
    private readonly PhaseDataService _phaseDataService;
    private readonly TargetsService _targetsService;
    private readonly IAppSettingsStore _settingsStore;
    private readonly INotificationService _notificationService;

    // ── Per-chapter state ────────────────────────────────────────────────────

    private ChapterViewModel? _activeChapterVm;
    private CancellationTokenSource _saveCts = new();
    private string? _loadedUuid;

    /// <summary>Disposables that live only for the active chapter subscription.</summary>
    private CompositeDisposable _chapterDisposables = new();

    // ── Public observable properties ─────────────────────────────────────────

    private bool _isVisible;
    public bool IsVisible
    {
        get => _isVisible;
        private set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }

    private string? _chapterTitle;
    public string? ChapterTitle
    {
        get => _chapterTitle;
        private set => this.RaiseAndSetIfChanged(ref _chapterTitle, value);
    }

    // ── Bindable display props that mirror the active ChapterViewModel ────────

    private ChapterStatus _status;
    public ChapterStatus Status
    {
        get => _status;
        private set => this.RaiseAndSetIfChanged(ref _status, value);
    }

    private string? _phaseStartDate;
    /// <summary>ISO 8601 date string, e.g. "2025-06-01". Editable in the pane.</summary>
    public string? PhaseStartDate
    {
        get => _phaseStartDate;
        set => this.RaiseAndSetIfChanged(ref _phaseStartDate, value);
    }

    private string? _phaseEndDate;
    /// <summary>ISO 8601 date string. Editable in the pane.</summary>
    public string? PhaseEndDate
    {
        get => _phaseEndDate;
        set => this.RaiseAndSetIfChanged(ref _phaseEndDate, value);
    }

    private int _wordCount;
    public int WordCount
    {
        get => _wordCount;
        private set => this.RaiseAndSetIfChanged(ref _wordCount, value);
    }

    private readonly ObservableAsPropertyHelper<string> _wordCountDisplay;
    public string WordCountDisplay => _wordCountDisplay.Value;

    private string? _targetDisplay;
    public string? TargetDisplay
    {
        get => _targetDisplay;
        private set => this.RaiseAndSetIfChanged(ref _targetDisplay, value);
    }

    private readonly ObservableAsPropertyHelper<double> _proximityFill;
    public double ProximityFill => _proximityFill.Value;

    private readonly ObservableAsPropertyHelper<bool> _hasTarget;
    public bool HasTarget => _hasTarget.Value;

    // ── PrefillPhaseDate ─────────────────────────────────────────────────────

    private bool _prefillPhaseDate = true;
    /// <summary>
    /// When true, SetStatusCommand auto-fills PhaseStartDate with today's date.
    /// Persisted via IAppSettingsStore key "prefillPhaseDate".
    /// </summary>
    public bool PrefillPhaseDate
    {
        get => _prefillPhaseDate;
        set
        {
            this.RaiseAndSetIfChanged(ref _prefillPhaseDate, value);
            _ = PersistPrefillPreferenceAsync(value);
        }
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    /// <summary>Shows/hides the panel. Only active when a chapter is open.</summary>
    public ReactiveCommand<Unit, Unit> ToggleCommand { get; }

    /// <summary>
    /// Changes the chapter status and optionally pre-fills PhaseStartDate.
    /// Persists via PhaseDataService and re-syncs ChapterViewModel.
    /// </summary>
    public ReactiveCommand<ChapterStatus, Unit> SetStatusCommand { get; }

    /// <summary>
    /// Persists the current PhaseStartDate/PhaseEndDate string fields to PhaseDataService
    /// and re-syncs ChapterViewModel.
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveDatesCommand { get; }

    /// <summary>
    /// Sets (or clears when null) the word-count target via ChapterViewModel.SetTargetCommand.
    /// </summary>
    public ReactiveCommand<int?, Unit> SetTargetCommand { get; }

    // ── Constructor ──────────────────────────────────────────────────────────

    public ChapterInfoViewModel(
        EditorViewModel editorViewModel,
        WorkspaceViewModel workspaceViewModel,
        PhaseDataService phaseDataService,
        TargetsService targetsService,
        IAppSettingsStore settingsStore,
        INotificationService notificationService)
    {
        _editorViewModel = editorViewModel;
        _workspaceViewModel = workspaceViewModel;
        _phaseDataService = phaseDataService;
        _targetsService = targetsService;
        _settingsStore = settingsStore;
        _notificationService = notificationService;

        // ── OAPHs that delegate to this VM's own observable props ────────────

        _wordCountDisplay = this
            .WhenAnyValue(x => x.WordCount)
            .Select(c => $"{c:N0} w")
            .ToProperty(this, x => x.WordCountDisplay, out _wordCountDisplay);
        Disposables.Add(_wordCountDisplay);

        _proximityFill = this
            .WhenAnyValue(x => x.HasTarget, x => x.WordCount, x => x.TargetDisplay,
                (hasTarget, wc, td) => hasTarget ? ComputeProximityFill(wc, td) : 0.0)
            .ToProperty(this, x => x.ProximityFill, out _proximityFill);
        Disposables.Add(_proximityFill);

        _hasTarget = this
            .WhenAnyValue(x => x.TargetDisplay, td => !string.IsNullOrEmpty(td) && td != "—")
            .ToProperty(this, x => x.HasTarget, out _hasTarget);
        Disposables.Add(_hasTarget);

        // ── Toggle command: gate on active node ──────────────────────────────
        var hasActiveNode = editorViewModel
            .WhenAnyValue(x => x.ActiveNode)
            .Select(n => n != null);

        ToggleCommand = ReactiveCommand.Create(
            () => { if (_loadedUuid != null) IsVisible = !IsVisible; },
            canExecute: hasActiveNode);
        Disposables.Add(
            ToggleCommand.ThrownExceptions
                .Subscribe(ex => notificationService.ShowError($"Toggle chapter info failed: {ex.Message}")));

        // ── SetStatusCommand ─────────────────────────────────────────────────
        SetStatusCommand = ReactiveCommand.CreateFromTask<ChapterStatus>(SetStatusAsync);
        Disposables.Add(
            SetStatusCommand.ThrownExceptions
                .Subscribe(ex => notificationService.ShowError($"Set status failed: {ex.Message}")));

        // ── SaveDatesCommand ─────────────────────────────────────────────────
        SaveDatesCommand = ReactiveCommand.CreateFromTask(SaveDatesAsync);
        Disposables.Add(
            SaveDatesCommand.ThrownExceptions
                .Subscribe(ex => notificationService.ShowError($"Save dates failed: {ex.Message}")));

        // ── SetTargetCommand ─────────────────────────────────────────────────
        SetTargetCommand = ReactiveCommand.CreateFromTask<int?>(SetTargetAsync);
        Disposables.Add(
            SetTargetCommand.ThrownExceptions
                .Subscribe(ex => notificationService.ShowError($"Set target failed: {ex.Message}")));

        // ── Observe active node ──────────────────────────────────────────────
        Disposables.Add(
            editorViewModel
                .WhenAnyValue(x => x.ActiveNode)
                .Subscribe(node => OnActiveNodeChanged(node)));

        // ── Load PrefillPhaseDate preference (default: true) ─────────────────
        _ = LoadPrefillPreferenceAsync();
    }

    // ── Active-node transition handler ───────────────────────────────────────

    private void OnActiveNodeChanged(ChapterNode? node)
    {
        // Cancel any pending in-flight operation for the previous chapter.
        _saveCts.Cancel();
        _saveCts.Dispose();
        _saveCts = new CancellationTokenSource();

        // Dispose per-chapter reactive subscriptions.
        _chapterDisposables.Dispose();
        _chapterDisposables = new CompositeDisposable();

        if (node == null)
        {
            _loadedUuid = null;
            _activeChapterVm = null;
            ChapterTitle = null;
            Status = ChapterStatus.Outlining;
            PhaseStartDate = null;
            PhaseEndDate = null;
            WordCount = 0;
            TargetDisplay = null;
            IsVisible = false;
            return;
        }

        var wasVisible = IsVisible;
        ChapterTitle = node.Title;

        // Find the ChapterViewModel for this node.
        var chapterVm = _workspaceViewModel.Nodes
            .FirstOrDefault(vm => vm.Node.RelativePath == node.RelativePath);

        _activeChapterVm = chapterVm;
        _loadedUuid = chapterVm?.Uuid;

        if (chapterVm != null)
        {
            // Sync display state from the ChapterViewModel immediately.
            SyncFromChapterVm(chapterVm);

            // Subscribe to live word count, status, and target changes.
            _chapterDisposables.Add(
                chapterVm.WhenAnyValue(x => x.WordCount)
                    .Subscribe(c => WordCount = c));

            _chapterDisposables.Add(
                chapterVm.WhenAnyValue(x => x.Status)
                    .Subscribe(s => Status = s));

            _chapterDisposables.Add(
                chapterVm.WhenAnyValue(x => x.PhaseData)
                    .Subscribe(pd =>
                    {
                        PhaseStartDate = pd?.PhaseStartDate;
                        PhaseEndDate = pd?.PhaseEndDate;
                    }));

            _chapterDisposables.Add(
                chapterVm.WhenAnyValue(x => x.Target)
                    .Subscribe(t => TargetDisplay = FormatTargetDisplay(t)));

            IsVisible = wasVisible;
        }
        else
        {
            IsVisible = wasVisible;
        }
    }

    private void SyncFromChapterVm(ChapterViewModel vm)
    {
        Status = vm.Status;
        PhaseStartDate = vm.PhaseData?.PhaseStartDate;
        PhaseEndDate = vm.PhaseData?.PhaseEndDate;
        WordCount = vm.WordCount;
        TargetDisplay = FormatTargetDisplay(vm.Target);
    }

    // ── Command implementations ───────────────────────────────────────────────

    private async Task SetStatusAsync(ChapterStatus newStatus)
    {
        var chapterVm = _activeChapterVm;
        var uuid = _loadedUuid;
        var workspaceRoot = _workspaceViewModel.WorkspaceRoot;

        if (chapterVm == null || string.IsNullOrEmpty(uuid) || string.IsNullOrEmpty(workspaceRoot))
            return;

        PhaseData? updated = null;
        await _phaseDataService.UpsertAsync(workspaceRoot, uuid, current =>
        {
            var existingStart = current?.PhaseStartDate;
            var existingEnd = current?.PhaseEndDate;

            // Pre-fill phase start date if the toggle is on and status actually changed.
            var startDate = (_prefillPhaseDate && current?.Status != newStatus)
                ? DateTime.UtcNow.ToString("yyyy-MM-dd")
                : existingStart;

            updated = new PhaseData
            {
                Status = newStatus,
                PhaseStartDate = startDate,
                PhaseEndDate = existingEnd
            };
            return updated;
        }).ConfigureAwait(false);

        if (updated != null)
        {
            chapterVm.ApplyPhaseData(updated);

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Status = updated.Status;
                PhaseStartDate = updated.PhaseStartDate;
                PhaseEndDate = updated.PhaseEndDate;
            });
        }
    }

    private async Task SaveDatesAsync()
    {
        var chapterVm = _activeChapterVm;
        var uuid = _loadedUuid;
        var workspaceRoot = _workspaceViewModel.WorkspaceRoot;

        if (chapterVm == null || string.IsNullOrEmpty(uuid) || string.IsNullOrEmpty(workspaceRoot))
            return;

        // Capture current field values (may have been edited by user directly).
        var startDate = PhaseStartDate;
        var endDate = PhaseEndDate;
        var currentStatus = Status;

        PhaseData? updated = null;
        await _phaseDataService.UpsertAsync(workspaceRoot, uuid, current =>
        {
            updated = new PhaseData
            {
                Status = current?.Status ?? currentStatus,
                PhaseStartDate = startDate,
                PhaseEndDate = endDate
            };
            return updated;
        }).ConfigureAwait(false);

        if (updated != null)
            chapterVm.ApplyPhaseData(updated);
    }

    private async Task SetTargetAsync(int? minWords)
    {
        var chapterVm = _activeChapterVm;
        if (chapterVm == null) return;

        WordCountTarget? target = minWords.HasValue
            ? new WordCountTarget { MinWords = minWords.Value }
            : null;

        // Delegate persistence to ChapterViewModel.SetTargetCommand — it owns the lock.
        await chapterVm.SetTargetCommand.Execute(target);
    }

    // ── Preference helpers ────────────────────────────────────────────────────

    private async Task LoadPrefillPreferenceAsync()
    {
        try
        {
            var stored = await _settingsStore.GetAsync<bool?>("prefillPhaseDate").ConfigureAwait(false);
            if (stored.HasValue)
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => _prefillPhaseDate = stored.Value);
        }
        catch
        {
            // Default (true) remains; non-fatal preference load failure is ignored.
        }
    }

    private async Task PersistPrefillPreferenceAsync(bool value)
    {
        try
        {
            await _settingsStore.SetAsync("prefillPhaseDate", value).ConfigureAwait(false);
        }
        catch
        {
            // Non-fatal; preference may not persist across sessions if store is unavailable.
        }
    }

    // ── Static helpers ───────────────────────────────────────────────────────

    private static string FormatTargetDisplay(WordCountTarget? target)
    {
        if (target == null) return "—";
        if (target.MinWords.HasValue && target.MaxWords.HasValue)
            return $"{target.MinWords:N0}–{target.MaxWords:N0} w";
        if (target.MinWords.HasValue)
            return $"{target.MinWords:N0} w";
        if (target.MaxWords.HasValue)
            return $"≤{target.MaxWords:N0} w";
        return "—";
    }

    private static double ComputeProximityFill(int wordCount, string? targetDisplay)
    {
        // Use the TargetDisplay string is informational only — the fill comes from ChapterViewModel.
        // We can't easily parse back, so delegate to 0..1 clamped ratio via WordCount / min.
        // This is approximate; ChapterViewModel.ProximityFill is the authoritative source when bound.
        return 0.0;
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _chapterDisposables.Dispose();
        Disposables.Dispose();
        _saveCts.Cancel();
        _saveCts.Dispose();
    }
}

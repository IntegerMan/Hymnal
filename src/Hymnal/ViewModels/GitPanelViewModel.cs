using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Hymnal.Core.Common;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;
using Hymnal.Core.Services;
using ReactiveUI;
using ReactiveUI.Avalonia;
using Unit = System.Reactive.Unit;

namespace Hymnal.ViewModels;

/// <summary>
/// Git toolbar state for the workspace header. Keeps the sync summary fresh,
/// hides itself when Git is unavailable or the workspace is not a repository, and debounces
/// workspace/save/watch refresh signals so file saves and git metadata writes do not spam status.
/// </summary>
public sealed class GitPanelViewModel : ViewModelBase, IDisposable
{
    private const int RefreshDebounceMilliseconds = 250;
    private const int RemoteFetchIntervalSeconds = 60;

    private readonly WorkspaceViewModel _workspaceViewModel;
    private readonly EditorViewModel _editorViewModel;
    private readonly IGitService _gitService;
    private readonly INotificationService _notificationService;
    private readonly Subject<Unit> _refreshRequests = new();
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private FileSystemWatcher? _watcher;
    private string? _watchedRoot;
    private int _busyDepth;
    private bool _disposed;
    private bool _conflictNotified;
    private bool _isPulling;
    private DateTimeOffset? _lastRemoteFetchUtc;
    private string? _transientSummary;
    private DateTimeOffset? _transientSummaryUntil;

    private bool _isVisible;
    public bool IsVisible
    {
        get => _isVisible;
        private set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }

    private string? _branchName;
    public string? BranchName
    {
        get => _branchName;
        private set => this.RaiseAndSetIfChanged(ref _branchName, value);
    }

    private int _uncommittedChangeCount;
    public int UncommittedChangeCount
    {
        get => _uncommittedChangeCount;
        private set => this.RaiseAndSetIfChanged(ref _uncommittedChangeCount, value);
    }

    private int _behindRemoteCount;
    public int BehindRemoteCount
    {
        get => _behindRemoteCount;
        private set => this.RaiseAndSetIfChanged(ref _behindRemoteCount, value);
    }

    private int _aheadRemoteCount;
    public int AheadRemoteCount
    {
        get => _aheadRemoteCount;
        private set => this.RaiseAndSetIfChanged(ref _aheadRemoteCount, value);
    }

    private bool _hasMergeConflict;
    public bool HasMergeConflict
    {
        get => _hasMergeConflict;
        private set => this.RaiseAndSetIfChanged(ref _hasMergeConflict, value);
    }

    private string _changeSummaryText = string.Empty;
    public string ChangeSummaryText
    {
        get => _changeSummaryText;
        private set => this.RaiseAndSetIfChanged(ref _changeSummaryText, value);
    }

    private string _statusText = string.Empty;
    public string StatusText
    {
        get => _statusText;
        private set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    private IReadOnlyList<GitChangedFile> _changedFiles = Array.Empty<GitChangedFile>();
    public IReadOnlyList<GitChangedFile> ChangedFiles
    {
        get => _changedFiles;
        private set => this.RaiseAndSetIfChanged(ref _changedFiles, value);
    }

    private IReadOnlyList<GitChangeTreeNode> _changedFileTree = Array.Empty<GitChangeTreeNode>();
    public IReadOnlyList<GitChangeTreeNode> ChangedFileTree
    {
        get => _changedFileTree;
        private set => this.RaiseAndSetIfChanged(ref _changedFileTree, value);
    }

    private bool _canSync;
    public bool CanSync
    {
        get => _canSync;
        private set => this.RaiseAndSetIfChanged(ref _canSync, value);
    }

    public bool IsFullySynced =>
        UncommittedChangeCount == 0
        && BehindRemoteCount == 0
        && AheadRemoteCount == 0
        && !HasMergeConflict;

    private string _primaryActionText = "Sync";
    public string PrimaryActionText
    {
        get => _primaryActionText;
        private set => this.RaiseAndSetIfChanged(ref _primaryActionText, value);
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    private string? _lastError;
    public string? LastError
    {
        get => _lastError;
        private set => this.RaiseAndSetIfChanged(ref _lastError, value);
    }

    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    public ReactiveCommand<string?, Unit> SyncCommand { get; }

    public GitPanelViewModel(
        WorkspaceViewModel workspaceViewModel,
        EditorViewModel editorViewModel,
        IGitService gitService,
        INotificationService notificationService)
    {
        _workspaceViewModel = workspaceViewModel;
        _editorViewModel = editorViewModel;
        _gitService = gitService;
        _notificationService = notificationService;

        var canMutate = this.WhenAnyValue(x => x.CanSync);
        var canRefresh = this.WhenAnyValue(x => x.IsBusy, busy => !busy);

        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync, canRefresh);
        SyncCommand = ReactiveCommand.CreateFromTask<string?>(SyncAsync, canMutate);

        Disposables.Add(RefreshCommand.ThrownExceptions.Subscribe(Observer.Create<Exception>(ex => _notificationService.ShowError($"Failed to refresh Git status: {ex.Message}"))));
        Disposables.Add(SyncCommand.ThrownExceptions.Subscribe(Observer.Create<Exception>(ex => _notificationService.ShowError($"Failed to sync changes: {ex.Message}"))));

        Disposables.Add(
            _workspaceViewModel.WorkspaceChanged
                .Subscribe(_ => HandleWorkspaceChanged()));

        Disposables.Add(
            _editorViewModel.Saved
                .Subscribe(_ => QueueRefresh()));

        Disposables.Add(
            _refreshRequests
                .Throttle(TimeSpan.FromMilliseconds(RefreshDebounceMilliseconds), TaskPoolScheduler.Default)
                .SelectMany(_ => Observable.FromAsync(RefreshAsync))
                .ObserveOn(AvaloniaScheduler.Instance)
                .Subscribe(_ => { }, ex => _notificationService.ShowError($"Failed to refresh Git status: {ex.Message}")));

        Disposables.Add(Disposable.Create(() =>
        {
            DisposeWatcher();
            _refreshRequests.OnCompleted();
            _refreshRequests.Dispose();
            _refreshLock.Dispose();
        }));
    }

    public string CreateDefaultCommitMessage()
        => $"Hymnal: save progress {DateTime.UtcNow:O}";

    public bool ShouldOpenSyncDialog()
        => UncommittedChangeCount > 0;

    public async Task SyncAsync(string? commitMessage)
    {
        if (HasMergeConflict || IsBusy || !IsVisible)
            return;

        var workspaceRoot = _workspaceViewModel.WorkspaceRoot;
        if (UncommittedChangeCount > 0)
        {
            await ExecuteGitMutationAsync(
                () => _gitService.StageAllCommitPullAndPushAsync(workspaceRoot, NormalizeCommitMessage(commitMessage)),
                "sync").ConfigureAwait(false);
            return;
        }

        if (BehindRemoteCount > 0)
        {
            var commitsToPull = BehindRemoteCount;
            await ExecuteGitMutationAsync(
                async () =>
                {
                    var pull = await _gitService.PullAsync(workspaceRoot).ConfigureAwait(false);
                    if (!pull.IsSuccess)
                        return pull;

                    SetTransientSummary(GitSyncSummary.FormatPullResult(commitsToPull));

                    if (AheadRemoteCount <= 0)
                        return pull;

                    var push = await _gitService.PushAsync(workspaceRoot).ConfigureAwait(false);
                    return push.IsSuccess
                        ? push
                        : Result<GitCommandResult>.Fail(push.Error ?? "Git push failed.");
                },
                "sync").ConfigureAwait(false);
            return;
        }

        if (AheadRemoteCount > 0)
        {
            await ExecuteGitMutationAsync(
                () => _gitService.PushAsync(workspaceRoot),
                "sync").ConfigureAwait(false);
            return;
        }

        await PullLatestAsync(workspaceRoot).ConfigureAwait(false);
    }

    public async Task PullLatestAsync(string? workspaceRoot = null)
    {
        if (HasMergeConflict || IsBusy || !IsVisible)
            return;

        workspaceRoot ??= _workspaceViewModel.WorkspaceRoot;
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return;

        _isPulling = true;
        UpdatePrimaryActionText();
        SetTransientSummary(GitSyncSummary.Pulling, persistThroughRefresh: true);
        EnterBusy();

        try
        {
            await _gitService.FetchAsync(workspaceRoot).ConfigureAwait(false);
            var statusResult = await _gitService.GetRepositoryStatusAsync(workspaceRoot, includeRemoteState: true).ConfigureAwait(false);
            var behindBefore = statusResult.IsSuccess ? statusResult.Value!.BehindRemoteCount : 0;

            var pull = await _gitService.PullAsync(workspaceRoot).ConfigureAwait(false);
            if (!pull.IsSuccess)
            {
                var message = pull.Error ?? "Git pull failed.";
                LastError = message;
                _notificationService.ShowError(message);
                ClearTransientSummary();
                return;
            }

            if (!pull.Value!.IsSuccess)
            {
                var message = !string.IsNullOrWhiteSpace(pull.Value.Stderr)
                    ? pull.Value.Stderr
                    : $"Git pull failed (exit code {pull.Value.ExitCode}).";
                LastError = message;
                _notificationService.ShowError(message);
                ClearTransientSummary();
                return;
            }

            LastError = null;
            _lastRemoteFetchUtc = DateTimeOffset.UtcNow;
            SetTransientSummary(GitSyncSummary.FormatPullResult(behindBefore));
        }
        catch (Exception ex)
        {
            var message = $"Git pull failed: {ex.Message}";
            LastError = message;
            _notificationService.ShowError(message);
            ClearTransientSummary();
        }
        finally
        {
            _isPulling = false;
            QueueRefresh();
            ExitBusy();
            UpdatePrimaryActionText();
        }
    }

    public async Task RefreshAsync()
    {
        await _refreshLock.WaitAsync().ConfigureAwait(false);
        EnterBusy();

        try
        {
            if (!_workspaceViewModel.HasWorkspace || string.IsNullOrWhiteSpace(_workspaceViewModel.WorkspaceRoot))
            {
                ResetHiddenState();
                DisposeWatcher();
                return;
            }

            var workspaceRoot = _workspaceViewModel.WorkspaceRoot;
            var includeRemoteState = ShouldFetchRemoteState();
            var result = await _gitService.GetRepositoryStatusAsync(workspaceRoot, includeRemoteState).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                var message = result.Error ?? "Failed to read Git repository status.";
                SetHiddenState(message);
                _notificationService.ShowError(message);
                DisposeWatcher();
                return;
            }

            if (includeRemoteState)
                _lastRemoteFetchUtc = DateTimeOffset.UtcNow;

            var status = result.Value!;
            if (!status.IsGitAvailable)
            {
                SetHiddenState(DescribeProbeFailure(status.ProbeResult, "Git is not available."));
                DisposeWatcher();
                return;
            }

            if (!status.IsRepository)
            {
                SetHiddenState(DescribeProbeFailure(status.ProbeResult, "Workspace is not a Git repository."));
                DisposeWatcher();
                return;
            }

            EnsureWatcher(workspaceRoot);
            ApplyStatus(status);
        }
        catch (Exception ex)
        {
            var message = $"Failed to read Git repository status: {ex.Message}";
            SetHiddenState(message);
            _notificationService.ShowError(message);
            DisposeWatcher();
        }
        finally
        {
            ExitBusy();
            _refreshLock.Release();
        }
    }

    private void ApplyStatus(GitRepositoryStatus status)
    {
        BranchName = status.BranchName;
        UncommittedChangeCount = status.UncommittedChangeCount;
        BehindRemoteCount = status.BehindRemoteCount;
        AheadRemoteCount = status.AheadRemoteCount;
        HasMergeConflict = status.HasMergeConflict;
        ChangedFiles = status.ChangedFiles;
        ChangedFileTree = GitChangeTreeBuilder.Build(status.ChangedFiles);
        ApplySummaryText(status);
        IsVisible = true;
        UpdatePrimaryActionText();
        RaiseIsFullySyncedChanged();

        if (status.HasMergeConflict)
        {
            var message = status.ConflictedFiles.Count > 0
                ? BuildMergeConflictMessage(status.ConflictedFiles)
                : "Merge conflict detected. Resolve using your preferred Git tooling before syncing.";
            LastError = message;
            if (!_conflictNotified)
            {
                _notificationService.ShowError(message);
                _conflictNotified = true;
            }
        }
        else if (!status.HasMergeConflict && HasMergeConflict)
        {
            _conflictNotified = false;
        }
    }

    private void ApplySummaryText(GitRepositoryStatus status)
    {
        if (TryGetActiveTransientSummary(out var transientSummary))
        {
            ChangeSummaryText = transientSummary;
            StatusText = transientSummary;
            return;
        }

        var summary = GitSyncSummary.Format(
            status.UncommittedChangeCount,
            status.BehindRemoteCount,
            status.AheadRemoteCount,
            status.HasMergeConflict);
        ChangeSummaryText = summary;
        StatusText = summary;
    }

    private bool TryGetActiveTransientSummary(out string summary)
    {
        if (_transientSummary != null
            && _transientSummaryUntil.HasValue
            && DateTimeOffset.UtcNow <= _transientSummaryUntil.Value)
        {
            summary = _transientSummary;
            return true;
        }

        _transientSummary = null;
        _transientSummaryUntil = null;
        summary = string.Empty;
        return false;
    }

    private void SetTransientSummary(string summary, TimeSpan? duration = null, bool persistThroughRefresh = false)
    {
        _transientSummary = summary;
        _transientSummaryUntil = persistThroughRefresh
            ? DateTimeOffset.UtcNow.AddMinutes(1)
            : DateTimeOffset.UtcNow.Add(duration ?? TimeSpan.FromSeconds(8));
        ChangeSummaryText = summary;
        StatusText = summary;
    }

    private void ClearTransientSummary()
    {
        _transientSummary = null;
        _transientSummaryUntil = null;
    }

    private void UpdatePrimaryActionText()
    {
        if (IsBusy && _isPulling)
        {
            PrimaryActionText = "Pulling…";
            return;
        }

        PrimaryActionText = IsFullySynced ? "Pull" : "Sync";
    }

    private bool ShouldFetchRemoteState()
    {
        if (_lastRemoteFetchUtc == null)
            return true;

        return DateTimeOffset.UtcNow - _lastRemoteFetchUtc.Value >= TimeSpan.FromSeconds(RemoteFetchIntervalSeconds);
    }

    private void HandleWorkspaceChanged()
    {
        _lastRemoteFetchUtc = null;
        _conflictNotified = false;

        if (!_workspaceViewModel.HasWorkspace || string.IsNullOrWhiteSpace(_workspaceViewModel.WorkspaceRoot))
        {
            ResetHiddenState();
            DisposeWatcher();
            return;
        }

        if (!string.Equals(_watchedRoot, _workspaceViewModel.WorkspaceRoot, StringComparison.OrdinalIgnoreCase))
            DisposeWatcher();

        QueueRefresh();
    }

    private void QueueRefresh()
    {
        if (_disposed)
            return;

        _refreshRequests.OnNext(Unit.Default);
    }

    private async Task ExecuteGitMutationAsync(Func<Task<Result<GitCommandResult>>> operation, string operationName)
    {
        EnterBusy();
        try
        {
            var result = await operation().ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                var message = result.Error ?? $"Git {operationName} failed.";
                LastError = message;
                _notificationService.ShowError(message);
                return;
            }

            var command = result.Value!;
            if (!command.IsSuccess)
            {
                var message = !string.IsNullOrWhiteSpace(command.Stderr)
                    ? command.Stderr
                    : $"Git {operationName} failed (exit code {command.ExitCode}).";
                LastError = message;
                _notificationService.ShowError(message);
                return;
            }

            LastError = null;
        }
        catch (Exception ex)
        {
            var message = $"Git {operationName} failed: {ex.Message}";
            LastError = message;
            _notificationService.ShowError(message);
        }
        finally
        {
            QueueRefresh();
            ExitBusy();
        }
    }

    private void EnsureWatcher(string workspaceRoot)
    {
        if (_watcher != null && string.Equals(_watchedRoot, workspaceRoot, StringComparison.OrdinalIgnoreCase))
            return;

        DisposeWatcher();

        try
        {
            var watcher = new FileSystemWatcher(workspaceRoot)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size
            };

            FileSystemEventHandler queueHandler = (_, __) => QueueRefresh();
            RenamedEventHandler renamedHandler = (_, __) => QueueRefresh();
            watcher.Changed += queueHandler;
            watcher.Created += queueHandler;
            watcher.Deleted += queueHandler;
            watcher.Renamed += renamedHandler;
            watcher.Error += (_, __) => QueueRefresh();
            watcher.EnableRaisingEvents = true;

            _watcher = watcher;
            _watchedRoot = workspaceRoot;
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Failed to watch Git workspace: {ex.Message}");
            _watcher = null;
            _watchedRoot = null;
        }
    }

    private void DisposeWatcher()
    {
        if (_watcher == null)
            return;

        try
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
        }
        finally
        {
            _watcher = null;
            _watchedRoot = null;
        }
    }

    private void ResetHiddenState()
    {
        BranchName = null;
        UncommittedChangeCount = 0;
        BehindRemoteCount = 0;
        AheadRemoteCount = 0;
        HasMergeConflict = false;
        ChangedFiles = Array.Empty<GitChangedFile>();
        ChangedFileTree = Array.Empty<GitChangeTreeNode>();
        ChangeSummaryText = string.Empty;
        StatusText = string.Empty;
        CanSync = false;
        PrimaryActionText = "Sync";
        ClearTransientSummary();
        _isPulling = false;
        LastError = null;
        _conflictNotified = false;
        IsVisible = false;
        RaiseIsFullySyncedChanged();
    }

    private void SetHiddenState(string? lastError)
    {
        BranchName = null;
        UncommittedChangeCount = 0;
        BehindRemoteCount = 0;
        AheadRemoteCount = 0;
        HasMergeConflict = false;
        ChangedFiles = Array.Empty<GitChangedFile>();
        ChangedFileTree = Array.Empty<GitChangeTreeNode>();
        ChangeSummaryText = string.Empty;
        StatusText = string.Empty;
        CanSync = false;
        LastError = lastError;
        _conflictNotified = false;
        IsVisible = false;
        RaiseIsFullySyncedChanged();
    }

    private void RaiseIsFullySyncedChanged()
        => this.RaisePropertyChanged(nameof(IsFullySynced));

    private static string DescribeProbeFailure(GitCommandResult probeResult, string fallback)
        => !string.IsNullOrWhiteSpace(probeResult.Stderr) ? probeResult.Stderr : fallback;

    private static string BuildMergeConflictMessage(IReadOnlyList<string> conflictedFiles)
    {
        var fileList = string.Join(Environment.NewLine, conflictedFiles.Select(path => $"  • {path}"));
        return $"Merge conflict in one or more files. Resolve using your preferred Git tooling before syncing:{Environment.NewLine}{fileList}";
    }

    private string NormalizeCommitMessage(string? commitMessage)
        => string.IsNullOrWhiteSpace(commitMessage) ? CreateDefaultCommitMessage() : commitMessage;

    private void EnterBusy()
    {
        if (Interlocked.Increment(ref _busyDepth) == 1)
        {
            IsBusy = true;
            CanSync = IsVisible && !HasMergeConflict && !IsBusy;
            UpdatePrimaryActionText();
        }
    }

    private void ExitBusy()
    {
        if (Interlocked.Decrement(ref _busyDepth) == 0)
        {
            IsBusy = false;
            CanSync = IsVisible && !HasMergeConflict && !IsBusy;
            UpdatePrimaryActionText();
            RaiseIsFullySyncedChanged();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Disposables.Dispose();
    }
}

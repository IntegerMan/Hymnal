using System;
using System.IO;
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
using ReactiveUI;
using Unit = System.Reactive.Unit;

namespace Hymnal.ViewModels;

/// <summary>
/// Git toolbar state for the workspace header. Keeps the branch / change-count display fresh,
/// hides itself when Git is unavailable or the workspace is not a repository, and debounces
/// workspace/save/watch refresh signals so file saves and git metadata writes do not spam status.
/// </summary>
public sealed class GitPanelViewModel : ViewModelBase, IDisposable
{
    private const int RefreshDebounceMilliseconds = 250;

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

    private string _statusText = string.Empty;
    public string StatusText
    {
        get => _statusText;
        private set => this.RaiseAndSetIfChanged(ref _statusText, value);
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
    public ReactiveCommand<string?, Unit> CommitOnlyCommand { get; }
    public ReactiveCommand<string?, Unit> CommitAndPushCommand { get; }

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

        var canMutate = this.WhenAnyValue(x => x.IsVisible, x => x.IsBusy, (visible, busy) => visible && !busy);
        var canRefresh = this.WhenAnyValue(x => x.IsBusy, busy => !busy);

        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync, canRefresh);
        CommitOnlyCommand = ReactiveCommand.CreateFromTask<string?>(CommitOnlyAsync, canMutate);
        CommitAndPushCommand = ReactiveCommand.CreateFromTask<string?>(CommitAndPushAsync, canMutate);

        Disposables.Add(RefreshCommand.ThrownExceptions.Subscribe(Observer.Create<Exception>(ex => _notificationService.ShowError($"Failed to refresh Git status: {ex.Message}"))));
        Disposables.Add(CommitOnlyCommand.ThrownExceptions.Subscribe(Observer.Create<Exception>(ex => _notificationService.ShowError($"Failed to commit changes: {ex.Message}"))));
        Disposables.Add(CommitAndPushCommand.ThrownExceptions.Subscribe(Observer.Create<Exception>(ex => _notificationService.ShowError($"Failed to push changes: {ex.Message}"))));

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

    public async Task CommitOnlyAsync(string? commitMessage)
        => await ExecuteGitMutationAsync(
            () => _gitService.StageAllAndCommitAsync(_workspaceViewModel.WorkspaceRoot, NormalizeCommitMessage(commitMessage)),
            "commit").ConfigureAwait(false);

    public async Task CommitAndPushAsync(string? commitMessage)
        => await ExecuteGitMutationAsync(
            () => _gitService.StageAllCommitAndPushAsync(_workspaceViewModel.WorkspaceRoot, NormalizeCommitMessage(commitMessage)),
            "push").ConfigureAwait(false);

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
            var result = await _gitService.GetRepositoryStatusAsync(workspaceRoot).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                var message = result.Error ?? "Failed to read Git repository status.";
                SetHiddenState(message);
                _notificationService.ShowError(message);
                DisposeWatcher();
                return;
            }

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
            BranchName = status.BranchName;
            UncommittedChangeCount = status.UncommittedChangeCount;
            StatusText = FormatStatusText(status.BranchName, status.UncommittedChangeCount);
            IsVisible = true;
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

    private void HandleWorkspaceChanged()
    {
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
        StatusText = string.Empty;
        LastError = null;
        IsVisible = false;
    }

    private void SetHiddenState(string? lastError)
    {
        BranchName = null;
        UncommittedChangeCount = 0;
        StatusText = string.Empty;
        LastError = lastError;
        IsVisible = false;
    }

    private static string DescribeProbeFailure(GitCommandResult probeResult, string fallback)
        => !string.IsNullOrWhiteSpace(probeResult.Stderr) ? probeResult.Stderr : fallback;

    private static string FormatStatusText(string? branchName, int uncommittedChangeCount)
    {
        var branch = string.IsNullOrWhiteSpace(branchName) ? "HEAD" : branchName;
        var changeLabel = uncommittedChangeCount == 0
            ? "clean"
            : uncommittedChangeCount == 1
                ? "1 change"
                : $"{uncommittedChangeCount:N0} changes";

        return $"{branch} · {changeLabel}";
    }

    private string NormalizeCommitMessage(string? commitMessage)
        => string.IsNullOrWhiteSpace(commitMessage) ? CreateDefaultCommitMessage() : commitMessage;

    private void EnterBusy()
    {
        if (Interlocked.Increment(ref _busyDepth) == 1)
            IsBusy = true;
    }

    private void ExitBusy()
    {
        if (Interlocked.Decrement(ref _busyDepth) == 0)
            IsBusy = false;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Disposables.Dispose();
    }
}

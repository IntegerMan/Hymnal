using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Hymnal.Core.Common;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;
using Hymnal.Core.Services;
using ReactiveUI.Builder;
using Xunit;
using CoreUnit = Hymnal.Core.Common.Unit;

namespace Hymnal.ViewModels;

public sealed class GitPanelViewModelTests : IDisposable
{
    private readonly TestContext _context = new();

    static GitPanelViewModelTests()
    {
        RxAppBuilder.CreateReactiveUIBuilder()
            .WithCoreServices()
            .BuildApp();
    }

    [Fact]
    public async Task RefreshAsync_WithNoWorkspace_ResetsStateAndStaysHidden()
    {
        _context.ClearWorkspace();
        var vm = _context.CreateGitPanel();

        await vm.RefreshAsync();

        Assert.False(vm.IsVisible);
        Assert.Null(vm.BranchName);
        Assert.Equal(0, vm.UncommittedChangeCount);
        Assert.Equal(string.Empty, vm.StatusText);
        Assert.Null(vm.LastError);
        Assert.False(vm.IsBusy);
        Assert.Equal(0, _context.GitService.StatusCalls);
    }

    [Fact]
    public async Task RefreshAsync_WhenRepositoryProbeFails_HidesPanelAndStoresLastError()
    {
        _context.EnableWorkspace();
        _context.GitService.EnqueueStatus(Result<GitRepositoryStatus>.Ok(GitRepositoryStatus.Hidden(
            ProbeFailure("fatal: not a git repository"),
            isGitAvailable: true)));
        var vm = _context.CreateGitPanel();

        await vm.RefreshAsync();

        Assert.False(vm.IsVisible);
        Assert.Null(vm.BranchName);
        Assert.Equal(0, vm.UncommittedChangeCount);
        Assert.Equal(string.Empty, vm.StatusText);
        Assert.Equal("fatal: not a git repository", vm.LastError);
        Assert.False(vm.IsBusy);
        Assert.Equal(1, _context.GitService.StatusCalls);
    }

    [Fact]
    public async Task RefreshAsync_WithVisibleRepository_ShowsBranchCountAndStatusText()
    {
        _context.EnableWorkspace();
        _context.GitService.EnqueueStatus(Result<GitRepositoryStatus>.Ok(VisibleStatus("feature/git-toolbar", 3)));
        var vm = _context.CreateGitPanel();

        await vm.RefreshAsync();

        Assert.True(vm.IsVisible);
        Assert.Equal("feature/git-toolbar", vm.BranchName);
        Assert.Equal(3, vm.UncommittedChangeCount);
        Assert.Equal("feature/git-toolbar · 3 changes", vm.StatusText);
        Assert.Null(vm.LastError);
        Assert.False(vm.IsBusy);
        Assert.Equal(1, _context.GitService.StatusCalls);
    }

    [Fact]
    public async Task SaveAsync_QueuesSingleRefreshForSaveAndWatcherEvents()
    {
        _context.EnableWorkspace();
        var filePath = _context.CreateWorkspaceFile("draft.md", "before");
        await _context.Editor.OpenArbitraryFileAsync(filePath);
        _context.GitService.EnqueueStatus(Result<GitRepositoryStatus>.Ok(VisibleStatus("main", 2)));
        _context.GitService.EnqueueStatus(Result<GitRepositoryStatus>.Ok(VisibleStatus("main", 1)));
        var vm = _context.CreateGitPanel();

        await vm.RefreshAsync();
        Assert.Equal(1, _context.GitService.StatusCalls);

        _context.Editor.Text = "after save";
        await _context.Editor.SaveAsync();

        Assert.True(SpinWait.SpinUntil(() => _context.GitService.StatusCalls == 2, TimeSpan.FromSeconds(3)));
        Assert.Equal("main", vm.BranchName);
        Assert.Equal(1, vm.UncommittedChangeCount);
        Assert.Equal("main · 1 change", vm.StatusText);
    }

    [Fact]
    public async Task ExternalFileChange_RefreshesGitStatusThroughWorkspaceWatcher()
    {
        _context.EnableWorkspace();
        var filePath = _context.CreateWorkspaceFile("watch.md", "before");
        await _context.Editor.OpenArbitraryFileAsync(filePath);
        _context.GitService.EnqueueStatus(Result<GitRepositoryStatus>.Ok(VisibleStatus("main", 4)));
        _context.GitService.EnqueueStatus(Result<GitRepositoryStatus>.Ok(VisibleStatus("main", 5)));
        var vm = _context.CreateGitPanel();

        await vm.RefreshAsync();
        Assert.Equal(1, _context.GitService.StatusCalls);

        await File.WriteAllTextAsync(filePath, "external change");

        Assert.True(SpinWait.SpinUntil(() => _context.GitService.StatusCalls == 2, TimeSpan.FromSeconds(3)));
        Assert.Equal(5, vm.UncommittedChangeCount);
        Assert.Equal("main · 5 changes", vm.StatusText);
    }

    [Fact]
    public async Task CommitOnlyAsync_UsesDefaultMessageWhenBlank_AndRefreshesAfterSuccess()
    {
        _context.EnableWorkspace();
        _context.GitService.EnqueueStatus(Result<GitRepositoryStatus>.Ok(VisibleStatus("main", 2)));
        _context.GitService.EnqueueCommit(Result<GitCommandResult>.Ok(CommitResult("[main abc123] commit\n", string.Empty)));
        _context.GitService.EnqueueStatus(Result<GitRepositoryStatus>.Ok(VisibleStatus("main", 0)));
        var vm = _context.CreateGitPanel();

        await vm.RefreshAsync();
        await vm.CommitOnlyAsync("   ");

        Assert.Single(_context.GitService.CommitMessages);
        Assert.StartsWith("Hymnal: save progress ", _context.GitService.CommitMessages[0]);
        Assert.True(SpinWait.SpinUntil(() => _context.GitService.StatusCalls == 2, TimeSpan.FromSeconds(3)));
        Assert.True(vm.IsVisible);
        Assert.Equal("main", vm.BranchName);
        Assert.Equal(0, vm.UncommittedChangeCount);
        Assert.Equal("main · clean", vm.StatusText);
        Assert.Null(vm.LastError);
    }

    [Fact]
    public void CreateDefaultCommitMessage_UsesIso8601UtcTimestamp()
    {
        var vm = _context.CreateGitPanel();

        var message = vm.CreateDefaultCommitMessage();

        Assert.StartsWith("Hymnal: save progress ", message);
        var timestamp = message["Hymnal: save progress ".Length..];
        var parsed = DateTime.ParseExact(timestamp, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        Assert.Equal(DateTimeKind.Utc, parsed.Kind);
    }

    [Fact]
    public async Task CommitFailure_ShowsRawStderrAndStillRefreshes()
    {
        _context.EnableWorkspace();
        _context.GitService.EnqueueStatus(Result<GitRepositoryStatus>.Ok(VisibleStatus("main", 2)));
        _context.GitService.EnqueueCommit(Result<GitCommandResult>.Ok(CommitFailure("fatal: index.lock exists")));
        _context.GitService.EnqueueStatus(Result<GitRepositoryStatus>.Ok(VisibleStatus("main", 2)));
        var vm = _context.CreateGitPanel();

        await vm.RefreshAsync();
        await vm.CommitOnlyAsync("Save draft");

        Assert.Contains("fatal: index.lock exists", _context.Notifications.Errors);
        Assert.True(SpinWait.SpinUntil(() => _context.GitService.StatusCalls == 2, TimeSpan.FromSeconds(3)));
        Assert.Equal("fatal: index.lock exists", vm.LastError);
        Assert.True(vm.IsVisible);
    }

    [Fact]
    public async Task WorkspaceChange_DisposesOldWatcherBeforeNewRootRefreshes()
    {
        _context.EnableWorkspace();
        var oldFile = _context.CreateWorkspaceFile("old.md", "before");
        await _context.Editor.OpenArbitraryFileAsync(oldFile);
        _context.GitService.EnqueueStatus(Result<GitRepositoryStatus>.Ok(VisibleStatus("main", 1)));
        _context.GitService.EnqueueStatus(Result<GitRepositoryStatus>.Ok(VisibleStatus("feature/new-root", 2)));
        var vm = _context.CreateGitPanel();

        await vm.RefreshAsync();
        Assert.Equal(1, _context.GitService.StatusCalls);

        _context.SwitchWorkspace();
        Assert.True(SpinWait.SpinUntil(() => _context.GitService.StatusCalls == 2, TimeSpan.FromSeconds(3)));

        var callsBefore = _context.GitService.StatusCalls;
        await File.WriteAllTextAsync(oldFile, "after switch");

        Assert.False(SpinWait.SpinUntil(() => _context.GitService.StatusCalls > callsBefore, TimeSpan.FromMilliseconds(700)));
        Assert.Equal(callsBefore, _context.GitService.StatusCalls);
    }

    [Fact]
    public async Task CommitAndPushAsync_UsesDefaultMessageAndRefreshesAfterPush()
    {
        _context.EnableWorkspace();
        _context.GitService.EnqueueStatus(Result<GitRepositoryStatus>.Ok(VisibleStatus("main", 4)));
        _context.GitService.EnqueuePush(Result<GitCommandResult>.Ok(CommitResult("To origin\n", string.Empty)));
        _context.GitService.EnqueueStatus(Result<GitRepositoryStatus>.Ok(VisibleStatus("main", 0)));
        var vm = _context.CreateGitPanel();

        await vm.RefreshAsync();
        await vm.CommitAndPushAsync(null);

        Assert.Single(_context.GitService.PushMessages);
        Assert.StartsWith("Hymnal: save progress ", _context.GitService.PushMessages[0]);
        Assert.True(SpinWait.SpinUntil(() => _context.GitService.StatusCalls == 2, TimeSpan.FromSeconds(3)));
        Assert.Equal("main · clean", vm.StatusText);
        Assert.True(vm.IsVisible);
    }

    private static GitRepositoryStatus VisibleStatus(string branch, int changeCount)
    {
        var probe = new GitCommandResult("git", new[] { "-C", "workspace", "rev-parse", "--is-inside-work-tree" }, "workspace", 0, "true\n", string.Empty);
        var branchResult = new GitCommandResult("git", new[] { "-C", "workspace", "branch", "--show-current" }, "workspace", 0, branch + "\n", string.Empty);
        var statusResult = new GitCommandResult("git", new[] { "-C", "workspace", "status", "--porcelain" }, "workspace", 0, string.Join(Environment.NewLine, Enumerable.Range(0, changeCount).Select(i => $" M file{i}.md")) + (changeCount > 0 ? Environment.NewLine : string.Empty), string.Empty);
        return new GitRepositoryStatus(true, true, branch, changeCount, probe, branchResult, statusResult);
    }

    private static GitCommandResult ProbeFailure(string stderr)
        => new("git", new[] { "-C", "workspace", "rev-parse", "--is-inside-work-tree" }, "workspace", 128, string.Empty, stderr);

    private static GitCommandResult CommitResult(string stdout, string stderr)
        => new("git", new[] { "-C", "workspace", "commit", "-m", "message" }, "workspace", 0, stdout, stderr);

    private static GitCommandResult CommitFailure(string stderr)
        => new("git", new[] { "-C", "workspace", "commit", "-m", "message" }, "workspace", 1, string.Empty, stderr);

    public void Dispose() => _context.Dispose();

    private sealed class TestContext : IDisposable
    {
        public RecordingNotificationService Notifications { get; } = new();
        public RecordingGitService GitService { get; } = new();
        public RecordingMetadataStore MetadataStore { get; } = new();
        public FakeAppSettingsStore SettingsStore { get; } = new();
        public FakeFolderPickerService FolderPicker { get; } = new();
        public WordCountService WordCountService { get; } = new();
        public EditorViewModel Editor { get; }
        public WorkspaceViewModel Workspace { get; }
        public string WorkspaceRoot { get; private set; }
        public string ManuscriptRoot { get; private set; }

        public TestContext()
        {
            WorkspaceRoot = Path.Combine(Path.GetTempPath(), "hymnal-git-panel-tests", Guid.NewGuid().ToString("N"));
            ManuscriptRoot = Path.Combine(WorkspaceRoot, "manuscript");
            Directory.CreateDirectory(WorkspaceRoot);
            Directory.CreateDirectory(ManuscriptRoot);

            Editor = new EditorViewModel(MetadataStore, Notifications, WordCountService)
            {
                HasWorkspace = true
            };

            Workspace = new WorkspaceViewModel(
                new ManuscriptService(Notifications),
                SettingsStore,
                FolderPicker,
                Notifications,
                Editor,
                new ChapterRegistryService(MetadataStore),
                new PhaseDataService(MetadataStore),
                new TargetsService(MetadataStore),
                WordCountService,
                new WordCountHistoryService(MetadataStore));

            ApplyWorkspace(WorkspaceRoot, ManuscriptRoot);
        }

        public GitPanelViewModel CreateGitPanel()
            => new(Workspace, Editor, GitService, Notifications);

        public void EnableWorkspace()
        {
            WorkspaceRoot = Path.Combine(Path.GetTempPath(), "hymnal-git-panel-tests", Guid.NewGuid().ToString("N"));
            ManuscriptRoot = Path.Combine(WorkspaceRoot, "manuscript");
            Directory.CreateDirectory(WorkspaceRoot);
            Directory.CreateDirectory(ManuscriptRoot);
            ApplyWorkspace(WorkspaceRoot, ManuscriptRoot);
        }

        public void SwitchWorkspace()
        {
            WorkspaceRoot = Path.Combine(Path.GetTempPath(), "hymnal-git-panel-tests", Guid.NewGuid().ToString("N"));
            ManuscriptRoot = Path.Combine(WorkspaceRoot, "manuscript");
            Directory.CreateDirectory(WorkspaceRoot);
            Directory.CreateDirectory(ManuscriptRoot);
            ApplyWorkspace(WorkspaceRoot, ManuscriptRoot);
        }

        public string CreateWorkspaceFile(string relativePath, string content)
        {
            var path = Path.Combine(WorkspaceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
            return path;
        }

        public void ClearWorkspace()
        {
            var modelField = typeof(WorkspaceViewModel).GetField("_model", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Workspace model field not found.");
            modelField.SetValue(Workspace, null);
            SetPrivateProperty(Workspace, nameof(WorkspaceViewModel.HasWorkspace), false);
            SetPrivateProperty<string?>(Workspace, nameof(WorkspaceViewModel.WorkspaceName), null);
            RaiseWorkspaceChanged(Workspace);
        }

        private void ApplyWorkspace(string workspaceRoot, string manuscriptRoot)
        {
            var model = new ManuscriptModel();
            model.SetRoots(workspaceRoot, manuscriptRoot);
            SetPrivateField(Workspace, "_model", model);
            SetPrivateProperty(Workspace, nameof(WorkspaceViewModel.HasWorkspace), true);
            SetPrivateProperty(Workspace, nameof(WorkspaceViewModel.WorkspaceName), Path.GetFileName(workspaceRoot));
            RaiseWorkspaceChanged(Workspace);
        }

        private static void RaiseWorkspaceChanged(WorkspaceViewModel workspace)
        {
            var field = typeof(WorkspaceViewModel).GetField("_workspaceChanged", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Workspace change subject not found.");

            var subject = (IObserver<CoreUnit>)field.GetValue(workspace)!;
            subject.OnNext(CoreUnit.Default);
        }

        public void Dispose()
        {
            try
            {
                Editor.Dispose();
            }
            catch
            {
                // Best-effort cleanup.
            }

            try
            {
                if (Directory.Exists(WorkspaceRoot))
                    Directory.Delete(WorkspaceRoot, recursive: true);
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }

    private sealed class RecordingGitService : IGitService
    {
        private readonly Queue<Result<GitRepositoryStatus>> _statusResults = new();
        private readonly Queue<Result<GitCommandResult>> _commitResults = new();
        private readonly Queue<Result<GitCommandResult>> _pushResults = new();

        public int StatusCalls { get; private set; }
        public List<string> CommitMessages { get; } = new();
        public List<string> PushMessages { get; } = new();

        public void EnqueueStatus(Result<GitRepositoryStatus> result) => _statusResults.Enqueue(result);
        public void EnqueueCommit(Result<GitCommandResult> result) => _commitResults.Enqueue(result);
        public void EnqueuePush(Result<GitCommandResult> result) => _pushResults.Enqueue(result);

        public Task<Result<GitCommandResult>> CheckGitAvailableAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Result<GitCommandResult>.Ok(new GitCommandResult("git", new[] { "--version" }, null, 0, "git version\n", string.Empty)));

        public Task<Result<GitRepositoryStatus>> GetRepositoryStatusAsync(string workspaceRoot, CancellationToken cancellationToken = default)
        {
            StatusCalls++;

            if (_statusResults.Count == 0)
                throw new InvalidOperationException("No Git status result was queued.");

            return Task.FromResult(_statusResults.Dequeue());
        }

        public Task<Result<GitCommandResult>> StageAllAndCommitAsync(string workspaceRoot, string commitMessage, CancellationToken cancellationToken = default)
        {
            CommitMessages.Add(commitMessage);
            if (_commitResults.Count == 0)
                throw new InvalidOperationException("No Git commit result was queued.");

            return Task.FromResult(_commitResults.Dequeue());
        }

        public Task<Result<GitCommandResult>> StageAllCommitAndPushAsync(string workspaceRoot, string commitMessage, CancellationToken cancellationToken = default)
        {
            PushMessages.Add(commitMessage);
            if (_pushResults.Count == 0)
                throw new InvalidOperationException("No Git push result was queued.");

            return Task.FromResult(_pushResults.Dequeue());
        }
    }

    private sealed class RecordingMetadataStore : IMetadataStore
    {
        public Task WriteTextAtomicAsync(string absolutePath, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
            return File.WriteAllTextAsync(absolutePath, content);
        }
    }

    private sealed class FakeAppSettingsStore : IAppSettingsStore
    {
        private readonly Dictionary<string, object?> _values = new(StringComparer.OrdinalIgnoreCase);

        public Task<T?> GetAsync<T>(string key)
            => Task.FromResult(_values.TryGetValue(key, out var value) && value is T typed ? typed : default);

        public Task SetAsync<T>(string key, T value)
        {
            _values[key] = value;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeFolderPickerService : IFolderPickerService
    {
        public Task<string?> PickFolderAsync() => Task.FromResult<string?>(null);
    }

    private sealed class RecordingNotificationService : INotificationService
    {
        public List<string> Errors { get; } = new();
        public List<string> Infos { get; } = new();
        public List<string> Successes { get; } = new();

        public void ShowError(string message) => Errors.Add(message);
        public void ShowInfo(string message) => Infos.Add(message);
        public void ShowSuccess(string message) => Successes.Add(message);
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        var field = typeof(WorkspaceViewModel).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field '{fieldName}' not found.");
        field.SetValue(target, value);
    }

    private static void SetPrivateProperty<T>(object target, string propertyName, T value)
    {
        var property = typeof(WorkspaceViewModel).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found.");
        property.SetValue(target, value);
    }
}

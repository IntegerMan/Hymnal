using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Hymnal.Core.Common;
using Hymnal.Core.Infrastructure;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;
using Hymnal.Core.Services;
using Hymnal.Infrastructure;
using Hymnal.Views;
using ReactiveUI.Builder;
using Xunit;

namespace Hymnal.ViewModels;

public sealed class MainWindowGitPanelTests : IDisposable
{
    private readonly TestContext _context = new();

    static MainWindowGitPanelTests()
    {
        RxAppBuilder.CreateReactiveUIBuilder()
            .WithCoreServices()
            .BuildApp();
    }

    [Fact]
    public async Task MainWindowViewModel_ExposesGitPanelState()
    {
        _context.EnableWorkspace();
        _context.GitService.EnqueueStatus(Result<GitRepositoryStatus>.Ok(VisibleStatus("feature/git-toolbar", 3)));

        var window = _context.CreateMainWindow();

        Assert.NotNull(window.GitPanelViewModel);
        await window.GitPanelViewModel.RefreshAsync();

        Assert.True(window.GitPanelViewModel.IsVisible);
        Assert.Equal("feature/git-toolbar", window.GitPanelViewModel.BranchName);
        Assert.Equal(3, window.GitPanelViewModel.UncommittedChangeCount);
        Assert.Equal("feature/git-toolbar · 3 changes", window.GitPanelViewModel.StatusText);
    }

    [Fact]
    public async Task ExecuteGitCommitActionAsync_RoutesCommitOnlyThroughGitPanel()
    {
        _context.EnableWorkspace();
        _context.GitService.EnqueueStatus(Result<GitRepositoryStatus>.Ok(VisibleStatus("main", 2)));
        _context.GitService.EnqueueCommit(Result<GitCommandResult>.Ok(CommitResult("[main abc123] save\n", string.Empty)));
        _context.GitService.EnqueueStatus(Result<GitRepositoryStatus>.Ok(VisibleStatus("main", 0)));

        var window = _context.CreateMainWindow();
        await window.GitPanelViewModel.RefreshAsync();

        await MainWindow.ExecuteGitCommitActionAsync(window.GitPanelViewModel, GitCommitDialogAction.CommitOnly, "save draft");

        Assert.Equal(new[] { "save draft" }, _context.GitService.CommitMessages);
        Assert.Empty(_context.GitService.PushMessages);
        Assert.True(SpinWait.SpinUntil(() => _context.GitService.StatusCalls == 2, TimeSpan.FromSeconds(3)));
        Assert.Equal("main · clean", window.GitPanelViewModel.StatusText);
        Assert.True(window.GitPanelViewModel.IsVisible);
    }

    [Fact]
    public async Task ExecuteGitCommitActionAsync_RoutesCommitAndPushThroughGitPanel()
    {
        _context.EnableWorkspace();
        _context.GitService.EnqueueStatus(Result<GitRepositoryStatus>.Ok(VisibleStatus("main", 4)));
        _context.GitService.EnqueueCommit(Result<GitCommandResult>.Ok(CommitResult("[main abc123] save\n", string.Empty)));
        _context.GitService.EnqueuePush(Result<GitCommandResult>.Ok(PushResult("To origin\n", string.Empty)));
        _context.GitService.EnqueueStatus(Result<GitRepositoryStatus>.Ok(VisibleStatus("main", 0)));

        var window = _context.CreateMainWindow();
        await window.GitPanelViewModel.RefreshAsync();

        await MainWindow.ExecuteGitCommitActionAsync(window.GitPanelViewModel, GitCommitDialogAction.CommitAndPush, null);

        Assert.Single(_context.GitService.CommitMessages);
        Assert.Single(_context.GitService.PushMessages);
        Assert.StartsWith("Hymnal: save progress ", _context.GitService.CommitMessages[0]);
        Assert.StartsWith("Hymnal: save progress ", _context.GitService.PushMessages[0]);
        Assert.True(SpinWait.SpinUntil(() => _context.GitService.StatusCalls == 2, TimeSpan.FromSeconds(3)));
        Assert.Equal("main · clean", window.GitPanelViewModel.StatusText);
    }

    private static GitRepositoryStatus VisibleStatus(string branch, int changeCount)
    {
        var probe = new GitCommandResult("git", new[] { "-C", "workspace", "rev-parse", "--is-inside-work-tree" }, "workspace", 0, "true\n", string.Empty);
        var branchResult = new GitCommandResult("git", new[] { "-C", "workspace", "branch", "--show-current" }, "workspace", 0, branch + "\n", string.Empty);
        var statusResult = new GitCommandResult("git", new[] { "-C", "workspace", "status", "--porcelain" }, "workspace", 0, string.Join(Environment.NewLine, Enumerable.Range(0, changeCount).Select(i => $" M file{i}.md")) + (changeCount > 0 ? Environment.NewLine : string.Empty), string.Empty);
        return new GitRepositoryStatus(true, true, branch, changeCount, probe, branchResult, statusResult);
    }

    private static GitCommandResult CommitResult(string stdout, string stderr)
        => new("git", new[] { "-C", "workspace", "commit", "-m", "message" }, "workspace", 0, stdout, stderr);

    private static GitCommandResult PushResult(string stdout, string stderr)
        => new("git", new[] { "-C", "workspace", "push" }, "workspace", 0, stdout, stderr);

    public void Dispose() => _context.Dispose();

    private sealed class TestContext : IDisposable
    {
        public NotificationService Notifications { get; } = new();
        public RecordingGitService GitService { get; } = new();
        public RecordingMetadataStore MetadataStore { get; } = new();
        public FakeAppSettingsStore SettingsStore { get; } = new();
        public FakeFolderPickerService FolderPicker { get; } = new();
        public WordCountService WordCountService { get; } = new();
        public EditorViewModel Editor { get; }
        public WorkspaceViewModel Workspace { get; }
        public string WorkspaceRoot { get; }
        public string ManuscriptRoot { get; }

        public TestContext()
        {
            WorkspaceRoot = Path.Combine(Path.GetTempPath(), "hymnal-main-window-git-tests", Guid.NewGuid().ToString("N"));
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
        }

        public MainWindowViewModel CreateMainWindow()
        {
            var supplementalDocs = new SupplementalDocsViewModel(Workspace, new SupplementalDocsService(MetadataStore), Editor, Notifications);
            var gitPanel = new GitPanelViewModel(Workspace, Editor, GitService, Notifications);
            return new MainWindowViewModel(
                Workspace,
                Editor,
                new NotesViewModel(Editor, Workspace, new NotesService(MetadataStore), Notifications, SettingsStore),
                new ChapterInfoViewModel(Editor, Workspace, new PhaseDataService(MetadataStore), new TargetsService(MetadataStore), SettingsStore, Notifications),
                new GanttViewModel(Workspace, new PhaseDataService(MetadataStore), Notifications),
                new CorkboardViewModel(Workspace, new FakeBookTxtStructureService(), Notifications),
                supplementalDocs,
                gitPanel,
                Notifications,
                SettingsStore);
        }

        public void EnableWorkspace()
        {
            var model = new ManuscriptModel();
            model.SetRoots(WorkspaceRoot, ManuscriptRoot);
            SetPrivateField(Workspace, "_model", model);
            SetPrivateProperty(Workspace, nameof(WorkspaceViewModel.HasWorkspace), true);
            SetPrivateProperty(Workspace, nameof(WorkspaceViewModel.WorkspaceName), Path.GetFileName(WorkspaceRoot));
            Editor.HasWorkspace = true;
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
            return Task.FromResult(_statusResults.Dequeue());
        }

        public Task<Result<GitCommandResult>> StageAllAndCommitAsync(string workspaceRoot, string commitMessage, CancellationToken cancellationToken = default)
        {
            CommitMessages.Add(commitMessage);
            return Task.FromResult(_commitResults.Dequeue());
        }

        public Task<Result<GitCommandResult>> StageAllCommitAndPushAsync(string workspaceRoot, string commitMessage, CancellationToken cancellationToken = default)
        {
            CommitMessages.Add(commitMessage);
            PushMessages.Add(commitMessage);
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

    private sealed class FakeBookTxtStructureService : IBookTxtStructureService
    {
        public Task<Result<IReadOnlyList<string>>> ReadNormalizedEntriesAsync(string bookTxtPath)
            => Task.FromResult(Result<IReadOnlyList<string>>.Ok(Array.Empty<string>()));

        public Task<Result<Unit>> ReorderEntryAsync(string bookTxtPath, string chapterPath, int newIndex)
            => Task.FromResult(Result<Unit>.Ok(Unit.Default));

        public Task<Result<Unit>> RenameEntryAsync(string bookTxtPath, string existingPath, string replacementPath)
            => Task.FromResult(Result<Unit>.Ok(Unit.Default));

        public Task<Result<Unit>> AddExistingEntryAsync(string bookTxtPath, string chapterPath, int index)
            => Task.FromResult(Result<Unit>.Ok(Unit.Default));

        public Task<Result<Unit>> AddExistingEntryAfterPartAsync(string bookTxtPath, string chapterPath, string partPath)
            => Task.FromResult(Result<Unit>.Ok(Unit.Default));

        public Task<Result<Unit>> CreateNewChapterAsync(string bookTxtPath, string chapterPath, string content, int index)
            => Task.FromResult(Result<Unit>.Ok(Unit.Default));

        public Task<Result<Unit>> RemoveEntryAsync(string bookTxtPath, string chapterPath)
            => Task.FromResult(Result<Unit>.Ok(Unit.Default));

        public Task<Result<Unit>> DeleteChapterFileAsync(string bookTxtPath, string chapterPath)
            => Task.FromResult(Result<Unit>.Ok(Unit.Default));
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

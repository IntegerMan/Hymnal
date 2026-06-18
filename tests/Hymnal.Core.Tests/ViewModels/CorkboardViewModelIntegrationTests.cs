using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hymnal.Core.Common;
using Hymnal.Core.Infrastructure;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;
using Hymnal.Core.Services;
using Hymnal.Core.Tests.Infrastructure;
using ReactiveUI;
using Xunit;

namespace Hymnal.ViewModels;

[Collection("AvaloniaUi")]
public sealed class CorkboardViewModelIntegrationTests
{
    static CorkboardViewModelIntegrationTests() => ReactiveUiTestBootstrap.EnsureInitialized();

    [Fact]
    public async Task DropCardCommand_CrossPartMove_UpdatesFilesBookTxtRegistryAndReloadedProjection()
    {
        using var context = new TestContext(CreateWorkspace(
            ("Book.txt", "part-one/part.md\npart-one/chapter-one.md\npart-two/part.md\npart-two/chapter-two.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One\n\nBody words."),
            ("part-two/part.md", "{class: part}\n# Part Two"),
            ("part-two/chapter-two.md", "# Chapter Two\n\nBody words.")));

        await context.OpenWorkspaceAsync(expectedNodeCount: 4);
        var original = await context.WaitForNodeAsync("part-one/chapter-one.md", requireUuid: true);
        Assert.False(string.IsNullOrWhiteSpace(original.Uuid));
        await SeedUuidSidecarsAsync(context.WorkspaceRoot, original.Uuid);

        using var board = context.CreateBoard();
        await WaitForBoardProjectionAsync(board,
            "part-one/part.md",
            "part-one/chapter-one.md",
            "part-two/part.md",
            "part-two/chapter-two.md");

        var source = GetChapterCard(board, "part-one/chapter-one.md");
        await ExecuteCommandAsync(board.SelectCardCommand.Execute(source));
        await ExecuteCommandAsync(board.DropCardCommand.Execute(
            new CorkboardDropRequest(
                "part-one/chapter-one.md",
                TargetPartPath: "part-two/part.md",
                AfterRelativePath: "part-two/chapter-two.md")));

        await context.WaitForNodeAsync("part-two/chapter-one.md", requireUuid: true);
        await WaitForBoardProjectionAsync(board,
            "part-one/part.md",
            "part-two/part.md",
            "part-two/chapter-two.md",
            "part-two/chapter-one.md");

        Assert.Equal(
            new[]
            {
                "part-one/part.md",
                "part-two/part.md",
                "part-two/chapter-two.md",
                "part-two/chapter-one.md"
            },
            ReadBookTxtLines(context.BookTxtPath));

        Assert.False(File.Exists(AbsolutePath(context.WorkspaceRoot, "part-one/chapter-one.md")));
        Assert.True(File.Exists(AbsolutePath(context.WorkspaceRoot, "part-two/chapter-one.md")));

        var moved = Assert.Single(context.Workspace.Nodes, node => node.Node.RelativePath == "part-two/chapter-one.md");
        Assert.Equal(original.Uuid, moved.Uuid);
        Assert.Equal(ChapterStatus.Drafting, moved.Status);
        Assert.Equal(1000, moved.Target?.MinWords);
        Assert.Equal(1500, moved.Target?.MaxWords);
        Assert.Equal("part-two/chapter-one.md", board.SelectedCard?.RelativePath);
        Assert.Null(board.LastStructuralError);
        Assert.Empty(context.NotificationService.Errors);

        var registry = await context.RegistryService.LoadAsync(context.WorkspaceRoot);
        Assert.Equal("part-two/chapter-one.md", registry[original.Uuid].CurrentPath);
        await AssertUuidSidecarsAsync(context.WorkspaceRoot, original.Uuid);
    }

    [Fact]
    public async Task DropCardCommand_SamePartReorder_PersistsThroughRealReload()
    {
        using var context = new TestContext(CreateWorkspace(
            ("Book.txt", "part-one/part.md\npart-one/chapter-one.md\npart-one/chapter-two.md\npart-one/chapter-three.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"),
            ("part-one/chapter-two.md", "# Chapter Two"),
            ("part-one/chapter-three.md", "# Chapter Three")));

        await context.OpenWorkspaceAsync(expectedNodeCount: 4);

        using var board = context.CreateBoard();
        await WaitForBoardProjectionAsync(board,
            "part-one/part.md",
            "part-one/chapter-one.md",
            "part-one/chapter-two.md",
            "part-one/chapter-three.md");

        var source = GetChapterCard(board, "part-one/chapter-one.md");
        await ExecuteCommandAsync(board.SelectCardCommand.Execute(source));
        await ExecuteCommandAsync(board.DropCardCommand.Execute(
            new CorkboardDropRequest(
                "part-one/chapter-one.md",
                TargetPartPath: "part-one/part.md",
                AfterRelativePath: "part-one/chapter-three.md")));

        await WaitUntilAsync(
            () => context.Workspace.Nodes.Select(node => node.Node.RelativePath).SequenceEqual(
                new[]
                {
                    "part-one/part.md",
                    "part-one/chapter-two.md",
                    "part-one/chapter-three.md",
                    "part-one/chapter-one.md"
                }),
            $"Workspace nodes did not reorder after same-Part drop. Actual: {string.Join(", ", context.Workspace.Nodes.Select(node => node.Node.RelativePath))}.");

        await WaitForBoardProjectionAsync(board,
            "part-one/part.md",
            "part-one/chapter-two.md",
            "part-one/chapter-three.md",
            "part-one/chapter-one.md");

        Assert.Equal(
            new[]
            {
                "part-one/part.md",
                "part-one/chapter-two.md",
                "part-one/chapter-three.md",
                "part-one/chapter-one.md"
            },
            ReadBookTxtLines(context.BookTxtPath));

        Assert.Equal(
            new[]
            {
                "part-one/part.md",
                "part-one/chapter-two.md",
                "part-one/chapter-three.md",
                "part-one/chapter-one.md"
            },
            context.Workspace.Nodes.Select(node => node.Node.RelativePath));

        Assert.Equal("part-one/chapter-one.md", board.SelectedCard?.RelativePath);
        Assert.Null(board.LastStructuralError);
        Assert.Empty(context.NotificationService.Errors);
    }

    [Fact]
    public async Task DropCardCommand_TargetFileConflict_SurfacesStructuralErrorAndLeavesDiskStateUnchanged()
    {
        using var context = new TestContext(CreateWorkspace(
            ("Book.txt", "part-one/part.md\npart-one/chapter-one.md\npart-two/part.md\npart-two/chapter-two.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"),
            ("part-two/part.md", "{class: part}\n# Part Two"),
            ("part-two/chapter-two.md", "# Chapter Two"),
            ("part-two/chapter-one.md", "# Conflicting Target")));

        await context.OpenWorkspaceAsync(expectedNodeCount: 4);

        using var board = context.CreateBoard();
        await WaitForBoardProjectionAsync(board,
            "part-one/part.md",
            "part-one/chapter-one.md",
            "part-two/part.md",
            "part-two/chapter-two.md");

        var beforeProjection = GetProjectedPaths(board).ToArray();
        var source = GetChapterCard(board, "part-one/chapter-one.md");
        await ExecuteCommandAsync(board.SelectCardCommand.Execute(source));

        await ExecuteCommandAsync(board.DropCardCommand.Execute(
            new CorkboardDropRequest(
                "part-one/chapter-one.md",
                TargetPartPath: "part-two/part.md",
                AfterRelativePath: "part-two/chapter-two.md")));

        Assert.Equal(beforeProjection, GetProjectedPaths(board).ToArray());
        Assert.Equal(
            new[]
            {
                "part-one/part.md",
                "part-one/chapter-one.md",
                "part-two/part.md",
                "part-two/chapter-two.md"
            },
            ReadBookTxtLines(context.BookTxtPath));
        Assert.True(File.Exists(AbsolutePath(context.WorkspaceRoot, "part-one/chapter-one.md")));
        Assert.True(File.Exists(AbsolutePath(context.WorkspaceRoot, "part-two/chapter-one.md")));
        Assert.Equal("part-one/chapter-one.md", board.SelectedCard?.RelativePath);
        Assert.Equal("Drop card", board.LastStructuralError?.Operation);
        Assert.Equal("part-one/chapter-one.md", board.LastStructuralError?.Path);
        Assert.Equal(context.BookTxtPath, board.LastStructuralError?.BookTxtPath);
        Assert.Contains("target file", board.LastStructuralError?.Message ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        var error = Assert.Single(context.NotificationService.Errors);
        Assert.Contains(context.BookTxtPath, error);
        Assert.Contains("target file", error, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateWorkspace(params (string Path, string Content)[] files)
    {
        var root = Path.Combine(Path.GetTempPath(), $"hymnal-corkboard-integration-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        foreach (var (relativePath, content) in files)
            WriteWorkspaceFile(root, relativePath, content);

        return root;
    }

    private static void WriteWorkspaceFile(string root, string relativePath, string content)
    {
        var absolutePath = AbsolutePath(root, relativePath);
        var directory = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(absolutePath, content);
    }

    private static string AbsolutePath(string workspaceRoot, string relativePath) =>
        Path.Combine(workspaceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

    private static string[] ReadBookTxtLines(string bookTxtPath) => File.ReadAllLines(bookTxtPath);

    private static IEnumerable<string> GetProjectedPaths(CorkboardViewModel board) =>
        board.Items
            .Where(item => item.Kind is CorkboardItemKind.PartDivider or CorkboardItemKind.ChapterCard)
            .Select(item => item.RelativePath);

    private static ChapterCardItemViewModel GetChapterCard(CorkboardViewModel board, string relativePath) =>
        Assert.IsType<ChapterCardItemViewModel>(board.Items.First(item => item.RelativePath == relativePath));

    private static async Task ExecuteCommandAsync(IObservable<System.Reactive.Unit> execution)
    {
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = execution.Subscribe(
            _ => { },
            ex => completion.TrySetException(ex),
            () => completion.TrySetResult(null));

        await completion.Task;
    }

    private static async Task WaitForBoardProjectionAsync(CorkboardViewModel board, params string[] expectedPaths)
    {
        await WaitUntilAsync(
            () => GetProjectedPaths(board).SequenceEqual(expectedPaths),
            $"Board projection did not match expected order. Expected: {string.Join(", ", expectedPaths)}. Actual: {string.Join(", ", GetProjectedPaths(board))}.");
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, string failureMessage, Func<Task>? settleAsync = null)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (DateTime.UtcNow < deadline && !predicate())
        {
            ReactiveUiTestBootstrap.RunOnUiThread(() => { });
            if (settleAsync != null)
                await settleAsync();
            await Task.Delay(25);
        }

        if (settleAsync != null)
            await settleAsync();

        if (!predicate())
            Assert.Fail(failureMessage);

        ReactiveUiTestBootstrap.RunOnUiThread(() => { });
    }

    private static async Task SeedUuidSidecarsAsync(string workspaceRoot, string uuid)
    {
        var store = new MetadataStore();
        await new PhaseDataService(store).SaveAsync(workspaceRoot, new Dictionary<string, PhaseData>
        {
            [uuid] = new() { Status = ChapterStatus.Drafting }
        });
        await new TargetsService(store).SaveAsync(workspaceRoot, new Dictionary<string, WordCountTarget>
        {
            [uuid] = new() { MinWords = 1000, MaxWords = 1500 }
        });
        await new WordCountHistoryService(store).AppendAsync(workspaceRoot, uuid, "2026-06-04", 1234);

        var notesDirectory = Path.Combine(workspaceRoot, ".hymnal-data", "notes");
        Directory.CreateDirectory(notesDirectory);
        await File.WriteAllTextAsync(Path.Combine(notesDirectory, uuid + ".md"), "UUID keyed note");
    }

    private static async Task AssertUuidSidecarsAsync(string workspaceRoot, string uuid)
    {
        var store = new MetadataStore();
        var phases = await new PhaseDataService(store).LoadAsync(workspaceRoot);
        Assert.Equal(ChapterStatus.Drafting, phases[uuid].Status);

        var targets = await new TargetsService(store).LoadAsync(workspaceRoot);
        Assert.Equal(1000, targets[uuid].MinWords);
        Assert.Equal(1500, targets[uuid].MaxWords);

        var history = await new WordCountHistoryService(store).GetAllAsync(workspaceRoot);
        var historyEntry = Assert.Single(history, entry => entry.Uuid == uuid);
        Assert.Equal("2026-06-04", historyEntry.Date);
        Assert.Equal(1234, historyEntry.WordCount);

        Assert.Equal("UUID keyed note", await File.ReadAllTextAsync(Path.Combine(workspaceRoot, ".hymnal-data", "notes", uuid + ".md")));
    }

    private sealed class TestContext : IDisposable
    {
        private static readonly FieldInfo HydrationTaskField = typeof(WorkspaceViewModel)
            .GetField("_hydrationTask", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Workspace hydration task field was not found.");

        public RecordingNotificationService NotificationService { get; } = new();
        public MetadataStore MetadataStore { get; } = new();
        public InMemoryAppSettingsStore SettingsStore { get; } = new();
        public PhaseDataService PhaseDataService { get; }
        public TargetsService TargetsService { get; }
        public WordCountService WordCountService { get; } = new();
        public StubFolderPickerService FolderPickerService { get; }
        public StubFilePickerService FilePickerService { get; } = new();
        public ExclusionManifestService ExclusionManifestService { get; }
        public ManuscriptService ManuscriptService { get; }
        public OrphanFileDiscoveryService OrphanFileDiscoveryService { get; } = new();
        public EditorViewModel Editor { get; }
        public WorkspaceViewModel Workspace { get; }
        public ChapterRegistryService RegistryService { get; }

        public string WorkspaceRoot { get; }
        public string BookTxtPath => Path.Combine(WorkspaceRoot, "Book.txt");

        public TestContext(string workspaceRoot)
        {
            WorkspaceRoot = workspaceRoot;
            FolderPickerService = new StubFolderPickerService(workspaceRoot);
            ExclusionManifestService = new ExclusionManifestService(MetadataStore);
            PhaseDataService = new PhaseDataService(MetadataStore);
            TargetsService = new TargetsService(MetadataStore);
            Editor = new EditorViewModel(MetadataStore, NotificationService, WordCountService);
            RegistryService = new ChapterRegistryService(MetadataStore);
            var history = new WordCountHistoryService(MetadataStore);
            ManuscriptService = new ManuscriptService(NotificationService, ExclusionManifestService);
            var structure = new BookTxtStructureService(MetadataStore, ExclusionManifestService, RegistryService);

            Workspace = new WorkspaceViewModel(
                ManuscriptService,
                structure,
                FilePickerService,
                SettingsStore,
                FolderPickerService,
                NotificationService,
                Editor,
                RegistryService,
                PhaseDataService,
                TargetsService,
                WordCountService,
                history,
                ExclusionManifestService,
                OrphanFileDiscoveryService);
        }

        public CorkboardViewModel CreateBoard() => new(
            Workspace,
            new BookTxtStructureService(MetadataStore, ExclusionManifestService, RegistryService),
            OrphanFileDiscoveryService,
            SettingsStore,
            NotificationService,
            ManuscriptService);

        public async Task OpenWorkspaceAsync(int expectedNodeCount)
        {
            await ExecuteCommandAsync(Workspace.OpenWorkspaceCommand.Execute());
            await WaitUntilAsync(
                () => Workspace.HasWorkspace && Workspace.Nodes.Count == expectedNodeCount,
                $"Workspace did not load expected node count {expectedNodeCount}. Actual count: {Workspace.Nodes.Count}.");
        }

        public async Task AwaitWorkspaceHydrationAsync()
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);

            while (true)
            {
                var hydrationTask = HydrationTaskField.GetValue(Workspace) as Task;
                if (hydrationTask == null)
                {
                    ReactiveUiTestBootstrap.RunOnUiThread(() => { });
                    return;
                }

                while (!hydrationTask.IsCompleted)
                {
                    if (DateTime.UtcNow >= deadline)
                    {
                        throw new TimeoutException(
                            $"Workspace hydration did not settle. Status={hydrationTask.Status}; nodes={Workspace.Nodes.Count}; hasWorkspace={Workspace.HasWorkspace}.");
                    }

                    ReactiveUiTestBootstrap.RunOnUiThread(() => { });
                    await Task.Delay(10);
                }

                ReactiveUiTestBootstrap.RunOnUiThread(() => { });

                if (ReferenceEquals(hydrationTask, HydrationTaskField.GetValue(Workspace)))
                    return;
            }
        }

        public async Task<ChapterViewModel> WaitForNodeAsync(string relativePath, bool requireUuid = false)
        {
            await WaitUntilAsync(
                () => TryGetNode(relativePath, requireUuid, out _),
                $"Workspace node '{relativePath}' did not appear with requireUuid={requireUuid}.");

            TryGetNode(relativePath, requireUuid, out var node);
            return node!;
        }

        private bool TryGetNode(string relativePath, bool requireUuid, out ChapterViewModel? node)
        {
            node = Workspace.Nodes.FirstOrDefault(candidate =>
                string.Equals(candidate.Node.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase));

            return node != null && (!requireUuid || !string.IsNullOrWhiteSpace(node.Uuid));
        }

        public void Dispose()
        {
            Editor.Dispose();
            ManuscriptService.Dispose();

            try
            {
                if (Directory.Exists(WorkspaceRoot))
                    Directory.Delete(WorkspaceRoot, recursive: true);
            }
            catch
            {
                // Best effort cleanup only.
            }
        }
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

    private sealed class InMemoryAppSettingsStore : IAppSettingsStore
    {
        private readonly Dictionary<string, object?> _values = new(StringComparer.OrdinalIgnoreCase);

        public Task<T?> GetAsync<T>(string key)
        {
            if (_values.TryGetValue(key, out var value) && value is T typed)
                return Task.FromResult<T?>(typed);

            return Task.FromResult<T?>(default);
        }

        public Task SetAsync<T>(string key, T value)
        {
            _values[key] = value;
            return Task.CompletedTask;
        }
    }

    private sealed class StubFolderPickerService : IFolderPickerService
    {
        private readonly string _workspaceRoot;

        public StubFolderPickerService(string workspaceRoot) => _workspaceRoot = workspaceRoot;

        public Task<string?> PickFolderAsync() => Task.FromResult<string?>(_workspaceRoot);
    }

    private sealed class StubFilePickerService : IFilePickerService
    {
        public Task<string?> PickFileAsync(string? suggestedStartDirectory = null) => Task.FromResult<string?>(null);
    }
}

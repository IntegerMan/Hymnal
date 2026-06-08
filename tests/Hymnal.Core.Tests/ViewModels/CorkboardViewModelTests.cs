using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Hymnal.Core.Common;
using Unit = Hymnal.Core.Common.Unit;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;
using Hymnal.Core.Services;
using DynamicData.Binding;
using Hymnal.Core.Tests.Infrastructure;
using ReactiveUI;
using Xunit;

namespace Hymnal.ViewModels;

[Collection("AvaloniaUi")]
public sealed class CorkboardViewModelTests
{
    static CorkboardViewModelTests() => ReactiveUiTestBootstrap.EnsureInitialized();

    [Fact]
    public void Ctor_ProjectsMixedItemsInBookOrder()
    {
        var context = CreateContext();
        SeedWorkspaceNodes(context,
            CreateChapter(context, "part-one/part.md", "Part One", NodeKind.Part),
            CreateChapter(context, "part-one/chapter-one.md", "Chapter One"),
            CreateChapter(context, "part-two/part.md", "Part Two", NodeKind.Part),
            CreateChapter(context, "part-two/chapter-two.md", "Chapter Two"));

        using var board = context.CreateCorkboard();

        Assert.Equal(
            new[]
            {
                CorkboardItemKind.PartDivider,
                CorkboardItemKind.ChapterCard,
                CorkboardItemKind.PartDivider,
                CorkboardItemKind.ChapterCard
            },
            board.Items.Select(item => item.Kind));

        Assert.Equal(
            new[] { "part-one/part.md", "part-one/chapter-one.md", "part-two/part.md", "part-two/chapter-two.md" },
            board.Items.Select(item => item.RelativePath));
    }

    [Fact]
    public void Ctor_IncludesEmptyPartHintsAndEmptyWorkspaceHasNoItems()
    {
        var context = CreateContext();

        using var emptyBoard = context.CreateCorkboard();
        Assert.Empty(emptyBoard.Items);
        Assert.Null(emptyBoard.SelectedCard);

        SeedWorkspaceNodes(context,
            CreateChapter(context, "part-one/part.md", "Part One", NodeKind.Part),
            CreateChapter(context, "part-one/chapter-one.md", "Chapter One"),
            CreateChapter(context, "part-two/part.md", "Part Two", NodeKind.Part));

        using var board = context.CreateCorkboard();

        Assert.Equal(
            new[]
            {
                CorkboardItemKind.PartDivider,
                CorkboardItemKind.ChapterCard,
                CorkboardItemKind.PartDivider,
                CorkboardItemKind.EmptyPartHint
            },
            board.Items.Select(item => item.Kind));
    }

    [Fact]
    public void WorkspaceNodeCollectionChanges_RebuildBoardLive()
    {
        var context = CreateContext();
        SeedWorkspaceNodes(context,
            CreateChapter(context, "part-one/part.md", "Part One", NodeKind.Part),
            CreateChapter(context, "part-one/chapter-one.md", "Chapter One"));

        using var board = context.CreateCorkboard();
        Assert.Equal(2, board.Items.Count);

        AddWorkspaceNode(context, CreateChapter(context, "part-one/chapter-two.md", "Chapter Two"));

        Assert.True(
            SpinWait.SpinUntil(() => board.Items.Count == 3, TimeSpan.FromSeconds(2)),
            "Corkboard items did not rebuild after workspace node collection change.");
        Assert.Contains(board.Items, item => item.RelativePath == "part-one/chapter-two.md");
    }

    [Fact]
    public async Task SelectCardCommand_EnforcesSelectionExclusivity()
    {
        var context = CreateContext();
        SeedWorkspaceNodes(context,
            CreateChapter(context, "part-one/part.md", "Part One", NodeKind.Part),
            CreateChapter(context, "part-one/chapter-one.md", "Chapter One"),
            CreateChapter(context, "part-one/chapter-two.md", "Chapter Two"));

        using var board = context.CreateCorkboard();
        var first = GetChapterCard(board, "part-one/chapter-one.md");
        var second = GetChapterCard(board, "part-one/chapter-two.md");

        await ExecuteCommandAsync(board.SelectCardCommand.Execute(first));
        Assert.Same(first.Card, board.SelectedCard);
        Assert.True(first.Card.IsSelected);
        Assert.False(second.Card.IsSelected);

        await ExecuteCommandAsync(board.SelectCardCommand.Execute(second));
        Assert.Same(second.Card, board.SelectedCard);
        Assert.False(first.Card.IsSelected);
        Assert.True(second.Card.IsSelected);
    }

    [Fact]
    public async Task SelectCardCommand_IgnoresPartItems()
    {
        var context = CreateContext();
        SeedWorkspaceNodes(context,
            CreateChapter(context, "part-one/part.md", "Part One", NodeKind.Part),
            CreateChapter(context, "part-one/chapter-one.md", "Chapter One"));

        using var board = context.CreateCorkboard();
        var chapter = GetChapterCard(board, "part-one/chapter-one.md");
        var part = Assert.IsType<PartDividerItemViewModel>(board.Items[0]);

        await ExecuteCommandAsync(board.SelectCardCommand.Execute(chapter));
        Assert.Same(chapter.Card, board.SelectedCard);

        await ExecuteCommandAsync(board.SelectCardCommand.Execute(part));
        Assert.Same(chapter.Card, board.SelectedCard);
        Assert.True(chapter.Card.IsSelected);
    }

    [Fact]
    public void OpenSelectedCardCommand_WithNoSelection_DoesNotEmit()
    {
        var context = CreateContext();
        SeedWorkspaceNodes(context,
            CreateChapter(context, "part-one/part.md", "Part One", NodeKind.Part),
            CreateChapter(context, "part-one/chapter-one.md", "Chapter One"));

        using var board = context.CreateCorkboard();
        var emitted = 0;
        using var sub = board.OpenChapterRequested.Subscribe(_ => emitted++);

        Assert.False(WaitForFirstCanExecute(board.OpenSelectedCardCommand));

        Assert.Equal(0, emitted);
    }

    [Fact]
    public async Task OpenSelectedCardCommand_EmitsSelectedChapter()
    {
        var context = CreateContext();
        var chapterNode = CreateChapter(context, "part-one/chapter-one.md", "Chapter One");
        SeedWorkspaceNodes(context,
            CreateChapter(context, "part-one/part.md", "Part One", NodeKind.Part),
            chapterNode,
            CreateChapter(context, "part-one/chapter-two.md", "Chapter Two"));

        using var board = context.CreateCorkboard();
        var card = GetChapterCard(board, "part-one/chapter-one.md");
        ChapterViewModel? opened = null;
        using var sub = board.OpenChapterRequested.Subscribe(chapter => opened = chapter);

        await ExecuteCommandAsync(board.SelectCardCommand.Execute(card));
        await ExecuteCommandAsync(board.OpenSelectedCardCommand.Execute());

        Assert.Same(card.Card.Chapter, opened);
        Assert.Same(card.Card, board.SelectedCard);
    }

    [Fact]
    public async Task OpenCardCommand_EmitsSpecificChapter()
    {
        var context = CreateContext();
        var chapterNode = CreateChapter(context, "part-one/chapter-one.md", "Chapter One");
        SeedWorkspaceNodes(context,
            CreateChapter(context, "part-one/part.md", "Part One", NodeKind.Part),
            chapterNode,
            CreateChapter(context, "part-one/chapter-two.md", "Chapter Two"));

        using var board = context.CreateCorkboard();
        var card = GetChapterCard(board, "part-one/chapter-two.md");
        ChapterViewModel? opened = null;
        using var sub = board.OpenChapterRequested.Subscribe(chapter => opened = chapter);

        await ExecuteCommandAsync(board.OpenCardCommand.Execute(card));

        Assert.Same(card.Card.Chapter, opened);
        Assert.Same(card.Card, board.SelectedCard);
    }

    [Fact]
    public async Task ReorderCardCommand_CallsStructureServiceAndReorders()
    {
        var context = CreateContext();
        context.EnableWorkspace();
        SeedWorkspaceNodes(context,
            CreateChapter(context, "part-one/part.md", "Part One", NodeKind.Part),
            CreateChapter(context, "part-one/chapter-one.md", "Chapter One"),
            CreateChapter(context, "part-one/chapter-two.md", "Chapter Two"),
            CreateChapter(context, "part-one/chapter-three.md", "Chapter Three"));

        using var board = context.CreateCorkboard();
        var card = GetChapterCard(board, "part-one/chapter-one.md");
        await ExecuteCommandAsync(board.SelectCardCommand.Execute(card));

        await ExecuteCommandAsync(board.ReorderCardCommand.Execute(
            new ReorderCardRequest("part-one/chapter-one.md", AfterRelativePath: "part-one/chapter-three.md")));

        Assert.Equal(1, context.StructureService.ReorderCalls.Count);
        Assert.Equal((context.Workspace.BookTxtPath, "part-one/chapter-one.md", 3), context.StructureService.ReorderCalls[0]);
        Assert.Equal(1, context.Workspace.ReorderCount);
        Assert.Equal(0, context.Workspace.ReloadCount);
        Assert.Same(card.Card, board.SelectedCard);
        Assert.Null(board.LastStructuralError);
    }

    [Fact]
    public async Task StructuralFailure_LeavesSelectionIntactAndSurfacesError()
    {
        var context = CreateContext();
        context.EnableWorkspace();
        context.StructureService.ReorderResult = Result<Unit>.Fail("disk write exploded");
        SeedWorkspaceNodes(context,
            CreateChapter(context, "part-one/part.md", "Part One", NodeKind.Part),
            CreateChapter(context, "part-one/chapter-one.md", "Chapter One"),
            CreateChapter(context, "part-one/chapter-two.md", "Chapter Two"));

        using var board = context.CreateCorkboard();
        var card = GetChapterCard(board, "part-one/chapter-one.md");
        await ExecuteCommandAsync(board.SelectCardCommand.Execute(card));
        var itemsBefore = board.Items.Select(item => item.RelativePath).ToArray();

        await ExecuteCommandAsync(board.ReorderCardCommand.Execute(
            new ReorderCardRequest("part-one/chapter-one.md", NewIndex: 1)));

        Assert.Same(card.Card, board.SelectedCard);
        Assert.Equal(itemsBefore, board.Items.Select(item => item.RelativePath));
        Assert.Equal(1, context.NotificationService.Errors.Count);
        Assert.Contains("disk write exploded", context.NotificationService.Errors[0]);
        Assert.Equal("Reorder card", board.LastStructuralError?.Operation);
        Assert.Equal("part-one/chapter-one.md", board.LastStructuralError?.Path);
        Assert.Equal(context.Workspace.BookTxtPath, board.LastStructuralError?.BookTxtPath);
        Assert.Equal("disk write exploded", board.LastStructuralError?.Message);
        Assert.Equal(0, context.Workspace.ReloadCount);
    }

    [Fact]
    public void CardDisplaySize_DefaultsToLarge()
    {
        var context = CreateContext();
        using var board = context.CreateCorkboard();

        Assert.Equal(CardDisplaySize.Large, board.CardDisplaySize);
    }

    [Fact]
    public async Task SetCardSizeCommand_UpdatesCardDisplaySize()
    {
        var context = CreateContext();
        using var board = context.CreateCorkboard();

        await ExecuteCommandAsync(board.SetCardSizeCommand.Execute(CardDisplaySize.Medium));
        Assert.Equal(CardDisplaySize.Medium, board.CardDisplaySize);

        await ExecuteCommandAsync(board.SetCardSizeCommand.Execute(CardDisplaySize.Tiny));
        Assert.Equal(CardDisplaySize.Tiny, board.CardDisplaySize);

        await ExecuteCommandAsync(board.SetCardSizeCommand.Execute(CardDisplaySize.List));
        Assert.Equal(CardDisplaySize.List, board.CardDisplaySize);

        await ExecuteCommandAsync(board.SetCardSizeCommand.Execute(CardDisplaySize.Large));
        Assert.Equal(CardDisplaySize.Large, board.CardDisplaySize);
    }

    [Fact]
    public async Task TogglePartExpandedCommand_CollapsesAndExpandsChildCards()
    {
        var context = CreateContext();
        SeedWorkspaceNodes(context,
            CreateChapter(context, "part-one/part.md", "Part One", NodeKind.Part),
            CreateChapter(context, "part-one/chapter-one.md", "Chapter One"),
            CreateChapter(context, "part-one/chapter-two.md", "Chapter Two"));

        using var board = context.CreateCorkboard();
        var part = Assert.IsType<PartDividerItemViewModel>(board.Items[0]);
        var first = GetChapterCard(board, "part-one/chapter-one.md");
        var second = GetChapterCard(board, "part-one/chapter-two.md");

        Assert.Equal(2, part.ChapterCount);
        Assert.True(part.IsExpanded);
        Assert.True(first.IsVisibleOnBoard);
        Assert.True(second.IsVisibleOnBoard);

        await ExecuteCommandAsync(board.TogglePartExpandedCommand.Execute(part));

        Assert.False(part.IsExpanded);
        Assert.False(first.IsVisibleOnBoard);
        Assert.False(second.IsVisibleOnBoard);

        await ExecuteCommandAsync(board.TogglePartExpandedCommand.Execute(part));

        Assert.True(part.IsExpanded);
        Assert.True(first.IsVisibleOnBoard);
        Assert.True(second.IsVisibleOnBoard);
    }

    [Fact]
    public async Task RebuildItems_PreservesCollapsedPartState()
    {
        var context = CreateContext();
        SeedWorkspaceNodes(context,
            CreateChapter(context, "part-one/part.md", "Part One", NodeKind.Part),
            CreateChapter(context, "part-one/chapter-one.md", "Chapter One"));

        using var board = context.CreateCorkboard();
        var part = Assert.IsType<PartDividerItemViewModel>(board.Items[0]);
        await ExecuteCommandAsync(board.TogglePartExpandedCommand.Execute(part));
        Assert.False(part.IsExpanded);

        AddWorkspaceNode(context, CreateChapter(context, "part-one/chapter-two.md", "Chapter Two"));

        Assert.True(
            SpinWait.SpinUntil(
                () => board.Items.Any(item => item.RelativePath == "part-one/chapter-two.md"),
                TimeSpan.FromSeconds(2)),
            "Corkboard items did not rebuild after workspace node collection change.");

        part = Assert.IsType<PartDividerItemViewModel>(board.Items[0]);
        var second = GetChapterCard(board, "part-one/chapter-two.md");

        Assert.False(part.IsExpanded);
        Assert.False(second.IsVisibleOnBoard);
    }

    [Fact]
    public async Task DeleteChapterCommand_RequiresConfirmation()
    {
        var context = CreateContext();
        context.EnableWorkspace();
        SeedWorkspaceNodes(context,
            CreateChapter(context, "part-one/part.md", "Part One", NodeKind.Part),
            CreateChapter(context, "part-one/chapter-one.md", "Chapter One"));

        using var board = context.CreateCorkboard();
        var card = GetChapterCard(board, "part-one/chapter-one.md");
        await ExecuteCommandAsync(board.SelectCardCommand.Execute(card));

        await ExecuteCommandAsync(board.DeleteChapterCommand.Execute(
            new DeleteChapterRequest("part-one/chapter-one.md", Confirmed: false)));

        Assert.Empty(context.StructureService.DeleteCalls);
        Assert.Equal(1, context.NotificationService.Errors.Count);
        Assert.Contains("confirmation was not provided", context.NotificationService.Errors[0]);
        Assert.Equal("Delete chapter", board.LastStructuralError?.Operation);
        Assert.Equal("part-one/chapter-one.md", board.LastStructuralError?.Path);
        Assert.Equal(0, context.Workspace.ReloadCount);
    }

    [Fact]
    public void GetBookChapterInsertIndex_WithParts_ReturnsIndexOfFirstPart()
    {
        var context = CreateContext();
        SeedWorkspaceNodes(context,
            CreateChapter(context, "prologue.md", "Prologue"),
            CreateChapter(context, "part-one/part.md", "Part One", NodeKind.Part),
            CreateChapter(context, "part-one/chapter-one.md", "Chapter One"),
            CreateChapter(context, "part-two/part.md", "Part Two", NodeKind.Part),
            CreateChapter(context, "part-two/chapter-two.md", "Chapter Two"));

        using var board = context.CreateCorkboard();

        // The first Part is at node index 1 (prologue is 0).
        Assert.Equal(1, board.GetBookChapterInsertIndex());
    }

    [Fact]
    public void GetBookChapterInsertIndex_WithNoParts_ReturnsNodeCount()
    {
        var context = CreateContext();
        SeedWorkspaceNodes(context,
            CreateChapter(context, "chapter-one.md", "Chapter One"),
            CreateChapter(context, "chapter-two.md", "Chapter Two"));

        using var board = context.CreateCorkboard();

        Assert.Equal(2, board.GetBookChapterInsertIndex());
    }

    [Fact]
    public void GetBookChapterInsertIndex_WithEmptyBook_ReturnsZero()
    {
        var context = CreateContext();

        using var board = context.CreateCorkboard();

        Assert.Equal(0, board.GetBookChapterInsertIndex());
    }

    private static TestContext CreateContext() => new();

    private static ChapterViewModel CreateChapter(TestContext context, string relativePath, string title, NodeKind kind = NodeKind.Chapter)
    {
        var node = new ChapterNode(relativePath, relativePath, title, kind, false, 0);
        return new ChapterViewModel(
            node,
            Guid.NewGuid().ToString("N"),
            null,
            context.PhaseDataService,
            context.TargetsService,
            context.SettingsStore,
            context.NotificationService,
            context.WorkspaceRoot);
    }

    private static void SeedWorkspaceNodes(TestContext context, params ChapterViewModel[] nodes)
    {
        var collection = GetWorkspaceNodeCollection(context.Workspace);
        collection.Clear();
        foreach (var node in nodes)
            collection.Add(node);
    }

    private static void AddWorkspaceNode(TestContext context, ChapterViewModel node)
    {
        GetWorkspaceNodeCollection(context.Workspace).Add(node);
    }

    private static ChapterCardItemViewModel GetChapterCard(CorkboardViewModel board, string relativePath)
        => Assert.IsType<ChapterCardItemViewModel>(board.Items.First(item => item.RelativePath == relativePath));

    private static IList GetWorkspaceNodeCollection(WorkspaceViewModel workspace)
    {
        var field = typeof(WorkspaceViewModel).GetField("_nodes", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Workspace node collection field was not found.");

        return (IList)(field.GetValue(workspace)
            ?? throw new InvalidOperationException("Workspace node collection was null."));
    }

    private static async Task ExecuteCommandAsync(IObservable<System.Reactive.Unit> execution)
    {
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = execution.Subscribe(
            _ => { },
            ex => completion.TrySetException(ex),
            () => completion.TrySetResult(null));

        await completion.Task;
    }

    private static bool WaitForFirstCanExecute(ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> command)
    {
        var observed = false;
        using var subscription = command.CanExecute.Subscribe(value => observed = value);
        return observed;
    }

    private sealed class TestContext
    {
        public FakeNotificationService NotificationService { get; } = new();
        public FakeMetadataStore MetadataStore { get; } = new();
        public FakeAppSettingsStore SettingsStore { get; } = new();
        public FakeFolderPickerService FolderPickerService { get; } = new();
        public WordCountService WordCountService { get; } = new();
        public EditorViewModel EditorViewModel { get; }
        public ChapterRegistryService RegistryService { get; }
        public PhaseDataService PhaseDataService { get; }
        public TargetsService TargetsService { get; }
        public WordCountHistoryService HistoryService { get; }
        public ManuscriptService ManuscriptService { get; }
        public FakeBookTxtStructureService StructureService { get; } = new();
        public SpyWorkspaceViewModel Workspace { get; }
        public string WorkspaceRoot { get; } = Path.Combine("C:", "Dev", "Hymnal", "tests", "workspace");
        public string ManuscriptRoot { get; } = Path.Combine("C:", "Dev", "Hymnal", "tests", "workspace", "manuscript");

        public TestContext()
        {
            EditorViewModel = new EditorViewModel(MetadataStore, NotificationService, WordCountService);
            RegistryService = new ChapterRegistryService(MetadataStore);
            PhaseDataService = new PhaseDataService(MetadataStore);
            TargetsService = new TargetsService(MetadataStore);
            HistoryService = new WordCountHistoryService(MetadataStore);
            ManuscriptService = new ManuscriptService(NotificationService);
            Workspace = new SpyWorkspaceViewModel(
                ManuscriptService,
                StructureService,
                new FakeFilePickerService(),
                SettingsStore,
                FolderPickerService,
                NotificationService,
                EditorViewModel,
                RegistryService,
                PhaseDataService,
                TargetsService,
                WordCountService,
                HistoryService);
        }

        public CorkboardViewModel CreateCorkboard() => new(
            Workspace,
            StructureService,
            new OrphanFileDiscoveryService(),
            SettingsStore,
            NotificationService,
            ManuscriptService);

        public void EnableWorkspace()
        {
            var model = new ManuscriptModel();
            model.SetRoots(WorkspaceRoot, ManuscriptRoot);
            SetPrivateField(Workspace, "_model", model);
            SetPrivateField(Workspace, "_hasWorkspace", true);
        }
    }

    private sealed class SpyWorkspaceViewModel : WorkspaceViewModel
    {
        public int ReloadCount { get; private set; }
        public int ReorderCount { get; private set; }
        public Result<Unit> ReloadResult { get; set; } = Result<Unit>.Ok(Unit.Default);
        public Result<Unit> ReorderResult { get; set; } = Result<Unit>.Ok(Unit.Default);

        public SpyWorkspaceViewModel(
            ManuscriptService manuscriptService,
            IBookTxtStructureService structureService,
            IFilePickerService filePicker,
            IAppSettingsStore settingsStore,
            IFolderPickerService folderPicker,
            INotificationService notificationService,
            EditorViewModel editor,
            ChapterRegistryService registryService,
            PhaseDataService phaseDataService,
            TargetsService targetsService,
            WordCountService wordCountService,
            WordCountHistoryService historyService)
            : base(manuscriptService, structureService, filePicker, settingsStore, folderPicker, notificationService, editor, registryService, phaseDataService, targetsService, wordCountService, historyService)
        {
        }

        public override Task<Result<Unit>> ReloadCurrentWorkspaceAsync()
        {
            ReloadCount++;
            return Task.FromResult(ReloadResult);
        }

        public override Task<Result<Unit>> ReorderNodesAsync()
        {
            ReorderCount++;
            return Task.FromResult(ReorderResult);
        }
    }

    private sealed class FakeNotificationService : INotificationService
    {
        public List<string> Errors { get; } = new();
        public List<string> Infos { get; } = new();
        public List<string> Successes { get; } = new();

        public void ShowError(string message) => Errors.Add(message);
        public void ShowInfo(string message) => Infos.Add(message);
        public void ShowSuccess(string message) => Successes.Add(message);
    }

    private sealed class FakeMetadataStore : IMetadataStore
    {
        public List<(string Path, string Content)> Writes { get; } = new();

        public Task WriteTextAtomicAsync(string absolutePath, string content)
        {
            Writes.Add((absolutePath, content));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAppSettingsStore : IAppSettingsStore
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

    private sealed class FakeFolderPickerService : IFolderPickerService
    {
        public Task<string?> PickFolderAsync() => Task.FromResult<string?>(null);
    }

    private sealed class FakeFilePickerService : IFilePickerService
    {
        public Task<string?> PickFileAsync(string? suggestedStartDirectory = null)
            => Task.FromResult<string?>(null);
    }

    private sealed class FakeBookTxtStructureService : IBookTxtStructureService
    {
        public List<(string BookTxtPath, string ChapterPath, int NewIndex)> ReorderCalls { get; } = new();
        public List<(string BookTxtPath, string ExistingPath, string ReplacementPath)> RenameCalls { get; } = new();
        public List<(string BookTxtPath, string ChapterPath, string Content, int Index)> CreateCalls { get; } = new();
        public List<(string BookTxtPath, string ChapterPath, int Index)> IncludeIndexCalls { get; } = new();
        public List<(string BookTxtPath, string ChapterPath, string PartPath)> IncludePartCalls { get; } = new();
        public List<(string BookTxtPath, string ChapterPath)> RemoveCalls { get; } = new();
        public List<(string BookTxtPath, string ChapterPath)> DeleteCalls { get; } = new();

        public Result<Unit> ReorderResult { get; set; } = Result<Unit>.Ok(Unit.Default);
        public Result<Unit> RenameResult { get; set; } = Result<Unit>.Ok(Unit.Default);
        public Result<Unit> CreateResult { get; set; } = Result<Unit>.Ok(Unit.Default);
        public Result<Unit> IncludeIndexResult { get; set; } = Result<Unit>.Ok(Unit.Default);
        public Result<Unit> IncludePartResult { get; set; } = Result<Unit>.Ok(Unit.Default);
        public Result<Unit> RemoveResult { get; set; } = Result<Unit>.Ok(Unit.Default);
        public Result<Unit> DeleteResult { get; set; } = Result<Unit>.Ok(Unit.Default);

        public Task<Result<IReadOnlyList<string>>> ReadNormalizedEntriesAsync(string bookTxtPath)
            => Task.FromResult(Result<IReadOnlyList<string>>.Ok(Array.Empty<string>()));

        public Task<Result<Unit>> ReorderEntryAsync(string bookTxtPath, string chapterPath, int newIndex)
        {
            ReorderCalls.Add((bookTxtPath, chapterPath, newIndex));
            return Task.FromResult(ReorderResult);
        }

        public Task<Result<Unit>> RenameEntryAsync(string bookTxtPath, string existingPath, string replacementPath)
        {
            RenameCalls.Add((bookTxtPath, existingPath, replacementPath));
            return Task.FromResult(RenameResult);
        }

        public Task<Result<Unit>> AddExistingEntryAsync(string bookTxtPath, string chapterPath, int index)
        {
            IncludeIndexCalls.Add((bookTxtPath, chapterPath, index));
            return Task.FromResult(IncludeIndexResult);
        }

        public Task<Result<Unit>> AddExistingEntryAfterPartAsync(string bookTxtPath, string chapterPath, string partPath)
        {
            IncludePartCalls.Add((bookTxtPath, chapterPath, partPath));
            return Task.FromResult(IncludePartResult);
        }

        public Task<Result<Unit>> CreateNewChapterAsync(string bookTxtPath, string chapterPath, string content, int index)
        {
            CreateCalls.Add((bookTxtPath, chapterPath, content, index));
            return Task.FromResult(CreateResult);
        }

        public Task<Result<Unit>> CreateNewPartAsync(string bookTxtPath, string partPath, string title, int index)
            => Task.FromResult(CreateResult);

        public Task<Result<Unit>> RemoveEntryAsync(string bookTxtPath, string chapterPath)
        {
            RemoveCalls.Add((bookTxtPath, chapterPath));
            return Task.FromResult(RemoveResult);
        }

        public Task<Result<Unit>> DeleteChapterFileAsync(string bookTxtPath, string chapterPath)
        {
            DeleteCalls.Add((bookTxtPath, chapterPath));
            return Task.FromResult(DeleteResult);
        }
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        var type = target.GetType();
        FieldInfo? field = null;

        while (type != null && field == null)
        {
            field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            type = type.BaseType;
        }

        if (field == null)
            throw new InvalidOperationException($"Field '{fieldName}' was not found on {target.GetType().Name}.");

        field.SetValue(target, value);
    }
}

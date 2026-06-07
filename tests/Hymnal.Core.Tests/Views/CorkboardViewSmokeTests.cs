using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Hymnal.Core.Common;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Tests.Infrastructure;
using Hymnal.Core.Models;
using Hymnal.Core.Services;
using Hymnal.ViewModels;
using Hymnal.Views;
using Xunit;

namespace Hymnal.Core.Tests.Views;

[Collection("AvaloniaUi")]
public sealed class CorkboardViewSmokeTests
{
    static CorkboardViewSmokeTests() => ReactiveUiTestBootstrap.EnsureInitialized();

    [Fact]
    public void CorkboardView_LoadsXaml_AndTogglesEmptyStateWithDataContext()
    {
        ReactiveUiTestBootstrap.RunOnUiThread(() =>
        {
            using var context = new TestContext();
            var emptyBoard = context.CreateBoard();

            var emptyView = new CorkboardView
            {
                DataContext = emptyBoard
            };

            LayoutView(emptyView);

            Assert.False(emptyBoard.HasItems);
            Assert.NotNull(emptyView.FindControl<TextBlock>("EmptyBoardState"));
            Assert.True(emptyView.FindControl<TextBlock>("EmptyBoardState")!.IsVisible);
            Assert.False(emptyView.FindControl<ScrollViewer>("BoardScroll")!.IsVisible);
            Assert.Same(emptyBoard.Items, emptyView.FindControl<ItemsControl>("BoardItems")!.ItemsSource);

            var chapter = context.AddChapter("part-one/chapter-one.md", "Chapter One");
            context.SeedWorkspace(chapter);

            var populatedBoard = context.CreateBoard();
            var populatedView = new CorkboardView
            {
                DataContext = populatedBoard
            };

            LayoutView(populatedView);

            Assert.True(populatedBoard.HasItems);
            Assert.False(populatedView.FindControl<TextBlock>("EmptyBoardState")!.IsVisible);
            Assert.True(populatedView.FindControl<ScrollViewer>("BoardScroll")!.IsVisible);
            Assert.Same(populatedBoard.Items, populatedView.FindControl<ItemsControl>("BoardItems")!.ItemsSource);
            Assert.Equal(CardDisplaySize.Large, populatedBoard.CardDisplaySize);
        });
    }

    // Manual smoke checklist for UI interaction coverage that is brittle to automate:
    // 1. Open a sample workspace and click PLAN.
    // 2. Verify Part dividers and chapter cards render compactly in the center panel.
    // 3. Single-click a chapter card and confirm it is selected without switching to Write mode.
    // 4. Double-click the same card and confirm Write mode opens that chapter.
    // 5. Click a part header to collapse and expand its chapter cards; verify chapter count on the header.
    // 6. Right-click a non-Done card and use Mark complete from the context menu.
    // 7. Use the context menu for Rename / New Chapter / Include Existing Chapter / Remove from Book.
    // 8. Trigger Delete Chapter File and confirm cancellation does not delete.
    // 9. Drag a card onto another card to reorder, and drop onto invalid targets to confirm they are ignored.

    private static void LayoutView(Control view)
    {
        view.ApplyTemplate();
        view.Measure(new Size(1280, 800));
        view.Arrange(new Rect(0, 0, 1280, 800));
        view.UpdateLayout();
    }

    private sealed class TestContext : IDisposable
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
        public string WorkspaceRoot { get; }
        public string ManuscriptRoot { get; }

        public TestContext()
        {
            var root = Path.Combine(Path.GetTempPath(), "hymnal-corkboard-view-smoke-tests", Guid.NewGuid().ToString("N"));
            WorkspaceRoot = root;
            ManuscriptRoot = Path.Combine(root, "manuscript");
            Directory.CreateDirectory(ManuscriptRoot);

            EditorViewModel = new EditorViewModel(MetadataStore, NotificationService, WordCountService);
            RegistryService = new ChapterRegistryService(MetadataStore);
            PhaseDataService = new PhaseDataService(MetadataStore);
            TargetsService = new TargetsService(MetadataStore);
            HistoryService = new WordCountHistoryService(MetadataStore);
            ManuscriptService = new ManuscriptService(NotificationService);
            Workspace = new SpyWorkspaceViewModel(
                ManuscriptService,
                SettingsStore,
                FolderPickerService,
                NotificationService,
                EditorViewModel,
                RegistryService,
                PhaseDataService,
                TargetsService,
                WordCountService,
                HistoryService);

            SeedWorkspace(Array.Empty<ChapterViewModel>());
            SetWorkspaceModel(false);
        }

        public ChapterViewModel AddChapter(string relativePath, string title)
        {
            var absolutePath = Path.Combine(ManuscriptRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
            File.WriteAllText(absolutePath, $"# {title}\n\nBody text.");

            var node = new ChapterNode(relativePath, relativePath, title, NodeKind.Chapter, IsMissing: false, Index: 0);
            return new ChapterViewModel(
                node,
                uuid: Guid.NewGuid().ToString("N"),
                phaseData: null,
                PhaseDataService,
                TargetsService,
                SettingsStore,
                NotificationService,
                WorkspaceRoot);
        }

        public void SeedWorkspace(params ChapterViewModel[] nodes)
        {
            var collection = GetWorkspaceNodeCollection(Workspace);
            collection.Clear();
            foreach (var node in nodes)
                collection.Add(node);

            SetWorkspaceModel(nodes.Length > 0);
        }

        public CorkboardViewModel CreateBoard() => new(Workspace, StructureService, NotificationService, ManuscriptService);

        private void SetWorkspaceModel(bool hasWorkspace)
        {
            var model = new ManuscriptModel();
            model.SetRoots(WorkspaceRoot, ManuscriptRoot);

            SetPrivateField(Workspace, "_model", model);
            SetPrivateField(Workspace, "_hasWorkspace", hasWorkspace);
            SetPrivateField(Workspace, "_workspaceName", Path.GetFileName(WorkspaceRoot));
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(WorkspaceRoot))
                    Directory.Delete(WorkspaceRoot, recursive: true);
            }
            catch
            {
                // Best effort cleanup.
            }
        }
    }

    private sealed class SpyWorkspaceViewModel : WorkspaceViewModel
    {
        public SpyWorkspaceViewModel(
            ManuscriptService manuscriptService,
            IAppSettingsStore settingsStore,
            IFolderPickerService folderPicker,
            INotificationService notificationService,
            EditorViewModel editor,
            ChapterRegistryService registryService,
            PhaseDataService phaseDataService,
            TargetsService targetsService,
            WordCountService wordCountService,
            WordCountHistoryService historyService)
            : base(manuscriptService, settingsStore, folderPicker, notificationService, editor, registryService, phaseDataService, targetsService, wordCountService, historyService)
        {
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

    private static IList GetWorkspaceNodeCollection(WorkspaceViewModel workspace)
    {
        var field = typeof(WorkspaceViewModel).GetField("_nodes", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Workspace node collection field was not found.");

        return (IList)(field.GetValue(workspace)
            ?? throw new InvalidOperationException("Workspace node collection was null."));
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        var field = typeof(WorkspaceViewModel).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field '{fieldName}' not found on WorkspaceViewModel.");

        field.SetValue(target, value);
    }
}

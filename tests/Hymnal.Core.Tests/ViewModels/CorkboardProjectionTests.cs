using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;
using Hymnal.Core.Services;
using Hymnal.ViewModels;
using NSubstitute;
using ReactiveUI.Builder;

namespace Hymnal.Core.Tests.ViewModels;

public class CorkboardProjectionTests
{
    private sealed class FakeMetadataStore : IMetadataStore
    {
        public Task WriteTextAtomicAsync(string absolutePath, string content)
        {
            var directory = Path.GetDirectoryName(absolutePath)!;
            Directory.CreateDirectory(directory);
            var tempPath = absolutePath + ".tmp";
            return File.WriteAllTextAsync(tempPath, content)
                .ContinueWith(_ => File.Move(tempPath, absolutePath, overwrite: true));
        }
    }

    static CorkboardProjectionTests()
    {
        RxAppBuilder.CreateReactiveUIBuilder()
            .WithCoreServices()
            .BuildApp();
    }

    private static ChapterNode MakeNode(
        string title = "Chapter One",
        NodeKind kind = NodeKind.Chapter,
        string path = "ch01.md",
        bool missing = false,
        int index = 0) =>
        new ChapterNode(path, path, title, kind, missing, index);

    private static PhaseData MakePhase(
        ChapterStatus status = ChapterStatus.Drafting,
        string? start = null,
        string? end = null) =>
        new PhaseData { Status = status, PhaseStartDate = start, PhaseEndDate = end };

    private static ChapterViewModel CreateChapterViewModel(
        PhaseData? phaseData,
        WordCountTarget? target = null,
        string title = "Chapter One",
        string path = "ch01.md",
        string uuid = "uuid-1",
        bool missing = false,
        NodeKind kind = NodeKind.Chapter)
    {
        var metadataStore = new FakeMetadataStore();
        var phaseDataService = new PhaseDataService(metadataStore);
        var targetsService = new TargetsService(metadataStore);
        var settingsStore = Substitute.For<IAppSettingsStore>();
        var notificationService = Substitute.For<INotificationService>();

        return new ChapterViewModel(
            MakeNode(title: title, kind: kind, path: path, missing: missing),
            uuid,
            phaseData,
            phaseDataService,
            targetsService,
            settingsStore,
            notificationService,
            workspaceRoot: Path.Combine(Path.GetTempPath(), "hymnal-corkboard-tests"),
            target: target);
    }

    [Fact]
    public void Project_EmptyWorkspace_ReturnsNoItems()
    {
        var items = CorkboardItemViewModel.Project(Array.Empty<ChapterViewModel>());

        Assert.Empty(items);
    }

    [Fact]
    public void Project_MixedRows_EmitsDividersCardsAndEmptyPartHints()
    {
        var part1 = CreateChapterViewModel(
            phaseData: null,
            title: "Part One",
            path: "part-01.md",
            uuid: "part-1",
            kind: NodeKind.Part);
        var part2 = CreateChapterViewModel(
            phaseData: null,
            title: "Part Two",
            path: "part-02.md",
            uuid: "part-2",
            kind: NodeKind.Part);
        var chapter = CreateChapterViewModel(
            phaseData: MakePhase(status: ChapterStatus.Drafting, start: "2024-01-01", end: "2024-02-01"),
            title: "Chapter One",
            path: "ch01.md",
            uuid: "ch-1");
        var part3 = CreateChapterViewModel(
            phaseData: null,
            title: "Part Three",
            path: "part-03.md",
            uuid: "part-3",
            kind: NodeKind.Part);

        var nodes = new[] { part1, part2, chapter, part3 };
        var items = CorkboardItemViewModel.Project(nodes);

        Assert.Equal(6, items.Count);
        Assert.IsType<PartDividerItemViewModel>(items[0]);
        Assert.IsType<EmptyPartHintItemViewModel>(items[1]);
        Assert.IsType<PartDividerItemViewModel>(items[2]);
        var cardItem = Assert.IsType<ChapterCardItemViewModel>(items[3]);
        Assert.IsType<PartDividerItemViewModel>(items[4]);
        Assert.IsType<EmptyPartHintItemViewModel>(items[5]);

        Assert.False(items[0].IsSelectableCard);
        Assert.False(items[1].IsSelectableCard);
        Assert.False(items[2].IsSelectableCard);
        Assert.True(cardItem.IsSelectableCard);
        Assert.Equal("Chapter One", cardItem.Card.Title);
        Assert.Equal("ch01.md", cardItem.Card.RelativePath);
    }

    [Fact]
    public async Task CardViewModel_ProjectsLiveDisplayValuesAndFallbacks()
    {
        var chapter = CreateChapterViewModel(
            phaseData: MakePhase(status: ChapterStatus.Drafting, start: "bad-date", end: null),
            title: "Original Chapter",
            path: "ch01.md",
            uuid: "uuid-1",
            missing: false);

        var card = new CardViewModel(chapter);

        Assert.Equal("Original Chapter", card.Title);
        Assert.Equal("ch01.md", card.RelativePath);
        Assert.Equal(ChapterStatus.Drafting, card.Status);
        Assert.Equal("Drafting", card.StatusDisplay);
        Assert.Equal("corkboard-status-drafting", card.StatusBrushKey);
        Assert.Equal("—", card.WordCountDisplay);
        Assert.Equal("No target", card.TargetDisplay);
        Assert.Equal(0.0, card.ProximityFill);
        Assert.Equal("—", card.PhaseStartDateDisplay);
        Assert.Equal("—", card.PhaseEndDateDisplay);
        Assert.False(card.IsMissing);
        Assert.Equal(string.Empty, card.MissingStateDisplay);
        Assert.False(card.IsSelected);

        chapter.UpdateWordCount(1200);
        Assert.Equal("1,200 w", card.WordCountDisplay);

        chapter.UpdateNode(chapter.Node with
        {
            Title = "Renamed Chapter",
            RelativePath = "renamed/ch01.md",
            IsMissing = true
        });

        Assert.Equal("Renamed Chapter", card.Title);
        Assert.Equal("renamed/ch01.md", card.RelativePath);
        Assert.True(card.IsMissing);
        Assert.Equal("Missing file", card.MissingStateDisplay);

        typeof(ChapterViewModel)
            .GetProperty(nameof(ChapterViewModel.Target), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(chapter, new WordCountTarget { MinWords = 1000, MaxWords = 2000 });

        var targetRefreshed = SpinWait.SpinUntil(
            () => card.TargetDisplay == "1,000–2,000 w"
               && card.ProximityFill == 0.6,
            TimeSpan.FromSeconds(2));

        Assert.True(targetRefreshed, "CardViewModel did not refresh after SetTargetCommand.");

        chapter.ApplyPhaseData(MakePhase(status: ChapterStatus.Reviewing, start: "2024-04-01", end: "2024-04-20"));

        var refreshed = SpinWait.SpinUntil(
            () => card.Status == ChapterStatus.Reviewing
               && card.StatusDisplay == "Reviewing"
               && card.PhaseStartDateDisplay == "2024-04-01"
               && card.PhaseEndDateDisplay == "2024-04-20",
            TimeSpan.FromSeconds(2));

        Assert.True(refreshed, "CardViewModel did not refresh after ChapterViewModel.ApplyPhaseData().");

        card.IsSelected = true;
        Assert.True(card.IsSelected);
    }

    [Fact]
    public void CardViewModel_DisposeStopsLiveRefreshes()
    {
        var chapter = CreateChapterViewModel(
            phaseData: MakePhase(status: ChapterStatus.Drafting, start: "2024-01-01", end: "2024-02-01"),
            title: "Disposable Chapter",
            path: "ch-dispose.md",
            uuid: "uuid-dispose");

        var card = new CardViewModel(chapter);
        var originalTitle = card.Title;
        var originalWordCount = card.WordCountDisplay;

        card.Dispose();

        chapter.UpdateWordCount(999);
        chapter.UpdateNode(chapter.Node with { Title = "Changed After Dispose" });
        chapter.ApplyPhaseData(MakePhase(status: ChapterStatus.Done, start: "2025-01-01", end: "2025-02-01"));

        Assert.Equal(originalTitle, card.Title);
        Assert.Equal(originalWordCount, card.WordCountDisplay);
        Assert.Equal("Disposable Chapter", card.Title);
        Assert.Equal("No target", card.TargetDisplay);
        Assert.Equal("2024-01-01", card.PhaseStartDateDisplay);
        Assert.Equal("2024-02-01", card.PhaseEndDateDisplay);
        Assert.Equal(ChapterStatus.Drafting, card.Status);
    }
}

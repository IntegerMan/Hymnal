using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;
using Hymnal.Core.Services;
using Hymnal.ViewModels;
using NSubstitute;
using ReactiveUI.Builder;

namespace Hymnal.Core.Tests.ViewModels;

/// <summary>
/// Tests for <see cref="GanttProjection"/> — the pure projection/date-parsing logic
/// that backs <c>GanttViewModel</c>. Kept in Hymnal.Core.Tests because the projection
/// is a Core-layer concern; the ViewModel is a thin reactive wrapper around it.
/// </summary>
public class GanttViewModelTests
{
    static GanttViewModelTests()
    {
        RxAppBuilder.CreateReactiveUIBuilder()
            .WithCoreServices()
            .BuildApp();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ChapterNode MakeNode(
        string title      = "Chapter One",
        NodeKind kind     = NodeKind.Chapter,
        string path       = "ch01.md",
        int index         = 0) =>
        new ChapterNode(path, path, title, kind, IsMissing: false, Index: index);

    private static PhaseData MakePhase(
        ChapterStatus status = ChapterStatus.Drafting,
        string? start        = null,
        string? end          = null) =>
        new PhaseData { Status = status, PhaseStartDate = start, PhaseEndDate = end };

    private static WorkspaceViewModel CreateWorkspace(params ChapterViewModel[] nodes)
    {
        var workspace = (WorkspaceViewModel)RuntimeHelpers.GetUninitializedObject(typeof(WorkspaceViewModel));
        var backing = new ObservableCollection<ChapterViewModel>(nodes);
        var readOnly = new ReadOnlyObservableCollection<ChapterViewModel>(backing);

        var nodesField = typeof(WorkspaceViewModel).GetField(
            "<Nodes>k__BackingField",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to locate WorkspaceViewModel.Nodes backing field.");

        nodesField.SetValue(workspace, readOnly);
        return workspace;
    }

    private static ChapterViewModel CreateChapterViewModel(
        PhaseData? phaseData,
        string title = "Chapter One",
        string path = "ch01.md",
        string uuid = "uuid-1")
    {
        var metadataStore = Substitute.For<IMetadataStore>();
        var phaseDataService = new PhaseDataService(metadataStore);
        var targetsService = new TargetsService(metadataStore);
        var settingsStore = Substitute.For<IAppSettingsStore>();
        var notificationService = Substitute.For<INotificationService>();

        return new ChapterViewModel(
            MakeNode(title: title, path: path),
            uuid,
            phaseData,
            phaseDataService,
            targetsService,
            settingsStore,
            notificationService,
            workspaceRoot: Path.Combine(Path.GetTempPath(), "hymnal-gantt-tests"));
    }

    // ── ParseDate ─────────────────────────────────────────────────────────────

    [Fact]
    public void ParseDate_ValidIso_ReturnsParsedDate()
    {
        var result = GanttProjection.ParseDate("2024-03-15");
        Assert.Equal(new DateOnly(2024, 3, 15), result);
    }

    [Fact]
    public void ParseDate_Null_ReturnsNull()
    {
        Assert.Null(GanttProjection.ParseDate(null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseDate_EmptyOrWhitespace_ReturnsNull(string input)
    {
        Assert.Null(GanttProjection.ParseDate(input));
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("2024/03/15")]
    [InlineData("15-03-2024")]
    [InlineData("2024-13-01")] // month 13 is invalid
    public void ParseDate_InvalidFormat_ReturnsNull(string input)
    {
        Assert.Null(GanttProjection.ParseDate(input));
    }

    // ── Project — identity ────────────────────────────────────────────────────

    [Fact]
    public void Project_TitleAndPath_MappedFromNode()
    {
        var node = MakeNode(title: "The Title", path: "chapter-02.md");
        var row  = GanttProjection.Project(node, null);

        Assert.Equal("The Title",      row.Title);
        Assert.Equal("chapter-02.md",  row.RelativePath);
    }

    [Fact]
    public void Project_ChapterKind_PreservedInRow()
    {
        var row = GanttProjection.Project(MakeNode(kind: NodeKind.Chapter), null);
        Assert.Equal(NodeKind.Chapter, row.Kind);
    }

    [Fact]
    public void Project_PartKind_PreservedInRow()
    {
        var row = GanttProjection.Project(MakeNode(kind: NodeKind.Part), null);
        Assert.Equal(NodeKind.Part, row.Kind);
    }

    // ── Project — date parsing ────────────────────────────────────────────────

    [Fact]
    public void Project_ValidDates_ParsedCorrectly()
    {
        var phase = MakePhase(start: "2024-01-10", end: "2024-03-20");
        var row   = GanttProjection.Project(MakeNode(), phase);

        Assert.Equal(new DateOnly(2024, 1, 10),  row.StartDate);
        Assert.Equal(new DateOnly(2024, 3, 20),  row.EndDate);
        Assert.False(row.IsMissingDates);
    }

    [Fact]
    public void Project_NullPhaseData_YieldsMissingDates()
    {
        var row = GanttProjection.Project(MakeNode(), phase: null);

        Assert.Null(row.StartDate);
        Assert.Null(row.EndDate);
        Assert.True(row.IsMissingDates);
    }

    [Fact]
    public void Project_EmptyDateStrings_YieldsMissingDates()
    {
        var phase = MakePhase(start: "", end: "");
        var row   = GanttProjection.Project(MakeNode(), phase);

        Assert.True(row.IsMissingDates);
    }

    [Fact]
    public void Project_OnlyStartDatePresent_YieldsMissingDates()
    {
        var phase = MakePhase(start: "2024-01-10", end: null);
        var row   = GanttProjection.Project(MakeNode(), phase);

        Assert.NotNull(row.StartDate);
        Assert.Null(row.EndDate);
        Assert.True(row.IsMissingDates);
    }

    [Fact]
    public void Project_OnlyEndDatePresent_YieldsMissingDates()
    {
        var phase = MakePhase(start: null, end: "2024-06-01");
        var row   = GanttProjection.Project(MakeNode(), phase);

        Assert.Null(row.StartDate);
        Assert.NotNull(row.EndDate);
        Assert.True(row.IsMissingDates);
    }

    [Fact]
    public void Project_InvalidDateStrings_YieldsMissingDates()
    {
        var phase = MakePhase(start: "not-a-date", end: "also-bad");
        var row   = GanttProjection.Project(MakeNode(), phase);

        Assert.Null(row.StartDate);
        Assert.Null(row.EndDate);
        Assert.True(row.IsMissingDates);
    }

    // ── Project — status ──────────────────────────────────────────────────────

    [Fact]
    public void Project_NullPhaseData_DefaultsToOutliningStatus()
    {
        var row = GanttProjection.Project(MakeNode(), phase: null);
        Assert.Equal(ChapterStatus.Outlining, row.Status);
    }

    [Theory]
    [InlineData(ChapterStatus.Drafting)]
    [InlineData(ChapterStatus.Editing)]
    [InlineData(ChapterStatus.Done)]
    public void Project_ExplicitStatus_PreservedInRow(ChapterStatus status)
    {
        var phase = MakePhase(status: status, start: "2024-01-01", end: "2024-12-31");
        var row   = GanttProjection.Project(MakeNode(), phase);

        Assert.Equal(status, row.Status);
    }

    // ── GanttViewModel projection refresh ─────────────────────────────────────

    [Fact]
    public void GanttViewModel_MissingDatesStillProduceRows()
    {
        var chapter = CreateChapterViewModel(phaseData: MakePhase(start: null, end: null));
        var workspace = CreateWorkspace(chapter);
        var gantt = new GanttViewModel(workspace, new PhaseDataService(Substitute.For<IMetadataStore>()));

        Assert.Single(gantt.Rows);
        Assert.True(gantt.Rows[0].IsMissingDates);
        Assert.Equal(chapter.Node.Title, gantt.Rows[0].Title);
    }

    [Fact]
    public void GanttViewModel_RefreshesWhenChapterPhaseDataChanges()
    {
        var initialPhase = MakePhase(status: ChapterStatus.Drafting, start: null, end: null);
        var chapter = CreateChapterViewModel(phaseData: initialPhase);
        var workspace = CreateWorkspace(chapter);
        var gantt = new GanttViewModel(workspace, new PhaseDataService(Substitute.For<IMetadataStore>()));

        Assert.Single(gantt.Rows);
        Assert.True(gantt.Rows[0].IsMissingDates);
        Assert.Equal(ChapterStatus.Drafting, gantt.Rows[0].Status);

        chapter.ApplyPhaseData(MakePhase(
            status: ChapterStatus.Reviewing,
            start: "2024-02-10",
            end: "2024-03-15"));

        var updated = SpinWait.SpinUntil(
            () => gantt.Rows[0].Status == ChapterStatus.Reviewing
               && gantt.Rows[0].StartDate == new DateOnly(2024, 2, 10)
               && gantt.Rows[0].EndDate == new DateOnly(2024, 3, 15)
               && !gantt.Rows[0].IsMissingDates,
            TimeSpan.FromSeconds(2));

        Assert.True(updated, "Gantt rows did not refresh after ChapterViewModel.ApplyPhaseData().");
    }

    // ── Part rollup rows ──────────────────────────────────────────────────────

    [Fact]
    public void GanttViewModel_PartRow_SpansMinStartToMaxEndOfChildChapters()
    {
        var part = CreateChapterViewModel(
            phaseData: null,
            title: "Part One",
            path: "part-01.md",
            uuid: "uuid-part");
        // Swap the Part node in — helper creates Chapter by default.
        var partNode = new ChapterNode("part-01.md", "part-01.md", "Part One", NodeKind.Part, IsMissing: false, Index: 0);

        var ch1 = CreateChapterViewModel(
            phaseData: MakePhase(start: "2024-02-01", end: "2024-04-30"),
            title: "Chapter One", path: "ch01.md", uuid: "uuid-ch1");
        var ch2 = CreateChapterViewModel(
            phaseData: MakePhase(start: "2024-01-15", end: "2024-05-31"),
            title: "Chapter Two", path: "ch02.md", uuid: "uuid-ch2");

        // Inject the Part node into the ChapterViewModel via field reflection.
        InjectNode(part, partNode);

        var workspace = CreateWorkspace(part, ch1, ch2);
        var gantt = new GanttViewModel(workspace, new PhaseDataService(Substitute.For<IMetadataStore>()));

        Assert.Equal(3, gantt.Rows.Count);
        var partRow = gantt.Rows[0];

        Assert.True(partRow.IsPart);
        // Start = min(2024-02-01, 2024-01-15) = 2024-01-15
        Assert.Equal(new DateOnly(2024, 1, 15), partRow.StartDate);
        // End = max(2024-04-30, 2024-05-31) = 2024-05-31
        Assert.Equal(new DateOnly(2024, 5, 31), partRow.EndDate);
        Assert.False(partRow.IsMissingDates);
    }

    [Fact]
    public void GanttViewModel_PartRow_CompletionPercentage_ReflectsDoneChapters()
    {
        var part = CreateChapterViewModel(
            phaseData: null,
            title: "Part One", path: "part-01.md", uuid: "uuid-part");
        InjectNode(part, new ChapterNode("part-01.md", "part-01.md", "Part One", NodeKind.Part, IsMissing: false, Index: 0));

        var ch1 = CreateChapterViewModel(
            phaseData: MakePhase(status: ChapterStatus.Done, start: "2024-01-01", end: "2024-03-01"),
            title: "Ch1", path: "ch01.md", uuid: "uuid-ch1");
        var ch2 = CreateChapterViewModel(
            phaseData: MakePhase(status: ChapterStatus.Drafting, start: "2024-01-01", end: "2024-04-01"),
            title: "Ch2", path: "ch02.md", uuid: "uuid-ch2");
        var ch3 = CreateChapterViewModel(
            phaseData: MakePhase(status: ChapterStatus.Done, start: "2024-01-01", end: "2024-05-01"),
            title: "Ch3", path: "ch03.md", uuid: "uuid-ch3");

        var workspace = CreateWorkspace(part, ch1, ch2, ch3);
        var gantt = new GanttViewModel(workspace, new PhaseDataService(Substitute.For<IMetadataStore>()));

        var partRow = gantt.Rows[0];
        Assert.True(partRow.IsPart);
        // 2 out of 3 chapters are Done → ~0.667
        Assert.Equal(2.0 / 3.0, partRow.CompletionPercentage, precision: 10);
    }

    [Fact]
    public void GanttViewModel_PartRow_NoChildren_ZeroCompletionAndMissingDates()
    {
        var part = CreateChapterViewModel(
            phaseData: null,
            title: "Part One", path: "part-01.md", uuid: "uuid-part");
        InjectNode(part, new ChapterNode("part-01.md", "part-01.md", "Part One", NodeKind.Part, IsMissing: false, Index: 0));

        var workspace = CreateWorkspace(part);
        var gantt = new GanttViewModel(workspace, new PhaseDataService(Substitute.For<IMetadataStore>()));

        var partRow = gantt.Rows[0];
        Assert.True(partRow.IsPart);
        Assert.True(partRow.IsMissingDates);
        Assert.Equal(0.0, partRow.CompletionPercentage);
    }

    [Fact]
    public void GanttViewModel_PartRow_StopsAtNextPart()
    {
        // Part1 → ch1, ch2; Part2 → ch3
        var part1 = CreateChapterViewModel(phaseData: null, title: "Part 1", path: "part-01.md", uuid: "p1");
        InjectNode(part1, new ChapterNode("part-01.md", "part-01.md", "Part 1", NodeKind.Part, IsMissing: false, Index: 0));

        var ch1 = CreateChapterViewModel(phaseData: MakePhase(start: "2024-01-01", end: "2024-02-28"), title: "Ch1", path: "ch01.md", uuid: "ch1");
        var ch2 = CreateChapterViewModel(phaseData: MakePhase(start: "2024-03-01", end: "2024-04-30"), title: "Ch2", path: "ch02.md", uuid: "ch2");

        var part2 = CreateChapterViewModel(phaseData: null, title: "Part 2", path: "part-02.md", uuid: "p2");
        InjectNode(part2, new ChapterNode("part-02.md", "part-02.md", "Part 2", NodeKind.Part, IsMissing: false, Index: 3));

        var ch3 = CreateChapterViewModel(phaseData: MakePhase(start: "2024-06-01", end: "2024-12-31"), title: "Ch3", path: "ch03.md", uuid: "ch3");

        var workspace = CreateWorkspace(part1, ch1, ch2, part2, ch3);
        var gantt = new GanttViewModel(workspace, new PhaseDataService(Substitute.For<IMetadataStore>()));

        Assert.Equal(5, gantt.Rows.Count);
        var row1 = gantt.Rows[0]; // Part 1 rollup
        var row4 = gantt.Rows[3]; // Part 2 rollup

        Assert.True(row1.IsPart);
        // Part 1 spans ch1+ch2 only: 2024-01-01 to 2024-04-30
        Assert.Equal(new DateOnly(2024, 1, 1),  row1.StartDate);
        Assert.Equal(new DateOnly(2024, 4, 30), row1.EndDate);

        Assert.True(row4.IsPart);
        // Part 2 spans ch3 only: 2024-06-01 to 2024-12-31
        Assert.Equal(new DateOnly(2024, 6, 1),   row4.StartDate);
        Assert.Equal(new DateOnly(2024, 12, 31), row4.EndDate);
    }

    // ── EditDatesCommand / RowEditRequested ───────────────────────────────────

    [Fact]
    public void GanttViewModel_RowEditRequested_FiresWhenEditDatesCommandExecutedOnChapterRow()
    {
        var chapter = CreateChapterViewModel(
            phaseData: MakePhase(status: ChapterStatus.Drafting, start: "2024-01-01", end: "2024-06-30"),
            title: "Chapter One", path: "ch01.md");
        var workspace = CreateWorkspace(chapter);
        var gantt = new GanttViewModel(workspace, new PhaseDataService(Substitute.For<IMetadataStore>()));

        GanttRowViewModel? received = null;
        gantt.RowEditRequested.Subscribe(vm => received = vm);

        gantt.Rows[0].EditDatesCommand.Execute().Subscribe();

        var fired = SpinWait.SpinUntil(() => received != null, TimeSpan.FromSeconds(2));
        Assert.True(fired, "RowEditRequested did not emit after EditDatesCommand.Execute().");
        Assert.Equal("Chapter One", received!.Title);
        Assert.True(received.IsChapter);
    }

    [Fact]
    public void GanttViewModel_RowEditRequested_DoesNotFireForPartRows()
    {
        var part = CreateChapterViewModel(phaseData: null, title: "Part One", path: "part-01.md", uuid: "p1");
        InjectNode(part, new ChapterNode("part-01.md", "part-01.md", "Part One", NodeKind.Part, IsMissing: false, Index: 0));
        var ch1 = CreateChapterViewModel(
            phaseData: MakePhase(start: "2024-01-01", end: "2024-03-01"),
            title: "Ch1", path: "ch01.md", uuid: "ch1");

        var workspace = CreateWorkspace(part, ch1);
        var gantt = new GanttViewModel(workspace, new PhaseDataService(Substitute.For<IMetadataStore>()));

        var editFired = false;
        gantt.RowEditRequested.Subscribe(_ => editFired = true);

        // Execute the Part row's command directly — should NOT propagate to RowEditRequested.
        gantt.Rows[0].EditDatesCommand.Execute().Subscribe();

        // Allow 500 ms for any async dispatch; it should still be false.
        Thread.Sleep(500);
        Assert.False(editFired, "RowEditRequested should not fire when a Part row's EditDatesCommand is executed.");
    }

    [Fact]
    public void GanttRowViewModel_EditDatesCommand_ExposedAndExecutable()
    {
        var data = new GanttRowData(
            RelativePath: "ch01.md", Title: "Ch1", Kind: NodeKind.Chapter,
            Status: ChapterStatus.Drafting,
            StartDate: new DateOnly(2024, 1, 1), EndDate: new DateOnly(2024, 3, 1),
            IsMissingDates: false);

        var vm = new GanttRowViewModel(data);

        Assert.NotNull(vm.EditDatesCommand);

        // Subscribe to the command output, execute it, then wait for the result.
        var executed = false;
        vm.EditDatesCommand.Subscribe(_ => executed = true);
        vm.EditDatesCommand.Execute().Subscribe();

        var completed = SpinWait.SpinUntil(() => executed, TimeSpan.FromSeconds(2));
        Assert.True(completed, "EditDatesCommand did not emit a result after Execute().");
    }

    // ── Helpers for Part rollup tests ─────────────────────────────────────────

    private static void InjectNode(ChapterViewModel vm, ChapterNode node)
    {
        // ChapterViewModel.Node has a 'private set' backed by '_node'.
        var field = typeof(ChapterViewModel).GetField(
            "_node",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to locate ChapterViewModel._node backing field.");
        field.SetValue(vm, node);
    }
}

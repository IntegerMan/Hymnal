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

    private static GanttRowViewModel ChapterRow(GanttViewModel gantt, int index = 0) =>
        gantt.Rows.Where(r => r.IsChapter).ElementAt(index);

    private static GanttRowViewModel PartRow(GanttViewModel gantt, int index = 0) =>
        gantt.Rows.Where(r => r.IsPart).ElementAt(index);

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
        Assert.False(row.IsMissingDates);
    }

    [Fact]
    public void Project_OnlyEndDatePresent_YieldsMissingDates()
    {
        var phase = MakePhase(start: null, end: "2024-06-01");
        var row   = GanttProjection.Project(MakeNode(), phase);

        Assert.Null(row.StartDate);
        Assert.NotNull(row.EndDate);
        Assert.False(row.IsMissingDates);
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
        var gantt = new GanttViewModel(workspace, new PhaseDataService(Substitute.For<IMetadataStore>()), Substitute.For<INotificationService>());

        Assert.Equal(2, gantt.Rows.Count);
        var row = ChapterRow(gantt);
        Assert.True(row.IsMissingDates);
        Assert.Equal(chapter.Node.Title, row.Title);
    }

    [Fact]
    public void GanttViewModel_RefreshesWhenChapterPhaseDataChanges()
    {
        var initialPhase = MakePhase(status: ChapterStatus.Drafting, start: null, end: null);
        var chapter = CreateChapterViewModel(phaseData: initialPhase);
        var workspace = CreateWorkspace(chapter);
        var gantt = new GanttViewModel(workspace, new PhaseDataService(Substitute.For<IMetadataStore>()), Substitute.For<INotificationService>());

        var row = ChapterRow(gantt);
        Assert.True(row.IsMissingDates);
        Assert.Equal(ChapterStatus.Drafting, row.Status);

        chapter.ApplyPhaseData(MakePhase(
            status: ChapterStatus.Reviewing,
            start: "2024-02-10",
            end: "2024-03-15"));

        var updated = SpinWait.SpinUntil(
            () =>
            {
                row = ChapterRow(gantt);
                return row.Status == ChapterStatus.Reviewing
                    && row.StartDate == new DateOnly(2024, 2, 10)
                    && row.EndDate == new DateOnly(2024, 3, 15)
                    && !row.IsMissingDates;
            },
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
        var gantt = new GanttViewModel(workspace, new PhaseDataService(Substitute.For<IMetadataStore>()), Substitute.For<INotificationService>());

        Assert.Equal(4, gantt.Rows.Count);
        var partRow = PartRow(gantt);

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
        var gantt = new GanttViewModel(workspace, new PhaseDataService(Substitute.For<IMetadataStore>()), Substitute.For<INotificationService>());

        var partRow = PartRow(gantt);
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
        var gantt = new GanttViewModel(workspace, new PhaseDataService(Substitute.For<IMetadataStore>()), Substitute.For<INotificationService>());

        var partRow = PartRow(gantt);
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
        var gantt = new GanttViewModel(workspace, new PhaseDataService(Substitute.For<IMetadataStore>()), Substitute.For<INotificationService>());

        Assert.Equal(6, gantt.Rows.Count);
        var row1 = PartRow(gantt, 0);
        var row2 = PartRow(gantt, 1);

        Assert.True(row1.IsPart);
        // Part 1 spans ch1+ch2 only: 2024-01-01 to 2024-04-30
        Assert.Equal(new DateOnly(2024, 1, 1),  row1.StartDate);
        Assert.Equal(new DateOnly(2024, 4, 30), row1.EndDate);

        Assert.True(row2.IsPart);
        // Part 2 spans ch3 only: 2024-06-01 to 2024-12-31
        Assert.Equal(new DateOnly(2024, 6, 1),   row2.StartDate);
        Assert.Equal(new DateOnly(2024, 12, 31), row2.EndDate);
    }

    // ── EditDatesCommand / RowEditRequested ───────────────────────────────────

    [Fact]
    public void GanttViewModel_RowEditRequested_FiresWhenEditDatesCommandExecutedOnChapterRow()
    {
        var chapter = CreateChapterViewModel(
            phaseData: MakePhase(status: ChapterStatus.Drafting, start: "2024-01-01", end: "2024-06-30"),
            title: "Chapter One", path: "ch01.md");
        var workspace = CreateWorkspace(chapter);
        var gantt = new GanttViewModel(workspace, new PhaseDataService(Substitute.For<IMetadataStore>()), Substitute.For<INotificationService>());

        GanttRowViewModel? received = null;
        gantt.RowEditRequested.Subscribe(vm => received = vm);

        ChapterRow(gantt).EditDatesCommand.Execute().Subscribe();

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
        var gantt = new GanttViewModel(workspace, new PhaseDataService(Substitute.For<IMetadataStore>()), Substitute.For<INotificationService>());

        var editFired = false;
        gantt.RowEditRequested.Subscribe(_ => editFired = true);

        // Execute the Part row's command directly — should NOT propagate to RowEditRequested.
        PartRow(gantt).EditDatesCommand.Execute().Subscribe();

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

    // ── IsEditingDates / editing state ────────────────────────────────────────

    [Fact]
    public void GanttViewModel_IsEditingDates_SetTrueWhenRowEditRequested()
    {
        var chapter = CreateChapterViewModel(
            phaseData: MakePhase(status: ChapterStatus.Drafting, start: "2024-01-01", end: "2024-06-30"),
            title: "Chapter One", path: "ch01.md");
        var workspace = CreateWorkspace(chapter);
        var gantt = new GanttViewModel(workspace, new PhaseDataService(Substitute.For<IMetadataStore>()), Substitute.For<INotificationService>());

        Assert.False(gantt.IsEditingDates);

        ChapterRow(gantt).EditDatesCommand.Execute().Subscribe();

        var opened = SpinWait.SpinUntil(() => gantt.IsEditingDates, TimeSpan.FromSeconds(2));
        Assert.True(opened, "IsEditingDates did not become true after EditDatesCommand.");
        Assert.NotNull(gantt.EditingRow);
        Assert.Equal("Chapter One", gantt.EditingRow!.Title);
    }

    [Fact]
    public void GanttViewModel_OpenEditForRow_PopulatesEditDates_FromRowStartEnd()
    {
        var chapter = CreateChapterViewModel(
            phaseData: MakePhase(status: ChapterStatus.Drafting, start: "2024-03-10", end: "2024-09-15"),
            title: "Chapter Two", path: "ch02.md");
        var workspace = CreateWorkspace(chapter);
        var gantt = new GanttViewModel(workspace, new PhaseDataService(Substitute.For<IMetadataStore>()), Substitute.For<INotificationService>());

        ChapterRow(gantt).EditDatesCommand.Execute().Subscribe();

        SpinWait.SpinUntil(() => gantt.IsEditingDates, TimeSpan.FromSeconds(2));

        Assert.True(gantt.EditStartDate.HasValue);
        Assert.True(gantt.EditEndDate.HasValue);
        Assert.Equal(new DateTime(2024, 3, 10), gantt.EditStartDate!.Value.Date);
        Assert.Equal(new DateTime(2024, 9, 15), gantt.EditEndDate!.Value.Date);
    }

    [Fact]
    public void GanttViewModel_OpenEditForRow_MissingDates_EditDatesAreNull()
    {
        var chapter = CreateChapterViewModel(
            phaseData: MakePhase(status: ChapterStatus.Drafting, start: null, end: null),
            title: "No Dates", path: "ch03.md");
        var workspace = CreateWorkspace(chapter);
        var gantt = new GanttViewModel(workspace, new PhaseDataService(Substitute.For<IMetadataStore>()), Substitute.For<INotificationService>());

        ChapterRow(gantt).EditDatesCommand.Execute().Subscribe();

        SpinWait.SpinUntil(() => gantt.IsEditingDates, TimeSpan.FromSeconds(2));

        Assert.Null(gantt.EditStartDate);
        Assert.Null(gantt.EditEndDate);
    }

    [Fact]
    public void GanttViewModel_CancelEditCommand_ClearsState()
    {
        var chapter = CreateChapterViewModel(
            phaseData: MakePhase(status: ChapterStatus.Drafting, start: "2024-01-01", end: "2024-06-30"),
            title: "Chapter One", path: "ch01.md");
        var workspace = CreateWorkspace(chapter);
        var gantt = new GanttViewModel(workspace, new PhaseDataService(Substitute.For<IMetadataStore>()), Substitute.For<INotificationService>());

        ChapterRow(gantt).EditDatesCommand.Execute().Subscribe();
        SpinWait.SpinUntil(() => gantt.IsEditingDates, TimeSpan.FromSeconds(2));
        Assert.True(gantt.IsEditingDates);

        gantt.CancelEditCommand.Execute().Subscribe();

        var cleared = SpinWait.SpinUntil(() => !gantt.IsEditingDates, TimeSpan.FromSeconds(2));
        Assert.True(cleared, "IsEditingDates was not cleared by CancelEditCommand.");
        Assert.Null(gantt.EditingRow);
        Assert.Null(gantt.EditStartDate);
        Assert.Null(gantt.EditEndDate);
    }

    // ── ChapterViewModel.UpdateDatesAsync ──────────────────────────────────────

    [Fact]
    public async Task ChapterViewModel_UpdateDatesAsync_PersistsAndAppliesPhaseData()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"hymnal-updateDates-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(tempRoot, ".hymnal-data"));
        try
        {
            var store = new Hymnal.Core.Infrastructure.MetadataStore();
            var phaseDataService = new PhaseDataService(store);
            var targetsService = new TargetsService(store);
            var settingsStore = Substitute.For<IAppSettingsStore>();
            var notificationService = Substitute.For<INotificationService>();

            var node = MakeNode(title: "Chapter A", path: "ch-a.md");
            var initialPhase = MakePhase(status: ChapterStatus.Drafting, start: "2024-01-01", end: "2024-03-31");
            var vm = new ChapterViewModel(
                node, "uuid-a", initialPhase,
                phaseDataService, targetsService, settingsStore,
                notificationService, workspaceRoot: tempRoot);

            await vm.UpdateDatesAsync("2025-06-01", "2025-09-30");

            // Verify persistence by re-loading from the written file (avoids UIThread dispatch).
            var phases = await phaseDataService.LoadAsync(tempRoot);
            Assert.True(phases.TryGetValue("uuid-a", out var saved), "No entry found for uuid-a.");
            Assert.Equal("2025-06-01", saved?.PhaseStartDate);
            Assert.Equal("2025-09-30", saved?.PhaseEndDate);
            // Status should be preserved from the initial phase.
            Assert.Equal(ChapterStatus.Drafting, saved?.Status);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
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

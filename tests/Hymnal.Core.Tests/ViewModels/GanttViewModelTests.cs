using Hymnal.Core.Models;
using Hymnal.Core.Services;

namespace Hymnal.Core.Tests.ViewModels;

/// <summary>
/// Tests for <see cref="GanttProjection"/> — the pure projection/date-parsing logic
/// that backs <c>GanttViewModel</c>. Kept in Hymnal.Core.Tests because the projection
/// is a Core-layer concern; the ViewModel is a thin reactive wrapper around it.
/// </summary>
public class GanttViewModelTests
{
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
}

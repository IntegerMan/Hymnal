using Avalonia;
using Hymnal.Core.Models;
using Hymnal.ViewModels;
using Hymnal.Views;

namespace Hymnal.Core.Tests.Views;

public class GanttCanvasTests
{
    private const double HeaderHeight = 36.0;
    private const double RowHeight = 28.0;

    [Fact]
    public void Measure_WithInfiniteAvailableSize_ReturnsFiniteDesiredSize()
    {
        var canvas = CreateCanvas();

        canvas.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        Assert.True(double.IsFinite(canvas.DesiredSize.Width));
        Assert.True(double.IsFinite(canvas.DesiredSize.Height));
        Assert.True(canvas.DesiredSize.Width >= 400);
        Assert.Equal(64, canvas.DesiredSize.Height);
    }

    [Fact]
    public void Measure_WithNaNAvailableSize_ThrowsInvalidOperationException()
    {
        var canvas = CreateCanvas();

        Assert.Throws<InvalidOperationException>(() =>
            canvas.Measure(new Size(double.NaN, double.NaN)));
    }

    [Fact]
    public void TryCreateRowReorderIntent_ForChapterRowsInSamePart_ReturnsSourceAndTargetRows()
    {
        var canvas = CreateCanvasWithSections();
        var rows = canvas.Rows!;

        var created = canvas.TryCreateRowReorderIntent(
            RowPoint(2),
            RowPoint(4, yOffset: 4),
            out var args);

        Assert.True(created);
        Assert.NotNull(args);
        Assert.Same(rows[2], args.SourceRow);
        Assert.Same(rows[4], args.TargetRow);
        Assert.True(args.DropBeforeTarget);
    }

    [Fact]
    public void TryCreateRowReorderIntent_UsesTargetRowMidpointForBeforeAfterPlacement()
    {
        var canvas = CreateCanvasWithSections();

        var before = canvas.TryCreateRowReorderIntent(
            RowPoint(2),
            RowPoint(4, yOffset: RowHeight / 2.0 - 1),
            out var beforeArgs);

        var after = canvas.TryCreateRowReorderIntent(
            RowPoint(2),
            RowPoint(4, yOffset: RowHeight / 2.0 + 1),
            out var afterArgs);

        Assert.True(before);
        Assert.NotNull(beforeArgs);
        Assert.True(beforeArgs.DropBeforeTarget);

        Assert.True(after);
        Assert.NotNull(afterArgs);
        Assert.False(afterArgs.DropBeforeTarget);
    }

    [Fact]
    public void TryCreateRowReorderIntent_IgnoresHeaderAndOutsideRows()
    {
        var canvas = CreateCanvasWithSections();

        Assert.False(canvas.TryCreateRowReorderIntent(new Point(12, HeaderHeight - 1), RowPoint(3), out _));
        Assert.False(canvas.TryCreateRowReorderIntent(new Point(-1, HeaderHeight + 2 * RowHeight + 4), RowPoint(3), out _));
        Assert.False(canvas.TryCreateRowReorderIntent(RowPoint(2), new Point(12, HeaderHeight + 99 * RowHeight), out _));
    }

    [Fact]
    public void TryCreateRowReorderIntent_IgnoresBookAndPartRowsAsSourcesOrTargets()
    {
        var canvas = CreateCanvasWithSections();

        Assert.False(canvas.TryCreateRowReorderIntent(RowPoint(0), RowPoint(2), out _));
        Assert.False(canvas.TryCreateRowReorderIntent(RowPoint(1), RowPoint(2), out _));
        Assert.False(canvas.TryCreateRowReorderIntent(RowPoint(2), RowPoint(0), out _));
        Assert.False(canvas.TryCreateRowReorderIntent(RowPoint(2), RowPoint(1), out _));
    }

    [Fact]
    public void TryCreateRowReorderIntent_IgnoresCrossPartDrops()
    {
        var canvas = CreateCanvasWithSections();

        var created = canvas.TryCreateRowReorderIntent(
            RowPoint(2),
            RowPoint(6, yOffset: 4),
            out _);

        Assert.False(created);
    }

    [Fact]
    public void TryCreateRowReorderIntent_IgnoresNoOpDrops()
    {
        var canvas = CreateCanvasWithSections();

        Assert.False(canvas.TryCreateRowReorderIntent(RowPoint(2), RowPoint(2), out _));

        // Chapter 1 is already immediately before Chapter 2.
        Assert.False(canvas.TryCreateRowReorderIntent(
            RowPoint(2),
            RowPoint(3, yOffset: 4),
            out _));

        // Chapter 2 is already immediately after Chapter 1.
        Assert.False(canvas.TryCreateRowReorderIntent(
            RowPoint(3),
            RowPoint(2, yOffset: RowHeight - 4),
            out _));
    }

    [Fact]
    public void TryRaiseRowReorderIntent_RaisesDeterministicEventForLegalDrag()
    {
        var canvas = CreateCanvasWithSections();
        var rows = canvas.Rows!;
        GanttCanvas.GanttRowReorderRequestedEventArgs? raisedArgs = null;
        canvas.RowReorderRequested += (_, args) => raisedArgs = args;

        var raised = canvas.TryRaiseRowReorderIntent(
            RowPoint(2),
            RowPoint(4, yOffset: RowHeight - 2));

        Assert.True(raised);
        Assert.NotNull(raisedArgs);
        Assert.Same(rows[2], raisedArgs.SourceRow);
        Assert.Same(rows[4], raisedArgs.TargetRow);
        Assert.False(raisedArgs.DropBeforeTarget);
    }

    private static Point RowPoint(int rowIndex, double yOffset = RowHeight / 2.0)
    {
        return new Point(12, HeaderHeight + rowIndex * RowHeight + yOffset);
    }

    private static GanttCanvas CreateCanvas()
    {
        return new GanttCanvas
        {
            Rows = new[]
            {
                CreateRow("chapter-1.md", "Chapter One", NodeKind.Chapter)
            }
        };
    }

    private static GanttCanvas CreateCanvasWithSections()
    {
        return new GanttCanvas
        {
            Rows = new[]
            {
                CreateRow("__book__", "Book", NodeKind.Part, isBook: true),
                CreateRow("part-one.md", "Part One", NodeKind.Part),
                CreateRow("chapter-1.md", "Chapter One", NodeKind.Chapter),
                CreateRow("chapter-2.md", "Chapter Two", NodeKind.Chapter),
                CreateRow("chapter-3.md", "Chapter Three", NodeKind.Chapter),
                CreateRow("part-two.md", "Part Two", NodeKind.Part),
                CreateRow("chapter-4.md", "Chapter Four", NodeKind.Chapter),
            }
        };
    }

    private static GanttRowViewModel CreateRow(
        string relativePath,
        string title,
        NodeKind kind,
        bool isBook = false)
    {
        return new GanttRowViewModel(new GanttRowData(
            RelativePath: relativePath,
            Title: title,
            Kind: kind,
            Status: ChapterStatus.Drafting,
            StartDate: new DateOnly(2024, 1, 1),
            EndDate: new DateOnly(2024, 3, 1),
            IsMissingDates: false,
            CompletionPercentage: 0.0,
            IsBook: isBook));
    }
}

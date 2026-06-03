using Avalonia;
using Hymnal.Core.Models;
using Hymnal.ViewModels;
using Hymnal.Views;

namespace Hymnal.Core.Tests.Views;

public class GanttCanvasTests
{
    [Fact]
    public void Measure_WithInfiniteAvailableSize_ReturnsFiniteDesiredSize()
    {
        var canvas = CreateCanvas();

        canvas.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        Assert.True(double.IsFinite(canvas.DesiredSize.Width));
        Assert.True(double.IsFinite(canvas.DesiredSize.Height));
        Assert.True(canvas.DesiredSize.Width >= 400);
        Assert.Equal(62, canvas.DesiredSize.Height);
    }

    [Fact]
    public void Measure_WithNaNAvailableSize_ReturnsFiniteDesiredSize()
    {
        var canvas = CreateCanvas();

        canvas.Measure(new Size(double.NaN, double.NaN));

        Assert.True(double.IsFinite(canvas.DesiredSize.Width));
        Assert.True(double.IsFinite(canvas.DesiredSize.Height));
        Assert.True(canvas.DesiredSize.Width >= 400);
        Assert.Equal(62, canvas.DesiredSize.Height);
    }

    private static GanttCanvas CreateCanvas()
    {
        return new GanttCanvas
        {
            Rows = new[]
            {
                new GanttRowViewModel(new GanttRowData(
                    RelativePath: "chapter-1.md",
                    Title: "Chapter One",
                    Kind: NodeKind.Chapter,
                    Status: ChapterStatus.Drafting,
                    StartDate: new DateOnly(2024, 1, 1),
                    EndDate: new DateOnly(2024, 3, 1),
                    IsMissingDates: false,
                    CompletionPercentage: 0.0))
            }
        };
    }
}

using Hymnal.Core.Services;

namespace Hymnal.Core.Tests.Services;

public class GanttCellValueParserTests
{
    [Theory]
    [InlineData("2026-06-04")]
    [InlineData("2026/06/04")]
    [InlineData("06/04/2026")]
    public void TryParseDate_AcceptsCommonFormats(string input)
    {
        Assert.True(GanttCellValueParser.TryParseDate(input, out var date));
        Assert.Equal(new DateOnly(2026, 6, 4), date);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-date")]
    public void TryParseDate_Invalid_ReturnsFalse(string input)
    {
        Assert.False(GanttCellValueParser.TryParseDate(input, out _));
    }

    [Theory]
    [InlineData("100", 100.0)]
    [InlineData("100%", 100.0)]
    [InlineData("45.5", 45.5)]
    [InlineData(" 75 % ", 75.0)]
    public void TryParseProgress_AcceptsCommonFormats(string input, double expected)
    {
        Assert.True(GanttCellValueParser.TryParseProgress(input, out var progress));
        Assert.Equal(expected, progress);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    public void TryParseProgress_Invalid_ReturnsFalse(string input)
    {
        Assert.False(GanttCellValueParser.TryParseProgress(input, out _));
    }

    [Fact]
    public void TryParseProgress_ClampsAbove100()
    {
        Assert.True(GanttCellValueParser.TryParseProgress("150", out var progress));
        Assert.Equal(100.0, progress);
    }
}

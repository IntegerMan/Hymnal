using Hymnal.Core.Services;

namespace Hymnal.Core.Tests.Services;

public class WordCountServiceTests
{
    private readonly WordCountService _svc = new();

    // -------------------------------------------------------------------------
    // Empty / whitespace content
    // -------------------------------------------------------------------------

    [Fact]
    public void CountWords_NullContent_ReturnsZero()
    {
        Assert.Equal(0, _svc.CountWords(null));
    }

    [Fact]
    public void CountWords_EmptyString_ReturnsZero()
    {
        Assert.Equal(0, _svc.CountWords(""));
    }

    [Fact]
    public void CountWords_WhitespaceOnly_ReturnsZero()
    {
        Assert.Equal(0, _svc.CountWords("   \t\n  "));
    }

    // -------------------------------------------------------------------------
    // Plain text token counting
    // -------------------------------------------------------------------------

    [Fact]
    public void CountWords_TwoWordLine_ReturnsTwo()
    {
        Assert.Equal(2, _svc.CountWords("Hello world"));
    }

    [Fact]
    public void CountWords_FiveWordLine_ReturnsFive()
    {
        Assert.Equal(5, _svc.CountWords("one two three four five"));
    }

    // -------------------------------------------------------------------------
    // Markua directive lines excluded
    // -------------------------------------------------------------------------

    [Fact]
    public void CountWords_MarkuaDirectiveLine_Excluded()
    {
        var content = "Hello world\n{sample: true}\nFoo bar";
        // "Hello world" = 2, "{sample: true}" excluded, "Foo bar" = 2 → 4
        Assert.Equal(4, _svc.CountWords(content));
    }

    // -------------------------------------------------------------------------
    // Heading lines count normally
    // -------------------------------------------------------------------------

    [Fact]
    public void CountWords_HeadingLine_Counted()
    {
        // "# Chapter One" → 3 tokens
        Assert.Equal(3, _svc.CountWords("# Chapter One"));
    }

    // -------------------------------------------------------------------------
    // Mixed content
    // -------------------------------------------------------------------------

    [Fact]
    public void CountWords_MixedContent_ExcludesOnlyBraceLines()
    {
        var content = "# Part One\nSome prose here.\n{class: blurb}\nMore prose.";
        // "# Part One" = 3, "Some prose here." = 3, "{class: blurb}" excluded, "More prose." = 2 → 8
        Assert.Equal(8, _svc.CountWords(content));
    }

    // -------------------------------------------------------------------------
    // Inline braces do NOT trigger exclusion; only line-start '{'
    // -------------------------------------------------------------------------

    [Fact]
    public void CountWords_InlineBraceNotAtLineStart_NotExcluded()
    {
        var content = "text {inside} line";
        // All three tokens counted — brace not at start of trimmed line
        Assert.Equal(3, _svc.CountWords(content));
    }

    [Fact]
    public void CountWords_AttributeListAtLineStart_Excluded()
    {
        var content = "{class: blurb}\nNormal text";
        // "{class: blurb}" excluded, "Normal text" = 2 → 2
        Assert.Equal(2, _svc.CountWords(content));
    }
}

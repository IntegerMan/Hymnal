using System;
using System.Globalization;
using System.IO;
using Avalonia.Media;
using Hymnal.Core.Models;
using Hymnal.Views.Converters;
using Xunit;

namespace Hymnal.Core.Tests.Views;

public sealed class SidebarViewSmokeTests
{
    [Fact]
    public void SidebarView_XamlDeclaresExcludedPresentationAndConditionalIncludeExcludeActions()
    {
        var xaml = File.ReadAllText(GetSidebarViewPath());

        Assert.Contains("<MenuItem Header=\"Include in book\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding Node.IsExcluded}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<MenuItem Header=\"Exclude from book\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Binding Path=\"Node.IsExcluded\" />", xaml, StringComparison.Ordinal);
        Assert.Contains("Converter=\"{StaticResource NodeKindIsChapterAndPresentConverter}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Converter=\"{StaticResource NodeKindAndMissingToForegroundConverter}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"excluded\"", xaml, StringComparison.Ordinal);
        Assert.Contains("BoolToFontStyleConverter", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void NodeKindAndMissingToForegroundConverter_TreatsExcludedChaptersDistinctly()
    {
        var converter = new NodeKindAndMissingToForegroundConverter();

        var included = (SolidColorBrush)converter.Convert(
            new object?[] { NodeKind.Chapter, false, false },
            typeof(IBrush),
            null,
            CultureInfo.InvariantCulture)!;

        var excluded = (SolidColorBrush)converter.Convert(
            new object?[] { NodeKind.Chapter, false, true },
            typeof(IBrush),
            null,
            CultureInfo.InvariantCulture)!;

        var missing = (SolidColorBrush)converter.Convert(
            new object?[] { NodeKind.Chapter, true, false },
            typeof(IBrush),
            null,
            CultureInfo.InvariantCulture)!;

        Assert.NotEqual(included.Color, excluded.Color);
        Assert.NotEqual(excluded.Color, missing.Color);
    }

    private static string GetSidebarViewPath() => Path.GetFullPath(
        Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "Hymnal",
            "Views",
            "SidebarView.axaml"));
}

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Hymnal.Core.Models;

namespace Hymnal.Views.Converters;

public class NodeKindToFontWeightConverter : IValueConverter
{
    public static readonly NodeKindToFontWeightConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is NodeKind.Part ? FontWeight.SemiBold : FontWeight.Normal;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class NodeKindToMarginConverter : IValueConverter
{
    public static readonly NodeKindToMarginConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is NodeKind.Chapter ? new Thickness(12, 0, 0, 0) : new Thickness(0);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class NodeKindToForegroundConverter : IValueConverter
{
    public static readonly NodeKindToForegroundConverter Instance = new();

    private static readonly SolidColorBrush PartBrush    = new(Color.Parse("#9D4EDD"));
    private static readonly SolidColorBrush ChapterBrush = new(Color.Parse("#EDE8F5"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is NodeKind.Part ? PartBrush : ChapterBrush;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Returns purple for parts, grey for missing chapters, and light for present chapters.</summary>
public class NodeKindAndMissingToForegroundConverter : IMultiValueConverter
{
    public static readonly NodeKindAndMissingToForegroundConverter Instance = new();

    private static readonly SolidColorBrush PartBrush    = new(Color.Parse("#9D4EDD"));
    private static readonly SolidColorBrush ChapterBrush = new(Color.Parse("#EDE8F5"));
    private static readonly SolidColorBrush MissingBrush = new(Color.Parse("#6B7280"));

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2) return ChapterBrush;
        if (values[0] is NodeKind.Part) return PartBrush;
        return values[1] is true ? MissingBrush : ChapterBrush;
    }
}

public class NodeKindIsChapterConverter : IValueConverter
{
    public static readonly NodeKindIsChapterConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is NodeKind.Chapter;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Returns true when NodeKind == Chapter AND IsMissing == false.</summary>
public class NodeKindIsChapterAndPresentConverter : IMultiValueConverter
{
    public static readonly NodeKindIsChapterAndPresentConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2) return false;
        return values[0] is NodeKind.Chapter && values[1] is false;
    }
}

public class NodeKindIsPartConverter : IValueConverter
{
    public static readonly NodeKindIsPartConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is NodeKind.Part;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts bool → SynthwavePurpleBrush (active) or OnSurfaceDimBrush (inactive).
/// Used for section-header icons in the explorer bar so the lit icon matches the expanded state.
/// </summary>
public sealed class ActiveIconBrushConverter : IValueConverter
{
    public static readonly ActiveIconBrushConverter Instance = new();

    private static readonly IBrush ActiveBrush  = new SolidColorBrush(Color.Parse("#9D4EDD"));
    private static readonly IBrush InactiveBrush = new SolidColorBrush(Color.Parse("#88919B"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? ActiveBrush : InactiveBrush;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts <see cref="bool"/> → <see cref="GridLength(1, Star)"/> when true,
/// <see cref="GridLength(0)"/> when false.  Used to collapse hidden right-rail sections.
/// </summary>
public sealed class BoolToGridLengthConverter : IValueConverter
{
    public static readonly BoolToGridLengthConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
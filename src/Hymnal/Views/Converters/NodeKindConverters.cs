using System;
using System.Globalization;
using Avalonia;
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

    // Parts get the purple accent; chapters get the full-brightness text colour
    private static readonly SolidColorBrush PartBrush    = new(Color.Parse("#9D4EDD"));
    private static readonly SolidColorBrush ChapterBrush = new(Color.Parse("#EDE8F5")); // OnSurfaceBrush

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is NodeKind.Part ? PartBrush : ChapterBrush;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}


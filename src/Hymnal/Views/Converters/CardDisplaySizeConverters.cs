using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Hymnal.ViewModels;

namespace Hymnal.Views.Converters;

/// <summary>
/// Returns true when the current <see cref="CardDisplaySize"/> equals the converter parameter.
/// </summary>
public sealed class CardDisplaySizeEqualsConverter : IValueConverter
{
    public static readonly CardDisplaySizeEqualsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not CardDisplaySize current || parameter is null)
            return false;

        return Enum.TryParse<CardDisplaySize>(parameter.ToString(), out var expected)
               && current == expected;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Returns true when the current <see cref="CardDisplaySize"/> is at least the converter parameter size.
/// </summary>
public sealed class CardDisplaySizeAtLeastConverter : IValueConverter
{
    public static readonly CardDisplaySizeAtLeastConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not CardDisplaySize current || parameter is null)
            return false;

        return Enum.TryParse<CardDisplaySize>(parameter.ToString(), out var minimum)
               && current >= minimum;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

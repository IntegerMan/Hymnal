using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Hymnal.Views.Converters;

/// <summary>
/// Converts a <see cref="Hymnal.ViewModels.ShellMode"/> value to a boolean
/// indicating whether it matches the requested mode name.
/// </summary>
public sealed class ShellModeEqualsConverter : IValueConverter
{
    public static readonly ShellModeEqualsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || parameter is null)
            return false;

        var requested = parameter.ToString();
        return string.Equals(value.ToString(), requested, StringComparison.Ordinal);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

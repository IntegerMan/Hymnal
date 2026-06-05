using System;
using System.Globalization;

namespace Hymnal.Core.Services;

/// <summary>
/// Parses clipboard / typed values for Gantt DataGrid date and progress columns.
/// </summary>
public static class GanttCellValueParser
{
    private static readonly string[] DateFormats =
    [
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "MM/dd/yyyy",
        "M/d/yyyy"
    ];

    public static bool TryParseDate(string? raw, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var trimmed = raw.Trim();
        var parsed = GanttProjection.ParseDate(trimmed);
        if (parsed.HasValue)
        {
            date = parsed.Value;
            return true;
        }

        if (DateOnly.TryParseExact(trimmed, DateFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out date))
            return true;

        return false;
    }

    public static bool TryParseProgress(string? raw, out double progress)
    {
        progress = 0;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var trimmed = raw.Trim().TrimEnd('%').Trim();
        if (!double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            && !double.TryParse(trimmed, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            return false;

        progress = Math.Clamp(value, 0.0, 100.0);
        return true;
    }
}

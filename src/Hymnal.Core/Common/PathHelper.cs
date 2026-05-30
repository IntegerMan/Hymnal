using System;
using System.IO;

namespace Hymnal.Core.Common;

/// <summary>
/// File-system path utilities.
/// </summary>
public static class PathHelper
{
    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="a"/> and <paramref name="b"/>
    /// refer to the same file-system path after full normalisation: relative segments
    /// (<c>..</c>) are resolved, separators are canonicalised, and the comparison is
    /// case-insensitive (safe for Windows and macOS desktop targets).
    /// Returns <see langword="false"/> when either argument is <see langword="null"/> or
    /// empty.
    /// </summary>
    public static bool IsSamePath(string? a, string? b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return false;

        return string.Equals(
            Path.GetFullPath(a),
            Path.GetFullPath(b),
            StringComparison.OrdinalIgnoreCase);
    }
}

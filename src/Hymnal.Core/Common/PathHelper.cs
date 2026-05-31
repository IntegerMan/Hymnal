using System;
using System.IO;

namespace Hymnal.Core.Common;

public static class PathHelper
{
    public static bool IsSamePath(string? a, string? b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return false;

        // Normalise backslashes to the platform separator before resolution
        // so paths authored on Windows compare correctly on Linux/macOS.
        var sepChar = Path.DirectorySeparatorChar;
        return string.Equals(
            Path.GetFullPath(a.Replace((char)92, sepChar)),
            Path.GetFullPath(b.Replace((char)92, sepChar)),
            StringComparison.OrdinalIgnoreCase);
    }
}

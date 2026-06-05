using Hymnal.Core.Models;

namespace Hymnal.Core.Services;

/// <summary>
/// Filters lock files from Git change counts and UI display.
/// Excluded files are still committed via <c>git add --all</c>.
/// </summary>
public static class GitChangeDisplayFilter
{
    public static IReadOnlyList<GitChangedFile> Apply(IReadOnlyList<GitChangedFile> files)
        => files.Where(file => !ShouldExclude(file.RelativePath)).ToList();

    public static bool ShouldExclude(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/');
        return normalized.EndsWith(".lock", StringComparison.OrdinalIgnoreCase);
    }
}

using Hymnal.Core.Models;

namespace Hymnal.Core.Services;

/// <summary>
/// Display labels for changed files in the sync dialog.
/// </summary>
public static class GitChangedFileDisplay
{
    public static string FormatLabel(GitChangedFile file)
        => $"{file.RelativePath}  {FormatStatusLabel(file.StatusCode)}";

    private static string FormatStatusLabel(string statusCode)
    {
        if (statusCode.Contains('?', StringComparison.Ordinal))
            return "untracked";

        return statusCode switch
        {
            "M" or "MM" or "AM" => "modified",
            "A" or "AA" => "added",
            "D" or "DD" => "deleted",
            "R" => "renamed",
            "C" => "copied",
            _ => statusCode.ToLowerInvariant()
        };
    }
}

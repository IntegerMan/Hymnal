using Hymnal.Core.Models;

namespace Hymnal.Core.Services;

/// <summary>
/// Parses <c>git status --porcelain</c> output into changed file entries.
/// </summary>
public static class GitPorcelainParser
{
    public static IReadOnlyList<GitChangedFile> Parse(string stdout)
    {
        if (string.IsNullOrWhiteSpace(stdout))
            return Array.Empty<GitChangedFile>();

        var results = new List<GitChangedFile>();
        foreach (var rawLine in stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.TrimEnd();
            if (line.Length < 4)
                continue;

            var statusCode = line[..2].Trim();
            var pathPart = line[3..].Trim();
            if (string.IsNullOrWhiteSpace(pathPart))
                continue;

            var relativePath = ExtractPath(pathPart);
            if (string.IsNullOrWhiteSpace(relativePath))
                continue;

            results.Add(new GitChangedFile(NormalizePath(relativePath), statusCode));
        }

        return results;
    }

    private static string ExtractPath(string pathPart)
    {
        const string renameMarker = " -> ";
        var renameIndex = pathPart.IndexOf(renameMarker, StringComparison.Ordinal);
        if (renameIndex >= 0)
            pathPart = pathPart[(renameIndex + renameMarker.Length)..];

        return Unquote(pathPart.Trim());
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            return value[1..^1].Replace("\\\"", "\"").Replace("\\\\", "\\");

        return value;
    }

    private static string NormalizePath(string path)
        => path.Replace('\\', '/');
}

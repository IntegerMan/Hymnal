using Hymnal.Core.Models;

namespace Hymnal.Core.Services;

public static class BookTxtParser
{
    /// <summary>
    /// Parses Book.txt lines without per-chapter disk I/O. Titles default to filenames;
    /// use <see cref="EnrichNodesAsync"/> to resolve headings, part markers, and missing flags.
    /// </summary>
    public static IReadOnlyList<ChapterNode> ParseLinesOnly(string folderPath, IEnumerable<string> bookTxtLines)
    {
        var nodes = new List<ChapterNode>();
        int index = 0;

        foreach (var raw in bookTxtLines)
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            var relativePath = line.Replace('\\', '/');
            var title = Path.GetFileNameWithoutExtension(line);

            nodes.Add(new ChapterNode(
                Key: relativePath,
                RelativePath: relativePath,
                Title: title,
                Kind: NodeKind.Chapter,
                IsMissing: false,
                Index: index++
            ));
        }

        return nodes.AsReadOnly();
    }

    public static IReadOnlyList<ChapterNode> Parse(string folderPath, IEnumerable<string> bookTxtLines)
    {
        var nodes = new List<ChapterNode>();
        int index = 0;

        foreach (var raw in bookTxtLines)
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            // Normalize to forward slashes for Key/RelativePath
            var relativePath = line.Replace('\\', '/');
            var absolutePath = Path.Combine(folderPath, line);

            bool isMissing = !File.Exists(absolutePath);
            NodeKind kind = NodeKind.Chapter;
            string title = Path.GetFileNameWithoutExtension(line);

            if (!isMissing)
            {
                var firstLines = ReadFirstNonBlankLines(absolutePath, 3);

                if (firstLines.Any(l => l.Trim() == "{class: part}"))
                    kind = NodeKind.Part;

                var heading = firstLines.FirstOrDefault(l => l.TrimStart().StartsWith("# "));
                if (heading != null)
                    title = heading.TrimStart().Substring(2).Trim();
            }

            nodes.Add(new ChapterNode(
                Key: relativePath,
                RelativePath: relativePath,
                Title: title,
                Kind: kind,
                IsMissing: isMissing,
                Index: index++
            ));
        }

        return nodes.AsReadOnly();
    }

    public static async Task<IReadOnlyList<ChapterNode>> ParseAsync(
        string folderPath,
        IEnumerable<string> bookTxtLines,
        CancellationToken cancellationToken = default)
    {
        var entries = new List<(string RelativePath, string Line, int Index)>();
        int index = 0;

        foreach (var raw in bookTxtLines)
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            var relativePath = line.Replace('\\', '/');
            entries.Add((relativePath, line, index++));
        }

        var tasks = entries.Select(entry => EnrichEntryAsync(folderPath, entry, cancellationToken));
        var nodes = await Task.WhenAll(tasks).ConfigureAwait(false);
        return nodes.OrderBy(n => n.Index).ToList().AsReadOnly();
    }

    private static async Task<ChapterNode> EnrichEntryAsync(
        string folderPath,
        (string RelativePath, string Line, int Index) entry,
        CancellationToken cancellationToken)
    {
        var absolutePath = Path.Combine(folderPath, entry.Line);
        var exists = await Task.Run(() => File.Exists(absolutePath), cancellationToken).ConfigureAwait(false);

        if (!exists)
        {
            return new ChapterNode(
                Key: entry.RelativePath,
                RelativePath: entry.RelativePath,
                Title: Path.GetFileNameWithoutExtension(entry.Line),
                Kind: NodeKind.Chapter,
                IsMissing: true,
                Index: entry.Index);
        }

        var firstLines = await ReadFirstNonBlankLinesAsync(absolutePath, 3, cancellationToken).ConfigureAwait(false);
        var kind = firstLines.Any(l => l.Trim() == "{class: part}") ? NodeKind.Part : NodeKind.Chapter;
        var title = Path.GetFileNameWithoutExtension(entry.Line);

        var heading = firstLines.FirstOrDefault(l => l.TrimStart().StartsWith("# "));
        if (heading != null)
            title = heading.TrimStart().Substring(2).Trim();

        return new ChapterNode(
            Key: entry.RelativePath,
            RelativePath: entry.RelativePath,
            Title: title,
            Kind: kind,
            IsMissing: false,
            Index: entry.Index);
    }

    /// <summary>
    /// Scans the first few non-blank lines of <paramref name="text"/> and returns the
    /// Markua/ATX heading title (the text after the leading <c># </c>), or <c>null</c>
    /// if no heading is found in the first 5 non-blank lines.
    /// </summary>
    public static string? ExtractTitleFromText(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        using var reader = new StringReader(text);
        string? line;
        int nonBlankCount = 0;
        while ((line = reader.ReadLine()) != null && nonBlankCount < 5)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            nonBlankCount++;
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("# "))
                return trimmed.Substring(2).Trim();
        }
        return null;
    }

    private static List<string> ReadFirstNonBlankLines(string filePath, int count)
    {
        var result = new List<string>(count);
        foreach (var line in File.ReadLines(filePath))
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                result.Add(line);
                if (result.Count >= count)
                    break;
            }
        }
        return result;
    }

    private static async Task<List<string>> ReadFirstNonBlankLinesAsync(
        string filePath,
        int count,
        CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var result = new List<string>(count);
            foreach (var line in File.ReadLines(filePath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    result.Add(line);
                    if (result.Count >= count)
                        break;
                }
            }

            return result;
        }, cancellationToken).ConfigureAwait(false);
    }
}

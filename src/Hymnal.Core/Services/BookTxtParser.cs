using Hymnal.Core.Models;

namespace Hymnal.Core.Services;

public static class BookTxtParser
{
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

    /// <summary>
    /// Scans the first few non-blank lines of <paramref name="text"/> and returns the
    /// Markua/ATX heading title (the text after the leading <c># </c>), or <c>null</c>
    /// if no heading is found in the first 5 non-blank lines.
    /// </summary>
    public static string? ExtractTitleFromText(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        using var reader = new System.IO.StringReader(text);
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
}

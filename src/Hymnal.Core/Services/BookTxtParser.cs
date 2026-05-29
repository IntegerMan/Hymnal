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

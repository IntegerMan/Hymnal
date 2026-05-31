namespace Hymnal.Core.Services;

public sealed class WordCountService
{
    /// <summary>
    /// Counts whitespace-delimited tokens in <paramref name="content"/>, excluding
    /// Markua directive/attribute lines whose trimmed form starts with '{'.
    /// </summary>
    public int CountWords(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        var total = 0;
        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            var trimmed = line.TrimStart();

            // Skip Markua attribute/directive lines that start with '{'
            if (trimmed.StartsWith('{'))
                continue;

            total += line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        return total;
    }
}

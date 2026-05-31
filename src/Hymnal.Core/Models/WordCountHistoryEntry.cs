namespace Hymnal.Core.Models;

public sealed class WordCountHistoryEntry
{
    public string Uuid { get; init; } = "";
    public string Date { get; init; } = ""; // ISO 8601 date-only e.g. "2026-05-30"
    public int WordCount { get; init; }
}

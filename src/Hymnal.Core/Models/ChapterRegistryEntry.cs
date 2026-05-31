namespace Hymnal.Core.Models;

public sealed class ChapterRegistryEntry
{
    public string Uuid { get; init; } = "";
    public string CurrentPath { get; init; } = "";
    public bool Orphaned { get; init; }
    public string? Title { get; init; }
}

namespace Hymnal.Core.Models.Ai;

/// <summary>
/// Snapshot of the context at conversation creation time. Informational only — does not constrain future turns.
/// </summary>
public record ContextTag(
    string View,
    string Scope,
    string? ChapterPath,
    string? ChapterTitle);

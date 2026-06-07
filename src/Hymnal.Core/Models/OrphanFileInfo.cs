namespace Hymnal.Core.Models;

/// <summary>
/// A manuscript <c>.md</c> file that exists on disk but is not listed in <c>Book.txt</c>.
/// </summary>
public sealed record OrphanFileInfo(
    string RelativePath,
    string Title,
    string? DetectedPartFolder);

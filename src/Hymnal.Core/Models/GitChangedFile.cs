namespace Hymnal.Core.Models;

/// <summary>
/// A single changed path reported by <c>git status --porcelain</c>.
/// </summary>
public sealed record GitChangedFile(string RelativePath, string StatusCode);

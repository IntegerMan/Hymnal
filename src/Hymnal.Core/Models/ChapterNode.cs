namespace Hymnal.Core.Models;

public enum NodeKind { Part, Chapter }

public record ChapterNode(string Key, string RelativePath, string Title, NodeKind Kind, bool IsMissing, int Index);

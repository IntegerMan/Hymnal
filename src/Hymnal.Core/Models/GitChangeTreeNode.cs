namespace Hymnal.Core.Models;

public enum GitChangeTreeNodeKind
{
    Folder,
    File
}

/// <summary>
/// Folder/file node for displaying changed paths in the sync dialog tree.
/// </summary>
public sealed record GitChangeTreeNode(
    string Key,
    string DisplayName,
    string RelativePath,
    GitChangeTreeNodeKind Kind,
    string? StatusLabel,
    IReadOnlyList<GitChangeTreeNode> Children)
{
    public bool IsFolder => Kind == GitChangeTreeNodeKind.Folder;

    public bool IsFile => Kind == GitChangeTreeNodeKind.File;
}

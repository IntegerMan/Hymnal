namespace Hymnal.Core.Models;

public enum SupplementalDocNodeKind
{
    Folder,
    File
}

public sealed record SupplementalDocNode(
    string Key,
    string DisplayName,
    string RelativePath,
    string AbsolutePath,
    SupplementalDocNodeKind Kind,
    IReadOnlyList<SupplementalDocNode> Children)
{
    public bool IsFolder => Kind == SupplementalDocNodeKind.Folder;

    public bool IsFile => Kind == SupplementalDocNodeKind.File;
}

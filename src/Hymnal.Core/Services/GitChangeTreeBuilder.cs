using Hymnal.Core.Models;

namespace Hymnal.Core.Services;

/// <summary>
/// Builds a folder/file tree from flat changed-file paths for the sync dialog.
/// </summary>
public static class GitChangeTreeBuilder
{
    public static IReadOnlyList<GitChangeTreeNode> Build(IReadOnlyList<GitChangedFile> files)
    {
        if (files.Count == 0)
            return Array.Empty<GitChangeTreeNode>();

        var root = new Dictionary<string, MutableNode>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files.OrderBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase))
        {
            var segments = file.RelativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
                continue;

            var current = root;
            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                var isLeaf = i == segments.Length - 1;
                var relativePath = string.Join('/', segments.AsSpan(0, i + 1).ToArray());

                if (!current.TryGetValue(segment, out var node))
                {
                    node = new MutableNode(segment, relativePath, isLeaf ? GitChangeTreeNodeKind.File : GitChangeTreeNodeKind.Folder);
                    current[segment] = node;
                }

                if (isLeaf)
                {
                    node.Kind = GitChangeTreeNodeKind.File;
                    node.StatusLabel = FormatStatusLabel(file.StatusCode);
                }

                current = node.Children;
            }
        }

        return ProjectChildren(root);
    }

    private static IReadOnlyList<GitChangeTreeNode> ProjectChildren(Dictionary<string, MutableNode> nodes)
        => nodes.Values
            .OrderBy(node => node.Kind == GitChangeTreeNodeKind.Folder ? 0 : 1)
            .ThenBy(node => node.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ProjectNode)
            .ToList();

    private static GitChangeTreeNode ProjectNode(MutableNode node)
    {
        var displayName = node.Kind == GitChangeTreeNodeKind.File && !string.IsNullOrWhiteSpace(node.StatusLabel)
            ? $"{node.Name}  {node.StatusLabel}"
            : node.Name;

        return new GitChangeTreeNode(
            node.RelativePath,
            displayName,
            node.RelativePath,
            node.Kind,
            node.StatusLabel,
            ProjectChildren(node.Children));
    }

    private static string FormatStatusLabel(string statusCode)
    {
        if (statusCode.Contains('?', StringComparison.Ordinal))
            return "untracked";

        return statusCode switch
        {
            "M" or "MM" or "AM" => "modified",
            "A" or "AA" => "added",
            "D" or "DD" => "deleted",
            "R" => "renamed",
            "C" => "copied",
            _ => statusCode.ToLowerInvariant()
        };
    }

    private sealed class MutableNode
    {
        public MutableNode(string name, string relativePath, GitChangeTreeNodeKind kind)
        {
            Name = name;
            RelativePath = relativePath;
            Kind = kind;
        }

        public string Name { get; }
        public string RelativePath { get; }
        public GitChangeTreeNodeKind Kind { get; set; }
        public string? StatusLabel { get; set; }
        public Dictionary<string, MutableNode> Children { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}

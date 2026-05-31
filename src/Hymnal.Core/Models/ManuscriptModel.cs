using System.Collections.Generic;
using System.IO;
using DynamicData;

namespace Hymnal.Core.Models;

public class ManuscriptModel
{
    private readonly SourceCache<ChapterNode, string> _cache = new(n => n.RelativePath);

    public IObservableCache<ChapterNode, string> Nodes => _cache.AsObservableCache();

    /// <summary>The root folder of the workspace (the folder the user opened).</summary>
    public string WorkspaceRoot { get; private set; } = string.Empty;

    /// <summary>The folder that contains Book.txt (workspace root or workspace/manuscript/).</summary>
    public string ManuscriptRoot { get; private set; } = string.Empty;

    /// <summary>Absolute path to Book.txt (derived from ManuscriptRoot).</summary>
    public string BookTxtPath => Path.Combine(ManuscriptRoot, "Book.txt");

    public void Load(IEnumerable<ChapterNode> nodes)
    {
        _cache.EditDiff(nodes, (a, b) => a.RelativePath == b.RelativePath);
    }

    /// <summary>Populated by ManuscriptService after loading nodes.</summary>
    public void SetRoots(string workspaceRoot, string manuscriptRoot)
    {
        WorkspaceRoot = workspaceRoot;
        ManuscriptRoot = manuscriptRoot;
    }
}

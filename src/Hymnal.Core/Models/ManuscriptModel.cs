using DynamicData;

namespace Hymnal.Core.Models;

public class ManuscriptModel
{
    private readonly SourceCache<ChapterNode, string> _cache = new(n => n.RelativePath);

    public IObservableCache<ChapterNode, string> Nodes => _cache.AsObservableCache();

    public void Load(IEnumerable<ChapterNode> nodes)
    {
        _cache.EditDiff(nodes, (a, b) => a.RelativePath == b.RelativePath);
    }
}

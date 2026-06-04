using System;
using System.Collections.Generic;
using Hymnal.Core.Models;

namespace Hymnal.ViewModels;

public enum CorkboardItemKind
{
    PartDivider,
    EmptyPartHint,
    ChapterCard
}

/// <summary>
/// Discriminated corkboard row wrapper for the mixed set of part dividers,
/// empty-part hints, and chapter cards.
/// </summary>
public abstract class CorkboardItemViewModel : ViewModelBase, IDisposable
{
    public abstract CorkboardItemKind Kind { get; }

    public virtual bool IsSelectableCard => false;

    public virtual string Title => string.Empty;

    public virtual string RelativePath => string.Empty;

    public virtual bool IsMissing => false;

    public virtual CardViewModel? Card => null;

    public static IReadOnlyList<CorkboardItemViewModel> Project(IReadOnlyList<ChapterViewModel> chapters)
    {
        var items = new List<CorkboardItemViewModel>();

        for (var i = 0; i < chapters.Count; i++)
        {
            var chapter = chapters[i];

            if (chapter.Node.Kind == NodeKind.Part)
            {
                items.Add(new PartDividerItemViewModel(chapter));

                var hasChapterBetween = false;
                for (var j = i + 1; j < chapters.Count; j++)
                {
                    if (chapters[j].Node.Kind == NodeKind.Part)
                        break;

                    if (chapters[j].Node.Kind == NodeKind.Chapter)
                    {
                        hasChapterBetween = true;
                        break;
                    }
                }

                if (!hasChapterBetween)
                    items.Add(new EmptyPartHintItemViewModel(chapter));

                continue;
            }

            if (chapter.Node.Kind == NodeKind.Chapter)
                items.Add(new ChapterCardItemViewModel(new CardViewModel(chapter)));
        }

        return items;
    }

    public static void DisposeItems(IEnumerable<CorkboardItemViewModel> items)
    {
        foreach (var item in items)
            item.Dispose();
    }

    private bool _disposed;

    public virtual void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Disposables.Dispose();
    }
}

public sealed class PartDividerItemViewModel : CorkboardItemViewModel
{
    public ChapterViewModel Part { get; }

    public override CorkboardItemKind Kind => CorkboardItemKind.PartDivider;

    public override string Title => Part.Node.Title;

    public override string RelativePath => Part.Node.RelativePath;

    public PartDividerItemViewModel(ChapterViewModel part)
    {
        Part = part;
    }
}

public sealed class EmptyPartHintItemViewModel : CorkboardItemViewModel
{
    public ChapterViewModel Part { get; }

    public string HintText => $"No chapters in {Part.Node.Title}";

    public override CorkboardItemKind Kind => CorkboardItemKind.EmptyPartHint;

    public override string Title => Part.Node.Title;

    public override string RelativePath => Part.Node.RelativePath;

    public EmptyPartHintItemViewModel(ChapterViewModel part)
    {
        Part = part;
    }
}

public sealed class ChapterCardItemViewModel : CorkboardItemViewModel
{
    public override CorkboardItemKind Kind => CorkboardItemKind.ChapterCard;

    public override bool IsSelectableCard => true;

    public override string Title => Card.Title;

    public override string RelativePath => Card.RelativePath;

    public override bool IsMissing => Card.IsMissing;

    public override CardViewModel Card { get; }

    public ChapterCardItemViewModel(CardViewModel card)
    {
        Card = card;
    }

    public override void Dispose()
    {
        if (Card is not null)
            Card.Dispose();

        base.Dispose();
    }
}

using System;
using Hymnal.Core.Models.Ai;
using ReactiveUI;

namespace Hymnal.ViewModels.Ai;

/// <summary>
/// List-item view-model for the conversation drawer.
/// </summary>
public class ConversationListItemViewModel : ViewModelBase
{
    private string _title;
    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public string Id { get; }
    public string Role { get; }
    public string Scope { get; }
    public DateTimeOffset UpdatedAt { get; }
    public int MessageCount { get; }
    public bool Archived { get; }

    public string RelativeDate => FormatRelativeDate(UpdatedAt);

    public ConversationListItemViewModel(ConversationMetadata meta)
    {
        Id = meta.Id;
        _title = meta.Title;
        Role = meta.Role;
        Scope = meta.ContextTag.Scope;
        UpdatedAt = meta.UpdatedAt;
        MessageCount = meta.MessageCount;
        Archived = meta.Archived;
    }

    private static string FormatRelativeDate(DateTimeOffset date)
    {
        var diff = DateTimeOffset.UtcNow - date;
        if (diff.TotalMinutes < 1) return "just now";
        if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalDays < 1) return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
        return date.ToString("MMM d");
    }
}

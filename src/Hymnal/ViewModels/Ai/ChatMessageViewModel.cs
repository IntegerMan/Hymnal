using System;
using ReactiveUI;

namespace Hymnal.ViewModels.Ai;

/// <summary>
/// Represents a single message in the chat view — user, assistant, or system.
/// </summary>
public class ChatMessageViewModel : ViewModelBase
{
    private string _content;
    public string Content
    {
        get => _content;
        set => this.RaiseAndSetIfChanged(ref _content, value);
    }

    public string Role { get; }
    public string Id { get; }
    public DateTimeOffset Timestamp { get; }
    public bool IsUser => Role == "user";
    public bool IsAssistant => Role == "assistant";
    public bool IsSystem => Role == "system";
    public bool IsStreaming { get; private set; }

    public ChatMessageViewModel(string id, string role, string content, DateTimeOffset timestamp)
    {
        Id = id;
        Role = role;
        _content = content;
        Timestamp = timestamp;
    }

    internal void AppendChunk(string chunk)
    {
        _content += chunk;
        this.RaisePropertyChanged(nameof(Content));
    }

    internal void FinalizeStreaming()
    {
        IsStreaming = false;
        this.RaisePropertyChanged(nameof(IsStreaming));
    }

    internal void SetStreaming()
    {
        IsStreaming = true;
        this.RaisePropertyChanged(nameof(IsStreaming));
    }
}

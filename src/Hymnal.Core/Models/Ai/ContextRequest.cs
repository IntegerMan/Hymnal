namespace Hymnal.Core.Models.Ai;

/// <summary>
/// Carries all inputs an IContextBuilder needs to assemble the prompt prefix.
/// ShellMode is resolved by the ViewModel before constructing this record.
/// </summary>
public record ContextRequest(
    ConversationScope Scope,
    string WorkspaceRoot,
    string ManuscriptRoot,
    string BookTxtPath,
    string? ActiveChapterRelativePath,
    /// <summary>Live (possibly unsaved) text of the active chapter from the editor buffer.</summary>
    string? LiveActiveChapterText,
    string RoleSystemPrompt,
    int ScopeTokenBudget = 4000,
    int HistoryTokenBudget = 2000);

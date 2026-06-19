using Hymnal.Core.Models.Ai;

namespace Hymnal.Core.Interfaces;

/// <summary>
/// Provides system-prompt text and role availability rules for AI roles.
/// </summary>
public interface IRolePromptProvider
{
    /// <summary>
    /// Returns the full system prompt (common preamble + role-specific body) for the given role.
    /// </summary>
    string GetSystemPrompt(AiRole role);

    /// <summary>
    /// Returns roles available for the given view, ordered by suggestion priority.
    /// The first entry is the default role for that view.
    /// </summary>
    IReadOnlyList<AiRole> GetAvailableRoles(string shellModeName);

    /// <summary>
    /// Returns the default role for the given view.
    /// </summary>
    AiRole GetDefaultRole(string shellModeName);
}

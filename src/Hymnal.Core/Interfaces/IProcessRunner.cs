using Hymnal.Core.Models;

namespace Hymnal.Core.Interfaces;

/// <summary>
/// Process execution seam used by Git services and tests.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Runs a process directly without invoking a shell.
    /// </summary>
    Task<GitCommandResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default);
}

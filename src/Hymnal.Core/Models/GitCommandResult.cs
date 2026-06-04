namespace Hymnal.Core.Models;

/// <summary>
/// Structured result from an invoked Git command.
/// </summary>
public sealed record GitCommandResult(
    string FileName,
    IReadOnlyList<string> Arguments,
    string? WorkingDirectory,
    int ExitCode,
    string Stdout,
    string Stderr)
{
    public bool IsSuccess => ExitCode == 0;

    public string CommandText => string.Join(" ", new[] { FileName }.Concat(Arguments));

    public static GitCommandResult Failure(
        string fileName,
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        string stderr,
        int exitCode = -1)
        => new(fileName, arguments, workingDirectory, exitCode, string.Empty, stderr);
}

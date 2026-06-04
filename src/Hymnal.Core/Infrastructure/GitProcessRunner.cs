using System.Diagnostics;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;

namespace Hymnal.Core.Infrastructure;

/// <summary>
/// Production process runner for Git. Starts processes directly and captures stdout/stderr.
/// </summary>
public sealed class GitProcessRunner : IProcessRunner
{
    public async Task<GitCommandResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        if (!string.IsNullOrWhiteSpace(workingDirectory))
            startInfo.WorkingDirectory = workingDirectory;

        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        try
        {
            using var process = new Process { StartInfo = startInfo };
            if (!process.Start())
                return GitCommandResult.Failure(fileName, arguments, workingDirectory, "Failed to start git process.");

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            var stdout = await stdoutTask.ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);

            return new GitCommandResult(
                fileName,
                arguments.ToArray(),
                workingDirectory,
                process.ExitCode,
                stdout,
                stderr);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GitCommandResult.Failure(fileName, arguments, workingDirectory, ex.Message);
        }
    }
}

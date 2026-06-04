using System.ComponentModel;
using System.Diagnostics;
using Hymnal.Core.Infrastructure;
using Hymnal.Core.Models;
using Hymnal.Core.Services;

namespace Hymnal.Core.Tests.Services;

public sealed class ProcessGitServiceLocalRepoTests : IDisposable
{
    private readonly string _root;
    private readonly string _workspaceRoot;
    private readonly string _remoteRoot;
    private readonly string _manuscriptPath;
    private readonly GitProcessRunner _gitRunner = new();
    private readonly ProcessGitService _service;
    private bool _initialized;

    public ProcessGitServiceLocalRepoTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "hymnal-git-service-local-repo-tests", Guid.NewGuid().ToString("N"));
        _workspaceRoot = Path.Combine(_root, "workspace");
        _remoteRoot = Path.Combine(_root, "remote.git");
        _manuscriptPath = Path.Combine(_workspaceRoot, "manuscript", "chapter-01.md");
        _service = new ProcessGitService(_gitRunner);
    }

    [Fact]
    public async Task CommitOnlyAndCommitPlusPush_WorkAgainstARealLocalRepository()
    {
        if (!await CanStartProcessAsync("git", Array.Empty<string>(), null))
            return;

        await InitializeRepositoryAsync();

        var availability = await _service.CheckGitAvailableAsync();
        Assert.True(availability.IsSuccess, availability.Error);
        Assert.True(availability.Value!.IsSuccess);

        var initialStatus = await _service.GetRepositoryStatusAsync(_workspaceRoot);
        Assert.True(initialStatus.IsSuccess, initialStatus.Error);
        Assert.Equal("main", initialStatus.Value!.BranchName);
        Assert.Equal(0, initialStatus.Value.UncommittedChangeCount);

        await File.AppendAllTextAsync(_manuscriptPath, "\nLocal repo integration change 1\n");

        var dirtyStatus = await _service.GetRepositoryStatusAsync(_workspaceRoot);
        Assert.True(dirtyStatus.IsSuccess, dirtyStatus.Error);
        Assert.Equal("main", dirtyStatus.Value!.BranchName);
        Assert.Equal(1, dirtyStatus.Value.UncommittedChangeCount);
        Assert.True(dirtyStatus.Value.StatusResult!.Stdout.Contains("chapter-01.md", StringComparison.OrdinalIgnoreCase));

        var commitMessageOne = CreateCommitMessage();
        var commitOnly = await _service.StageAllAndCommitAsync(_workspaceRoot, commitMessageOne);
        Assert.True(commitOnly.IsSuccess, commitOnly.Error);
        Assert.True(commitOnly.Value!.IsSuccess);

        var localCommitMessage = await ReadGitAsync(_workspaceRoot, "log", "-1", "--format=%s", "HEAD");
        Assert.StartsWith(CreateCommitMessagePrefix(), localCommitMessage.Stdout.Trim());

        var cleanStatus = await _service.GetRepositoryStatusAsync(_workspaceRoot);
        Assert.True(cleanStatus.IsSuccess, cleanStatus.Error);
        Assert.Equal(0, cleanStatus.Value!.UncommittedChangeCount);

        await File.AppendAllTextAsync(_manuscriptPath, "\nLocal repo integration change 2\n");

        var dirtyAgain = await _service.GetRepositoryStatusAsync(_workspaceRoot);
        Assert.True(dirtyAgain.IsSuccess, dirtyAgain.Error);
        Assert.Equal(1, dirtyAgain.Value!.UncommittedChangeCount);

        var commitMessageTwo = CreateCommitMessage();
        var commitAndPush = await _service.StageAllCommitAndPushAsync(_workspaceRoot, commitMessageTwo);
        Assert.True(commitAndPush.IsSuccess, commitAndPush.Error);
        Assert.True(commitAndPush.Value!.IsSuccess);

        var pushedRemoteHead = await ReadGitAsync(_remoteRoot, "--git-dir", _remoteRoot, "rev-parse", "refs/heads/main");
        var localHead = await ReadGitAsync(_workspaceRoot, "rev-parse", "HEAD");
        Assert.Equal(localHead.Stdout.Trim(), pushedRemoteHead.Stdout.Trim());

        var remoteCommitMessage = await ReadGitAsync(_remoteRoot, "--git-dir", _remoteRoot, "log", "-1", "--format=%s", "refs/heads/main");
        Assert.StartsWith(CreateCommitMessagePrefix(), remoteCommitMessage.Stdout.Trim());

        var cleanAfterPush = await _service.GetRepositoryStatusAsync(_workspaceRoot);
        Assert.True(cleanAfterPush.IsSuccess, cleanAfterPush.Error);
        Assert.Equal(0, cleanAfterPush.Value!.UncommittedChangeCount);
        Assert.Equal("main", cleanAfterPush.Value.BranchName);
    }

    private async Task InitializeRepositoryAsync()
    {
        if (_initialized)
            return;

        Directory.CreateDirectory(_workspaceRoot);
        Directory.CreateDirectory(Path.Combine(_workspaceRoot, "manuscript"));
        Directory.CreateDirectory(_remoteRoot);

        await File.WriteAllTextAsync(Path.Combine(_workspaceRoot, "Book.txt"), "manuscript/chapter-01.md\n");
        await File.WriteAllTextAsync(_manuscriptPath, "# Chapter 1\n\nInitial text.\n");

        await RunGitAsync(_workspaceRoot, "init");
        await RunGitAsync(_workspaceRoot, "checkout", "-b", "main");
        await RunGitAsync(_workspaceRoot, "config", "user.name", "Hymnal Test");
        await RunGitAsync(_workspaceRoot, "config", "user.email", "hymnal.test@example.com");
        await RunGitAsync(_remoteRoot, "init", "--bare");
        await RunGitAsync(_workspaceRoot, "remote", "add", "origin", _remoteRoot);
        await RunGitAsync(_workspaceRoot, "add", "--all");
        await RunGitAsync(_workspaceRoot, "commit", "-m", "Initial commit");
        await RunGitAsync(_workspaceRoot, "push", "-u", "origin", "main");

        _initialized = true;
    }

    private async Task<GitCommandResult> ReadGitAsync(string workingDirectory, params string[] arguments)
    {
        var result = await RunGitAsync(workingDirectory, arguments);
        Assert.True(result.IsSuccess, BuildFailureMessage(result));
        return result;
    }

    private async Task<GitCommandResult> RunGitAsync(string workingDirectory, params string[] arguments)
    {
        var result = await _gitRunner.RunAsync("git", arguments, workingDirectory);
        Assert.True(result.IsSuccess, BuildFailureMessage(result));
        return result;
    }

    private static string BuildFailureMessage(GitCommandResult result)
        => $"git {result.CommandText} failed with exit code {result.ExitCode}.\nstdout:\n{result.Stdout}\nstderr:\n{result.Stderr}";

    private static string CreateCommitMessage()
        => $"Hymnal: save progress {DateTime.UtcNow:O}";

    private static string CreateCommitMessagePrefix()
        => "Hymnal: save progress ";

    private static async Task<bool> CanStartProcessAsync(string fileName, IReadOnlyList<string> arguments, string? workingDirectory)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            if (!string.IsNullOrWhiteSpace(workingDirectory))
                psi.WorkingDirectory = workingDirectory;

            foreach (var argument in arguments)
                psi.ArgumentList.Add(argument);

            using var process = new Process { StartInfo = psi };
            if (!process.Start())
                return false;

            await process.StandardOutput.ReadToEndAsync();
            await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            return true;
        }
        catch (Win32Exception)
        {
            return false;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // Best-effort cleanup for temp Git repositories.
        }
    }

}

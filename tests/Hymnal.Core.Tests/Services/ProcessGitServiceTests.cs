using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;
using Hymnal.Core.Services;

namespace Hymnal.Core.Tests.Services;

public class ProcessGitServiceTests
{
    private const string WorkspaceRoot = "/repo/workspace";

    [Fact]
    public async Task CheckGitAvailableAsync_ReturnsSuccessfulVersionResult()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(0, "git version 2.50.0\n", string.Empty);
        var service = new ProcessGitService(runner);

        var result = await service.CheckGitAvailableAsync();

        Assert.True(result.IsSuccess, result.Error);
        Assert.True(result.Value!.IsSuccess);
        Assert.Equal("git version 2.50.0\n", result.Value.Stdout);
        var call = Assert.Single(runner.Calls);
        Assert.Equal("git", call.FileName);
        Assert.Equal(new[] { "--version" }, call.Arguments);
        Assert.Null(call.WorkingDirectory);
    }

    [Fact]
    public async Task GetRepositoryStatusAsync_HidesWhenGitUnavailable()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(-1, string.Empty, "git executable not found");
        var service = new ProcessGitService(runner);

        var result = await service.GetRepositoryStatusAsync(WorkspaceRoot);

        Assert.True(result.IsSuccess, result.Error);
        var status = result.Value!;
        Assert.False(status.IsGitAvailable);
        Assert.False(status.IsRepository);
        Assert.Null(status.BranchName);
        Assert.Equal(0, status.UncommittedChangeCount);
        Assert.Equal("git executable not found", status.ProbeResult.Stderr);
        Assert.Single(runner.Calls);
    }

    [Fact]
    public async Task GetRepositoryStatusAsync_HidesWhenWorkspaceIsNotRepository()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(0, "git version 2.50.0\n", string.Empty);
        runner.Enqueue(128, string.Empty, "fatal: not a git repository");
        var service = new ProcessGitService(runner);

        var result = await service.GetRepositoryStatusAsync(WorkspaceRoot);

        Assert.True(result.IsSuccess, result.Error);
        var status = result.Value!;
        Assert.True(status.IsGitAvailable);
        Assert.False(status.IsRepository);
        Assert.Null(status.BranchName);
        Assert.Equal(0, status.UncommittedChangeCount);
        Assert.Equal("fatal: not a git repository", status.ProbeResult.Stderr);
        Assert.Equal(2, runner.Calls.Count);
        Assert.Equal(new[] { "-C", WorkspaceRoot, "rev-parse", "--is-inside-work-tree" }, runner.Calls[1].Arguments);
    }

    [Fact]
    public async Task CheckGitAvailableAsync_ReturnsStructuredFailureWhenRunnerThrows()
    {
        var runner = new ThrowingProcessRunner(new InvalidOperationException("process launch failed"));
        var service = new ProcessGitService(runner);

        var result = await service.CheckGitAvailableAsync();

        Assert.True(result.IsSuccess, result.Error);
        Assert.False(result.Value!.IsSuccess);
        Assert.Equal(-1, result.Value.ExitCode);
        Assert.Equal("process launch failed", result.Value.Stderr);
        Assert.Equal(new[] { "--version" }, result.Value.Arguments);
    }

    [Fact]
    public async Task GetRepositoryStatusAsync_ParsesBranchAndCountsPorcelainLines()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(0, "git version 2.50.0\n", string.Empty);
        runner.Enqueue(0, "true\n", string.Empty);
        runner.Enqueue(0, "feature/git-toolbar\n", string.Empty);
        runner.Enqueue(0, " M manuscript/ch01.md\nA  docs/outline.md\n?? manuscript/ch02.md\nR  old.md -> new.md\n", string.Empty);
        var service = new ProcessGitService(runner);

        var result = await service.GetRepositoryStatusAsync(WorkspaceRoot);

        Assert.True(result.IsSuccess, result.Error);
        var status = result.Value!;
        Assert.True(status.IsGitAvailable);
        Assert.True(status.IsRepository);
        Assert.Equal("feature/git-toolbar", status.BranchName);
        Assert.Equal(4, status.UncommittedChangeCount);
        Assert.Equal(new[] { "-C", WorkspaceRoot, "branch", "--show-current" }, runner.Calls[2].Arguments);
        Assert.Equal(new[] { "-C", WorkspaceRoot, "status", "--porcelain" }, runner.Calls[3].Arguments);
    }

    [Fact]
    public async Task GetRepositoryStatusAsync_UsesHeadWhenBranchOutputIsEmpty()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(0, "git version 2.50.0\n", string.Empty);
        runner.Enqueue(0, "true\n", string.Empty);
        runner.Enqueue(0, "\n", string.Empty);
        runner.Enqueue(0, string.Empty, string.Empty);
        var service = new ProcessGitService(runner);

        var result = await service.GetRepositoryStatusAsync(WorkspaceRoot);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal("HEAD", result.Value!.BranchName);
        Assert.Equal(0, result.Value.UncommittedChangeCount);
    }

    [Fact]
    public async Task StageAllAndCommitAsync_StagesThenCommitsInOrder()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(0, string.Empty, string.Empty);
        runner.Enqueue(0, "[main abc123] Save draft\n", string.Empty);
        var service = new ProcessGitService(runner);

        var result = await service.StageAllAndCommitAsync(WorkspaceRoot, "Save draft");

        Assert.True(result.IsSuccess, result.Error);
        Assert.True(result.Value!.IsSuccess);
        Assert.Equal("[main abc123] Save draft\n", result.Value.Stdout);
        Assert.Equal(2, runner.Calls.Count);
        Assert.Equal(new[] { "-C", WorkspaceRoot, "add", "--all" }, runner.Calls[0].Arguments);
        Assert.Equal(new[] { "-C", WorkspaceRoot, "commit", "-m", "Save draft" }, runner.Calls[1].Arguments);
        Assert.All(runner.Calls, call => Assert.Equal("git", call.FileName));
        Assert.All(runner.Calls, call => Assert.Null(call.WorkingDirectory));
    }

    [Fact]
    public async Task StageAllAndCommitAsync_ReturnsStageFailureAndSkipsCommit()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(128, string.Empty, "fatal: index.lock exists");
        var service = new ProcessGitService(runner);

        var result = await service.StageAllAndCommitAsync(WorkspaceRoot, "Save draft");

        Assert.True(result.IsSuccess, result.Error);
        Assert.False(result.Value!.IsSuccess);
        Assert.Equal("fatal: index.lock exists", result.Value.Stderr);
        Assert.Single(runner.Calls);
        Assert.Equal(new[] { "-C", WorkspaceRoot, "add", "--all" }, runner.Calls[0].Arguments);
    }

    [Fact]
    public async Task StageAllCommitAndPushAsync_StagesCommitsThenPushesInOrder()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(0, string.Empty, string.Empty);
        runner.Enqueue(0, "[main abc123] Save draft\n", string.Empty);
        runner.Enqueue(0, "To origin\n", string.Empty);
        var service = new ProcessGitService(runner);

        var result = await service.StageAllCommitAndPushAsync(WorkspaceRoot, "Save draft");

        Assert.True(result.IsSuccess, result.Error);
        Assert.True(result.Value!.IsSuccess);
        Assert.Equal("To origin\n", result.Value.Stdout);
        Assert.Equal(3, runner.Calls.Count);
        Assert.Equal(new[] { "-C", WorkspaceRoot, "add", "--all" }, runner.Calls[0].Arguments);
        Assert.Equal(new[] { "-C", WorkspaceRoot, "commit", "-m", "Save draft" }, runner.Calls[1].Arguments);
        Assert.Equal(new[] { "-C", WorkspaceRoot, "push" }, runner.Calls[2].Arguments);
    }

    [Fact]
    public async Task StageAllCommitAndPushAsync_ReturnsCommitFailureAndSkipsPush()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(0, string.Empty, string.Empty);
        runner.Enqueue(1, string.Empty, "nothing to commit, working tree clean");
        var service = new ProcessGitService(runner);

        var result = await service.StageAllCommitAndPushAsync(WorkspaceRoot, "Save draft");

        Assert.True(result.IsSuccess, result.Error);
        Assert.False(result.Value!.IsSuccess);
        Assert.Equal("nothing to commit, working tree clean", result.Value.Stderr);
        Assert.Equal(2, runner.Calls.Count);
    }

    [Fact]
    public async Task StageAllCommitAndPushAsync_PreservesRawPushStderr()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue(0, string.Empty, string.Empty);
        runner.Enqueue(0, "[main abc123] Save draft\n", string.Empty);
        runner.Enqueue(1, string.Empty, "fatal: unable to access 'https://example.invalid/repo.git': Could not resolve host\n");
        var service = new ProcessGitService(runner);

        var result = await service.StageAllCommitAndPushAsync(WorkspaceRoot, "Save draft");

        Assert.True(result.IsSuccess, result.Error);
        Assert.False(result.Value!.IsSuccess);
        Assert.Equal("fatal: unable to access 'https://example.invalid/repo.git': Could not resolve host\n", result.Value.Stderr);
        Assert.Equal(new[] { "-C", WorkspaceRoot, "push" }, runner.Calls[2].Arguments);
    }

    private sealed class FakeProcessRunner : IProcessRunner
    {
        private readonly Queue<GitCommandResult> _results = new();

        public List<ProcessCall> Calls { get; } = new();

        public void Enqueue(int exitCode, string stdout, string stderr)
            => _results.Enqueue(new GitCommandResult("git", Array.Empty<string>(), null, exitCode, stdout, stderr));

        public Task<GitCommandResult> RunAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            string? workingDirectory = null,
            CancellationToken cancellationToken = default)
        {
            Calls.Add(new ProcessCall(fileName, arguments.ToArray(), workingDirectory));

            if (_results.Count == 0)
                throw new InvalidOperationException("No fake process result was queued.");

            var result = _results.Dequeue();
            return Task.FromResult(result with
            {
                FileName = fileName,
                Arguments = arguments.ToArray(),
                WorkingDirectory = workingDirectory
            });
        }
    }

    private sealed class ThrowingProcessRunner : IProcessRunner
    {
        private readonly Exception _exception;

        public ThrowingProcessRunner(Exception exception)
        {
            _exception = exception;
        }

        public Task<GitCommandResult> RunAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            string? workingDirectory = null,
            CancellationToken cancellationToken = default)
            => Task.FromException<GitCommandResult>(_exception);
    }

    private sealed record ProcessCall(string FileName, IReadOnlyList<string> Arguments, string? WorkingDirectory);
}

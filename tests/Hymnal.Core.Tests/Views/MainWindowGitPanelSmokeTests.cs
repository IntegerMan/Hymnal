using System;
using System.IO;
using ReactiveUI.Builder;
using Xunit;

namespace Hymnal.Views;

public sealed class MainWindowGitPanelSmokeTests
{
    static MainWindowGitPanelSmokeTests()
    {
        RxAppBuilder.CreateReactiveUIBuilder()
            .WithCoreServices()
            .BuildApp();
    }

    [Fact]
    public void MainWindowAndGitCommitDialogWireGitToolbarAndDialogActions()
    {
        var mainWindowAxaml = File.ReadAllText(FindRepositoryFile("src/Hymnal/Views/MainWindow.axaml"));
        Assert.Contains("GitPanelViewModel.IsVisible", mainWindowAxaml);
        Assert.Contains("GitPanelViewModel.BranchName", mainWindowAxaml);
        Assert.Contains("GitPanelViewModel.UncommittedChangeCount", mainWindowAxaml);
        Assert.Contains("Click=\"CommitGitButton_Click\"", mainWindowAxaml);

        var mainWindowCodeBehind = File.ReadAllText(FindRepositoryFile("src/Hymnal/Views/MainWindow.axaml.cs"));
        Assert.Contains("ExecuteGitCommitActionAsync", mainWindowCodeBehind);
        Assert.Contains("new GitCommitDialog(vm.GitPanelViewModel.CreateDefaultCommitMessage())", mainWindowCodeBehind);
        Assert.Contains("CommitGitButton_Click", mainWindowCodeBehind);

        var dialogAxaml = File.ReadAllText(FindRepositoryFile("src/Hymnal/Views/GitCommitDialog.axaml"));
        Assert.Contains("x:Name=\"CommitMessageBox\"", dialogAxaml);
        Assert.Contains("Commit only", dialogAxaml);
        Assert.Contains("Commit &amp; Push", dialogAxaml);
        Assert.Contains("Cancel", dialogAxaml);

        var dialogCodeBehind = File.ReadAllText(FindRepositoryFile("src/Hymnal/Views/GitCommitDialog.axaml.cs"));
        Assert.Contains("CommitMessageBox.Text = initialMessage;", dialogCodeBehind);
        Assert.Contains("public string CommitMessage => CommitMessageBox.Text ?? string.Empty;", dialogCodeBehind);
        Assert.Contains("CommitOnlyButton_Click", dialogCodeBehind);
        Assert.Contains("CommitAndPushButton_Click", dialogCodeBehind);
        Assert.Contains("CancelButton_Click", dialogCodeBehind);
    }

    private static string FindRepositoryFile(string relativePath)
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate))
                return candidate;

            current = current.Parent;
        }

        throw new FileNotFoundException($"Could not find repository file '{relativePath}'.");
    }
}

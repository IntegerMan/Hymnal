using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Hymnal.Core.Models;
using Hymnal.ViewModels;

namespace Hymnal.Views;

public enum GitCommitDialogAction
{
    CommitOnly,
    CommitAndPush,
    Sync
}

public partial class GitCommitDialog : Window
{
    public GitCommitDialogAction? Result { get; private set; }

    public GitCommitDialog() : this(string.Empty, null)
    {
    }

    public GitCommitDialog(string initialMessage, IReadOnlyList<GitChangedFile>? changedFiles)
    {
        InitializeComponent();
        DataContext = new GitCommitDialogViewModel(initialMessage, changedFiles);
        CommitMessageBox.Text = initialMessage;
        Opened += (_, _) => CommitMessageBox.Focus();
    }

    public string CommitMessage => CommitMessageBox.Text ?? string.Empty;

    private void SyncButton_Click(object? sender, RoutedEventArgs e)
    {
        Result = GitCommitDialogAction.Sync;
        Close();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close();
    }
}

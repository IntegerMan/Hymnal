using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Hymnal.Views;

public enum GitCommitDialogAction
{
    CommitOnly,
    CommitAndPush
}

public partial class GitCommitDialog : Window
{
    public GitCommitDialogAction? Result { get; private set; }

    public GitCommitDialog() : this(string.Empty)
    {
    }

    public GitCommitDialog(string initialMessage)
    {
        InitializeComponent();
        CommitMessageBox.Text = initialMessage;
        Opened += (_, _) => CommitMessageBox.Focus();
    }

    public string CommitMessage => CommitMessageBox.Text ?? string.Empty;

    private void CommitOnlyButton_Click(object? sender, RoutedEventArgs e)
    {
        Result = GitCommitDialogAction.CommitOnly;
        Close();
    }

    private void CommitAndPushButton_Click(object? sender, RoutedEventArgs e)
    {
        Result = GitCommitDialogAction.CommitAndPush;
        Close();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close();
    }
}

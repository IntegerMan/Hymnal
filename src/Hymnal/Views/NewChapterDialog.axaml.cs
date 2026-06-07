using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Hymnal.Views;

public enum NewManuscriptEntryKind
{
    Chapter,
    Part
}

public sealed record NewChapterDialogResult(string FilePath, string? Title);

public partial class NewChapterDialog : Window
{
    private NewManuscriptEntryKind _kind = NewManuscriptEntryKind.Chapter;

    public NewChapterDialog()
    {
        InitializeComponent();
    }

    public static async Task<NewChapterDialogResult?> ShowAsync(
        Window owner,
        NewManuscriptEntryKind kind,
        string title,
        string prompt,
        string suggestedPath,
        string? suggestedTitle = null)
    {
        var dialog = new NewChapterDialog
        {
            Title = title,
            _kind = kind
        };

        dialog.PromptText.Text = prompt;
        dialog.FilePathInput.Text = suggestedPath;
        dialog.TitleInput.Text = suggestedTitle ?? string.Empty;

        if (kind == NewManuscriptEntryKind.Part)
        {
            dialog.TitleLabel.Text = "TITLE (required)";
        }

        var result = await dialog.ShowDialog<NewChapterDialogResult?>(owner);
        return result;
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        var path = NormalizePath(FilePathInput.Text);
        if (string.IsNullOrWhiteSpace(path))
        {
            Close(null);
            return;
        }

        var title = TitleInput.Text?.Trim();
        if (_kind == NewManuscriptEntryKind.Part && string.IsNullOrWhiteSpace(title))
        {
            Close(null);
            return;
        }

        Close(new NewChapterDialogResult(path, string.IsNullOrWhiteSpace(title) ? null : title));
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e) => Close(null);

    private static string NormalizePath(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var trimmed = raw.Trim().Replace('\\', '/');
        if (!trimmed.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            trimmed += ".md";

        return trimmed;
    }
}

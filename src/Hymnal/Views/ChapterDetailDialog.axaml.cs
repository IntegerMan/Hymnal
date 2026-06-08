using Avalonia.Interactivity;
using Hymnal.ViewModels;

namespace Hymnal.Views;

public partial class ChapterDetailDialog : Avalonia.Controls.Window
{
    public ChapterDetailDialog()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ChapterDetailViewModel vm)
            vm.RequestImmediateScheduleSave();

        Close();
    }

    protected override void OnClosing(Avalonia.Controls.WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (DataContext is ChapterDetailViewModel vm)
            vm.Dispose();
    }
}

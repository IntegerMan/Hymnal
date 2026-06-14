using System.Reactive;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Hymnal.ViewModels;

namespace Hymnal.Views;

public partial class SidebarView : UserControl
{
    public SidebarView()
    {
        InitializeComponent();
    }

    private void RemoveFromBook_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not WorkspaceViewModel vm) return;
        if (sender is MenuItem { DataContext: ChapterViewModel chapter })
            vm.RemoveFromBookCommand.Execute(chapter.Node.RelativePath)
                .Subscribe(Observer.Create<Unit>(_ => { }));
    }
}

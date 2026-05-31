using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Hymnal.Views;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e) => Close();
}

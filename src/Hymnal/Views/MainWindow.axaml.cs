using Avalonia.Controls;

namespace Hymnal.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // Notification banner state is driven by MainWindowViewModel (HasBanner / BannerMessage).
        // The debug-log subscription that was here in S01 has been removed; the VM handles all
        // notification routing and the 5-second auto-dismiss timer.
    }
}

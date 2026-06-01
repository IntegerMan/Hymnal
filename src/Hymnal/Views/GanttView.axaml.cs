using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Hymnal.Views;

public partial class GanttView : UserControl
{
    public GanttView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

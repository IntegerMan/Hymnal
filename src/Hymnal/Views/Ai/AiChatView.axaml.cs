using Avalonia.Controls;
using Avalonia.Input;
using Hymnal.ViewModels.Ai;

namespace Hymnal.Views.Ai;

public partial class AiChatView : UserControl
{
    public AiChatView()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // Enter submits; Shift+Enter falls through (inserts newline via TextBox default behavior)
        if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            if (DataContext is AiChatViewModel vm)
            {
                // Fire-and-forget via ReactiveCommand ICommand interface
                ((System.Windows.Input.ICommand)vm.SendCommand).Execute(null);
                e.Handled = true;
            }
        }
    }
}

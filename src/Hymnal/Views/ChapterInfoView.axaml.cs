using System;
using Avalonia.Controls;
using Hymnal.Core.Models;
using Hymnal.ViewModels;

namespace Hymnal.Views;

public partial class ChapterInfoView : UserControl
{
    public ChapterInfoView()
    {
        InitializeComponent();

        // Wire the status ComboBox SelectionChanged → SetStatusCommand.
        // SelectedItem is bound OneWay (Status has private setter), so user selections must be
        // forwarded to the command manually.  The guard `status != vm.Status` prevents the
        // feedback loop when the VM itself updates Status (which re-selects the item).
        StatusComboBox.SelectionChanged += OnStatusSelectionChanged;
    }

    private void OnStatusSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ChapterInfoViewModel vm
            && e.AddedItems.Count > 0
            && e.AddedItems[0] is ChapterStatus status
            && status != vm.Status)
        {
            // Fire-and-forget; command ThrownExceptions are handled inside the ViewModel.
            vm.SetStatusCommand.Execute(status).Subscribe(_ => { });
        }
    }
}

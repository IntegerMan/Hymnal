using Avalonia.Controls;
using Hymnal.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;

namespace Hymnal.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Subscribe to notifications — logs to Debug now; S02+ will show banner UI
        if (App.Services?.GetService<NotificationService>() is { } notificationService)
        {
            notificationService.Notifications.Subscribe(
                n => Debug.WriteLine($"[Notification:{n.Kind}] {n.Message}"),
                ex => Debug.WriteLine($"[Notification:Error] {ex.Message}"));
        }
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Infrastructure;
using Hymnal.Core.Services;
using Hymnal.Infrastructure;
using Hymnal.ViewModels;
using Hymnal.Views;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Hymnal;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        services.AddSingleton<NotificationService>();
        services.AddSingleton<INotificationService>(sp => sp.GetRequiredService<NotificationService>());

        // Platform credential store stub — real impl deferred to a future milestone
        services.AddSingleton<ICredentialStore, CredentialStoreStub>();

        // S02 services
        services.AddSingleton<IAppSettingsStore, AppSettingsStore>();
        services.AddSingleton<ManuscriptService>();

        // S03 services
        services.AddSingleton<IMetadataStore, MetadataStore>();

        // M002/S01 services — chapter registry and phase data
        services.AddSingleton<ChapterRegistryService>();
        services.AddSingleton<PhaseDataService>();

        // S04 services
        services.AddSingleton<INotesService, NotesService>();

        // S03 view-models (order matters: EditorViewModel before WorkspaceViewModel)
        services.AddSingleton<EditorViewModel>(sp =>
            new EditorViewModel(
                sp.GetRequiredService<IMetadataStore>(),
                sp.GetRequiredService<INotificationService>()));

        services.AddSingleton<IFolderPickerService>(sp =>
            new FolderPickerService(() =>
                App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime dl
                    ? TopLevel.GetTopLevel(dl.MainWindow)
                    : null));

        services.AddSingleton<WorkspaceViewModel>(sp =>
            new WorkspaceViewModel(
                sp.GetRequiredService<ManuscriptService>(),
                sp.GetRequiredService<IAppSettingsStore>(),
                sp.GetRequiredService<IFolderPickerService>(),
                sp.GetRequiredService<INotificationService>(),
                sp.GetRequiredService<EditorViewModel>(),
                sp.GetRequiredService<ChapterRegistryService>(),
                sp.GetRequiredService<PhaseDataService>()));

        services.AddSingleton<NotesViewModel>(sp =>
            new NotesViewModel(
                sp.GetRequiredService<EditorViewModel>(),
                sp.GetRequiredService<WorkspaceViewModel>(),
                sp.GetRequiredService<INotesService>(),
                sp.GetRequiredService<INotificationService>()));

        services.AddTransient<MainWindowViewModel>(sp =>
            new MainWindowViewModel(
                sp.GetRequiredService<WorkspaceViewModel>(),
                sp.GetRequiredService<EditorViewModel>(),
                sp.GetRequiredService<NotesViewModel>(),
                sp.GetRequiredService<NotificationService>()));

        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = vm };
        }

        base.OnFrameworkInitializationCompleted();
    }
}

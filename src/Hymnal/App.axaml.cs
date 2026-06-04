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
    private ServiceProvider? _serviceProvider;
    private bool _servicesDisposed;

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
        services.AddSingleton<IProcessRunner, GitProcessRunner>();
        services.AddSingleton<IGitService, ProcessGitService>();
        services.AddSingleton<IMetadataStore, MetadataStore>();

        // M002/S01 services — chapter registry and phase data
        services.AddSingleton<ChapterRegistryService>();
        services.AddSingleton<PhaseDataService>();

        // M002/S02 services — word count, targets, history
        services.AddSingleton<WordCountService>();
        services.AddSingleton<TargetsService>();
        services.AddSingleton<WordCountHistoryService>();

        // S04 services
        services.AddSingleton<INotesService, NotesService>();

        // M004/S02 services — supplemental docs sidebar tree
        services.AddSingleton<ISupplementalDocsService, SupplementalDocsService>();

        // S03 view-models (order matters: EditorViewModel before WorkspaceViewModel)
        services.AddSingleton<EditorViewModel>(sp =>
            new EditorViewModel(
                sp.GetRequiredService<IMetadataStore>(),
                sp.GetRequiredService<INotificationService>(),
                sp.GetRequiredService<WordCountService>()));

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
                sp.GetRequiredService<PhaseDataService>(),
                sp.GetRequiredService<TargetsService>(),
                sp.GetRequiredService<WordCountService>(),
                sp.GetRequiredService<WordCountHistoryService>()));

        services.AddSingleton<NotesViewModel>(sp =>
            new NotesViewModel(
                sp.GetRequiredService<EditorViewModel>(),
                sp.GetRequiredService<WorkspaceViewModel>(),
                sp.GetRequiredService<INotesService>(),
                sp.GetRequiredService<INotificationService>(),
                sp.GetRequiredService<IAppSettingsStore>()));

        services.AddSingleton<ChapterInfoViewModel>(sp =>
            new ChapterInfoViewModel(
                sp.GetRequiredService<EditorViewModel>(),
                sp.GetRequiredService<WorkspaceViewModel>(),
                sp.GetRequiredService<PhaseDataService>(),
                sp.GetRequiredService<TargetsService>(),
                sp.GetRequiredService<IAppSettingsStore>(),
                sp.GetRequiredService<INotificationService>()));

        services.AddSingleton<GanttViewModel>(sp =>
            new GanttViewModel(
                sp.GetRequiredService<WorkspaceViewModel>(),
                sp.GetRequiredService<PhaseDataService>(),
                sp.GetRequiredService<INotificationService>()));

        // S01 services — plan corkboard structural editing
        services.AddSingleton<IBookTxtStructureService, BookTxtStructureService>();

        services.AddSingleton<CorkboardViewModel>(sp =>
            new CorkboardViewModel(
                sp.GetRequiredService<WorkspaceViewModel>(),
                sp.GetRequiredService<IBookTxtStructureService>(),
                sp.GetRequiredService<INotificationService>()));

        services.AddSingleton<SupplementalDocsViewModel>(sp =>
            new SupplementalDocsViewModel(
                sp.GetRequiredService<WorkspaceViewModel>(),
                sp.GetRequiredService<ISupplementalDocsService>(),
                sp.GetRequiredService<EditorViewModel>(),
                sp.GetRequiredService<INotificationService>()));

        services.AddSingleton<GitPanelViewModel>(sp =>
            new GitPanelViewModel(
                sp.GetRequiredService<WorkspaceViewModel>(),
                sp.GetRequiredService<EditorViewModel>(),
                sp.GetRequiredService<IGitService>(),
                sp.GetRequiredService<INotificationService>()));

        services.AddTransient<MainWindowViewModel>(sp =>
            new MainWindowViewModel(
                sp.GetRequiredService<WorkspaceViewModel>(),
                sp.GetRequiredService<EditorViewModel>(),
                sp.GetRequiredService<NotesViewModel>(),
                sp.GetRequiredService<ChapterInfoViewModel>(),
                sp.GetRequiredService<GanttViewModel>(),
                sp.GetRequiredService<CorkboardViewModel>(),
                sp.GetRequiredService<SupplementalDocsViewModel>(),
                sp.GetRequiredService<GitPanelViewModel>(),
                sp.GetRequiredService<NotificationService>(),
                sp.GetRequiredService<IAppSettingsStore>()));

        _serviceProvider = services.BuildServiceProvider();
        Services = _serviceProvider;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Exit += (_, _) => DisposeServices();
            var vm = Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = vm };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisposeServices()
    {
        if (_servicesDisposed)
            return;

        _servicesDisposed = true;
        _serviceProvider?.Dispose();
        _serviceProvider = null;
        Services = null;
    }
}

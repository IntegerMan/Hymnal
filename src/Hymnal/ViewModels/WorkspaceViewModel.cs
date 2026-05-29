using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Binding;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;
using Hymnal.Core.Services;
using ReactiveUI;

namespace Hymnal.ViewModels;

public class WorkspaceViewModel : ViewModelBase
{
    private readonly ManuscriptService _manuscriptService;
    private readonly IAppSettingsStore _settingsStore;
    private readonly IFolderPickerService _folderPicker;
    private readonly INotificationService _notificationService;
    private ManuscriptModel? _model;

    private readonly ObservableCollectionExtended<ChapterNode> _nodes = new();
    public ReadOnlyObservableCollection<ChapterNode> Nodes { get; }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    private bool _hasWorkspace;
    public bool HasWorkspace
    {
        get => _hasWorkspace;
        private set => this.RaiseAndSetIfChanged(ref _hasWorkspace, value);
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    public ReactiveCommand<Unit, Unit> OpenWorkspaceCommand { get; }

    public WorkspaceViewModel(
        ManuscriptService manuscriptService,
        IAppSettingsStore settingsStore,
        IFolderPickerService folderPicker,
        INotificationService notificationService)
    {
        _manuscriptService = manuscriptService;
        _settingsStore = settingsStore;
        _folderPicker = folderPicker;
        _notificationService = notificationService;

        Nodes = new ReadOnlyObservableCollection<ChapterNode>(_nodes);

        OpenWorkspaceCommand = ReactiveCommand.CreateFromTask(OpenWorkspaceAsync);

        Disposables.Add(
            OpenWorkspaceCommand.ThrownExceptions
                .Subscribe(Observer.Create<Exception>(ex => _notificationService.ShowError(ex.Message))));
    }

    private async Task OpenWorkspaceAsync()
    {
        var path = await _folderPicker.PickFolderAsync();
        if (path == null)
            return;

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var result = await _manuscriptService.LoadWorkspaceAsync(path);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error;
                _notificationService.ShowError(result.Error!);
                return;
            }

            ErrorMessage = null;
            await _settingsStore.SetAsync("lastWorkspacePath", path);
            BindModel(result.Value!);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task InitAsync()
    {
        var lastPath = await _settingsStore.GetAsync<string>("lastWorkspacePath");
        if (lastPath == null)
            return;

        IsLoading = true;
        try
        {
            var result = await _manuscriptService.LoadWorkspaceAsync(lastPath);
            if (result.IsSuccess)
                BindModel(result.Value!);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void BindModel(ManuscriptModel model)
    {
        _model = model;
        HasWorkspace = true;

        Disposables.Add(
            model.Nodes
                .Connect()
                .SortBy(n => n.Index)
                .Bind(_nodes)
                .Subscribe(Observer.Create<DynamicData.ISortedChangeSet<ChapterNode, string>>(_ => { })));
    }
}

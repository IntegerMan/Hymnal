using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models.Ai;
using Hymnal.Core.Services.Ai;
using Hymnal.ViewModels.Ai;
using ReactiveUI;

namespace Hymnal.ViewModels;

/// <summary>
/// Settings surface view-model. Loaded fresh each time Settings opens (AddTransient).
/// Manages provider profile list; calls AiChatService.RefreshClientAsync on save/delete/activate.
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly IProviderProfileService _profileService;
    private readonly ICredentialStore _credentials;
    private readonly IAiChatService _aiChatService;
    private readonly INotificationService _notifications;

    public ObservableCollection<ProviderProfileViewModel> Profiles { get; } = new();

    private string? _activeProfileId;
    public string? ActiveProfileId
    {
        get => _activeProfileId;
        private set => this.RaiseAndSetIfChanged(ref _activeProfileId, value);
    }

    public ReactiveCommand<Unit, Unit> AddProfileCommand { get; }

    public SettingsViewModel(
        IProviderProfileService profileService,
        ICredentialStore credentials,
        IAiChatService aiChatService,
        INotificationService notifications)
    {
        _profileService = profileService;
        _credentials = credentials;
        _aiChatService = aiChatService;
        _notifications = notifications;

        AddProfileCommand = ReactiveCommand.CreateFromTask(AddProfileAsync);
        Disposables.Add(AddProfileCommand.ThrownExceptions
            .Subscribe(ex => _notifications.ShowError($"Add profile failed: {ex.Message}")));

        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        try
        {
            var profiles = await _profileService.LoadAllAsync().ConfigureAwait(false);
            var active = await _profileService.GetActiveAsync().ConfigureAwait(false);
            ActiveProfileId = active?.Id;

            Profiles.Clear();
            foreach (var p in profiles)
            {
                var vm = CreateProfileViewModel(p, p.Id == ActiveProfileId);
                Profiles.Add(vm);
            }
        }
        catch (Exception ex)
        {
            _notifications.ShowError($"Failed to load settings: {ex.Message}");
        }
    }

    private ProviderProfileViewModel CreateProfileViewModel(ProviderProfile profile, bool isActive)
    {
        var vm = new ProviderProfileViewModel(profile, isActive, _profileService, _credentials);

        Disposables.Add(vm.SaveCommand.Subscribe(u => { var t = OnProfileSavedAsync(vm); }));
        Disposables.Add(vm.DeleteCommand.Subscribe(u => { var t = OnProfileDeletedAsync(vm); }));
        Disposables.Add(vm.SetActiveCommand.Subscribe(u => { var t = OnSetActiveAsync(vm); }));

        return vm;
    }

    private async Task AddProfileAsync()
    {
        var defaultType = ProviderType.OpenAI;
        var defaults    = ProviderProfileViewModel.TypeDefaults[defaultType];
        var newProfile  = new ProviderProfile(
            Id: Guid.NewGuid().ToString(),
            DisplayName: defaultType.ToString(),
            BaseUrl: defaults.BaseUrl,
            ModelId: defaults.ModelId,
            ContextWindowTokens: null,
            Type: defaultType);

        await _profileService.SaveAsync(newProfile).ConfigureAwait(false);
        var vm = CreateProfileViewModel(newProfile, false);
        Profiles.Add(vm);
    }

    private async Task OnProfileSavedAsync(ProviderProfileViewModel vm)
    {
        await _aiChatService.RefreshClientAsync().ConfigureAwait(false);
    }

    private async Task OnProfileDeletedAsync(ProviderProfileViewModel vm)
    {
        var existing = Profiles.FirstOrDefault(p => p.Id == vm.Id);
        if (existing is not null) Profiles.Remove(existing);
        await _aiChatService.RefreshClientAsync().ConfigureAwait(false);
    }

    private async Task OnSetActiveAsync(ProviderProfileViewModel vm)
    {
        ActiveProfileId = vm.Id;
        foreach (var p in Profiles) p.IsActive = p.Id == vm.Id;
        this.RaisePropertyChanged(nameof(Profiles));
        await _aiChatService.RefreshClientAsync().ConfigureAwait(false);
    }
}

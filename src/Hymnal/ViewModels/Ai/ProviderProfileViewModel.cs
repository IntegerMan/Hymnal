using System;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Linq;
using Hymnal.Core.Common;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models.Ai;
using Hymnal.Core.Services.Ai;
using ReactiveUI;

namespace Hymnal.ViewModels.Ai;

/// <summary>
/// Wraps a single ProviderProfile for inline editing in Settings.
/// </summary>
public class ProviderProfileViewModel : ViewModelBase
{
    private readonly IProviderProfileService _profileService;
    private readonly ICredentialStore _credentials;

    // ── Editable fields ──────────────────────────────────────────────────────

    private string _displayName;
    public string DisplayName
    {
        get => _displayName;
        set => this.RaiseAndSetIfChanged(ref _displayName, value);
    }

    private string _baseUrl;
    public string BaseUrl
    {
        get => _baseUrl;
        set => this.RaiseAndSetIfChanged(ref _baseUrl, value);
    }

    private string _modelId;
    public string ModelId
    {
        get => _modelId;
        set => this.RaiseAndSetIfChanged(ref _modelId, value);
    }

    private string _apiKey = string.Empty;
    public string ApiKey
    {
        get => _apiKey;
        set
        {
            this.RaiseAndSetIfChanged(ref _apiKey, value);
            IsApiKeyDirty = true;
        }
    }

    private int? _contextWindowTokens;
    public int? ContextWindowTokens
    {
        get => _contextWindowTokens;
        set => this.RaiseAndSetIfChanged(ref _contextWindowTokens, value);
    }

    public bool IsApiKeyDirty { get; private set; }

    public string Id { get; }
    public bool IsActive { get; set; }

    // ── Validation ───────────────────────────────────────────────────────────

    private string? _baseUrlError;
    public string? BaseUrlError
    {
        get => _baseUrlError;
        private set => this.RaiseAndSetIfChanged(ref _baseUrlError, value);
    }

    // ── Test connection ──────────────────────────────────────────────────────

    private bool _isTesting;
    public bool IsTesting
    {
        get => _isTesting;
        private set => this.RaiseAndSetIfChanged(ref _isTesting, value);
    }

    private string? _testResult;
    public string? TestResult
    {
        get => _testResult;
        private set => this.RaiseAndSetIfChanged(ref _testResult, value);
    }

    private bool _testSucceeded;
    public bool TestSucceeded
    {
        get => _testSucceeded;
        private set => this.RaiseAndSetIfChanged(ref _testSucceeded, value);
    }

    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> TestConnectionCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> SaveCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> DeleteCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> SetActiveCommand { get; }

    public ProviderProfileViewModel(
        ProviderProfile profile,
        bool isActive,
        IProviderProfileService profileService,
        ICredentialStore credentials)
    {
        Id = profile.Id;
        _displayName = profile.DisplayName;
        _baseUrl = profile.BaseUrl;
        _modelId = profile.ModelId;
        _contextWindowTokens = profile.ContextWindowTokens;
        IsActive = isActive;
        _profileService = profileService;
        _credentials = credentials;

        var hasUrlAndModel = this.WhenAnyValue(x => x.BaseUrl, x => x.ModelId,
            (url, model) => Uri.TryCreate(url, UriKind.Absolute, out _) && !string.IsNullOrWhiteSpace(model));

        TestConnectionCommand = ReactiveCommand.CreateFromTask<System.Reactive.Unit, System.Reactive.Unit>(
            async _ => { await TestConnectionAsync().ConfigureAwait(false); return System.Reactive.Unit.Default; },
            canExecute: hasUrlAndModel);
        Disposables.Add(TestConnectionCommand.ThrownExceptions
            .Subscribe(ex => TestResult = $"Error: {ex.Message}"));

        SaveCommand = ReactiveCommand.CreateFromTask<System.Reactive.Unit, System.Reactive.Unit>(
            async _ => { await SaveAsync().ConfigureAwait(false); return System.Reactive.Unit.Default; });
        Disposables.Add(SaveCommand.ThrownExceptions.Subscribe(ex => TestResult = $"Save failed: {ex.Message}"));

        DeleteCommand = ReactiveCommand.CreateFromTask<System.Reactive.Unit, System.Reactive.Unit>(
            async _ => { await profileService.DeleteAsync(Id); return System.Reactive.Unit.Default; });

        SetActiveCommand = ReactiveCommand.CreateFromTask<System.Reactive.Unit, System.Reactive.Unit>(
            async _ => { await profileService.SetActiveAsync(Id); return System.Reactive.Unit.Default; });

        // Inline URL validation on change
        Disposables.Add(
            this.WhenAnyValue(x => x.BaseUrl)
                .Subscribe(url =>
                {
                    BaseUrlError = Uri.TryCreate(url, UriKind.Absolute, out _) || string.IsNullOrWhiteSpace(url)
                        ? null
                        : "Enter a valid URL.";
                }));
    }

    public ProviderProfile ToProfile() =>
        new(Id, DisplayName.Trim(), BaseUrl.Trim(), ModelId.Trim(), ContextWindowTokens);

    private async Task TestConnectionAsync()
    {
        IsTesting = true;
        TestResult = null;
        TestSucceeded = false;
        try
        {
            var result = await _profileService.TestConnectionAsync(ToProfile(), ApiKey, CancellationToken.None)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                TestResult = "Connected.";
                TestSucceeded = true;
            }
            else
            {
                TestResult = result.Error ?? "Connection failed.";
                TestSucceeded = false;
            }
        }
        finally
        {
            IsTesting = false;
        }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
            DisplayName = ModelId;

        await _profileService.SaveAsync(ToProfile()).ConfigureAwait(false);

        if (IsApiKeyDirty)
        {
            await _credentials.StoreAsync(
                ProviderProfileService.ApiKeyCredentialKey(Id), ApiKey).ConfigureAwait(false);
            IsApiKeyDirty = false;
        }
    }
}

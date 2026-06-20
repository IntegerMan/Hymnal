using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hymnal.Core.Common;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models.Ai;
using Hymnal.Core.Services.Ai;
using ReactiveUI;

namespace Hymnal.ViewModels.Ai;

/// <summary>
/// Per-type defaults used both for prefilling and for display.
/// </summary>
public sealed record ProviderTypeDefaults(
    string BaseUrl,
    string ModelId,
    string BaseUrlLabel,
    string ModelIdLabel,
    string ApiKeyHint,
    bool ApiKeyRequired,
    string HelpText);

/// <summary>
/// Wraps a single ProviderProfile for inline editing in Settings.
/// </summary>
public class ProviderProfileViewModel : ViewModelBase
{
    private readonly IProviderProfileService _profileService;
    private readonly ICredentialStore _credentials;

    // ── Static per-type data ─────────────────────────────────────────────────

    public static IReadOnlyList<ProviderType> AvailableTypes { get; } =
        new[] { ProviderType.OpenAI, ProviderType.Ollama, ProviderType.AzureOpenAI, ProviderType.Anthropic };

    public static IReadOnlyDictionary<ProviderType, ProviderTypeDefaults> TypeDefaults { get; } =
        new Dictionary<ProviderType, ProviderTypeDefaults>
        {
            [ProviderType.OpenAI]      = new("https://api.openai.com/v1",            "gpt-4o",            "Base URL",          "Model ID",        "API key",          true,
                                             "Get an API key from platform.openai.com/api-keys."),
            [ProviderType.Ollama]      = new("http://localhost:11434/v1",             "llama3.1",          "Base URL",          "Model ID",        "API key (optional)", false,
                                             "Install Ollama from ollama.com and run it locally (ollama serve). No API key required."),
            [ProviderType.AzureOpenAI] = new("https://<resource>.openai.azure.com",  "my-deployment",     "Resource endpoint", "Deployment name", "API key",          true,
                                             "Create a resource in Azure AI Foundry. The resource endpoint is under Keys & Endpoint; the deployment name comes from your model deployment."),
            [ProviderType.Anthropic]   = new("https://api.anthropic.com",            "claude-sonnet-4-5", "Base URL",          "Model ID",        "API key",          true,
                                             "Get an API key from console.anthropic.com/settings/keys."),
        };

    // ── Type ─────────────────────────────────────────────────────────────────

    private ProviderType _type;
    public ProviderType Type
    {
        get => _type;
        set => this.RaiseAndSetIfChanged(ref _type, value);
    }

    // ── Computed per-type labels ─────────────────────────────────────────────

    private string _baseUrlLabel = "Base URL";
    public string BaseUrlLabel
    {
        get => _baseUrlLabel;
        private set => this.RaiseAndSetIfChanged(ref _baseUrlLabel, value);
    }

    private string _modelIdLabel = "Model ID";
    public string ModelIdLabel
    {
        get => _modelIdLabel;
        private set => this.RaiseAndSetIfChanged(ref _modelIdLabel, value);
    }

    private string _apiKeyHint = "API key";
    public string ApiKeyHint
    {
        get => _apiKeyHint;
        private set => this.RaiseAndSetIfChanged(ref _apiKeyHint, value);
    }

    private bool _apiKeyRequired = true;
    public bool ApiKeyRequired
    {
        get => _apiKeyRequired;
        private set => this.RaiseAndSetIfChanged(ref _apiKeyRequired, value);
    }

    private string _helpText = string.Empty;
    public string HelpText
    {
        get => _helpText;
        private set => this.RaiseAndSetIfChanged(ref _helpText, value);
    }

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
        private set
        {
            this.RaiseAndSetIfChanged(ref _testResult, value);
            this.RaisePropertyChanged(nameof(TestFailed));
        }
    }

    private bool _testSucceeded;
    public bool TestSucceeded
    {
        get => _testSucceeded;
        private set
        {
            this.RaiseAndSetIfChanged(ref _testSucceeded, value);
            this.RaisePropertyChanged(nameof(TestFailed));
        }
    }

    public bool TestFailed => TestResult is not null && !TestSucceeded;

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
        _type = profile.Type;
        _displayName = profile.DisplayName;
        _baseUrl = profile.BaseUrl;
        _modelId = profile.ModelId;
        _contextWindowTokens = profile.ContextWindowTokens;
        IsActive = isActive;
        _profileService = profileService;
        _credentials = credentials;

        ApplyTypeMetadata(_type);

        // On type change: update labels and prefill defaults if fields are empty or are a known default
        Disposables.Add(
            this.WhenAnyValue(x => x.Type)
                .Skip(1)
                .Subscribe(newType =>
                {
                    ApplyTypeMetadata(newType);
                    PrefillDefaultsForType(newType);
                }));

        var canTestOrSave = this.WhenAnyValue(
            x => x.BaseUrl, x => x.ModelId, x => x.ApiKey, x => x.Type,
            (url, model, key, type) =>
                Uri.TryCreate(url, UriKind.Absolute, out _)
                && !string.IsNullOrWhiteSpace(model)
                && (!TypeDefaults[type].ApiKeyRequired || !string.IsNullOrEmpty(key)));

        TestConnectionCommand = ReactiveCommand.CreateFromTask<System.Reactive.Unit, System.Reactive.Unit>(
            async _ => { await TestConnectionAsync().ConfigureAwait(false); return System.Reactive.Unit.Default; },
            canExecute: canTestOrSave);
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
        new(Id, DisplayName.Trim(), BaseUrl.Trim(), ModelId.Trim(), ContextWindowTokens, Type);

    private void ApplyTypeMetadata(ProviderType type)
    {
        var d = TypeDefaults[type];
        BaseUrlLabel    = d.BaseUrlLabel;
        ModelIdLabel    = d.ModelIdLabel;
        ApiKeyHint      = d.ApiKeyHint;
        ApiKeyRequired  = d.ApiKeyRequired;
        HelpText        = d.HelpText;
    }

    // Prefills BaseUrl, ModelId, and DisplayName when the user changes Type and the existing values
    // are empty or are one of the known defaults (meaning the user hasn't customised them).
    private void PrefillDefaultsForType(ProviderType newType)
    {
        var urlIsDefault   = IsKnownDefaultUrl(BaseUrl);
        var modelIsDefault = IsKnownDefaultModel(ModelId);

        var d = TypeDefaults[newType];
        if (string.IsNullOrWhiteSpace(BaseUrl) || urlIsDefault)
            BaseUrl = d.BaseUrl;
        if (string.IsNullOrWhiteSpace(ModelId) || modelIsDefault)
            ModelId = d.ModelId;

        // Auto-derive display name from type so card header stays meaningful
        DisplayName = newType.ToString();
    }

    private static bool IsKnownDefaultUrl(string url)
    {
        foreach (var d in TypeDefaults.Values)
            if (string.Equals(d.BaseUrl, url, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static bool IsKnownDefaultModel(string model)
    {
        foreach (var d in TypeDefaults.Values)
            if (string.Equals(d.ModelId, model, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

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
            DisplayName = Type.ToString();

        await _profileService.SaveAsync(ToProfile()).ConfigureAwait(false);

        if (IsApiKeyDirty)
        {
            await _credentials.StoreAsync(
                ProviderProfileService.ApiKeyCredentialKey(Id), ApiKey).ConfigureAwait(false);
            IsApiKeyDirty = false;
        }
    }
}

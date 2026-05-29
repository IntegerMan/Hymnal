using System;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;
using ReactiveUI;

namespace Hymnal.ViewModels;

/// <summary>
/// Single-buffer chapter editor state. One instance per application lifetime (singleton).
/// Manages open chapter content, dirty state, atomic save, FileSystemWatcher, and conflict detection.
/// </summary>
public class EditorViewModel : ViewModelBase, IDisposable
{
    private readonly IMetadataStore _metadataStore;
    private readonly INotificationService _notificationService;

    private FileSystemWatcher? _watcher;
    private bool _disposed;

    // ── Active chapter ──────────────────────────────────────────────────────

    private ChapterNode? _activeNode;
    public ChapterNode? ActiveNode
    {
        get => _activeNode;
        private set => this.RaiseAndSetIfChanged(ref _activeNode, value);
    }

    private string? _activeFilePath;
    public string? ActiveFilePath
    {
        get => _activeFilePath;
        private set => this.RaiseAndSetIfChanged(ref _activeFilePath, value);
    }

    // ── Buffer content ──────────────────────────────────────────────────────

    private string _text = "";
    public string Text
    {
        get => _text;
        set => this.RaiseAndSetIfChanged(ref _text, value);
    }

    private string _originalText = "";
    public string OriginalText
    {
        get => _originalText;
        private set => this.RaiseAndSetIfChanged(ref _originalText, value);
    }

    // ── Derived state (OAPH) ────────────────────────────────────────────────

    private readonly ObservableAsPropertyHelper<bool> _isDirty;
    public bool IsDirty => _isDirty.Value;

    private readonly ObservableAsPropertyHelper<bool> _canSave;
    public bool CanSave => _canSave.Value;

    // ── Conflict state ──────────────────────────────────────────────────────

    private bool _hasConflict;
    public bool HasConflict
    {
        get => _hasConflict;
        private set => this.RaiseAndSetIfChanged(ref _hasConflict, value);
    }

    private string? _conflictMessage;
    public string? ConflictMessage
    {
        get => _conflictMessage;
        private set => this.RaiseAndSetIfChanged(ref _conflictMessage, value);
    }

    // ── Commands ────────────────────────────────────────────────────────────

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    /// <summary>Accept the external version: reload from disk, clear HasConflict.</summary>
    public ReactiveCommand<Unit, Unit> AcceptExternalCommand { get; }

    /// <summary>Keep local edits: clear HasConflict / ConflictMessage only.</summary>
    public ReactiveCommand<Unit, Unit> KeepLocalCommand { get; }

    public EditorViewModel(IMetadataStore metadataStore, INotificationService notificationService)
    {
        _metadataStore = metadataStore;
        _notificationService = notificationService;

        _isDirty = this.WhenAnyValue(x => x.Text, x => x.OriginalText, (t, o) => t != o)
            .ToProperty(this, x => x.IsDirty);

        _canSave = this.WhenAnyValue(x => x.IsDirty, x => x.ActiveFilePath, (dirty, path) => dirty && path != null)
            .ToProperty(this, x => x.CanSave);

        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync, this.WhenAnyValue(x => x.CanSave));

        AcceptExternalCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (ActiveFilePath == null) return;
            try
            {
                var content = await File.ReadAllTextAsync(ActiveFilePath);
                Text = content;
                OriginalText = content;
                HasConflict = false;
                ConflictMessage = null;
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to reload file from disk: {ex.Message}");
            }
        });

        KeepLocalCommand = ReactiveCommand.Create(() =>
        {
            HasConflict = false;
            ConflictMessage = null;
        });

        // Register OAPHs for disposal so they unsubscribe cleanly.
        Disposables.Add(_isDirty);
        Disposables.Add(_canSave);

        // Register watcher teardown with the composite disposable.
        Disposables.Add(Disposable.Create(StopWatcher));
    }

    // ── Chapter lifecycle ────────────────────────────────────────────────────

    /// <summary>
    /// Opens a chapter: stops any existing watcher, reads file content,
    /// sets Text/OriginalText (clearing dirty), then starts a new FileSystemWatcher.
    /// </summary>
    public async Task OpenChapterAsync(ChapterNode node, string absolutePath)
    {
        StopWatcher();

        var content = await File.ReadAllTextAsync(absolutePath);
        Text = content;
        OriginalText = content;
        ActiveNode = node;
        ActiveFilePath = absolutePath;
        HasConflict = false;
        ConflictMessage = null;

        StartWatcher(absolutePath);
    }

    // ── Save ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Atomically writes Text to ActiveFilePath. On success, sets OriginalText = Text
    /// (clears IsDirty). On failure, calls ShowError and re-throws so save-before-switch
    /// callers can abort the chapter switch.
    /// </summary>
    public async Task SaveAsync()
    {
        if (!CanSave) return;

        try
        {
            await _metadataStore.WriteTextAtomicAsync(ActiveFilePath!, Text);
            OriginalText = Text;
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Save failed: {ex.Message}");
            throw;
        }
    }

    // ── FileSystemWatcher ────────────────────────────────────────────────────

    private void StartWatcher(string absolutePath)
    {
        var dir = Path.GetDirectoryName(absolutePath);
        var fileName = Path.GetFileName(absolutePath);
        if (dir == null || fileName == null) return;

        var watcher = new FileSystemWatcher(dir, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };

        watcher.Changed += OnFileChanged;
        _watcher = watcher;
    }

    private async void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Small delay so the writing process has time to finish the file.
        await Task.Delay(150);

        if (ActiveFilePath == null) return;

        if (!IsDirty)
        {
            try
            {
                var content = await File.ReadAllTextAsync(ActiveFilePath);
                Text = content;
                OriginalText = content;
                _notificationService.ShowInfo(
                    $"'{Path.GetFileName(ActiveFilePath)}' was changed externally and reloaded.");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Auto-reload failed: {ex.Message}");
            }
        }
        else
        {
            HasConflict = true;
            ConflictMessage =
                $"'{Path.GetFileName(ActiveFilePath)}' was changed externally. " +
                "Choose 'Reload from disk' to accept the external version, or 'Keep my edits' to discard it.";
        }
    }

    private void StopWatcher()
    {
        if (_watcher == null) return;
        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnFileChanged;
        _watcher.Dispose();
        _watcher = null;
    }

    // ── IDisposable ──────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Disposables.Dispose();  // triggers StopWatcher via registered Disposable.Create
    }
}

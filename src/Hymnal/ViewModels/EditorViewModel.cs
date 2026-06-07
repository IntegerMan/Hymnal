using System;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;
using Hymnal.Core.Services;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace Hymnal.ViewModels;

/// <summary>
/// Single-buffer chapter editor state. One instance per application lifetime (singleton).
/// Manages open chapter content, dirty state, atomic save, FileSystemWatcher, and conflict detection.
/// </summary>
public class EditorViewModel : ViewModelBase, IDisposable
{
    private readonly IMetadataStore _metadataStore;
    private readonly INotificationService _notificationService;
    private readonly WordCountService _wordCountService;

    private FileSystemWatcher? _watcher;
    private Timer? _watcherDebounceTimer;
    private bool _disposed;
    private bool _suppressDirtyTracking;
    private bool _isDirtyFlag;

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
        set
        {
            if (_text == value)
                return;

            this.RaiseAndSetIfChanged(ref _text, value);
            if (!_suppressDirtyTracking)
                SetDirtyFlag(true);
        }
    }

    private string _originalText = "";
    public string OriginalText
    {
        get => _originalText;
        private set => this.RaiseAndSetIfChanged(ref _originalText, value);
    }

    // ── Derived state (OAPH) ────────────────────────────────────────────────

    public bool IsDirty
    {
        get => _isDirtyFlag;
        private set => this.RaiseAndSetIfChanged(ref _isDirtyFlag, value);
    }

    private readonly ObservableAsPropertyHelper<bool> _canSave;
    public bool CanSave => _canSave.Value;

    private readonly ObservableAsPropertyHelper<bool> _hasActiveChapter;
    public bool HasActiveChapter => _hasActiveChapter.Value;

    private readonly ObservableAsPropertyHelper<bool> _showWorkspacePrompt;
    public bool ShowWorkspacePrompt => _showWorkspacePrompt.Value;

    private readonly ObservableAsPropertyHelper<bool> _showNoChapterPrompt;
    public bool ShowNoChapterPrompt => _showNoChapterPrompt.Value;

    private readonly ObservableAsPropertyHelper<bool> _showNoResearchDocPrompt;
    public bool ShowNoResearchDocPrompt => _showNoResearchDocPrompt.Value;

    private bool _isResearchSurface;
    /// <summary>True when the editor is hosted on the RESEARCH surface (docs only, no chapters).</summary>
    public bool IsResearchSurface
    {
        get => _isResearchSurface;
        set => this.RaiseAndSetIfChanged(ref _isResearchSurface, value);
    }

    private bool _hasWorkspace;
    public bool HasWorkspace
    {
        get => _hasWorkspace;
        set => this.RaiseAndSetIfChanged(ref _hasWorkspace, value);
    }

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

    // ── Special editor states ────────────────────────────────────────────────

    /// <summary>True when Book.txt is open in the editor (book line was clicked).</summary>
    private bool _isBookSelected;
    public bool IsBookSelected
    {
        get => _isBookSelected;
        private set => this.RaiseAndSetIfChanged(ref _isBookSelected, value);
    }

    private readonly ObservableAsPropertyHelper<bool> _showEditor;
    /// <summary>True when there is a chapter or Book.txt loaded — controls editor visibility.</summary>
    public bool ShowEditor => _showEditor.Value;

    private readonly ObservableAsPropertyHelper<bool> _showBookTxtWarning;
    /// <summary>True when Book.txt is open; drives the warning banner in EditorView.</summary>
    public bool ShowBookTxtWarning => _showBookTxtWarning.Value;

    /// <summary>True when the selected chapter is referenced in Book.txt but missing on disk.</summary>
    private bool _showMissingChapterPrompt;
    public bool ShowMissingChapterPrompt
    {
        get => _showMissingChapterPrompt;
        private set => this.RaiseAndSetIfChanged(ref _showMissingChapterPrompt, value);
    }

    private readonly ObservableAsPropertyHelper<string?> _missingChapterMessage;
    /// <summary>Formatted warning message shown in the missing-chapter overlay.</summary>
    public string? MissingChapterMessage => _missingChapterMessage.Value;

    // ── Word count ──────────────────────────────────────────────────────────

    private readonly ObservableAsPropertyHelper<int> _liveWordCount;
    public int LiveWordCount => _liveWordCount.Value;

    // ── Saved signal ─────────────────────────────────────────────────────────

    private readonly Subject<Unit> _savedSubject = new();
    /// <summary>Fires a Unit after every successful atomic save.</summary>
    public IObservable<Unit> Saved => _savedSubject.AsObservable();

    // ── Commands ────────────────────────────────────────────────────────────

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    /// <summary>Accept the external version: reload from disk, clear HasConflict.</summary>
    public ReactiveCommand<Unit, Unit> AcceptExternalCommand { get; }

    /// <summary>Keep local edits: clear HasConflict / ConflictMessage only.</summary>
    public ReactiveCommand<Unit, Unit> KeepLocalCommand { get; }

    public EditorViewModel(IMetadataStore metadataStore, INotificationService notificationService,
        WordCountService wordCountService)
    {
        _metadataStore = metadataStore;
        _notificationService = notificationService;
        _wordCountService = wordCountService;

        _canSave = this.WhenAnyValue(x => x.IsDirty, x => x.ActiveFilePath, (dirty, path) => dirty && path != null)
            .ToProperty(this, x => x.CanSave);

        _hasActiveChapter = this.WhenAnyValue(x => x.ActiveNode)
            .Select(node => node != null)
            .ToProperty(this, x => x.HasActiveChapter);

        _showWorkspacePrompt = this.WhenAnyValue(x => x.HasWorkspace)
            .Select(hasWorkspace => !hasWorkspace)
            .ToProperty(this, x => x.ShowWorkspacePrompt);

        _showNoChapterPrompt = this.WhenAnyValue(
                x => x.HasWorkspace, x => x.ActiveNode, x => x.ActiveFilePath, x => x.IsBookSelected,
                x => x.ShowMissingChapterPrompt, x => x.IsResearchSurface,
                (hasWorkspace, node, path, isBook, isMissing, isResearch) =>
                    !isResearch && hasWorkspace && node == null && path == null && !isBook && !isMissing)
            .ToProperty(this, x => x.ShowNoChapterPrompt);

        _showNoResearchDocPrompt = this.WhenAnyValue(
                x => x.HasWorkspace, x => x.ActiveNode, x => x.ActiveFilePath, x => x.IsBookSelected,
                x => x.ShowMissingChapterPrompt, x => x.IsResearchSurface,
                (hasWorkspace, node, path, isBook, isMissing, isResearch) =>
                    isResearch && hasWorkspace && node == null && path == null && !isBook && !isMissing)
            .ToProperty(this, x => x.ShowNoResearchDocPrompt);
        Disposables.Add(_showNoResearchDocPrompt);

        _showEditor = this.WhenAnyValue(x => x.HasActiveChapter, x => x.IsBookSelected, x => x.ActiveFilePath,
                (ch, book, path) => ch || book || path != null)
            .ToProperty(this, x => x.ShowEditor);
        Disposables.Add(_showEditor);

        _showBookTxtWarning = this.WhenAnyValue(x => x.IsBookSelected)
            .ToProperty(this, x => x.ShowBookTxtWarning);
        Disposables.Add(_showBookTxtWarning);

        _missingChapterMessage = this.WhenAnyValue(x => x.ActiveNode)
            .Select(n => n != null
                ? $"\"{n.Title}\" is referenced in Book.txt but the file could not be found on disk."
                : null)
            .ToProperty(this, x => x.MissingChapterMessage);
        Disposables.Add(_missingChapterMessage);

        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync, this.WhenAnyValue(x => x.CanSave));

        AcceptExternalCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (ActiveFilePath == null) return;
            try
            {
                var content = await File.ReadAllTextAsync(ActiveFilePath);
                SetBufferContent(content);
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
        Disposables.Add(_canSave);

        // Live word count: debounced 300 ms on the thread-pool, result marshalled to UI thread.
        _liveWordCount = this.WhenAnyValue(x => x.Text)
            .Throttle(TimeSpan.FromMilliseconds(300), TaskPoolScheduler.Default)
            .Select(t => _wordCountService.CountWords(t))
            .ObserveOn(AvaloniaScheduler.Instance)
            .ToProperty(this, x => x.LiveWordCount, out _liveWordCount);
        Disposables.Add(_liveWordCount);

        // Register watcher teardown with the composite disposable.
        Disposables.Add(Disposable.Create(StopWatcher));
    }

    private void SetBufferContent(string content)
    {
        _suppressDirtyTracking = true;
        try
        {
            Text = content;
            OriginalText = content;
            SetDirtyFlag(false);
        }
        finally
        {
            _suppressDirtyTracking = false;
        }
    }

    private void SetDirtyFlag(bool dirty)
    {
        if (_isDirtyFlag == dirty)
            return;

        IsDirty = dirty;
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
        SetBufferContent(content);
        ActiveNode = node;
        ActiveFilePath = absolutePath;
        HasConflict = false;
        ConflictMessage = null;
        ShowMissingChapterPrompt = false;
        IsBookSelected = false;

        StartWatcher(absolutePath);
    }

    /// <summary>
    /// Opens an arbitrary workspace file in the single-buffer editor without associating it
    /// with a Book.txt chapter node. Used by supplemental docs and preserves the same save,
    /// dirty-state, and watcher lifecycle as chapter files.
    /// </summary>
    public async Task OpenArbitraryFileAsync(string absolutePath)
    {
        StopWatcher();

        var content = await File.ReadAllTextAsync(absolutePath);
        SetBufferContent(content);
        ActiveNode = null;
        ActiveFilePath = absolutePath;
        HasConflict = false;
        ConflictMessage = null;
        ShowMissingChapterPrompt = false;
        IsBookSelected = false;

        StartWatcher(absolutePath);
    }

    /// <summary>
    /// Shows the missing-chapter warning overlay. The chapter is referenced
    /// in Book.txt but no file was found on disk.
    /// </summary>
    /// <summary>
    /// Updates ActiveNode in-place (e.g. after a title rename on save) so that
    /// downstream observers such as ChapterInfoViewModel see the new title immediately.
    /// </summary>
    public void UpdateActiveNode(ChapterNode updated)
    {
        if (_activeNode?.RelativePath == updated.RelativePath)
            ActiveNode = updated;
    }

    public void OpenMissingChapter(ChapterNode node)
    {
        StopWatcher();
        HasConflict = false;
        ConflictMessage = null;
        ActiveNode = node;
        ActiveFilePath = null;
        SetBufferContent(string.Empty);
        ShowMissingChapterPrompt = true;
        IsBookSelected = false;
    }

    /// <summary>
    /// <summary>
    /// Opens Book.txt in the editor. Shows a warning banner so the user knows edits affect
    /// the chapter list. Saves go to the same file; WorkspaceViewModel observes Saved to reload.
    /// </summary>
    public async Task SelectBookAsync(string bookTxtPath)
    {
        StopWatcher();
        HasConflict = false;
        ConflictMessage = null;
        ShowMissingChapterPrompt = false;
        ActiveNode = null;

        if (File.Exists(bookTxtPath))
        {
            try
            {
                var content = await File.ReadAllTextAsync(bookTxtPath);
                SetBufferContent(content);
                ActiveFilePath = bookTxtPath;
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Could not read Book.txt: {ex.Message}");
                SetBufferContent(string.Empty);
                ActiveFilePath = null;
            }
        }
        else
        {
            SetBufferContent(string.Empty);
            ActiveFilePath = null;
        }

        IsBookSelected = true;
        if (ActiveFilePath != null)
            StartWatcher(ActiveFilePath);
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

        var savePath = ActiveFilePath;
        StopWatcher();

        try
        {
            await _metadataStore.WriteTextAtomicAsync(savePath!, Text);
            _suppressDirtyTracking = true;
            try
            {
                OriginalText = Text;
                SetDirtyFlag(false);
            }
            finally
            {
                _suppressDirtyTracking = false;
            }

            _savedSubject.OnNext(Unit.Default);
            HasConflict = false;
            ConflictMessage = null;
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Save failed: {ex.Message}");
            throw;
        }
        finally
        {
            if (savePath != null && ActiveFilePath == savePath)
                StartWatcher(savePath);
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

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (_watcher == null || ActiveFilePath == null)
            return;

        _watcherDebounceTimer?.Dispose();
        _watcherDebounceTimer = new Timer(
            _ => _ = ProcessExternalFileChangeAsync(),
            null,
            150,
            Timeout.Infinite);
    }

    private async Task ProcessExternalFileChangeAsync()
    {
        var path = ActiveFilePath;
        if (_watcher == null || path == null)
            return;

        string? content;
        try
        {
            content = await File.ReadAllTextAsync(path).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                _notificationService.ShowError($"Auto-reload failed: {ex.Message}"));
            return;
        }

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => ApplyExternalFileChange(content));
    }

    private void ApplyExternalFileChange(string content)
    {
        // Guard again after the async read — state may have changed.
        if (_watcher == null || ActiveFilePath == null) return;

        if (IsBookSelected)
        {
            // Book.txt: always silently reload, never prompt, never notify.
            SetBufferContent(content);
            HasConflict = false;
            ConflictMessage = null;
            return;
        }

        if (!IsDirty)
        {
            SetBufferContent(content);
            _notificationService.ShowInfo(
                $"'{Path.GetFileName(ActiveFilePath)}' was changed externally and reloaded.");
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
        _watcherDebounceTimer?.Dispose();
        _watcherDebounceTimer = null;

        if (_watcher == null) return;
        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnFileChanged;
        _watcher.Dispose();
        _watcher = null;
    }

    /// <summary>
    /// Clears the active chapter and buffer, leaving the editor ready for a new workspace.
    /// </summary>
    public void CloseChapter()
    {
        StopWatcher();
        HasConflict = false;
        ConflictMessage = null;
        ActiveNode = null;
        ActiveFilePath = null;
        SetBufferContent(string.Empty);
        ShowMissingChapterPrompt = false;
        IsBookSelected = false;
    }

    // ── IDisposable ──────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Disposables.Dispose();  // triggers StopWatcher via registered Disposable.Create
    }
}

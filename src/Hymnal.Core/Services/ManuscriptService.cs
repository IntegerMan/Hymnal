using Hymnal.Core.Common;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;

namespace Hymnal.Core.Services;

public sealed class ManuscriptService : IDisposable
{
    private readonly INotificationService _notificationService;
    private FileSystemWatcher? _watcher;
    private Timer? _debounceTimer;
    private SynchronizationContext? _syncContext;
    private int _suppressCount;

    public ManuscriptService(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public Task<Result<ManuscriptModel>> LoadWorkspaceAsync(string folderPath)
    {
        // PRD FR-2: check {workspace}/Book.txt first, then {workspace}/manuscript/Book.txt
        var bookTxtPath = Path.Combine(folderPath, "Book.txt");
        if (!File.Exists(bookTxtPath))
            bookTxtPath = Path.Combine(folderPath, "manuscript", "Book.txt");

        if (!File.Exists(bookTxtPath))
            return Task.FromResult(Result<ManuscriptModel>.Fail(
                "Book.txt not found. Expected at the workspace root or in a 'manuscript' subfolder."));

        var manuscriptRoot = Path.GetDirectoryName(bookTxtPath)!;

        var lines = File.ReadAllLines(bookTxtPath);
        var nodes = BookTxtParser.Parse(manuscriptRoot, lines);

        var model = new ManuscriptModel();
        model.Load(nodes);
        model.SetRoots(folderPath, manuscriptRoot);

        _syncContext = SynchronizationContext.Current;

        DisposeWatcher();
        _watcher = new FileSystemWatcher(manuscriptRoot, "Book.txt")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnBookTxtChanged;
        _watcher.Renamed += OnBookTxtChanged;

        return Task.FromResult(Result<ManuscriptModel>.Ok(model));
    }

    /// <summary>
    /// Temporarily suppress file-watcher notifications for programmatic Book.txt writes.
    /// Dispose the returned guard to resume watching.
    /// </summary>
    public IDisposable SuppressFileWatcher()
    {
        Interlocked.Increment(ref _suppressCount);
        return new WatcherSuppressionGuard(this);
    }

    private void ResumeFileWatcher()
    {
        Interlocked.Decrement(ref _suppressCount);
        _debounceTimer?.Dispose();
        _debounceTimer = null;
    }

    private void OnBookTxtChanged(object sender, FileSystemEventArgs e)
    {
        if (_suppressCount > 0)
            return;

        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(_ =>
        {
            if (_suppressCount > 0)
                return;

            if (_syncContext != null)
                _syncContext.Post(_ => _notificationService.ShowInfo("Book.txt changed — reload?"), null);
            else
                _notificationService.ShowInfo("Book.txt changed — reload?");
        }, null, 300, Timeout.Infinite);
    }

    private sealed class WatcherSuppressionGuard : IDisposable
    {
        private ManuscriptService? _service;

        public WatcherSuppressionGuard(ManuscriptService service) => _service = service;

        public void Dispose()
        {
            Interlocked.Exchange(ref _service, null)?.ResumeFileWatcher();
        }
    }

    private void DisposeWatcher()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnBookTxtChanged;
            _watcher.Renamed -= OnBookTxtChanged;
            _watcher.Dispose();
            _watcher = null;
        }
    }

    public void UnloadWorkspace()
    {
        _debounceTimer?.Dispose();
        _debounceTimer = null;
        _syncContext = null;
        DisposeWatcher();
    }

    public void Dispose()
    {
        _debounceTimer?.Dispose();
        _debounceTimer = null;
        DisposeWatcher();
    }
}

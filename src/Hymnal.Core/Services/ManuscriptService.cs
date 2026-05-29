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

    public ManuscriptService(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public Task<Result<ManuscriptModel>> LoadWorkspaceAsync(string folderPath)
    {
        var bookTxtPath = Path.Combine(folderPath, "Book.txt");

        if (!File.Exists(bookTxtPath))
            return Task.FromResult(Result<ManuscriptModel>.Fail("Book.txt not found in folder"));

        var lines = File.ReadAllLines(bookTxtPath);
        var nodes = BookTxtParser.Parse(folderPath, lines);

        var model = new ManuscriptModel();
        model.Load(nodes);

        _syncContext = SynchronizationContext.Current;

        DisposeWatcher();
        _watcher = new FileSystemWatcher(folderPath, "Book.txt")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnBookTxtChanged;
        _watcher.Renamed += OnBookTxtChanged;

        return Task.FromResult(Result<ManuscriptModel>.Ok(model));
    }

    private void OnBookTxtChanged(object sender, FileSystemEventArgs e)
    {
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(_ =>
        {
            if (_syncContext != null)
                _syncContext.Post(_ => _notificationService.ShowInfo("Book.txt changed — reload?"), null);
            else
                _notificationService.ShowInfo("Book.txt changed — reload?");
        }, null, 300, Timeout.Infinite);
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

    public void Dispose()
    {
        _debounceTimer?.Dispose();
        _debounceTimer = null;
        DisposeWatcher();
    }
}

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Hymnal.Infrastructure;
using ReactiveUI;
using System;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Hymnal.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public WorkspaceViewModel WorkspaceViewModel { get; }
    public EditorViewModel EditorViewModel { get; }

    // ── Window title ──────────────────────────────────────────────────────────

    private string _title = "Hymnal";
    public string Title
    {
        get => _title;
        private set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    // ── Notification banner ───────────────────────────────────────────────────

    private bool _hasBanner;
    public bool HasBanner
    {
        get => _hasBanner;
        set => this.RaiseAndSetIfChanged(ref _hasBanner, value);
    }

    private string? _bannerTitle = "Notice";
    public string? BannerTitle
    {
        get => _bannerTitle;
        set => this.RaiseAndSetIfChanged(ref _bannerTitle, value);
    }

    private string? _bannerMessage;
    public string? BannerMessage
    {
        get => _bannerMessage;
        set => this.RaiseAndSetIfChanged(ref _bannerMessage, value);
    }

    private Infrastructure.NotificationKind _bannerKind;
    public Infrastructure.NotificationKind BannerKind
    {
        get => _bannerKind;
        set => this.RaiseAndSetIfChanged(ref _bannerKind, value);
    }

    // ── Exit ─────────────────────────────────────────────────────────────────

    public ReactiveCommand<Unit, Unit> ExitCommand { get; } =
        ReactiveCommand.Create(() =>
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown();
        });

    public MainWindowViewModel(
        WorkspaceViewModel workspaceViewModel,
        EditorViewModel editorViewModel,
        NotificationService notificationService)
    {
        WorkspaceViewModel = workspaceViewModel;
        EditorViewModel = editorViewModel;

        EditorViewModel.HasWorkspace = WorkspaceViewModel.HasWorkspace;
        Disposables.Add(
            WorkspaceViewModel.WhenAnyValue(x => x.HasWorkspace)
                .Subscribe(hasWorkspace => EditorViewModel.HasWorkspace = hasWorkspace));

        // ── Reactive window title ────────────────────────────────────────────
        // Reflects "• filename — Hymnal" (dirty), "filename — Hymnal" (clean), or "Hymnal" (no chapter).
        Disposables.Add(
            Observable.CombineLatest(
                editorViewModel.WhenAnyValue(x => x.IsDirty),
                editorViewModel.WhenAnyValue(x => x.ActiveNode),
                (dirty, node) =>
                {
                    if (node == null) return "Hymnal";
                    var fileName = Path.GetFileName(node.RelativePath);
                    return dirty
                        ? $"\u2022 {fileName} \u2014 Hymnal"   // • filename — Hymnal
                        : $"{fileName} \u2014 Hymnal";          // filename — Hymnal
                })
            .Subscribe(t => Title = t));

        // ── Notification banner with 5-second auto-dismiss ───────────────────
        // SerialDisposable ensures only the latest timer is active; a new notification resets the clock.
        var timerDisposable = new SerialDisposable();
        Disposables.Add(timerDisposable);

        Disposables.Add(
            notificationService.Notifications
                .Subscribe(n =>
                {
                    HasBanner = true;
                    BannerKind = n.Kind;
                    BannerTitle = n.Kind switch
                    {
                        Hymnal.Infrastructure.NotificationKind.Error => "Error",
                        Hymnal.Infrastructure.NotificationKind.Success => "Success",
                        Hymnal.Infrastructure.NotificationKind.Info => "Info",
                        _ => "Notice"
                    };
                    BannerMessage = n.Message;

                    timerDisposable.Disposable = Observable
                        .Timer(TimeSpan.FromSeconds(5))
                        .Subscribe(_ => HasBanner = false);
                }));

        // Start workspace init (loads last workspace + restores last chapter).
        _ = workspaceViewModel.InitAsync();
    }
}

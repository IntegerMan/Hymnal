using System;
using System.Reactive.Disposables;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using Hymnal.ViewModels;
using ReactiveUI;

namespace Hymnal.Views;

/// <summary>
/// Code-behind for EditorView. Handles:
///   1. Markua XSHD syntax highlighting load on first attach.
///   2. Two-way text sync: ViewModel.Text ↔ PART_Editor.Text
///      (AvaloniaEdit.TextEditor does not expose Text as a bindable AvaloniaProperty,
///      so we wire it manually via TextChanged and WhenAnyValue).
/// </summary>
public partial class EditorView : UserControl
{
    private CompositeDisposable _viewDisposables = new();
    private IDisposable? _vmTextSub;
    private bool _highlightingLoaded;

    public EditorView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _viewDisposables = new CompositeDisposable();

        // Load syntax highlighting once (idempotent via the loaded flag).
        if (!_highlightingLoaded)
        {
            LoadMarkuaHighlighting();
            _highlightingLoaded = true;
        }

        // Editor → VM: fire on every text edit in the AvaloniaEdit surface.
        PART_Editor.TextChanged += OnEditorTextChanged;
        _viewDisposables.Add(Disposable.Create(() =>
        {
            PART_Editor.TextChanged -= OnEditorTextChanged;
        }));

        // VM → Editor: initial sync + react to DataContext changes.
        SubscribeToVmText();
        DataContextChanged += OnDataContextChanged;
        _viewDisposables.Add(Disposable.Create(() =>
        {
            DataContextChanged -= OnDataContextChanged;
            _vmTextSub?.Dispose();
            _vmTextSub = null;
        }));
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _viewDisposables.Dispose();
    }

    // ── VM → Editor ──────────────────────────────────────────────────────────

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _vmTextSub?.Dispose();
        _vmTextSub = null;
        SubscribeToVmText();
    }

    /// <summary>
    /// Observes <c>EditorViewModel.Text</c> and updates <c>PART_Editor.Text</c> when it changes
    /// (handles initial load and external-reload scenarios). The equality guard breaks the
    /// potential notify loop with <see cref="OnEditorTextChanged"/>.
    /// </summary>
    private void SubscribeToVmText()
    {
        if (DataContext is not EditorViewModel vm) return;

        _vmTextSub = vm.WhenAnyValue(x => x.Text)
            .Subscribe(text =>
            {
                if (PART_Editor.Text != text)
                    PART_Editor.Text = text;
            });
    }

    // ── Editor → VM ──────────────────────────────────────────────────────────

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        if (DataContext is EditorViewModel vm)
            vm.Text = PART_Editor.Text;
    }

    // ── Syntax highlighting ───────────────────────────────────────────────────

    private void LoadMarkuaHighlighting()
    {
        try
        {
            using var stream = AssetLoader.Open(
                new Uri("avares://Hymnal/Views/MarkuaHighlighting.xshd"));
            using var reader = XmlReader.Create(stream);
            PART_Editor.SyntaxHighlighting =
                HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[EditorView] Failed to load Markua syntax highlighting: {ex.Message}");
        }
    }
}

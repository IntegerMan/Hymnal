using ReactiveUI;

namespace Hymnal.ViewModels;

/// <summary>
/// Shell view-model for RESEARCH mode. Composes the shared supplemental docs
/// sidebar and editor with a reserved right column for a future AI chat panel.
/// </summary>
public sealed class ResearchViewModel : ViewModelBase
{
    public WorkspaceViewModel WorkspaceViewModel { get; }
    public SupplementalDocsViewModel SupplementalDocsViewModel { get; }
    public EditorViewModel EditorViewModel { get; }

    private readonly ObservableAsPropertyHelper<bool> _hasWorkspace;

    public bool HasWorkspace => _hasWorkspace.Value;

    public ResearchViewModel(
        WorkspaceViewModel workspaceViewModel,
        SupplementalDocsViewModel supplementalDocsViewModel,
        EditorViewModel editorViewModel)
    {
        WorkspaceViewModel = workspaceViewModel;
        SupplementalDocsViewModel = supplementalDocsViewModel;
        EditorViewModel = editorViewModel;

        _hasWorkspace = workspaceViewModel
            .WhenAnyValue(x => x.HasWorkspace)
            .ToProperty(this, x => x.HasWorkspace);
        Disposables.Add(_hasWorkspace);
    }
}

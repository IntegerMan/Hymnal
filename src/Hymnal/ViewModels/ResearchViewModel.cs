using ReactiveUI;
using Hymnal.ViewModels.Ai;

namespace Hymnal.ViewModels;

/// <summary>
/// Shell view-model for RESEARCH mode. Composes the shared supplemental docs
/// sidebar and editor with an AI chat panel on the right.
/// </summary>
public sealed class ResearchViewModel : ViewModelBase
{
    public WorkspaceViewModel WorkspaceViewModel { get; }
    public SupplementalDocsViewModel SupplementalDocsViewModel { get; }
    public EditorViewModel EditorViewModel { get; }
    public AiChatViewModel AiChatViewModel { get; }

    private readonly ObservableAsPropertyHelper<bool> _hasWorkspace;

    public bool HasWorkspace => _hasWorkspace.Value;

    public ResearchViewModel(
        WorkspaceViewModel workspaceViewModel,
        SupplementalDocsViewModel supplementalDocsViewModel,
        EditorViewModel editorViewModel,
        AiChatViewModel aiChatViewModel)
    {
        WorkspaceViewModel = workspaceViewModel;
        SupplementalDocsViewModel = supplementalDocsViewModel;
        EditorViewModel = editorViewModel;
        AiChatViewModel = aiChatViewModel;

        _hasWorkspace = workspaceViewModel
            .WhenAnyValue(x => x.HasWorkspace)
            .ToProperty(this, x => x.HasWorkspace);
        Disposables.Add(_hasWorkspace);
    }
}

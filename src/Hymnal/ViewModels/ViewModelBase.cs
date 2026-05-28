using System.Reactive.Disposables;
using ReactiveUI;

namespace Hymnal.ViewModels;

public abstract class ViewModelBase : ReactiveObject, IActivatableViewModel
{
    /// <summary>
    /// Use this to subscribe to ReactiveCommands' ThrownExceptions:
    /// Command.ThrownExceptions.Subscribe(ex => ...).DisposeWith(Disposables);
    /// </summary>
    protected CompositeDisposable Disposables { get; } = new();

    public ViewModelActivator Activator { get; } = new();
}

using ReactiveUI.Builder;

namespace Hymnal.Core.Tests.Infrastructure;

public static class ReactiveUiTestBootstrap
{
    private static readonly object Gate = new();
    private static bool _initialized;

    public static void EnsureInitialized()
    {
        if (_initialized)
            return;

        lock (Gate)
        {
            if (_initialized)
                return;

            RxAppBuilder.CreateReactiveUIBuilder()
                .WithCoreServices()
                .BuildApp();

            _initialized = true;
        }
    }
}

using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Hymnal.Core.Interfaces;

namespace Hymnal.Infrastructure;

public class FolderPickerService : IFolderPickerService
{
    private readonly Func<TopLevel?> _topLevelAccessor;

    public FolderPickerService(Func<TopLevel?> topLevelAccessor)
    {
        _topLevelAccessor = topLevelAccessor;
    }

    public async Task<string?> PickFolderAsync()
    {
        var topLevel = _topLevelAccessor();
        if (topLevel == null)
            return null;

        var results = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { AllowMultiple = false });

        return results.Count > 0 ? results[0].Path.LocalPath : null;
    }
}

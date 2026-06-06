using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Hymnal.Core.Interfaces;

namespace Hymnal.Infrastructure;

public class FilePickerService : IFilePickerService
{
    private readonly Func<TopLevel?> _topLevelAccessor;

    public FilePickerService(Func<TopLevel?> topLevelAccessor)
    {
        _topLevelAccessor = topLevelAccessor;
    }

    public async Task<string?> PickFileAsync()
    {
        var topLevel = _topLevelAccessor();
        if (topLevel == null)
            return null;

        var results = await topLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions { AllowMultiple = false });

        return results.Count > 0 ? results[0].Path.LocalPath : null;
    }
}

namespace Hymnal.Core.Interfaces;

public interface IFolderPickerService
{
    Task<string?> PickFolderAsync();
}

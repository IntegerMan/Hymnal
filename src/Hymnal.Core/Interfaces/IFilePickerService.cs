namespace Hymnal.Core.Interfaces;

/// <summary>
/// Opens a native file picker dialog and returns the selected file path.
/// </summary>
public interface IFilePickerService
{
    Task<string?> PickFileAsync(string? suggestedStartDirectory = null);
}

namespace Hymnal.Core.Interfaces;

public interface INotificationService
{
    void ShowError(string message);
    void ShowInfo(string message);
    void ShowSuccess(string message);
}

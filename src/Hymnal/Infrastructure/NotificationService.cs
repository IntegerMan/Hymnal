using System;
using System.Reactive.Subjects;
using Hymnal.Core.Interfaces;

namespace Hymnal.Infrastructure;

public enum NotificationKind { Error, Info, Success }

public record Notification(NotificationKind Kind, string Message);

public class NotificationService : INotificationService, IDisposable
{
    private readonly Subject<Notification> _subject = new();

    public IObservable<Notification> Notifications => _subject;

    public void ShowError(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[ERROR] {message}");
        _subject.OnNext(new Notification(NotificationKind.Error, message));
    }

    public void ShowInfo(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[INFO] {message}");
        _subject.OnNext(new Notification(NotificationKind.Info, message));
    }

    public void ShowSuccess(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[SUCCESS] {message}");
        _subject.OnNext(new Notification(NotificationKind.Success, message));
    }

    public void Dispose() => _subject.Dispose();
}

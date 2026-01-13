using System.Collections.ObjectModel;
using MojsAjsli.Patterns.Observer;

namespace MojsAjsli.ViewModels;

/// <summary>
/// ViewModel odpowiedzialny za powiadomienia (SRP)
/// </summary>
public class NotificationViewModel : BaseViewModel, Patterns.Observer.IObserver<OrderNotification>
{
    private readonly ObservableCollection<string> _notifications = new();
    private const int MaxNotifications = 100;

    public ObservableCollection<string> Notifications => _notifications;

    public void AddNotification(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        _notifications.Insert(0, $"[{timestamp}] {message}");

        while (_notifications.Count > MaxNotifications)
            _notifications.RemoveAt(_notifications.Count - 1);
    }

    public void Update(OrderNotification data)
    {
        AddNotification($"[{data.Status}] {data.Message}");
    }
}

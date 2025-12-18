namespace MojsAjsli.Patterns.Observer;

public class OrderNotification
{
    public string Message { get; set; } = "";
    public int OrderId { get; set; }
    public int TableNumber { get; set; }
    public string Status { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class RestaurantNotificationSubject : ISubject<OrderNotification>
{
    private readonly List<IObserver<OrderNotification>> _observers = new();

    public void Attach(IObserver<OrderNotification> observer)
    {
        if (!_observers.Contains(observer))
            _observers.Add(observer);
    }

    public void Detach(IObserver<OrderNotification> observer)
    {
        _observers.Remove(observer);
    }

    public void Notify(OrderNotification data)
    {
        foreach (var observer in _observers.ToList())
            observer.Update(data);
    }

    public void NotifyNewOrder(int orderId, int tableNumber)
    {
        Notify(new OrderNotification
        {
            Message = "Nowe zamowienie #" + orderId,
            OrderId = orderId,
            TableNumber = tableNumber,
            Status = "New"
        });
    }

    public void NotifyOrderReady(int orderId, int tableNumber)
    {
        Notify(new OrderNotification
        {
            Message = "Zamowienie #" + orderId + " gotowe!",
            OrderId = orderId,
            TableNumber = tableNumber,
            Status = "Ready"
        });
    }

    public void NotifyOrderDelivered(int orderId, int tableNumber)
    {
        Notify(new OrderNotification
        {
            Message = "Zamowienie #" + orderId + " dostarczone",
            OrderId = orderId,
            TableNumber = tableNumber,
            Status = "Delivered"
        });
    }
}


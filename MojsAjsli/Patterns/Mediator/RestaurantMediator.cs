using MojsAjsli.Patterns.State;

namespace MojsAjsli.Patterns.Mediator;

public class RestaurantMediator : IRestaurantMediator
{
    private readonly List<IColleague> _colleagues = new();
    
    public event EventHandler<string>? OnNotification;
    public event EventHandler<Order>? OnOrderSentToKitchen;
    public event EventHandler<Order>? OnOrderReady;
    public event EventHandler<Order>? OnOrderDelivered;
    public event EventHandler<(int TableNumber, decimal Amount)>? OnPaymentComplete;

    public void Register(IColleague colleague)
    {
        if (!_colleagues.Contains(colleague))
        {
            _colleagues.Add(colleague);
            colleague.SetMediator(this);
        }
    }

    public void SendOrderToKitchen(Order order)
    {
        order.Accept();
        OnOrderSentToKitchen?.Invoke(this, order);
        var kitchen = _colleagues.FirstOrDefault(c => c.Name == "Kitchen");
        kitchen?.ReceiveNotification("NewOrder", order);
        Broadcast("Zamowienie #" + order.Id + " wyslane do kuchni (Stolik " + order.TableNumber + ")", order);
    }

    public void NotifyOrderReady(Order order)
    {
        order.MarkReady();
        OnOrderReady?.Invoke(this, order);
        var waiters = _colleagues.Where(c => c.Name.StartsWith("Waiter"));
        foreach (var waiter in waiters)
            waiter.ReceiveNotification("OrderReady", order);
        Broadcast("Zamowienie #" + order.Id + " gotowe do wydania (Stolik " + order.TableNumber + ")", order);
    }

    public void NotifyOrderDelivered(Order order)
    {
        order.Deliver();
        OnOrderDelivered?.Invoke(this, order);
        Broadcast("Zamowienie #" + order.Id + " dostarczone do stolika " + order.TableNumber, order);
    }

    public void RequestBill(int tableNumber)
    {
        var cashier = _colleagues.FirstOrDefault(c => c.Name == "Cashier");
        cashier?.ReceiveNotification("BillRequest", tableNumber);
        Broadcast("Prosba o rachunek dla stolika " + tableNumber);
    }

    public void SendMessage(string message, IColleague sender)
    {
        Broadcast(message, sender);
    }

    public void NotifyOrderPlaced(int orderId, int tableNumber)
    {
        Broadcast($"Nowe zamowienie #{orderId} dla stolika {tableNumber}");
    }

    public void NotifyOrderReady(int orderId)
    {
        Broadcast($"Zamowienie #{orderId} gotowe!");
    }

    public void NotifyPaymentProcessed(int tableNumber, decimal amount)
    {
        Broadcast($"Platnosc {amount:N2} zl przetworzona dla stolika {tableNumber}");
    }

    public void NotifyPaymentComplete(int tableNumber, decimal amount)
    {
        OnPaymentComplete?.Invoke(this, (tableNumber, amount));
        Broadcast("Platnosc " + amount.ToString("N2") + " zl zrealizowana dla stolika " + tableNumber);
    }

    public void Broadcast(string message, object? data = null)
    {
        OnNotification?.Invoke(this, message);
        foreach (var colleague in _colleagues)
            colleague.ReceiveNotification(message, data);
    }
}

using MojsAjsli.Patterns.Decorator;
using MojsAjsli.Patterns.Mediator;
using MojsAjsli.Patterns.Memento;
using MojsAjsli.Patterns.State;
using System.Collections.ObjectModel;

namespace MojsAjsli.Services;

public class WaiterService : IColleague
{
    private IRestaurantMediator? _mediator;
    private readonly string _waiterName;
    private readonly ObservableCollection<Order> _activeOrders = new();
    private readonly ObservableCollection<Order> _readyOrders = new();
    private readonly OrderHistory _orderHistory = new();

    public string Name => "Waiter_" + _waiterName;
    public string WaiterName => _waiterName;
    public ObservableCollection<Order> ActiveOrders => _activeOrders;
    public ObservableCollection<Order> ReadyOrders => _readyOrders;

    public event EventHandler<Order>? OnOrderReady;
    public event EventHandler<string>? OnNotification;

    public WaiterService(string waiterName) => _waiterName = waiterName;

    public void SetMediator(IRestaurantMediator mediator) => _mediator = mediator;

    public void ReceiveNotification(string message, object? data = null)
    {
        if (message == "OrderReady" && data is Order order)
        {
            if (_activeOrders.Any(o => o.Id == order.Id))
            {
                _readyOrders.Add(order);
                OnOrderReady?.Invoke(this, order);
            }
        }
        OnNotification?.Invoke(this, message);
    }

    public Order CreateOrder(int tableNumber)
    {
        var order = new Order(tableNumber);
        _activeOrders.Add(order);
        _orderHistory.SaveState(order);
        return order;
    }

    public void AddItemToOrder(Order order, IDish dish)
    {
        order.AddItem(dish);
        _orderHistory.SaveState(order); //  stan PO dodaniu
    }

    public void RemoveItemFromOrder(Order order, IDish dish)
    {
        order.RemoveItem(dish);
        _orderHistory.SaveState(order); //  stan PO usunięciu
    }

    public void UndoLastAction(Order order)
    {
        if (_orderHistory.CanUndo(order.Id))
            _orderHistory.Undo(order);
    }

    public void RedoAction(Order order)
    {
        if (_orderHistory.CanRedo(order.Id))
            _orderHistory.Redo(order);
    }

    public bool CanUndo(Order order) => _orderHistory.CanUndo(order.Id);
    public bool CanRedo(Order order) => _orderHistory.CanRedo(order.Id);

    public void SubmitOrder(Order order)
    {
        if (order.Items.Count == 0)
            throw new InvalidOperationException("Nie mozna wyslac pustego zamowienia.");
        _mediator?.SendOrderToKitchen(order);
    }

    public void DeliverOrder(Order order)
    {
        if (_readyOrders.Contains(order))
        {
            _readyOrders.Remove(order);
            _mediator?.NotifyOrderDelivered(order);
        }
    }

    public void RequestBill(int tableNumber) => _mediator?.RequestBill(tableNumber);

    public void CompleteOrder(Order order)
    {
        _activeOrders.Remove(order);
        _orderHistory.ClearHistory(order.Id);
    }
}

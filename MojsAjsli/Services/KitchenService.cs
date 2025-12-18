using MojsAjsli.Patterns.Mediator;
using MojsAjsli.Patterns.State;
using System.Collections.ObjectModel;

namespace MojsAjsli.Services;

public class KitchenService : IColleague
{
    private IRestaurantMediator? _mediator;
    private readonly ObservableCollection<Order> _orderQueue = new();
    private readonly ObservableCollection<Order> _preparingOrders = new();

    public string Name => "Kitchen";
    public ObservableCollection<Order> OrderQueue => _orderQueue;
    public ObservableCollection<Order> PreparingOrders => _preparingOrders;

    public event EventHandler<Order>? OnOrderReceived;
    public event EventHandler<Order>? OnOrderStartedPreparing;
    public event EventHandler<Order>? OnOrderCompleted;

    public void SetMediator(IRestaurantMediator mediator) => _mediator = mediator;

    public void ReceiveNotification(string message, object? data = null)
    {
        if (message == "NewOrder" && data is Order order)
        {
            _orderQueue.Add(order);
            OnOrderReceived?.Invoke(this, order);
        }
    }

    public void StartPreparing(Order order)
    {
        if (_orderQueue.Contains(order))
        {
            _orderQueue.Remove(order);
            order.StartPreparing();
            _preparingOrders.Add(order);
            OnOrderStartedPreparing?.Invoke(this, order);
        }
    }

    public void CompleteOrder(Order order)
    {
        if (_preparingOrders.Contains(order))
        {
            _preparingOrders.Remove(order);
            _mediator?.NotifyOrderReady(order);
            OnOrderCompleted?.Invoke(this, order);
        }
    }

    public int GetQueueLength() => _orderQueue.Count;
    public int GetPreparingCount() => _preparingOrders.Count;

    public TimeSpan EstimateWaitTime()
    {
        var totalMinutes = _orderQueue.Sum(o => o.EstimatedTime) + 
                          _preparingOrders.Sum(o => o.EstimatedTime / 2);
        return TimeSpan.FromMinutes(totalMinutes);
    }
}

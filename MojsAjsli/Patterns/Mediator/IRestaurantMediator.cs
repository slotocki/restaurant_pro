using MojsAjsli.Patterns.State;

namespace MojsAjsli.Patterns.Mediator;

public interface IRestaurantMediator
{
    void SendMessage(string message, IColleague sender);
    void NotifyOrderPlaced(int orderId, int tableNumber);
    void NotifyOrderReady(int orderId);
    void NotifyPaymentProcessed(int tableNumber, decimal amount);
    void NotifyPaymentComplete(int tableNumber, decimal amount);
    void SendOrderToKitchen(Order order);
    void NotifyOrderReady(Order order);
    void NotifyOrderDelivered(Order order);
    void RequestBill(int tableNumber);
}

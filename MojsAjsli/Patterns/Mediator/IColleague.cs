namespace MojsAjsli.Patterns.Mediator;

public interface IColleague
{
    string Name { get; }
    void SetMediator(IRestaurantMediator mediator);
    void ReceiveNotification(string message, object? data = null);
}


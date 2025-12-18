using MojsAjsli.Patterns.Decorator;
using MojsAjsli.Patterns.State;

namespace MojsAjsli.Patterns.Memento;

public class OrderMemento
{
    public int OrderId { get; }
    public int TableNumber { get; }
    public List<IDish> Items { get; }
    public IOrderState State { get; }
    public DateTime CreatedAt { get; }

    public OrderMemento(int orderId, int tableNumber, List<IDish> items, IOrderState state)
    {
        OrderId = orderId;
        TableNumber = tableNumber;
        Items = new List<IDish>(items);
        State = state;
        CreatedAt = DateTime.Now;
    }
}


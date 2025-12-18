using MojsAjsli.Patterns.State;

namespace MojsAjsli.Patterns.Memento;

public class OrderHistory
{
    private readonly Dictionary<int, Stack<OrderMemento>> _undoStacks = new();
    private readonly Dictionary<int, Stack<OrderMemento>> _redoStacks = new();

    public void SaveState(Order order)
    {
        if (!_undoStacks.ContainsKey(order.Id))
        {
            _undoStacks[order.Id] = new Stack<OrderMemento>();
            _redoStacks[order.Id] = new Stack<OrderMemento>();
        }

        _undoStacks[order.Id].Push(order.CreateMemento());
        _redoStacks[order.Id].Clear();
    }

    public bool CanUndo(int orderId)
    {
        return _undoStacks.ContainsKey(orderId) && _undoStacks[orderId].Count > 1;
    }

    public bool CanRedo(int orderId)
    {
        return _redoStacks.ContainsKey(orderId) && _redoStacks[orderId].Count > 0;
    }

    public void Undo(Order order)
    {
        if (!CanUndo(order.Id))
            throw new InvalidOperationException("Nie mozna cofnac - brak historii.");

        var currentState = _undoStacks[order.Id].Pop();
        _redoStacks[order.Id].Push(currentState);

        var previousState = _undoStacks[order.Id].Peek();
        order.RestoreFromMemento(previousState);
    }

    public void Redo(Order order)
    {
        if (!CanRedo(order.Id))
            throw new InvalidOperationException("Nie mozna powtorzyc - brak historii.");

        var nextState = _redoStacks[order.Id].Pop();
        _undoStacks[order.Id].Push(nextState);
        order.RestoreFromMemento(nextState);
    }

    public void ClearHistory(int orderId)
    {
        if (_undoStacks.ContainsKey(orderId))
            _undoStacks[orderId].Clear();
        if (_redoStacks.ContainsKey(orderId))
            _redoStacks[orderId].Clear();
    }
}

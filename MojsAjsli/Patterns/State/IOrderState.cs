namespace MojsAjsli.Patterns.State;

public interface IOrderState
{
    string StateName { get; }
    void Accept(Order order);
    void StartPreparing(Order order);
    void MarkReady(Order order);
    void Deliver(Order order);
    void Pay(Order order);
    void Cancel(Order order);
    bool CanModify { get; }
    bool CanCancel { get; }
}


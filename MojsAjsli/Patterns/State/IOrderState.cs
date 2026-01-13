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
    void Return(Order order); // Nowa metoda do zwrotu zamówienia
    bool CanModify { get; }
    bool CanCancel { get; }
}

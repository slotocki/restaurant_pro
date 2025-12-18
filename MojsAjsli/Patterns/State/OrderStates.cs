namespace MojsAjsli.Patterns.State;

public class NewOrderState : IOrderState
{
    public string StateName => "Nowe";
    public bool CanModify => true;
    public bool CanCancel => true;

    public void Accept(Order order)
    {
        if (order.Items.Count == 0)
            throw new InvalidOperationException("Nie mozna przyjac pustego zamowienia.");
        order.State = new AcceptedState();
    }

    public void StartPreparing(Order order) => throw new InvalidOperationException("Zamowienie musi byc najpierw przyjete.");
    public void MarkReady(Order order) => throw new InvalidOperationException("Zamowienie musi byc najpierw przyjete i przygotowane.");
    public void Deliver(Order order) => throw new InvalidOperationException("Zamowienie musi byc najpierw gotowe.");
    public void Pay(Order order) => throw new InvalidOperationException("Zamowienie musi byc najpierw dostarczone.");
    public void Cancel(Order order) => order.State = new CancelledState();
}

public class AcceptedState : IOrderState
{
    public string StateName => "Przyjete";
    public bool CanModify => false;
    public bool CanCancel => true;

    public void Accept(Order order) => throw new InvalidOperationException("Zamowienie jest juz przyjete.");
    public void StartPreparing(Order order) => order.State = new PreparingState();
    public void MarkReady(Order order) => throw new InvalidOperationException("Zamowienie musi byc najpierw w przygotowaniu.");
    public void Deliver(Order order) => throw new InvalidOperationException("Zamowienie musi byc najpierw gotowe.");
    public void Pay(Order order) => throw new InvalidOperationException("Zamowienie musi byc najpierw dostarczone.");
    public void Cancel(Order order) => order.State = new CancelledState();
}

public class PreparingState : IOrderState
{
    public string StateName => "W przygotowaniu";
    public bool CanModify => false;
    public bool CanCancel => false;

    public void Accept(Order order) => throw new InvalidOperationException("Zamowienie jest juz w przygotowaniu.");
    public void StartPreparing(Order order) => throw new InvalidOperationException("Zamowienie jest juz w przygotowaniu.");
    public void MarkReady(Order order) => order.State = new ReadyState();
    public void Deliver(Order order) => throw new InvalidOperationException("Zamowienie musi byc najpierw gotowe.");
    public void Pay(Order order) => throw new InvalidOperationException("Zamowienie musi byc najpierw dostarczone.");
    public void Cancel(Order order) => throw new InvalidOperationException("Nie mozna anulowac zamowienia w trakcie przygotowywania.");
}

public class ReadyState : IOrderState
{
    public string StateName => "Gotowe";
    public bool CanModify => false;
    public bool CanCancel => false;

    public void Accept(Order order) => throw new InvalidOperationException("Zamowienie jest juz gotowe.");
    public void StartPreparing(Order order) => throw new InvalidOperationException("Zamowienie jest juz gotowe.");
    public void MarkReady(Order order) => throw new InvalidOperationException("Zamowienie jest juz gotowe.");
    public void Deliver(Order order) => order.State = new DeliveredState();
    public void Pay(Order order) => throw new InvalidOperationException("Zamowienie musi byc najpierw dostarczone.");
    public void Cancel(Order order) => throw new InvalidOperationException("Nie mozna anulowac gotowego zamowienia.");
}

public class DeliveredState : IOrderState
{
    public string StateName => "Dostarczone";
    public bool CanModify => false;
    public bool CanCancel => false;

    public void Accept(Order order) => throw new InvalidOperationException("Zamowienie jest juz dostarczone.");
    public void StartPreparing(Order order) => throw new InvalidOperationException("Zamowienie jest juz dostarczone.");
    public void MarkReady(Order order) => throw new InvalidOperationException("Zamowienie jest juz dostarczone.");
    public void Deliver(Order order) => throw new InvalidOperationException("Zamowienie jest juz dostarczone.");
    public void Pay(Order order)
    {
        order.State = new PaidState();
        order.CompletedAt = DateTime.Now;
    }
    public void Cancel(Order order) => throw new InvalidOperationException("Nie mozna anulowac dostarczonego zamowienia.");
}

public class PaidState : IOrderState
{
    public string StateName => "Oplacone";
    public bool CanModify => false;
    public bool CanCancel => false;

    public void Accept(Order order) => throw new InvalidOperationException("Zamowienie jest juz oplacone.");
    public void StartPreparing(Order order) => throw new InvalidOperationException("Zamowienie jest juz oplacone.");
    public void MarkReady(Order order) => throw new InvalidOperationException("Zamowienie jest juz oplacone.");
    public void Deliver(Order order) => throw new InvalidOperationException("Zamowienie jest juz oplacone.");
    public void Pay(Order order) => throw new InvalidOperationException("Zamowienie jest juz oplacone.");
    public void Cancel(Order order) => throw new InvalidOperationException("Nie mozna anulowac oplaconego zamowienia.");
}

public class CancelledState : IOrderState
{
    public string StateName => "Anulowane";
    public bool CanModify => false;
    public bool CanCancel => false;

    public void Accept(Order order) => throw new InvalidOperationException("Zamowienie zostalo anulowane.");
    public void StartPreparing(Order order) => throw new InvalidOperationException("Zamowienie zostalo anulowane.");
    public void MarkReady(Order order) => throw new InvalidOperationException("Zamowienie zostalo anulowane.");
    public void Deliver(Order order) => throw new InvalidOperationException("Zamowienie zostalo anulowane.");
    public void Pay(Order order) => throw new InvalidOperationException("Zamowienie zostalo anulowane.");
    public void Cancel(Order order) => throw new InvalidOperationException("Zamowienie jest juz anulowane.");
}


namespace MojsAjsli.Payments;

public class BankPayment : IPaymentMethod
{
    private string _lastTransactionRef = "";
    private readonly string _accountNumber;
    public string ApiKey { get; set; } = "DEMO-API-KEY";

    public string Name => "Przelew bankowy";

    public BankPayment(string accountNumber)
    {
        _accountNumber = accountNumber;
    }

    public bool ProcessPayment(decimal amount)
    {
        // Symulacja przelewu bankowego
        Thread.Sleep(100);
        
        _lastTransactionRef = "BANK-" + Guid.NewGuid().ToString();
        return true;
    }

    public bool Refund(decimal amount)
    {
        if (string.IsNullOrEmpty(_lastTransactionRef))
            return false;

        // Symulacja zwrotu
        Thread.Sleep(100);
        return true;
    }

    public string GetTransactionId() => _lastTransactionRef;
}

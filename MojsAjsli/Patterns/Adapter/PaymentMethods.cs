namespace MojsAjsli.Patterns.Adapter;

public class CashPayment : IPaymentMethod
{
    private string _transactionId = "";
    
    public string Name => "Gotowka";

    public bool ProcessPayment(decimal amount)
    {
        _transactionId = "CASH-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString().Substring(0, 8);
        return true;
    }

    public bool Refund(decimal amount) => true;
    public string GetTransactionId() => _transactionId;
}

public class CardPayment : IPaymentMethod
{
    private string _transactionId = "";
    
    public string Name => "Karta";

    public bool ProcessPayment(decimal amount)
    {
        _transactionId = "CARD-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString().Substring(0, 8);
        return true;
    }

    public bool Refund(decimal amount) => true;
    public string GetTransactionId() => _transactionId;
}

public class BlikPayment : IPaymentMethod
{
    private string _transactionId = "";
    public string BlikCode { get; set; } = "";
    
    public string Name => "BLIK";

    public bool ProcessPayment(decimal amount)
    {
        if (string.IsNullOrEmpty(BlikCode) || BlikCode.Length != 6)
            return false;

        _transactionId = "BLIK-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString().Substring(0, 8);
        return true;
    }

    public bool Refund(decimal amount) => true;
    public string GetTransactionId() => _transactionId;
}


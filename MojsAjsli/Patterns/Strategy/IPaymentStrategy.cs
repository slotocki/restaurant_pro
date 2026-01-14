namespace MojsAjsli.Patterns.Strategy;

//strategy
public interface IPaymentStrategy
{
    string Name { get; }
    PaymentMethodType Type { get; }
    bool ProcessPayment(decimal amount);
    bool Refund(decimal amount);
    string GetTransactionId();
}

public enum PaymentMethodType
{
    Cash,
    Card,
    Blik,
    BankTransfer
}


public abstract class PaymentStrategyBase : IPaymentStrategy
{
    protected string TransactionId = "";
    
    public abstract string Name { get; }
    public abstract PaymentMethodType Type { get; }
    
    protected string GenerateTransactionId(string prefix)
    {
        return $"{prefix}-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8]}";
    }
    
    public abstract bool ProcessPayment(decimal amount);
    public virtual bool Refund(decimal amount) => !string.IsNullOrEmpty(TransactionId);
    public string GetTransactionId() => TransactionId;
}


public class CashPaymentStrategy : PaymentStrategyBase
{
    public override string Name => "Gotówka";
    public override PaymentMethodType Type => PaymentMethodType.Cash;

    public override bool ProcessPayment(decimal amount)
    {
        if (amount <= 0) return false;
        TransactionId = GenerateTransactionId("CASH");
        return true;
    }
}


public class CardPaymentStrategy : PaymentStrategyBase
{
    public override string Name => "Karta";
    public override PaymentMethodType Type => PaymentMethodType.Card;

    public override bool ProcessPayment(decimal amount)
    {
        if (amount <= 0) return false;
        TransactionId = GenerateTransactionId("CARD");
        return true;
    }
}

/// <summary>
/// Strategia płatności BLIK.
/// </summary>
public class BlikPaymentStrategy : PaymentStrategyBase
{
    public string BlikCode { get; set; } = "";
    
    public override string Name => "BLIK";
    public override PaymentMethodType Type => PaymentMethodType.Blik;

    public override bool ProcessPayment(decimal amount)
    {
        if (amount <= 0) return false;
        if (string.IsNullOrEmpty(BlikCode) || BlikCode.Length != 6) return false;
        
        TransactionId = GenerateTransactionId("BLIK");
        return true;
    }
}

/// <summary>
/// Strategia płatności przelewem bankowym.
/// </summary>
public class BankTransferPaymentStrategy : PaymentStrategyBase
{
    public string AccountNumber { get; }
    
    public override string Name => "Przelew bankowy";
    public override PaymentMethodType Type => PaymentMethodType.BankTransfer;

    public BankTransferPaymentStrategy(string accountNumber = "PL00000000000000000000000000")
    {
        AccountNumber = accountNumber;
    }

    public override bool ProcessPayment(decimal amount)
    {
        if (amount <= 0) return false;
        TransactionId = GenerateTransactionId("BANK");
        return true;
    }
}


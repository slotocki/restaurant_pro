namespace MojsAjsli.Patterns.Adapter;

public class ExternalBankAPI
{
    public string ApiKey { get; set; } = "DEMO-API-KEY";
    
    public BankTransactionResult MakeTransaction(decimal amount, string currency, string accountNumber)
    {
        Thread.Sleep(100);
        
        return new BankTransactionResult
        {
            Success = true,
            TransactionReference = "BANK-" + Guid.NewGuid().ToString(),
            ProcessedAmount = amount,
            Currency = currency,
            Timestamp = DateTime.Now
        };
    }

    public BankTransactionResult RefundTransaction(string transactionReference, decimal amount)
    {
        Thread.Sleep(100);
        
        return new BankTransactionResult
        {
            Success = true,
            TransactionReference = "REFUND-" + transactionReference,
            ProcessedAmount = amount,
            Currency = "PLN",
            Timestamp = DateTime.Now
        };
    }
}

public class BankTransactionResult
{
    public bool Success { get; set; }
    public string TransactionReference { get; set; } = "";
    public decimal ProcessedAmount { get; set; }
    public string Currency { get; set; } = "PLN";
    public DateTime Timestamp { get; set; }
    public string? ErrorMessage { get; set; }
}


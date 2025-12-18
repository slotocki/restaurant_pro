namespace MojsAjsli.Models;

public enum PaymentType
{
    Cash,
    Card,
    BankTransfer,
    MobilePayment,
    Blik
}

public class Payment
{
    public int Id { get; set; }
    public int TableNumber { get; set; }
    public decimal Amount { get; set; }
    public PaymentType Type { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsProcessed { get; set; }
    public bool IsSuccessful { get; set; }
    public string? TransactionId { get; set; }

    public Payment(int id, int tableNumber, decimal amount, PaymentType type)
    {
        Id = id;
        TableNumber = tableNumber;
        Amount = amount;
        Type = type;
        Timestamp = DateTime.Now;
        IsProcessed = false;
        IsSuccessful = false;
    }

    public Payment(decimal amount, PaymentType type)
    {
        Amount = amount;
        Type = type;
        Timestamp = DateTime.Now;
        IsProcessed = false;
        IsSuccessful = false;
    }
}

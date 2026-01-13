using MojsAjsli.Patterns.Strategy;

namespace MojsAjsli.Models;

public class Payment
{
    public int Id { get; set; }
    public int TableNumber { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethodType Type { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsProcessed { get; set; }
    public bool IsSuccessful { get; set; }
    public string? TransactionId { get; set; }

    public Payment(int id, int tableNumber, decimal amount, PaymentMethodType type)
    {
        Id = id;
        TableNumber = tableNumber;
        Amount = amount;
        Type = type;
        Timestamp = DateTime.Now;
        IsProcessed = false;
        IsSuccessful = false;
    }

    public Payment(decimal amount, PaymentMethodType type)
    {
        Amount = amount;
        Type = type;
        Timestamp = DateTime.Now;
        IsProcessed = false;
        IsSuccessful = false;
    }
}

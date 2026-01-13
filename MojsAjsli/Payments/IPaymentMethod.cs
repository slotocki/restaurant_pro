namespace MojsAjsli.Payments;

public interface IPaymentMethod
{
    string Name { get; }
    bool ProcessPayment(decimal amount);
    bool Refund(decimal amount);
    string GetTransactionId();
}

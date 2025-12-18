namespace MojsAjsli.Patterns.Adapter;

public class BankPaymentAdapter : IPaymentMethod
{
    private readonly ExternalBankAPI _bankApi;
    private string _lastTransactionRef = "";
    private readonly string _accountNumber;

    public string Name => "Przelew bankowy";

    public BankPaymentAdapter(string accountNumber)
    {
        _bankApi = new ExternalBankAPI();
        _accountNumber = accountNumber;
    }

    public BankPaymentAdapter(ExternalBankAPI bankApi, string accountNumber)
    {
        _bankApi = bankApi;
        _accountNumber = accountNumber;
    }

    public bool ProcessPayment(decimal amount)
    {
        var result = _bankApi.MakeTransaction(amount, "PLN", _accountNumber);
        
        if (result.Success)
        {
            _lastTransactionRef = result.TransactionReference;
            return true;
        }
        
        return false;
    }

    public bool Refund(decimal amount)
    {
        if (string.IsNullOrEmpty(_lastTransactionRef))
            return false;

        var result = _bankApi.RefundTransaction(_lastTransactionRef, amount);
        return result.Success;
    }

    public string GetTransactionId() => _lastTransactionRef;
}

using MojsAjsli.Models;
using MojsAjsli.Patterns.Adapter;
using MojsAjsli.Patterns.Mediator;
using MojsAjsli.Patterns.State;
using MojsAjsli.Patterns.Strategy;

namespace MojsAjsli.Services;

public class CashierService : IColleague
{
    private IRestaurantMediator? _mediator;
    private readonly List<IPaymentMethod> _paymentMethods = new();
    private readonly List<IPricingStrategy> _pricingStrategies = new();
    private IPricingStrategy _currentStrategy;
    private readonly List<Payment> _transactionHistory = new();

    public string Name => "Cashier";
    public IReadOnlyList<Payment> TransactionHistory => _transactionHistory;
    public IPricingStrategy CurrentStrategy => _currentStrategy;

    public event EventHandler<(int TableNumber, decimal Amount)>? OnBillRequested;
    public event EventHandler<Payment>? OnPaymentProcessed;

    public CashierService()
    {
        _paymentMethods.Add(new CashPayment());
        _paymentMethods.Add(new CardPayment());
        _paymentMethods.Add(new BlikPayment());
        _paymentMethods.Add(new BankPaymentAdapter("PL12345678901234567890123456"));

        _pricingStrategies.Add(new RegularPricingStrategy());
        _pricingStrategies.Add(new HappyHourStrategy());
        _pricingStrategies.Add(new LoyaltyCardStrategy());
        _pricingStrategies.Add(new GroupDiscountStrategy());
        _pricingStrategies.Add(new StudentDiscountStrategy());
        _pricingStrategies.Add(new WeekendStrategy());

        _currentStrategy = _pricingStrategies[0];
    }

    public void SetMediator(IRestaurantMediator mediator) => _mediator = mediator;

    public void ReceiveNotification(string message, object? data = null)
    {
        if (message == "BillRequest" && data is int tableNumber)
            OnBillRequested?.Invoke(this, (tableNumber, 0));
    }

    public List<IPricingStrategy> GetAvailableStrategies() => new(_pricingStrategies);
    public List<IPaymentMethod> GetPaymentMethods() => new(_paymentMethods);
    public void SetPricingStrategy(IPricingStrategy strategy) => _currentStrategy = strategy;

    public IPricingStrategy GetBestApplicableStrategy(Order order, int groupSize = 1)
    {
        var now = DateTime.Now;
        var groupStrategy = _pricingStrategies.OfType<GroupDiscountStrategy>().FirstOrDefault();
        if (groupStrategy != null)
            groupStrategy.GroupSize = groupSize;

        var applicableStrategies = _pricingStrategies
            .Where(s => s.IsApplicable(order, now))
            .OrderByDescending(s => s.GetDiscountPercentage())
            .ToList();

        return applicableStrategies.FirstOrDefault() ?? new RegularPricingStrategy();
    }

    public decimal CalculateBill(Order order) => _currentStrategy.CalculatePrice(order);
    public decimal CalculateBillWithStrategy(Order order, IPricingStrategy strategy) => strategy.CalculatePrice(order);

    public Payment ProcessPayment(Order order, PaymentType paymentType, string? blikCode = null)
    {
        var amount = CalculateBill(order);
        var payment = new Payment(amount, paymentType);

        IPaymentMethod? method = paymentType switch
        {
            PaymentType.Cash => _paymentMethods.OfType<CashPayment>().FirstOrDefault(),
            PaymentType.Card => _paymentMethods.OfType<CardPayment>().FirstOrDefault(),
            PaymentType.Blik => GetBlikPayment(blikCode),
            PaymentType.BankTransfer => _paymentMethods.OfType<BankPaymentAdapter>().FirstOrDefault(),
            _ => null
        };

        if (method != null && method.ProcessPayment(amount))
        {
            payment.IsSuccessful = true;
            payment.TransactionId = method.GetTransactionId();
            order.Pay();
            _transactionHistory.Add(payment);
            _mediator?.NotifyPaymentComplete(order.TableNumber, amount);
            OnPaymentProcessed?.Invoke(this, payment);
        }

        return payment;
    }

    private IPaymentMethod? GetBlikPayment(string? blikCode)
    {
        var blik = _paymentMethods.OfType<BlikPayment>().FirstOrDefault();
        if (blik != null && !string.IsNullOrEmpty(blikCode))
            blik.BlikCode = blikCode;
        return blik;
    }

    public decimal GetDailyRevenue()
    {
        var today = DateTime.Today;
        return _transactionHistory
            .Where(p => p.IsSuccessful && p.Timestamp.Date == today)
            .Sum(p => p.Amount);
    }

    public int GetTodayTransactionCount()
    {
        var today = DateTime.Today;
        return _transactionHistory.Count(p => p.IsSuccessful && p.Timestamp.Date == today);
    }
}


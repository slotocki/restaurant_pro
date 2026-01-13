using MojsAjsli.Models;
using MojsAjsli.Patterns.Mediator;
using MojsAjsli.Patterns.State;
using MojsAjsli.Patterns.Strategy;

namespace MojsAjsli.Services;

public class CashierService : IColleague
{
    private IRestaurantMediator? _mediator;
    private readonly List<IPaymentStrategy> _paymentStrategies = new();
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
        _paymentStrategies.Add(new CashPaymentStrategy());
        _paymentStrategies.Add(new CardPaymentStrategy());
        _paymentStrategies.Add(new BlikPaymentStrategy());
        _paymentStrategies.Add(new BankTransferPaymentStrategy("PL12345678901234567890123456"));

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
    public List<IPaymentStrategy> GetPaymentStrategies() => new(_paymentStrategies);
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

    public Payment ProcessPayment(Order order, PaymentMethodType paymentType, string? blikCode = null)
    {
        var amount = CalculateBill(order);
        var payment = new Payment(amount, paymentType);

        IPaymentStrategy? strategy = _paymentStrategies.FirstOrDefault(s => s.Type == paymentType);

        if (strategy is BlikPaymentStrategy blikStrategy && !string.IsNullOrEmpty(blikCode))
        {
            blikStrategy.BlikCode = blikCode;
        }

        if (strategy != null && strategy.ProcessPayment(amount))
        {
            payment.IsSuccessful = true;
            payment.TransactionId = strategy.GetTransactionId();
            order.Pay();
            _transactionHistory.Add(payment);
            _mediator?.NotifyPaymentComplete(order.TableNumber, amount);
            OnPaymentProcessed?.Invoke(this, payment);
        }

        return payment;
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

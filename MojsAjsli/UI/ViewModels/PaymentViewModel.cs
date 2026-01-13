using System.Collections.ObjectModel;
using System.Windows.Input;
using MojsAjsli.Models;
using MojsAjsli.Patterns.State;
using MojsAjsli.Patterns.Strategy;
using MojsAjsli.Services;

namespace MojsAjsli.UI.ViewModels;

/// <summary>
/// ViewModel odpowiedzialny za płatności (SRP)
/// </summary>
public class PaymentViewModel : BaseViewModel
{
    private readonly CashierService _cashierService;
    private readonly WaiterService _waiterService;
    private readonly TableService _tableService;
    private readonly StatisticsService _statisticsService;
    private readonly Action<string> _addNotification;

    private Order? _selectedOrder;
    private IPricingStrategy? _selectedStrategy;
    private int _selectedPaymentMethodIndex;
    private string _blikCode = "";
    private string _paymentSummary = "Wybierz zamówienie";
    private string _paymentDiscount = "";

    public PaymentViewModel(
        CashierService cashierService,
        WaiterService waiterService,
        TableService tableService,
        StatisticsService statisticsService,
        Action<string> addNotification)
    {
        _cashierService = cashierService;
        _waiterService = waiterService;
        _tableService = tableService;
        _statisticsService = statisticsService;
        _addNotification = addNotification;

        DeliveredOrders = new ObservableCollection<Order>();
        AvailableStrategies = new ObservableCollection<IPricingStrategy>(_cashierService.GetAvailableStrategies());
        PaymentMethods = new ObservableCollection<string> { "Gotówka", "Karta", "BLIK", "Przelew bankowy" };

        ProcessPaymentCommand = new RelayCommand(ProcessPayment, () => SelectedOrder != null);

        if (AvailableStrategies.Count > 0)
            SelectedStrategy = AvailableStrategies[0];
    }

    public ObservableCollection<Order> DeliveredOrders { get; }
    public ObservableCollection<IPricingStrategy> AvailableStrategies { get; }
    public ObservableCollection<string> PaymentMethods { get; }

    public Order? SelectedOrder
    {
        get => _selectedOrder;
        set
        {
            if (SetProperty(ref _selectedOrder, value))
                UpdatePaymentSummary();
        }
    }

    public IPricingStrategy? SelectedStrategy
    {
        get => _selectedStrategy;
        set
        {
            if (SetProperty(ref _selectedStrategy, value) && value != null)
            {
                _cashierService.SetPricingStrategy(value);
                UpdatePaymentSummary();
                OnStrategyChanged?.Invoke();
            }
        }
    }

    public int SelectedPaymentMethodIndex
    {
        get => _selectedPaymentMethodIndex;
        set => SetProperty(ref _selectedPaymentMethodIndex, value);
    }

    public string BlikCode
    {
        get => _blikCode;
        set => SetProperty(ref _blikCode, value);
    }

    public string PaymentSummary
    {
        get => _paymentSummary;
        private set => SetProperty(ref _paymentSummary, value);
    }

    public string PaymentDiscount
    {
        get => _paymentDiscount;
        private set => SetProperty(ref _paymentDiscount, value);
    }

    public ICommand ProcessPaymentCommand { get; }

    public event Action? OnPaymentCompleted;
    public event Action? OnStrategyChanged;

    public void AddDeliveredOrder(Order order)
    {
        DeliveredOrders.Add(order);
    }

    private void UpdatePaymentSummary()
    {
        if (_selectedOrder != null)
        {
            var strategy = _cashierService.CurrentStrategy;
            var originalPrice = _selectedOrder.TotalPrice;
            var finalPrice = strategy.CalculatePrice(_selectedOrder);
            var discount = strategy.GetDiscountPercentage();

            PaymentSummary = $"Do zapłaty: {finalPrice:N2} zł";
            PaymentDiscount = discount > 0
                ? $"Zniżka {discount}% (było: {originalPrice:N2} zł)"
                : "";
        }
        else
        {
            PaymentSummary = "Wybierz zamówienie";
            PaymentDiscount = "";
        }
    }

    private void ProcessPayment()
    {
        if (_selectedOrder == null) return;

        var paymentType = _selectedPaymentMethodIndex switch
        {
            0 => PaymentMethodType.Cash,
            1 => PaymentMethodType.Card,
            2 => PaymentMethodType.Blik,
            3 => PaymentMethodType.BankTransfer,
            _ => PaymentMethodType.Cash
        };

        string? blikCode = null;
        if (paymentType == PaymentMethodType.Blik)
        {
            blikCode = BlikCode;
            if (string.IsNullOrEmpty(blikCode) || blikCode.Length != 6)
            {
                OnPaymentError?.Invoke("Podaj prawidłowy 6-cyfrowy kod BLIK!");
                return;
            }
        }

        try
        {
            var payment = _cashierService.ProcessPayment(_selectedOrder, paymentType, blikCode);

            if (payment.IsSuccessful)
            {
                var finalPrice = _cashierService.CurrentStrategy.CalculatePrice(_selectedOrder);
                _statisticsService.RecordCompletedOrder(_selectedOrder, finalPrice);
                
                var order = _selectedOrder;
                DeliveredOrders.Remove(order);
                _waiterService.CompleteOrder(order);
                _tableService.FreeTable(order.TableNumber);

                _addNotification($"Płatność {finalPrice:N2} zł - {payment.Type} (#{payment.TransactionId})");
                
                OnPaymentCompleted?.Invoke();
                OnPaymentSuccess?.Invoke(finalPrice, payment.Type.ToString(), payment.TransactionId ?? "BRAK");
            }
            else
            {
                OnPaymentError?.Invoke("Płatność nie powiodła się!");
            }
        }
        catch (Exception ex)
        {
            OnPaymentError?.Invoke(ex.Message);
        }
    }

    public event Action<string>? OnPaymentError;
    public event Action<decimal, string, string>? OnPaymentSuccess;
}

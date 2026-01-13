using System.Collections.ObjectModel;
using System.Windows.Input;
using MojsAjsli.Models;
using MojsAjsli.Patterns.Decorator;
using MojsAjsli.Patterns.Observer;
using MojsAjsli.Patterns.State;
using MojsAjsli.Patterns.Strategy;
using MojsAjsli.Services;
using MojsAjsli.Services.Interfaces.Dishes;

namespace MojsAjsli.UI.ViewModels;

/// <summary>
/// ViewModel odpowiedzialny za zarządzanie zamówieniami (SRP)
/// </summary>
public class OrderViewModel : BaseViewModel
{
    private readonly WaiterService _waiterService;
    private readonly MenuService _menuService;
    private readonly CashierService _cashierService;
    private readonly RestaurantNotificationSubject _notificationSubject;
    private readonly Action<string> _addNotification;

    private Order? _currentOrder;
    private MenuItem? _selectedMenuItem;
    private string _totalPrice = "0,00 zł";
    private string _discountText = "";
    private string _finalPriceText = "";
    private bool _canUndo;
    private bool _canRedo;

    // Extras
    private bool _extraCheese;
    private bool _bacon;
    private bool _spicySauce;
    private bool _glutenFree;
    private bool _extraPortion;

    public OrderViewModel(
        WaiterService waiterService,
        MenuService menuService,
        CashierService cashierService,
        RestaurantNotificationSubject notificationSubject,
        Action<string> addNotification)
    {
        _waiterService = waiterService;
        _menuService = menuService;
        _cashierService = cashierService;
        _notificationSubject = notificationSubject;
        _addNotification = addNotification;

        CurrentOrderItems = new ObservableCollection<OrderItemViewModel>();

        AddToOrderCommand = new RelayCommand(AddToOrder, () => _currentOrder != null && SelectedMenuItem != null);
        RemoveFromOrderCommand = new RelayCommand<OrderItemViewModel>(RemoveFromOrder, item => _currentOrder != null && item != null);
        SubmitOrderCommand = new RelayCommand(SubmitOrder, () => _currentOrder != null && _currentOrder.Items.Count > 0);
        UndoCommand = new RelayCommand(Undo, () => CanUndo);
        RedoCommand = new RelayCommand(Redo, () => CanRedo);
    }

    public ObservableCollection<OrderItemViewModel> CurrentOrderItems { get; }

    public Order? CurrentOrder
    {
        get => _currentOrder;
        private set => SetProperty(ref _currentOrder, value);
    }

    public MenuItem? SelectedMenuItem
    {
        get => _selectedMenuItem;
        set => SetProperty(ref _selectedMenuItem, value);
    }

    public string TotalPrice
    {
        get => _totalPrice;
        private set => SetProperty(ref _totalPrice, value);
    }

    public string DiscountText
    {
        get => _discountText;
        private set => SetProperty(ref _discountText, value);
    }

    public string FinalPriceText
    {
        get => _finalPriceText;
        private set => SetProperty(ref _finalPriceText, value);
    }

    public bool CanUndo
    {
        get => _canUndo;
        private set => SetProperty(ref _canUndo, value);
    }

    public bool CanRedo
    {
        get => _canRedo;
        private set => SetProperty(ref _canRedo, value);
    }

    public bool ExtraCheese { get => _extraCheese; set => SetProperty(ref _extraCheese, value); }
    public bool Bacon { get => _bacon; set => SetProperty(ref _bacon, value); }
    public bool SpicySauce { get => _spicySauce; set => SetProperty(ref _spicySauce, value); }
    public bool GlutenFree { get => _glutenFree; set => SetProperty(ref _glutenFree, value); }
    public bool ExtraPortion { get => _extraPortion; set => SetProperty(ref _extraPortion, value); }

    public ICommand AddToOrderCommand { get; }
    public ICommand RemoveFromOrderCommand { get; }
    public ICommand SubmitOrderCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }

    public event Action? OnOrderSubmitted;

    public void CreateOrderForTable(int tableNumber)
    {
        _currentOrder = _waiterService.CreateOrder(tableNumber);
        CurrentOrderItems.Clear();
        UpdateOrderUI();
    }

    public void ClearOrder()
    {
        _currentOrder = null;
        CurrentOrderItems.Clear();
        UpdateOrderUI();
    }

    private void AddToOrder()
    {
        if (_currentOrder == null || _selectedMenuItem == null) return;

        var extras = CollectExtras();
        IDish dish = _menuService.CreateDishWithExtras(_selectedMenuItem, extras);

        _waiterService.AddItemToOrder(_currentOrder, dish);
        CurrentOrderItems.Add(new OrderItemViewModel(dish));

        ClearExtras();
        UpdateOrderUI();
        _addNotification($"Dodano: {dish.GetDescription()} ({dish.GetPrice():N2} zł)");
    }

    private List<DishExtra> CollectExtras()
    {
        var extras = new List<DishExtra>();
        if (ExtraCheese) extras.Add(DishExtra.ExtraCheese);
        if (Bacon) extras.Add(DishExtra.Bacon);
        if (SpicySauce) extras.Add(DishExtra.SpicySauce);
        if (GlutenFree) extras.Add(DishExtra.GlutenFree);
        if (ExtraPortion) extras.Add(DishExtra.ExtraPortion);
        return extras;
    }

    private void ClearExtras()
    {
        ExtraCheese = false;
        Bacon = false;
        SpicySauce = false;
        GlutenFree = false;
        ExtraPortion = false;
    }

    private void RemoveFromOrder(OrderItemViewModel? item)
    {
        if (_currentOrder == null || item == null) return;

        var dish = _currentOrder.Items.FirstOrDefault(d => d.GetDescription() == item.Description);
        if (dish != null)
        {
            _waiterService.RemoveItemFromOrder(_currentOrder, dish);
            CurrentOrderItems.Remove(item);
            UpdateOrderUI();
            _addNotification($"Usunięto: {item.Name}");
        }
    }

    private void SubmitOrder()
    {
        if (_currentOrder == null || _currentOrder.Items.Count == 0) return;

        _waiterService.SubmitOrder(_currentOrder);
        _notificationSubject.NotifyNewOrder(_currentOrder.Id, _currentOrder.TableNumber);

        var tableNumber = _currentOrder.TableNumber;
        _currentOrder = null;
        CurrentOrderItems.Clear();
        UpdateOrderUI();

        OnOrderSubmitted?.Invoke();

        // Utwórz nowe zamówienie dla stolika jeśli wciąż zajęty
        CreateOrderForTable(tableNumber);
    }

    private void Undo()
    {
        if (_currentOrder == null) return;

        _waiterService.UndoLastAction(_currentOrder);
        RefreshOrderItems();
        UpdateOrderUI();
        _addNotification("Cofnięto ostatnią akcję (Memento)");
    }

    private void Redo()
    {
        if (_currentOrder == null) return;

        _waiterService.RedoAction(_currentOrder);
        RefreshOrderItems();
        UpdateOrderUI();
        _addNotification("Powtórzono akcję (Memento)");
    }

    private void RefreshOrderItems()
    {
        if (_currentOrder == null) return;

        CurrentOrderItems.Clear();
        foreach (var dish in _currentOrder.Items)
            CurrentOrderItems.Add(new OrderItemViewModel(dish));
    }

    private void UpdateOrderUI()
    {
        if (_currentOrder != null)
        {
            TotalPrice = $"{_currentOrder.TotalPrice:N2} zł";

            var strategy = _cashierService.CurrentStrategy;
            var finalPrice = strategy.CalculatePrice(_currentOrder);
            var discount = strategy.GetDiscountPercentage();

            if (discount > 0)
            {
                DiscountText = $"{strategy.Name} (-{discount}%)";
                FinalPriceText = $"Po zniżce: {finalPrice:N2} zł";
            }
            else
            {
                DiscountText = "";
                FinalPriceText = "";
            }

            CanUndo = _waiterService.CanUndo(_currentOrder);
            CanRedo = _waiterService.CanRedo(_currentOrder);
        }
        else
        {
            TotalPrice = "0,00 zł";
            DiscountText = "";
            FinalPriceText = "";
            CanUndo = false;
            CanRedo = false;
        }
    }
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
    public void Execute(object? parameter) => _execute((T?)parameter);
}

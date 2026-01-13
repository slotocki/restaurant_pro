using System.Collections.ObjectModel;
using System.Windows.Input;
using MojsAjsli.Models;
using MojsAjsli.Patterns.Mediator;
using MojsAjsli.Patterns.Observer;
using MojsAjsli.Patterns.State;
using MojsAjsli.Services;

namespace MojsAjsli.UI.ViewModels;

/// <summary>
/// Główny ViewModel koordynujący pozostałe ViewModele (Fasada)
/// Każdy ViewModel ma pojedynczą odpowiedzialność (SRP)
/// </summary>
public class MainViewModel : BaseViewModel
{
    private readonly MenuService _menuService;
    private readonly KitchenService _kitchenService;
    private readonly WaiterService _waiterService;
    private readonly RestaurantMediator _mediator;

    private DishCategory? _selectedCategory;

    public MainViewModel()
    {
        // Inicjalizacja serwisów
        var tableService = new TableService();
        _menuService = MenuService.Instance;
        _kitchenService = new KitchenService();
        _waiterService = new WaiterService("Jan");
        var cashierService = new CashierService();
        var statisticsService = new StatisticsService();
        var simulationService = new SimulationService(tableService, _menuService);

        // Mediator
        _mediator = new RestaurantMediator();
        _mediator.Register(_kitchenService);
        _mediator.Register(_waiterService);
        _mediator.Register(cashierService);

        // Observer
        var notificationSubject = new RestaurantNotificationSubject();

        // Inicjalizacja ViewModeli z pojedynczymi odpowiedzialnościami (SRP)
        NotificationVM = new NotificationViewModel();
        notificationSubject.Attach(NotificationVM);

        TableVM = new TableViewModel(tableService, NotificationVM.AddNotification);
        OrderVM = new OrderViewModel(_waiterService, _menuService, cashierService, notificationSubject, NotificationVM.AddNotification);
        KitchenVM = new KitchenViewModel(_kitchenService, notificationSubject, NotificationVM.AddNotification);
        PaymentVM = new PaymentViewModel(cashierService, _waiterService, tableService, statisticsService, NotificationVM.AddNotification);
        StatisticsVM = new StatisticsViewModel(cashierService, statisticsService, tableService);
        SimulationVM = new SimulationViewModel(simulationService, NotificationVM.AddNotification);

        // Menu
        MenuCategories = new ObservableCollection<DishCategory>(Enum.GetValues<DishCategory>());
        MenuItems = new ObservableCollection<MenuItem>(_menuService.GetAllItems());

        // Komendy dla dostarczania zamówień (delegacja do WaiterService)
        DeliverOrderCommand = new RelayCommand<Order>(DeliverOrder, order => order != null);

        // Łączenie zdarzeń między ViewModelami
        ConnectViewModels();
        SetupMediatorEvents();

        NotificationVM.AddNotification("System uruchomiony. Witamy w Mojs Ajsli - Kantynie na Rubieżach Galaktyki!");
    }

    // ViewModele z pojedynczymi odpowiedzialnościami
    public TableViewModel TableVM { get; }
    public OrderViewModel OrderVM { get; }
    public KitchenViewModel KitchenVM { get; }
    public PaymentViewModel PaymentVM { get; }
    public StatisticsViewModel StatisticsVM { get; }
    public SimulationViewModel SimulationVM { get; }
    public NotificationViewModel NotificationVM { get; }

    // Komendy
    public ICommand DeliverOrderCommand { get; }

    // Menu
    public ObservableCollection<DishCategory> MenuCategories { get; }
    public ObservableCollection<MenuItem> MenuItems { get; }

    public DishCategory? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value) && value.HasValue)
            {
                MenuItems.Clear();
                foreach (var item in _menuService.GetItemsByCategory(value.Value))
                    MenuItems.Add(item);
            }
        }
    }

    public string CurrentTime => DateTime.Now.ToString("HH:mm:ss");

    private void DeliverOrder(Order? order)
    {
        if (order == null) return;

        _waiterService.DeliverOrder(order);
        // PaymentVM.AddDeliveredOrder jest wywoływane przez mediator, więc nie dodajemy tutaj
        NotificationVM.AddNotification($"Zamówienie #{order.Id} dostarczone do stolika {order.TableNumber}");
    }

    private void ConnectViewModels()
    {
        // Gdy stolik zostanie wybrany, utwórz zamówienie
        TableVM.OnTableSelected += table =>
        {
            if (table?.Status == Models.TableStatus.Occupied)
            {
                OrderVM.CreateOrderForTable(table.Number);
            }
            else
            {
                OrderVM.ClearOrder();
            }
        };

        // Aktualizuj statystyki po zmianie stanu stolika
        TableVM.OnTableStateChanged += () =>
        {
            StatisticsVM.Update();
            if (TableVM.SelectedTable?.Status == Models.TableStatus.Occupied)
            {
                OrderVM.CreateOrderForTable(TableVM.SelectedTable.Number);
            }
        };

        // Aktualizuj kuchnię po złożeniu zamówienia
        OrderVM.OnOrderSubmitted += () =>
        {
            KitchenVM.UpdateStatus();
        };

        // Gdy zamówienie gotowe
        KitchenVM.OnOrderReady += order =>
        {
            NotificationVM.AddNotification("Zamówienie #" + order.Id + " gotowe!");
        };

        // Aktualizuj statystyki po płatności
        PaymentVM.OnPaymentCompleted += () =>
        {
            StatisticsVM.Update();
        };

        // Aktualizuj UI zamówień po zmianie strategii cenowej
        PaymentVM.OnStrategyChanged += () =>
        {
            // OrderVM może wymagać aktualizacji cen
        };
    }

    private void SetupMediatorEvents()
    {
        _mediator.OnNotification += (sender, msg) => NotificationVM.AddNotification(msg);
        _mediator.OnOrderReady += (sender, order) => NotificationVM.AddNotification("Zamówienie #" + order.Id + " gotowe!");
        _mediator.OnOrderDelivered += (sender, order) =>
        {
            NotificationVM.AddNotification("Zamówienie #" + order.Id + " dostarczone do stolika " + order.TableNumber);
            PaymentVM.AddDeliveredOrder(order);
        };
        _mediator.OnPaymentComplete += (sender, data) => StatisticsVM.Update();
    }

    public void UpdateTime()
    {
        OnPropertyChanged(nameof(CurrentTime));
        StatisticsVM.Update();
        KitchenVM.UpdateStatus();
    }

    // Dostęp do serwisów dla kompatybilności wstecznej
    public KitchenService KitchenService => _kitchenService;
    public WaiterService WaiterService => _waiterService;
    public ObservableCollection<Order> DeliveredOrders => PaymentVM.DeliveredOrders;
}

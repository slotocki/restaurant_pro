using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MojsAjsli.Models;
using MojsAjsli.Patterns.Decorator;
using MojsAjsli.Patterns.Mediator;
using MojsAjsli.Patterns.Observer;
using MojsAjsli.Patterns.State;
using MojsAjsli.Patterns.Strategy;
using MojsAjsli.Services;
using MenuItem = MojsAjsli.Models.MenuItem;

namespace MojsAjsli;

public partial class MainWindow : Window, Patterns.Observer.IObserver<OrderNotification>
{
    private readonly TableService _tableService;
    private readonly MenuService _menuService;
    private readonly KitchenService _kitchenService;
    private readonly WaiterService _waiterService;
    private readonly CashierService _cashierService;
    private readonly StatisticsService _statisticsService;
    private readonly RestaurantMediator _mediator;
    private readonly RestaurantNotificationSubject _notificationSubject;

    private Table? _selectedTable;
    private Order? _currentOrder;
    private readonly ObservableCollection<OrderItemViewModel> _currentOrderItems = new();
    private readonly ObservableCollection<Order> _deliveredOrders = new();
    private readonly ObservableCollection<string> _notificationLog = new();
    private readonly DispatcherTimer _timer;
    private int _selectedGuestCount = 2; // Domyślnie 2 gości
    private Border? _selectedGuestTile;

    public MainWindow()
    {
        InitializeComponent();

        _tableService = new TableService();
        _menuService = MenuService.Instance;
        _kitchenService = new KitchenService();
        _waiterService = new WaiterService("Jan");
        _cashierService = new CashierService();
        _statisticsService = new StatisticsService();

        _mediator = new RestaurantMediator();
        _mediator.Register(_kitchenService);
        _mediator.Register(_waiterService);
        _mediator.Register(_cashierService);

        _mediator.OnNotification += Mediator_OnNotification;
        _mediator.OnOrderReady += Mediator_OnOrderReady;
        _mediator.OnOrderDelivered += Mediator_OnOrderDelivered;
        _mediator.OnPaymentComplete += Mediator_OnPaymentComplete;

        _notificationSubject = new RestaurantNotificationSubject();
        _notificationSubject.Attach(this);

        InitializeUI();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += Timer_Tick;
        _timer.Start();

        AddNotification("System uruchomiony. Witamy w Mojs Ajsli - Kantynie na Rubieżach Galaktyki!");
    }

    private void InitializeUI()
    {
        // Inicjalizacja kafelków kategorii
        var categories = new List<DishCategory>();
        foreach (DishCategory category in Enum.GetValues<DishCategory>())
            categories.Add(category);
        CategoryTilesControl.ItemsSource = categories;

        TablesControl.ItemsSource = _tableService.Tables;
        MenuListBox.ItemsSource = _menuService.GetAllItems();
        CurrentOrderListBox.ItemsSource = _currentOrderItems;

        KitchenQueueListBox.ItemsSource = _kitchenService.OrderQueue;
        PreparingListBox.ItemsSource = _kitchenService.PreparingOrders;

        ReadyOrdersListBox.ItemsSource = _waiterService.ReadyOrders;
        DeliveredOrdersListBox.ItemsSource = _deliveredOrders;

        foreach (var strategy in _cashierService.GetAvailableStrategies())
            PricingStrategyComboBox.Items.Add(strategy);
        PricingStrategyComboBox.SelectedIndex = 0;
        PricingStrategyComboBox.DisplayMemberPath = "Name";

        PaymentMethodComboBox.Items.Add("Gotowka");
        PaymentMethodComboBox.Items.Add("Karta");
        PaymentMethodComboBox.Items.Add("BLIK");
        PaymentMethodComboBox.Items.Add("Przelew bankowy");
        PaymentMethodComboBox.SelectedIndex = 0;

        DeliveredOrdersListBox.SelectionChanged += (s, e) => UpdatePaymentSummary();

        NotificationLogListBox.ItemsSource = _notificationLog;

        UpdateStatistics();
        UpdateStatus("Gotowy do pracy");
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        CurrentTimeText.Text = DateTime.Now.ToString("HH:mm:ss");
        
        var now = DateTime.Now;
        ActiveStrategyText.Text = (now.Hour >= 15 && now.Hour < 18) ? "Happy Hour aktywne!" : "";

        UpdateKitchenStatus();
    }

    private void Table_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.Border border && border.Tag is int tableNumber)
        {
            _selectedTable = _tableService.GetTable(tableNumber);
            if (_selectedTable != null)
            {
                SelectedTableText.Text = "Stolik " + _selectedTable.Number + " (" + _selectedTable.Seats + " miejsc)";
                OrderStatusText.Text = "Status: " + GetTableStatusText(_selectedTable.Status);

                if (_selectedTable.Status == TableStatus.Occupied && _currentOrder == null)
                {
                    _currentOrder = _waiterService.CreateOrder(_selectedTable.Number);
                    UpdateOrderUI();
                }
            }
        }
    }

    private string GetTableStatusText(TableStatus status) => status switch
    {
        TableStatus.Free => "Wolny",
        TableStatus.Occupied => "Zajety",
        TableStatus.Reserved => "Zarezerwowany",
        TableStatus.NeedsCleaning => "Do sprzatania",
        _ => "Nieznany"
    };

    private void OccupyTable_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTable == null)
        {
            MessageBox.Show("Najpierw wybierz stolik!", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_tableService.SeatGuests(_selectedTable.Number, _selectedGuestCount))
        {
            _currentOrder = _waiterService.CreateOrder(_selectedTable.Number);
            UpdateOrderUI();
            AddNotification("Stolik " + _selectedTable.Number + " zajety przez " + _selectedGuestCount + " gosci");
            UpdateStatistics();
        }
        else
        {
            MessageBox.Show("Nie mozna posadzic " + _selectedGuestCount + " gosci przy tym stoliku!", "Blad", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
    
    private void FreeTable_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTable == null)
        {
            MessageBox.Show("Najpierw wybierz stolik!", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _tableService.FreeTable(_selectedTable.Number);
        _currentOrder = null;
        _currentOrderItems.Clear();
        UpdateOrderUI();
        AddNotification("Stolik " + _selectedTable.Number + " zwolniony");
        UpdateStatistics();
    }

    private void CleanTable_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTable == null)
        {
            MessageBox.Show("Najpierw wybierz stolik!", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _tableService.CleanTable(_selectedTable.Number);
        AddNotification("Stolik " + _selectedTable.Number + " wyczyszczony");
        UpdateStatistics();
    }

    private void AddToOrder_Click(object sender, RoutedEventArgs e)
    {
        if (_currentOrder == null)
        {
            MessageBox.Show("Najpierw zajmij stolik!", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MenuListBox.SelectedItem is not Models.MenuItem menuItem)
        {
            MessageBox.Show("Wybierz danie z menu!", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IDish dish = _menuService.CreateDishWithExtras(
                menuItem,
                ExtraCheeseCheck.IsChecked == true,
                BaconCheck.IsChecked == true,
                SpicySauceCheck.IsChecked == true,
                GlutenFreeCheck.IsChecked == true,
                ExtraPortionCheck.IsChecked == true
            );

            _waiterService.AddItemToOrder(_currentOrder, dish);
            _currentOrderItems.Add(new OrderItemViewModel(dish));

            ExtraCheeseCheck.IsChecked = false;
            BaconCheck.IsChecked = false;
            SpicySauceCheck.IsChecked = false;
            GlutenFreeCheck.IsChecked = false;
            ExtraPortionCheck.IsChecked = false;

            UpdateOrderUI();
            AddNotification("Dodano: " + dish.GetDescription() + " (" + dish.GetPrice().ToString("N2") + " zl)");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RemoveFromOrder_Click(object sender, RoutedEventArgs e)
    {
        if (_currentOrder == null || CurrentOrderListBox.SelectedItem is not OrderItemViewModel item)
        {
            MessageBox.Show("Wybierz pozycje do usuniecia!", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var dish = _currentOrder.Items.FirstOrDefault(d => d.GetDescription() == item.Description);
            if (dish != null)
            {
                _waiterService.RemoveItemFromOrder(_currentOrder, dish);
                _currentOrderItems.Remove(item);
                UpdateOrderUI();
                AddNotification("Usunieto: " + item.Name);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UndoOrder_Click(object sender, RoutedEventArgs e)
    {
        if (_currentOrder == null) return;

        try
        {
            _waiterService.UndoLastAction(_currentOrder);
            RefreshOrderItems();
            UpdateOrderUI();
            AddNotification("Cofnieto ostatnia akcje (Memento)");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RedoOrder_Click(object sender, RoutedEventArgs e)
    {
        if (_currentOrder == null) return;

        try
        {
            _waiterService.RedoAction(_currentOrder);
            RefreshOrderItems();
            UpdateOrderUI();
            AddNotification("Powtorzono akcje (Memento)");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshOrderItems()
    {
        if (_currentOrder == null) return;

        _currentOrderItems.Clear();
        foreach (var dish in _currentOrder.Items)
            _currentOrderItems.Add(new OrderItemViewModel(dish));
    }

    private void SubmitOrder_Click(object sender, RoutedEventArgs e)
    {
        if (_currentOrder == null || _currentOrder.Items.Count == 0)
        {
            MessageBox.Show("Zamowienie jest puste!", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _waiterService.SubmitOrder(_currentOrder);
            _notificationSubject.NotifyNewOrder(_currentOrder.Id, _currentOrder.TableNumber);

            _currentOrder = null;
            _currentOrderItems.Clear();
            UpdateOrderUI();

            if (_selectedTable?.Status == TableStatus.Occupied)
                _currentOrder = _waiterService.CreateOrder(_selectedTable.Number);

            UpdateKitchenStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateOrderUI()
    {
        if (_currentOrder != null)
        {
            TotalPriceText.Text = _currentOrder.TotalPrice.ToString("N2") + " zl";

            var strategy = _cashierService.CurrentStrategy;
            var finalPrice = strategy.CalculatePrice(_currentOrder);
            var discount = strategy.GetDiscountPercentage();

            if (discount > 0)
            {
                DiscountText.Text = strategy.Name + " (-" + discount + "%)";
                FinalPriceText.Text = "Po znizce: " + finalPrice.ToString("N2") + " zl";
            }
            else
            {
                DiscountText.Text = "";
                FinalPriceText.Text = "";
            }

            UndoButton.IsEnabled = _waiterService.CanUndo(_currentOrder);
            RedoButton.IsEnabled = _waiterService.CanRedo(_currentOrder);
        }
        else
        {
            TotalPriceText.Text = "0,00 zl";
            DiscountText.Text = "";
            FinalPriceText.Text = "";
            UndoButton.IsEnabled = false;
            RedoButton.IsEnabled = false;
        }
    }

    private void StartPreparing_Click(object sender, RoutedEventArgs e)
    {
        if (KitchenQueueListBox.SelectedItem is Order order)
        {
            _kitchenService.StartPreparing(order);
            AddNotification("Rozpoczeto przygotowanie zamowienia #" + order.Id);
            UpdateKitchenStatus();
        }
        else
        {
            MessageBox.Show("Wybierz zamowienie z kolejki!", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void MarkReady_Click(object sender, RoutedEventArgs e)
    {
        if (PreparingListBox.SelectedItem is Order order)
        {
            _kitchenService.CompleteOrder(order);
            _notificationSubject.NotifyOrderReady(order.Id, order.TableNumber);
            UpdateKitchenStatus();
        }
        else
        {
            MessageBox.Show("Wybierz zamowienie!", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void UpdateKitchenStatus()
    {
        QueueCountText.Text = "W kolejce: " + _kitchenService.GetQueueLength() + " zamowien";
        PreparingCountText.Text = "Przygotowywanych: " + _kitchenService.GetPreparingCount();

        KitchenQueueStatusText.Text = "W kolejce: " + _kitchenService.GetQueueLength();
        KitchenPreparingStatusText.Text = "W przygotowaniu: " + _kitchenService.GetPreparingCount();
        EstimatedWaitText.Text = "Szacowany czas: " + _kitchenService.EstimateWaitTime().TotalMinutes.ToString("N0") + " min";
    }

    private void DeliverOrder_Click(object sender, RoutedEventArgs e)
    {
        if (ReadyOrdersListBox.SelectedItem is Order order)
        {
            _waiterService.DeliverOrder(order);
            _deliveredOrders.Add(order);
            _notificationSubject.NotifyOrderDelivered(order.Id, order.TableNumber);
            UpdatePaymentSummary();
        }
        else
        {
            MessageBox.Show("Wybierz zamowienie do dostarczenia!", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void PricingStrategy_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (PricingStrategyComboBox.SelectedItem is IPricingStrategy strategy)
        {
            _cashierService.SetPricingStrategy(strategy);
            UpdatePaymentSummary();
            UpdateOrderUI();
        }
    }

    private void UpdatePaymentSummary()
    {
        if (DeliveredOrdersListBox.SelectedItem is Order order)
        {
            var strategy = _cashierService.CurrentStrategy;
            var originalPrice = order.TotalPrice;
            var finalPrice = strategy.CalculatePrice(order);
            var discount = strategy.GetDiscountPercentage();

            PaymentSummaryText.Text = "Do zaplaty: " + finalPrice.ToString("N2") + " zl";
            PaymentDiscountText.Text = discount > 0 
                ? "Znizka " + discount + "% (bylo: " + originalPrice.ToString("N2") + " zl)" 
                : "";
        }
        else
        {
            PaymentSummaryText.Text = "Wybierz zamowienie";
            PaymentDiscountText.Text = "";
        }
    }

    private void ProcessPayment_Click(object sender, RoutedEventArgs e)
    {
        if (DeliveredOrdersListBox.SelectedItem is not Order order)
        {
            MessageBox.Show("Wybierz zamowienie do oplacenia!", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var paymentType = PaymentMethodComboBox.SelectedIndex switch
        {
            0 => PaymentType.Cash,
            1 => PaymentType.Card,
            2 => PaymentType.Blik,
            3 => PaymentType.BankTransfer,
            _ => PaymentType.Cash
        };

        string? blikCode = null;
        if (paymentType == PaymentType.Blik)
        {
            blikCode = BlikCodeTextBox.Text;
            if (string.IsNullOrEmpty(blikCode) || blikCode.Length != 6)
            {
                MessageBox.Show("Podaj prawidlowy 6-cyfrowy kod BLIK!", "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        try
        {
            var payment = _cashierService.ProcessPayment(order, paymentType, blikCode);

            if (payment.IsSuccessful)
            {
                var finalPrice = _cashierService.CurrentStrategy.CalculatePrice(order);
                _statisticsService.RecordCompletedOrder(order, finalPrice);
                _deliveredOrders.Remove(order);
                _waiterService.CompleteOrder(order);

                _tableService.FreeTable(order.TableNumber);

                AddNotification("Platnosc " + finalPrice.ToString("N2") + " zl - " + payment.Type + " (#" + payment.TransactionId + ")");
                UpdateStatistics();

                MessageBox.Show("Platnosc zrealizowana!\n\nKwota: " + finalPrice.ToString("N2") + " zl\nMetoda: " + payment.Type + "\nTransakcja: " + payment.TransactionId,
                    "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Platnosc nie powiodla sie!", "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateStatistics()
    {
        DailyRevenueText.Text = _cashierService.GetDailyRevenue().ToString("N2") + " zl";
        TodayOrdersText.Text = _cashierService.GetTodayTransactionCount().ToString();
        AverageOrderText.Text = _statisticsService.GetAverageOrderValue().ToString("N2") + " zl";

        FreeTablesText.Text = "Wolne: " + _tableService.GetFreeTablesCount();
        OccupiedTablesText.Text = "Zajete: " + _tableService.GetOccupiedTablesCount();
        TotalSeatsText.Text = "Miejsca: " + _tableService.GetOccupiedSeats() + "/" + _tableService.GetTotalSeats();

        TablesStatusText.Text = "Stoliki: " + _tableService.GetOccupiedTablesCount() + "/" + _tableService.Tables.Count + " zajetych";

        TopDishesListBox.ItemsSource = _statisticsService.GetTopDishes();
    }

    private void Mediator_OnNotification(object? sender, string message)
    {
        Dispatcher.Invoke(() => AddNotification(message));
    }

    private void Mediator_OnOrderReady(object? sender, Order order)
    {
        Dispatcher.Invoke(() => AddNotification("Zamowienie #" + order.Id + " gotowe!"));
    }

    private void Mediator_OnOrderDelivered(object? sender, Order order)
    {
        Dispatcher.Invoke(() => AddNotification("Zamowienie #" + order.Id + " dostarczone do stolika " + order.TableNumber));
    }

    private void Mediator_OnPaymentComplete(object? sender, (int TableNumber, decimal Amount) data)
    {
        Dispatcher.Invoke(() => UpdateStatistics());
    }

    public void Update(OrderNotification data)
    {
        Dispatcher.Invoke(() => AddNotification("[" + data.Status + "] " + data.Message));
    }

    private void AddNotification(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        _notificationLog.Insert(0, "[" + timestamp + "] " + message);

        while (_notificationLog.Count > 100)
            _notificationLog.RemoveAt(_notificationLog.Count - 1);
    }

    private void UpdateStatus(string status)
    {
        StatusText.Text = status;
    }

    private void CategoryTile_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.Border border && border.Tag is DishCategory category)
        {
            var converter = new UI.Converters.CategoryToNameConverter();
            SelectedCategoryText.Text = converter.Convert(category, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture).ToString();
            MenuListBox.ItemsSource = _menuService.GetItemsByCategory(category);
        }
    }

    private void GuestCountTile_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is int guestCount)
        {
            _selectedGuestCount = guestCount;
            
            // Resetuj kolor wszystkich kafelków do domyślnego
            Guest1Tile.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8D6E63"));
            Guest2Tile.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8D6E63"));
            Guest3Tile.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8D6E63"));
            Guest4Tile.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8D6E63"));
            
            // Ustaw kolor wybranego kafelka
            border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5D4037"));
            _selectedGuestTile = border;
            
            // Wyczyść pole niestandardowe
            CustomGuestCountTextBox.Text = "";
            
            AddNotification("Wybrano liczbe gosci: " + guestCount);
        }
    }
    
    private void CustomGuestCount_Changed(object sender, TextChangedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(CustomGuestCountTextBox.Text))
        {
            if (int.TryParse(CustomGuestCountTextBox.Text, out int customCount) && customCount > 0)
            {
                _selectedGuestCount = customCount;
                
                // Resetuj kolor wszystkich kafelków
                Guest1Tile.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8D6E63"));
                Guest2Tile.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8D6E63"));
                Guest3Tile.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8D6E63"));
                Guest4Tile.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8D6E63"));
                _selectedGuestTile = null;
                
                AddNotification("Wybrano niestandardowa liczbe gosci: " + customCount);
            }
        }
        else
        {
            // Jeśli pole zostało wyczyszczone, przywróć domyślny wybór (2)
            _selectedGuestCount = 2;
            Guest1Tile.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8D6E63"));
            Guest2Tile.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5D4037"));
            Guest3Tile.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8D6E63"));
            Guest4Tile.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8D6E63"));
            _selectedGuestTile = Guest2Tile;
        }
    }
}

public class OrderItemViewModel
{
    public string Name { get; }
    public string Description { get; }
    public decimal Price { get; }

    public OrderItemViewModel(IDish dish)
    {
        Name = dish.GetName();
        Description = dish.GetDescription();
        Price = dish.GetPrice();
    }
}

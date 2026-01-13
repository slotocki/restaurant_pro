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
    private readonly SimulationService _simulationService;
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
        _simulationService = new SimulationService(_tableService, _menuService);

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
        TablesControl.ItemsSource = _tableService.Tables;
        MenuListBox.ItemsSource = _menuService.GetAllItems();
        CurrentOrderListBox.ItemsSource = _currentOrderItems;

        // Inicjalizacja kafelków kategorii
        CategoryTilesControl.ItemsSource = Enum.GetValues(typeof(DishCategory)).Cast<DishCategory>();

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

        // Inicjalizacja domyślnego wyboru kafelka gości (2)
        _selectedGuestTile = Guest2Tile;
        Guest2Tile.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5D4037"));

        UpdateStatistics();
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

    private void RunSimulation_Click(object sender, RoutedEventArgs e)
    {
        // Pobierz czas trwania symulacji z pola tekstowego
        if (!int.TryParse(SimulationDurationTextBox.Text, out int duration) || duration <= 0)
        {
            MessageBox.Show("Podaj prawidłowy czas symulacji (w minutach)!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Pobierz prędkość symulacji
        int speed = 500; // domyślna
        if (SimulationSpeedComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string tagValue)
        {
            speed = int.Parse(tagValue);
        }

        // Uruchom symulację w trybie czasu rzeczywistego
        _simulationService.StartSimulation(duration, speed);
        
        // Przełącz źródła danych dla zakładki Kuchnia i Kelner na dane z symulacji
        SwitchToSimulationDataSources();
        
        // Podłącz obserwację wyników na żywo
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        timer.Tick += (s, args) =>
        {
            var result = _simulationService.GetCurrentResults();
            
            // Aktualizuj wyniki na bieżąco
            SimServedText.Text = result.ServedGuests.ToString();
            SimLostText.Text = result.LostGuests.ToString();
            SimRevenueText.Text = result.TotalRevenue.ToString("N2") + " zł";
            SimServiceRateText.Text = result.ServiceRate.ToString("N1") + "%";
            
            // Top danie
            if (result.OrderedDishes.Any())
            {
                var topDish = result.OrderedDishes.OrderByDescending(x => x.Value).First();
                SimTopDishText.Text = topDish.Key + " (" + topDish.Value + " szt.)";
            }
            else
            {
                SimTopDishText.Text = "-";
            }
            
            // Aktualizuj log
            SimulationLogListBox.ItemsSource = _simulationService.SimulationLog;
            
            // Przewiń log na dół
            if (SimulationLogListBox.Items.Count > 0)
            {
                SimulationLogListBox.ScrollIntoView(SimulationLogListBox.Items[SimulationLogListBox.Items.Count - 1]);
            }
            
            // Aktualizuj liczniki w zakładce Kuchnia
            QueueCountText.Text = $"W kolejce: {_simulationService.KitchenQueueCount}";
            PreparingCountText.Text = $"Przygotowywane: {_simulationService.PreparingCount}";
            
            // Status
            if (result.AcceptingNewGuests)
            {
                SimStatusText.Text = $"Trwa... Czas: {_simulationService.CurrentTimeFormatted} (przyjmowanie gości)";
            }
            else if (result.IsRunning)
            {
                SimStatusText.Text = $"Trwa... Czas: {_simulationService.CurrentTimeFormatted} (finalizacja)";
            }
            else
            {
                SimStatusText.Text = "Zakończona";
                timer.Stop();
                RunSimulationButton.IsEnabled = true;
                StopSimulationButton.IsEnabled = false;
                
                // Przywróć oryginalne źródła danych
                RestoreOriginalDataSources();
                UpdateStatistics();
            }
        };
        timer.Start();

        // Dezaktywuj/aktywuj przyciski
        RunSimulationButton.IsEnabled = false;
        StopSimulationButton.IsEnabled = true;
        SimStatusText.Text = "Uruchamianie symulacji...";
        
        AddNotification("Symulacja uruchomiona w trybie czasu rzeczywistego");
    }

    private void ApplySimulationParams_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Pobierz i zastosuj parametry symulacji
            if (int.TryParse(MinArrivalTextBox.Text, out int minArrival))
                _simulationService.MinArrivalInterval = minArrival;
            
            if (int.TryParse(MaxArrivalTextBox.Text, out int maxArrival))
                _simulationService.MaxArrivalInterval = maxArrival;
            
            if (int.TryParse(MinGroupSizeTextBox.Text, out int minGroup))
                _simulationService.MinGroupSize = minGroup;
            
            if (int.TryParse(MaxGroupSizeTextBox.Text, out int maxGroup))
                _simulationService.MaxGroupSize = maxGroup;
            
            if (int.TryParse(MinWaitTimeTextBox.Text, out int minWait))
                _simulationService.MinWaitTime = minWait;
            
            if (int.TryParse(MaxWaitTimeTextBox.Text, out int maxWait))
                _simulationService.MaxWaitTime = maxWait;
            
            if (int.TryParse(MaxConcurrentOrdersTextBox.Text, out int maxConcurrent))
                _simulationService.MaxConcurrentOrders = maxConcurrent;
            
            AddNotification("Parametry symulacji zaktualizowane");
            MessageBox.Show("Parametry symulacji zostały zaktualizowane!\n\n" +
                $"Interwał klientów: {_simulationService.MinArrivalInterval}-{_simulationService.MaxArrivalInterval} min\n" +
                $"Rozmiar grup: {_simulationService.MinGroupSize}-{_simulationService.MaxGroupSize} osób\n" +
                $"Czas oczekiwania: {_simulationService.MinWaitTime}-{_simulationService.MaxWaitTime} min\n" +
                $"Równoległe zamówienia: {_simulationService.MaxConcurrentOrders}",
                "Parametry zapisane", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Błąd podczas zapisywania parametrów: " + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void StopSimulation_Click(object sender, RoutedEventArgs e)
    {
        _simulationService.StopSimulation();
        
        RunSimulationButton.IsEnabled = true;
        StopSimulationButton.IsEnabled = false;
        
        var result = _simulationService.GetCurrentResults();
        SimStatusText.Text = $"Zatrzymana - obsłużono {result.ServedGuests} gości, utracono {result.LostGuests}";
        
        // Przywróć oryginalne źródła danych
        RestoreOriginalDataSources();
        UpdateStatistics();
        AddNotification("Symulacja zatrzymana przez użytkownika");
    }
    
    private void SwitchToSimulationDataSources()
    {
        // Przełącz źródła danych list na kolekcje z symulacji
        KitchenQueueListBox.ItemsSource = _simulationService.KitchenQueue;
        PreparingListBox.ItemsSource = _simulationService.PreparingOrders;
        ReadyOrdersListBox.ItemsSource = _simulationService.ReadyOrders;
        DeliveredOrdersListBox.ItemsSource = _simulationService.DeliveredOrders;
    }
    
    private void RestoreOriginalDataSources()
    {
        // Przywróć oryginalne źródła danych z serwisów
        KitchenQueueListBox.ItemsSource = _kitchenService.OrderQueue;
        PreparingListBox.ItemsSource = _kitchenService.PreparingOrders;
        ReadyOrdersListBox.ItemsSource = _waiterService.ReadyOrders;
        DeliveredOrdersListBox.ItemsSource = _deliveredOrders;
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

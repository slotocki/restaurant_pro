using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MojsAjsli.Models;
using MojsAjsli.Patterns.Decorator;
using MojsAjsli.Patterns.State;
using MojsAjsli.Patterns.Strategy;
using MojsAjsli.Services.Interfaces.Dishes;
using MojsAjsli.ViewModels;
using MenuItem = MojsAjsli.Models.MenuItem;

namespace MojsAjsli;

/// <summary>
/// MainWindow - odpowiedzialność ograniczona do:
/// - Inicjalizacji UI i bindingów
/// - Obsługi zdarzeń UI specyficznych dla WPF (kliknięcia, nawigacja)
/// - Delegowania logiki biznesowej do ViewModeli
/// 
/// Logika biznesowa została wydzielona do ViewModeli zgodnie z SRP:
/// - TableViewModel - zarządzanie stolikami
/// - OrderViewModel - zarządzanie zamówieniami
/// - KitchenViewModel - obsługa kuchni
/// - PaymentViewModel - obsługa płatności
/// - StatisticsViewModel - statystyki
/// - SimulationViewModel - symulacja
/// - NotificationViewModel - powiadomienia
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly DispatcherTimer _timer;
    private Border? _selectedGuestTile;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        InitializeUI();
        SubscribeToViewModelEvents();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }

    #region Inicjalizacja UI

    private void InitializeUI()
    {
        // Stoliki
        TablesControl.ItemsSource = _viewModel.TableVM.Tables;

        // Menu
        MenuListBox.ItemsSource = _viewModel.MenuItems;
        CategoryTilesControl.ItemsSource = _viewModel.MenuCategories;

        // Zamówienia
        CurrentOrderListBox.ItemsSource = _viewModel.OrderVM.CurrentOrderItems;

        // Kuchnia
        KitchenQueueListBox.ItemsSource = _viewModel.KitchenVM.OrderQueue;
        PreparingListBox.ItemsSource = _viewModel.KitchenVM.PreparingOrders;

        // Kelner/Dostawa
        ReadyOrdersListBox.ItemsSource = _viewModel.WaiterService.ReadyOrders;
        DeliveredOrdersListBox.ItemsSource = _viewModel.DeliveredOrders;

        // Płatności
        foreach (var strategy in _viewModel.PaymentVM.AvailableStrategies)
            PricingStrategyComboBox.Items.Add(strategy);
        PricingStrategyComboBox.SelectedIndex = 0;
        PricingStrategyComboBox.DisplayMemberPath = "Name";

        foreach (var method in _viewModel.PaymentVM.PaymentMethods)
            PaymentMethodComboBox.Items.Add(method);
        PaymentMethodComboBox.SelectedIndex = 0;

        // Domyślny wybór liczby gości
        _selectedGuestTile = Guest2Tile;
        Guest2Tile.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5D4037"));

        UpdateStatisticsUI();
    }

    private void SubscribeToViewModelEvents()
    {
        // Zdarzenia płatności
        _viewModel.PaymentVM.OnPaymentError += msg =>
            MessageBox.Show(msg, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);

        _viewModel.PaymentVM.OnPaymentSuccess += (amount, method, transactionId) =>
        {
            UpdateStatisticsUI();
            MessageBox.Show(
                $"Płatność zrealizowana!\n\nKwota: {amount:N2} zł\nMetoda: {method}\nTransakcja: {transactionId}",
                "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
        };

        _viewModel.PaymentVM.OnPaymentCompleted += UpdateStatisticsUI;
        _viewModel.PaymentVM.OnStrategyChanged += UpdateOrderUI;

        // Zdarzenia symulacji
        _viewModel.SimulationVM.OnSimulationStarted += () =>
        {
            RunSimulationButton.IsEnabled = false;
            StopSimulationButton.IsEnabled = true;
            SwitchToSimulationDataSources();
            StartSimulationUpdateTimer();
        };

        _viewModel.SimulationVM.OnSimulationStopped += () =>
        {
            RunSimulationButton.IsEnabled = true;
            StopSimulationButton.IsEnabled = false;
            RestoreOriginalDataSources();
            UpdateStatisticsUI();
        };

        _viewModel.SimulationVM.OnParamsApplied += () =>
            MessageBox.Show(
                $"Parametry symulacji zostały zaktualizowane!\n\n" +
                $"Interwał klientów: {_viewModel.SimulationVM.MinArrivalInterval}-{_viewModel.SimulationVM.MaxArrivalInterval} min\n" +
                $"Rozmiar grup: {_viewModel.SimulationVM.MinGroupSize}-{_viewModel.SimulationVM.MaxGroupSize} osób\n" +
                $"Czas oczekiwania: {_viewModel.SimulationVM.MinWaitTime}-{_viewModel.SimulationVM.MaxWaitTime} min\n" +
                $"Równoległe zamówienia: {_viewModel.SimulationVM.MaxConcurrentOrders}",
                "Parametry zapisane", MessageBoxButton.OK, MessageBoxImage.Information);

        // Zdarzenia stolików
        _viewModel.TableVM.OnTableStateChanged += UpdateStatisticsUI;
    }

    #endregion

    #region Timer i aktualizacje UI

    private void Timer_Tick(object? sender, EventArgs e)
    {
        CurrentTimeText.Text = DateTime.Now.ToString("HH:mm:ss");
        _viewModel.UpdateTime();

        var now = DateTime.Now;
        ActiveStrategyText.Text = (now.Hour >= 15 && now.Hour < 18) ? "Happy Hour aktywne!" : "";

        UpdateKitchenStatusUI();
    }

    private void UpdateStatisticsUI()
    {
        DailyRevenueText.Text = _viewModel.StatisticsVM.DailyRevenue;
        TodayOrdersText.Text = _viewModel.StatisticsVM.TodayOrders;
        AverageOrderText.Text = _viewModel.StatisticsVM.AverageOrder;
        FreeTablesText.Text = _viewModel.StatisticsVM.FreeTables;
        OccupiedTablesText.Text = _viewModel.StatisticsVM.OccupiedTables;
        TotalSeatsText.Text = _viewModel.StatisticsVM.TotalSeats;
    }

    private void UpdateOrderUI()
    {
        TotalPriceText.Text = _viewModel.OrderVM.TotalPrice;
        DiscountText.Text = _viewModel.OrderVM.DiscountText;
        FinalPriceText.Text = _viewModel.OrderVM.FinalPriceText;
        UndoButton.IsEnabled = _viewModel.OrderVM.CanUndo;
        RedoButton.IsEnabled = _viewModel.OrderVM.CanRedo;
    }

    private void UpdateKitchenStatusUI()
    {
        _viewModel.KitchenVM.UpdateStatus();
        QueueCountText.Text = _viewModel.KitchenVM.QueueCount;
        PreparingCountText.Text = _viewModel.KitchenVM.PreparingCount;
        KitchenQueueStatusText.Text = _viewModel.KitchenVM.QueueCount;
        KitchenPreparingStatusText.Text = _viewModel.KitchenVM.PreparingCount;
        EstimatedWaitText.Text = _viewModel.KitchenVM.EstimatedWaitTime;
    }

    private void UpdatePaymentSummaryUI()
    {
        PaymentSummaryText.Text = _viewModel.PaymentVM.PaymentSummary;
        PaymentDiscountText.Text = _viewModel.PaymentVM.PaymentDiscount;
    }

    #endregion

    #region Obsługa stolików (delegacja do TableViewModel)

    private void Table_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is int tableNumber)
        {
            _viewModel.TableVM.SelectTable(tableNumber);
            SelectedTableText.Text = _viewModel.TableVM.SelectedTableInfo;
            OrderStatusText.Text = _viewModel.TableVM.TableStatus;
            UpdateOrderUI();
        }
    }

    private void OccupyTable_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.TableVM.SelectedTable == null)
        {
            MessageBox.Show("Najpierw wybierz stolik!", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _viewModel.TableVM.OccupyTableCommand.Execute(null);
        UpdateOrderUI();
        UpdateStatisticsUI();
    }

    private void FreeTable_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.TableVM.SelectedTable == null)
        {
            MessageBox.Show("Najpierw wybierz stolik!", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _viewModel.TableVM.FreeTableCommand.Execute(null);
        _viewModel.OrderVM.ClearOrder();
        UpdateOrderUI();
        UpdateStatisticsUI();
    }

    private void CleanTable_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.TableVM.SelectedTable == null)
        {
            MessageBox.Show("Najpierw wybierz stolik!", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _viewModel.TableVM.CleanTableCommand.Execute(null);
        UpdateStatisticsUI();
    }

    private void GuestCountTile_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is int guestCount)
        {
            _viewModel.TableVM.SelectedGuestCount = guestCount;
            ResetGuestTileColors();
            border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5D4037"));
            _selectedGuestTile = border;
            CustomGuestCountTextBox.Text = "";
            _viewModel.NotificationVM.AddNotification($"Wybrano liczbę gości: {guestCount}");
        }
    }

    private void CustomGuestCount_Changed(object sender, TextChangedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(CustomGuestCountTextBox.Text))
        {
            if (int.TryParse(CustomGuestCountTextBox.Text, out int customCount) && customCount > 0)
            {
                _viewModel.TableVM.SelectedGuestCount = customCount;
                ResetGuestTileColors();
                _selectedGuestTile = null;
                _viewModel.NotificationVM.AddNotification($"Wybrano niestandardową liczbę gości: {customCount}");
            }
        }
        else
        {
            _viewModel.TableVM.SelectedGuestCount = 2;
            ResetGuestTileColors();
            Guest2Tile.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5D4037"));
            _selectedGuestTile = Guest2Tile;
        }
    }

    private void ResetGuestTileColors()
    {
        var defaultColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8D6E63"));
        Guest1Tile.Background = defaultColor;
        Guest2Tile.Background = defaultColor;
        Guest3Tile.Background = defaultColor;
        Guest4Tile.Background = defaultColor;
    }

    #endregion

    #region Obsługa zamówień (delegacja do OrderViewModel)

    private void CategoryTile_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is DishCategory category)
        {
            _viewModel.SelectedCategory = category;
            var converter = new UI.Converters.CategoryToNameConverter();
            SelectedCategoryText.Text = converter.Convert(category, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture)?.ToString();
            MenuListBox.ItemsSource = _viewModel.MenuItems;
        }
    }

    private void AddToOrder_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.OrderVM.CurrentOrder == null)
        {
            MessageBox.Show("Najpierw zajmij stolik!", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MenuListBox.SelectedItem is not MenuItem menuItem)
        {
            MessageBox.Show("Wybierz danie z menu!", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _viewModel.OrderVM.SelectedMenuItem = menuItem;
            _viewModel.OrderVM.ExtraCheese = ExtraCheeseCheck.IsChecked == true;
            _viewModel.OrderVM.Bacon = BaconCheck.IsChecked == true;
            _viewModel.OrderVM.SpicySauce = SpicySauceCheck.IsChecked == true;
            _viewModel.OrderVM.GlutenFree = GlutenFreeCheck.IsChecked == true;
            _viewModel.OrderVM.ExtraPortion = ExtraPortionCheck.IsChecked == true;

            _viewModel.OrderVM.AddToOrderCommand.Execute(null);

            ExtraCheeseCheck.IsChecked = false;
            BaconCheck.IsChecked = false;
            SpicySauceCheck.IsChecked = false;
            GlutenFreeCheck.IsChecked = false;
            ExtraPortionCheck.IsChecked = false;

            UpdateOrderUI();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RemoveFromOrder_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.OrderVM.CurrentOrder == null || CurrentOrderListBox.SelectedItem is not OrderItemViewModel item)
        {
            MessageBox.Show("Wybierz pozycję do usunięcia!", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _viewModel.OrderVM.RemoveFromOrderCommand.Execute(item);
        UpdateOrderUI();
    }

    private void UndoOrder_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.OrderVM.CurrentOrder == null) return;

        try
        {
            _viewModel.OrderVM.UndoCommand.Execute(null);
            UpdateOrderUI();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RedoOrder_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.OrderVM.CurrentOrder == null) return;

        try
        {
            _viewModel.OrderVM.RedoCommand.Execute(null);
            UpdateOrderUI();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SubmitOrder_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.OrderVM.CurrentOrder == null || _viewModel.OrderVM.CurrentOrderItems.Count == 0)
        {
            MessageBox.Show("Zamówienie jest puste!", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _viewModel.OrderVM.SubmitOrderCommand.Execute(null);
            UpdateOrderUI();
            UpdateKitchenStatusUI();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region Obsługa kuchni (delegacja do KitchenViewModel)

    private void StartPreparing_Click(object sender, RoutedEventArgs e)
    {
        if (KitchenQueueListBox.SelectedItem is Order order)
        {
            _viewModel.KitchenVM.SelectedQueueOrder = order;
            _viewModel.KitchenVM.StartPreparingCommand.Execute(null);
            UpdateKitchenStatusUI();
        }
        else
        {
            MessageBox.Show("Wybierz zamówienie z kolejki!", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void MarkReady_Click(object sender, RoutedEventArgs e)
    {
        if (PreparingListBox.SelectedItem is Order order)
        {
            _viewModel.KitchenVM.SelectedPreparingOrder = order;
            _viewModel.KitchenVM.MarkReadyCommand.Execute(null);
            UpdateKitchenStatusUI();
        }
        else
        {
            MessageBox.Show("Wybierz zamówienie!", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    #endregion

    #region Obsługa dostawy i płatności (delegacja do PaymentViewModel)

    private void DeliverOrder_Click(object sender, RoutedEventArgs e)
    {
        if (ReadyOrdersListBox.SelectedItem is Order order)
        {
            _viewModel.WaiterService.DeliverOrder(order);
            _viewModel.PaymentVM.AddDeliveredOrder(order);
            _viewModel.NotificationVM.AddNotification($"Zamówienie #{order.Id} dostarczone do stolika {order.TableNumber}");
            UpdatePaymentSummaryUI();
        }
        else
        {
            MessageBox.Show("Wybierz zamówienie do dostarczenia!", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void PricingStrategy_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (PricingStrategyComboBox.SelectedItem is IPricingStrategy strategy)
        {
            _viewModel.PaymentVM.SelectedStrategy = strategy;
            UpdatePaymentSummaryUI();
            UpdateOrderUI();
        }
    }

    private void DeliveredOrdersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DeliveredOrdersListBox.SelectedItem is Order order)
        {
            _viewModel.PaymentVM.SelectedOrder = order;
            UpdatePaymentSummaryUI();
        }
    }

    private void ProcessPayment_Click(object sender, RoutedEventArgs e)
    {
        if (DeliveredOrdersListBox.SelectedItem is not Order)
        {
            MessageBox.Show("Wybierz zamówienie do opłacenia!", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _viewModel.PaymentVM.SelectedPaymentMethodIndex = PaymentMethodComboBox.SelectedIndex;
        _viewModel.PaymentVM.BlikCode = BlikCodeTextBox.Text;
        _viewModel.PaymentVM.ProcessPaymentCommand.Execute(null);
    }

    #endregion

    #region Symulacja (delegacja do SimulationViewModel)

    private void RunSimulation_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(SimulationDurationTextBox.Text, out int duration) || duration <= 0)
        {
            MessageBox.Show("Podaj prawidłowy czas symulacji (w minutach)!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int speed = 500;
        if (SimulationSpeedComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string tagValue)
        {
            speed = int.Parse(tagValue);
        }

        _viewModel.SimulationVM.Duration = duration;
        _viewModel.SimulationVM.Speed = speed;
        _viewModel.SimulationVM.Run();
    }

    private void StopSimulation_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SimulationVM.Stop();
    }

    private void ApplySimulationParams_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (int.TryParse(MinArrivalTextBox.Text, out int minArrival))
                _viewModel.SimulationVM.MinArrivalInterval = minArrival;
            if (int.TryParse(MaxArrivalTextBox.Text, out int maxArrival))
                _viewModel.SimulationVM.MaxArrivalInterval = maxArrival;
            if (int.TryParse(MinGroupSizeTextBox.Text, out int minGroup))
                _viewModel.SimulationVM.MinGroupSize = minGroup;
            if (int.TryParse(MaxGroupSizeTextBox.Text, out int maxGroup))
                _viewModel.SimulationVM.MaxGroupSize = maxGroup;
            if (int.TryParse(MinWaitTimeTextBox.Text, out int minWait))
                _viewModel.SimulationVM.MinWaitTime = minWait;
            if (int.TryParse(MaxWaitTimeTextBox.Text, out int maxWait))
                _viewModel.SimulationVM.MaxWaitTime = maxWait;
            if (int.TryParse(MaxConcurrentOrdersTextBox.Text, out int maxConcurrent))
                _viewModel.SimulationVM.MaxConcurrentOrders = maxConcurrent;

            _viewModel.SimulationVM.ApplyParamsCommand.Execute(null);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Błąd podczas zapisywania parametrów: " + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void StartSimulationUpdateTimer()
    {
        var simTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        simTimer.Tick += (s, args) =>
        {
            _viewModel.SimulationVM.UpdateResults();

            SimServedText.Text = _viewModel.SimulationVM.ServedGuests;
            SimLostText.Text = _viewModel.SimulationVM.LostGuests;
            SimRevenueText.Text = _viewModel.SimulationVM.TotalRevenue;
            SimServiceRateText.Text = _viewModel.SimulationVM.ServiceRate;
            SimTopDishText.Text = _viewModel.SimulationVM.TopDish;
            SimStatusText.Text = _viewModel.SimulationVM.Status;

            SimulationLogListBox.ItemsSource = _viewModel.SimulationVM.Log;

            if (SimulationLogListBox.Items.Count > 0)
                SimulationLogListBox.ScrollIntoView(SimulationLogListBox.Items[^1]);

            if (!_viewModel.SimulationVM.CanStop)
            {
                simTimer.Stop();
            }
        };
        simTimer.Start();
    }

    private void SwitchToSimulationDataSources()
    {
        KitchenQueueListBox.ItemsSource = _viewModel.SimulationVM.KitchenQueue;
        PreparingListBox.ItemsSource = _viewModel.SimulationVM.PreparingOrders;
        ReadyOrdersListBox.ItemsSource = _viewModel.SimulationVM.ReadyOrders;
        DeliveredOrdersListBox.ItemsSource = _viewModel.SimulationVM.DeliveredOrders;
    }

    private void RestoreOriginalDataSources()
    {
        KitchenQueueListBox.ItemsSource = _viewModel.KitchenVM.OrderQueue;
        PreparingListBox.ItemsSource = _viewModel.KitchenVM.PreparingOrders;
        ReadyOrdersListBox.ItemsSource = _viewModel.WaiterService.ReadyOrders;
        DeliveredOrdersListBox.ItemsSource = _viewModel.DeliveredOrders;
    }

    #endregion
}

/// <summary>
/// ViewModel dla pojedynczej pozycji zamówienia
/// </summary>
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

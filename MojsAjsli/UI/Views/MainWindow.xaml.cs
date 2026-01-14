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
using MojsAjsli.UI.ViewModels;
using MenuItem = MojsAjsli.Models.MenuItem;

namespace MojsAjsli.UI.Views;

/// <summary>
/// MainWindow - odpowiedzialność ograniczona do:
/// - Inicjalizacji UI i bindingów
/// - Obsługi zdarzeń UI specyficznych dla WPF (kliknięcia, nawigacja)
/// - Delegowania logiki biznesowej do ViewModeli
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
        // Statystyki zostały przeniesione do symulacji - usunięte niepotrzebne referencje
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
        // Usunięte odwołania do elementów ze Statystyk
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
            SelectedCategoryText.Text = converter.Convert(category, typeof(string), null!, System.Globalization.CultureInfo.CurrentCulture)?.ToString() ?? string.Empty;
            MenuListBox.ItemsSource = _viewModel.MenuItems;
        }
    }

    #endregion

    #region Obsługa płatności i symulacji

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

    #endregion

    #region Symulacja

    private void RunSimulationButton_Click(object sender, RoutedEventArgs e)
    {
        // Odczytaj wartości z UI przed uruchomieniem symulacji
        if (int.TryParse(SimulationDurationTextBox.Text, out int duration))
        {
            _viewModel.SimulationVM.Duration = duration;
        }

        // Odczytaj prędkość z ComboBox
        if (SimulationSpeedComboBox.SelectedItem is ComboBoxItem selectedItem && 
            selectedItem.Tag != null &&
            int.TryParse(selectedItem.Tag.ToString(), out int speed))
        {
            _viewModel.SimulationVM.Speed = speed;
        }

        // Uruchom symulację
        if (_viewModel.SimulationVM.RunCommand.CanExecute(null))
        {
            _viewModel.SimulationVM.RunCommand.Execute(null);
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

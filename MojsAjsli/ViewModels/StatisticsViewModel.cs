using MojsAjsli.Services;

namespace MojsAjsli.ViewModels;

/// <summary>
/// ViewModel odpowiedzialny za statystyki (SRP)
/// </summary>
public class StatisticsViewModel : BaseViewModel
{
    private readonly CashierService _cashierService;
    private readonly StatisticsService _statisticsService;
    private readonly TableService _tableService;

    private string _dailyRevenue = "0,00 zł";
    private string _todayOrders = "0";
    private string _averageOrder = "0,00 zł";
    private string _freeTables = "";
    private string _occupiedTables = "";
    private string _totalSeats = "";
    private string _activeStrategy = "";

    public StatisticsViewModel(
        CashierService cashierService,
        StatisticsService statisticsService,
        TableService tableService)
    {
        _cashierService = cashierService;
        _statisticsService = statisticsService;
        _tableService = tableService;

        Update();
    }

    public string DailyRevenue
    {
        get => _dailyRevenue;
        private set => SetProperty(ref _dailyRevenue, value);
    }

    public string TodayOrders
    {
        get => _todayOrders;
        private set => SetProperty(ref _todayOrders, value);
    }

    public string AverageOrder
    {
        get => _averageOrder;
        private set => SetProperty(ref _averageOrder, value);
    }

    public string FreeTables
    {
        get => _freeTables;
        private set => SetProperty(ref _freeTables, value);
    }

    public string OccupiedTables
    {
        get => _occupiedTables;
        private set => SetProperty(ref _occupiedTables, value);
    }

    public string TotalSeats
    {
        get => _totalSeats;
        private set => SetProperty(ref _totalSeats, value);
    }

    public string ActiveStrategy
    {
        get => _activeStrategy;
        private set => SetProperty(ref _activeStrategy, value);
    }

    public void Update()
    {
        DailyRevenue = $"{_cashierService.GetDailyRevenue():N2} zł";
        TodayOrders = _cashierService.GetTodayTransactionCount().ToString();
        AverageOrder = $"{_statisticsService.GetAverageOrderValue():N2} zł";

        FreeTables = $"Wolne: {_tableService.GetFreeTablesCount()}";
        OccupiedTables = $"Zajęte: {_tableService.GetOccupiedTablesCount()}";
        TotalSeats = $"Miejsca: {_tableService.GetOccupiedSeats()}/{_tableService.GetTotalSeats()}";

        var now = DateTime.Now;
        ActiveStrategy = (now.Hour >= 15 && now.Hour < 18) ? "Happy Hour aktywne!" : "";
    }
}


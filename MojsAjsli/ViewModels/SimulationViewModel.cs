using System.Collections.ObjectModel;
using System.Windows.Input;
using MojsAjsli.Services;

namespace MojsAjsli.ViewModels;

/// <summary>
/// ViewModel odpowiedzialny za symulację (SRP)
/// </summary>
public class SimulationViewModel : BaseViewModel
{
    private readonly SimulationService _simulationService;
    private readonly Action<string> _addNotification;

    private int _duration = 60;
    private int _speed = 500;
    private string _status = "Gotowa do uruchomienia";
    private bool _canRun = true;
    private bool _canStop;

    // Parametry
    private int _minArrivalInterval;
    private int _maxArrivalInterval;
    private int _minGroupSize;
    private int _maxGroupSize;
    private int _minWaitTime;
    private int _maxWaitTime;
    private int _maxConcurrentOrders;

    // Wyniki
    private string _servedGuests = "0";
    private string _lostGuests = "0";
    private string _totalRevenue = "0,00 zł";
    private string _serviceRate = "0%";
    private string _topDish = "-";

    public SimulationViewModel(SimulationService simulationService, Action<string> addNotification)
    {
        _simulationService = simulationService;
        _addNotification = addNotification;

        RunCommand = new RelayCommand(Run, () => CanRun);
        StopCommand = new RelayCommand(Stop, () => CanStop);
        ApplyParamsCommand = new RelayCommand(ApplyParams);

        LoadCurrentParams();
    }

    public int Duration
    {
        get => _duration;
        set => SetProperty(ref _duration, value);
    }

    public int Speed
    {
        get => _speed;
        set => SetProperty(ref _speed, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public bool CanRun
    {
        get => _canRun;
        set => SetProperty(ref _canRun, value);
    }

    public bool CanStop
    {
        get => _canStop;
        set => SetProperty(ref _canStop, value);
    }

    // Parametry
    public int MinArrivalInterval { get => _minArrivalInterval; set => SetProperty(ref _minArrivalInterval, value); }
    public int MaxArrivalInterval { get => _maxArrivalInterval; set => SetProperty(ref _maxArrivalInterval, value); }
    public int MinGroupSize { get => _minGroupSize; set => SetProperty(ref _minGroupSize, value); }
    public int MaxGroupSize { get => _maxGroupSize; set => SetProperty(ref _maxGroupSize, value); }
    public int MinWaitTime { get => _minWaitTime; set => SetProperty(ref _minWaitTime, value); }
    public int MaxWaitTime { get => _maxWaitTime; set => SetProperty(ref _maxWaitTime, value); }
    public int MaxConcurrentOrders { get => _maxConcurrentOrders; set => SetProperty(ref _maxConcurrentOrders, value); }

    // Wyniki
    public string ServedGuests { get => _servedGuests; set => SetProperty(ref _servedGuests, value); }
    public string LostGuests { get => _lostGuests; set => SetProperty(ref _lostGuests, value); }
    public string TotalRevenue { get => _totalRevenue; set => SetProperty(ref _totalRevenue, value); }
    public string ServiceRate { get => _serviceRate; set => SetProperty(ref _serviceRate, value); }
    public string TopDish { get => _topDish; set => SetProperty(ref _topDish, value); }

    public ObservableCollection<string> Log => _simulationService.SimulationLog;
    public ObservableCollection<SimulationOrder> KitchenQueue => _simulationService.KitchenQueue;
    public ObservableCollection<SimulationOrder> PreparingOrders => _simulationService.PreparingOrders;
    public ObservableCollection<SimulationOrder> ReadyOrders => _simulationService.ReadyOrders;
    public ObservableCollection<SimulationOrder> DeliveredOrders => _simulationService.DeliveredOrders;

    public ICommand RunCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand ApplyParamsCommand { get; }

    public event Action? OnSimulationStarted;
    public event Action? OnSimulationStopped;

    private void LoadCurrentParams()
    {
        MinArrivalInterval = _simulationService.MinArrivalInterval;
        MaxArrivalInterval = _simulationService.MaxArrivalInterval;
        MinGroupSize = _simulationService.MinGroupSize;
        MaxGroupSize = _simulationService.MaxGroupSize;
        MinWaitTime = _simulationService.MinWaitTime;
        MaxWaitTime = _simulationService.MaxWaitTime;
        MaxConcurrentOrders = _simulationService.MaxConcurrentOrders;
    }

    public void Run()
    {
        if (Duration <= 0) return;

        _simulationService.StartSimulation(Duration, Speed);

        CanRun = false;
        CanStop = true;
        Status = "Uruchamianie symulacji...";

        _addNotification("Symulacja uruchomiona w trybie czasu rzeczywistego");
        OnSimulationStarted?.Invoke();
    }

    public void Stop()
    {
        _simulationService.StopSimulation();

        CanRun = true;
        CanStop = false;

        var result = _simulationService.GetCurrentResults();
        Status = $"Zatrzymana - obsłużono {result.ServedGuests} gości, utracono {result.LostGuests}";

        _addNotification("Symulacja zatrzymana przez użytkownika");
        OnSimulationStopped?.Invoke();
    }

    public void UpdateResults()
    {
        var result = _simulationService.GetCurrentResults();

        ServedGuests = result.ServedGuests.ToString();
        LostGuests = result.LostGuests.ToString();
        TotalRevenue = $"{result.TotalRevenue:N2} zł";
        ServiceRate = $"{result.ServiceRate:N1}%";

        if (result.OrderedDishes.Any())
        {
            var topDish = result.OrderedDishes.OrderByDescending(x => x.Value).First();
            TopDish = $"{topDish.Key} ({topDish.Value} szt.)";
        }
        else
        {
            TopDish = "-";
        }

        if (result.AcceptingNewGuests)
        {
            Status = $"Trwa... Czas: {_simulationService.CurrentTimeFormatted} (przyjmowanie gości)";
        }
        else if (result.IsRunning)
        {
            Status = $"Trwa... Czas: {_simulationService.CurrentTimeFormatted} (finalizacja)";
        }
        else
        {
            Status = "Zakończona";
            CanRun = true;
            CanStop = false;
            OnSimulationStopped?.Invoke();
        }
    }

    private void ApplyParams()
    {
        _simulationService.MinArrivalInterval = MinArrivalInterval;
        _simulationService.MaxArrivalInterval = MaxArrivalInterval;
        _simulationService.MinGroupSize = MinGroupSize;
        _simulationService.MaxGroupSize = MaxGroupSize;
        _simulationService.MinWaitTime = MinWaitTime;
        _simulationService.MaxWaitTime = MaxWaitTime;
        _simulationService.MaxConcurrentOrders = MaxConcurrentOrders;

        _addNotification("Parametry symulacji zaktualizowane");
        OnParamsApplied?.Invoke();
    }

    public event Action? OnParamsApplied;
}

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using MojsAjsli.Models;

namespace MojsAjsli.Services;

public class SimulationService : INotifyPropertyChanged
{
    private readonly TableService _tableService;
    private readonly MenuService _menuService;
    private readonly Random _random = new();
    private readonly DispatcherTimer _timer;
    
    private int _currentTime; // w minutach
    private int _simulationDuration; // czas trwania symulacji
    private bool _isRunning;
    private bool _acceptingNewGuests = true;
    private int _servedGuests;
    private int _lostGuests;
    private decimal _totalRevenue;
    private int _nextGuestArrival;
    private int _simulationSpeed = 1000; // milisekundy na minutę symulacji
    private int _returnedOrdersCount; // liczba zwróconych zamówień
    
    // Awaria kuchni - nieliniowość
    private int _kitchenBreakdownTime = 0; // Czas zakończenia awarii (0 = brak awarii)
    private int _normalMaxConcurrentOrders = 3; // Normalna przepustowość
    private bool _isKitchenBreakdown = false; // Czy trwa awaria
    private int _nextBreakdownCheck = 0; // Kiedy sprawdzić czy wystąpi awaria
    
    // Konfigurowalne parametry symulacji
    private int _minArrivalInterval = 5;
    private int _maxArrivalInterval = 25;
    private int _minGroupSize = 1;
    private int _maxGroupSize = 8;
    private int _minWaitTime = 5;
    private int _maxWaitTime = 15;
    private int _maxConcurrentOrders = 3;
    private int _returnChancePercent = 5; // Procent szansy na zwrot zamówienia
    
    private readonly List<SimulationGuestGroup> _waitingGroups = new();
    private readonly List<SimulationGuestGroup> _seatedGroups = new();
    private readonly ObservableCollection<SimulationOrder> _kitchenQueue = new();
    private readonly ObservableCollection<SimulationOrder> _preparingOrders = new();
    private readonly ObservableCollection<SimulationOrder> _readyOrders = new();
    private readonly ObservableCollection<SimulationOrder> _deliveredOrders = new();
    private readonly ObservableCollection<SimulationOrder> _returnedOrders = new(); // Zwrócone zamówienia z priorytetem
    private readonly ObservableCollection<string> _simulationLog = new();
    private readonly Dictionary<string, int> _orderedDishes = new();

    public int CurrentTime
    {
        get => _currentTime;
        private set { _currentTime = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentTimeFormatted)); }
    }
    
    public string CurrentTimeFormatted => $"{_currentTime / 60:D2}:{_currentTime % 60:D2}";
    
    public bool IsRunning
    {
        get => _isRunning;
        private set { _isRunning = value; OnPropertyChanged(); }
    }
    
    public int ServedGuests
    {
        get => _servedGuests;
        private set { _servedGuests = value; OnPropertyChanged(); }
    }
    
    public int LostGuests
    {
        get => _lostGuests;
        private set { _lostGuests = value; OnPropertyChanged(); }
    }
    
    public decimal TotalRevenue
    {
        get => _totalRevenue;
        private set { _totalRevenue = value; OnPropertyChanged(); }
    }
    
    public int WaitingGroupsCount => _waitingGroups.Count;
    public int SeatedGroupsCount => _seatedGroups.Count;
    public int KitchenQueueCount => _kitchenQueue.Count;
    public int PreparingCount => _preparingOrders.Count;
    
    public ObservableCollection<string> SimulationLog => _simulationLog;
    public Dictionary<string, int> OrderedDishes => _orderedDishes;
    
    // Publiczne kolekcje do wyświetlania w UI
    public ObservableCollection<SimulationOrder> KitchenQueue => _kitchenQueue;
    public ObservableCollection<SimulationOrder> PreparingOrders => _preparingOrders;
    public ObservableCollection<SimulationOrder> ReadyOrders => _readyOrders;
    public ObservableCollection<SimulationOrder> DeliveredOrders => _deliveredOrders;
    public ObservableCollection<SimulationOrder> ReturnedOrders => _returnedOrders;

    public int MinArrivalInterval
    {
        get => _minArrivalInterval;
        set 
        { 
            _minArrivalInterval = Math.Max(1, value); 
            OnPropertyChanged();
            // Jeśli symulacja jest uruchomiona, przelicz czas następnego przybycia
            if (_isRunning && _acceptingNewGuests)
            {
                RecalculateNextArrival();
            }
        }
    }
    
    public int MaxArrivalInterval
    {
        get => _maxArrivalInterval;
        set 
        { 
            _maxArrivalInterval = Math.Max(_minArrivalInterval + 1, value); 
            OnPropertyChanged();
            // Jeśli symulacja jest uruchomiona, przelicz czas następnego przybycia
            if (_isRunning && _acceptingNewGuests)
            {
                RecalculateNextArrival();
            }
        }
    }
    
    public int MinGroupSize
    {
        get => _minGroupSize;
        set { _minGroupSize = Math.Max(1, value); OnPropertyChanged(); }
    }
    
    public int MaxGroupSize
    {
        get => _maxGroupSize;
        set { _maxGroupSize = Math.Max(_minGroupSize + 1, value); OnPropertyChanged(); }
    }
    
    public int MinWaitTime
    {
        get => _minWaitTime;
        set { _minWaitTime = Math.Max(1, value); OnPropertyChanged(); }
    }
    
    public int MaxWaitTime
    {
        get => _maxWaitTime;
        set { _maxWaitTime = Math.Max(_minWaitTime + 1, value); OnPropertyChanged(); }
    }
    
    public int MaxConcurrentOrders
    {
        get => _maxConcurrentOrders;
        set { _maxConcurrentOrders = Math.Max(1, value); OnPropertyChanged(); }
    }
    
    public int ReturnedOrdersCount
    {
        get => _returnedOrdersCount;
        private set { _returnedOrdersCount = value; OnPropertyChanged(); }
    }
    
    public int ReturnChancePercent
    {
        get => _returnChancePercent;
        set { _returnChancePercent = Math.Clamp(value, 0, 100); OnPropertyChanged(); }
    }
    
    public int SimulationSpeed
    {
        get => _simulationSpeed;
        set 
        { 
            _simulationSpeed = Math.Max(10, value); 
            OnPropertyChanged();
            // Aktualizuj timer jeśli jest uruchomiony
            if (_timer.IsEnabled)
            {
                _timer.Interval = TimeSpan.FromMilliseconds(_simulationSpeed);
            }
        }
    }
    
    public SimulationService(TableService tableService, MenuService menuService)
    {
        _tableService = tableService;
        _menuService = menuService;
        
        _timer = new DispatcherTimer();
        _timer.Tick += Timer_Tick;
    }

    public void StartSimulation(int durationMinutes, int speedMilliseconds = 1000)
    {
        Reset();
        _simulationDuration = durationMinutes;
        _simulationSpeed = speedMilliseconds;
        _acceptingNewGuests = true;
        IsRunning = true;
        
        _nextGuestArrival = GetNextArrivalTime();
        _normalMaxConcurrentOrders = _maxConcurrentOrders; // Zapamiętaj normalną przepustowość
        _nextBreakdownCheck = _currentTime + _random.Next(20, 41); // Pierwsza awaria może wystąpić po 20-40 minutach
        
        AddLog($"=== Rozpoczęcie symulacji ({durationMinutes} minut) ===");
        AddLog($"Prędkość: 1 minuta symulacji = {speedMilliseconds}ms");
        AddLog($"System wykrywania awarii uruchomiony");
        
        _timer.Interval = TimeSpan.FromMilliseconds(_simulationSpeed);
        _timer.Start();
    }

    public void StopSimulation()
    {
        _timer.Stop();
        IsRunning = false;
        _acceptingNewGuests = false;
        
        AddLog($"=== Symulacja zatrzymana ===");
        AddLog($"Obsłużono gości: {_servedGuests}");
        AddLog($"Utracono gości: {_lostGuests}");
        AddLog($"Całkowity przychód: {_totalRevenue:N2} zł");
    }

    public void ChangeSpeed(int speedMilliseconds)
    {
        _simulationSpeed = speedMilliseconds;
        if (_timer.IsEnabled)
        {
            _timer.Interval = TimeSpan.FromMilliseconds(_simulationSpeed);
        }
        AddLog($"Zmieniono prędkość symulacji: 1 min = {speedMilliseconds}ms");
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        // Sprawdź czy minął czas trwania symulacji
        if (_acceptingNewGuests && _currentTime >= _simulationDuration)
        {
            _acceptingNewGuests = false;
            AddLog($"=== Czas symulacji minął - przestano przyjmować nowych gości ===");
            AddLog($"Obsługiwanie pozostałych gości...");
        }
        
        // System awarii kuchni - nieliniowość
        CheckKitchenBreakdown();
        
        // Przybycie nowych gości (tylko jeśli akceptujemy)
        if (_acceptingNewGuests && _currentTime >= _nextGuestArrival)
        {
            SpawnGuestGroup();
            _nextGuestArrival = _currentTime + GetNextArrivalTime();
        }
        
        // Przetwarzanie czekających gości
        ProcessWaitingGroups();
        
        // Przetwarzanie usadzonych gości
        ProcessSeatedGroups();
        
        // Przetwarzanie kuchni
        ProcessKitchen();
        
        // Przetwarzanie przygotowywanych zamówień
        ProcessPreparingOrders();
        
        CurrentTime++;
        UpdateCounts();
        
        // Sprawdź czy wszyscy goście zostali obsłużeni
        if (!_acceptingNewGuests && _waitingGroups.Count == 0 && _seatedGroups.Count == 0)
        {
            _timer.Stop();
            IsRunning = false;
            AddLog($"=== Symulacja zakończona - wszyscy goście obsłużeni ===");
            AddLog($"Obsłużono gości: {_servedGuests}");
            AddLog($"Utracono gości: {_lostGuests}");
            AddLog($"Reklamacje: {_returnedOrdersCount}");
            AddLog($"Całkowity przychód: {_totalRevenue:N2} zł");
            AddLog($"Całkowity czas: {CurrentTimeFormatted}");
        }
    }

    public SimulationResult GetCurrentResults()
    {
        return new SimulationResult
        {
            ServedGuests = _servedGuests,
            LostGuests = _lostGuests,
            TotalRevenue = _totalRevenue,
            DurationMinutes = _currentTime,
            OrderedDishes = new Dictionary<string, int>(_orderedDishes),
            Log = _simulationLog.ToList(),
            IsRunning = _isRunning,
            AcceptingNewGuests = _acceptingNewGuests
        };
    }

    private void Reset()
    {
        CurrentTime = 0;
        ServedGuests = 0;
        LostGuests = 0;
        TotalRevenue = 0;
        ReturnedOrdersCount = 0; // Reset licznika zwrotów
        _waitingGroups.Clear();
        _seatedGroups.Clear();
        _kitchenQueue.Clear();
        _preparingOrders.Clear();
        _readyOrders.Clear();
        _deliveredOrders.Clear();
        _returnedOrders.Clear();
        _simulationLog.Clear();
        _orderedDishes.Clear();
        
        // Reset stolików
        foreach (var table in _tableService.Tables)
        {
            table.Status = TableStatus.Free;
            table.OccupiedSeats = 0;
        }
    }

    private int GetNextArrivalTime()
    {
        // Nierównomierne rozkładanie - więcej szans na krótsze przerwy
        // Rozkład wykładniczy przybliżony
        double u = _random.NextDouble();
        int baseTime = MinArrivalInterval;
        int maxTime = MaxArrivalInterval;
        
        // Nierównomierny rozkład - bardziej prawdopodobne krótsze interwały
        double skewed = Math.Pow(u, 0.6); // skośność w kierunku mniejszych wartości
        return baseTime + (int)(skewed * (maxTime - baseTime));
    }
    
    private void RecalculateNextArrival()
    {
        // Jeśli następne przybycie jest w przyszłości, dostosuj je do nowych parametrów
        if (_nextGuestArrival > _currentTime)
        {
            // Oblicz nowy interwał i ustaw od bieżącego czasu
            int newInterval = GetNextArrivalTime();
            _nextGuestArrival = _currentTime + newInterval;
            AddLog($"[{CurrentTimeFormatted}] Zaktualizowano czas następnego przybycia (interwał: {MinArrivalInterval}-{MaxArrivalInterval} min)");
        }
    }

    private void SpawnGuestGroup()
    {
        int groupSize = _random.Next(MinGroupSize, MaxGroupSize + 1); // 1-8 osób
        var group = new SimulationGuestGroup
        {
            Size = groupSize,
            ArrivalTime = _currentTime,
            MaxWaitTime = _random.Next(MinWaitTime, MaxWaitTime + 1), // 5-15 minut czekania
            State = GuestState.Waiting
        };
        
        _waitingGroups.Add(group);
        AddLog($"[{CurrentTimeFormatted}] Przybyła grupa {groupSize} gości");
    }

    private void ProcessWaitingGroups()
    {
        var groupsToRemove = new List<SimulationGuestGroup>();
        
        foreach (var group in _waitingGroups.ToList())
        {
            // Sprawdź czy przekroczono czas oczekiwania
            if (_currentTime - group.ArrivalTime >= group.MaxWaitTime)
            {
                LostGuests += group.Size;
                groupsToRemove.Add(group);
                AddLog($"[{CurrentTimeFormatted}] Grupa {group.Size} gości odeszła (zbyt długie oczekiwanie)");
                continue;
            }
            
            // Próba usadzenia - szukamy od stolika 1
            var suitableTable = _tableService.Tables
                .Where(t => t.Status == TableStatus.Free && t.Seats >= group.Size)
                .OrderBy(t => t.Number)
                .FirstOrDefault();
            
            if (suitableTable != null)
            {
                suitableTable.Status = TableStatus.Occupied;
                suitableTable.OccupiedSeats = group.Size;
                
                group.TableNumber = suitableTable.Number;
                group.State = GuestState.Seated;
                group.SeatedTime = _currentTime;
                group.OrderTime = _currentTime + _random.Next(5, 16); // 5-15 minut po usadzeniu
                
                _seatedGroups.Add(group);
                groupsToRemove.Add(group);
                
                AddLog($"[{CurrentTimeFormatted}] Grupa {group.Size} gości usadzona przy stoliku {suitableTable.Number}");
            }
        }
        
        foreach (var group in groupsToRemove)
        {
            _waitingGroups.Remove(group);
        }
    }

    private void ProcessSeatedGroups()
    {
        var groupsToRemove = new List<SimulationGuestGroup>();
        
        foreach (var group in _seatedGroups.ToList())
        {
            switch (group.State)
            {
                case GuestState.Seated:
                    // Czekanie na złożenie zamówienia
                    if (_currentTime >= group.OrderTime)
                    {
                        PlaceOrder(group);
                        group.State = GuestState.WaitingForFood;
                    }
                    break;
                    
                case GuestState.Eating:
                    // Sprawdź czy skończyli jeść
                    if (_currentTime >= group.EatingEndTime)
                    {
                        group.State = GuestState.Paying;
                        group.PaymentEndTime = _currentTime + _random.Next(3, 11); // 3-10 minut płacenia
                    }
                    break;
                    
                case GuestState.Paying:
                    // Sprawdź czy zapłacili
                    if (_currentTime >= group.PaymentEndTime)
                    {
                        // Płatność
                        TotalRevenue += group.TotalBill;
                        ServedGuests += group.Size;
                        
                        // Stolik do sprzątania
                        var table = _tableService.GetTable(group.TableNumber);
                        if (table != null)
                        {
                            table.Status = TableStatus.NeedsCleaning;
                            group.CleaningEndTime = _currentTime + 5; // 5 minut sprzątania
                        }
                        
                        group.State = GuestState.Left;
                        AddLog($"[{CurrentTimeFormatted}] Grupa ze stolika {group.TableNumber} zapłaciła {group.TotalBill:N2} zł i wychodzi");
                    }
                    break;
                    
                case GuestState.Left:
                    // Sprzątanie stolika
                    if (_currentTime >= group.CleaningEndTime)
                    {
                        var table = _tableService.GetTable(group.TableNumber);
                        if (table != null)
                        {
                            table.Status = TableStatus.Free;
                            table.OccupiedSeats = 0;
                        }
                        groupsToRemove.Add(group);
                        AddLog($"[{CurrentTimeFormatted}] Stolik {group.TableNumber} posprzątany i gotowy");
                    }
                    break;
            }
        }
        
        foreach (var group in groupsToRemove)
        {
            _seatedGroups.Remove(group);
        }
    }

    private void PlaceOrder(SimulationGuestGroup group)
    {
        var order = new SimulationOrder
        {
            Group = group,
            TableNumber = group.TableNumber,
            OrderedTime = _currentTime
        };
        
        var allItems = _menuService.GetAllItems();
        
        // Każda osoba zamawia
        for (int i = 0; i < group.Size; i++)
        {
            // 50% szans na każdą kategorię
            if (_random.NextDouble() < 0.5)
                AddRandomDishFromCategory(order, allItems, DishCategory.Appetizer);
            
            if (_random.NextDouble() < 0.5)
                AddRandomDishFromCategory(order, allItems, DishCategory.MainCourse);
            
            if (_random.NextDouble() < 0.5)
                AddRandomDishFromCategory(order, allItems, DishCategory.Dessert);
            
            if (_random.NextDouble() < 0.5)
                AddRandomDishFromCategory(order, allItems, DishCategory.Drink);
            
            if (_random.NextDouble() < 0.5)
                AddRandomDishFromCategory(order, allItems, DishCategory.Vegetarian);
        }
        
        // Jeśli nic nie zamówili, dodaj przynajmniej jedno danie główne
        if (order.Items.Count == 0)
        {
            AddRandomDishFromCategory(order, allItems, DishCategory.MainCourse);
        }
        
        // Oblicz czas przygotowania (max z wszystkich dań)
        order.PreparationTime = order.Items.Max(i => i.PreparationTimeMinutes);
        order.TotalPrice = order.Items.Sum(i => i.BasePrice);
        group.TotalBill = order.TotalPrice;
        
        _kitchenQueue.Add(order);
        AddLog($"[{CurrentTimeFormatted}] Stolik {group.TableNumber} złożył zamówienie ({order.Items.Count} pozycji, {order.TotalPrice:N2} zł)");
    }

    private void AddRandomDishFromCategory(SimulationOrder order, List<MenuItem> allItems, DishCategory category)
    {
        var categoryItems = allItems.Where(i => i.Category == category).ToList();
        if (categoryItems.Any())
        {
            var item = categoryItems[_random.Next(categoryItems.Count)];
            order.Items.Add(item);
            
            // Zliczanie popularności
            if (_orderedDishes.ContainsKey(item.Name))
                _orderedDishes[item.Name]++;
            else
                _orderedDishes[item.Name] = 1;
        }
    }

    private void ProcessKitchen()
    {
        // PRIORYTET: Najpierw zwrócone zamówienia (reklamacje)
        while (_preparingOrders.Count < MaxConcurrentOrders && _returnedOrders.Any())
        {
            var returnedOrder = _returnedOrders[0];
            _returnedOrders.RemoveAt(0);
            
            // Czas przygotowania dla zwróconego zamówienia - połowa normalnego czasu (szybkie naprawienie)
            returnedOrder.PreparationStartTime = _currentTime;
            returnedOrder.PreparationEndTime = _currentTime + Math.Max(1, returnedOrder.PreparationTime / 2);
            returnedOrder.DeliveryTime = 0; // Reset czasu dostawy
            returnedOrder.IsReturned = false; // Oznacz jako ponownie przygotowane
            _preparingOrders.Add(returnedOrder);
            
            AddLog($"[{CurrentTimeFormatted}] 🔄 PRIORYTET: Kuchnia naprawia reklamowane zamówienie dla stolika {returnedOrder.TableNumber}");
        }
        
        // Następnie normalne zamówienia
        while (_preparingOrders.Count < MaxConcurrentOrders && _kitchenQueue.Any())
        {
            var order = _kitchenQueue[0];
            _kitchenQueue.RemoveAt(0);
            
            order.PreparationStartTime = _currentTime;
            order.PreparationEndTime = _currentTime + order.PreparationTime;
            _preparingOrders.Add(order);
            
            AddLog($"[{CurrentTimeFormatted}] Kuchnia rozpoczęła przygotowanie zamówienia dla stolika {order.TableNumber}");
        }
    }

    private void ProcessPreparingOrders()
    {
        var completedOrders = new List<SimulationOrder>();
        
        foreach (var order in _preparingOrders.ToList())
        {
            if (_currentTime >= order.PreparationEndTime)
            {
                completedOrders.Add(order);
                AddLog($"[{CurrentTimeFormatted}] Zamówienie dla stolika {order.TableNumber} gotowe do wydania");
            }
        }
        
        foreach (var order in completedOrders)
        {
            _preparingOrders.Remove(order);
            _readyOrders.Add(order);
        }
        
        // Przetwarzanie zamówień gotowych do wydania (automatyczne dostarczanie po 1-2 minutach)
        ProcessReadyOrders();
        
        // Przetwarzanie zamówień dostarczonych
        ProcessDeliveredOrders();
    }
    
    private static readonly string[] ReturnReasons = 
    {
        "zimna zupa",
        "włos w jedzeniu",
        "za długie oczekiwanie",
        "pomylone zamówienie",
        "niedogotowane danie",
        "przypalone jedzenie",
        "brak składnika"
    };
    
    private void ProcessReadyOrders()
    {
        var ordersToDeliver = new List<SimulationOrder>();
        
        foreach (var order in _readyOrders.ToList())
        {
            // Automatyczne dostarczenie po 1-2 minutach od gotowości
            if (order.DeliveryTime == 0)
            {
                order.DeliveryTime = _currentTime + _random.Next(1, 3);
            }
            
            if (_currentTime >= order.DeliveryTime)
            {
                ordersToDeliver.Add(order);
            }
        }
        
        foreach (var order in ordersToDeliver)
        {
            _readyOrders.Remove(order);
            _deliveredOrders.Add(order);
            
            // Sprawdź czy klient jest zadowolony (szansa na reklamację)
            // Zamówienia, które już były zwracane, nie mogą być ponownie zwrócone
            if (order.ReturnCount == 0 && _random.Next(100) < _returnChancePercent)
            {
                // Klient niezadowolony - zwrot zamówienia!
                HandleOrderReturn(order);
            }
            else
            {
                // Klient zadowolony - rozpoczyna jedzenie
                order.Group.State = GuestState.Eating;
                order.Group.EatingEndTime = _currentTime + _random.Next(5, 31);
                AddLog($"[{CurrentTimeFormatted}] Zamówienie dostarczone do stolika {order.TableNumber}");
            }
        }
    }
    
    private void HandleOrderReturn(SimulationOrder order)
    {
        // Losowy powód zwrotu
        var reason = ReturnReasons[_random.Next(ReturnReasons.Length)];
        order.ReturnReason = reason;
        order.IsReturned = true;
        order.ReturnCount++;
        
        // Usunięcie z dostarczonych
        _deliveredOrders.Remove(order);
        
        // Dodanie do kolejki zwrotów (priorytetowej)
        _returnedOrders.Add(order);
        
        // Aktualizacja stanu grupy - wracają do czekania na jedzenie
        order.Group.State = GuestState.WaitingForFood;
        
        // Aktualizacja statystyk
        ReturnedOrdersCount++;
        
        AddLog($"[{CurrentTimeFormatted}] ⚠️ REKLAMACJA! Stolik {order.TableNumber} zwrócił zamówienie - powód: {reason}");
        AddLog($"[{CurrentTimeFormatted}] 🔄 Zamówienie wraca do kuchni z priorytetem!");
    }
    
    private void ProcessDeliveredOrders()
    {
        var ordersToComplete = new List<SimulationOrder>();
        
        foreach (var order in _deliveredOrders.ToList())
        {
            // Usuń z listy dostarczonych gdy goście zaczną płacić
            if (order.Group.State == GuestState.Paying || order.Group.State == GuestState.Left)
            {
                ordersToComplete.Add(order);
            }
        }
        
        foreach (var order in ordersToComplete)
        {
            _deliveredOrders.Remove(order);
        }
    }

    private void UpdateCounts()
    {
        OnPropertyChanged(nameof(WaitingGroupsCount));
        OnPropertyChanged(nameof(SeatedGroupsCount));
        OnPropertyChanged(nameof(KitchenQueueCount));
        OnPropertyChanged(nameof(PreparingCount));
    }

    private void AddLog(string message)
    {
        _simulationLog.Add(message);
    }
    
    /// <summary>
    /// Sprawdza i obsługuje awarie kuchni - nieliniowość w systemie
    /// Awaria powoduje drastyczne zmniejszenie przepustowości (bottleneck)
    /// co prowadzi do wykładniczego wzrostu kolejki i sprzężenia zwrotnego
    /// </summary>
    private void CheckKitchenBreakdown()
    {
        // Sprawdź czy trwa awaria i czy już się skończyła
        if (_isKitchenBreakdown && _currentTime >= _kitchenBreakdownTime)
        {
            // Koniec awarii - przywróć normalną przepustowość
            _isKitchenBreakdown = false;
            _maxConcurrentOrders = _normalMaxConcurrentOrders;
            OnPropertyChanged(nameof(MaxConcurrentOrders));
            
            AddLog($"[{CurrentTimeFormatted}] ✅ NAPRAWA: Kuchnia wraca do pełnej sprawności! Przepustowość: {_maxConcurrentOrders}");
            AddLog($"[{CurrentTimeFormatted}] 📊 NIELINIOWOŚĆ: Obserwuj jak system stopniowo wraca do równowagi");
            
            // Zaplanuj następną potencjalną awarię za 30-60 minut
            _nextBreakdownCheck = _currentTime + _random.Next(30, 61);
        }
        // Sprawdź czy nie nastąpił czas sprawdzenia nowej awarii
        else if (!_isKitchenBreakdown && _currentTime >= _nextBreakdownCheck)
        {
            // 30% szansy na awarię w tym momencie
            if (_random.Next(100) < 30)
            {
                // Wystąpiła awaria!
                _isKitchenBreakdown = true;
                int breakdownDuration = _random.Next(10, 21); // 10-20 minut awarii
                _kitchenBreakdownTime = _currentTime + breakdownDuration;
                
                // Drastyczne zmniejszenie przepustowości - bottleneck!
                int reducedCapacity = Math.Max(1, _normalMaxConcurrentOrders / 3); // Zmniejsz do 1/3
                _maxConcurrentOrders = reducedCapacity;
                OnPropertyChanged(nameof(MaxConcurrentOrders));
                
                AddLog($"[{CurrentTimeFormatted}] 🔥 AWARIA KUCHNI! Piec przestał działać!");
                AddLog($"[{CurrentTimeFormatted}] ⚠️ Przepustowość spadła z {_normalMaxConcurrentOrders} do {reducedCapacity} zamówień!");
                AddLog($"[{CurrentTimeFormatted}] 🔧 Szacowany czas naprawy: {breakdownDuration} minut");
                AddLog($"[{CurrentTimeFormatted}] 📈 NIELINIOWOŚĆ: Kolejka zacznie rosnąć wykładniczo (bottleneck)!");
                
                // Ostrzeżenie o sprzężeniu zwrotnym
                if (_kitchenQueue.Count > 3)
                {
                    AddLog($"[{CurrentTimeFormatted}] ⚡ SPRZĘŻENIE ZWROTNE: Długa kolejka ({_kitchenQueue.Count}) + awaria = krytyczna sytuacja!");
                }
            }
            else
            {
                // Nie było awarii, sprawdź ponownie za 15-30 minut
                _nextBreakdownCheck = _currentTime + _random.Next(15, 31);
            }
        }
        
        // Monitoruj rosnącą kolejkę podczas awarii (efekt nieliniowy)
        if (_isKitchenBreakdown && _kitchenQueue.Count > 5 && _currentTime % 5 == 0)
        {
            int minutesRemaining = _kitchenBreakdownTime - _currentTime;
            AddLog($"[{CurrentTimeFormatted}] 📊 KRYTYCZNIE: Kolejka kuchni: {_kitchenQueue.Count} zamówień! Naprawa za {minutesRemaining} min");
            
            // Wzrost irytacji klientów - zwiększ szansę na reklamacje podczas awarii
            if (_kitchenQueue.Count > 8)
            {
                AddLog($"[{CurrentTimeFormatted}] 😡 SPRZĘŻENIE: Długie oczekiwanie zwiększa irytację klientów!");
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum GuestState
{
    Waiting,
    Seated,
    WaitingForFood,
    Eating,
    Paying,
    Left
}

public class SimulationGuestGroup
{
    public int Size { get; set; }
    public int ArrivalTime { get; set; }
    public int MaxWaitTime { get; set; }
    public int TableNumber { get; set; }
    public int SeatedTime { get; set; }
    public int OrderTime { get; set; }
    public int EatingEndTime { get; set; }
    public int PaymentEndTime { get; set; }
    public int CleaningEndTime { get; set; }
    public decimal TotalBill { get; set; }
    public GuestState State { get; set; }
}

public class SimulationOrder
{
    public SimulationGuestGroup Group { get; set; } = null!;
    public int TableNumber { get; set; }
    public int OrderedTime { get; set; }
    public int PreparationTime { get; set; }
    public int PreparationStartTime { get; set; }
    public int PreparationEndTime { get; set; }
    public int DeliveryTime { get; set; }
    public decimal TotalPrice { get; set; }
    public List<MenuItem> Items { get; set; } = new();
    public bool IsReturned { get; set; } // Czy zamówienie zostało zwrócone (reklamacja)
    public int ReturnCount { get; set; } // Ile razy zamówienie było zwracane
    public string? ReturnReason { get; set; } // Powód zwrotu
}

public class SimulationResult
{
    public int ServedGuests { get; set; }
    public int LostGuests { get; set; }
    public decimal TotalRevenue { get; set; }
    public int DurationMinutes { get; set; }
    public Dictionary<string, int> OrderedDishes { get; set; } = new();
    public List<string> Log { get; set; } = new();
    public bool IsRunning { get; set; }
    public bool AcceptingNewGuests { get; set; }
    
    public double ServiceRate => ServedGuests + LostGuests > 0 
        ? (double)ServedGuests / (ServedGuests + LostGuests) * 100 
        : 0;
}





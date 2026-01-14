# 🍽️ MojsAjsli - System Zarządzania Restauracją

## 📋 Opis Projektu

**MojsAjsli** to zaawansowany symulator restauracji w klimacie Star Wars (Kantyna Mos Eisley), zbudowany w technologii WPF (.NET 9.0). Projekt modeluje złożony, rzeczywisty problem zarządzania restauracją, składający się z wielu współzależnych podsystemów o nieliniowym charakterze.

---

## 🎯 Spełnienie Wymagań Projektowych

### 1. Problem Złożony o Nieliniowym Charakterze

Projekt modeluje **rzeczywisty problem złożony** - zarządzanie restauracją, który składa się z następujących **problemów składowych o nieliniowym charakterze**:

#### 🔄 Nieliniowość #1: System Kolejek i Bottleneck Kuchni

```
Przepustowość kuchni → Czas oczekiwania → Zadowolenie klientów → Reklamacje → Dodatkowe zamówienia → Większa kolejka
```

- **Awaria kuchni** (`CheckKitchenBreakdown()`) - losowe awarie redukują przepustowość do 1/3, powodując wykładniczy wzrost kolejki
- Sprzężenie zwrotne: długa kolejka + awaria = kaskadowy efekt negatywny
- System priorytetów: reklamowane zamówienia mają pierwszeństwo, co wpływa na pozostałe

#### 🔄 Nieliniowość #2: Dynamika Gości

```
Czas oczekiwania na stolik → Rezygnacja gości → Utrata przychodu → Wolniejsze stoliki → Dłuższe oczekiwanie
```

- Grupy gości mają **losowy czas cierpliwości** (5-15 min)
- Rozkład przybyć jest **nierównomierny** (skośny rozkład wykładniczy)
- Zajętość stolików wpływa na możliwość obsługi nowych gości

#### 🔄 Nieliniowość #3: System Reklamacji

```
Jakość obsługi → Szansa na reklamację → Powrót do kolejki z priorytetem → Wydłużenie kolejki → Pogorszenie jakości
```

- Losowe reklamacje (konfigurowalny procent)
- Reklamacje wracają do kuchni z **priorytetem** (przeskakują normalną kolejkę)
- Podczas awarii irytacja klientów rośnie, zwiększając ryzyko reklamacji

#### 🔄 Nieliniowość #4: Przepływ Zamówień (State Machine)

Stan zamówienia determinuje możliwe akcje - nieliniowa maszyna stanów:

```
Nowe → Przyjęte → W przygotowaniu → Gotowe → Dostarczone → Opłacone
                        ↓                         ↓
                   (nie można anulować)      Zwrócone → W przygotowaniu (priorytet)
```

---

### 2. Zasady SOLID

Projekt **ściśle przestrzega** wszystkich pięciu zasad SOLID:

#### S - Single Responsibility Principle (SRP)

| Klasa/Interfejs | Pojedyncza Odpowiedzialność |
|-----------------|----------------------------|
| `IMenuQueryService` | Tylko odczyt menu |
| `IDishFactory` | Tylko tworzenie dań |
| `TableService` | Tylko zarządzanie stolikami |
| `KitchenService` | Tylko obsługa kuchni |
| `WaiterService` | Tylko obsługa kelnerska |
| `CashierService` | Tylko płatności i wycena |
| `StatisticsService` | Tylko statystyki |
| `SimulationService` | Tylko logika symulacji |

#### O - Open/Closed Principle (OCP)

Rozszerzalność bez modyfikacji istniejącego kodu:

```csharp
// Nowe dodatki do dań - wystarczy dodać do enum i słownika
public enum DishExtra
{
    ExtraCheese, Bacon, SpicySauce, GlutenFree, ExtraPortion, VeganOption
    // Łatwo dodać nowe bez zmiany interfejsu
}

// Fabryka wykorzystuje słownik dekoratorów
_decoratorMap = new Dictionary<DishExtra, Func<IDish, IDish>>
{
    { DishExtra.ExtraCheese, dish => new ExtraCheeseDecorator(dish) },
    // Nowy dodatek = nowa linia, brak zmian w logice
};
```

#### L - Liskov Substitution Principle (LSP)

Wszystkie stany zamówienia (`IOrderState`) są wzajemnie zamienne:

```csharp
public interface IOrderState
{
    void Accept(Order order);
    void StartPreparing(Order order);
    void MarkReady(Order order);
    // ... każda implementacja zachowuje kontrakt
}
```

Każda strategia płatności/wyceny może zastąpić inną:

```csharp
public interface IPaymentStrategy { bool ProcessPayment(decimal amount); }
public interface IPricingStrategy { decimal CalculatePrice(Order order); }
```

#### I - Interface Segregation Principle (ISP)

Małe, wyspecjalizowane interfejsy zamiast jednego dużego:

```csharp
// Zamiast jednego IMenuService z wieloma metodami:
public interface IMenuQueryService      // Odczyt menu
public interface IDishFactory           // Tworzenie dań
public interface IMenuService : IMenuQueryService, IDishFactory  // Fasada
```

#### D - Dependency Inversion Principle (DIP)

Moduły wysokiego poziomu zależą od abstrakcji:

```csharp
public class TableService : ITableService { ... }
public class MenuService : IMenuService { ... }

// Wstrzykiwanie przez konstruktor:
public SimulationService(TableService tableService, MenuService menuService)
```

---

### 3. Wzorce Projektowe

Projekt wykorzystuje **7 wzorców projektowych** w sposób jakościowy i uzasadniony:

#### 🎨 Wzorzec Decorator (Strukturalny)

**Lokalizacja:** `Patterns/Decorator/`

**Zastosowanie:** Dynamiczne dodawanie dodatków do dań

```csharp
IDish burger = new Burger();                           // 25 zł
burger = new ExtraCheeseDecorator(burger);            // +3 zł
burger = new BaconDecorator(burger);                  // +5 zł
// Wynik: Burger z dodatkowym serem i bekonem = 33 zł
```

**Elementy:**
- `IDish` - interfejs komponentu
- `BaseDish`, `Burger`, `Pizza`, `Pasta` - konkretne komponenty
- `DishDecorator` - abstrakcyjny dekorator
- `ExtraCheeseDecorator`, `BaconDecorator`, `SpicySauceDecorator`, `GlutenFreeDecorator`, `ExtraPortionDecorator`, `VeganOptionDecorator` - konkretne dekoratory

---

#### 🔄 Wzorzec State (Behawioralny)

**Lokalizacja:** `Patterns/State/`

**Zastosowanie:** Zarządzanie cyklem życia zamówienia

```csharp
order.Accept();        // NewOrderState → AcceptedState
order.StartPreparing();// AcceptedState → PreparingState
order.MarkReady();     // PreparingState → ReadyState
order.Deliver();       // ReadyState → DeliveredState
order.Return();        // DeliveredState → ReturnedState (reklamacja)
order.Pay();           // DeliveredState → PaidState
```

**Stany:** `NewOrderState`, `AcceptedState`, `PreparingState`, `ReadyState`, `DeliveredState`, `ReturnedState`, `PaidState`, `CancelledState`

---

#### 🎯 Wzorzec Strategy (Behawioralny)

**Lokalizacja:** `Patterns/Strategy/`

**Zastosowanie:** Wymienne strategie płatności i wyceny

**Strategie płatności:**
```csharp
IPaymentStrategy cash = new CashPaymentStrategy();
IPaymentStrategy card = new CardPaymentStrategy();
IPaymentStrategy blik = new BlikPaymentStrategy();
IPaymentStrategy transfer = new BankTransferPaymentStrategy();
```

**Strategie wyceny (zniżki):**
```csharp
IPricingStrategy regular = new RegularPricingStrategy();     // 0%
IPricingStrategy happyHour = new HappyHourStrategy();        // 30% (15:00-18:00)
IPricingStrategy loyalty = new LoyaltyCardStrategy();        // 15%
IPricingStrategy group = new GroupDiscountStrategy();        // 10% (5+ osób)
IPricingStrategy student = new StudentDiscountStrategy();    // 20%
IPricingStrategy weekend = new WeekendStrategy();            // zniżka weekendowa
```

---

#### 🔔 Wzorzec Observer (Behawioralny)

**Lokalizacja:** `Patterns/Observer/`

**Zastosowanie:** Powiadamianie o zdarzeniach w restauracji

```csharp
public interface IObserver<T> { void Update(T data); }
public interface ISubject<T> 
{ 
    void Attach(IObserver<T> observer);
    void Detach(IObserver<T> observer);
    void Notify(T data);
}

// RestaurantNotificationSubject wysyła powiadomienia:
subject.NotifyNewOrder(orderId, tableNumber);
subject.NotifyOrderReady(orderId, tableNumber);
subject.NotifyOrderDelivered(orderId, tableNumber);
```

---

#### 🔗 Wzorzec Mediator (Behawioralny)

**Lokalizacja:** `Patterns/Mediator/`

**Zastosowanie:** Koordynacja komunikacji między serwisami restauracji

```csharp
public interface IRestaurantMediator
{
    void SendOrderToKitchen(Order order);
    void NotifyOrderReady(Order order);
    void NotifyOrderDelivered(Order order);
    void RequestBill(int tableNumber);
    void NotifyPaymentComplete(int tableNumber, decimal amount);
}

// Kolaboranci (Colleagues):
public interface IColleague
{
    void SetMediator(IRestaurantMediator mediator);
    void ReceiveNotification(string message, object? data);
}
```

**Kolaboranci:** `KitchenService`, `WaiterService`, `CashierService`

---

#### 📜 Wzorzec Memento (Behawioralny)

**Lokalizacja:** `Patterns/Memento/`

**Zastosowanie:** Undo/Redo dla edycji zamówień

```csharp
// Tworzenie migawki stanu:
OrderMemento memento = order.CreateMemento();

// Przywracanie stanu:
order.RestoreFromMemento(memento);

// Historia z Undo/Redo:
OrderHistory history = new OrderHistory();
history.SaveState(order);
history.Undo(order);
history.Redo(order);
```

---

#### 🔍 Wzorzec Iterator (Behawioralny)

**Lokalizacja:** `Patterns/Iterator/`

**Zastosowanie:** Iteracja po menu z różnymi filtrami

```csharp
MenuAggregate menu = new MenuAggregate();

// Różne iteratory:
IIterator<MenuItem> all = menu.CreateIterator();
IIterator<MenuItem> appetizers = menu.CreateCategoryIterator(DishCategory.Appetizer);
IIterator<MenuItem> cheap = menu.CreatePriceRangeIterator(10, 30);

while (iterator.HasNext())
{
    MenuItem item = iterator.Next();
    // ...
}
```

---

## 🏗️ Architektura Projektu

```
MojsAjsli/
├── Models/                          # Modele domenowe
│   ├── MenuItem.cs                  # Pozycja w menu
│   ├── Table.cs                     # Stolik (INotifyPropertyChanged)
│   └── Payment.cs                   # Płatność
│
├── Patterns/                        # Wzorce projektowe
│   ├── Decorator/                   # Dekoratory dań
│   │   ├── IDish.cs
│   │   ├── BaseDish.cs
│   │   └── DishDecorator.cs
│   ├── Iterator/                    # Iteratory menu
│   │   ├── IIterator.cs
│   │   └── MenuIterator.cs
│   ├── Mediator/                    # Mediator restauracji
│   │   ├── IColleague.cs
│   │   ├── IRestaurantMediator.cs
│   │   └── RestaurantMediator.cs
│   ├── Memento/                     # Historia zamówień
│   │   ├── OrderMemento.cs
│   │   └── OrderHistory.cs
│   ├── Observer/                    # Powiadomienia
│   │   ├── IObserver.cs
│   │   └── RestaurantNotificationSubject.cs
│   ├── State/                       # Stany zamówienia
│   │   ├── IOrderState.cs
│   │   ├── Order.cs
│   │   └── OrderStates.cs
│   └── Strategy/                    # Strategie płatności/wyceny
│       ├── IPaymentStrategy.cs
│       ├── IPricingStrategy.cs
│       └── PricingStrategies.cs
│
├── Services/                        # Serwisy aplikacji
│   ├── Interfaces/                  # Interfejsy (DIP)
│   │   ├── IMenuService.cs
│   │   ├── ITableService.cs
│   │   ├── Dishes/
│   │   │   ├── IDishFactory.cs
│   │   │   └── DishExtra.cs
│   │   └── Menu/
│   │       └── IMenuQueryService.cs
│   ├── Implementations/             # Implementacje
│   │   ├── Dishes/
│   │   │   └── DishFactory.cs
│   │   └── Menu/
│   │       └── MenuQueryService.cs
│   ├── TableService.cs              # Zarządzanie stolikami
│   ├── MenuService.cs               # Serwis menu (Singleton + Fasada)
│   ├── KitchenService.cs            # Kuchnia (IColleague)
│   ├── WaiterService.cs             # Kelner (IColleague)
│   ├── CashierService.cs            # Kasjer (IColleague)
│   ├── StatisticsService.cs         # Statystyki
│   └── SimulationService.cs         # ⭐ Główna logika symulacji
│
└── UI/                              # Warstwa prezentacji (MVVM)
    ├── ViewModels/
    │   ├── BaseViewModel.cs
    │   ├── MainViewModel.cs
    │   ├── SimulationViewModel.cs
    │   ├── KitchenViewModel.cs
    │   ├── OrderViewModel.cs
    │   ├── PaymentViewModel.cs
    │   └── ...
    ├── Views/
    │   └── MainWindow.xaml
    ├── Converters/
    ├── Formatters/
    ├── Presenters/
    └── Resources/
```

---

## ⚙️ Parametry Symulacji

| Parametr | Domyślna wartość | Opis |
|----------|------------------|------|
| `MinArrivalInterval` | 5 min | Minimalny czas między przybyciami grup |
| `MaxArrivalInterval` | 25 min | Maksymalny czas między przybyciami grup |
| `MinGroupSize` | 1 | Minimalna wielkość grupy gości |
| `MaxGroupSize` | 8 | Maksymalna wielkość grupy gości |
| `MinWaitTime` | 5 min | Minimalny czas oczekiwania na stolik |
| `MaxWaitTime` | 15 min | Maksymalny czas oczekiwania na stolik |
| `MaxConcurrentOrders` | 3 | Przepustowość kuchni |
| `ReturnChancePercent` | 5% | Szansa na reklamację zamówienia |
| `SimulationSpeed` | 1000 ms | Prędkość symulacji (ms na minutę) |

---

## 📊 Metryki Symulacji

- **Obsłużeni goście** - liczba gości, którzy ukończyli wizytę
- **Utraceni goście** - liczba gości, którzy odeszli (zbyt długie oczekiwanie)
- **Całkowity przychód** - suma zapłaconych rachunków
- **Reklamacje** - liczba zwróconych zamówień
- **Wskaźnik obsługi** - % obsłużonych vs utraconych gości
- **Popularność dań** - ranking najczęściej zamawianych pozycji

---

## 🚀 Uruchomienie

### Wymagania
- .NET 9.0 SDK
- Windows (WPF)

### Budowanie i uruchomienie

```bash
cd MojsAjsli
dotnet build
dotnet run
```

---

## 📝 Licencja

Projekt edukacyjny demonstrujący zastosowanie wzorców projektowych i zasad SOLID.

---

## 👨‍💻 Autor

Projekt wykonany jako demonstracja modelowania złożonego problemu z wykorzystaniem wzorców projektowych i zasad czystego kodu.


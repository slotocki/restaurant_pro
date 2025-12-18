using System.ComponentModel;
using System.Runtime.CompilerServices;
using MojsAjsli.Patterns.Decorator;
using MojsAjsli.Patterns.Memento;

namespace MojsAjsli.Patterns.State;

public class Order : INotifyPropertyChanged
{
    private static int _nextId = 1;
    
    public int Id { get; }
    public int TableNumber { get; set; }
    public List<IDish> Items { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? CompletedAt { get; set; }
    
    private IOrderState _state;
    public IOrderState State
    {
        get => _state;
        set
        {
            _state = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StateName));
        }
    }
    
    public string StateName => _state.StateName;
    public decimal TotalPrice => Items.Sum(i => i.GetPrice());
    public int EstimatedTime => Items.Sum(i => i.GetPreparationTime());

    public Order(int tableNumber)
    {
        Id = _nextId++;
        TableNumber = tableNumber;
        Items = new List<IDish>();
        CreatedAt = DateTime.Now;
        _state = new NewOrderState();
    }

    public void AddItem(IDish dish)
    {
        if (_state.CanModify)
        {
            Items.Add(dish);
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(TotalPrice));
            OnPropertyChanged(nameof(EstimatedTime));
        }
        else
        {
            throw new InvalidOperationException("Nie mozna modyfikowac zamowienia w stanie: " + _state.StateName);
        }
    }

    public void RemoveItem(IDish dish)
    {
        if (_state.CanModify)
        {
            Items.Remove(dish);
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(TotalPrice));
            OnPropertyChanged(nameof(EstimatedTime));
        }
        else
        {
            throw new InvalidOperationException("Nie mozna modyfikowac zamowienia w stanie: " + _state.StateName);
        }
    }

    public void Accept() => _state.Accept(this);
    public void StartPreparing() => _state.StartPreparing(this);
    public void MarkReady() => _state.MarkReady(this);
    public void Deliver() => _state.Deliver(this);
    public void Pay() => _state.Pay(this);
    public void Cancel() => _state.Cancel(this);

    public OrderMemento CreateMemento()
    {
        return new OrderMemento(Id, TableNumber, new List<IDish>(Items), _state);
    }

    public void RestoreFromMemento(OrderMemento memento)
    {
        Items = new List<IDish>(memento.Items);
        State = memento.State;
        OnPropertyChanged(nameof(Items));
        OnPropertyChanged(nameof(TotalPrice));
        OnPropertyChanged(nameof(EstimatedTime));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}


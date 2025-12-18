using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MojsAjsli.Models;

public enum TableStatus
{
    Free,
    Occupied,
    Reserved,
    NeedsCleaning
}

public class Table : INotifyPropertyChanged
{
    private int _number;
    private int _seats;
    private TableStatus _status;
    private int _occupiedSeats;

    public int Number
    {
        get => _number;
        set { _number = value; OnPropertyChanged(); }
    }

    public int Seats
    {
        get => _seats;
        set { _seats = value; OnPropertyChanged(); }
    }

    public TableStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public int OccupiedSeats
    {
        get => _occupiedSeats;
        set { _occupiedSeats = value; OnPropertyChanged(); }
    }

    public Table(int number, int seats)
    {
        Number = number;
        Seats = seats;
        Status = TableStatus.Free;
        OccupiedSeats = 0;
    }

    public bool CanSeat(int guests)
    {
        return Status == TableStatus.Free && guests <= Seats;
    }

    public void Occupy(int guests)
    {
        if (CanSeat(guests))
        {
            Status = TableStatus.Occupied;
            OccupiedSeats = guests;
        }
    }

    public void Free()
    {
        Status = TableStatus.NeedsCleaning;
        OccupiedSeats = 0;
    }

    public void Clean()
    {
        Status = TableStatus.Free;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}


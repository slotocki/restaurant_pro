using MojsAjsli.Models;
using System.Collections.ObjectModel;

namespace MojsAjsli.Services;

public class TableService
{
    private readonly ObservableCollection<Table> _tables = new();

    public ObservableCollection<Table> Tables => _tables;

    public TableService()
    {
        InitializeTables();
    }

    private void InitializeTables()
    {
        _tables.Add(new Table(1, 2));
        _tables.Add(new Table(2, 2));
        _tables.Add(new Table(3, 4));
        _tables.Add(new Table(4, 4));
        _tables.Add(new Table(5, 4));
        _tables.Add(new Table(6, 6));
        _tables.Add(new Table(7, 6));
        _tables.Add(new Table(8, 8));
        _tables.Add(new Table(9, 8));
        _tables.Add(new Table(10, 4));
    }

    public Table? GetTable(int number)
    {
        return _tables.FirstOrDefault(t => t.Number == number);
    }

    public List<Table> GetFreeTables()
    {
        return _tables.Where(t => t.Status == TableStatus.Free).ToList();
    }

    public List<Table> GetOccupiedTables()
    {
        return _tables.Where(t => t.Status == TableStatus.Occupied).ToList();
    }

    public List<Table> GetTablesForGuests(int guestCount)
    {
        return _tables
            .Where(t => t.Status == TableStatus.Free && t.Seats >= guestCount)
            .OrderBy(t => t.Seats)
            .ToList();
    }

    public bool SeatGuests(int tableNumber, int guestCount)
    {
        var table = GetTable(tableNumber);
        if (table != null && table.CanSeat(guestCount))
        {
            table.Occupy(guestCount);
            return true;
        }
        return false;
    }

    public void FreeTable(int tableNumber)
    {
        var table = GetTable(tableNumber);
        table?.Free();
    }

    public void CleanTable(int tableNumber)
    {
        var table = GetTable(tableNumber);
        table?.Clean();
    }

    public int GetFreeTablesCount() => _tables.Count(t => t.Status == TableStatus.Free);
    public int GetOccupiedTablesCount() => _tables.Count(t => t.Status == TableStatus.Occupied);
    public int GetTotalSeats() => _tables.Sum(t => t.Seats);
    public int GetOccupiedSeats() => _tables.Sum(t => t.OccupiedSeats);
}


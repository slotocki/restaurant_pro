using MojsAjsli.Models;
using System.Collections.ObjectModel;

namespace MojsAjsli.Services.Interfaces;

/// <summary>
/// Interfejs serwisu zarządzania stolikami - DIP (Dependency Inversion Principle)
/// </summary>
public interface ITableService
{
    ObservableCollection<Table> Tables { get; }
    
    Table? GetTable(int number);
    List<Table> GetFreeTables();
    List<Table> GetOccupiedTables();
    List<Table> GetTablesForGuests(int guestCount);
    
    bool SeatGuests(int tableNumber, int guestCount);
    void FreeTable(int tableNumber);
    void CleanTable(int tableNumber);
    
    int GetFreeTablesCount();
    int GetOccupiedTablesCount();
    int GetTotalSeats();
    int GetOccupiedSeats();
}


using MojsAjsli.Models;
using MojsAjsli.Services.Interfaces;

namespace MojsAjsli.UI.Presenters;

/// <summary>
/// Presenter do zarządzania stolikami - SRP (Single Responsibility Principle)
/// Odpowiada TYLKO za logikę biznesową związaną ze stolikami
/// </summary>
public class TableManagementPresenter
{
    private readonly ITableService _tableService;
    
    public event EventHandler<string>? NotificationRequested;
    public event EventHandler? TableStateChanged;

    public TableManagementPresenter(ITableService tableService)
    {
        _tableService = tableService ?? throw new ArgumentNullException(nameof(tableService));
    }

    public (bool Success, string Message) OccupyTable(int tableNumber, int guestCount)
    {
        var table = _tableService.GetTable(tableNumber);
        if (table == null)
        {
            return (false, "Stolik nie istnieje!");
        }

        if (_tableService.SeatGuests(tableNumber, guestCount))
        {
            var message = $"Stolik {tableNumber} zajęty przez {guestCount} gości";
            NotificationRequested?.Invoke(this, message);
            TableStateChanged?.Invoke(this, EventArgs.Empty);
            return (true, message);
        }

        return (false, $"Nie można posadzić {guestCount} gości przy tym stoliku!");
    }

    public (bool Success, string Message) FreeTable(int tableNumber)
    {
        var table = _tableService.GetTable(tableNumber);
        if (table == null)
        {
            return (false, "Stolik nie istnieje!");
        }

        _tableService.FreeTable(tableNumber);
        var message = $"Stolik {tableNumber} zwolniony";
        NotificationRequested?.Invoke(this, message);
        TableStateChanged?.Invoke(this, EventArgs.Empty);
        return (true, message);
    }

    public (bool Success, string Message) CleanTable(int tableNumber)
    {
        var table = _tableService.GetTable(tableNumber);
        if (table == null)
        {
            return (false, "Stolik nie istnieje!");
        }

        _tableService.CleanTable(tableNumber);
        var message = $"Stolik {tableNumber} wyczyszczony";
        NotificationRequested?.Invoke(this, message);
        TableStateChanged?.Invoke(this, EventArgs.Empty);
        return (true, message);
    }

    public Table? GetSelectedTable(int tableNumber) => _tableService.GetTable(tableNumber);
}

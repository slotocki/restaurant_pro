using System.Collections.ObjectModel;
using System.Windows.Input;
using MojsAjsli.Models;
using MojsAjsli.Services;

namespace MojsAjsli.UI.ViewModels;

/// <summary>
/// ViewModel odpowiedzialny za zarządzanie stolikami (SRP)
/// </summary>
public class TableViewModel : BaseViewModel
{
    private readonly TableService _tableService;
    private readonly Action<string> _addNotification;

    private Table? _selectedTable;
    private int _selectedGuestCount = 2;
    private string _selectedTableInfo = "";
    private string _tableStatus = "";

    public TableViewModel(TableService tableService, Action<string> addNotification)
    {
        _tableService = tableService;
        _addNotification = addNotification;

        OccupyTableCommand = new RelayCommand(OccupyTable, () => SelectedTable != null);
        FreeTableCommand = new RelayCommand(FreeTable, () => SelectedTable != null);
        CleanTableCommand = new RelayCommand(CleanTable, () => SelectedTable != null);
    }

    public ObservableCollection<Table> Tables => new(_tableService.Tables);

    public Table? SelectedTable
    {
        get => _selectedTable;
        set
        {
            if (SetProperty(ref _selectedTable, value))
            {
                UpdateTableInfo();
                OnTableSelected?.Invoke(value);
            }
        }
    }

    public int SelectedGuestCount
    {
        get => _selectedGuestCount;
        set => SetProperty(ref _selectedGuestCount, value);
    }

    public string SelectedTableInfo
    {
        get => _selectedTableInfo;
        private set => SetProperty(ref _selectedTableInfo, value);
    }

    public string TableStatus
    {
        get => _tableStatus;
        private set => SetProperty(ref _tableStatus, value);
    }

    public int FreeTablesCount => _tableService.GetFreeTablesCount();
    public int OccupiedTablesCount => _tableService.GetOccupiedTablesCount();
    public int OccupiedSeats => _tableService.GetOccupiedSeats();
    public int TotalSeats => _tableService.GetTotalSeats();

    public ICommand OccupyTableCommand { get; }
    public ICommand FreeTableCommand { get; }
    public ICommand CleanTableCommand { get; }

    public event Action<Table?>? OnTableSelected;
    public event Action? OnTableStateChanged;

    public void SelectTable(int tableNumber)
    {
        SelectedTable = _tableService.GetTable(tableNumber);
    }

    private void UpdateTableInfo()
    {
        if (_selectedTable != null)
        {
            SelectedTableInfo = $"Stolik {_selectedTable.Number} ({_selectedTable.Seats} miejsc)";
            TableStatus = $"Status: {GetTableStatusText(_selectedTable.Status)}";
        }
        else
        {
            SelectedTableInfo = "";
            TableStatus = "";
        }
    }

    private void OccupyTable()
    {
        if (_selectedTable == null) return;

        if (_tableService.SeatGuests(_selectedTable.Number, _selectedGuestCount))
        {
            _addNotification($"Stolik {_selectedTable.Number} zajęty przez {_selectedGuestCount} gości");
            UpdateTableInfo();
            NotifyStateChanged();
        }
    }

    private void FreeTable()
    {
        if (_selectedTable == null) return;

        _tableService.FreeTable(_selectedTable.Number);
        _addNotification($"Stolik {_selectedTable.Number} zwolniony");
        UpdateTableInfo();
        NotifyStateChanged();
    }

    private void CleanTable()
    {
        if (_selectedTable == null) return;

        _tableService.CleanTable(_selectedTable.Number);
        _addNotification($"Stolik {_selectedTable.Number} wyczyszczony");
        UpdateTableInfo();
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(FreeTablesCount));
        OnPropertyChanged(nameof(OccupiedTablesCount));
        OnPropertyChanged(nameof(OccupiedSeats));
        OnTableStateChanged?.Invoke();
    }

    private static string GetTableStatusText(Models.TableStatus status) => status switch
    {
        Models.TableStatus.Free => "Wolny",
        Models.TableStatus.Occupied => "Zajęty",
        Models.TableStatus.Reserved => "Zarezerwowany",
        Models.TableStatus.NeedsCleaning => "Do sprzątania",
        _ => "Nieznany"
    };
}

using System.Collections.ObjectModel;
using MojsAjsli.Models;
using MojsAjsli.Patterns.Decorator;
using MojsAjsli.Patterns.State;
using MojsAjsli.Services;
using MojsAjsli.Services.Interfaces;

namespace MojsAjsli.Presenters;

/// <summary>
/// Presenter do zarządzania zamówieniami - SRP (Single Responsibility Principle)
/// Odpowiada TYLKO za logikę biznesową związaną z zamówieniami
/// </summary>
public class OrderManagementPresenter
{
    private readonly IMenuService _menuService;
    private readonly WaiterService _waiterService;
    
    public event EventHandler<string>? NotificationRequested;
    public event EventHandler? OrderChanged;

    public OrderManagementPresenter(IMenuService menuService, WaiterService waiterService)
    {
        _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
        _waiterService = waiterService ?? throw new ArgumentNullException(nameof(waiterService));
    }

    public (bool Success, string Message) AddItemToOrder(Order order, MenuItem menuItem, 
        bool extraCheese, bool bacon, bool spicySauce, bool glutenFree, bool extraPortion)
    {
        if (order == null)
        {
            return (false, "Najpierw zajmij stolik!");
        }

        try
        {
            IDish dish = _menuService.CreateDishWithExtras(
                menuItem, extraCheese, bacon, spicySauce, glutenFree, extraPortion);

            _waiterService.AddItemToOrder(order, dish);
            
            var message = $"Dodano: {dish.GetDescription()} ({dish.GetPrice():N2} zł)";
            NotificationRequested?.Invoke(this, message);
            OrderChanged?.Invoke(this, EventArgs.Empty);
            
            return (true, message);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public (bool Success, string Message) RemoveItemFromOrder(Order order, IDish dish)
    {
        if (order == null)
        {
            return (false, "Brak aktywnego zamówienia!");
        }

        try
        {
            _waiterService.RemoveItemFromOrder(order, dish);
            var message = $"Usunięto: {dish.GetName()}";
            NotificationRequested?.Invoke(this, message);
            OrderChanged?.Invoke(this, EventArgs.Empty);
            return (true, message);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public (bool Success, string Message) SubmitOrder(Order order)
    {
        if (order == null || order.Items.Count == 0)
        {
            return (false, "Zamówienie jest puste!");
        }

        try
        {
            _waiterService.SubmitOrder(order);
            var message = $"Zamówienie #{order.Id} wysłane do kuchni";
            NotificationRequested?.Invoke(this, message);
            OrderChanged?.Invoke(this, EventArgs.Empty);
            return (true, message);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public (bool Success, string Message) UndoLastAction(Order order)
    {
        if (order == null) return (false, "Brak aktywnego zamówienia!");

        try
        {
            _waiterService.UndoLastAction(order);
            NotificationRequested?.Invoke(this, "Cofnięto ostatnią akcję (Memento)");
            OrderChanged?.Invoke(this, EventArgs.Empty);
            return (true, "Cofnięto");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public (bool Success, string Message) RedoAction(Order order)
    {
        if (order == null) return (false, "Brak aktywnego zamówienia!");

        try
        {
            _waiterService.RedoAction(order);
            NotificationRequested?.Invoke(this, "Powtórzono akcję (Memento)");
            OrderChanged?.Invoke(this, EventArgs.Empty);
            return (true, "Powtórzono");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public bool CanUndo(Order? order) => order != null && _waiterService.CanUndo(order);
    public bool CanRedo(Order? order) => order != null && _waiterService.CanRedo(order);
    public Order CreateOrder(int tableNumber) => _waiterService.CreateOrder(tableNumber);
}


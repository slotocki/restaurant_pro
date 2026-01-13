using System.Collections.ObjectModel;
using System.Windows.Input;
using MojsAjsli.Models;
using MojsAjsli.Patterns.Observer;
using MojsAjsli.Patterns.State;
using MojsAjsli.Services;

namespace MojsAjsli.UI.ViewModels;

/// <summary>
/// ViewModel odpowiedzialny za zarządzanie kuchnią (SRP)
/// </summary>
public class KitchenViewModel : BaseViewModel
{
    private readonly KitchenService _kitchenService;
    private readonly RestaurantNotificationSubject _notificationSubject;
    private readonly Action<string> _addNotification;

    private Order? _selectedQueueOrder;
    private Order? _selectedPreparingOrder;
    private string _queueCount = "";
    private string _preparingCount = "";
    private string _estimatedWaitTime = "";

    public KitchenViewModel(
        KitchenService kitchenService,
        RestaurantNotificationSubject notificationSubject,
        Action<string> addNotification)
    {
        _kitchenService = kitchenService;
        _notificationSubject = notificationSubject;
        _addNotification = addNotification;

        StartPreparingCommand = new RelayCommand<Order>(StartPreparing, order => order != null);
        MarkReadyCommand = new RelayCommand<Order>(MarkReady, order => order != null);

        UpdateStatus();
    }

    public ObservableCollection<Order> OrderQueue => _kitchenService.OrderQueue;
    public ObservableCollection<Order> PreparingOrders => _kitchenService.PreparingOrders;

    public Order? SelectedQueueOrder
    {
        get => _selectedQueueOrder;
        set => SetProperty(ref _selectedQueueOrder, value);
    }

    public Order? SelectedPreparingOrder
    {
        get => _selectedPreparingOrder;
        set => SetProperty(ref _selectedPreparingOrder, value);
    }

    public string QueueCount
    {
        get => _queueCount;
        private set => SetProperty(ref _queueCount, value);
    }

    public string PreparingCount
    {
        get => _preparingCount;
        private set => SetProperty(ref _preparingCount, value);
    }

    public string EstimatedWaitTime
    {
        get => _estimatedWaitTime;
        private set => SetProperty(ref _estimatedWaitTime, value);
    }

    public ICommand StartPreparingCommand { get; }
    public ICommand MarkReadyCommand { get; }

    public event Action<Order>? OnOrderReady;

    public void UpdateStatus()
    {
        QueueCount = $"W kolejce: {_kitchenService.GetQueueLength()} zamówień";
        PreparingCount = $"Przygotowywanych: {_kitchenService.GetPreparingCount()}";
        EstimatedWaitTime = $"Szacowany czas: {_kitchenService.EstimateWaitTime().TotalMinutes:N0} min";
    }

    private void StartPreparing(Order? order)
    {
        if (order == null) return;

        _kitchenService.StartPreparing(order);
        _addNotification($"Rozpoczęto przygotowanie zamówienia #{order.Id}");
        UpdateStatus();
    }

    private void MarkReady(Order? order)
    {
        if (order == null) return;

        _kitchenService.CompleteOrder(order);
        _notificationSubject.NotifyOrderReady(order.Id, order.TableNumber);
        
        OnOrderReady?.Invoke(order);
        UpdateStatus();
    }
}

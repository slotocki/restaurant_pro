using MojsAjsli.Patterns.Decorator;
using MojsAjsli.Services.Interfaces.Dishes;

namespace MojsAjsli.UI.ViewModels;

/// <summary>
/// ViewModel dla pojedynczej pozycji zamówienia
/// </summary>
public class OrderItemViewModel
{
    public string Name { get; }
    public string Description { get; }
    public decimal Price { get; }

    public OrderItemViewModel(IDish dish)
    {
        Name = dish.GetName();
        Description = dish.GetDescription();
        Price = dish.GetPrice();
    }
}

using MojsAjsli.Models;
using MojsAjsli.Patterns.Decorator;

namespace MojsAjsli.Services.Interfaces;

/// <summary>
/// Interfejs serwisu menu - DIP (Dependency Inversion Principle)
/// </summary>
public interface IMenuService
{
    List<MenuItem> GetAllItems();
    List<MenuItem> GetItemsByCategory(DishCategory category);
    List<MenuItem> GetItemsInPriceRange(decimal minPrice, decimal maxPrice);
    
    IDish CreateDish(MenuItem menuItem);
    IDish CreateDishWithExtras(MenuItem menuItem, bool extraCheese = false, bool bacon = false,
        bool spicySauce = false, bool glutenFree = false, bool extraPortion = false, bool veganOption = false);
    
    int MenuItemsCount { get; }
}


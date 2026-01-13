using MojsAjsli.Models;
using MojsAjsli.Patterns.Iterator;
using MojsAjsli.Services.Interfaces.Menu;

namespace MojsAjsli.Services.Implementations.Menu;

/// <summary>
/// Implementacja serwisu zapytań menu - SRP
/// Odpowiada TYLKO za pobieranie danych z menu
/// </summary>
public class MenuQueryService : IMenuQueryService
{
    private readonly MenuAggregate _menu;

    public MenuQueryService(MenuAggregate menu)
    {
        _menu = menu ?? throw new ArgumentNullException(nameof(menu));
    }

    public int MenuItemsCount => _menu.Count;

    public List<MenuItem> GetAllItems() => _menu.GetAllItems();

    public List<MenuItem> GetItemsByCategory(DishCategory category)
    {
        var result = new List<MenuItem>();
        var iterator = _menu.CreateCategoryIterator(category);
        while (iterator.HasNext())
            result.Add(iterator.Next());
        return result;
    }

    public List<MenuItem> GetItemsInPriceRange(decimal minPrice, decimal maxPrice)
    {
        var result = new List<MenuItem>();
        var iterator = _menu.CreatePriceRangeIterator(minPrice, maxPrice);
        while (iterator.HasNext())
            result.Add(iterator.Next());
        return result;
    }
}


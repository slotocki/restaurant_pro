using MojsAjsli.Models;

namespace MojsAjsli.Services.Interfaces.Menu;

/// <summary>
/// Serwis do odczytu menu - SRP (Single Responsibility Principle)
/// Odpowiada TYLKO za pobieranie danych z menu
/// </summary>
public interface IMenuQueryService
{
    List<MenuItem> GetAllItems();
    List<MenuItem> GetItemsByCategory(DishCategory category);
    List<MenuItem> GetItemsInPriceRange(decimal minPrice, decimal maxPrice);
    int MenuItemsCount { get; }
}


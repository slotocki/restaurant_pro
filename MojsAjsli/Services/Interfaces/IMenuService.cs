using MojsAjsli.Models;
using MojsAjsli.Patterns.Decorator;
using MojsAjsli.Services.Interfaces.Dishes;
using MojsAjsli.Services.Interfaces.Menu;

namespace MojsAjsli.Services.Interfaces;

/// <summary>
/// Interfejs fasady serwisu menu - ISP (Interface Segregation Principle)
/// Łączy mniejsze interfejsy dla wygody użycia
/// </summary>
public interface IMenuService : IMenuQueryService, IDishFactory
{
    /// <summary>
    /// Metoda zachowana dla kompatybilności wstecznej
    /// </summary>
    [Obsolete("Użyj CreateDishWithExtras z IEnumerable<DishExtra> zamiast parametrów bool")]
    IDish CreateDishWithExtras(MenuItem menuItem, bool extraCheese = false, bool bacon = false,
        bool spicySauce = false, bool glutenFree = false, bool extraPortion = false, bool veganOption = false);
}

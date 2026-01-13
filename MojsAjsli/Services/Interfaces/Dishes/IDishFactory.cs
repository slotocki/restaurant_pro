using MojsAjsli.Models;
using MojsAjsli.Patterns.Decorator;

namespace MojsAjsli.Services.Interfaces.Dishes;

/// <summary>
/// Fabryka dań - SRP (Single Responsibility Principle)
/// Odpowiada TYLKO za tworzenie dań z dodatkami
/// </summary>
public interface IDishFactory
{
    IDish CreateDish(MenuItem menuItem);
    IDish CreateDishWithExtras(MenuItem menuItem, IEnumerable<DishExtra> extras);
}


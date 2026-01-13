using MojsAjsli.Models;
using MojsAjsli.Patterns.Decorator;
using MojsAjsli.Services.Interfaces.Dishes;

namespace MojsAjsli.Services.Implementations.Dishes;

/// <summary>
/// Fabryka dań - SRP + OCP (Open/Closed Principle)
/// Nowe dodatki można dodawać przez rozszerzenie słownika dekoratorów
/// </summary>
public class DishFactory : IDishFactory
{
    private readonly IDictionary<DishExtra, Func<IDish, IDish>> _decoratorMap;

    public DishFactory()
    {
        _decoratorMap = new Dictionary<DishExtra, Func<IDish, IDish>>
        {
            { DishExtra.ExtraCheese, dish => new ExtraCheeseDecorator(dish) },
            { DishExtra.Bacon, dish => new BaconDecorator(dish) },
            { DishExtra.SpicySauce, dish => new SpicySauceDecorator(dish) },
            { DishExtra.GlutenFree, dish => new GlutenFreeDecorator(dish) },
            { DishExtra.ExtraPortion, dish => new ExtraPortionDecorator(dish) },
            { DishExtra.VeganOption, dish => new VeganOptionDecorator(dish) }
        };
    }

    public IDish CreateDish(MenuItem menuItem)
    {
        return new BaseDish(menuItem.Name, menuItem.Description, menuItem.BasePrice, menuItem.PreparationTimeMinutes);
    }

    public IDish CreateDishWithExtras(MenuItem menuItem, IEnumerable<DishExtra> extras)
    {
        IDish dish = CreateDish(menuItem);

        foreach (var extra in extras)
        {
            if (_decoratorMap.TryGetValue(extra, out var decorator))
            {
                dish = decorator(dish);
            }
        }

        return dish;
    }
}

using MojsAjsli.Models;
using MojsAjsli.Patterns.Decorator;
using MojsAjsli.Patterns.Iterator;
using MojsAjsli.Services.Implementations.Dishes;
using MojsAjsli.Services.Implementations.Menu;
using MojsAjsli.Services.Interfaces;
using MojsAjsli.Services.Interfaces.Dishes;
using MojsAjsli.Services.Interfaces.Menu;

namespace MojsAjsli.Services;

/// <summary>
/// Implementacja IMenuService - Fasada łącząca serwisy
/// Singleton - zapewnia pojedynczą instancję w aplikacji
/// DIP (Dependency Inversion Principle) - zależy od abstrakcji
/// </summary>
public class MenuService : IMenuService
{
    private static MenuService? _instance;
    private static readonly object _lock = new();
    
    private readonly IMenuQueryService _queryService;
    private readonly IDishFactory _dishFactory;
    private readonly MenuAggregate _menu = new();

    public static MenuService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new MenuService();
                }
            }
            return _instance;
        }
    }

    private MenuService()
    {
        InitializeMenu();
        _queryService = new MenuQueryService(_menu);
        _dishFactory = new DishFactory();
    }

    private void InitializeMenu()
    {
        // Przystawki - klimat pustyni
        _menu.AddItem(new MenuItem("Zupa Bantha", 15.00m, DishCategory.Appetizer, "Kremowa zupa z korzeni pustynnych", 5));
        _menu.AddItem(new MenuItem("Grillowane Dewbacki", 18.00m, DishCategory.Appetizer, "Pikantne kąski z grilla", 8));
        _menu.AddItem(new MenuItem("Kraykaty w Czosnku", 28.00m, DishCategory.Appetizer, "Smażone w aromatycznych przyprawach", 10));

        // Dania główne
        _menu.AddItem(new MenuItem("Burger Pustynnego Łowcy", 25.00m, DishCategory.MainCourse, "Soczysty burger z lokalnego mięsa", 15));
        _menu.AddItem(new MenuItem("Pizza Jawajska", 30.00m, DishCategory.MainCourse, "Pizza z pieczarkami i ziołami pustynnymi", 20));
        _menu.AddItem(new MenuItem("Makaron Tatuinowy", 28.00m, DishCategory.MainCourse, "Makaron z pikantnym sosem i warzywami", 15));
        _menu.AddItem(new MenuItem("Stek z Nerfa", 55.00m, DishCategory.MainCourse, "Grillowany stek z lokalnych hodowli", 25));
        _menu.AddItem(new MenuItem("Ryba Panna z Wydm", 35.00m, DishCategory.MainCourse, "Smażona w chrupiącej panierce", 18));

        // Wegetariańskie
        _menu.AddItem(new MenuItem("Sałatka Fermy Wilgoci", 22.00m, DishCategory.Vegetarian, "Świeże warzywa z lokalnych upraw", 8));
        _menu.AddItem(new MenuItem("Burger Zbieracza", 24.00m, DishCategory.Vegetarian, "Burger z warzywnym kotletem", 15));

        // Desery
        _menu.AddItem(new MenuItem("Słodkie Muffiny", 18.00m, DishCategory.Dessert, "Tradycyjny deser z korzeniami", 5));
        _menu.AddItem(new MenuItem("Lodowa Kula z Hoth", 15.00m, DishCategory.Dessert, "Zimny deser w upalne dni", 8));

        // Napoje
        _menu.AddItem(new MenuItem("Niebieskie Mleko", 8.00m, DishCategory.Drink, "Słynny napój z farm wilgoci", 2));
        _menu.AddItem(new MenuItem("Caf Coruscański", 10.00m, DishCategory.Drink, "Mocna kawa z zewnętrznych rubieży", 2));
        _menu.AddItem(new MenuItem("Sok z Owoców Endoru", 12.00m, DishCategory.Drink, "Egzotyczny napój leśny", 2));
    }

    #region IMenuQueryService - delegacja do _queryService

    public List<MenuItem> GetAllItems() => _queryService.GetAllItems();

    public List<MenuItem> GetItemsByCategory(DishCategory category) => _queryService.GetItemsByCategory(category);

    public List<MenuItem> GetItemsInPriceRange(decimal minPrice, decimal maxPrice) => 
        _queryService.GetItemsInPriceRange(minPrice, maxPrice);

    public int MenuItemsCount => _queryService.MenuItemsCount;

    #endregion

    #region IDishFactory - delegacja do _dishFactory

    public IDish CreateDish(MenuItem menuItem) => _dishFactory.CreateDish(menuItem);

    public IDish CreateDishWithExtras(MenuItem menuItem, IEnumerable<DishExtra> extras) => 
        _dishFactory.CreateDishWithExtras(menuItem, extras);

    #endregion

    #region Kompatybilność wsteczna

    
    public IDish CreateDishWithExtras(MenuItem menuItem, bool extraCheese = false, bool bacon = false,
        bool spicySauce = false, bool glutenFree = false, bool extraPortion = false, bool veganOption = false)
    {
        var extras = new List<DishExtra>();
        
        if (extraCheese) extras.Add(DishExtra.ExtraCheese);
        if (bacon) extras.Add(DishExtra.Bacon);
        if (spicySauce) extras.Add(DishExtra.SpicySauce);
        if (glutenFree) extras.Add(DishExtra.GlutenFree);
        if (extraPortion) extras.Add(DishExtra.ExtraPortion);
        if (veganOption) extras.Add(DishExtra.VeganOption);

        return CreateDishWithExtras(menuItem, extras);
    }

    #endregion
}

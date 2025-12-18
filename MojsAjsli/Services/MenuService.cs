using MojsAjsli.Models;
using MojsAjsli.Patterns.Decorator;
using MojsAjsli.Patterns.Iterator;

namespace MojsAjsli.Services;

public class MenuService
{
    private static MenuService? _instance;
    private static readonly object _lock = new();
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

    public IDish CreateDish(MenuItem menuItem)
    {
        return new BaseDish(menuItem.Name, menuItem.Description, menuItem.BasePrice, menuItem.PreparationTimeMinutes);
    }

    public IDish CreateDishWithExtras(MenuItem menuItem, bool extraCheese = false, bool bacon = false, 
        bool spicySauce = false, bool glutenFree = false, bool extraPortion = false, bool veganOption = false)
    {
        IDish dish = CreateDish(menuItem);

        if (extraCheese) dish = new ExtraCheeseDecorator(dish);
        if (bacon) dish = new BaconDecorator(dish);
        if (spicySauce) dish = new SpicySauceDecorator(dish);
        if (glutenFree) dish = new GlutenFreeDecorator(dish);
        if (extraPortion) dish = new ExtraPortionDecorator(dish);
        if (veganOption) dish = new VeganOptionDecorator(dish);

        return dish;
    }

    public int MenuItemsCount => _menu.Count;
}

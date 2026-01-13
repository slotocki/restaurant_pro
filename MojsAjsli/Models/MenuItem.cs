namespace MojsAjsli.Models;

public enum DishCategory
{
    Appetizer,
    MainCourse,
    Dessert,
    Drink,
    Vegetarian
}

public class MenuItem
{
    public string Name { get; set; }
    public decimal BasePrice { get; set; }
    public DishCategory Category { get; set; }
    public string Description { get; set; }
    public int PreparationTimeMinutes { get; set; }

    public MenuItem(string name, decimal basePrice, DishCategory category, string description = "", int prepTime = 10)
    {
        Name = name;
        BasePrice = basePrice;
        Category = category;
        Description = description;
        PreparationTimeMinutes = prepTime;
    }
}

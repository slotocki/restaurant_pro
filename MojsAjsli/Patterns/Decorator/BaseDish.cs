namespace MojsAjsli.Patterns.Decorator;

public class BaseDish : IDish
{
    private readonly string _name;
    private readonly string _description;
    private readonly decimal _price;
    private readonly int _preparationTime;

    public BaseDish(string name, string description, decimal price, int preparationTime = 10)
    {
        _name = name;
        _description = description;
        _price = price;
        _preparationTime = preparationTime;
    }

    public string Name => _name;
    public string Description => _description;
    public decimal Price => _price;

    public string GetName() => _name;
    public string GetDescription() => _description;
    public decimal GetPrice() => _price;
    public int GetPreparationTime() => _preparationTime;
}

public class Burger : BaseDish
{
    public Burger() : base("Burger", "Klasyczny burger wolowy z salata i pomidorem", 25.00m, 15) { }
}

public class Pizza : BaseDish
{
    public Pizza() : base("Pizza Margherita", "Klasyczna pizza z sosem pomidorowym i mozzarella", 30.00m, 20) { }
}

public class Pasta : BaseDish
{
    public Pasta() : base("Pasta Carbonara", "Makaron z boczkiem, jajkiem i parmezanem", 28.00m, 15) { }
}

public class Salad : BaseDish
{
    public Salad() : base("Salatka Cezar", "Salata rzymska z sosem cezar i grzankami", 22.00m, 8) { }
}

public class Drink : BaseDish
{
    public Drink(string name, decimal price) : base(name, "Napoj: " + name, price, 2) { }
}

public class Soup : BaseDish
{
    public Soup() : base("Zupa dnia", "Swieza zupa przygotowywana codziennie", 15.00m, 5) { }
}

public class Steak : BaseDish
{
    public Steak() : base("Stek wolowy", "Stek z poledwicy wolowej z frytkami", 55.00m, 25) { }
}

public class FishAndChips : BaseDish
{
    public FishAndChips() : base("Fish and Chips", "Smazona ryba z frytkami i sosem tatarskim", 35.00m, 18) { }
}

namespace MojsAjsli.Patterns.Bridge;

public abstract class CookableDish
{
    protected ICookingMethod _cookingMethod;
    public string Name { get; protected set; }
    public decimal BasePrice { get; protected set; }

    protected CookableDish(ICookingMethod cookingMethod)
    {
        _cookingMethod = cookingMethod;
        Name = "Unknown";
    }

    public void SetCookingMethod(ICookingMethod cookingMethod)
    {
        _cookingMethod = cookingMethod;
    }

    public abstract string Prepare();
    
    public virtual int GetTotalPreparationTime()
    {
        return GetBasePreparationTime() + _cookingMethod.GetAdditionalTime();
    }

    protected abstract int GetBasePreparationTime();
}

public class PizzaDish : CookableDish
{
    public PizzaDish(ICookingMethod cookingMethod) : base(cookingMethod)
    {
        Name = "Pizza";
        BasePrice = 30.00m;
    }

    public override string Prepare()
    {
        return "Przygotowanie pizzy: rozwalkowanie ciasta, dodanie skladnikow, " + _cookingMethod.Cook(Name);
    }

    protected override int GetBasePreparationTime() => 15;
}

public class PastaDish : CookableDish
{
    public PastaDish(ICookingMethod cookingMethod) : base(cookingMethod)
    {
        Name = "Pasta";
        BasePrice = 28.00m;
    }

    public override string Prepare()
    {
        return "Przygotowanie makaronu: gotowanie al dente, przygotowanie sosu, " + _cookingMethod.Cook(Name);
    }

    protected override int GetBasePreparationTime() => 12;
}

public class SaladDish : CookableDish
{
    public SaladDish(ICookingMethod cookingMethod) : base(cookingMethod)
    {
        Name = "Salatka";
        BasePrice = 22.00m;
    }

    public override string Prepare()
    {
        return "Przygotowanie salatki: mycie warzyw, krojenie, " + _cookingMethod.Cook(Name);
    }

    protected override int GetBasePreparationTime() => 5;
}

public class SteakDish : CookableDish
{
    public SteakDish(ICookingMethod cookingMethod) : base(cookingMethod)
    {
        Name = "Stek";
        BasePrice = 55.00m;
    }

    public override string Prepare()
    {
        return "Przygotowanie steku: marynowanie, doprawienie, " + _cookingMethod.Cook(Name);
    }

    protected override int GetBasePreparationTime() => 20;
}

public class FishDish : CookableDish
{
    public FishDish(ICookingMethod cookingMethod) : base(cookingMethod)
    {
        Name = "Ryba";
        BasePrice = 35.00m;
    }

    public override string Prepare()
    {
        return "Przygotowanie ryby: filetowanie, przyprawianie, " + _cookingMethod.Cook(Name);
    }

    protected override int GetBasePreparationTime() => 15;
}


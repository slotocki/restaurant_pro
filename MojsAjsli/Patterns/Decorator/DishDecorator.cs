namespace MojsAjsli.Patterns.Decorator;

public abstract class DishDecorator : IDish
{
    protected readonly IDish _baseDish;

    protected DishDecorator(IDish baseDish)
    {
        _baseDish = baseDish;
    }

    public virtual string Name => _baseDish.Name;
    public virtual string Description => _baseDish.Description;
    public virtual decimal Price => _baseDish.Price;

    public virtual string GetName() => _baseDish.GetName();
    public virtual string GetDescription() => _baseDish.GetDescription();
    public virtual decimal GetPrice() => _baseDish.GetPrice();
    public virtual int GetPreparationTime() => _baseDish.GetPreparationTime();
}

public class ExtraCheeseDecorator : DishDecorator
{
    public ExtraCheeseDecorator(IDish baseDish) : base(baseDish) { }

    public override string GetDescription() => _baseDish.GetDescription() + " + Extra Ser";
    public override decimal GetPrice() => _baseDish.GetPrice() + 3.00m;
    public override int GetPreparationTime() => _baseDish.GetPreparationTime() + 1;
}

public class BaconDecorator : DishDecorator
{
    public BaconDecorator(IDish baseDish) : base(baseDish) { }

    public override string GetDescription() => _baseDish.GetDescription() + " + Bekon";
    public override decimal GetPrice() => _baseDish.GetPrice() + 5.00m;
    public override int GetPreparationTime() => _baseDish.GetPreparationTime() + 2;
}

public class SpicySauceDecorator : DishDecorator
{
    public SpicySauceDecorator(IDish baseDish) : base(baseDish) { }

    public override string GetDescription() => _baseDish.GetDescription() + " + Ostry Sos";
    public override decimal GetPrice() => _baseDish.GetPrice() + 2.00m;
}

public class GlutenFreeDecorator : DishDecorator
{
    public GlutenFreeDecorator(IDish baseDish) : base(baseDish) { }

    public override string GetDescription() => _baseDish.GetDescription() + " (Bezglutenowe)";
    public override decimal GetPrice() => _baseDish.GetPrice() + 4.00m;
    public override int GetPreparationTime() => _baseDish.GetPreparationTime() + 3;
}

public class ExtraPortionDecorator : DishDecorator
{
    public ExtraPortionDecorator(IDish baseDish) : base(baseDish) { }

    public override string GetDescription() => _baseDish.GetDescription() + " (Duza porcja)";
    public override decimal GetPrice() => _baseDish.GetPrice() * 1.5m;
    public override int GetPreparationTime() => _baseDish.GetPreparationTime() + 5;
}

public class VeganOptionDecorator : DishDecorator
{
    public VeganOptionDecorator(IDish baseDish) : base(baseDish) { }

    public override string GetDescription() => _baseDish.GetDescription() + " (Wersja weganska)";
    public override decimal GetPrice() => _baseDish.GetPrice() + 3.00m;
    public override int GetPreparationTime() => _baseDish.GetPreparationTime() + 4;
}

namespace MojsAjsli.Patterns.Bridge;

public class OvenCooking : ICookingMethod
{
    public string MethodName => "Piekarnik";
    public string Cook(string dishName) => dishName + " pieczony w piekarniku do zlotego koloru";
    public int GetAdditionalTime() => 10;
}

public class StoveCooking : ICookingMethod
{
    public string MethodName => "Kuchenka";
    public string Cook(string dishName) => dishName + " gotowany na kuchence";
    public int GetAdditionalTime() => 5;
}

public class GrillCooking : ICookingMethod
{
    public string MethodName => "Grill";
    public string Cook(string dishName) => dishName + " grillowany na otwartym ogniu";
    public int GetAdditionalTime() => 8;
}

public class RawPreparation : ICookingMethod
{
    public string MethodName => "Bez obrobki";
    public string Cook(string dishName) => dishName + " przygotowany na swiezo bez obrobki termicznej";
    public int GetAdditionalTime() => 0;
}

public class SteamCooking : ICookingMethod
{
    public string MethodName => "Na parze";
    public string Cook(string dishName) => dishName + " gotowany na parze - zdrowsza opcja";
    public int GetAdditionalTime() => 7;
}

public class FryingCooking : ICookingMethod
{
    public string MethodName => "Smazenie";
    public string Cook(string dishName) => dishName + " smazony na glebokim oleju";
    public int GetAdditionalTime() => 6;
}


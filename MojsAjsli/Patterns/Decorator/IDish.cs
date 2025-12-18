namespace MojsAjsli.Patterns.Decorator;

public interface IDish
{
    string Name { get; }
    decimal Price { get; }
    string Description { get; }
    
    string GetName();
    string GetDescription();
    decimal GetPrice();
    int GetPreparationTime();
}

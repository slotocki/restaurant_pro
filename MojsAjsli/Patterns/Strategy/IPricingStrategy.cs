using MojsAjsli.Patterns.State;

namespace MojsAjsli.Patterns.Strategy;

public interface IPricingStrategy
{
    string Name { get; }
    string Description { get; }
    decimal CalculatePrice(Order order);
    decimal GetDiscountPercentage();
    bool IsApplicable(Order order, DateTime currentTime);
}


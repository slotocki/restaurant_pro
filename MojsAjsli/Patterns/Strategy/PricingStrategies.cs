using MojsAjsli.Patterns.State;

namespace MojsAjsli.Patterns.Strategy;

public class RegularPricingStrategy : IPricingStrategy
{
    public string Name => "Normalna cena";
    public string Description => "Standardowe ceny bez znizek";

    public decimal CalculatePrice(Order order)
    {
        return order.TotalPrice;
    }

    public decimal GetDiscountPercentage() => 0;

    public bool IsApplicable(Order order, DateTime currentTime) => true;
}

public class HappyHourStrategy : IPricingStrategy
{
    private readonly TimeSpan _startTime = new TimeSpan(15, 0, 0);
    private readonly TimeSpan _endTime = new TimeSpan(18, 0, 0);
    private const decimal DiscountPercent = 0.30m;

    public string Name => "Happy Hour";
    public string Description => "30% znizki w godzinach 15:00-18:00";

    public decimal CalculatePrice(Order order)
    {
        return order.TotalPrice * (1 - DiscountPercent);
    }

    public decimal GetDiscountPercentage() => DiscountPercent * 100;

    public bool IsApplicable(Order order, DateTime currentTime)
    {
        var time = currentTime.TimeOfDay;
        return time >= _startTime && time <= _endTime;
    }
}

public class LoyaltyCardStrategy : IPricingStrategy
{
    private const decimal DiscountPercent = 0.15m;

    public string Name => "Karta stalego klienta";
    public string Description => "15% znizki dla stalych klientow";

    public decimal CalculatePrice(Order order)
    {
        return order.TotalPrice * (1 - DiscountPercent);
    }

    public decimal GetDiscountPercentage() => DiscountPercent * 100;

    public bool IsApplicable(Order order, DateTime currentTime) => true;
}

public class GroupDiscountStrategy : IPricingStrategy
{
    private const decimal DiscountPercent = 0.10m;
    private const int MinGroupSize = 5;

    public int GroupSize { get; set; }

    public string Name => "Znizka grupowa";
    public string Description => "10% znizki dla grup 5+ osob";

    public decimal CalculatePrice(Order order)
    {
        return order.TotalPrice * (1 - DiscountPercent);
    }

    public decimal GetDiscountPercentage() => DiscountPercent * 100;

    public bool IsApplicable(Order order, DateTime currentTime)
    {
        return GroupSize >= MinGroupSize;
    }
}

public class StudentDiscountStrategy : IPricingStrategy
{
    private const decimal DiscountPercent = 0.20m;

    public string Name => "Znizka studencka";
    public string Description => "20% znizki dla studentow";

    public decimal CalculatePrice(Order order)
    {
        return order.TotalPrice * (1 - DiscountPercent);
    }

    public decimal GetDiscountPercentage() => DiscountPercent * 100;

    public bool IsApplicable(Order order, DateTime currentTime) => true;
}

public class WeekendStrategy : IPricingStrategy
{
    private const decimal DiscountPercent = 0.10m;

    public string Name => "Promocja weekendowa";
    public string Description => "10% znizki w weekendy";

    public decimal CalculatePrice(Order order)
    {
        return order.TotalPrice * (1 - DiscountPercent);
    }

    public decimal GetDiscountPercentage() => DiscountPercent * 100;

    public bool IsApplicable(Order order, DateTime currentTime)
    {
        return currentTime.DayOfWeek == DayOfWeek.Saturday || 
               currentTime.DayOfWeek == DayOfWeek.Sunday;
    }
}


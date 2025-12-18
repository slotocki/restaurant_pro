using MojsAjsli.Patterns.State;

namespace MojsAjsli.Services;

public class StatisticsService
{
    private readonly List<Order> _completedOrders = new();
    private readonly Dictionary<string, int> _dishPopularity = new();
    private decimal _totalRevenue = 0;

    public void RecordCompletedOrder(Order order, decimal paidAmount)
    {
        _completedOrders.Add(order);
        _totalRevenue += paidAmount;

        foreach (var item in order.Items)
        {
            var dishName = item.GetName();
            if (_dishPopularity.ContainsKey(dishName))
                _dishPopularity[dishName]++;
            else
                _dishPopularity[dishName] = 1;
        }
    }

    public decimal GetTotalRevenue() => _totalRevenue;

    public decimal GetDailyRevenue(DateTime date)
    {
        return _completedOrders
            .Where(o => o.CompletedAt?.Date == date.Date)
            .Sum(o => o.TotalPrice);
    }

    public decimal GetTodayRevenue() => GetDailyRevenue(DateTime.Today);
    public int GetTotalOrdersCount() => _completedOrders.Count;

    public int GetTodayOrdersCount()
    {
        return _completedOrders.Count(o => o.CompletedAt?.Date == DateTime.Today);
    }

    public Dictionary<string, int> GetDishPopularity()
    {
        return _dishPopularity
            .OrderByDescending(kvp => kvp.Value)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public List<(string DishName, int Count)> GetTopDishes(int count = 5)
    {
        return _dishPopularity
            .OrderByDescending(kvp => kvp.Value)
            .Take(count)
            .Select(kvp => (kvp.Key, kvp.Value))
            .ToList();
    }

    public TimeSpan GetAverageOrderTime()
    {
        var completedWithTime = _completedOrders
            .Where(o => o.CompletedAt.HasValue)
            .ToList();

        if (!completedWithTime.Any())
            return TimeSpan.Zero;

        var totalMinutes = completedWithTime
            .Average(o => (o.CompletedAt!.Value - o.CreatedAt).TotalMinutes);

        return TimeSpan.FromMinutes(totalMinutes);
    }

    public decimal GetAverageOrderValue()
    {
        if (!_completedOrders.Any()) return 0;
        return _completedOrders.Average(o => o.TotalPrice);
    }
}


using MojsAjsli.Models;

namespace MojsAjsli.Patterns.Iterator;

public class MenuAggregate : IAggregate<MenuItem>
{
    private readonly List<MenuItem> _items = new();

    public void AddItem(MenuItem item) => _items.Add(item);
    public void RemoveItem(MenuItem item) => _items.Remove(item);
    public List<MenuItem> GetAllItems() => new(_items);
    public IIterator<MenuItem> CreateIterator() => new MenuIterator(_items);
    public IIterator<MenuItem> CreateCategoryIterator(DishCategory category) => new CategoryIterator(_items, category);
    public IIterator<MenuItem> CreatePriceRangeIterator(decimal minPrice, decimal maxPrice) => new PriceRangeIterator(_items, minPrice, maxPrice);
    public int Count => _items.Count;
}

public class MenuIterator : IIterator<MenuItem>
{
    private readonly List<MenuItem> _items;
    private int _position = -1;

    public MenuIterator(List<MenuItem> items) => _items = items;

    public bool HasNext() => _position + 1 < _items.Count;
    
    public MenuItem Next()
    {
        if (!HasNext()) throw new InvalidOperationException("Brak kolejnych elementow.");
        _position++;
        return _items[_position];
    }

    public void Reset() => _position = -1;

    public MenuItem Current => _position >= 0 && _position < _items.Count 
        ? _items[_position] 
        : throw new InvalidOperationException("Brak biezacego elementu.");
}

public class CategoryIterator : IIterator<MenuItem>
{
    private readonly List<MenuItem> _filteredItems;
    private int _position = -1;

    public CategoryIterator(List<MenuItem> items, DishCategory category)
    {
        _filteredItems = items.Where(i => i.Category == category).ToList();
    }

    public bool HasNext() => _position + 1 < _filteredItems.Count;
    
    public MenuItem Next()
    {
        if (!HasNext()) throw new InvalidOperationException("Brak kolejnych elementow w tej kategorii.");
        _position++;
        return _filteredItems[_position];
    }

    public void Reset() => _position = -1;

    public MenuItem Current => _position >= 0 && _position < _filteredItems.Count 
        ? _filteredItems[_position] 
        : throw new InvalidOperationException("Brak biezacego elementu.");
}

public class PriceRangeIterator : IIterator<MenuItem>
{
    private readonly List<MenuItem> _filteredItems;
    private int _position = -1;

    public PriceRangeIterator(List<MenuItem> items, decimal minPrice, decimal maxPrice)
    {
        _filteredItems = items
            .Where(i => i.BasePrice >= minPrice && i.BasePrice <= maxPrice)
            .OrderBy(i => i.BasePrice)
            .ToList();
    }

    public bool HasNext() => _position + 1 < _filteredItems.Count;
    
    public MenuItem Next()
    {
        if (!HasNext()) throw new InvalidOperationException("Brak kolejnych elementow w tym zakresie cenowym.");
        _position++;
        return _filteredItems[_position];
    }

    public void Reset() => _position = -1;

    public MenuItem Current => _position >= 0 && _position < _filteredItems.Count 
        ? _filteredItems[_position] 
        : throw new InvalidOperationException("Brak biezacego elementu.");
}

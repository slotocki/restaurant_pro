namespace MojsAjsli.Patterns.Iterator;

public interface IIterator<T>
{
    bool HasNext();
    T Next();
    void Reset();
    T Current { get; }
}

public interface IAggregate<T>
{
    IIterator<T> CreateIterator();
    int Count { get; }
}


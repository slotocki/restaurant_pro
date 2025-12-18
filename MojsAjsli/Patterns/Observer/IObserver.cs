namespace MojsAjsli.Patterns.Observer;

public interface IObserver<T>
{
    void Update(T data);
}

public interface ISubject<T>
{
    void Attach(IObserver<T> observer);
    void Detach(IObserver<T> observer);
    void Notify(T data);
}


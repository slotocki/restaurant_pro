namespace MojsAjsli.Patterns.Bridge;

public interface ICookingMethod
{
    string MethodName { get; }
    string Cook(string dishName);
    int GetAdditionalTime();
}


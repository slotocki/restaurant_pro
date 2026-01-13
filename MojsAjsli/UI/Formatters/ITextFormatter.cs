namespace MojsAjsli.UI.Formatters;

/// <summary>
/// Interfejs do formatowania tekstu - ISP (Interface Segregation Principle)
/// Klienci nie są zmuszani do zależności od metod, których nie używają
/// </summary>
public interface ITextFormatter
{
    string Format(object value);
}

/// <summary>
/// Formatter dla cen
/// </summary>
public class PriceFormatter : ITextFormatter
{
    public string Format(object value)
    {
        if (value is decimal price)
            return $"{price:N2} zł";
        return value?.ToString() ?? string.Empty;
    }
}

/// <summary>
/// Formatter dla czasu
/// </summary>
public class TimeFormatter : ITextFormatter
{
    private readonly string _format;

    public TimeFormatter(string format = "HH:mm:ss")
    {
        _format = format;
    }

    public string Format(object value)
    {
        if (value is DateTime dateTime)
            return dateTime.ToString(_format);
        return value?.ToString() ?? string.Empty;
    }
}

/// <summary>
/// Formatter dla statusów stolików
/// </summary>
public class TableStatusFormatter : ITextFormatter
{
    public string Format(object value)
    {
        if (value is not MojsAjsli.Models.TableStatus status)
            return string.Empty;

        return status switch
        {
            MojsAjsli.Models.TableStatus.Free => "Wolny",
            MojsAjsli.Models.TableStatus.Occupied => "Zajęty",
            MojsAjsli.Models.TableStatus.Reserved => "Zarezerwowany",
            MojsAjsli.Models.TableStatus.NeedsCleaning => "Do sprzątania",
            _ => "Nieznany"
        };
    }
}

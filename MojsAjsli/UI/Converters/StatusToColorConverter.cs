using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using MojsAjsli.Models;

namespace MojsAjsli.UI.Converters;

public class TableStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TableStatus status)
        {
            return status switch
            {
                TableStatus.Free => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                TableStatus.Occupied => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                TableStatus.Reserved => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                TableStatus.NeedsCleaning => new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class OrderStateToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string stateName)
        {
            return stateName switch
            {
                "Nowe" => new SolidColorBrush(Color.FromRgb(141, 110, 99)),
                "Przyjete" => new SolidColorBrush(Color.FromRgb(161, 136, 127)),
                "W przygotowaniu" => new SolidColorBrush(Color.FromRgb(245, 124, 0)),
                "Gotowe" => new SolidColorBrush(Color.FromRgb(158, 157, 36)),
                "Dostarczone" => new SolidColorBrush(Color.FromRgb(175, 180, 43)),
                "Oplacone" => new SolidColorBrush(Color.FromRgb(121, 85, 72)),
                "Anulowane" => new SolidColorBrush(Color.FromRgb(191, 54, 12)),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class PriceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal price)
        {
            return string.Format("{0:N2} zl", price);
        }
        return "0,00 zl";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class TableStatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TableStatus status)
        {
            return status switch
            {
                TableStatus.Free => "Wolny",
                TableStatus.Occupied => "Zajety",
                TableStatus.Reserved => "Zarezerwowany",
                TableStatus.NeedsCleaning => "Do sprzatania",
                _ => "Nieznany"
            };
        }
        return "Nieznany";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class CategoryToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DishCategory category)
        {
            return category switch
            {
                DishCategory.Appetizer => "🥗",
                DishCategory.MainCourse => "🍽️",
                DishCategory.Dessert => "🍰",
                DishCategory.Drink => "🥤",
                DishCategory.Vegetarian => "🌱",
                DishCategory.Special => "⭐",
                _ => "?"
            };
        }
        return "?";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class CategoryToNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DishCategory category)
        {
            return category switch
            {
                DishCategory.Appetizer => "Przystawki",
                DishCategory.MainCourse => "Dania Główne",
                DishCategory.Dessert => "Desery",
                DishCategory.Drink => "Napoje",
                DishCategory.Vegetarian => "Wegetariańskie",
                DishCategory.Special => "Specjały",
                _ => "Inne"
            };
        }
        return "Inne";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class CategoryToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DishCategory category)
        {
            return category switch
            {
                DishCategory.Appetizer => new SolidColorBrush(Color.FromRgb(141, 110, 99)),    // Brązowy
                DishCategory.MainCourse => new SolidColorBrush(Color.FromRgb(93, 64, 55)),      // Ciemny brąz
                DishCategory.Dessert => new SolidColorBrush(Color.FromRgb(215, 204, 200)),      // Jasny brąz
                DishCategory.Drink => new SolidColorBrush(Color.FromRgb(161, 136, 127)),        // Piaskowy
                DishCategory.Vegetarian => new SolidColorBrush(Color.FromRgb(158, 157, 36)),    // Oliwkowy
                DishCategory.Special => new SolidColorBrush(Color.FromRgb(245, 124, 0)),        // Pomarańczowy
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

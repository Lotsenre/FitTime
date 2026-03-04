using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FitTime.Helpers;

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is string expected)
        {
            // Special case: "." means "show if non-empty"
            if (expected == ".")
                return !string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;

            if (value is string str)
                return str == expected ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

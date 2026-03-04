using System.Globalization;
using System.Windows.Data;

namespace FitTime.Helpers;

public class BooleanToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? 220.0 : 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

using System.Globalization;
using System.Windows.Data;

namespace FitTime.Helpers;

public class CountToBarHeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            // Scale to max 180px height, minimum 2px if > 0
            var height = count > 0 ? Math.Max(2.0, count * 2.5) : 0.0;
            return Math.Min(180.0, height);
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

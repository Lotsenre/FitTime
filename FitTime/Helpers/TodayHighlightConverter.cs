using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FitTime.Helpers;

public class TodayHighlightConverter : IValueConverter
{
    private static readonly SolidColorBrush TodayBrush = new(Color.FromRgb(0xF4, 0xB4, 0x48));
    private static readonly SolidColorBrush NormalBrush = new(Color.FromRgb(0x55, 0x55, 0x55));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int todayIndex && parameter is string paramStr && int.TryParse(paramStr, out var dayIndex))
        {
            return todayIndex == dayIndex ? TodayBrush : NormalBrush;
        }
        return NormalBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

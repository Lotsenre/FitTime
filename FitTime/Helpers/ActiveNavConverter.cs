using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FitTime.Helpers;

public class ActiveNavConverter : IValueConverter
{
    private static readonly SolidColorBrush ActiveFg = new(Colors.White);
    private static readonly SolidColorBrush InactiveFg = new(Color.FromRgb(0x80, 0x80, 0x80));
    private static readonly SolidColorBrush ActiveUnderline = new(Color.FromRgb(0xF4, 0xB4, 0x48));

    /// <summary>
    /// ConverterParameter format: "index" or "index,mode"
    /// mode = "fg" (default) → White/Gray foreground
    /// mode = "underline" → Gold/Transparent for bottom border
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int activeIndex && parameter is string paramStr)
        {
            var parts = paramStr.Split(',');
            if (int.TryParse(parts[0].Trim(), out var idx))
            {
                bool isActive = activeIndex == idx;
                var mode = parts.Length > 1 ? parts[1].Trim() : "fg";
                return mode switch
                {
                    "underline" => isActive ? ActiveUnderline : (SolidColorBrush)Brushes.Transparent,
                    _ => isActive ? ActiveFg : InactiveFg
                };
            }
        }
        return InactiveFg;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

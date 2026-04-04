using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RamDump.Converters;

public class PercentToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double percent = value is double d ? d : 0;

        return percent switch
        {
            < 60 => new SolidColorBrush(Color.FromRgb(0xA6, 0xE3, 0xA1)),  // Grün
            < 80 => new SolidColorBrush(Color.FromRgb(0xF9, 0xE2, 0xAF)),  // Gelb
            _ => new SolidColorBrush(Color.FromRgb(0xF3, 0x8B, 0xA8)),     // Rot
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

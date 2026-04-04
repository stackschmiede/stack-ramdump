using System.Globalization;
using System.Windows.Data;

namespace RamDump.Converters;

public class BytesToGigabytesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not long bytes) return "0";
        double gb = bytes / (1024.0 * 1024 * 1024);
        return $"{gb:F1}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

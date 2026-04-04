using System.Globalization;
using System.Windows.Data;

namespace RamDump.Converters;

public class BytesToReadableConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not long bytes) return "0 B";
        if (bytes < 0) return "0 B";

        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double val = bytes;
        int i = 0;
        while (val >= 1024 && i < units.Length - 1)
        {
            val /= 1024;
            i++;
        }
        return $"{val:F1} {units[i]}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

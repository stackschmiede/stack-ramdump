using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RamDump.Converters;

// Two-way: value ↔ bool (IsChecked == value==parameter). ConvertBack setzt den Parameter-Wert zurück.
public class StringEqualsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var v = value?.ToString() ?? string.Empty;
        var p = parameter?.ToString() ?? string.Empty;
        return string.Equals(v, p, StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return parameter?.ToString() ?? string.Empty;
        return Binding.DoNothing;
    }
}

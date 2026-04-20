using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RamDump.Converters;

// WorkingSet >= 500 MB → Visible, sonst Collapsed. Für "Heavy"-Badge.
public class WorkingSetIsHeavyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        long bytes = value switch
        {
            long l => l,
            int i => i,
            _ => 0L,
        };
        return bytes >= 500_000_000 ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

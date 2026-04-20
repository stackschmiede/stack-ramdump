using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace RamDump.Converters;

// WorkingSet-Bytes → Level-Brush (Load-Ok / Warn / High / Critical) anhand absoluter Schwellen.
public class WorkingSetToLevelBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        long bytes = value switch
        {
            long l => l,
            int i => i,
            double d => (long)d,
            _ => 0L,
        };

        string key = bytes switch
        {
            >= 1_500_000_000 => "LoadCriticalBrush",
            >= 500_000_000 => "LoadHighBrush",
            >= 100_000_000 => "LoadWarnBrush",
            _ => "LoadOkBrush",
        };

        if (Application.Current?.TryFindResource(key) is Brush brush)
            return brush;
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

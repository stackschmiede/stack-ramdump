using System.Globalization;
using System.Windows.Data;

namespace RamDump.Converters;

// MultiBinding: [0] = WorkingSet (long), [1] = TotalRam (long), [2] = MaxPixelWidth (double)
// Liefert Breite (double) für Microbars in Rows/Group-Headers.
public class WorkingSetToBarWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3) return 0.0;

        double val = values[0] switch
        {
            long l => l,
            double d => d,
            int i => i,
            _ => 0.0,
        };
        double max = values[1] switch
        {
            long l => l,
            double d => d,
            int i => i,
            _ => 0.0,
        };
        double pw = values[2] is double w ? w : 0.0;

        if (max <= 0 || pw <= 0 || val <= 0) return 0.0;
        double ratio = val / max;
        if (ratio > 1.0) ratio = 1.0;
        if (ratio < 0) ratio = 0;
        return ratio * pw;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

using System.Collections;
using System.Globalization;
using System.Windows.Data;
using RamDump.ViewModels;

namespace RamDump.Converters;

// MultiBinding: [0] = IList Items (ProcessMemoryInfoViewModel), [1] = TotalRam (long), [2] = PixelWidth (double)
public class GroupBarWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3) return 0.0;

        long sum = 0;
        if (values[0] is IList items)
        {
            foreach (var item in items)
            {
                if (item is ProcessMemoryInfoViewModel vm)
                    sum += vm.WorkingSet;
            }
        }

        double max = values[1] switch
        {
            long l => l,
            double d => d,
            _ => 0.0,
        };
        double pw = values[2] is double w ? w : 0.0;

        if (max <= 0 || pw <= 0 || sum <= 0) return 0.0;
        double ratio = (double)sum / max;
        if (ratio > 1.0) ratio = 1.0;
        return ratio * pw;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

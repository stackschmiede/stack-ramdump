using System.Collections;
using System.Globalization;
using System.Windows.Data;
using RamDump.ViewModels;

namespace RamDump.Converters;

// MultiBinding: [0] = IList Items, [1] = TotalRam (long)
// Liefert String "12.3%" für Group-Badge.
public class GroupPctConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2) return "0%";

        long sum = 0;
        if (values[0] is IList items)
        {
            foreach (var item in items)
            {
                if (item is ProcessMemoryInfoViewModel vm)
                    sum += vm.WorkingSet;
            }
        }

        long total = values[1] switch
        {
            long l => l,
            double d => (long)d,
            _ => 0L,
        };

        if (total <= 0) return "0%";
        double pct = (double)sum / total * 100.0;
        return $"{pct:F1}%";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

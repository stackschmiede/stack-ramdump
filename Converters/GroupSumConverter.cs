using System.Collections;
using System.Globalization;
using System.Windows.Data;
using RamDump.ViewModels;

namespace RamDump.Converters;

public class GroupSumConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not IList items) return "0 B";

        long sum = 0;
        string prop = parameter as string ?? "WorkingSet";

        foreach (var item in items)
        {
            if (item is not ProcessMemoryInfoViewModel vm) continue;
            sum += prop switch
            {
                "PrivateBytes" => vm.PrivateBytes,
                "PeakWorkingSet" => vm.PeakWorkingSet,
                _ => vm.WorkingSet,
            };
        }

        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double val = sum;
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

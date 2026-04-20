using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RamDump.Converters;

// COLORS.md §5: Load states — Grün → Ocker → Orange → Rot an Auslastungs-Schwellen gekoppelt
public class PercentToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double percent = value is double d ? d : 0;

        return percent switch
        {
            < 60 => new SolidColorBrush(Color.FromRgb(0x6F, 0xA2, 0x6A)), // LoadOk
            < 80 => new SolidColorBrush(Color.FromRgb(0xD4, 0xA5, 0x74)), // LoadWarn (= Accent, bewusst)
            < 92 => new SolidColorBrush(Color.FromRgb(0xD9, 0x8A, 0x5C)), // LoadHigh
            _    => new SolidColorBrush(Color.FromRgb(0xC2, 0x5A, 0x4E)), // LoadCritical
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

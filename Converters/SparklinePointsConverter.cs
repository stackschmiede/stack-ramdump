using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace RamDump.Converters;

// MultiBinding: [0] = IEnumerable<double> (Werte 0..max), [1] = ActualWidth, [2] = ActualHeight
// Parameter = Max (z.B. "100")
// Liefert PointCollection mit pixelgenauen Koordinaten für Polyline Stretch="None".
public class SparklinePointsConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var empty = new PointCollection();
        if (values.Length < 3) return empty;
        if (values[0] is not IEnumerable seq) return empty;
        if (values[1] is not double w || values[2] is not double h) return empty;
        if (w <= 0 || h <= 0) return empty;

        double max = parameter switch
        {
            double d => d,
            string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var p) => p,
            _ => 100.0,
        };
        if (max <= 0) max = 100.0;

        var list = new List<double>();
        foreach (var item in seq)
        {
            var v = item switch
            {
                double d => d,
                int n => n,
                float f => f,
                _ => System.Convert.ToDouble(item, CultureInfo.InvariantCulture),
            };
            list.Add(Math.Max(0, Math.Min(max, v)));
        }
        if (list.Count == 0) return empty;

        var pts = new PointCollection(list.Count);
        var stepX = list.Count > 1 ? w / (list.Count - 1) : 0;
        for (int i = 0; i < list.Count; i++)
        {
            var x = i * stepX;
            var y = h - (list[i] / max) * h; // invertiert: hohe Werte oben
            pts.Add(new Point(x, y));
        }
        return pts;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

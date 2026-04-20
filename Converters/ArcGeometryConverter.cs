using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace RamDump.Converters;

// Percent (0..100) -> Geometry für einen Donut-Arc (Start bei 12 Uhr, Richtung im Uhrzeigersinn).
// Parameter: "cx,cy,r" (z.B. "80,80,66"). Default: "80,80,66".
public class ArcGeometryConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double pct;
        try
        {
            pct = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            pct = 0;
        }
        pct = Math.Clamp(pct / 100.0, 0, 1);

        var parts = (parameter as string ?? "80,80,66").Split(',');
        double cx = ParseD(parts, 0, 80);
        double cy = ParseD(parts, 1, 80);
        double r = ParseD(parts, 2, 66);

        if (pct <= 0)
            return Geometry.Empty;

        if (pct >= 0.9999)
        {
            var eg = new EllipseGeometry(new Point(cx, cy), r, r);
            eg.Freeze();
            return eg;
        }

        const double topDeg = -90.0;
        double angle = pct * 360.0;
        double endDeg = topDeg + angle;
        double topRad = topDeg * Math.PI / 180.0;
        double endRad = endDeg * Math.PI / 180.0;

        var start = new Point(cx + r * Math.Cos(topRad), cy + r * Math.Sin(topRad));
        var end = new Point(cx + r * Math.Cos(endRad), cy + r * Math.Sin(endRad));

        var figure = new PathFigure { StartPoint = start, IsClosed = false };
        figure.Segments.Add(new ArcSegment(
            end,
            new Size(r, r),
            0,
            angle > 180,
            SweepDirection.Clockwise,
            true));

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        geometry.Freeze();
        return geometry;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static double ParseD(string[] parts, int i, double fallback)
    {
        if (i >= parts.Length) return fallback;
        return double.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
            ? d : fallback;
    }
}

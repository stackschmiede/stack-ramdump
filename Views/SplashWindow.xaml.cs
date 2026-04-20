using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace RamDump.Views;

public partial class SplashWindow : Window
{
    private const double DUR = 4.0;

    private const double T_BOTTOM_START = 0.00, T_BOTTOM_LAND = 0.60;
    private const double T_MIDDLE_START = 0.35, T_MIDDLE_LAND = 0.95;
    private const double T_TOP_START    = 0.70, T_TOP_LAND    = 1.30;
    private const double T_PINS_START   = 1.35, T_PINS_END    = 2.10;
    private const double T_IMPACT       = 2.10;
    private const double T_FLASH_END    = 2.30;
    private const double T_SPARK_END    = 2.80;
    private const double T_SHAKE_END    = 2.75;

    private const double WORDMARK_START = 2.30;
    private const double WORDMARK_END   = 2.80;

    private const double FADE_OUT_START = 3.55;
    private const double FADE_OUT_END   = 3.95;

    private static readonly Brush InkBrush   = (SolidColorBrush)new BrushConverter().ConvertFromString("#0F0F10")!;
    private static readonly Brush AccentBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#D4A574")!;
    private static readonly Brush LineBrush  = (SolidColorBrush)new BrushConverter().ConvertFromString("#EDEAE3")!;
    private static readonly Brush SparkBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#FFD79A")!;
    private static readonly Brush SparkHotBrush = Brushes.White;

    private readonly Stopwatch _clock = new();
    private readonly Random _rng = new();
    private readonly List<Particle> _particles = new();
    private readonly List<Ellipse> _sparkEllipses = new();
    private readonly List<(TextBlock Tb, TranslateTransform Tx)> _letters = new();
    private readonly List<Path> _pins;

    private bool _finished;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    public SplashWindow()
    {
        InitializeComponent();
        _pins = new List<Path> { Pin0, Pin1, Pin2, Pin3, Pin4, Pin5 };

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyDarkTitleBar();
        BuildSparks();
        BuildWordmark();
        _clock.Start();
        CompositionTarget.Rendering += OnRender;
    }

    private void ApplyDarkTitleBar()
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int value = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
        }
        catch { /* best effort */ }
    }

    private void BuildSparks()
    {
        _particles.Clear();
        _sparkEllipses.Clear();
        SparksLayer.Children.Clear();

        for (int i = 0; i < 18; i++)
        {
            double angle = -Math.PI / 2 + (_rng.NextDouble() - 0.5) * Math.PI * 0.9;
            double speed = 16 + _rng.NextDouble() * 22;
            double size = 0.35 + _rng.NextDouble() * 0.55;
            double life = 0.55 + _rng.NextDouble() * 0.35;
            var brush = _rng.NextDouble() < 0.3 ? SparkHotBrush : SparkBrush;
            _particles.Add(new Particle(angle, speed, size, life, brush));

            var el = new Ellipse
            {
                Fill = brush,
                Width = size * 2,
                Height = size * 2,
                Opacity = 0,
                IsHitTestVisible = false,
            };
            _sparkEllipses.Add(el);
            SparksLayer.Children.Add(el);
        }
    }

    private void BuildWordmark()
    {
        const string text = "ramdump-stack";
        Wordmark.Children.Clear();
        _letters.Clear();

        foreach (var ch in text)
        {
            var isDash = ch == '-';
            var tx = new TranslateTransform(0, 10);
            var tb = new TextBlock
            {
                Text = ch.ToString(),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 28,
                FontWeight = FontWeights.SemiBold,
                Foreground = isDash ? AccentBrush : LineBrush,
                Opacity = 0,
                RenderTransform = tx,
                Padding = isDash ? new Thickness(2, 0, 2, 0) : new Thickness(0),
            };
            Wordmark.Children.Add(tb);
            _letters.Add((tb, tx));
        }
    }

    private void OnRender(object? sender, EventArgs e)
    {
        if (_finished) return;

        double t = _clock.Elapsed.TotalSeconds;

        if (t >= DUR)
        {
            Finish();
            return;
        }

        // Chip Y offsets (relative to resting position).
        double chipOff(double start, double land) => ChipTransY(t, start, land);

        double bottomOff = chipOff(T_BOTTOM_START, T_BOTTOM_LAND);
        double middleOff = chipOff(T_MIDDLE_START, T_MIDDLE_LAND);
        double topOff    = chipOff(T_TOP_START,    T_TOP_LAND);

        BottomTransform.Y = bottomOff;
        MiddleTransform.Y = middleOff;
        TopTransform.Y    = topOff;

        // Accent line: fades in after top chip lands
        double accentOp = Clamp01((t - T_TOP_LAND) / 0.5);
        AccentLine.Opacity = accentOp;

        // Pins: stagger after impact
        double pinProgress = 0;
        if (t >= T_PINS_START)
        {
            double raw = Clamp01((t - T_PINS_START) / (T_PINS_END - T_PINS_START));
            pinProgress = EaseOutCubic(raw);
        }
        UpdatePins(pinProgress);

        // Impact flash
        double flashT = (t >= T_IMPACT && t <= T_FLASH_END)
            ? (t - T_IMPACT) / (T_FLASH_END - T_IMPACT) : -1;
        FlashRect.Opacity = flashT >= 0 ? (1 - flashT) * 0.55 : 0;

        // Spark timeline
        double sparkT = (t >= T_IMPACT && t <= T_SPARK_END)
            ? (t - T_IMPACT) / (T_SPARK_END - T_IMPACT) : -1;

        // Shake
        double shakeElapsed = (t >= T_IMPACT && t <= T_SHAKE_END) ? (t - T_IMPACT) : -1;
        (double shx, double shy) = shakeElapsed >= 0
            ? Shake(shakeElapsed, T_SHAKE_END - T_IMPACT, 0.8)
            : (0, 0);
        ShakeTransform.X = shx;
        ShakeTransform.Y = shy;

        // Glow position (base 32, 20) + shake + top-chip offset
        GlowTransform.X = 32 + shx;
        GlowTransform.Y = 20 + shy + topOff;

        // Glow opacity
        double glowOp = 0.12 * accentOp + (flashT >= 0 ? (1 - flashT) * 0.4 : 0);
        GlowOuter.Opacity = glowOp * 0.5;
        GlowInner.Opacity = glowOp;

        // Sparks position (base 32, 20) + shake
        SparksTransform.X = 32 + shx;
        SparksTransform.Y = 20 + shy;
        UpdateSparks(sparkT);

        // Wordmark
        UpdateWordmark(t);

        // Fade-out toward the end
        if (t >= FADE_OUT_START)
        {
            double f = Clamp01((t - FADE_OUT_START) / (FADE_OUT_END - FADE_OUT_START));
            Opacity = 1 - f;
        }
    }

    private void UpdatePins(double progress)
    {
        int n = _pins.Count;
        double per = 1.0 / n;
        for (int i = 0; i < n; i++)
        {
            double localT = Clamp01((progress - i * per) / per);
            _pins[i].Opacity = localT;
            _pins[i].StrokeDashOffset = (1 - localT) * 5;
        }
    }

    private void UpdateSparks(double sparkT)
    {
        if (sparkT <= 0 || sparkT >= 1)
        {
            foreach (var el in _sparkEllipses)
                el.Opacity = 0;
            return;
        }

        const double gravity = 60.0;

        for (int i = 0; i < _particles.Count; i++)
        {
            var p = _particles[i];
            var el = _sparkEllipses[i];
            double localT = sparkT / p.Life;
            if (localT >= 1)
            {
                el.Opacity = 0;
                continue;
            }

            double tt = localT * 0.55;
            double vx = Math.Cos(p.Angle) * p.Speed;
            double vy = Math.Sin(p.Angle) * p.Speed;
            double x = vx * tt;
            double y = vy * tt + 0.5 * gravity * tt * tt;
            double fade = 1 - localT;
            double r = p.Size * (1 - localT * 0.5);

            el.Width = r * 2;
            el.Height = r * 2;
            Canvas.SetLeft(el, x - r);
            Canvas.SetTop(el, y - r);
            el.Opacity = fade;
        }
    }

    private void UpdateWordmark(double t)
    {
        if (t < WORDMARK_START)
        {
            Wordmark.Opacity = 0;
            return;
        }

        double groupP = Clamp01((t - WORDMARK_START) / (WORDMARK_END - WORDMARK_START));
        Wordmark.Opacity = EaseOutCubic(groupP);

        for (int i = 0; i < _letters.Count; i++)
        {
            var (_, tx) = _letters[i];
            double lp = Clamp01((t - (WORDMARK_START + i * 0.025)) / 0.35);
            double eased = EaseOutCubic(lp);
            _letters[i].Tb.Opacity = eased;
            tx.Y = (1 - eased) * 10;
        }
    }

    private static double ChipTransY(double t, double start, double land)
    {
        if (t < start) return -80;
        if (t >= land)
        {
            double since = t - land;
            if (since < 0.22)
            {
                double p = since / 0.22;
                return -Math.Sin(p * Math.PI) * 1.6 * (1 - p);
            }
            return 0;
        }
        double rel = (t - start) / (land - start);
        double eased = rel * rel; // easeInQuad
        return -80 * (1 - eased);
    }

    private static (double x, double y) Shake(double elapsed, double duration, double amp)
    {
        if (elapsed <= 0 || elapsed >= duration) return (0, 0);
        double p = elapsed / duration;
        double decay = Math.Exp(-p * 5);
        double x = Math.Sin(p * 32) * amp * decay;
        double y = Math.Sin(p * 32 * 1.3 + 0.5) * amp * 0.4 * decay;
        return (x, y);
    }

    private static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;
    private static double EaseOutCubic(double t) { var u = t - 1; return u * u * u + 1; }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Skip splash on click
        Finish();
    }

    private void Finish()
    {
        if (_finished) return;
        _finished = true;
        CompositionTarget.Rendering -= OnRender;
        Close();
    }

    private readonly record struct Particle(double Angle, double Speed, double Size, double Life, Brush Hue);
}

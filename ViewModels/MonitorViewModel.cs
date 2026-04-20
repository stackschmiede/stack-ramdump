using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RamDump.Models;
using RamDump.Services;
using System.Collections.ObjectModel;

namespace RamDump.ViewModels;

public partial class MonitorViewModel : ObservableObject, IDisposable
{
    private const int HistorySize = 60;

    private readonly HardwareSensorService _sensor = new();
    private System.Threading.Timer? _timer;

    private readonly double[] _cpuBuf = new double[HistorySize];
    private readonly double[] _gpuBuf = new double[HistorySize];
    private readonly double[] _diskReadBuf = new double[HistorySize];
    private readonly double[] _diskWriteBuf = new double[HistorySize];
    private readonly double[] _netDownBuf = new double[HistorySize];
    private readonly double[] _netUpBuf = new double[HistorySize];
    private readonly double[] _ramPctBuf = new double[HistorySize];

    // CPU
    [ObservableProperty] private double? _cpuUsage;
    [ObservableProperty] private double? _cpuTemp;
    [ObservableProperty] private double? _cpuFreqMHz;
    [ObservableProperty] private double _cpuAvg;
    [ObservableProperty] private IReadOnlyList<double> _cpuHistory = Array.Empty<double>();
    [ObservableProperty] private ObservableCollection<CoreUsageViewModel> _cores = [];

    // GPU
    [ObservableProperty] private double? _gpuUsage;
    [ObservableProperty] private double? _gpuTemp;
    [ObservableProperty] private double? _gpuClockMHz;
    [ObservableProperty] private double? _gpuVramUsedGb;
    [ObservableProperty] private double? _gpuVramTotalGb;
    [ObservableProperty] private double _gpuAvg;
    [ObservableProperty] private IReadOnlyList<double> _gpuHistory = Array.Empty<double>();

    // Disk (B/s → MB/s im VM)
    [ObservableProperty] private double? _diskReadMBps;
    [ObservableProperty] private double? _diskWriteMBps;
    [ObservableProperty] private double? _diskTemp;
    [ObservableProperty] private double _diskScaleMax = 50.0;
    [ObservableProperty] private IReadOnlyList<double> _diskReadHistory = Array.Empty<double>();
    [ObservableProperty] private IReadOnlyList<double> _diskWriteHistory = Array.Empty<double>();

    // Network (B/s → Mbit/s im VM)
    [ObservableProperty] private double? _netUpMbitps;
    [ObservableProperty] private double? _netDownMbitps;
    [ObservableProperty] private double _netScaleMax = 100.0;
    [ObservableProperty] private IReadOnlyList<double> _netDownHistory = Array.Empty<double>();
    [ObservableProperty] private IReadOnlyList<double> _netUpHistory = Array.Empty<double>();

    // RAM (aus MainViewModel.SystemMemory synchronisiert)
    [ObservableProperty] private SystemMemoryInfo _systemMemory = new();
    [ObservableProperty] private IReadOnlyList<double> _ramPctHistory = Array.Empty<double>();

    // Ticker
    [ObservableProperty] private string _uptimeText = "—";
    [ObservableProperty] private string _topProcessName = "—";
    [ObservableProperty] private string _topProcessSize = "—";

    [ObservableProperty] private string _lastUpdateTime = "—";
    [ObservableProperty] private bool _hardwareAvailable;

    [ObservableProperty] private bool _isActive;
    [ObservableProperty] private bool _isPaused;
    [ObservableProperty] private int _refreshIntervalSeconds = 2;

    partial void OnIsActiveChanged(bool value) => UpdateTimer();
    partial void OnIsPausedChanged(bool value) => UpdateTimer();
    partial void OnRefreshIntervalSecondsChanged(int value) => UpdateTimer();
    partial void OnSystemMemoryChanged(SystemMemoryInfo value) => PushRamPct(value.UsagePercent);

    [RelayCommand]
    private void TogglePause() => IsPaused = !IsPaused;

    private void UpdateTimer()
    {
        _timer?.Dispose();
        _timer = null;

        if (!IsActive || IsPaused) return;

        var interval = TimeSpan.FromSeconds(Math.Max(1, RefreshIntervalSeconds));
        _timer = new System.Threading.Timer(_ => Refresh(), null, TimeSpan.Zero, interval);
    }

    private void Refresh()
    {
        if (!_sensor.IsAvailable && HardwareAvailable)
        {
            _sensor.TryInitialize();
            HardwareAvailable = _sensor.IsAvailable;
        }

        var snap = _sensor.GetSnapshot();
        ApplySnapshot(snap);
    }

    public void EnsureInitialized()
    {
        if (HardwareAvailable || _sensor.IsAvailable) return;
        _sensor.TryInitialize();
        HardwareAvailable = _sensor.IsAvailable;
    }

    private void ApplySnapshot(SensorSnapshot snap)
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            CpuUsage = snap.CpuUsage;
            CpuTemp = snap.CpuTemp;
            CpuFreqMHz = snap.CpuFreqMHz;
            PushCpu(snap.CpuUsage ?? 0);

            SyncCores(snap.PerCore);

            GpuUsage = snap.GpuUsage;
            GpuTemp = snap.GpuTemp;
            GpuClockMHz = snap.GpuClockMHz;
            GpuVramUsedGb = snap.GpuVramUsed.HasValue ? snap.GpuVramUsed.Value / 1073741824.0 : null;
            GpuVramTotalGb = snap.GpuVramTotal.HasValue ? snap.GpuVramTotal.Value / 1073741824.0 : null;
            PushGpu(snap.GpuUsage ?? 0);

            DiskReadMBps = snap.DiskReadBps.HasValue ? snap.DiskReadBps.Value / 1_048_576.0 : null;
            DiskWriteMBps = snap.DiskWriteBps.HasValue ? snap.DiskWriteBps.Value / 1_048_576.0 : null;
            DiskTemp = snap.DiskTemp;
            PushDisk(DiskReadMBps ?? 0, DiskWriteMBps ?? 0);

            NetUpMbitps = snap.NetUpBps.HasValue ? snap.NetUpBps.Value * 8.0 / 1_000_000.0 : null;
            NetDownMbitps = snap.NetDownBps.HasValue ? snap.NetDownBps.Value * 8.0 / 1_000_000.0 : null;
            PushNet(NetDownMbitps ?? 0, NetUpMbitps ?? 0);

            var up = TimeSpan.FromMilliseconds(Environment.TickCount64);
            UptimeText = $"{(int)up.TotalDays} d {up.Hours} h {up.Minutes} m";

            LastUpdateTime = snap.Timestamp.ToString("HH:mm:ss");
        });
    }

    private void SyncCores(IReadOnlyList<double?> values)
    {
        while (Cores.Count < values.Count)
            Cores.Add(new CoreUsageViewModel(Cores.Count + 1));
        while (Cores.Count > values.Count)
            Cores.RemoveAt(Cores.Count - 1);

        for (int i = 0; i < values.Count; i++)
            Cores[i].Percent = values[i] ?? 0;
    }

    private void PushCpu(double v)
    {
        Shift(_cpuBuf, v);
        CpuHistory = _cpuBuf.ToArray();
        CpuAvg = _cpuBuf.Average();
    }

    private void PushGpu(double v)
    {
        Shift(_gpuBuf, v);
        GpuHistory = _gpuBuf.ToArray();
        GpuAvg = _gpuBuf.Average();
    }

    private void PushDisk(double read, double write)
    {
        Shift(_diskReadBuf, read);
        Shift(_diskWriteBuf, write);

        // Auto-scale: max der letzten Werte, gerundet auf nächste sinnvolle Stufe
        var maxSeen = Math.Max(_diskReadBuf.Max(), _diskWriteBuf.Max());
        DiskScaleMax = maxSeen < 10 ? 10 : maxSeen < 50 ? 50 : maxSeen < 200 ? 200 : maxSeen < 1000 ? 1000 : maxSeen * 1.2;

        // Normalisiere auf 0..100 für Sparkline (Parameter=100 in XAML)
        DiskReadHistory = _diskReadBuf.Select(v => v / DiskScaleMax * 100).ToArray();
        DiskWriteHistory = _diskWriteBuf.Select(v => v / DiskScaleMax * 100).ToArray();
    }

    private void PushNet(double down, double up)
    {
        Shift(_netDownBuf, down);
        Shift(_netUpBuf, up);

        var maxSeen = Math.Max(_netDownBuf.Max(), _netUpBuf.Max());
        NetScaleMax = maxSeen < 10 ? 10 : maxSeen < 100 ? 100 : maxSeen < 1000 ? 1000 : maxSeen * 1.2;

        NetDownHistory = _netDownBuf.Select(v => v / NetScaleMax * 100).ToArray();
        NetUpHistory = _netUpBuf.Select(v => v / NetScaleMax * 100).ToArray();
    }

    private void PushRamPct(double pct)
    {
        Shift(_ramPctBuf, pct);
        RamPctHistory = _ramPctBuf.ToArray();
    }

    private static void Shift(double[] buf, double next)
    {
        Array.Copy(buf, 1, buf, 0, buf.Length - 1);
        buf[^1] = next;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _sensor.Dispose();
    }
}

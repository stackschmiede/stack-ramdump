using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RamDump.Services;
using System.Collections.ObjectModel;

namespace RamDump.ViewModels;

public partial class MonitorViewModel : ObservableObject, IDisposable
{
    private readonly HardwareSensorService _sensor = new();
    private System.Threading.Timer? _timer;

    // CPU
    [ObservableProperty] private double? _cpuUsage;
    [ObservableProperty] private double? _cpuTemp;
    [ObservableProperty] private double? _cpuFreqMHz;
    [ObservableProperty] private ObservableCollection<CoreUsageViewModel> _cores = [];

    // GPU
    [ObservableProperty] private double? _gpuUsage;
    [ObservableProperty] private double? _gpuTemp;
    [ObservableProperty] private double? _gpuClockMHz;
    [ObservableProperty] private double? _gpuVramUsedGb;
    [ObservableProperty] private double? _gpuVramTotalGb;

    // Disk (B/s → MB/s im VM)
    [ObservableProperty] private double? _diskReadMBps;
    [ObservableProperty] private double? _diskWriteMBps;
    [ObservableProperty] private double? _diskTemp;

    // Network (B/s → Mbit/s im VM)
    [ObservableProperty] private double? _netUpMbitps;
    [ObservableProperty] private double? _netDownMbitps;

    [ObservableProperty] private string _lastUpdateTime = "—";
    [ObservableProperty] private bool _hardwareAvailable;

    [ObservableProperty] private bool _isActive;
    [ObservableProperty] private bool _isPaused;
    [ObservableProperty] private int _refreshIntervalSeconds = 2;

    partial void OnIsActiveChanged(bool value) => UpdateTimer();
    partial void OnIsPausedChanged(bool value) => UpdateTimer();
    partial void OnRefreshIntervalSecondsChanged(int value) => UpdateTimer();

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
            // LHM not yet initialized — lazy init on first Monitor-Tab open
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

            SyncCores(snap.PerCore);

            GpuUsage = snap.GpuUsage;
            GpuTemp = snap.GpuTemp;
            GpuClockMHz = snap.GpuClockMHz;
            GpuVramUsedGb = snap.GpuVramUsed.HasValue ? snap.GpuVramUsed.Value / 1073741824.0 : null;
            GpuVramTotalGb = snap.GpuVramTotal.HasValue ? snap.GpuVramTotal.Value / 1073741824.0 : null;

            DiskReadMBps = snap.DiskReadBps.HasValue ? snap.DiskReadBps.Value / 1_048_576.0 : null;
            DiskWriteMBps = snap.DiskWriteBps.HasValue ? snap.DiskWriteBps.Value / 1_048_576.0 : null;
            DiskTemp = snap.DiskTemp;

            NetUpMbitps = snap.NetUpBps.HasValue ? snap.NetUpBps.Value * 8.0 / 1_000_000.0 : null;
            NetDownMbitps = snap.NetDownBps.HasValue ? snap.NetDownBps.Value * 8.0 / 1_000_000.0 : null;

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

    public void Dispose()
    {
        _timer?.Dispose();
        _sensor.Dispose();
    }
}

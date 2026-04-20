using CommunityToolkit.Mvvm.ComponentModel;
using System.Management;
using System.Runtime.InteropServices;

namespace RamDump.ViewModels;

public partial class AboutViewModel : ObservableObject, IDisposable
{
    [ObservableProperty] private string _cpuName = "—";
    [ObservableProperty] private string _gpuName = "—";
    [ObservableProperty] private string _ramTotal = "—";
    [ObservableProperty] private string _osDescription = "—";
    [ObservableProperty] private string _uptime = "—";

    public string AppVersion { get; } =
        System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

    private System.Threading.Timer? _uptimeTimer;
    private bool _loaded;

    public void LoadSystemInfo()
    {
        if (_loaded) return;
        _loaded = true;

        Task.Run(() =>
        {
            var cpu = WmiString("SELECT Name FROM Win32_Processor", "Name");
            var gpu = WmiString("SELECT Name FROM Win32_VideoController", "Name");
            var ramBytes = WmiUlong("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem", "TotalPhysicalMemory");

            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                CpuName = cpu ?? "—";
                GpuName = gpu ?? "—";
                RamTotal = ramBytes.HasValue ? $"{ramBytes.Value / 1_073_741_824.0:F1} GB" : "—";
                OsDescription = RuntimeInformation.OSDescription;
                RefreshUptime();
            });
        });

        _uptimeTimer = new System.Threading.Timer(
            _ => System.Windows.Application.Current?.Dispatcher.Invoke(RefreshUptime),
            null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    private void RefreshUptime()
    {
        var t = TimeSpan.FromMilliseconds(Environment.TickCount64);
        Uptime = $"{(int)t.TotalDays}d {t.Hours}h {t.Minutes}m";
    }

    private static string? WmiString(string query, string prop)
    {
        try
        {
            using var s = new ManagementObjectSearcher(query);
            foreach (ManagementObject o in s.Get())
                return o[prop]?.ToString()?.Trim();
        }
        catch { }
        return null;
    }

    private static ulong? WmiUlong(string query, string prop)
    {
        try
        {
            using var s = new ManagementObjectSearcher(query);
            foreach (ManagementObject o in s.Get())
                if (o[prop] is ulong v) return v;
        }
        catch { }
        return null;
    }

    public void Dispose() => _uptimeTimer?.Dispose();
}

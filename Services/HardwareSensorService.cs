using LibreHardwareMonitor.Hardware;
using System.Diagnostics;

namespace RamDump.Services;

public sealed class HardwareSensorService : IDisposable
{
    private Computer? _computer;
    private PerformanceCounter? _cpuFallback;

    public bool IsAvailable { get; private set; }

    public void TryInitialize()
    {
        try
        {
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true,
                IsStorageEnabled = true,
                IsNetworkEnabled = true
            };
            _computer.Open();
            IsAvailable = true;
        }
        catch
        {
            IsAvailable = false;
            InitializeFallback();
        }
    }

    private void InitializeFallback()
    {
        try
        {
            _cpuFallback = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpuFallback.NextValue(); // first call always returns 0
        }
        catch { }
    }

    public SensorSnapshot GetSnapshot()
    {
        if (!IsAvailable)
            return GetFallbackSnapshot();

        double? cpuUsage = null, cpuTemp = null, cpuFreqMHz = null;
        var perCore = new List<(int index, double? value)>();
        double? gpuUsage = null, gpuTemp = null, gpuClockMHz = null;
        long? gpuVramUsed = null, gpuVramTotal = null;
        double? diskReadBps = null, diskWriteBps = null, diskTemp = null;
        double? netUpBps = null, netDownBps = null;

        foreach (var hardware in _computer!.Hardware)
        {
            hardware.Update();
            foreach (var sub in hardware.SubHardware)
                sub.Update();

            switch (hardware.HardwareType)
            {
                case HardwareType.Cpu:
                    ReadCpu(hardware, ref cpuUsage, ref cpuTemp, ref cpuFreqMHz, perCore);
                    break;
                case HardwareType.GpuNvidia:
                case HardwareType.GpuAmd:
                case HardwareType.GpuIntel:
                    ReadGpu(hardware, ref gpuUsage, ref gpuTemp, ref gpuClockMHz, ref gpuVramUsed, ref gpuVramTotal);
                    break;
                case HardwareType.Storage:
                    ReadStorage(hardware, ref diskReadBps, ref diskWriteBps, ref diskTemp);
                    break;
                case HardwareType.Network:
                    ReadNetwork(hardware, ref netUpBps, ref netDownBps);
                    break;
            }
        }

        var sortedCores = perCore
            .OrderBy(c => c.index)
            .Select(c => c.value)
            .ToList()
            .AsReadOnly();

        return new SensorSnapshot(
            cpuUsage, cpuTemp, cpuFreqMHz, sortedCores,
            gpuUsage, gpuTemp, gpuClockMHz, gpuVramUsed, gpuVramTotal,
            diskReadBps, diskWriteBps, diskTemp,
            netUpBps, netDownBps,
            DateTime.Now);
    }

    private SensorSnapshot GetFallbackSnapshot()
    {
        double? cpuUsage = null;
        try { cpuUsage = _cpuFallback?.NextValue(); } catch { }
        return new SensorSnapshot(
            cpuUsage, null, null, Array.Empty<double?>(),
            null, null, null, null, null,
            null, null, null,
            null, null,
            DateTime.Now);
    }

    private static void ReadCpu(IHardware hw, ref double? usage, ref double? temp, ref double? freqMHz,
        List<(int index, double? value)> perCore)
    {
        foreach (var s in hw.Sensors)
        {
            switch (s.SensorType)
            {
                case SensorType.Load when s.Name == "CPU Total":
                    usage = (double?)s.Value;
                    break;
                case SensorType.Load when s.Name.StartsWith("CPU Core #") &&
                    int.TryParse(s.Name["CPU Core #".Length..], out var idx):
                    perCore.Add((idx, (double?)s.Value));
                    break;
                case SensorType.Temperature when s.Name.Contains("Package"):
                    temp = (double?)s.Value;
                    break;
                case SensorType.Temperature when temp == null:
                    temp = (double?)s.Value;
                    break;
                case SensorType.Clock when freqMHz == null && s.Name.StartsWith("CPU Core"):
                    freqMHz = (double?)s.Value;
                    break;
            }
        }
    }

    private static void ReadGpu(IHardware hw, ref double? usage, ref double? temp, ref double? clockMHz,
        ref long? vramUsed, ref long? vramTotal)
    {
        foreach (var s in hw.Sensors)
        {
            switch (s.SensorType)
            {
                case SensorType.Load when s.Name == "GPU Core":
                    usage = (double?)s.Value;
                    break;
                case SensorType.Temperature when s.Name == "GPU Core":
                    temp = (double?)s.Value;
                    break;
                case SensorType.Clock when s.Name == "GPU Core":
                    clockMHz = (double?)s.Value;
                    break;
                case SensorType.SmallData when s.Name == "GPU Memory Used":
                    vramUsed = s.Value.HasValue ? (long)(s.Value.Value * 1024 * 1024) : null;
                    break;
                case SensorType.SmallData when s.Name == "GPU Memory Total":
                    vramTotal = s.Value.HasValue ? (long)(s.Value.Value * 1024 * 1024) : null;
                    break;
            }
        }
    }

    private static void ReadStorage(IHardware hw, ref double? readBps, ref double? writeBps, ref double? temp)
    {
        foreach (var s in hw.Sensors)
        {
            switch (s.SensorType)
            {
                case SensorType.Throughput when s.Name.Contains("Read"):
                    readBps = (double?)s.Value;
                    break;
                case SensorType.Throughput when s.Name.Contains("Write"):
                    writeBps = (double?)s.Value;
                    break;
                case SensorType.Temperature when temp == null:
                    temp = (double?)s.Value;
                    break;
            }
        }
    }

    private static void ReadNetwork(IHardware hw, ref double? upBps, ref double? downBps)
    {
        foreach (var s in hw.Sensors)
        {
            switch (s.SensorType)
            {
                case SensorType.Throughput when s.Name.Contains("Upload"):
                    upBps = (upBps ?? 0) + ((double?)s.Value ?? 0);
                    break;
                case SensorType.Throughput when s.Name.Contains("Download"):
                    downBps = (downBps ?? 0) + ((double?)s.Value ?? 0);
                    break;
            }
        }
    }

    public void Dispose()
    {
        _computer?.Close();
        _cpuFallback?.Dispose();
    }
}

public record SensorSnapshot(
    double? CpuUsage,
    double? CpuTemp,
    double? CpuFreqMHz,
    IReadOnlyList<double?> PerCore,
    double? GpuUsage,
    double? GpuTemp,
    double? GpuClockMHz,
    long? GpuVramUsed,
    long? GpuVramTotal,
    double? DiskReadBps,
    double? DiskWriteBps,
    double? DiskTemp,
    double? NetUpBps,
    double? NetDownBps,
    DateTime Timestamp);

using System.Diagnostics;
using RamDump.Models;

namespace RamDump.Services;

public class MemoryCleanupService
{
    private static readonly HashSet<string> BlockedProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "csrss", "lsass", "smss", "svchost", "dwm", "wininit", "winlogon",
        "services", "System", "Idle", "Registry", "Memory Compression",
        "fontdrvhost", "sihost", "taskhostw", "ctfmon", "conhost",
        "SecurityHealthService", "MsMpEng", "NisSrv",
    };

    public static async Task<CleanupResult> TrimAllWorkingSetsAsync()
    {
        return await Task.Run(() =>
        {
            var memBefore = MemoryQueryService.GetSystemMemory();
            int trimmed = 0;
            int failed = 0;

            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    if (BlockedProcesses.Contains(proc.ProcessName))
                        continue;

                    var handle = NativeMethods.OpenProcess(
                        NativeMethods.PROCESS_QUERY_INFORMATION | NativeMethods.PROCESS_SET_QUOTA,
                        false, proc.Id);

                    if (handle == IntPtr.Zero)
                    {
                        failed++;
                        continue;
                    }

                    try
                    {
                        if (NativeMethods.EmptyWorkingSet(handle))
                            trimmed++;
                        else
                            failed++;
                    }
                    finally
                    {
                        NativeMethods.CloseHandle(handle);
                    }
                }
                catch
                {
                    failed++;
                }
                finally
                {
                    proc.Dispose();
                }
            }

            var memAfter = MemoryQueryService.GetSystemMemory();
            long freed = memAfter.AvailablePhysical - memBefore.AvailablePhysical;
            if (freed < 0) freed = 0;

            return new CleanupResult
            {
                ProcessesTrimmed = trimmed,
                ProcessesFailed = failed,
                BytesFreed = freed,
                MemBefore = memBefore.UsedPhysical,
                MemAfter = memAfter.UsedPhysical,
                Summary = $"{FormatBeforeAfter(memBefore.UsedPhysical, memAfter.UsedPhysical)} — {trimmed} Prozesse getrimmt",
            };
        });
    }

    public static async Task<CleanupResult> ClearStandbyListAsync()
    {
        return await Task.Run(() =>
        {
            var memBefore = MemoryQueryService.GetSystemMemory();

            int command = NativeMethods.MemoryPurgeStandbyList;
            int result = NativeMethods.NtSetSystemInformation(
                NativeMethods.SystemMemoryListInformation,
                ref command,
                sizeof(int));

            var memAfter = MemoryQueryService.GetSystemMemory();
            long freed = memAfter.AvailablePhysical - memBefore.AvailablePhysical;
            if (freed < 0) freed = 0;

            bool success = result == 0;
            return new CleanupResult
            {
                StandbyCleared = success,
                BytesFreed = freed,
                MemBefore = memBefore.UsedPhysical,
                MemAfter = memAfter.UsedPhysical,
                Summary = success
                    ? $"{FormatBeforeAfter(memBefore.UsedPhysical, memAfter.UsedPhysical)} — Standby geleert"
                    : "Standby-Liste konnte nicht geleert werden (Admin-Rechte nötig)",
            };
        });
    }

    public static async Task<CleanupResult> FullCleanupAsync()
    {
        var memBefore = MemoryQueryService.GetSystemMemory();

        var trimResult = await TrimAllWorkingSetsAsync();
        await Task.Delay(500);
        var standbyResult = await ClearStandbyListAsync();

        var memAfter = MemoryQueryService.GetSystemMemory();
        long totalFreed = trimResult.BytesFreed + standbyResult.BytesFreed;

        return new CleanupResult
        {
            ProcessesTrimmed = trimResult.ProcessesTrimmed,
            ProcessesFailed = trimResult.ProcessesFailed,
            BytesFreed = totalFreed,
            StandbyCleared = standbyResult.StandbyCleared,
            MemBefore = memBefore.UsedPhysical,
            MemAfter = memAfter.UsedPhysical,
            Summary = $"{FormatBeforeAfter(memBefore.UsedPhysical, memAfter.UsedPhysical)} — {trimResult.ProcessesTrimmed} getrimmt + Standby geleert",
        };
    }

    public static async Task<CleanupResult> KillProcessAsync(int pid)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var proc = Process.GetProcessById(pid);
                if (BlockedProcesses.Contains(proc.ProcessName))
                {
                    return new CleanupResult
                    {
                        ProcessesFailed = 1,
                        Summary = $"{proc.ProcessName} ist ein geschützter Systemprozess",
                    };
                }

                string name = proc.ProcessName;
                proc.Kill();
                proc.WaitForExit(2000);
                return new CleanupResult
                {
                    ProcessesTrimmed = 1,
                    Summary = $"{name} (PID {pid}) beendet",
                };
            }
            catch (ArgumentException)
            {
                return new CleanupResult
                {
                    ProcessesFailed = 1,
                    Summary = $"Prozess {pid} nicht mehr vorhanden",
                };
            }
            catch (Exception ex)
            {
                return new CleanupResult
                {
                    ProcessesFailed = 1,
                    Summary = $"Konnte PID {pid} nicht beenden: {ex.Message}",
                };
            }
        });
    }

    public static async Task<CleanupResult> TrimProcessAsync(int pid)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var proc = Process.GetProcessById(pid);
                if (BlockedProcesses.Contains(proc.ProcessName))
                {
                    return new CleanupResult
                    {
                        Summary = $"{proc.ProcessName} ist ein geschützter Systemprozess",
                    };
                }

                var handle = NativeMethods.OpenProcess(
                    NativeMethods.PROCESS_QUERY_INFORMATION | NativeMethods.PROCESS_SET_QUOTA,
                    false, pid);

                if (handle == IntPtr.Zero)
                {
                    return new CleanupResult
                    {
                        ProcessesFailed = 1,
                        Summary = $"Kein Zugriff auf Prozess {pid}",
                    };
                }

                try
                {
                    bool ok = NativeMethods.EmptyWorkingSet(handle);
                    return new CleanupResult
                    {
                        ProcessesTrimmed = ok ? 1 : 0,
                        ProcessesFailed = ok ? 0 : 1,
                        Summary = ok
                            ? $"{proc.ProcessName} (PID {pid}) getrimmt"
                            : $"{proc.ProcessName} konnte nicht getrimmt werden",
                    };
                }
                finally
                {
                    NativeMethods.CloseHandle(handle);
                }
            }
            catch (ArgumentException)
            {
                return new CleanupResult
                {
                    ProcessesFailed = 1,
                    Summary = $"Prozess {pid} nicht mehr vorhanden",
                };
            }
        });
    }

    private static string FormatBeforeAfter(long usedBefore, long usedAfter)
    {
        long freed = usedBefore - usedAfter;
        if (freed < 0) freed = 0;
        return $"{FormatBytes(usedBefore)} \u2192 {FormatBytes(usedAfter)} ({FormatBytes(freed)} frei)";
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 0) return "0 B";
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        int i = 0;
        while (value >= 1024 && i < units.Length - 1)
        {
            value /= 1024;
            i++;
        }
        return $"{value:F1} {units[i]}";
    }
}

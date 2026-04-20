using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using RamDump.Models;

namespace RamDump.Services;

public class MemoryQueryService
{
    private static readonly string WinDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Windows);

    private static readonly HashSet<string> KnownSystemNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "system", "idle", "registry", "smss", "csrss", "wininit", "winlogon", "services",
        "lsass", "svchost", "spoolsv", "dwm", "fontdrvhost", "conhost", "searchindexer",
        "runtimebroker", "taskhostw", "sihost", "ctfmon", "audiodg", "dllhost",
        "searchhost", "startmenuexperiencehost", "shellexperiencehost", "textinputhost",
        "securityhealthservice", "applicationframehost", "backgroundtaskhost",
        "mousocoreworker", "sppsvc", "ntoskrnl", "wuauclt", "msiexec", "wsmprovhost",
        "lsaiso", "wlanext", "wermgr", "winsat", "upfc", "memory compression",
    };

    private static bool IsSystemProcess(Process proc)
    {
        try
        {
            var path = proc.MainModule?.FileName ?? string.Empty;
            if (!string.IsNullOrEmpty(path))
                return path.StartsWith(WinDir, StringComparison.OrdinalIgnoreCase);
        }
        catch { }
        return KnownSystemNames.Contains(proc.ProcessName);
    }

    public static bool IsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static void EnableRequiredPrivileges()
    {
        EnablePrivilege(NativeMethods.SE_DEBUG_NAME);
        EnablePrivilege(NativeMethods.SE_PROF_SINGLE_PROCESS_NAME);
        EnablePrivilege(NativeMethods.SE_INCREASE_QUOTA_NAME);
    }

    public static SystemMemoryInfo GetSystemMemory()
    {
        var status = new NativeMethods.MemoryStatusEx { dwLength = (uint)Marshal.SizeOf<NativeMethods.MemoryStatusEx>() };
        NativeMethods.GlobalMemoryStatusEx(ref status);

        return new SystemMemoryInfo
        {
            TotalPhysical = (long)status.ullTotalPhys,
            AvailablePhysical = (long)status.ullAvailPhys,
            CachedBytes = GetCachedBytes(),
        };
    }

    public static List<ProcessMemoryInfo> GetProcesses()
    {
        var result = new List<ProcessMemoryInfo>();
        foreach (var proc in Process.GetProcesses())
        {
            try
            {
                var info = GetProcessMemoryDetails(proc);
                if (info != null)
                    result.Add(info);
            }
            catch
            {
                // Zugriff verweigert — überspringen
            }
            finally
            {
                proc.Dispose();
            }
        }

        return result;
    }

    private static ProcessMemoryInfo? GetProcessMemoryDetails(Process proc)
    {
        bool isSys = IsSystemProcess(proc);

        var handle = NativeMethods.OpenProcess(
            NativeMethods.PROCESS_QUERY_INFORMATION | NativeMethods.PROCESS_VM_READ,
            false, proc.Id);

        if (handle == IntPtr.Zero)
        {
            return new ProcessMemoryInfo
            {
                Pid = proc.Id,
                Name = proc.ProcessName,
                WorkingSet = proc.WorkingSet64,
                PrivateBytes = proc.PrivateMemorySize64,
                PeakWorkingSet = proc.PeakWorkingSet64,
                IsSystemProcess = isSys,
            };
        }

        try
        {
            var counters = new NativeMethods.ProcessMemoryCounters
            {
                cb = (uint)Marshal.SizeOf<NativeMethods.ProcessMemoryCounters>()
            };

            if (NativeMethods.GetProcessMemoryInfo(handle, out counters, counters.cb))
            {
                return new ProcessMemoryInfo
                {
                    Pid = proc.Id,
                    Name = proc.ProcessName,
                    WorkingSet = (long)(ulong)counters.WorkingSetSize,
                    PrivateBytes = (long)(ulong)counters.PagefileUsage,
                    PeakWorkingSet = (long)(ulong)counters.PeakWorkingSetSize,
                    IsSystemProcess = isSys,
                };
            }

            return new ProcessMemoryInfo
            {
                Pid = proc.Id,
                Name = proc.ProcessName,
                WorkingSet = proc.WorkingSet64,
                PrivateBytes = proc.PrivateMemorySize64,
                PeakWorkingSet = proc.PeakWorkingSet64,
                IsSystemProcess = isSys,
            };
        }
        finally
        {
            NativeMethods.CloseHandle(handle);
        }
    }

    private static void EnablePrivilege(string privilegeName)
    {
        if (!NativeMethods.OpenProcessToken(
                Process.GetCurrentProcess().Handle,
                NativeMethods.TOKEN_ADJUST_PRIVILEGES | NativeMethods.TOKEN_QUERY,
                out var tokenHandle))
            return;

        try
        {
            if (!NativeMethods.LookupPrivilegeValue(null, privilegeName, out var luid))
                return;

            var tp = new NativeMethods.TokenPrivileges
            {
                PrivilegeCount = 1,
                Privileges = new NativeMethods.LuidAndAttributes
                {
                    Luid = luid,
                    Attributes = NativeMethods.SE_PRIVILEGE_ENABLED,
                }
            };

            NativeMethods.AdjustTokenPrivileges(tokenHandle, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
        }
        finally
        {
            NativeMethods.CloseHandle(tokenHandle);
        }
    }

    private static long GetCachedBytes()
    {
        if (NativeMethods.GetPerformanceInfo(
                out var pi,
                (uint)Marshal.SizeOf<NativeMethods.PerformanceInformation>()))
        {
            return (long)(ulong)pi.SystemCache * (long)(ulong)pi.PageSize;
        }
        return 0;
    }
}

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
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

    // Cache IsSystem pro PID. Neue PIDs werden einmalig aufgelöst, Alte rausgeputzt.
    private static readonly ConcurrentDictionary<int, bool> IsSystemByPid = new();
    // Pfad-Cache pro PID (für IconService wiederverwendbar).
    private static readonly ConcurrentDictionary<int, string> PathByPid = new();

    public static string? GetCachedPath(int pid) =>
        PathByPid.TryGetValue(pid, out var p) ? p : null;

    private static string? TryQueryImagePath(IntPtr handle)
    {
        if (handle == IntPtr.Zero) return null;
        try
        {
            var sb = new StringBuilder(1024);
            uint size = (uint)sb.Capacity;
            return NativeMethods.QueryFullProcessImageName(handle, 0, sb, ref size)
                ? sb.ToString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool ResolveIsSystem(int pid, IntPtr handle, string processName)
    {
        // Namens-Shortcut: bekannte Systemprozesse ohne Pfad-Auflösung bejahen.
        if (KnownSystemNames.Contains(processName))
            return true;

        if (IsSystemByPid.TryGetValue(pid, out var cached))
            return cached;

        bool isSys = false;
        var path = TryQueryImagePath(handle);
        if (!string.IsNullOrEmpty(path))
        {
            PathByPid[pid] = path;
            isSys = path.StartsWith(WinDir, StringComparison.OrdinalIgnoreCase);
        }

        IsSystemByPid[pid] = isSys;
        return isSys;
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

        long cached = 0, commit = 0, pageFile = 0, pageSize = 4096;
        if (NativeMethods.GetPerformanceInfo(
                out var pi,
                (uint)Marshal.SizeOf<NativeMethods.PerformanceInformation>()))
        {
            pageSize = (long)(ulong)pi.PageSize;
            cached = (long)(ulong)pi.SystemCache * pageSize;
            commit = (long)(ulong)pi.CommitTotal * pageSize;
        }
        // PageFile: Total - Available aus MemoryStatusEx
        pageFile = (long)(status.ullTotalPageFile - status.ullAvailPageFile);

        return new SystemMemoryInfo
        {
            TotalPhysical = (long)status.ullTotalPhys,
            AvailablePhysical = (long)status.ullAvailPhys,
            CachedBytes = cached,
            CommitBytes = commit,
            PageFileBytes = pageFile,
            StandbyBytes = 0, // Windows-API liefert das nicht direkt ohne WMI
        };
    }

    public static List<ProcessMemoryInfo> GetProcesses()
    {
        var all = Process.GetProcesses();
        var result = new List<ProcessMemoryInfo>(all.Length);
        var alive = new HashSet<int>(all.Length);

        foreach (var proc in all)
        {
            alive.Add(proc.Id);
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

        // Prune caches für nicht mehr lebende PIDs — hält Cache klein.
        PruneCache(IsSystemByPid, alive);
        PruneCache(PathByPid, alive);

        return result;
    }

    private static void PruneCache<TValue>(ConcurrentDictionary<int, TValue> cache, HashSet<int> alive)
    {
        if (cache.Count <= alive.Count + 64) return; // günstig: nichts zu tun
        foreach (var pid in cache.Keys)
        {
            if (!alive.Contains(pid))
                cache.TryRemove(pid, out _);
        }
    }

    private static ProcessMemoryInfo? GetProcessMemoryDetails(Process proc)
    {
        int pid = proc.Id;
        string name = proc.ProcessName;

        var handle = NativeMethods.OpenProcess(
            NativeMethods.PROCESS_QUERY_INFORMATION | NativeMethods.PROCESS_VM_READ,
            false, pid);

        try
        {
            bool isSys = ResolveIsSystem(pid, handle, name);

            if (handle == IntPtr.Zero)
            {
                // Fallback: managed API (langsamer, aber robust gegen Zugriffsverweigerung).
                return new ProcessMemoryInfo
                {
                    Pid = pid,
                    Name = name,
                    WorkingSet = proc.WorkingSet64,
                    PrivateBytes = proc.PrivateMemorySize64,
                    PeakWorkingSet = proc.PeakWorkingSet64,
                    IsSystemProcess = isSys,
                };
            }

            var counters = new NativeMethods.ProcessMemoryCounters
            {
                cb = (uint)Marshal.SizeOf<NativeMethods.ProcessMemoryCounters>()
            };

            if (NativeMethods.GetProcessMemoryInfo(handle, out counters, counters.cb))
            {
                return new ProcessMemoryInfo
                {
                    Pid = pid,
                    Name = name,
                    WorkingSet = (long)(ulong)counters.WorkingSetSize,
                    PrivateBytes = (long)(ulong)counters.PagefileUsage,
                    PeakWorkingSet = (long)(ulong)counters.PeakWorkingSetSize,
                    IsSystemProcess = isSys,
                };
            }

            return new ProcessMemoryInfo
            {
                Pid = pid,
                Name = name,
                WorkingSet = proc.WorkingSet64,
                PrivateBytes = proc.PrivateMemorySize64,
                PeakWorkingSet = proc.PeakWorkingSet64,
                IsSystemProcess = isSys,
            };
        }
        finally
        {
            if (handle != IntPtr.Zero)
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

}

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace RamDump.Services;

public static class ProcessIconService
{
    private static readonly ConcurrentDictionary<string, BitmapSource?> Cache = new();

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    public static BitmapSource? GetIcon(int pid)
    {
        var path = GetMainModulePath(pid);
        if (path == null) return null;
        return Cache.GetOrAdd(path, TryExtractIcon);
    }

    private static string? GetMainModulePath(int pid)
    {
        // Cache-Treffer aus MemoryQueryService (dort per QueryFullProcessImageName aufgelöst)
        var cached = MemoryQueryService.GetCachedPath(pid);
        if (!string.IsNullOrEmpty(cached)) return cached;

        // Fallback: eigener Handle-Pfad — vermeidet Process.MainModule (teuer unter Last)
        var handle = NativeMethods.OpenProcess(
            NativeMethods.PROCESS_QUERY_INFORMATION | NativeMethods.PROCESS_VM_READ,
            false, pid);
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
        finally
        {
            NativeMethods.CloseHandle(handle);
        }
    }

    private static BitmapSource? TryExtractIcon(string path)
    {
        try
        {
            var icon = System.Drawing.Icon.ExtractAssociatedIcon(path);
            if (icon == null) return null;
            using var bmp = icon.ToBitmap();
            var hBitmap = bmp.GetHbitmap();
            try
            {
                var source = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap, IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                source.Freeze();
                return source;
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }
        catch
        {
            return null;
        }
    }
}

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
        try
        {
            using var proc = Process.GetProcessById(pid);
            return proc.MainModule?.FileName;
        }
        catch
        {
            return null;
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

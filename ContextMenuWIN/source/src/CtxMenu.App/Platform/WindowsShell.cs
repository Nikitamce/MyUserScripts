using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CtxMenu.Core.Model;
using CtxMenu.Core.Registry;
using CtxMenu.Core.Scanning;

namespace CtxMenu.App.Platform;

public sealed class WindowsFileInfo : IFileInfoProvider
{
    private readonly ConcurrentDictionary<string, FileMeta?> _cache = new(StringComparer.OrdinalIgnoreCase);
    public string WindowsDirectory { get; } = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

    public FileMeta? GetMeta(string path) => _cache.GetOrAdd(path, p =>
    {
        try
        {
            var full = ResolvePath(p);
            if (full == null) return null;
            var v = FileVersionInfo.GetVersionInfo(full);
            return new FileMeta(v.CompanyName, v.ProductName, v.FileDescription);
        }
        catch (Exception) { return null; }
    });

    /// <summary>Expands variables and resolves bare names against System32 / Windows / PATH.</summary>
    public static string? ResolvePath(string p)
    {
        var s = Environment.ExpandEnvironmentVariables(p.Trim().Trim('"'));
        if (File.Exists(s)) return s;
        if (!s.Contains('\\'))
        {
            foreach (var dir in new[] { Environment.SystemDirectory, Environment.GetFolderPath(Environment.SpecialFolder.Windows) }
                         .Concat((Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries)))
            {
                try
                {
                    var c = Path.Combine(dir, s);
                    if (File.Exists(c)) return c;
                    if (!Path.HasExtension(c) && File.Exists(c + ".exe")) return c + ".exe";
                }
                catch (ArgumentException) { }
            }
        }
        return null;
    }
}

public sealed class WindowsStringResolver : IStringResolver
{
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int SHLoadIndirectString(string pszSource, StringBuilder pszOutBuf, int cchOutBuf, IntPtr ppvReserved);

    private readonly ConcurrentDictionary<string, string> _cache = new();

    public string Resolve(string value) => _cache.GetOrAdd(value, v =>
    {
        try
        {
            var sb = new StringBuilder(1024);
            return SHLoadIndirectString(v, sb, sb.Capacity, IntPtr.Zero) == 0 ? sb.ToString() : v;
        }
        catch (Exception) { return v; }
    });
}

/// <summary>Extracts small icons from "path,index" locations; cached, frozen for cross-thread use.</summary>
public static class IconCache
{
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint ExtractIconEx(string file, int index, IntPtr[]? large, IntPtr[] small, uint n);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr h);

    private static readonly ConcurrentDictionary<string, ImageSource?> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static ImageSource? Get(string? location)
    {
        if (string.IsNullOrWhiteSpace(location)) return null;
        return Cache.GetOrAdd(location, Load);
    }

    private static ImageSource? Load(string location)
    {
        try
        {
            var (path, index) = CommandLine.ParseIconLocation(location);
            var full = WindowsFileInfo.ResolvePath(path);
            if (full == null) return null;
            if (full.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) || full.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(full);
                bmp.DecodePixelWidth = 32;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            var small = new IntPtr[1];
            var large = new IntPtr[1];
            if (ExtractIconEx(full, index, large, small, 1) == 0) return null;
            var h = large[0] != IntPtr.Zero ? large[0] : small[0];
            try
            {
                if (h == IntPtr.Zero) return null;
                var src = Imaging.CreateBitmapSourceFromHIcon(h, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                src.Freeze();
                return src;
            }
            finally
            {
                if (small[0] != IntPtr.Zero) DestroyIcon(small[0]);
                if (large[0] != IntPtr.Zero) DestroyIcon(large[0]);
            }
        }
        catch (Exception) { return null; }
    }
}

public static class SystemHelper
{
    public static bool IsAdmin
    {
        get
        {
            using var id = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    public static bool IsWindows11 => Environment.OSVersion.Version.Build >= 22000;

    public static string OsDescription => $"Windows {(IsWindows11 ? "11" : "10")} (build {Environment.OSVersion.Version.Build})";

    /// <summary>Starts an elevated copy of the app. Returns false if the user declined UAC.</summary>
    public static bool RelaunchElevated(string args = "")
    {
        try
        {
            var exe = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule!.FileName;
            Process.Start(new ProcessStartInfo(exe, args) { UseShellExecute = true, Verb = "runas" });
            return true;
        }
        catch (System.ComponentModel.Win32Exception) { return false; }
    }

    public static void RestartExplorer()
    {
        foreach (var p in Process.GetProcessesByName("explorer"))
        {
            try { p.Kill(); p.WaitForExit(5000); }
            catch (Exception) { /* already exiting */ }
            finally { p.Dispose(); }
        }
        // Winlogon usually restarts the shell itself; start it if it did not.
        Thread.Sleep(1500);
        if (Process.GetProcessesByName("explorer").Length == 0)
            Process.Start(new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe")) { UseShellExecute = true });
    }

    /// <summary>Opens regedit at the key (regedit reads LastKey on start).</summary>
    public static void OpenInRegedit(RegHive hive, string path)
    {
        using (var k = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Applets\Regedit"))
            k.SetValue("LastKey", "Computer\\" + RegPath.Full(hive, path));
        foreach (var p in Process.GetProcessesByName("regedit")) { try { p.Kill(); } catch (Exception) { } }
        Process.Start(new ProcessStartInfo("regedit.exe", "/m") { UseShellExecute = true });
    }

    public static void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
    }

    public static void SelectFile(string path)
    {
        if (File.Exists(path)) Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
        else if (Directory.Exists(path)) OpenFolder(path);
    }

    public static bool SystemUsesDarkTheme()
    {
        using var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return k?.GetValue("AppsUseLightTheme") is int v && v == 0;
    }
}

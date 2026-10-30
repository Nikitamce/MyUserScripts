using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using CtxMenu.App.Platform;
using CtxMenu.App.ViewModels;
using CtxMenu.Core.Localization;
using CtxMenu.Core.Model;
using WpfThemeMode = System.Windows.ThemeMode;

namespace CtxMenu.App;

public partial class App : Application
{
    private static AppTheme _theme = AppTheme.System;

    /// <summary>True when the effective theme is dark (explicit dark, or "system" with dark apps mode).</summary>
    public static bool IsDarkActive => _theme == AppTheme.Dark || (_theme == AppTheme.System && SystemHelper.SystemUsesDarkTheme());

    public static void ApplyTheme(AppTheme theme)
    {
        _theme = theme;
        if (Current == null) return;
        Current.ThemeMode = theme switch
        {
            AppTheme.Light => WpfThemeMode.Light,
            AppTheme.Dark => WpfThemeMode.Dark,
            _ => WpfThemeMode.System,
        };
        foreach (Window w in Current.Windows) ApplyTitleBar(w);
    }

    public static void ApplyTitleBar(Window w)
    {
        try
        {
            var hwnd = new WindowInteropHelper(w).Handle;
            if (hwnd == IntPtr.Zero) return;
            int dark = IsDarkActive ? 1 : 0;
            // DWMWA_USE_IMMERSIVE_DARK_MODE = 20 (19 on early Windows 10 builds)
            if (DwmSetWindowAttribute(hwnd, 20, ref dark, sizeof(int)) != 0)
                DwmSetWindowAttribute(hwnd, 19, ref dark, sizeof(int));
        }
        catch { /* cosmetic only */ }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnUnhandled;
        AppDomain.CurrentDomain.UnhandledException += (_, a) => LogCrash(a.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, a) => { LogCrash(a.Exception); a.SetObserved(); };

        var paths = AppPaths.Default();
        MainViewModel vm;
        try
        {
            vm = new MainViewModel(new WindowsRegistry(), paths);
        }
        catch (Exception ex)
        {
            LogCrash(ex);
            MessageBox.Show(ex.ToString(), "Context Menu Manager", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        ApplyTheme(vm.Settings.Theme);

        var window = new MainWindow { DataContext = vm };
        MainWindow = window;
        window.SourceInitialized += (_, _) => ApplyTitleBar(window);
        window.Show();

        var page = ParsePage(e.Args);
        if (page != null) vm.SelectPage(page.Value);
        _ = vm.RefreshAsync();
    }

    private static Page? ParsePage(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i].Equals("--page", StringComparison.OrdinalIgnoreCase) && Enum.TryParse<Page>(args[i + 1], true, out var p))
                return p;
        return null;
    }

    private void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogCrash(e.Exception);
        var msg = e.Exception is UnauthorizedAccessException ? e.Exception.Message : e.Exception.ToString();
        MessageBox.Show(msg, Loc.Get("Dlg_Error"), MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void LogCrash(Exception? ex)
    {
        if (ex == null) return;
        try
        {
            var p = AppPaths.Default();
            Directory.CreateDirectory(p.BaseDir);
            File.AppendAllText(Path.Combine(p.BaseDir, "error.log"), $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch { }
    }
}

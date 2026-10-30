using System.Windows;
using CtxMenu.Core.Localization;
using Microsoft.Win32;

namespace CtxMenu.App.ViewModels;

public static class Dialogs
{
    private static Window? Owner => Application.Current?.MainWindow;

    public static bool Confirm(string message, string? title = null) =>
        Show(message, title ?? Loc.Get("App_Title"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public static bool Warn(string message) =>
        Show(message, Loc.Get("Dlg_Warning"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

    public static void Error(string message) => Show(message, Loc.Get("Dlg_Error"), MessageBoxButton.OK, MessageBoxImage.Error);

    public static void Info(string message) => Show(message, Loc.Get("App_Title"), MessageBoxButton.OK, MessageBoxImage.Information);

    private static MessageBoxResult Show(string m, string t, MessageBoxButton b, MessageBoxImage i) =>
        Owner != null ? MessageBox.Show(Owner, m, t, b, i) : MessageBox.Show(m, t, b, i);

    public static string? OpenFile(string filter, string? title = null)
    {
        var d = new OpenFileDialog { Filter = filter, Title = title ?? "" , CheckFileExists = true };
        return d.ShowDialog(Owner) == true ? d.FileName : null;
    }

    public static string? SaveFile(string filter, string fileName)
    {
        var d = new SaveFileDialog { Filter = filter, FileName = fileName, AddExtension = true };
        return d.ShowDialog(Owner) == true ? d.FileName : null;
    }
}

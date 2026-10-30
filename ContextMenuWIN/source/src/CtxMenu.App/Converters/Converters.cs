using System.Globalization;
using System.Windows;
using System.Windows.Data;
using CtxMenu.Core.Localization;

namespace CtxMenu.App.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }
    public object Convert(object? value, Type t, object? p, CultureInfo c)
    {
        var b = value switch { bool x => x, string s => s.Length > 0, null => false, int i => i > 0, _ => true };
        return b ^ Invert ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>Visible when the bound enum value equals ConverterParameter.</summary>
public sealed class EqualsToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c) =>
        string.Equals(value?.ToString(), p?.ToString(), StringComparison.Ordinal) ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>Translates an enum value via "Prefix_Value" keys; ConverterParameter = prefix.</summary>
public sealed class EnumLocConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c) => Loc.Get($"{p}_{value}");
    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) => throw new NotSupportedException();
}

public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c) => value is bool b ? !b : value!;
    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) => value is bool b ? !b : value!;
}

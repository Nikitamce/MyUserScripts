using System.Windows;
using System.Windows.Controls;

namespace CtxMenu.App.Views;

/// <summary>Label + selectable read-only value. Collapses itself when the value is empty.</summary>
public sealed class PropRow : StackPanel
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(PropRow), new PropertyMetadata("", (d, e) => ((PropRow)d)._label.Text = (string)e.NewValue));
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(PropRow), new PropertyMetadata(null, (d, e) => ((PropRow)d).Update()));

    private readonly TextBlock _label = new() { FontSize = 12, Margin = new Thickness(0, 0, 0, 1) };
    private readonly TextBox _value = new()
    {
        IsReadOnly = true, BorderThickness = new Thickness(0), Background = System.Windows.Media.Brushes.Transparent,
        Padding = new Thickness(0), TextWrapping = TextWrapping.Wrap, MinHeight = 0,
    };

    public PropRow()
    {
        Margin = new Thickness(0, 0, 0, 10);
        _label.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
        _value.SetResourceReference(Control.ForegroundProperty, "TextFillColorPrimaryBrush");
        _value.SetResourceReference(TextBox.SelectionBrushProperty, "AccentFillColorDefaultBrush");
        // neutralise Fluent's TextBox chrome so it reads as plain selectable text
        _value.Style = null;
        _value.Template = BuildPlainTemplate();
        Children.Add(_label);
        Children.Add(_value);
        Update();
    }

    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Value { get => (string?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    private void Update()
    {
        _value.Text = Value ?? "";
        Visibility = string.IsNullOrWhiteSpace(Value) ? Visibility.Collapsed : Visibility.Visible;
    }

    private static ControlTemplate BuildPlainTemplate()
    {
        var t = new ControlTemplate(typeof(TextBox));
        var host = new FrameworkElementFactory(typeof(ScrollViewer)) { Name = "PART_ContentHost" };
        host.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
        host.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
        host.SetValue(FrameworkElement.MarginProperty, new Thickness(-2, 0, 0, 0));
        t.VisualTree = host;
        return t;
    }
}

using System.Windows.Data;
using System.Windows.Markup;
using CtxMenu.Core.Localization;

namespace CtxMenu.App.Views;

/// <summary>XAML shorthand for a live-updating localized string: Text="{v:T Nav_All}".</summary>
[MarkupExtensionReturnType(typeof(object))]
public sealed class TExtension : MarkupExtension
{
    public TExtension() { }
    public TExtension(string key) => Key = key;
    [ConstructorArgument("key")] public string Key { get; set; } = "";

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new Binding($"[{Key}]") { Source = Loc.Instance, Mode = BindingMode.OneWay }.ProvideValue(serviceProvider);
}

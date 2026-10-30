using System.ComponentModel;
using System.Globalization;

namespace CtxMenu.Core.Localization;

/// <summary>Tiny two-language string table. Bind in XAML as {Binding [Key], Source={x:Static loc:Loc.Instance}}.</summary>
public sealed class Loc : INotifyPropertyChanged
{
    public static Loc Instance { get; } = new();
    public static readonly string[] Languages = { "ru", "en" };

    private string _language = "ru";
    public event PropertyChangedEventHandler? PropertyChanged;
    public static event Action? LanguageChanged;

    public string Language
    {
        get => _language;
        set
        {
            var v = Languages.Contains(value) ? value : "ru";
            if (v == _language) return;
            _language = v;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
            LanguageChanged?.Invoke();
        }
    }

    public string this[string key] => Get(key);

    public static string Get(string key)
    {
        if (Strings.Table.TryGetValue(key, out var pair))
            return Instance._language == "en" ? pair.en : pair.ru;
        return key;
    }

    public static string Format(string key, params object?[] args) => string.Format(CultureInfo.CurrentCulture, Get(key), args);

    public static bool Has(string key) => Strings.Table.ContainsKey(key);
}

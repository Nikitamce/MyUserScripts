namespace CtxMenu.Core.Registry;

public enum RegHive { CurrentUser, LocalMachine }

public enum RegKind { String, ExpandString, DWord, QWord, MultiString, Binary, None }

/// <summary>A registry value. Data types: String/ExpandString = string, DWord = uint, QWord = ulong,
/// MultiString = string[], Binary/None = byte[].</summary>
public sealed record RegValue(RegKind Kind, object Data)
{
    public static RegValue Str(string s) => new(RegKind.String, s);
    public static RegValue Expand(string s) => new(RegKind.ExpandString, s);
    public static RegValue DWord(uint v) => new(RegKind.DWord, v);

    public string? AsString() => Data switch
    {
        string s => s,
        string[] m => string.Join(" ", m),
        uint u => u.ToString(),
        ulong ul => ul.ToString(),
        _ => null
    };

    public uint? AsDWord() => Data switch
    {
        uint u => u,
        ulong ul => (uint)ul,
        string s when uint.TryParse(s, out var p) => p,
        _ => null
    };
}

/// <summary>Minimal registry abstraction. Paths are relative to the hive root, separated by '\'.
/// Value name "" means the default value. Reads never throw for missing keys.</summary>
public interface IRegistry
{
    bool KeyExists(RegHive hive, string path);
    IReadOnlyList<string> GetSubKeyNames(RegHive hive, string path);
    IReadOnlyList<string> GetValueNames(RegHive hive, string path);
    RegValue? GetValue(RegHive hive, string path, string name);
    void CreateKey(RegHive hive, string path);
    void SetValue(RegHive hive, string path, string name, RegValue value);
    void DeleteValue(RegHive hive, string path, string name);
    void DeleteKeyTree(RegHive hive, string path);
}

public static class RegPath
{
    public static string Combine(params string[] parts) =>
        string.Join("\\", parts.Where(p => !string.IsNullOrEmpty(p)).Select(p => p.Trim('\\')));

    public static string Parent(string path)
    {
        var i = path.LastIndexOf('\\');
        return i < 0 ? "" : path[..i];
    }

    public static string Leaf(string path)
    {
        var i = path.LastIndexOf('\\');
        return i < 0 ? path : path[(i + 1)..];
    }

    public static string HiveName(RegHive hive) => hive == RegHive.CurrentUser ? "HKEY_CURRENT_USER" : "HKEY_LOCAL_MACHINE";
    public static string HiveShort(RegHive hive) => hive == RegHive.CurrentUser ? "HKCU" : "HKLM";
    public static string Full(RegHive hive, string path) => HiveName(hive) + "\\" + path;
}

public static class RegistryExtensions
{
    public static string? GetString(this IRegistry r, RegHive h, string path, string name = "") => r.GetValue(h, path, name)?.AsString();
    public static bool HasValue(this IRegistry r, RegHive h, string path, string name) => r.GetValue(h, path, name) != null;
}

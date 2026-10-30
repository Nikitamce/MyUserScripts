using System.Text;

namespace CtxMenu.Core.Registry;

/// <summary>Writes keys in regedit's .reg format (Windows Registry Editor Version 5.00).</summary>
public static class RegFileExporter
{
    public const string Header = "Windows Registry Editor Version 5.00";

    public static string Export(IRegistry reg, RegHive hive, string path, bool recursive = true)
    {
        var sb = new StringBuilder();
        sb.Append(Header).Append("\r\n\r\n");
        AppendKey(sb, reg, hive, path, recursive);
        return sb.ToString();
    }

    public static void AppendKey(StringBuilder sb, IRegistry reg, RegHive hive, string path, bool recursive, int depth = 0)
    {
        if (!reg.KeyExists(hive, path)) return;
        sb.Append('[').Append(RegPath.Full(hive, path)).Append("]\r\n");
        foreach (var name in reg.GetValueNames(hive, path).OrderBy(n => n == "" ? 0 : 1))
        {
            var v = reg.GetValue(hive, path, name);
            if (v is null) continue;
            sb.Append(name == "" ? "@" : Quote(name)).Append('=').Append(FormatValue(v)).Append("\r\n");
        }
        sb.Append("\r\n");
        if (recursive && depth < RegistryTree.MaxDepth)
            foreach (var c in reg.GetSubKeyNames(hive, path))
                AppendKey(sb, reg, hive, RegPath.Combine(path, c), true, depth + 1);
    }

    public static string Quote(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    public static string FormatValue(RegValue v) => v.Kind switch
    {
        RegKind.String => Quote((string)v.Data),
        RegKind.DWord => "dword:" + ((uint)v.Data).ToString("x8"),
        RegKind.ExpandString => "hex(2):" + Hex(Utf16Z((string)v.Data)),
        RegKind.MultiString => "hex(7):" + Hex(MultiBytes((string[])v.Data)),
        RegKind.QWord => "hex(b):" + Hex(BitConverter.GetBytes((ulong)v.Data)),
        RegKind.None => "hex(0):" + Hex((byte[])v.Data),
        _ => "hex:" + Hex((byte[])v.Data),
    };

    private static byte[] Utf16Z(string s) => Encoding.Unicode.GetBytes(s + "\0");

    private static byte[] MultiBytes(string[] items) =>
        Encoding.Unicode.GetBytes(string.Concat(items.Select(i => i + "\0")) + "\0");

    private static string Hex(byte[] bytes) => string.Join(",", bytes.Select(b => b.ToString("x2")));

    /// <summary>.reg files must be UTF-16 LE with BOM to be importable by regedit.</summary>
    public static void WriteFile(string file, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, content, new UnicodeEncoding(false, true));
    }
}

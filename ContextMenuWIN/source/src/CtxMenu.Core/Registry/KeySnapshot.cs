namespace CtxMenu.Core.Registry;

/// <summary>JSON-friendly representation of a registry value.</summary>
public sealed class SValue
{
    public string Name { get; set; } = "";
    public RegKind Kind { get; set; }
    public string? Str { get; set; }
    public string[]? Multi { get; set; }
    public ulong? Num { get; set; }
    public string? B64 { get; set; }

    public static SValue From(string name, RegValue v) => v.Data switch
    {
        string s => new SValue { Name = name, Kind = v.Kind, Str = s },
        string[] m => new SValue { Name = name, Kind = v.Kind, Multi = m },
        uint u => new SValue { Name = name, Kind = v.Kind, Num = u },
        ulong ul => new SValue { Name = name, Kind = v.Kind, Num = ul },
        byte[] b => new SValue { Name = name, Kind = v.Kind, B64 = Convert.ToBase64String(b) },
        _ => new SValue { Name = name, Kind = v.Kind, Str = v.Data?.ToString() }
    };

    public RegValue ToRegValue() => Kind switch
    {
        RegKind.String or RegKind.ExpandString => new RegValue(Kind, Str ?? ""),
        RegKind.DWord => new RegValue(Kind, (uint)(Num ?? 0)),
        RegKind.QWord => new RegValue(Kind, Num ?? 0UL),
        RegKind.MultiString => new RegValue(Kind, Multi ?? Array.Empty<string>()),
        _ => new RegValue(Kind, B64 is null ? Array.Empty<byte>() : Convert.FromBase64String(B64)),
    };
}

/// <summary>Full copy of a key subtree, used to undo deletions and to move keys.</summary>
public sealed class KeySnapshot
{
    public string Name { get; set; } = "";
    public List<SValue> Values { get; set; } = new();
    public List<KeySnapshot> Children { get; set; } = new();
}

public static class RegistryTree
{
    public const int MaxDepth = 32;

    public static KeySnapshot? Capture(IRegistry reg, RegHive hive, string path, int depth = 0)
    {
        if (!reg.KeyExists(hive, path)) return null;
        var snap = new KeySnapshot { Name = RegPath.Leaf(path) };
        foreach (var name in reg.GetValueNames(hive, path))
            if (reg.GetValue(hive, path, name) is { } v) snap.Values.Add(SValue.From(name, v));
        if (depth < MaxDepth)
            foreach (var child in reg.GetSubKeyNames(hive, path))
                if (Capture(reg, hive, RegPath.Combine(path, child), depth + 1) is { } c) snap.Children.Add(c);
        return snap;
    }

    public static void Restore(IRegistry reg, RegHive hive, string path, KeySnapshot snap)
    {
        reg.CreateKey(hive, path);
        foreach (var v in snap.Values) reg.SetValue(hive, path, v.Name, v.ToRegValue());
        foreach (var c in snap.Children) Restore(reg, hive, RegPath.Combine(path, c.Name), c);
    }

    /// <summary>Registry has no rename: copy the subtree then delete the source.</summary>
    public static void Rename(IRegistry reg, RegHive hive, string from, string to)
    {
        if (!reg.KeyExists(hive, from)) throw new InvalidOperationException($"Key not found: {from}");
        if (reg.KeyExists(hive, to)) throw new InvalidOperationException($"Target key already exists: {to}");
        var snap = Capture(reg, hive, from)!;
        Restore(reg, hive, to, snap);
        reg.DeleteKeyTree(hive, from);
    }
}

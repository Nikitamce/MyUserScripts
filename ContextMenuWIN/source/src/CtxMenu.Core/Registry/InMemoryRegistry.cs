namespace CtxMenu.Core.Registry;

/// <summary>In-memory registry used by tests and dry runs. Case-insensitive like the real one.</summary>
public sealed class InMemoryRegistry : IRegistry
{
    private sealed class Node
    {
        public string Name = "";
        public readonly Dictionary<string, Node> Children = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, RegValue> Values = new(StringComparer.OrdinalIgnoreCase);
    }

    private readonly Dictionary<RegHive, Node> _roots = new()
    {
        [RegHive.CurrentUser] = new Node(),
        [RegHive.LocalMachine] = new Node(),
    };

    /// <summary>When set, writes to these hives throw UnauthorizedAccessException (simulates non-admin).</summary>
    public HashSet<RegHive> ReadOnlyHives { get; } = new();

    private static string[] Split(string path) => path.Split('\\', StringSplitOptions.RemoveEmptyEntries);

    private Node? Find(RegHive hive, string path)
    {
        var n = _roots[hive];
        foreach (var part in Split(path))
            if (!n.Children.TryGetValue(part, out n!)) return null;
        return n;
    }

    private Node Ensure(RegHive hive, string path)
    {
        CheckWrite(hive);
        var n = _roots[hive];
        foreach (var part in Split(path))
        {
            if (!n.Children.TryGetValue(part, out var c))
                n.Children[part] = c = new Node { Name = part };
            n = c;
        }
        return n;
    }

    private void CheckWrite(RegHive hive)
    {
        if (ReadOnlyHives.Contains(hive)) throw new UnauthorizedAccessException($"Access to {hive} denied");
    }

    public bool KeyExists(RegHive hive, string path) => Find(hive, path) != null;
    public IReadOnlyList<string> GetSubKeyNames(RegHive hive, string path) => Find(hive, path)?.Children.Values.Select(c => c.Name).ToList() ?? new List<string>();
    public IReadOnlyList<string> GetValueNames(RegHive hive, string path) => Find(hive, path)?.Values.Keys.ToList() ?? new List<string>();
    public RegValue? GetValue(RegHive hive, string path, string name) => Find(hive, path) is { } n && n.Values.TryGetValue(name, out var v) ? v : null;
    public void CreateKey(RegHive hive, string path) => Ensure(hive, path);
    public void SetValue(RegHive hive, string path, string name, RegValue value) => Ensure(hive, path).Values[name] = value;

    public void DeleteValue(RegHive hive, string path, string name)
    {
        CheckWrite(hive);
        Find(hive, path)?.Values.Remove(name);
    }

    public void DeleteKeyTree(RegHive hive, string path)
    {
        CheckWrite(hive);
        var parent = Find(hive, RegPath.Parent(path));
        parent?.Children.Remove(RegPath.Leaf(path));
    }

    // Convenience for tests
    public InMemoryRegistry Set(RegHive hive, string path, string name, string value)
    {
        SetValue(hive, path, name, RegValue.Str(value));
        return this;
    }
}

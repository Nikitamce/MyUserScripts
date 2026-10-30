using System.Text.Json;
using System.Text.Json.Serialization;
using CtxMenu.Core.Registry;

namespace CtxMenu.Core.Editing;

public enum OpType { SetValue, DeleteValue, CreateKey, DeleteTree, RenameKey }

/// <summary>One primitive registry change with everything needed to revert it.</summary>
public sealed class ChangeOp
{
    public OpType Type { get; set; }
    public RegHive Hive { get; set; }
    public string Path { get; set; } = "";
    public string? Name { get; set; }
    public string? NewPath { get; set; }
    public SValue? OldValue { get; set; }
    public SValue? NewValue { get; set; }
    public KeySnapshot? Snapshot { get; set; }
    /// <summary>Topmost key that did not exist before the op and was created by it (deleted on undo).</summary>
    public string? CreatedRoot { get; set; }

    public override string ToString() => Type switch
    {
        OpType.SetValue => $"{RegPath.HiveShort(Hive)}\\{Path} : {Name} = {NewValue?.Str ?? NewValue?.Num?.ToString() ?? "…"}",
        OpType.DeleteValue => $"{RegPath.HiveShort(Hive)}\\{Path} : −{Name}",
        OpType.CreateKey => $"+ {RegPath.HiveShort(Hive)}\\{Path}",
        OpType.DeleteTree => $"− {RegPath.HiveShort(Hive)}\\{Path}",
        OpType.RenameKey => $"{RegPath.HiveShort(Hive)}\\{Path} → {RegPath.Leaf(NewPath ?? "")}",
        _ => Type.ToString()
    };
}

public sealed class ChangeSet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset Time { get; set; } = DateTimeOffset.Now;
    public string Description { get; set; } = "";
    public List<ChangeOp> Ops { get; set; } = new();
    public string? BackupFile { get; set; }
    public bool Undone { get; set; }
}

/// <summary>Persistent list of applied change sets (JSON on disk).</summary>
public sealed class ChangeJournal
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string? _file;
    public List<ChangeSet> Sets { get; private set; } = new();

    public ChangeJournal(string? file = null)
    {
        _file = file;
        if (file != null && File.Exists(file))
        {
            try { Sets = JsonSerializer.Deserialize<List<ChangeSet>>(File.ReadAllText(file), Json) ?? new(); }
            catch (JsonException)
            {
                // Keep the broken file for manual recovery, start fresh.
                File.Copy(file, file + ".broken-" + DateTime.Now.ToString("yyyyMMddHHmmss"), true);
                Sets = new();
            }
        }
    }

    public IEnumerable<ChangeSet> Active => Sets.Where(s => !s.Undone);
    public ChangeSet? LastActive => Sets.LastOrDefault(s => !s.Undone);

    public void Add(ChangeSet set) { Sets.Add(set); Save(); }

    public void Save()
    {
        if (_file == null) return;
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_file)!);
        var tmp = _file + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(Sets, Json));
        File.Move(tmp, _file, true);
    }

    public static string Serialize<T>(T obj) => JsonSerializer.Serialize(obj, Json);
    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Json);
}

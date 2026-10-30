using System.Text;
using CtxMenu.Core.Registry;

namespace CtxMenu.Core.Editing;

/// <summary>Collects registry operations of one user action. Every op is recorded with undo data
/// and the affected key is exported to the .reg backup before it is touched.</summary>
public sealed class Transaction
{
    private readonly IRegistry _reg;
    private readonly HashSet<string> _backedUp = new(StringComparer.OrdinalIgnoreCase);
    internal readonly List<ChangeOp> Ops = new();
    internal readonly StringBuilder Backup = new();

    internal Transaction(IRegistry reg) => _reg = reg;

    public IRegistry Registry => _reg;

    private void BackupKey(RegHive hive, string path)
    {
        if (!_backedUp.Add(hive + "|" + path)) return;
        RegFileExporter.AppendKey(Backup, _reg, hive, path, recursive: true);
    }

    private string? FindCreatedRoot(RegHive hive, string path)
    {
        string? created = null;
        var cur = path;
        while (!string.IsNullOrEmpty(cur) && !_reg.KeyExists(hive, cur))
        {
            created = cur;
            cur = RegPath.Parent(cur);
        }
        return created;
    }

    public void SetValue(RegHive hive, string path, string name, RegValue value)
    {
        BackupKey(hive, path);
        var old = _reg.GetValue(hive, path, name);
        var op = new ChangeOp
        {
            Type = OpType.SetValue, Hive = hive, Path = path, Name = name,
            OldValue = old is null ? null : SValue.From(name, old),
            NewValue = SValue.From(name, value),
            CreatedRoot = FindCreatedRoot(hive, path),
        };
        _reg.SetValue(hive, path, name, value);
        Ops.Add(op);
    }

    public void SetString(RegHive hive, string path, string name, string value) => SetValue(hive, path, name, RegValue.Str(value));

    public void DeleteValue(RegHive hive, string path, string name)
    {
        var old = _reg.GetValue(hive, path, name);
        if (old is null) return;
        BackupKey(hive, path);
        _reg.DeleteValue(hive, path, name);
        Ops.Add(new ChangeOp { Type = OpType.DeleteValue, Hive = hive, Path = path, Name = name, OldValue = SValue.From(name, old) });
    }

    public void CreateKey(RegHive hive, string path)
    {
        var root = FindCreatedRoot(hive, path);
        if (root is null) return;
        _reg.CreateKey(hive, path);
        Ops.Add(new ChangeOp { Type = OpType.CreateKey, Hive = hive, Path = path, CreatedRoot = root });
    }

    public void DeleteTree(RegHive hive, string path)
    {
        var snap = RegistryTree.Capture(_reg, hive, path);
        if (snap is null) return;
        BackupKey(hive, path);
        _reg.DeleteKeyTree(hive, path);
        Ops.Add(new ChangeOp { Type = OpType.DeleteTree, Hive = hive, Path = path, Snapshot = snap });
    }

    public void RenameKey(RegHive hive, string from, string to)
    {
        BackupKey(hive, from);
        RegistryTree.Rename(_reg, hive, from, to);
        Ops.Add(new ChangeOp { Type = OpType.RenameKey, Hive = hive, Path = from, NewPath = to });
    }
}

public sealed class RegistryEditor
{
    private readonly IRegistry _reg;
    private readonly string? _backupDir;
    public ChangeJournal Journal { get; }

    public RegistryEditor(IRegistry reg, ChangeJournal journal, string? backupDir)
    {
        _reg = reg;
        Journal = journal;
        _backupDir = backupDir;
    }

    public IRegistry Registry => _reg;

    /// <summary>Runs <paramref name="body"/> atomically: on any exception all ops done so far are reverted
    /// and the exception is rethrown. Returns null if nothing changed.</summary>
    public ChangeSet? Execute(string description, Action<Transaction> body)
    {
        var tx = new Transaction(_reg);
        try
        {
            body(tx);
        }
        catch
        {
            Revert(tx.Ops);
            throw;
        }
        if (tx.Ops.Count == 0) return null;

        var set = new ChangeSet { Description = description, Ops = tx.Ops };
        if (_backupDir != null && tx.Backup.Length > 0)
        {
            var file = Path.Combine(_backupDir, $"{set.Time:yyyyMMdd-HHmmss-fff}-{set.Id.ToString()[..8]}.reg");
            RegFileExporter.WriteFile(file, RegFileExporter.Header + "\r\n\r\n" + tx.Backup);
            set.BackupFile = file;
        }
        Journal.Add(set);
        return set;
    }

    public void Undo(ChangeSet set)
    {
        if (set.Undone) return;
        Revert(set.Ops);
        set.Undone = true;
        Journal.Save();
    }

    public ChangeSet? UndoLast()
    {
        var last = Journal.LastActive;
        if (last != null) Undo(last);
        return last;
    }

    /// <summary>Reverts every active change set, newest first. Returns number of reverted sets.</summary>
    public int UndoAll()
    {
        var n = 0;
        foreach (var set in Journal.Sets.Where(s => !s.Undone).Reverse().ToList())
        {
            Revert(set.Ops);
            set.Undone = true;
            n++;
        }
        Journal.Save();
        return n;
    }

    private void Revert(List<ChangeOp> ops)
    {
        for (var i = ops.Count - 1; i >= 0; i--) RevertOne(ops[i]);
    }

    private void RevertOne(ChangeOp op)
    {
        switch (op.Type)
        {
            case OpType.SetValue:
                if (op.CreatedRoot != null) { _reg.DeleteKeyTree(op.Hive, op.CreatedRoot); break; }
                if (op.OldValue != null) _reg.SetValue(op.Hive, op.Path, op.Name!, op.OldValue.ToRegValue());
                else _reg.DeleteValue(op.Hive, op.Path, op.Name!);
                break;
            case OpType.DeleteValue:
                _reg.SetValue(op.Hive, op.Path, op.Name!, op.OldValue!.ToRegValue());
                break;
            case OpType.CreateKey:
                if (op.CreatedRoot != null) _reg.DeleteKeyTree(op.Hive, op.CreatedRoot);
                break;
            case OpType.DeleteTree:
                RegistryTree.Restore(_reg, op.Hive, op.Path, op.Snapshot!);
                break;
            case OpType.RenameKey:
                if (_reg.KeyExists(op.Hive, op.NewPath!))
                    RegistryTree.Rename(_reg, op.Hive, op.NewPath!, op.Path);
                break;
        }
    }
}

using CtxMenu.Core.Editing;
using CtxMenu.Core.Localization;
using CtxMenu.Core.Model;
using CtxMenu.Core.Registry;
using CtxMenu.Core.Scanning;

namespace CtxMenu.Core.Services;

/// <summary>Reversible enable/disable and flag changes on scanned entries. Never deletes foreign keys.</summary>
public sealed class ContextMenuActions
{
    private readonly RegistryEditor _editor;
    private IRegistry Reg => _editor.Registry;

    public ContextMenuActions(RegistryEditor editor) => _editor = editor;

    /// <summary>Hive that will be written when toggling (HKLM needs admin rights).</summary>
    public static RegHive TargetHive(ContextMenuEntry e, bool enable, HandlerDisableMethod method)
    {
        if (e.Kind == EntryKind.Packaged) return enable && e.State == DisableState.BlockedLocalMachine ? RegHive.LocalMachine : RegHive.CurrentUser;
        if (e.Kind == EntryKind.ShellExtension)
        {
            if (enable) return e.State == DisableState.BlockedCurrentUser ? RegHive.CurrentUser : e.State == DisableState.BlockedLocalMachine ? RegHive.LocalMachine : e.Hive;
            return method == HandlerDisableMethod.BlockClsid && e.Clsid != null ? RegHive.CurrentUser : e.Hive;
        }
        return e.Hive;
    }

    public ChangeSet? SetEnabled(ContextMenuEntry e, bool enable, HandlerDisableMethod method = HandlerDisableMethod.RenameKey)
    {
        if (e.IsEnabled == enable) return null;
        var desc = Loc.Format(enable ? "Journal_Enable" : "Journal_Disable", e.DisplayName);
        return SetEnabledMany(new[] { e }, enable, method, desc);
    }

    /// <summary>Applies several toggles as one undoable change set.</summary>
    public ChangeSet? SetEnabledMany(IReadOnlyCollection<ContextMenuEntry> entries, bool enable, HandlerDisableMethod method, string description)
    {
        var originals = entries.Select(e => (e, e.State, e.KeyPath, e.KeyName)).ToList();
        try
        {
            return _editor.Execute(description, tx =>
            {
                foreach (var e in entries.Where(x => x.IsEnabled != enable))
                {
                    switch (e.Kind)
                    {
                        case EntryKind.Verb: ToggleVerb(tx, e, enable); break;
                        case EntryKind.ShellExtension: ToggleHandler(tx, e, enable, method); break;
                        default: TogglePackaged(tx, e, enable); break;
                    }
                }
            });
        }
        catch
        {
            foreach (var (e, state, path, name) in originals) { e.State = state; e.KeyPath = path; e.KeyName = name; }
            throw;
        }
    }

    private void ToggleVerb(Transaction tx, ContextMenuEntry e, bool enable)
    {
        if (enable)
        {
            tx.DeleteValue(e.Hive, e.KeyPath, "LegacyDisable");
            tx.DeleteValue(e.Hive, e.KeyPath, "ProgrammaticAccessOnly");
            e.State = DisableState.Enabled;
        }
        else
        {
            tx.SetString(e.Hive, e.KeyPath, "LegacyDisable", "");
            e.State = DisableState.LegacyDisable;
        }
    }

    private void ToggleHandler(Transaction tx, ContextMenuEntry e, bool enable, HandlerDisableMethod method)
    {
        if (enable)
        {
            if (e.KeyName.StartsWith('-'))
            {
                var to = RegPath.Combine(RegPath.Parent(e.KeyPath), e.KeyName.TrimStart('-'));
                tx.RenameKey(e.Hive, e.KeyPath, to);
                e.KeyPath = to;
                e.KeyName = RegPath.Leaf(to);
            }
            if (e.Clsid != null)
            {
                tx.DeleteValue(RegHive.CurrentUser, ContextMenuScanner.BlockedPath, e.Clsid);
                if (Reg.HasValue(RegHive.LocalMachine, ContextMenuScanner.BlockedPath, e.Clsid))
                    tx.DeleteValue(RegHive.LocalMachine, ContextMenuScanner.BlockedPath, e.Clsid);
            }
            e.State = DisableState.Enabled;
            return;
        }

        if (method == HandlerDisableMethod.BlockClsid && e.Clsid != null)
        {
            tx.SetString(RegHive.CurrentUser, ContextMenuScanner.BlockedPath, e.Clsid, "");
            e.State = DisableState.BlockedCurrentUser;
            return;
        }
        var target = RegPath.Combine(RegPath.Parent(e.KeyPath), "-" + e.KeyName);
        if (Reg.KeyExists(e.Hive, target))
            throw new InvalidOperationException(Loc.Format("Err_TargetExists", RegPath.Full(e.Hive, target)));
        tx.RenameKey(e.Hive, e.KeyPath, target);
        e.KeyPath = target;
        e.KeyName = RegPath.Leaf(target);
        e.State = DisableState.RenamedKey;
    }

    private void TogglePackaged(Transaction tx, ContextMenuEntry e, bool enable)
    {
        if (e.Clsid == null) throw new InvalidOperationException("No CLSID");
        if (enable)
        {
            tx.DeleteValue(RegHive.CurrentUser, ContextMenuScanner.BlockedPath, e.Clsid);
            if (Reg.HasValue(RegHive.LocalMachine, ContextMenuScanner.BlockedPath, e.Clsid))
                tx.DeleteValue(RegHive.LocalMachine, ContextMenuScanner.BlockedPath, e.Clsid);
            e.State = DisableState.Enabled;
        }
        else
        {
            tx.SetString(RegHive.CurrentUser, ContextMenuScanner.BlockedPath, e.Clsid, e.DisplayName);
            e.State = DisableState.BlockedCurrentUser;
        }
    }

    /// <summary>Hide the item unless Shift is held (verbs only).</summary>
    public ChangeSet? SetExtended(ContextMenuEntry e, bool extended)
    {
        if (e.Kind != EntryKind.Verb || e.IsExtended == extended) return null;
        var set = _editor.Execute(Loc.Format(extended ? "Journal_ShiftOn" : "Journal_ShiftOff", e.DisplayName), tx =>
        {
            if (extended) tx.SetString(e.Hive, e.KeyPath, "Extended", "");
            else tx.DeleteValue(e.Hive, e.KeyPath, "Extended");
        });
        e.IsExtended = extended;
        return set;
    }

    /// <summary>Alternative to LegacyDisable: hidden from the menu but still callable by programs.</summary>
    public ChangeSet? SetProgrammaticOnly(ContextMenuEntry e, bool on)
    {
        if (e.Kind != EntryKind.Verb) return null;
        var set = _editor.Execute(Loc.Format("Journal_Programmatic", e.DisplayName), tx =>
        {
            if (on)
            {
                tx.DeleteValue(e.Hive, e.KeyPath, "LegacyDisable");
                tx.SetString(e.Hive, e.KeyPath, "ProgrammaticAccessOnly", "");
            }
            else tx.DeleteValue(e.Hive, e.KeyPath, "ProgrammaticAccessOnly");
        });
        e.State = on ? DisableState.ProgrammaticOnly : (Reg.HasValue(e.Hive, e.KeyPath, "LegacyDisable") ? DisableState.LegacyDisable : DisableState.Enabled);
        return set;
    }

    public ChangeSet? SetPosition(ContextMenuEntry e, string? position)
    {
        if (e.Kind != EntryKind.Verb) return null;
        var set = _editor.Execute(Loc.Format("Journal_Position", e.DisplayName), tx =>
        {
            if (string.IsNullOrEmpty(position)) tx.DeleteValue(e.Hive, e.KeyPath, "Position");
            else tx.SetString(e.Hive, e.KeyPath, "Position", position);
        });
        e.Position = string.IsNullOrEmpty(position) ? null : position;
        return set;
    }

    public string ExportReg(ContextMenuEntry e) => RegFileExporter.Export(Reg, e.Hive, e.KeyPath);
}

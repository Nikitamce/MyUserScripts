using System.Text;
using CtxMenu.Core.Editing;
using CtxMenu.Core.Localization;
using CtxMenu.Core.Model;
using CtxMenu.Core.Registry;
using CtxMenu.Core.Scanning;

namespace CtxMenu.Core.Services;

public enum MenuPosition { Default, Top, Bottom }

public sealed class CustomEntryDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public string Title { get; set; } = "";
    public string? Program { get; set; }
    public string? Arguments { get; set; }
    public string? Icon { get; set; }
    public string Scope { get; set; } = @"Directory\Background";
    public bool Extended { get; set; }
    public MenuPosition Position { get; set; }
    public bool RunAsAdmin { get; set; }
    public bool AllUsers { get; set; }
    public bool IsGroup { get; set; }
    public List<CustomEntryDefinition> Children { get; set; } = new();
}

/// <summary>Creates user-defined verbs and cascading groups. Every created key carries a marker value so it
/// can be recognised and safely deleted later.</summary>
public sealed class CustomEntryService
{
    private readonly RegistryEditor _editor;
    private readonly string? _storeFile;
    private IRegistry Reg => _editor.Registry;

    public CustomEntryService(RegistryEditor editor, string? storeFile)
    {
        _editor = editor;
        _storeFile = storeFile;
    }

    public static string DefaultArguments(string scope) =>
        scope.Contains("Background", StringComparison.OrdinalIgnoreCase) ? "\"%V\"" : "\"%1\"";

    public static string MakeKeyName(CustomEntryDefinition def)
    {
        var sb = new StringBuilder("CMM.");
        foreach (var c in def.Title)
            if (char.IsAsciiLetterOrDigit(c)) sb.Append(c);
        if (sb.Length > 36) sb.Length = 36;
        sb.Append('.').Append(def.Id);
        return sb.ToString();
    }

    /// <summary>Builds the registry command line. Run-as-admin is done via PowerShell Start-Process -Verb RunAs
    /// because only a verb literally named "runas" gets elevation from the shell.</summary>
    public static string BuildCommand(CustomEntryDefinition def)
    {
        var program = (def.Program ?? "").Trim().Trim('"');
        if (program.Length == 0) throw new ArgumentException(Loc.Get("Err_NoProgram"));
        var args = def.Arguments ?? DefaultArguments(def.Scope);
        if (!def.RunAsAdmin)
            return args.Length == 0 ? $"\"{program}\"" : $"\"{program}\" {args}";
        static string Ps(string s) => "'" + s.Replace("'", "''") + "'";
        var ps = $"Start-Process -FilePath {Ps(program)}";
        if (args.Length > 0) ps += $" -ArgumentList {Ps(args.Replace("\"", "\\\""))}";
        ps += " -Verb RunAs";
        return $"powershell.exe -NoProfile -WindowStyle Hidden -Command \"{ps}\"";
    }

    public static RegHive HiveFor(CustomEntryDefinition def) => def.AllUsers ? RegHive.LocalMachine : RegHive.CurrentUser;

    public static string ShellPathFor(CustomEntryDefinition def) => RegPath.Combine(ContextMenuScanner.ClassesPath(def.Scope), "shell");

    public static void Validate(CustomEntryDefinition def)
    {
        if (string.IsNullOrWhiteSpace(def.Title)) throw new ArgumentException(Loc.Get("Err_NoTitle"));
        if (string.IsNullOrWhiteSpace(def.Scope)) throw new ArgumentException(Loc.Get("Err_NoScope"));
        if (def.IsGroup)
        {
            if (def.Children.Count == 0) throw new ArgumentException(Loc.Get("Err_EmptyGroup"));
            foreach (var c in def.Children) { c.Scope = def.Scope; Validate(c); }
        }
        else BuildCommand(def);
    }

    public ChangeSet? Create(CustomEntryDefinition def)
    {
        Validate(def);
        var set = _editor.Execute(Loc.Format("Journal_CustomAdd", def.Title), tx => Write(tx, HiveFor(def), ShellPathFor(def), def));
        Store(list => { list.RemoveAll(d => d.Id == def.Id); list.Add(def); });
        return set;
    }

    /// <summary>Adds a child verb to an existing cascading menu (shell subkey based groups).</summary>
    public ChangeSet? AddToGroup(ContextMenuEntry group, CustomEntryDefinition child)
    {
        if (group.SubmenuShellPath == null) throw new InvalidOperationException(Loc.Get("Err_NotEditableGroup"));
        child.Scope = group.Scope;
        Validate(child);
        return _editor.Execute(Loc.Format("Journal_GroupAdd", child.Title, group.DisplayName),
            tx => Write(tx, group.SubmenuHive, group.SubmenuShellPath, child));
    }

    private static void Write(Transaction tx, RegHive hive, string shellPath, CustomEntryDefinition def)
    {
        var key = RegPath.Combine(shellPath, MakeKeyName(def));
        if (tx.Registry.KeyExists(hive, key)) throw new InvalidOperationException(Loc.Format("Err_TargetExists", RegPath.Full(hive, key)));
        tx.SetString(hive, key, "MUIVerb", def.Title.Trim());
        tx.SetString(hive, key, ContextMenuScanner.CustomMarker, def.Id);
        if (!string.IsNullOrWhiteSpace(def.Icon)) tx.SetString(hive, key, "Icon", def.Icon.Trim());
        if (def.Extended) tx.SetString(hive, key, "Extended", "");
        if (def.Position != MenuPosition.Default) tx.SetString(hive, key, "Position", def.Position.ToString());
        if (def.IsGroup)
        {
            tx.SetString(hive, key, "SubCommands", "");
            var childShell = RegPath.Combine(key, "shell");
            foreach (var c in def.Children) Write(tx, hive, childShell, c);
        }
        else
        {
            if (def.RunAsAdmin) tx.SetString(hive, key, "HasLUAShield", "");
            tx.SetString(hive, RegPath.Combine(key, "command"), "", BuildCommand(def));
        }
    }

    public ChangeSet? Delete(ContextMenuEntry e)
    {
        if (!e.IsCustom) throw new InvalidOperationException(Loc.Get("Err_NotCustom"));
        var set = _editor.Execute(Loc.Format("Journal_CustomRemove", e.DisplayName), tx => tx.DeleteTree(e.Hive, e.KeyPath));
        Store(list => list.RemoveAll(d => d.Id == e.CustomId));
        return set;
    }

    public List<CustomEntryDefinition> LoadDefinitions()
    {
        if (_storeFile == null || !File.Exists(_storeFile)) return new();
        try { return ChangeJournal.Deserialize<List<CustomEntryDefinition>>(File.ReadAllText(_storeFile)) ?? new(); }
        catch (Exception) { return new(); }
    }

    private void Store(Action<List<CustomEntryDefinition>> change)
    {
        if (_storeFile == null) return;
        var list = LoadDefinitions();
        change(list);
        Directory.CreateDirectory(Path.GetDirectoryName(_storeFile)!);
        File.WriteAllText(_storeFile, ChangeJournal.Serialize(list));
    }

    /// <summary>True if an item with this id is present anywhere in the scope's shell key.</summary>
    public bool Exists(CustomEntryDefinition def) =>
        Reg.KeyExists(HiveFor(def), RegPath.Combine(ShellPathFor(def), MakeKeyName(def)));
}

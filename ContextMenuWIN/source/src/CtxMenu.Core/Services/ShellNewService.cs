using CtxMenu.Core.Editing;
using CtxMenu.Core.Localization;
using CtxMenu.Core.Model;
using CtxMenu.Core.Registry;
using CtxMenu.Core.Scanning;

namespace CtxMenu.Core.Services;

public sealed class ShellNewItem
{
    public string Extension { get; set; } = "";
    public RegHive Hive { get; set; }
    public string KeyPath { get; set; } = "";
    public string? ProgId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Mode { get; set; } = "";
    public string? Detail { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsCustom { get; set; }
    public bool IsCritical { get; set; }
    public string? IconLocation { get; set; }
}

/// <summary>The Explorer "New" (Создать) submenu: ShellNew keys under extensions.</summary>
public sealed class ShellNewService
{
    public const string Enabled = "ShellNew";
    public const string Disabled = "-ShellNew";
    public const string Marker = "CtxMenuManager";
    public const string CustomProgIdPrefix = "CtxMenuManager.";
    public const string CacheKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Discardable\PostSetup\ShellNew";
    private static readonly string[] Modes = { "NullFile", "FileName", "Data", "Command", "Directory", "Handler" };
    private static readonly RegHive[] Hives = { RegHive.CurrentUser, RegHive.LocalMachine };

    private readonly RegistryEditor _editor;
    private readonly IStringResolver _strings;
    private readonly string _templatesDir;
    private IRegistry Reg => _editor.Registry;

    public ShellNewService(RegistryEditor editor, IStringResolver strings, string templatesDir)
    {
        _editor = editor;
        _strings = strings;
        _templatesDir = templatesDir;
    }

    public List<ShellNewItem> Scan()
    {
        var list = new List<ShellNewItem>();
        foreach (var hive in Hives)
        {
            foreach (var key in Reg.GetSubKeyNames(hive, ContextMenuScanner.Classes))
            {
                if (!(key.Length > 1 && key[0] == '.') && !key.Equals("Folder", StringComparison.OrdinalIgnoreCase)) continue;
                var extPath = ContextMenuScanner.ClassesPath(key);
                foreach (var sub in Reg.GetSubKeyNames(hive, extPath))
                {
                    if (IsShellNewName(sub)) list.Add(Read(hive, key, RegPath.Combine(extPath, sub), null));
                    else
                    {
                        var progPath = RegPath.Combine(extPath, sub);
                        foreach (var sub2 in Reg.GetSubKeyNames(hive, progPath))
                            if (IsShellNewName(sub2)) list.Add(Read(hive, key, RegPath.Combine(progPath, sub2), sub));
                    }
                }
            }
        }
        return list.OrderBy(i => i.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static bool IsShellNewName(string n) => n.Equals(Enabled, StringComparison.OrdinalIgnoreCase) || n.Equals(Disabled, StringComparison.OrdinalIgnoreCase);

    private ShellNewItem Read(RegHive hive, string ext, string path, string? progIdSubkey)
    {
        var item = new ShellNewItem
        {
            Extension = ext, Hive = hive, KeyPath = path,
            IsEnabled = RegPath.Leaf(path).Equals(Enabled, StringComparison.OrdinalIgnoreCase),
            IsCustom = Reg.HasValue(hive, path, Marker),
        };
        var names = Reg.GetValueNames(hive, path);
        item.Mode = Modes.FirstOrDefault(m => names.Contains(m, StringComparer.OrdinalIgnoreCase)) ?? "?";
        item.Detail = item.Mode is "FileName" or "Command" ? Reg.GetString(hive, path, item.Mode) : null;

        var progId = progIdSubkey ?? Merged(ContextMenuScanner.ClassesPath(ext), "");
        item.ProgId = progId;
        string? name = Reg.GetString(hive, path, "ItemName");
        if (string.IsNullOrWhiteSpace(name) && progId != null)
        {
            var pp = ContextMenuScanner.ClassesPath(progId);
            name = Merged(pp, "FriendlyTypeName") ?? Merged(pp, "");
            item.IconLocation = Merged(RegPath.Combine(pp, "DefaultIcon"), "");
        }
        if (!string.IsNullOrWhiteSpace(name) && name.StartsWith('@')) name = _strings.Resolve(name);
        item.DisplayName = string.IsNullOrWhiteSpace(name) || name.StartsWith('@')
            ? (ext.Equals("Folder", StringComparison.OrdinalIgnoreCase) ? Loc.Get("ShellNew_Folder") : ext)
            : name;
        item.IsCritical = ext.Equals("Folder", StringComparison.OrdinalIgnoreCase) || ext.Equals(".lnk", StringComparison.OrdinalIgnoreCase);
        return item;
    }

    private string? Merged(string path, string name) =>
        Reg.GetString(RegHive.CurrentUser, path, name) is { Length: > 0 } s ? s : Reg.GetString(RegHive.LocalMachine, path, name) is { Length: > 0 } m ? m : null;

    public ChangeSet? SetEnabled(ShellNewItem item, bool enable)
    {
        if (item.IsEnabled == enable) return null;
        var target = RegPath.Combine(RegPath.Parent(item.KeyPath), enable ? Enabled : Disabled);
        var set = _editor.Execute(Loc.Format(enable ? "Journal_NewOn" : "Journal_NewOff", item.DisplayName), tx =>
        {
            tx.RenameKey(item.Hive, item.KeyPath, target);
            InvalidateCache(tx);
        });
        item.KeyPath = target;
        item.IsEnabled = enable;
        return set;
    }

    /// <summary>Explorer caches the New menu; deleting the cache value forces a rebuild.</summary>
    private static void InvalidateCache(Transaction tx) => tx.DeleteValue(RegHive.CurrentUser, CacheKey, "Classes");

    /// <summary>Adds a new "New → …" item for the current user. Template file is copied to app data.</summary>
    public ChangeSet? AddTemplate(string extension, string displayName, string? templateFile)
    {
        var ext = ScopeCatalog.NormalizeExtension(extension);
        if (!ScopeCatalog.IsValidExtension(ext)) throw new ArgumentException(Loc.Format("Err_BadExtension", extension));
        if (string.IsNullOrWhiteSpace(displayName)) displayName = ext.TrimStart('.').ToUpperInvariant() + " " + Loc.Get("ShellNew_FileSuffix");

        string? storedTemplate = null;
        if (!string.IsNullOrWhiteSpace(templateFile))
        {
            if (!File.Exists(templateFile)) throw new FileNotFoundException(templateFile);
            Directory.CreateDirectory(_templatesDir);
            storedTemplate = Path.Combine(_templatesDir, "template" + ext);
            File.Copy(templateFile, storedTemplate, overwrite: true);
        }

        var extPath = ContextMenuScanner.ClassesPath(ext);
        var progId = Merged(extPath, "");
        if (progId != null && !(Reg.KeyExists(RegHive.CurrentUser, ContextMenuScanner.ClassesPath(progId)) || Reg.KeyExists(RegHive.LocalMachine, ContextMenuScanner.ClassesPath(progId))))
            progId = null;

        return _editor.Execute(Loc.Format("Journal_NewAdd", displayName), tx =>
        {
            if (progId == null)
            {
                progId = CustomProgIdPrefix + ext.TrimStart('.');
                tx.SetString(RegHive.CurrentUser, ContextMenuScanner.ClassesPath(progId), "", displayName);
                tx.SetString(RegHive.CurrentUser, ContextMenuScanner.ClassesPath(progId), Marker, "1");
                tx.SetString(RegHive.CurrentUser, extPath, "", progId);
            }
            var sn = RegPath.Combine(extPath, Enabled);
            if (Reg.KeyExists(RegHive.CurrentUser, sn) || Reg.KeyExists(RegHive.CurrentUser, RegPath.Combine(extPath, Disabled)))
                throw new InvalidOperationException(Loc.Format("Err_ShellNewExists", ext));
            if (storedTemplate != null) tx.SetString(RegHive.CurrentUser, sn, "FileName", storedTemplate);
            else tx.SetString(RegHive.CurrentUser, sn, "NullFile", "");
            tx.SetString(RegHive.CurrentUser, sn, Marker, "1");
            InvalidateCache(tx);
        });
    }

    /// <summary>Removes an item created by this program (foreign items can only be disabled).</summary>
    public ChangeSet? RemoveCustom(ShellNewItem item)
    {
        if (!item.IsCustom) throw new InvalidOperationException(Loc.Get("Err_NotCustom"));
        return _editor.Execute(Loc.Format("Journal_NewRemove", item.DisplayName), tx =>
        {
            tx.DeleteTree(item.Hive, item.KeyPath);
            var extPath = ContextMenuScanner.ClassesPath(item.Extension);
            var progId = Reg.GetString(RegHive.CurrentUser, extPath, "");
            if (progId != null && progId.StartsWith(CustomProgIdPrefix, StringComparison.OrdinalIgnoreCase))
            {
                tx.DeleteTree(RegHive.CurrentUser, ContextMenuScanner.ClassesPath(progId));
                tx.DeleteValue(RegHive.CurrentUser, extPath, "");
                if (Reg.GetSubKeyNames(RegHive.CurrentUser, extPath).Count == 0 && Reg.GetValueNames(RegHive.CurrentUser, extPath).Count == 0)
                    tx.DeleteTree(RegHive.CurrentUser, extPath);
            }
            InvalidateCache(tx);
        });
    }
}

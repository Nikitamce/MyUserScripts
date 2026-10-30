using CtxMenu.Core.Localization;
using CtxMenu.Core.Model;
using CtxMenu.Core.Registry;

namespace CtxMenu.Core.Scanning;

public sealed class ScopeTarget
{
    public required string Scope { get; init; }
    public MenuCategory Category { get; set; }
    public string? LocKey { get; init; }
    public List<string> Extensions { get; } = new();
}

/// <summary>Reads all classic (registry based) context menu items from HKCU and HKLM.</summary>
public sealed class ContextMenuScanner
{
    public const string Classes = @"Software\Classes";
    public const string BlockedPath = @"Software\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked";
    public const string CommandStore = @"Software\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell";
    public const string FileExts = @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts";
    public const string CustomMarker = "CtxMenuManager.Id";
    private const int MaxSubmenuDepth = 4;
    private static readonly RegHive[] Hives = { RegHive.CurrentUser, RegHive.LocalMachine };

    private readonly IRegistry _reg;
    private readonly IFileInfoProvider _files;
    private readonly IStringResolver _strings;
    private readonly Dictionary<string, (string? name, string? module)> _clsidCache = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _blockedCu = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _blockedLm = new(StringComparer.OrdinalIgnoreCase);

    public ContextMenuScanner(IRegistry reg, IFileInfoProvider files, IStringResolver strings)
    {
        _reg = reg;
        _files = files;
        _strings = strings;
    }

    public static string ClassesPath(string scope) => RegPath.Combine(Classes, scope);

    public List<ContextMenuEntry> Scan(CancellationToken ct = default)
    {
        _clsidCache.Clear();
        LoadBlocked();
        var scopes = BuildScopes(ct);
        var result = new List<ContextMenuEntry>();
        foreach (var target in scopes.Values)
        {
            ct.ThrowIfCancellationRequested();
            foreach (var hive in Hives)
            {
                var basePath = ClassesPath(target.Scope);
                var shell = RegPath.Combine(basePath, "shell");
                foreach (var verb in _reg.GetSubKeyNames(hive, shell))
                    result.Add(Finish(ParseVerb(hive, target.Scope, shell, verb, 0), target));
                var handlers = RegPath.Combine(basePath, @"shellex\ContextMenuHandlers");
                foreach (var name in _reg.GetSubKeyNames(hive, handlers))
                    result.Add(Finish(ParseHandler(hive, target.Scope, handlers, name), target));
            }
        }
        return result;
    }

    private ContextMenuEntry Finish(ContextMenuEntry e, ScopeTarget target)
    {
        foreach (var x in e.SelfAndDescendants())
        {
            x.Category = target.Category;
            x.AppliesToExtensions = target.Extensions;
            (x.IsCritical, _) = KnownItems.IsCritical(x);
            x.ArchiverId = KnownItems.DetectArchiver(x);
        }
        return e;
    }

    // ---------------- scopes ----------------

    public Dictionary<string, ScopeTarget> BuildScopes(CancellationToken ct = default)
    {
        var scopes = new Dictionary<string, ScopeTarget>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in ScopeCatalog.Static)
            scopes[s.Scope] = new ScopeTarget { Scope = s.Scope, Category = s.Category, LocKey = s.LocKey };

        var extensions = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var hive in Hives)
            foreach (var k in _reg.GetSubKeyNames(hive, Classes))
                if (k.Length > 1 && k[0] == '.') extensions.Add(k.ToLowerInvariant());

        foreach (var ext in extensions)
        {
            ct.ThrowIfCancellationRequested();
            var perceived = ReadMerged(ClassesPath(ext), "PerceivedType");
            var cat = ScopeCatalog.CategoryForExtension(ext, perceived);
            Add(scopes, ext, cat, ext);
            foreach (var progId in ProgIdsFor(ext))
                Add(scopes, progId, cat, ext);
            var sfa = @"SystemFileAssociations\" + ext;
            if (ExistsMerged(ClassesPath(sfa))) Add(scopes, sfa, cat, ext);
            if (perceived != null && scopes.TryGetValue(@"SystemFileAssociations\" + perceived, out var pt) && !pt.Extensions.Contains(ext))
                pt.Extensions.Add(ext);
        }
        return scopes;
    }

    private static void Add(Dictionary<string, ScopeTarget> scopes, string scope, MenuCategory cat, string ext)
    {
        if (!scopes.TryGetValue(scope, out var t))
            scopes[scope] = t = new ScopeTarget { Scope = scope, Category = cat };
        if (!t.Extensions.Contains(ext)) t.Extensions.Add(ext);
    }

    private IEnumerable<string> ProgIdsFor(string ext)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var def = ReadMerged(ClassesPath(ext), "");
        if (!string.IsNullOrWhiteSpace(def) && ExistsMerged(ClassesPath(def)) && seen.Add(def)) yield return def;
        var choice = _reg.GetString(RegHive.CurrentUser, RegPath.Combine(FileExts, ext, "UserChoice"), "ProgId");
        if (!string.IsNullOrWhiteSpace(choice) && ExistsMerged(ClassesPath(choice)) && seen.Add(choice)) yield return choice;
    }

    /// <summary>All class keys whose menu items appear when right-clicking a file with this extension.</summary>
    public HashSet<string> GetScopesForExtension(string ext)
    {
        ext = ScopeCatalog.NormalizeExtension(ext);
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "*", "AllFilesystemObjects", ext, @"SystemFileAssociations\" + ext };
        var perceived = ReadMerged(ClassesPath(ext), "PerceivedType");
        if (perceived != null) set.Add(@"SystemFileAssociations\" + perceived);
        var any = false;
        foreach (var p in ProgIdsFor(ext)) { set.Add(p); any = true; }
        if (!any) set.Add("Unknown");
        return set;
    }

    private string? ReadMerged(string path, string name) =>
        _reg.GetString(RegHive.CurrentUser, path, name) is { Length: > 0 } s ? s : _reg.GetString(RegHive.LocalMachine, path, name) is { Length: > 0 } m ? m : null;

    private bool ExistsMerged(string path) => _reg.KeyExists(RegHive.CurrentUser, path) || _reg.KeyExists(RegHive.LocalMachine, path);

    // ---------------- verbs ----------------

    public ContextMenuEntry ParseVerb(RegHive hive, string scope, string shellPath, string verb, int depth)
    {
        var path = RegPath.Combine(shellPath, verb);
        var e = new ContextMenuEntry { Kind = EntryKind.Verb, Hive = hive, KeyPath = path, KeyName = verb, Scope = scope };

        var name = _reg.GetString(hive, path, "MUIVerb");
        if (string.IsNullOrWhiteSpace(name)) name = _reg.GetString(hive, path, "");
        if (!string.IsNullOrWhiteSpace(name))
        {
            var resolved = name.StartsWith('@') ? _strings.Resolve(name) : name;
            e.DisplayName = resolved.StartsWith('@') ? FallbackVerbName(verb) : CommandLine.StripAccelerators(resolved);
        }
        else e.DisplayName = FallbackVerbName(verb);

        e.IconLocation = NullIfEmpty(_reg.GetString(hive, path, "Icon"));
        var commandPath = RegPath.Combine(path, "command");
        e.Command = NullIfEmpty(_reg.GetString(hive, commandPath, ""));
        var handler = _reg.GetString(hive, commandPath, "DelegateExecute") ?? _reg.GetString(hive, path, "ExplorerCommandHandler");
        if (handler != null && CommandLine.IsGuid(handler.Trim())) e.Clsid = handler.Trim();

        if (_reg.HasValue(hive, path, "LegacyDisable")) e.State = DisableState.LegacyDisable;
        else if (_reg.HasValue(hive, path, "ProgrammaticAccessOnly")) e.State = DisableState.ProgrammaticOnly;
        e.IsExtended = _reg.HasValue(hive, path, "Extended");
        e.Position = NullIfEmpty(_reg.GetString(hive, path, "Position"));
        e.HasShield = _reg.HasValue(hive, path, "HasLUAShield");
        e.CustomId = NullIfEmpty(_reg.GetString(hive, path, CustomMarker));
        e.IsCustom = e.CustomId != null;

        string? module = null;
        if (e.Clsid != null) module = ResolveClsid(e.Clsid).module;
        module ??= CommandLine.ExtractExecutable(Expand(e.Command));
        ApplyModule(e, module);

        if (depth < MaxSubmenuDepth) ParseSubmenu(e, hive, path, scope, depth);
        return e;
    }

    private void ParseSubmenu(ContextMenuEntry e, RegHive hive, string path, string scope, int depth)
    {
        var subCommands = _reg.GetValue(hive, path, "SubCommands")?.AsString();
        var extKey = NullIfEmpty(_reg.GetString(hive, path, "ExtendedSubCommandsKey"));
        if (!string.IsNullOrWhiteSpace(subCommands))
        {
            e.Submenu = SubmenuKind.SubCommands;
            foreach (var id in subCommands.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (id == "|") continue; // separator
                foreach (var h in Hives)
                {
                    if (!_reg.KeyExists(h, RegPath.Combine(CommandStore, id))) continue;
                    var child = ParseVerb(h, scope, CommandStore, id, depth + 1);
                    child.Parent = e;
                    e.Children.Add(child);
                    break;
                }
            }
            return;
        }
        if (extKey != null)
        {
            e.Submenu = SubmenuKind.ExtendedSubCommandsKey;
            var shell = RegPath.Combine(ClassesPath(extKey), "shell");
            var h = _reg.KeyExists(RegHive.CurrentUser, shell) ? RegHive.CurrentUser : RegHive.LocalMachine;
            e.SubmenuHive = h;
            e.SubmenuShellPath = shell;
            AddChildren(e, h, shell, scope, depth);
            return;
        }
        var own = RegPath.Combine(path, "shell");
        if (subCommands != null || _reg.KeyExists(hive, own))
        {
            e.Submenu = SubmenuKind.ShellSubkey;
            e.SubmenuHive = hive;
            e.SubmenuShellPath = own;
            AddChildren(e, hive, own, scope, depth);
        }
    }

    private void AddChildren(ContextMenuEntry parent, RegHive hive, string shell, string scope, int depth)
    {
        foreach (var child in _reg.GetSubKeyNames(hive, shell))
        {
            var c = ParseVerb(hive, scope, shell, child, depth + 1);
            c.Parent = parent;
            parent.Children.Add(c);
        }
    }

    private static string FallbackVerbName(string verb) =>
        KnownItems.VerbLocKeys.TryGetValue(verb.TrimStart('-'), out var key) ? Loc.Get(key) : verb;

    // ---------------- shell extensions ----------------

    public ContextMenuEntry ParseHandler(RegHive hive, string scope, string handlersPath, string name)
    {
        var path = RegPath.Combine(handlersPath, name);
        var trimmed = name.TrimStart('-');
        var e = new ContextMenuEntry { Kind = EntryKind.ShellExtension, Hive = hive, KeyPath = path, KeyName = name, Scope = scope };
        var raw = _reg.GetString(hive, path, "")?.Trim();
        if (raw != null && raw.StartsWith('-') && CommandLine.IsGuid(raw[1..])) raw = raw[1..];
        e.Clsid = raw != null && CommandLine.IsGuid(raw) ? raw : CommandLine.IsGuid(trimmed) ? trimmed : null;

        if (name.StartsWith('-')) e.State = DisableState.RenamedKey;
        else if (e.Clsid != null && _blockedCu.Contains(e.Clsid)) e.State = DisableState.BlockedCurrentUser;
        else if (e.Clsid != null && _blockedLm.Contains(e.Clsid)) e.State = DisableState.BlockedLocalMachine;

        var (clsName, module) = e.Clsid != null ? ResolveClsid(e.Clsid) : (null, null);
        e.DisplayName = !CommandLine.IsGuid(trimmed) ? trimmed : clsName ?? trimmed;
        ApplyModule(e, module);
        e.IconLocation = module;
        return e;
    }

    public (string? name, string? module) ResolveClsid(string clsid)
    {
        if (_clsidCache.TryGetValue(clsid, out var cached)) return cached;
        string? name = null, module = null;
        foreach (var hive in Hives)
        {
            var p = RegPath.Combine(Classes, "CLSID", clsid);
            if (!_reg.KeyExists(hive, p)) continue;
            var n = _reg.GetString(hive, p, "LocalizedString") ?? _reg.GetString(hive, p, "");
            if (!string.IsNullOrWhiteSpace(n) && name == null)
                name = n.StartsWith('@') ? NullIfIndirect(_strings.Resolve(n)) : n;
            module ??= NullIfEmpty(_reg.GetString(hive, RegPath.Combine(p, "InprocServer32"), ""))
                       ?? CommandLine.ExtractExecutable(_reg.GetString(hive, RegPath.Combine(p, "LocalServer32"), ""));
        }
        if (module != null) module = Expand(module)?.Trim('"');
        return _clsidCache[clsid] = (name, module);
    }

    private void ApplyModule(ContextMenuEntry e, string? module)
    {
        e.ModulePath = module;
        var meta = module != null ? _files.GetMeta(module) : null;
        e.Company = NullIfEmpty(meta?.Company?.Trim());
        e.Product = NullIfEmpty(meta?.Product?.Trim());
        e.FileDescription = NullIfEmpty(meta?.Description?.Trim());
        e.IsThirdParty = IsThirdParty(e.Company, module);
    }

    public bool IsThirdParty(string? company, string? module)
    {
        if (company != null) return !company.Contains("Microsoft", StringComparison.OrdinalIgnoreCase);
        if (module == null) return false;
        var win = _files.WindowsDirectory.TrimEnd('\\');
        var full = module.Replace('/', '\\');
        if (full.StartsWith(win + "\\", StringComparison.OrdinalIgnoreCase)) return false;
        // Bare names (e.g. "shell32.dll", "notepad.exe") are resolved from the system directories.
        if (!full.Contains('\\')) return false;
        return true;
    }

    private void LoadBlocked()
    {
        _blockedCu = new HashSet<string>(_reg.GetValueNames(RegHive.CurrentUser, BlockedPath), StringComparer.OrdinalIgnoreCase);
        _blockedLm = new HashSet<string>(_reg.GetValueNames(RegHive.LocalMachine, BlockedPath), StringComparer.OrdinalIgnoreCase);
    }

    public bool IsBlocked(string clsid, out RegHive hive)
    {
        hive = RegHive.CurrentUser;
        if (_reg.HasValue(RegHive.CurrentUser, BlockedPath, clsid)) return true;
        hive = RegHive.LocalMachine;
        return _reg.HasValue(RegHive.LocalMachine, BlockedPath, clsid);
    }

    private static string? Expand(string? s) => s == null ? null : Environment.ExpandEnvironmentVariables(s);
    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
    private static string? NullIfIndirect(string s) => s.StartsWith('@') ? null : s;
}

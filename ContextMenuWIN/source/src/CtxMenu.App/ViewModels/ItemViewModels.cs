using System.Windows.Media;
using CtxMenu.App.Platform;
using CtxMenu.Core.Editing;
using CtxMenu.Core.Localization;
using CtxMenu.Core.Model;
using CtxMenu.Core.Registry;
using CtxMenu.Core.Scanning;
using CtxMenu.Core.Services;

namespace CtxMenu.App.ViewModels;

public static class Glyphs
{
    // Segoe Fluent Icons / Segoe MDL2 Assets code points
    public static string For(MenuCategory c) => c switch
    {
        MenuCategory.Desktop => "\uE7F4",
        MenuCategory.Background => "\uE8B7",
        MenuCategory.Folder => "\uE8B7",
        MenuCategory.Drive => "\uEDA2",
        MenuCategory.AllFiles => "\uE8A5",
        MenuCategory.AllObjects => "\uE8B9",
        MenuCategory.Images => "\uEB9F",
        MenuCategory.Video => "\uE714",
        MenuCategory.Audio => "\uE8D6",
        MenuCategory.Documents => "\uE8A5",
        MenuCategory.Archives => "\uE7B8",
        MenuCategory.Executables => "\uE756",
        MenuCategory.OtherFileTypes => "\uE7C3",
        MenuCategory.RecycleBin => "\uE74D",
        MenuCategory.ThisPC => "\uE7F8",
        MenuCategory.Library => "\uE8F1",
        MenuCategory.Network => "\uE968",
        MenuCategory.Printers => "\uE749",
        _ => "\uE712",
    };
}

public enum Page { Entries, ShellNew, Archivers, Win11, Custom, Journal, Settings }

public sealed class NavItem : ObservableObject
{
    private int _count;
    public required Page Page { get; init; }
    public MenuCategory? Category { get; init; }
    public bool IsAll { get; init; }
    public required string Glyph { get; init; }
    public required string LocKey { get; init; }
    public string Title => Loc.Get(LocKey);
    public int Count { get => _count; set => Set(ref _count, value); }
    public bool IsHeader { get; init; }
}

public sealed class EntryViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private ImageSource? _icon;
    private bool _iconLoaded;

    public EntryViewModel(ContextMenuEntry model, MainViewModel main, EntryViewModel? parent = null)
    {
        Model = model;
        _main = main;
        Parent = parent;
        Children = model.Children.Select(c => new EntryViewModel(c, main, this)).ToList();
    }

    public ContextMenuEntry Model { get; }
    public EntryViewModel? Parent { get; }
    public List<EntryViewModel> Children { get; }
    public bool HasChildren => Children.Count > 0;

    public string DisplayName => string.IsNullOrWhiteSpace(Model.DisplayName) ? Model.KeyName : Model.DisplayName;
    public string KeyName => Model.KeyName;
    public string Source => string.IsNullOrEmpty(Model.SourceName) ? Loc.Get("Src_Windows") : Model.SourceName;
    public string ScopeDisplay => DescribeScope(Model);
    public string CategoryDisplay => Loc.Get("Cat_" + Model.Category);
    public string KindDisplay => Loc.Get("Kind_" + Model.Kind);
    public string StateDisplay => Loc.Get("State_" + Model.State);
    public string HiveDisplay => RegPath.HiveShort(Model.Hive);
    public string FullPath => Model.Kind == EntryKind.Packaged ? Loc.Get("Packaged_PathHint") : RegPath.Full(Model.Hive, Model.KeyPath);
    public string? Command => Model.Command;
    public string? Clsid => Model.Clsid;
    public string? ModulePath => Model.ModulePath;
    public string? Company => Model.Company;
    public string? Product => Model.Product ?? Model.FileDescription;
    public string Extensions => Model.AppliesToExtensions.Count == 0 ? "" : string.Join(" ", Model.AppliesToExtensions.Take(40)) + (Model.AppliesToExtensions.Count > 40 ? " …" : "");
    public bool IsThirdParty => Model.IsThirdParty;
    public bool IsCritical => Model.IsCritical;
    public bool IsCustom => Model.IsCustom;
    public bool IsVerb => Model.Kind == EntryKind.Verb;
    public bool IsHandler => Model.Kind == EntryKind.ShellExtension;
    public bool IsPackaged => Model.Kind == EntryKind.Packaged;
    public bool NeedsAdmin => Model.Hive == RegHive.LocalMachine && Model.Kind != EntryKind.Packaged;
    public bool HasSubmenu => Model.Submenu != SubmenuKind.None;
    public bool CanAddToGroup => Model.SubmenuShellPath != null;
    public string SubmenuDisplay => Loc.Get("Submenu_" + Model.Submenu);
    public string? ArchiverId => Model.ArchiverId;
    public bool IsArchiver => Model.ArchiverId != null;
    public string? CriticalReason => Model.IsCritical ? Loc.Get(KnownItems.IsCritical(Model).reasonKey ?? "Crit_Generic") : null;
    public string PositionDisplay => Model.Position ?? Loc.Get("Pos_Default");

    public ImageSource? Icon
    {
        get
        {
            if (_iconLoaded) return _icon;
            _iconLoaded = true;
            _icon = IconCache.Get(Model.IconLocation) ?? (Model.Kind == EntryKind.Verb ? IconCache.Get(CommandLine.ExtractExecutable(Model.Command)) : null);
            return _icon;
        }
    }

    public bool HasIcon => Icon != null;

    public bool IsEnabled
    {
        get => Model.IsEnabled;
        set => _main.Toggle(this, value);
    }

    public bool IsExtended
    {
        get => Model.IsExtended;
        set => _main.Apply(() => _main.Actions.SetExtended(Model, value), NeedsAdmin, this);
    }

    public bool IsProgrammaticOnly
    {
        get => Model.State == DisableState.ProgrammaticOnly;
        set => _main.Apply(() => _main.Actions.SetProgrammaticOnly(Model, value), NeedsAdmin, this);
    }

    public string SearchText => string.Join("\u0001", DisplayName, Model.KeyName, Source, Model.Command, Model.Clsid, Model.Scope, Model.ModulePath, Model.Company);

    public static string DescribeScope(ContextMenuEntry e)
    {
        if (e.Kind == EntryKind.Packaged) return Loc.Format("Scope_Packaged", e.Scope);
        var info = ScopeCatalog.Find(e.Scope);
        if (info != null) return Loc.Get(info.LocKey);
        if (e.Scope.StartsWith(@"SystemFileAssociations\", StringComparison.OrdinalIgnoreCase))
            return Loc.Format("Scope_SfaExt", e.Scope["SystemFileAssociations\\".Length..]);
        if (e.Scope.StartsWith('.')) return Loc.Format("Scope_Ext", e.Scope);
        if (e.Scope.StartsWith(@"CLSID\", StringComparison.OrdinalIgnoreCase)) return Loc.Format("Scope_Clsid", e.Scope[6..]);
        var exts = e.AppliesToExtensions;
        return exts.Count == 0 ? e.Scope : Loc.Format("Scope_ProgId", e.Scope, string.Join(", ", exts.Take(6)) + (exts.Count > 6 ? "…" : ""));
    }
}

public sealed class ShellNewItemViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    public ShellNewItemViewModel(ShellNewItem model, MainViewModel main) { Model = model; _main = main; }
    public ShellNewItem Model { get; }
    public string DisplayName => Model.DisplayName;
    public string Extension => Model.Extension;
    public string ModeDisplay => Loc.Get("NewMode_" + Model.Mode.Replace("?", "Unknown"));
    public string? Detail => Model.Detail;
    public string HiveDisplay => RegPath.HiveShort(Model.Hive);
    public string FullPath => RegPath.Full(Model.Hive, Model.KeyPath);
    public bool IsCustom => Model.IsCustom;
    public bool IsCritical => Model.IsCritical;
    public bool NeedsAdmin => Model.Hive == RegHive.LocalMachine;
    public ImageSource? Icon => IconCache.Get(Model.IconLocation);

    public bool IsEnabled
    {
        get => Model.IsEnabled;
        set
        {
            if (!value && Model.IsCritical && _main.Settings.ConfirmCritical && !Dialogs.Warn(Loc.Format("Warn_CriticalNew", DisplayName)))
            {
                _main.Later(RaiseAll);
                return;
            }
            _main.Apply(() => _main.ShellNew.SetEnabled(Model, value), NeedsAdmin, this);
        }
    }
}

public sealed class ArchiverItemViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly ArchiverInfo _archiver;
    public ArchiverItemViewModel(ArchiverInfo archiver, ArchiverItem model, MainViewModel main) { _archiver = archiver; Model = model; _main = main; }
    public ArchiverItem Model { get; }
    public string Title => Loc.Has(Model.LocKey) ? Loc.Get(Model.LocKey) : Model.ValueName;
    public string Technical => Model.Kind == ArchiverItemKind.SevenZipBit ? $"{Model.ValueName} bit 0x{Model.Bit:X}" : Model.ValueName;
    public bool IsDefault => !Model.IsExplicit;
    public bool IsEnabled
    {
        get => Model.IsEnabled;
        set => _main.Apply(() => _main.Archivers.SetItem(_archiver, Model, value), false, this, after: () => _main.ArchiversPage.RaiseItems(_archiver));
    }
}

public sealed class ArchiverViewModel : ObservableObject
{
    public ArchiverViewModel(ArchiverInfo info, MainViewModel main, IEnumerable<EntryViewModel> handlers)
    {
        Info = info;
        Items = info.Items.Select(i => new ArchiverItemViewModel(info, i, main)).ToList();
        Options = info.Options.Select(i => new ArchiverItemViewModel(info, i, main)).ToList();
        TopItems = info.TopItems.Select(i => new ArchiverItemViewModel(info, i, main)).ToList();
        Handlers = handlers.ToList();
    }
    public ArchiverInfo Info { get; }
    public string Name => Info.Name;
    public bool Installed => Info.Installed;
    public List<ArchiverItemViewModel> Items { get; }
    public List<ArchiverItemViewModel> Options { get; }
    public List<ArchiverItemViewModel> TopItems { get; }
    public bool HasTopItems => TopItems.Count > 0;
    public List<EntryViewModel> Handlers { get; }
    public bool HasHandlers => Handlers.Count > 0;
    public string Hint => Loc.Get(Info.Id == "7zip" ? "Arc_Hint7z" : "Arc_HintRar");
}

public sealed class ChangeSetViewModel
{
    public ChangeSetViewModel(ChangeSet model) => Model = model;
    public ChangeSet Model { get; }
    public string Time => Model.Time.LocalDateTime.ToString("dd.MM.yyyy HH:mm:ss");
    public string Description => Model.Description;
    public bool Undone => Model.Undone;
    public string StatusDisplay => Loc.Get(Model.Undone ? "Journal_Undone" : "Journal_Active");
    public string Details => string.Join(Environment.NewLine, Model.Ops.Select(o => "• " + o));
    public int OpCount => Model.Ops.Count;
    public string? BackupFile => Model.BackupFile;
}

public sealed record ScopeOption(string Scope, string Title);

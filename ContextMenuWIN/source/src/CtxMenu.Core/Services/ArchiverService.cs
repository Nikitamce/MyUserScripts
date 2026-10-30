using CtxMenu.Core.Editing;
using CtxMenu.Core.Localization;
using CtxMenu.Core.Registry;

namespace CtxMenu.Core.Services;

public enum ArchiverItemKind { WinRarValue, SevenZipBit, BoolOption }

public sealed class ArchiverItem
{
    public required string Id { get; init; }
    public required string LocKey { get; init; }
    public ArchiverItemKind Kind { get; init; }
    public string KeyPath { get; init; } = "";
    public string ValueName { get; init; } = "";
    public uint Bit { get; init; }
    public bool DefaultValue { get; init; } = true;
    public bool IsEnabled { get; set; }
    public bool IsExplicit { get; set; }
}

public sealed class ArchiverInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public bool Installed { get; set; }
    public List<ArchiverItem> Items { get; } = new();
    public List<ArchiverItem> Options { get; } = new();
    /// <summary>WinRAR 7+: items shown in the new (Windows 11) menu.</summary>
    public List<ArchiverItem> TopItems { get; } = new();
}

/// <summary>WinRAR and 7-Zip draw their submenus from a COM handler, so individual items can only be
/// switched through their own settings in HKCU. Values are documented by the vendors.</summary>
public sealed class ArchiverService
{
    public const string WinRarSetup = @"Software\WinRAR\Setup";
    public const string WinRarMenuItems = @"Software\WinRAR\Setup\MenuItems";
    public const string WinRarTopMenuItems = @"Software\WinRAR\Setup\TopMenuItems";
    public const string SevenZipOptions = @"Software\7-Zip\Options";
    public const string SevenZipFlags = "ContextMenu";

    private static readonly (string id, bool def)[] WinRarItems =
    {
        ("ExtrTo", true), ("ExtrHere", true), ("Extr", true), ("ExtrSep", false), ("OpenArc", true), ("OpenSFX", true),
        ("AddTo", true), ("AddArc", true), ("EmailArc", false), ("EmailOpt", false), ("Test", false), ("Convert", false),
    };

    // CPP/7zip/UI/Explorer/ContextMenuFlags.h
    private static readonly (string id, uint bit)[] SevenZipItems =
    {
        ("Open", 1u << 5), ("OpenAs", 1u << 6), ("Extract", 1u << 0), ("ExtractHere", 1u << 1), ("ExtractTo", 1u << 2),
        ("Test", 1u << 4), ("Compress", 1u << 8), ("CompressTo7z", 1u << 9), ("CompressToZip", 1u << 12),
        ("CompressEmail", 1u << 10), ("CompressTo7zEmail", 1u << 11), ("CompressToZipEmail", 1u << 13),
        ("CRC", 1u << 31), ("CRCCascaded", 1u << 30),
    };

    private readonly RegistryEditor _editor;
    private IRegistry Reg => _editor.Registry;

    public ArchiverService(RegistryEditor editor) => _editor = editor;

    public List<ArchiverInfo> Detect() => new() { DetectWinRar(), DetectSevenZip() };

    private bool Installed(string key) => Reg.KeyExists(RegHive.CurrentUser, key) || Reg.KeyExists(RegHive.LocalMachine, key);

    public ArchiverInfo DetectWinRar()
    {
        var info = new ArchiverInfo { Id = "winrar", Name = "WinRAR", Installed = Installed(@"Software\WinRAR") };
        foreach (var (id, def) in WinRarItems)
            info.Items.Add(ReadDword(new ArchiverItem { Id = id, LocKey = "WinRar_" + id, Kind = ArchiverItemKind.WinRarValue, KeyPath = WinRarMenuItems, ValueName = id, DefaultValue = def }));
        info.Options.Add(ReadDword(new ArchiverItem { Id = "CascadedMenu", LocKey = "Arc_Cascaded", Kind = ArchiverItemKind.BoolOption, KeyPath = WinRarSetup, ValueName = "CascadedMenu", DefaultValue = true }));
        info.Options.Add(ReadDword(new ArchiverItem { Id = "MenuIcons", LocKey = "Arc_Icons", Kind = ArchiverItemKind.BoolOption, KeyPath = WinRarSetup, ValueName = "MenuIcons", DefaultValue = true }));
        if (Reg.KeyExists(RegHive.CurrentUser, WinRarTopMenuItems))
            foreach (var name in Reg.GetValueNames(RegHive.CurrentUser, WinRarTopMenuItems).Where(n => n.Length > 0).OrderBy(n => n))
                info.TopItems.Add(ReadDword(new ArchiverItem { Id = "Top" + name, LocKey = "WinRar_" + name, Kind = ArchiverItemKind.WinRarValue, KeyPath = WinRarTopMenuItems, ValueName = name, DefaultValue = false }));
        return info;
    }

    public ArchiverInfo DetectSevenZip()
    {
        var info = new ArchiverInfo { Id = "7zip", Name = "7-Zip", Installed = Installed(@"Software\7-Zip") };
        var flagsValue = Reg.GetValue(RegHive.CurrentUser, SevenZipOptions, SevenZipFlags)?.AsDWord();
        var flags = flagsValue ?? 0xFFFFFFFF;
        foreach (var (id, bit) in SevenZipItems)
            info.Items.Add(new ArchiverItem
            {
                Id = id, LocKey = "SevenZip_" + id, Kind = ArchiverItemKind.SevenZipBit, KeyPath = SevenZipOptions, ValueName = SevenZipFlags,
                Bit = bit, IsEnabled = (flags & bit) != 0, IsExplicit = flagsValue.HasValue,
            });
        info.Options.Add(ReadDword(new ArchiverItem { Id = "CascadedMenu", LocKey = "Arc_Cascaded", Kind = ArchiverItemKind.BoolOption, KeyPath = SevenZipOptions, ValueName = "CascadedMenu", DefaultValue = true }));
        info.Options.Add(ReadDword(new ArchiverItem { Id = "MenuIcons", LocKey = "Arc_Icons", Kind = ArchiverItemKind.BoolOption, KeyPath = SevenZipOptions, ValueName = "MenuIcons", DefaultValue = false }));
        return info;
    }

    private ArchiverItem ReadDword(ArchiverItem item)
    {
        var v = Reg.GetValue(RegHive.CurrentUser, item.KeyPath, item.ValueName)?.AsDWord();
        item.IsExplicit = v.HasValue;
        item.IsEnabled = v.HasValue ? v.Value != 0 : item.DefaultValue;
        return item;
    }

    public ChangeSet? SetItem(ArchiverInfo archiver, ArchiverItem item, bool enabled)
    {
        if (item.IsEnabled == enabled && item.IsExplicit) return null;
        var set = _editor.Execute(Loc.Format("Journal_Archiver", archiver.Name, Loc.Get(item.LocKey), Loc.Get(enabled ? "State_On" : "State_Off")), tx =>
        {
            if (item.Kind == ArchiverItemKind.SevenZipBit)
            {
                var current = Reg.GetValue(RegHive.CurrentUser, item.KeyPath, item.ValueName)?.AsDWord() ?? 0xFFFFFFFF;
                var next = enabled ? current | item.Bit : current & ~item.Bit;
                tx.SetValue(RegHive.CurrentUser, item.KeyPath, item.ValueName, RegValue.DWord(next));
            }
            else tx.SetValue(RegHive.CurrentUser, item.KeyPath, item.ValueName, RegValue.DWord(enabled ? 1u : 0u));
        });
        item.IsEnabled = enabled;
        item.IsExplicit = true;
        if (item.Kind == ArchiverItemKind.SevenZipBit)
            foreach (var other in archiver.Items) other.IsExplicit = true;
        return set;
    }
}

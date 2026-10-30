using System.Text.Json.Serialization;
using CtxMenu.Core.Registry;

namespace CtxMenu.Core.Model;

public enum EntryKind { Verb, ShellExtension, Packaged }

/// <summary>Where the item shows up. Order = order in the navigation pane.</summary>
public enum MenuCategory
{
    Desktop, Background, Folder, Drive, AllFiles, AllObjects,
    Images, Video, Audio, Documents, Archives, Executables, OtherFileTypes,
    RecycleBin, ThisPC, Library, Network, Printers, Other
}

public enum DisableState { Enabled, LegacyDisable, ProgrammaticOnly, RenamedKey, BlockedCurrentUser, BlockedLocalMachine }

public enum SubmenuKind { None, ShellSubkey, ExtendedSubCommandsKey, SubCommands }

public enum HandlerDisableMethod { RenameKey, BlockClsid }

public sealed class ContextMenuEntry
{
    public EntryKind Kind { get; set; }
    public RegHive Hive { get; set; }
    /// <summary>Current registry path of the item's key (relative to hive).</summary>
    public string KeyPath { get; set; } = "";
    public string KeyName { get; set; } = "";
    /// <summary>Class key relative to Software\Classes, e.g. "*", "Directory\Background", ".jpg", "jpegfile".</summary>
    public string Scope { get; set; } = "";
    public MenuCategory Category { get; set; }
    public string DisplayName { get; set; } = "";
    public string? Command { get; set; }
    public string? IconLocation { get; set; }
    public string? Clsid { get; set; }
    public string? ModulePath { get; set; }
    public string? Company { get; set; }
    public string? Product { get; set; }
    public string? FileDescription { get; set; }
    public DisableState State { get; set; }
    public bool IsEnabled => State == DisableState.Enabled;
    public bool IsExtended { get; set; }
    public string? Position { get; set; }
    public bool HasShield { get; set; }
    public bool IsThirdParty { get; set; }
    public bool IsCritical { get; set; }
    public bool IsCustom { get; set; }
    public string? CustomId { get; set; }
    public string? ArchiverId { get; set; }
    public string? PackageName { get; set; }
    public List<string> AppliesToExtensions { get; set; } = new();

    public SubmenuKind Submenu { get; set; }
    /// <summary>Path of the "shell" key containing children (for ShellSubkey/ExtendedSubCommandsKey).</summary>
    public string? SubmenuShellPath { get; set; }
    public RegHive SubmenuHive { get; set; }
    public List<ContextMenuEntry> Children { get; set; } = new();
    [JsonIgnore] public ContextMenuEntry? Parent { get; set; }

    /// <summary>Identifier that survives disabling (the '-' rename) and is portable between machines.</summary>
    public string StableId
    {
        get
        {
            if (Kind == EntryKind.Packaged) return $"Packaged|{PackageName}|{Clsid}";
            var parent = RegPath.Parent(KeyPath);
            var leaf = RegPath.Leaf(KeyPath).TrimStart('-');
            return $"{Kind}|{Hive}|{RegPath.Combine(parent, leaf)}".ToLowerInvariant();
        }
    }

    public string SourceName => Product ?? Company ?? FileDescription ?? (ModulePath != null ? Path.GetFileName(ModulePath) : "");

    public IEnumerable<ContextMenuEntry> SelfAndDescendants()
    {
        yield return this;
        foreach (var c in Children)
            foreach (var d in c.SelfAndDescendants())
                yield return d;
    }

    public override string ToString() => $"{DisplayName} [{Kind} {RegPath.HiveShort(Hive)}\\{KeyPath}]";
}

public sealed record FileMeta(string? Company, string? Product, string? Description);

public interface IFileInfoProvider
{
    FileMeta? GetMeta(string path);
    string WindowsDirectory { get; }
}

public interface IStringResolver
{
    /// <summary>Resolves indirect strings like "@shell32.dll,-8506". Returns input on failure.</summary>
    string Resolve(string value);
}

public sealed class PassThroughResolver : IStringResolver
{
    public string Resolve(string value) => value;
}

using System.Xml.Linq;
using CtxMenu.Core.Model;
using CtxMenu.Core.Registry;
using CtxMenu.Core.Scanning;

namespace CtxMenu.Core.Services;

public sealed record PackagedVerb(string ItemType, string VerbId, string Clsid, string? ComDisplayName);

public sealed record PackagedManifest(string IdentityName, string? DisplayName, string? PublisherDisplayName, IReadOnlyList<PackagedVerb> Verbs);

public sealed record InstalledPackage(string FullName, string InstallPath, string? DisplayName);

/// <summary>MSIX/AppX packages register Windows 11 menu items (IExplorerCommand) in AppxManifest.xml:
/// desktop4:FileExplorerContextMenus → desktop5:ItemType → desktop5:Verb (Id, Clsid).</summary>
public static class AppxManifestParser
{
    public static PackagedManifest Parse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var root = doc.Root ?? throw new FormatException("Empty manifest");
        var identity = root.Elements().FirstOrDefault(e => e.Name.LocalName == "Identity")?.Attribute("Name")?.Value ?? "?";
        var props = root.Elements().FirstOrDefault(e => e.Name.LocalName == "Properties");
        var display = props?.Elements().FirstOrDefault(e => e.Name.LocalName == "DisplayName")?.Value;
        var publisher = props?.Elements().FirstOrDefault(e => e.Name.LocalName == "PublisherDisplayName")?.Value;

        var comNames = root.Descendants()
            .Where(e => e.Name.LocalName == "Class" && e.Attribute("Id") != null)
            .GroupBy(e => NormalizeClsid(e.Attribute("Id")!.Value), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Attribute("DisplayName")?.Value, StringComparer.OrdinalIgnoreCase);

        var verbs = new List<PackagedVerb>();
        foreach (var menus in root.Descendants().Where(e => e.Name.LocalName == "FileExplorerContextMenus"))
            foreach (var item in menus.Elements().Where(e => e.Name.LocalName == "ItemType"))
            {
                var type = item.Attribute("Type")?.Value ?? "*";
                foreach (var verb in item.Elements().Where(e => e.Name.LocalName == "Verb"))
                {
                    var clsid = verb.Attribute("Clsid")?.Value;
                    if (clsid == null) continue;
                    var norm = NormalizeClsid(clsid);
                    verbs.Add(new PackagedVerb(type, verb.Attribute("Id")?.Value ?? "", norm, comNames.GetValueOrDefault(norm)));
                }
            }
        return new PackagedManifest(identity, display, publisher, verbs);
    }

    public static string NormalizeClsid(string clsid)
    {
        var c = clsid.Trim();
        if (!c.StartsWith('{')) c = "{" + c + "}";
        return c.ToUpperInvariant();
    }

    public static MenuCategory CategoryForItemType(string type) => type switch
    {
        "*" => MenuCategory.AllFiles,
        _ => ScopeCatalog.CategoryForScope(type),
    };

    /// <summary>Turns manifests into entries; deduplicates the same CLSID registered for several item types.</summary>
    public static List<ContextMenuEntry> ToEntries(IEnumerable<(InstalledPackage pkg, PackagedManifest manifest)> packages, IRegistry reg)
    {
        var result = new List<ContextMenuEntry>();
        foreach (var (pkg, m) in packages)
        {
            foreach (var group in m.Verbs.GroupBy(v => v.Clsid))
            {
                var first = group.First();
                var e = new ContextMenuEntry
                {
                    Kind = EntryKind.Packaged,
                    Hive = RegHive.CurrentUser,
                    KeyPath = ContextMenuScanner.BlockedPath,
                    KeyName = first.VerbId,
                    Scope = string.Join(", ", group.Select(v => v.ItemType).Distinct()),
                    Category = CategoryForItemType(first.ItemType),
                    Clsid = first.Clsid,
                    PackageName = m.IdentityName,
                    DisplayName = first.ComDisplayName ?? (IsResource(pkg.DisplayName) ? null : pkg.DisplayName) ?? (IsResource(m.DisplayName) ? null : m.DisplayName) ?? first.VerbId,
                    Product = (IsResource(pkg.DisplayName) ? null : pkg.DisplayName) ?? m.IdentityName,
                    Company = IsResource(m.PublisherDisplayName) ? null : m.PublisherDisplayName,
                    ModulePath = pkg.InstallPath,
                };
                e.IsThirdParty = !(e.Company?.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) ?? false) && !m.IdentityName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase);
                if (reg.HasValue(RegHive.CurrentUser, ContextMenuScanner.BlockedPath, e.Clsid)) e.State = DisableState.BlockedCurrentUser;
                else if (reg.HasValue(RegHive.LocalMachine, ContextMenuScanner.BlockedPath, e.Clsid)) e.State = DisableState.BlockedLocalMachine;
                e.ArchiverId = KnownItems.DetectArchiver(e);
                result.Add(e);
            }
        }
        return result;
    }

    private static bool IsResource(string? s) => string.IsNullOrWhiteSpace(s) || s.StartsWith("ms-resource:", StringComparison.OrdinalIgnoreCase);
}

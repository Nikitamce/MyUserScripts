using CtxMenu.Core.Model;
using CtxMenu.Core.Registry;
using CtxMenu.Core.Services;
using Windows.Management.Deployment;

namespace CtxMenu.App.Platform;

/// <summary>Enumerates installed MSIX/AppX packages for the current user and reads their manifests.</summary>
public static class PackageScanner
{
    public static List<ContextMenuEntry> Scan(IRegistry reg, CancellationToken ct)
    {
        var found = new List<(InstalledPackage, PackagedManifest)>();
        var pm = new PackageManager();
        foreach (var p in pm.FindPackagesForUser(string.Empty))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                if (p.IsFramework || p.IsResourcePackage) continue;
                var dir = p.InstalledLocation?.Path;
                if (dir == null) continue;
                var file = Path.Combine(dir, "AppxManifest.xml");
                if (!File.Exists(file)) continue;
                var xml = File.ReadAllText(file);
                if (!xml.Contains("FileExplorerContextMenus", StringComparison.Ordinal)) continue;
                string? name = null;
                try { name = p.DisplayName; } catch (Exception) { }
                found.Add((new InstalledPackage(p.Id.FullName, dir, name), AppxManifestParser.Parse(xml)));
            }
            catch (Exception) { /* broken or inaccessible package, skip */ }
        }
        return AppxManifestParser.ToEntries(found, reg);
    }
}

using CtxMenu.Core.Model;
using CtxMenu.Core.Scanning;

namespace CtxMenu.Core.Preview;

/// <summary>Which registry class keys (scopes) Explorer consults for each kind of right-click.</summary>
public static class PreviewTargets
{
    public static readonly IReadOnlyList<PreviewTarget> All = Enum.GetValues<PreviewTarget>();

    public static bool IsFile(PreviewTarget t) => t >= PreviewTarget.File;

    public static bool IsBackground(PreviewTarget t) => t is PreviewTarget.DesktopBackground or PreviewTarget.FolderBackground;

    public static string? DefaultExtension(PreviewTarget t) => t switch
    {
        PreviewTarget.File => ".txt",
        PreviewTarget.Image => ".jpg",
        PreviewTarget.Video => ".mp4",
        PreviewTarget.Audio => ".mp3",
        PreviewTarget.Document => ".docx",
        PreviewTarget.Archive => ".zip",
        PreviewTarget.Executable => ".exe",
        _ => null,
    };

    /// <summary>Extension actually used for a file target (user input if valid, else the default).</summary>
    public static string? EffectiveExtension(PreviewTarget t, string? userExtension)
    {
        if (!IsFile(t)) return null;
        if (!string.IsNullOrWhiteSpace(userExtension))
        {
            var e = ScopeCatalog.NormalizeExtension(userExtension);
            if (ScopeCatalog.IsValidExtension(e)) return e;
        }
        return DefaultExtension(t);
    }

    /// <summary>All class keys whose items Windows merges into the menu for this target.</summary>
    /// <param name="extensionScopes">Resolves a file extension to its class keys (see <see cref="ContextMenuScanner.GetScopesForExtension"/>).</param>
    public static HashSet<string> ScopesFor(PreviewTarget t, string? extension, Func<string, IEnumerable<string>> extensionScopes)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        switch (t)
        {
            case PreviewTarget.DesktopBackground:
                // The desktop is a folder too: Directory\Background items show up there as well.
                set.Add("DesktopBackground");
                set.Add(@"Directory\Background");
                break;
            case PreviewTarget.FolderBackground:
                set.Add(@"Directory\Background");
                break;
            case PreviewTarget.Folder:
                set.Add("Directory");
                set.Add("Folder");
                set.Add("AllFilesystemObjects");
                break;
            case PreviewTarget.Drive:
                set.Add("Drive");
                set.Add("Folder");
                break;
            default:
                var ext = EffectiveExtension(t, extension)!;
                foreach (var s in extensionScopes(ext)) set.Add(s);
                set.Add("*");
                set.Add("AllFilesystemObjects");
                set.Add(ext);
                break;
        }
        return set;
    }

    /// <summary>Does a scanned item appear for these scopes? Packaged (Win11) items list their item types comma-separated.</summary>
    public static bool Applies(ContextMenuEntry e, IReadOnlySet<string> scopes)
    {
        if (e.Kind == EntryKind.Packaged)
            return e.Scope.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Any(scopes.Contains);
        return scopes.Contains(e.Scope);
    }

    /// <summary>Lower = more specific. Explorer lets the most specific class key win when a verb is registered twice.</summary>
    public static int ScopeRank(string scope)
    {
        if (scope.Equals("*", StringComparison.OrdinalIgnoreCase)) return 6;
        if (scope.Equals("AllFilesystemObjects", StringComparison.OrdinalIgnoreCase)) return 7;
        if (scope.Equals("Folder", StringComparison.OrdinalIgnoreCase)) return 5;
        if (scope.Equals("Unknown", StringComparison.OrdinalIgnoreCase)) return 6;
        if (scope.Equals(@"Directory\Background", StringComparison.OrdinalIgnoreCase)) return 2;
        if (scope.StartsWith(@"SystemFileAssociations\.", StringComparison.OrdinalIgnoreCase)) return 3;
        if (scope.StartsWith(@"SystemFileAssociations\", StringComparison.OrdinalIgnoreCase)) return 4;
        if (scope.StartsWith('.')) return 1;
        return 0; // ProgID, Directory, Drive, DesktopBackground
    }

    public static string LocKey(PreviewTarget t) => "PvT_" + t;

    public static string Glyph(PreviewTarget t) => t switch
    {
        PreviewTarget.DesktopBackground => "\uE7F4",
        PreviewTarget.FolderBackground => "\uE838",
        PreviewTarget.Folder => "\uE8B7",
        PreviewTarget.Drive => "\uEDA2",
        PreviewTarget.File => "\uE8A5",
        PreviewTarget.Image => "\uEB9F",
        PreviewTarget.Video => "\uE714",
        PreviewTarget.Audio => "\uE8D6",
        PreviewTarget.Document => "\uE8A5",
        PreviewTarget.Archive => "\uE7B8",
        PreviewTarget.Executable => "\uE756",
        _ => "\uE8A5",
    };
}

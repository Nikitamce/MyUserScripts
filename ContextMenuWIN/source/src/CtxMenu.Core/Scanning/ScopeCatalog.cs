using CtxMenu.Core.Model;

namespace CtxMenu.Core.Scanning;

public sealed record ScopeInfo(string Scope, MenuCategory Category, string LocKey);

/// <summary>Known class keys that host context menu items and where they appear.</summary>
public static class ScopeCatalog
{
    public const string RecycleBinClsid = "{645FF040-5081-101B-9F08-00AA002F954E}";
    public const string ThisPcClsid = "{20D04FE0-3AEA-1069-A2D8-08002B30309D}";

    public static readonly IReadOnlyList<ScopeInfo> Static = new List<ScopeInfo>
    {
        new("DesktopBackground", MenuCategory.Desktop, "Scope_DesktopBackground"),
        new(@"Directory\Background", MenuCategory.Background, "Scope_DirectoryBackground"),
        new(@"LibraryFolder\background", MenuCategory.Library, "Scope_LibraryBackground"),
        new("LibraryFolder", MenuCategory.Library, "Scope_LibraryFolder"),
        new("Directory", MenuCategory.Folder, "Scope_Directory"),
        new("Folder", MenuCategory.Folder, "Scope_Folder"),
        new("Drive", MenuCategory.Drive, "Scope_Drive"),
        new("*", MenuCategory.AllFiles, "Scope_AllFiles"),
        new("AllFilesystemObjects", MenuCategory.AllObjects, "Scope_AllFilesystemObjects"),
        new("Unknown", MenuCategory.OtherFileTypes, "Scope_Unknown"),
        new(@"SystemFileAssociations\image", MenuCategory.Images, "Scope_SfaImage"),
        new(@"SystemFileAssociations\video", MenuCategory.Video, "Scope_SfaVideo"),
        new(@"SystemFileAssociations\audio", MenuCategory.Audio, "Scope_SfaAudio"),
        new(@"SystemFileAssociations\text", MenuCategory.Documents, "Scope_SfaText"),
        new(@"SystemFileAssociations\document", MenuCategory.Documents, "Scope_SfaDocument"),
        new(@"SystemFileAssociations\compressed", MenuCategory.Archives, "Scope_SfaCompressed"),
        new(@"CLSID\" + RecycleBinClsid, MenuCategory.RecycleBin, "Scope_RecycleBin"),
        new(@"CLSID\" + ThisPcClsid, MenuCategory.ThisPC, "Scope_ThisPC"),
        new("Network", MenuCategory.Network, "Scope_Network"),
        new("NetServer", MenuCategory.Network, "Scope_NetServer"),
        new("NetShare", MenuCategory.Network, "Scope_NetShare"),
        new("Printers", MenuCategory.Printers, "Scope_Printers"),
    };

    private static readonly HashSet<string> Images = Set(".jpg .jpeg .jfif .png .gif .bmp .tif .tiff .webp .heic .heif .ico .svg .raw .cr2 .nef .dng .avif .jxl .psd");
    private static readonly HashSet<string> Videos = Set(".mp4 .mkv .avi .mov .wmv .webm .flv .m4v .mpg .mpeg .ts .m2ts .3gp .vob");
    private static readonly HashSet<string> Audios = Set(".mp3 .wav .flac .aac .ogg .opus .m4a .wma .aiff .ape .mid .midi");
    private static readonly HashSet<string> Docs = Set(".txt .doc .docx .rtf .odt .pdf .xls .xlsx .ods .csv .ppt .pptx .odp .md .log .ini .xml .json .html .htm .epub .djvu .fb2");
    private static readonly HashSet<string> Archives = Set(".zip .rar .7z .tar .gz .tgz .bz2 .xz .zst .cab .iso .arj .lzh .001 .wim");
    private static readonly HashSet<string> Executables = Set(".exe .msi .bat .cmd .ps1 .vbs .js .lnk .com .scr .reg .appx .msix");

    private static HashSet<string> Set(string list) => new(list.Split(' '), StringComparer.OrdinalIgnoreCase);

    public static MenuCategory CategoryForExtension(string ext, string? perceivedType)
    {
        if (Executables.Contains(ext)) return MenuCategory.Executables;
        if (Archives.Contains(ext)) return MenuCategory.Archives;
        switch (perceivedType?.ToLowerInvariant())
        {
            case "image": return MenuCategory.Images;
            case "video": return MenuCategory.Video;
            case "audio": return MenuCategory.Audio;
            case "text" or "document": return MenuCategory.Documents;
            case "compressed": return MenuCategory.Archives;
            case "application": return MenuCategory.Executables;
        }
        if (Images.Contains(ext)) return MenuCategory.Images;
        if (Videos.Contains(ext)) return MenuCategory.Video;
        if (Audios.Contains(ext)) return MenuCategory.Audio;
        if (Docs.Contains(ext)) return MenuCategory.Documents;
        return MenuCategory.OtherFileTypes;
    }

    /// <summary>Category for an arbitrary scope string (used for packaged items and custom entries).</summary>
    public static MenuCategory CategoryForScope(string scope)
    {
        var s = Static.FirstOrDefault(x => x.Scope.Equals(scope, StringComparison.OrdinalIgnoreCase));
        if (s != null) return s.Category;
        if (scope.StartsWith('.')) return CategoryForExtension(scope, null);
        if (scope.StartsWith(@"SystemFileAssociations\.", StringComparison.OrdinalIgnoreCase))
            return CategoryForExtension(scope["SystemFileAssociations\\".Length..], null);
        return MenuCategory.Other;
    }

    public static ScopeInfo? Find(string scope) => Static.FirstOrDefault(x => x.Scope.Equals(scope, StringComparison.OrdinalIgnoreCase));

    /// <summary>Scopes offered when the user creates a custom item.</summary>
    public static readonly IReadOnlyList<ScopeInfo> CustomTargets = new List<ScopeInfo>
    {
        Static[0], Static[1], Static[4], Static[6], Static[7], Static[8],
        Static[10], Static[11], Static[12], Static[13],
    };

    public static string NormalizeExtension(string ext)
    {
        ext = ext.Trim().ToLowerInvariant();
        if (ext.StartsWith("*")) ext = ext[1..];
        if (!ext.StartsWith('.')) ext = "." + ext;
        return ext;
    }

    public static bool IsValidExtension(string ext) =>
        ext.Length > 1 && ext[0] == '.' && ext.Skip(1).All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.' or '+');
}

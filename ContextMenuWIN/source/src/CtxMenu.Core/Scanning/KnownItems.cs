using CtxMenu.Core.Model;

namespace CtxMenu.Core.Scanning;

/// <summary>Knowledge about built-in Windows items: localized names for nameless verbs and items
/// whose removal breaks basic Explorer functions.</summary>
public static class KnownItems
{
    public static readonly IReadOnlyDictionary<string, string> VerbLocKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["open"] = "Verb_open", ["opennew"] = "Verb_opennew", ["openas"] = "Verb_openas", ["edit"] = "Verb_edit",
        ["print"] = "Verb_print", ["printto"] = "Verb_printto", ["runas"] = "Verb_runas", ["runasuser"] = "Verb_runasuser",
        ["explore"] = "Verb_explore", ["find"] = "Verb_find", ["cmd"] = "Verb_cmd", ["powershell"] = "Verb_powershell",
        ["play"] = "Verb_play", ["preview"] = "Verb_preview", ["pintohome"] = "Verb_pintohome", ["opennewwindow"] = "Verb_opennewwindow",
        ["opennewprocess"] = "Verb_opennewprocess", ["opennewtab"] = "Verb_opennewtab", ["setdesktopwallpaper"] = "Verb_wallpaper",
        ["rotateleft"] = "Verb_rotateleft", ["rotateright"] = "Verb_rotateright", ["enqueue"] = "Verb_enqueue",
        ["mount"] = "Verb_mount", ["burn"] = "Verb_burn", ["install"] = "Verb_install", ["uninstall"] = "Verb_uninstall",
        ["format"] = "Verb_format", ["properties"] = "Verb_properties", ["empty"] = "Verb_emptybin", ["edit_with_paint"] = "Verb_edit",
        ["WSL"] = "Verb_wsl", ["anycode"] = "Verb_anycode",
    };

    /// <summary>Verbs that Explorer uses as default actions for folders/drives/programs.</summary>
    private static readonly HashSet<string> CriticalVerbs = new(StringComparer.OrdinalIgnoreCase)
    { "open", "explore", "opennewwindow", "opennewprocess", "runas", "find", "empty" };

    private static readonly HashSet<string> CriticalVerbScopes = new(StringComparer.OrdinalIgnoreCase)
    { "Directory", "Folder", "Drive", "exefile", "lnkfile", "Directory\\Background", "DesktopBackground", "CLSID\\" + ScopeCatalog.RecycleBinClsid };

    /// <summary>Well-known Windows shell extension CLSIDs (upper-case) whose removal hides core functions.</summary>
    public static readonly IReadOnlyDictionary<string, string> CriticalHandlers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["{D969A300-E7FF-11d0-A93B-00A0C90F2719}"] = "Crit_New",        // Создать
        ["{09799AFB-AD67-11d1-ABCD-00C04FC30936}"] = "Crit_OpenWith",   // Открыть с помощью
        ["{7BA4C740-9E81-11CF-99D3-00AA004AE837}"] = "Crit_SendTo",     // Отправить
        ["{f81e9010-6ea4-11ce-a7ff-00aa003ca9f6}"] = "Crit_Sharing",    // Общий доступ
        ["{596AB062-B4D2-4215-9F74-E9109B0A8153}"] = "Crit_Previous",   // Восстановить прежнюю версию
    };

    private static readonly string[] CriticalHandlerNames = { "New", "Open With", "Sendto", "Sharing" };

    public static (bool critical, string? reasonKey) IsCritical(ContextMenuEntry e)
    {
        if (e.IsCustom) return (false, null);
        if (e.Kind == EntryKind.Verb && CriticalVerbs.Contains(e.KeyName.TrimStart('-')) &&
            (CriticalVerbScopes.Contains(e.Scope) || e.Scope.EndsWith("file", StringComparison.OrdinalIgnoreCase)))
            return (true, "Crit_DefaultVerb");
        if (e.Kind == EntryKind.ShellExtension)
        {
            if (e.Clsid != null && CriticalHandlers.TryGetValue(e.Clsid, out var key)) return (true, key);
            var name = e.KeyName.TrimStart('-');
            if (CriticalHandlerNames.Any(n => n.Equals(name, StringComparison.OrdinalIgnoreCase))) return (true, "Crit_Generic");
        }
        return (false, null);
    }

    public static string? DetectArchiver(ContextMenuEntry e)
    {
        var hay = string.Join("|", e.DisplayName, e.ModulePath, e.Company, e.Product, e.KeyName);
        if (hay.Contains("WinRAR", StringComparison.OrdinalIgnoreCase) || hay.Contains("rarext", StringComparison.OrdinalIgnoreCase)) return "winrar";
        if (hay.Contains("7-Zip", StringComparison.OrdinalIgnoreCase) || hay.Contains("7zip", StringComparison.OrdinalIgnoreCase)) return "7zip";
        return null;
    }
}

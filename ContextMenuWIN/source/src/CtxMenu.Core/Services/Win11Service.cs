using CtxMenu.Core.Editing;
using CtxMenu.Core.Localization;
using CtxMenu.Core.Registry;

namespace CtxMenu.Core.Services;

public sealed class Win11Service
{
    public const string ClassicMenuClsid = "{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}";
    public static readonly string ClassicMenuKey = @"Software\Classes\CLSID\" + ClassicMenuClsid;
    public static readonly string ClassicMenuInproc = ClassicMenuKey + @"\InprocServer32";

    private readonly RegistryEditor _editor;
    public Win11Service(RegistryEditor editor) => _editor = editor;

    public static bool IsWindows11(Version v) => v.Major >= 10 && v.Build >= 22000;

    public bool IsClassicMenuEnabled => _editor.Registry.KeyExists(RegHive.CurrentUser, ClassicMenuInproc);

    /// <summary>Win11: an empty InprocServer32 default value for this CLSID makes Explorer use the full classic menu.</summary>
    public ChangeSet? SetClassicMenu(bool classic)
    {
        if (classic == IsClassicMenuEnabled) return null;
        return _editor.Execute(Loc.Get(classic ? "Journal_ClassicOn" : "Journal_ClassicOff"), tx =>
        {
            if (classic) tx.SetString(RegHive.CurrentUser, ClassicMenuInproc, "", "");
            else tx.DeleteTree(RegHive.CurrentUser, ClassicMenuKey);
        });
    }
}

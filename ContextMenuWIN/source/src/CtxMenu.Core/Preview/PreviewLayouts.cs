namespace CtxMenu.Core.Preview;

/// <summary>Built-in parts of Explorer menus (Windows 11 23H2/24H2, Windows 10 classic), in the order Windows draws them.
/// Registry items are inserted at the Slot positions.</summary>
internal static class PreviewLayouts
{
    // Handler CLSIDs / key names of built-in shell extensions that draw well-known lines.
    private const string HNew = "{D969A300-E7FF-11d0-A93B-00A0C90F2719}|New";
    private const string HOpenWith = "{09799AFB-AD67-11d1-ABCD-00C04FC30936}|Open With";
    private const string HSendTo = "{7BA4C740-9E81-11CF-99D3-00AA004AE837}|SendTo|Sendto";
    private const string HSharing = "{f81e9010-6ea4-11ce-a7ff-00aa003ca9f6}|Sharing";
    private const string HModernShare = "{e2bf9676-5f8f-435c-97eb-11607a5bedf7}|ModernSharing";
    private const string HPrevious = "{596AB062-B4D2-4215-9F74-E9109B0A8153}";
    private const string HLibrary = "{3dad6c5d-2167-4cae-9914-f99e41c12cfa}|Library Location";
    private const string HPinStart = "{470C0EBD-5D73-4d58-9CED-E91E22E23282}|PintoStartScreen";
    private const string HCompat = "{1d27f844-3a1f-4410-85ac-14651078412d}|Compatibility";
    private const string HRotate = "{FFE2A43C-56B9-4bf5-9A79-CC6D4285608A}|ShellImagePreview";
    private const string HZipExtract = "{b8cdcb65-b1bf-4b42-9428-1dfdb7ee92af}|CompressedFolder";
    private const string HPinTaskbar = "{90AA3A4E-1CBA-4233-B8BB-535773D48449}|TaskbandPin";

    private static readonly B Sep = new(Slot: Slot.Separator);
    private static readonly B Top = new(Slot: Slot.Top);
    private static readonly B Reg = new(Slot: Slot.Registry);
    private static readonly B Bottom = new(Slot: Slot.Bottom);
    private static readonly B Pkg = new(Slot: Slot.Packaged);

    private static B[] DesktopView() => new[]
    {
        new B("Pv_ViewLarge"), new B("Pv_ViewMedium", Radio: true, Checked: true), new B("Pv_ViewSmall"), Sep,
        new B("Pv_AutoArrange"), new B("Pv_AlignGrid", Checked: true), Sep, new B("Pv_ShowIcons", Checked: true),
    };

    private static B[] DesktopSort() => new[] { new B("Pv_SortByName"), new B("Pv_SortSize"), new B("Pv_SortType"), new B("Pv_SortDate") };

    private static B[] FolderView() => new[]
    {
        new B("Pv_ViewXL"), new B("Pv_ViewLarge"), new B("Pv_ViewMedium"), new B("Pv_ViewSmall"),
        new B("Pv_ViewList"), new B("Pv_ViewDetails", Radio: true, Checked: true), new B("Pv_ViewTiles"), new B("Pv_ViewContent"),
    };

    private static B[] FolderSort() => new[]
    {
        new B("Pv_SortName", Radio: true, Checked: true), new B("Pv_SortDate"), new B("Pv_SortType"), new B("Pv_SortSize"), Sep,
        new B("Pv_SortAsc", Radio: true, Checked: true), new B("Pv_SortDesc"), Sep, new B("Pv_More"),
    };

    private static B[] FolderGroup() => new[]
    {
        new B("Pv_GroupNone", Radio: true, Checked: true), new B("Pv_SortName"), new B("Pv_SortDate"), new B("Pv_SortType"), new B("Pv_SortSize"), Sep, new B("Pv_More"),
    };

    private static B[] SendTo() => new[]
    {
        new B("Pv_SendBluetooth"), new B("Pv_SendZip"), new B("Pv_SendDesktop"), new B("Pv_SendDocs"), new B("Pv_SendMail"),
    };

    private static B[] OpenWith() => new[] { new B("Pv_OpenWithStore"), Sep, new B("Pv_ChooseApp") };

    private static B[] GiveAccess() => new[] { new B("Pv_AccessRemove"), new B("Pv_AccessPeople") };

    private static B[] Toolbar(bool cut, bool share, bool delete) =>
        new[]
        {
            cut ? new B("Pv_Cut", "\uE8C6", Toolbar: true) : null,
            new B("Pv_Copy", "\uE8C8", Toolbar: true),
            new B("Pv_Rename", "\uE8AC", Toolbar: true),
            share ? new B("Pv_Share", "\uE72D", Toolbar: true, Handler: HModernShare) : null,
            delete ? new B("Pv_Delete", "\uE74D", Toolbar: true) : null,
        }.Where(b => b != null).Select(b => b!).ToArray();

    public static IReadOnlyList<B> For(PreviewTarget t, PreviewStyle s) => s == PreviewStyle.Modern ? Modern(t) : Classic(t);

    // ---------------- Windows 11 compact menu ----------------

    private static IReadOnlyList<B> Modern(PreviewTarget t)
    {
        var showMore = new B("Pv_ShowMore", "\uE8A7", Shortcut: "Shift+F10", ShowMore: true);
        var undo = new B("Pv_Undo", "\uE7A7", "Ctrl+Z");
        var props = new B("Pv_Properties", "\uE946", "Alt+Enter");
        switch (t)
        {
            case PreviewTarget.DesktopBackground:
                return new[]
                {
                    new B("Pv_View", "\uF0E2", Sub: DesktopView()), new B("Pv_Sort", "\uE8CB", Sub: DesktopSort()), new B("Pv_Refresh", "\uE72C"), Sep,
                    undo, new B("Pv_New", "\uECC8", Handler: HNew, Slot: Slot.New), Sep,
                    new B("Pv_Display", "\uE7F4", Verb: "Display"), new B("Pv_Personalize", "\uE771", Verb: "Personalize"), Sep,
                    Pkg, Sep, showMore,
                };
            case PreviewTarget.FolderBackground:
                return new[]
                {
                    new B("Pv_View", "\uF0E2", Sub: FolderView()), new B("Pv_Sort", "\uE8CB", Sub: FolderSort()), new B("Pv_Group", "\uF168", Sub: FolderGroup()), Sep,
                    undo, new B("Pv_New", "\uECC8", Handler: HNew, Slot: Slot.New), Sep,
                    props, Sep, Pkg, Sep, showMore,
                };
            case PreviewTarget.Folder:
                return Toolbar(true, true, true).Concat(new[]
                {
                    new B("Pv_Open", "\uE838", "Enter", Verb: "open|explore", Default: true),
                    new B("Pv_OpenNewTab", "\uEC6C", Verb: "opennewtab"),
                    new B("Pv_OpenNewWindow", "\uE8A7", Verb: "opennewwindow"),
                    new B("Pv_PinQuick", "\uE718", Verb: "pintohome"),
                    new B("Pv_PinStart", "\uE718", Handler: HPinStart),
                    new B("Pv_Zip", "\uF012"), new B("Pv_CopyPath", "\uE62F", "Ctrl+Shift+C"),
                    props, Sep, Pkg, Sep, showMore,
                }).ToList();
            case PreviewTarget.Drive:
                return Toolbar(false, false, false).Concat(new[]
                {
                    new B("Pv_Open", "\uE838", "Enter", Verb: "open|explore", Default: true),
                    new B("Pv_OpenNewTab", "\uEC6C", Verb: "opennewtab"),
                    new B("Pv_OpenNewWindow", "\uE8A7", Verb: "opennewwindow"),
                    new B("Pv_PinQuick", "\uE718", Verb: "pintohome"),
                    new B("Pv_PinStart", "\uE718", Handler: HPinStart),
                    new B("Pv_Format", "\uEDA2", Verb: "format"),
                    props, Sep, Pkg, Sep, showMore,
                }).ToList();
            default:
                var list = Toolbar(true, true, true).ToList();
                list.Add(new B("Pv_Open", "\uE8E5", "Enter", Verb: "open|play", Default: true));
                list.Add(new B("Pv_OpenWith", "\uE7AC", Handler: HOpenWith, Sub: OpenWith()));
                if (t == PreviewTarget.Image)
                {
                    list.Add(new B("Pv_Wallpaper", "\uE7F4", Verb: "setdesktopwallpaper"));
                    list.Add(new B("Pv_RotateRight", "\uE7AD", Handler: HRotate));
                    list.Add(new B("Pv_RotateLeft", "\uE80C"));
                }
                if (t == PreviewTarget.Archive) list.Add(new B("Pv_Extract", "\uE8B7", Handler: HZipExtract));
                if (t == PreviewTarget.Executable)
                {
                    list.Add(new B("Pv_RunAs", "\uEA18", Verb: "runas"));
                    list.Add(new B("Pv_PinStart", "\uE718", Handler: HPinStart));
                }
                list.AddRange(new[] { new B("Pv_Zip", "\uF012"), new B("Pv_CopyPath", "\uE62F", "Ctrl+Shift+C"), props, Sep, Pkg, Sep, showMore });
                return list;
        }
    }

    // ---------------- classic menu (Windows 10 / "Показать дополнительные параметры") ----------------

    private static IReadOnlyList<B> Classic(PreviewTarget t)
    {
        var props = new B("Pv_Properties");
        var undo = new B("Pv_Undo", Shortcut: "CTRL+Z");
        switch (t)
        {
            case PreviewTarget.DesktopBackground:
                return new[]
                {
                    Top,
                    new B("Pv_View", Sub: DesktopView()), new B("Pv_Sort", Sub: DesktopSort()), new B("Pv_Refresh"), Sep,
                    new B("Pv_Paste", Greyed: true), undo, Reg, Sep,
                    new B("Pv_New", Handler: HNew, Slot: Slot.New), Sep,
                    Bottom, new B("Pv_Display", Verb: "Display"), new B("Pv_Personalize", Verb: "Personalize"),
                };
            case PreviewTarget.FolderBackground:
                return new[]
                {
                    Top,
                    new B("Pv_View", Sub: FolderView()), new B("Pv_Sort", Sub: FolderSort()), new B("Pv_Group", Sub: FolderGroup()), new B("Pv_Refresh"), Sep,
                    new B("Pv_CustomizeFolder"), Sep,
                    new B("Pv_Paste", Greyed: true), new B("Pv_PasteShortcut", Greyed: true), undo, Reg, Sep,
                    new B("Pv_GiveAccess", Handler: HSharing, Sub: GiveAccess()), new B("Pv_New", Handler: HNew, Slot: Slot.New), Sep,
                    Bottom, props,
                };
            case PreviewTarget.Folder:
                return new[]
                {
                    Top,
                    new B("Pv_Open", Verb: "open|explore", Default: true), new B("Pv_OpenNewWindow", Verb: "opennewwindow"),
                    new B("Pv_PinQuick", Verb: "pintohome"), Reg,
                    new B("Pv_GiveAccess", Handler: HSharing, Sub: GiveAccess()), new B("Pv_PrevVersions", Handler: HPrevious),
                    new B("Pv_IncludeLibrary", Handler: HLibrary, Dynamic: true), new B("Pv_PinStart", Handler: HPinStart), Sep,
                    new B("Pv_SendTo", Handler: HSendTo, Sub: SendTo()), Sep,
                    new B("Pv_Cut"), new B("Pv_Copy"), Sep,
                    new B("Pv_CreateShortcut"), new B("Pv_Delete"), new B("Pv_Rename"), Sep,
                    Bottom, props,
                };
            case PreviewTarget.Drive:
                return new[]
                {
                    Top,
                    new B("Pv_Open", Verb: "open|explore", Default: true), new B("Pv_OpenNewWindow", Verb: "opennewwindow"),
                    new B("Pv_PinQuick", Verb: "pintohome"), Reg,
                    new B("Pv_GiveAccess", Handler: HSharing, Sub: GiveAccess()), new B("Pv_PrevVersions", Handler: HPrevious),
                    new B("Pv_PinStart", Handler: HPinStart), Sep,
                    new B("Pv_Format", Verb: "format"), Sep,
                    new B("Pv_Copy"), Sep,
                    new B("Pv_CreateShortcut"), new B("Pv_Rename"), Sep,
                    Bottom, props,
                };
            default:
                var list = new List<B> { Top, new B("Pv_Open", Verb: "open|play", Default: true) };
                if (t == PreviewTarget.Image) list.Add(new B("Pv_Wallpaper", Verb: "setdesktopwallpaper"));
                if (t == PreviewTarget.Executable)
                {
                    list.Add(new B("Pv_RunAs", Verb: "runas"));
                    list.Add(new B("Pv_Troubleshoot", Handler: HCompat));
                    list.Add(new B("Pv_PinStart", Handler: HPinStart));
                }
                if (t == PreviewTarget.Archive) list.Add(new B("Pv_Extract", Handler: HZipExtract));
                list.Add(Reg);
                if (t == PreviewTarget.Image) list.Add(new B("Pv_RotateRight", Handler: HRotate));
                list.Add(new B("Pv_OpenWith", Handler: HOpenWith, Sub: OpenWith()));
                if (t == PreviewTarget.Executable) list.Add(new B("Pv_PinTaskbar", Handler: HPinTaskbar));
                list.AddRange(new[]
                {
                    Sep, new B("Pv_GiveAccess", Handler: HSharing, Sub: GiveAccess()), new B("Pv_PrevVersions", Handler: HPrevious), Sep,
                    new B("Pv_SendTo", Handler: HSendTo, Sub: SendTo()), Sep,
                    new B("Pv_Cut"), new B("Pv_Copy"), Sep,
                    new B("Pv_CreateShortcut"), new B("Pv_Delete"), new B("Pv_Rename"), Sep,
                    Bottom, props,
                });
                return list;
        }
    }
}

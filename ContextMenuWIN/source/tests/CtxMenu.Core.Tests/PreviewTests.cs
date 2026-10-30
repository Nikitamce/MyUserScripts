using CtxMenu.Core.Model;
using CtxMenu.Core.Preview;
using CtxMenu.Core.Registry;
using CtxMenu.Core.Services;

namespace CtxMenu.Core.Tests;

public class PreviewTargetTests
{
    private static IEnumerable<string> NoExt(string ext) => new[] { "*", "AllFilesystemObjects", ext, @"SystemFileAssociations\" + ext, "Unknown" };

    [Fact]
    public void DesktopBackground_IncludesDirectoryBackground_NotFolders()
    {
        var s = PreviewTargets.ScopesFor(PreviewTarget.DesktopBackground, null, NoExt);
        Assert.Contains("DesktopBackground", s);
        Assert.Contains(@"Directory\Background", s);
        Assert.DoesNotContain("Directory", s);
        Assert.DoesNotContain("*", s);
    }

    [Fact]
    public void FolderBackground_OnlyDirectoryBackground()
    {
        var s = PreviewTargets.ScopesFor(PreviewTarget.FolderBackground, null, NoExt);
        Assert.Equal(new[] { @"Directory\Background" }, s.ToArray());
    }

    [Fact]
    public void Folder_And_Drive_Scopes()
    {
        var f = PreviewTargets.ScopesFor(PreviewTarget.Folder, null, NoExt);
        Assert.Contains("Directory", f);
        Assert.Contains("Folder", f);
        Assert.Contains("AllFilesystemObjects", f);
        Assert.DoesNotContain("*", f);
        Assert.DoesNotContain(@"Directory\Background", f);

        var d = PreviewTargets.ScopesFor(PreviewTarget.Drive, null, NoExt);
        Assert.Contains("Drive", d);
        Assert.Contains("Folder", d);
        Assert.DoesNotContain("Directory", d);
    }

    [Fact]
    public void FileTargets_UseDefaultOrUserExtension()
    {
        Assert.Equal(".jpg", PreviewTargets.EffectiveExtension(PreviewTarget.Image, null));
        Assert.Equal(".mp4", PreviewTargets.EffectiveExtension(PreviewTarget.Video, "  "));
        Assert.Equal(".png", PreviewTargets.EffectiveExtension(PreviewTarget.Image, "PNG"));
        Assert.Equal(".txt", PreviewTargets.EffectiveExtension(PreviewTarget.File, "bad ext!"));
        Assert.Null(PreviewTargets.EffectiveExtension(PreviewTarget.Folder, ".jpg"));

        var s = PreviewTargets.ScopesFor(PreviewTarget.Video, null, NoExt);
        Assert.Contains(".mp4", s);
        Assert.Contains("*", s);
        Assert.Contains("AllFilesystemObjects", s);
    }

    [Fact]
    public void Image_UsesScannerScopes_ProgIdAndPerceivedType()
    {
        using var env = new TestEnv();
        env.Reg.Set(RegHive.LocalMachine, TestEnv.Cls(".jpg"), "", "jpegfile").Set(RegHive.LocalMachine, TestEnv.Cls(".jpg"), "PerceivedType", "image")
            .Set(RegHive.LocalMachine, TestEnv.Cls(@"jpegfile\shell\open\command"), "", "x.exe");
        var scanner = env.Scanner();
        var s = PreviewTargets.ScopesFor(PreviewTarget.Image, null, scanner.GetScopesForExtension);
        Assert.Contains("jpegfile", s);
        Assert.Contains(@"SystemFileAssociations\image", s);
        Assert.Contains(@"SystemFileAssociations\.jpg", s);
        Assert.DoesNotContain(@"SystemFileAssociations\video", s);
        Assert.DoesNotContain("Unknown", s);
    }

    [Fact]
    public void Packaged_ItemTypes_Match()
    {
        var bg = new ContextMenuEntry { Kind = EntryKind.Packaged, Scope = @"Directory\Background, Directory" };
        var star = new ContextMenuEntry { Kind = EntryKind.Packaged, Scope = "*" };
        var desk = PreviewTargets.ScopesFor(PreviewTarget.DesktopBackground, null, NoExt);
        var folder = PreviewTargets.ScopesFor(PreviewTarget.Folder, null, NoExt);
        var image = PreviewTargets.ScopesFor(PreviewTarget.Image, null, NoExt);
        Assert.True(PreviewTargets.Applies(bg, desk));
        Assert.True(PreviewTargets.Applies(bg, folder));
        Assert.False(PreviewTargets.Applies(bg, image));
        Assert.True(PreviewTargets.Applies(star, image));
        Assert.False(PreviewTargets.Applies(star, folder));
        Assert.False(PreviewTargets.Applies(star, desk));
    }
}

public class MenuPreviewBuilderTests
{
    private const RegHive CU = RegHive.CurrentUser, LM = RegHive.LocalMachine;
    private const string NewClsid = "{D969A300-E7FF-11d0-A93B-00A0C90F2719}";

    private static (TestEnv env, List<ContextMenuEntry> entries, List<ShellNewItem> shellNew) Desktop()
    {
        var env = new TestEnv();
        var r = env.Reg;
        r.Set(LM, TestEnv.Cls(@"DesktopBackground\Shell\Display"), "MUIVerb", "Параметры экрана").Set(LM, TestEnv.Cls(@"DesktopBackground\Shell\Display"), "Position", "Bottom")
         .Set(LM, TestEnv.Cls(@"DesktopBackground\Shell\Personalize"), "MUIVerb", "Персонализация").Set(LM, TestEnv.Cls(@"DesktopBackground\Shell\Personalize"), "Position", "Bottom");
        r.Set(LM, TestEnv.Cls(@"Directory\Background\shell\VSCode"), "", "Open with Code")
         .Set(LM, TestEnv.Cls(@"Directory\Background\shell\VSCode\command"), "", "code.exe \"%V\"");
        r.Set(LM, TestEnv.Cls(@"Directory\Background\shell\cmd"), "Extended", "")
         .Set(LM, TestEnv.Cls(@"Directory\Background\shell\cmd\command"), "", "cmd.exe");
        r.Set(CU, TestEnv.Cls(@"Directory\Background\shell\Old"), "MUIVerb", "Old tool").Set(CU, TestEnv.Cls(@"Directory\Background\shell\Old"), "LegacyDisable", "")
         .Set(CU, TestEnv.Cls(@"Directory\Background\shell\Old\command"), "", "old.exe");
        r.Set(LM, TestEnv.Cls(@"Directory\Background\shellex\ContextMenuHandlers\New"), "", NewClsid);
        r.Set(LM, TestEnv.Cls(@"*\shell\FileOnly"), "MUIVerb", "File only").Set(LM, TestEnv.Cls(@"*\shell\FileOnly\command"), "", "f.exe");
        // ShellNew
        r.Set(LM, TestEnv.Cls(@"Folder\ShellNew"), "Directory", "");
        r.Set(LM, TestEnv.Cls(@".txt"), "", "txtfile").Set(LM, TestEnv.Cls(@".txt\ShellNew"), "NullFile", "")
         .Set(LM, TestEnv.Cls("txtfile"), "", "Текстовый документ");
        r.Set(LM, TestEnv.Cls(@".docx\-ShellNew"), "NullFile", "").Set(LM, TestEnv.Cls(@".docx\-ShellNew"), "ItemName", "Документ Word");

        var entries = env.Scanner().Scan();
        entries.Add(new ContextMenuEntry
        {
            Kind = EntryKind.Packaged, Scope = @"Directory\Background", DisplayName = "Открыть в Терминале",
            Clsid = "{9F156763-7844-4DC4-B2B1-901F640F5155}", PackageName = "Microsoft.WindowsTerminal",
        });
        var shellNew = new ShellNewService(env.Editor, new PassThroughResolver(), env.Dir).Scan();
        return (env, entries, shellNew);
    }

    private static List<PreviewItem> Build(List<ContextMenuEntry> entries, List<ShellNewItem> sn, PreviewStyle style, bool showDisabled = false, bool showExtended = false, PreviewTarget target = PreviewTarget.DesktopBackground)
    {
        var o = new PreviewOptions(target, null, style, showDisabled, showExtended);
        var scopes = PreviewTargets.ScopesFor(target, null, e => new[] { e });
        return MenuPreviewBuilder.Build(o, entries, sn, scopes);
    }

    private static List<string> Texts(IEnumerable<PreviewItem> items) => items.Select(i => i.ToString()).ToList();

    [Fact]
    public void Classic_Desktop_MatchesWindowsOrder()
    {
        var (env, entries, sn) = Desktop();
        using var _ = env;
        var menu = Build(entries, sn, PreviewStyle.Classic);
        Assert.Equal(new[]
        {
            "Вид", "Сортировка", "Обновить", "----", "Вставить", "Отменить удаление", "Open with Code", "Открыть в Терминале", "----",
            "Создать", "----", "Параметры экрана", "Персонализация",
        }, Texts(menu));
        Assert.True(menu.Single(i => i.Text == "Вставить").IsGreyed);
        // bound to the registry: toggling them really hides the lines
        Assert.Equal("Display", menu.Single(i => i.Text == "Параметры экрана").Entry!.KeyName);
        Assert.Equal(NewClsid, menu.Single(i => i.Text == "Создать").Entry!.Clsid);
        Assert.DoesNotContain(menu, i => i.Text == "File only");
    }

    [Fact]
    public void Modern_Desktop_HidesRegistryVerbs_ShowsPackaged()
    {
        var (env, entries, sn) = Desktop();
        using var _ = env;
        var menu = Build(entries, sn, PreviewStyle.Modern);
        Assert.Equal(new[]
        {
            "Вид", "Сортировка", "Обновить", "----", "Отменить удаление", "Создать", "----",
            "Параметры экрана", "Персонализация", "----", "Открыть в Терминале", "----", "Показать дополнительные параметры",
        }, Texts(menu));
        Assert.True(menu[^1].IsShowMore);
        Assert.NotNull(menu.Single(i => i.Text == "Открыть в Терминале").Entry);
    }

    [Fact]
    public void DisabledAndShiftItems_FollowOptions()
    {
        var (env, entries, sn) = Desktop();
        using var _ = env;
        var plain = Build(entries, sn, PreviewStyle.Classic);
        Assert.DoesNotContain(plain, i => i.Text == "Old tool");
        Assert.DoesNotContain(plain, i => i.Verb == "cmd");

        var all = Build(entries, sn, PreviewStyle.Classic, showDisabled: true, showExtended: true);
        var old = all.Single(i => i.Text == "Old tool");
        Assert.True(old.IsHiddenByUser);
        Assert.True(old.IsEditable);
        var cmd = all.Single(i => i.Verb == "cmd");
        Assert.True(cmd.IsExtendedOnly);
    }

    [Fact]
    public void DisablingBoundHandler_RemovesBuiltInLine()
    {
        var (env, entries, sn) = Desktop();
        using var _ = env;
        var newHandler = entries.Single(e => e.Clsid == NewClsid);
        new ContextMenuActions(env.Editor).SetEnabled(newHandler, false);
        Assert.DoesNotContain(Build(entries, sn, PreviewStyle.Classic), i => i.Text == "Создать");
        var shown = Build(entries, sn, PreviewStyle.Classic, showDisabled: true).Single(i => i.Text == "Создать");
        Assert.True(shown.IsHiddenByUser);
    }

    [Fact]
    public void NewSubmenu_ListsShellNewItems()
    {
        var (env, entries, sn) = Desktop();
        using var _ = env;
        var create = Build(entries, sn, PreviewStyle.Classic).Single(i => i.Text == "Создать");
        Assert.Equal(new[] { "Папку", "Ярлык", "----", "Текстовый документ" }, Texts(create.Children));
        Assert.NotNull(create.Children[0].NewItem); // Folder\ShellNew is toggleable
        Assert.Null(create.Children[1].NewItem);    // .lnk not registered in this test → static line

        var withOff = Build(entries, sn, PreviewStyle.Classic, showDisabled: true).Single(i => i.Text == "Создать");
        Assert.Contains(withOff.Children, c => c.Text == "Документ Word" && c.IsHiddenByUser);
    }

    [Fact]
    public void Pool_PrefersSpecificScope_AndHkcu_ForDuplicateVerbs()
    {
        var a = new ContextMenuEntry { Kind = EntryKind.Verb, Hive = LM, Scope = "*", KeyName = "edit", DisplayName = "generic" };
        var b = new ContextMenuEntry { Kind = EntryKind.Verb, Hive = LM, Scope = "jpegfile", KeyName = "edit", DisplayName = "progid" };
        var c = new ContextMenuEntry { Kind = EntryKind.Verb, Hive = CU, Scope = "jpegfile", KeyName = "edit", DisplayName = "user" };
        var scopes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "*", "jpegfile" };
        var pool = MenuPreviewBuilder.Pool(new[] { a, b, c }, scopes);
        Assert.Single(pool);
        Assert.Equal("user", pool[0].DisplayName);
    }

    [Fact]
    public void Separators_AreCollapsed()
    {
        var items = new List<PreviewItem> { PreviewItem.Separator(), new() { Text = "a" }, PreviewItem.Separator(), PreviewItem.Separator(), new() { Text = "b" }, PreviewItem.Separator() };
        Assert.Equal(new[] { "a", "----", "b" }, Texts(MenuPreviewBuilder.Clean(items)));
    }

    [Fact]
    public void FileTarget_ShowsFileVerbs_NotBackgroundOnes()
    {
        var (env, entries, sn) = Desktop();
        using var _ = env;
        var menu = Build(entries, sn, PreviewStyle.Classic, target: PreviewTarget.File);
        Assert.Contains(menu, i => i.Text == "File only");
        Assert.DoesNotContain(menu, i => i.Text == "Open with Code");
        Assert.Equal("Свойства", menu[^1].Text);
    }

    [Fact]
    public void MatchReal_LinksByVerbAndText_AndAppendsDisabled()
    {
        var (env, entries, sn) = Desktop();
        using var _ = env;
        var real = new List<PreviewItem>
        {
            new() { Kind = PreviewItemKind.Real, Text = "&Open with Code", Verb = null },
            new() { Kind = PreviewItemKind.Real, Text = "Параметры экрана", Verb = "Display" },
            new() { Kind = PreviewItemKind.Real, Text = "Something else" },
        };
        var scopes = PreviewTargets.ScopesFor(PreviewTarget.DesktopBackground, null, e => new[] { e });
        var o = new PreviewOptions(PreviewTarget.DesktopBackground, null, PreviewStyle.Classic, ShowDisabled: true);
        var r = MenuPreviewBuilder.MatchReal(real, entries, scopes, o);
        Assert.Equal("VSCode", r[0].Entry!.KeyName);
        Assert.Equal("Display", r[1].Entry!.KeyName);
        Assert.Null(r[2].Entry);
        Assert.Contains(r, i => i.Text == "Old tool" && i.IsHiddenByUser);
    }
}

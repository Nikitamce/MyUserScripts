using CtxMenu.Core.Model;
using CtxMenu.Core.Registry;
using CtxMenu.Core.Scanning;

namespace CtxMenu.Core.Tests;

public class ScannerTests
{
    private const RegHive CU = RegHive.CurrentUser, LM = RegHive.LocalMachine;
    private const string Clsid = "{11111111-2222-3333-4444-555555555555}";

    private static TestEnv Sample()
    {
        var env = new TestEnv();
        var r = env.Reg;
        // classic verb with submenu on folder background
        r.Set(LM, TestEnv.Cls(@"Directory\Background\shell\VSCode"), "", "Open with &Code")
         .Set(LM, TestEnv.Cls(@"Directory\Background\shell\VSCode\command"), "", "\"C:\\Program Files\\Code\\Code.exe\" \"%V\"");
        r.Set(LM, TestEnv.Cls(@"Directory\Background\shell\cmd"), "Extended", "")
         .Set(LM, TestEnv.Cls(@"Directory\Background\shell\cmd\command"), "", "cmd.exe /s /k pushd \"%V\"");
        // disabled verb
        r.Set(CU, TestEnv.Cls(@"*\shell\Hidden"), "MUIVerb", "Hidden one").Set(CU, TestEnv.Cls(@"*\shell\Hidden"), "LegacyDisable", "")
         .Set(CU, TestEnv.Cls(@"*\shell\Hidden\command"), "", "notepad.exe \"%1\"");
        // shell extension handler on all files
        r.Set(LM, TestEnv.Cls(@"*\shellex\ContextMenuHandlers\FooExt"), "", Clsid)
         .Set(LM, TestEnv.Cls(@"CLSID\" + Clsid), "", "Foo Shell Ext")
         .Set(LM, TestEnv.Cls(@"CLSID\" + Clsid + @"\InprocServer32"), "", @"C:\Program Files\Foo\foo.dll");
        // renamed (disabled) handler
        r.Set(LM, TestEnv.Cls(@"Directory\shellex\ContextMenuHandlers\-Bar"), "", "{22222222-2222-3333-4444-555555555555}");
        // nested submenu
        r.Set(CU, TestEnv.Cls(@"Directory\shell\Tools"), "MUIVerb", "Tools").Set(CU, TestEnv.Cls(@"Directory\shell\Tools"), "SubCommands", "")
         .Set(CU, TestEnv.Cls(@"Directory\shell\Tools\shell\a"), "MUIVerb", "A").Set(CU, TestEnv.Cls(@"Directory\shell\Tools\shell\a\command"), "", "a.exe")
         .Set(CU, TestEnv.Cls(@"Directory\shell\Tools\shell\b"), "MUIVerb", "B").Set(CU, TestEnv.Cls(@"Directory\shell\Tools\shell\b\command"), "", "b.exe");
        // extension → progid with verb
        r.Set(LM, TestEnv.Cls(".jpg"), "", "jpegfile").Set(LM, TestEnv.Cls(".jpg"), "PerceivedType", "image")
         .Set(LM, TestEnv.Cls(@"jpegfile\shell\edit\command"), "", "mspaint.exe \"%1\"")
         .Set(LM, TestEnv.Cls(@"SystemFileAssociations\image\shell\rotate"), "MUIVerb", "Rotate");
        env.Files.Meta[@"C:\Program Files\Code\Code.exe"] = new FileMeta("Microsoft Corporation", "Visual Studio Code", "Code");
        env.Files.Meta[@"C:\Program Files\Foo\foo.dll"] = new FileMeta("Foo Inc.", "Foo Tools", "Foo ext");
        return env;
    }

    [Fact]
    public void Scan_FindsVerbsHandlersAndSubmenus()
    {
        using var env = Sample();
        var all = env.Scanner().Scan();

        var code = all.Single(e => e.KeyName == "VSCode");
        Assert.Equal("Open with Code", code.DisplayName);
        Assert.Equal(MenuCategory.Background, code.Category);
        Assert.Equal(@"C:\Program Files\Code\Code.exe", code.ModulePath);
        Assert.False(code.IsThirdParty); // Microsoft-signed
        Assert.Equal("Visual Studio Code", code.Product);

        var cmd = all.Single(e => e.KeyName == "cmd");
        Assert.True(cmd.IsExtended);
        Assert.False(cmd.IsThirdParty);

        var hidden = all.Single(e => e.KeyName == "Hidden");
        Assert.Equal(DisableState.LegacyDisable, hidden.State);
        Assert.Equal(RegHive.CurrentUser, hidden.Hive);

        var foo = all.Single(e => e.KeyName == "FooExt");
        Assert.Equal(EntryKind.ShellExtension, foo.Kind);
        Assert.Equal(Clsid, foo.Clsid);
        Assert.True(foo.IsThirdParty);
        Assert.Equal("Foo Inc.", foo.Company);
        Assert.Equal(MenuCategory.AllFiles, foo.Category);

        var bar = all.Single(e => e.KeyName == "-Bar");
        Assert.Equal(DisableState.RenamedKey, bar.State);
        Assert.Equal("Bar", bar.DisplayName);

        var tools = all.Single(e => e.KeyName == "Tools");
        Assert.Equal(SubmenuKind.ShellSubkey, tools.Submenu);
        Assert.Equal(new[] { "A", "B" }, tools.Children.Select(c => c.DisplayName).OrderBy(x => x));
        Assert.All(tools.Children, c => Assert.Same(tools, c.Parent));

        var edit = all.Single(e => e.Scope == "jpegfile" && e.KeyName == "edit");
        Assert.Equal(MenuCategory.Images, edit.Category);
        Assert.Contains(".jpg", edit.AppliesToExtensions);
        Assert.Equal(MenuCategory.Images, all.Single(e => e.KeyName == "rotate").Category);
    }

    [Fact]
    public void Scan_DetectsBlockedClsid()
    {
        using var env = Sample();
        env.Reg.Set(CU, ContextMenuScanner.BlockedPath, Clsid, "");
        var foo = env.Scanner().Scan().Single(e => e.KeyName == "FooExt");
        Assert.Equal(DisableState.BlockedCurrentUser, foo.State);
    }

    [Fact]
    public void Scan_SubCommandsFromCommandStore()
    {
        using var env = new TestEnv();
        env.Reg.Set(LM, TestEnv.Cls(@"*\shell\Grp"), "SubCommands", "Windows.copy;|;Windows.paste")
               .Set(LM, TestEnv.Cls(@"*\shell\Grp"), "MUIVerb", "Group");
        env.Reg.Set(LM, ContextMenuScanner.CommandStore + @"\Windows.copy", "MUIVerb", "Copy")
               .Set(LM, ContextMenuScanner.CommandStore + @"\Windows.paste", "MUIVerb", "Paste");
        var grp = env.Scanner().Scan().Single(e => e.KeyName == "Grp");
        Assert.Equal(SubmenuKind.SubCommands, grp.Submenu);
        Assert.Equal(new[] { "Copy", "Paste" }, grp.Children.Select(c => c.DisplayName));
    }

    [Fact]
    public void Scan_ExtendedSubCommandsKey()
    {
        using var env = new TestEnv();
        env.Reg.Set(CU, TestEnv.Cls(@"Directory\shell\Ext"), "ExtendedSubCommandsKey", @"MyMenu")
               .Set(CU, TestEnv.Cls(@"MyMenu\shell\one"), "MUIVerb", "One");
        var ext = env.Scanner().Scan().Single(e => e.KeyName == "Ext");
        Assert.Equal(SubmenuKind.ExtendedSubCommandsKey, ext.Submenu);
        Assert.Equal(TestEnv.Cls(@"MyMenu\shell"), ext.SubmenuShellPath);
        Assert.Equal("One", Assert.Single(ext.Children).DisplayName);
    }

    [Fact]
    public void CriticalItems_AreFlagged()
    {
        using var env = new TestEnv();
        env.Reg.Set(LM, TestEnv.Cls(@"Directory\Background\shellex\ContextMenuHandlers\New"), "", "{D969A300-E7FF-11d0-A93B-00A0C90F2719}");
        env.Reg.Set(LM, TestEnv.Cls(@"Directory\shell\open\command"), "", "explorer.exe");
        env.Reg.Set(LM, TestEnv.Cls(@"Directory\shell\Other\command"), "", "x.exe");
        var all = env.Scanner().Scan();
        Assert.True(all.Single(e => e.KeyName == "New").IsCritical);
        Assert.True(all.Single(e => e.KeyName == "open").IsCritical);
        Assert.False(all.Single(e => e.KeyName == "Other").IsCritical);
    }

    [Fact]
    public void GetScopesForExtension_IncludesProgIdPerceivedAndWildcards()
    {
        using var env = Sample();
        var s = env.Scanner().GetScopesForExtension("JPG");
        Assert.Contains("*", s);
        Assert.Contains("AllFilesystemObjects", s);
        Assert.Contains(".jpg", s);
        Assert.Contains("jpegfile", s);
        Assert.Contains(@"SystemFileAssociations\image", s);
        Assert.DoesNotContain("Unknown", s);
        Assert.Contains("Unknown", env.Scanner().GetScopesForExtension(".zzz"));
    }

    [Fact]
    public void UserChoiceProgId_IsScanned()
    {
        using var env = new TestEnv();
        env.Reg.Set(LM, TestEnv.Cls(".txt"), "", "txtfile");
        env.Reg.Set(CU, ContextMenuScanner.FileExts + @"\.txt\UserChoice", "ProgId", "Notepad++.txt");
        env.Reg.Set(LM, TestEnv.Cls(@"Notepad++.txt\shell\npp\command"), "", "npp.exe");
        var scopes = env.Scanner().BuildScopes();
        Assert.True(scopes.ContainsKey("Notepad++.txt"));
        Assert.Equal(MenuCategory.Documents, scopes["Notepad++.txt"].Category);
    }

    [Fact]
    public void ThirdParty_Heuristics()
    {
        using var env = new TestEnv();
        var s = env.Scanner();
        Assert.False(s.IsThirdParty(null, @"C:\Windows\System32\shell32.dll"));
        Assert.False(s.IsThirdParty(null, "shell32.dll"));
        Assert.True(s.IsThirdParty(null, @"D:\Apps\x.dll"));
        Assert.False(s.IsThirdParty("Microsoft Corporation", @"D:\Apps\x.dll"));
        Assert.True(s.IsThirdParty("Igor Pavlov", @"C:\Windows\x.dll"));
        Assert.False(s.IsThirdParty(null, null));
    }

    [Fact]
    public void StableId_SurvivesRename()
    {
        var a = new ContextMenuEntry { Kind = EntryKind.ShellExtension, Hive = LM, KeyPath = @"Software\Classes\*\shellex\ContextMenuHandlers\Foo" };
        var b = new ContextMenuEntry { Kind = EntryKind.ShellExtension, Hive = LM, KeyPath = @"Software\Classes\*\shellex\ContextMenuHandlers\-Foo" };
        Assert.Equal(a.StableId, b.StableId);
    }
}

public class HelpersTests
{
    [Theory]
    [InlineData("\"C:\\Program Files\\App\\app.exe\" \"%1\"", "C:\\Program Files\\App\\app.exe")]
    [InlineData("C:\\Program Files\\App\\app.exe %1", "C:\\Program Files\\App\\app.exe")]
    [InlineData("notepad.exe \"%1\"", "notepad.exe")]
    [InlineData("rundll32.exe C:\\x\\shimgvw.dll,ImageView_Fullscreen %1", "C:\\x\\shimgvw.dll")]
    [InlineData("rundll32 \"C:\\y z\\a.dll\",Entry", "C:\\y z\\a.dll")]
    [InlineData("cmd", "cmd")]
    public void ExtractExecutable(string cmd, string expected) => Assert.Equal(expected, CommandLine.ExtractExecutable(cmd));

    [Fact]
    public void ExtractExecutable_Empty() => Assert.Null(CommandLine.ExtractExecutable("  "));

    [Theory]
    [InlineData("C:\\a.dll,-12", "C:\\a.dll", -12)]
    [InlineData("\"C:\\a b.exe\",3", "C:\\a b.exe", 3)]
    [InlineData("C:\\a.ico", "C:\\a.ico", 0)]
    public void ParseIcon(string loc, string path, int index) => Assert.Equal((path, index), CommandLine.ParseIconLocation(loc));

    [Fact]
    public void StripAccelerators() => Assert.Equal("Save & Open", CommandLine.StripAccelerators("&Save && Open"));

    [Theory]
    [InlineData("mp4", ".mp4")]
    [InlineData("*.TXT", ".txt")]
    [InlineData(" .Md ", ".md")]
    public void NormalizeExtension(string input, string expected) => Assert.Equal(expected, ScopeCatalog.NormalizeExtension(input));

    [Theory]
    [InlineData(".tar.gz", true)]
    [InlineData(".c++", true)]
    [InlineData(".", false)]
    [InlineData(".a b", false)]
    [InlineData(".a\\b", false)]
    public void IsValidExtension(string ext, bool ok) => Assert.Equal(ok, ScopeCatalog.IsValidExtension(ext));

    [Theory]
    [InlineData(".exe", null, MenuCategory.Executables)]
    [InlineData(".zip", null, MenuCategory.Archives)]
    [InlineData(".xyz", "video", MenuCategory.Video)]
    [InlineData(".png", null, MenuCategory.Images)]
    [InlineData(".flac", null, MenuCategory.Audio)]
    [InlineData(".qqq", null, MenuCategory.OtherFileTypes)]
    public void CategoryForExtension(string ext, string? perceived, MenuCategory cat) => Assert.Equal(cat, ScopeCatalog.CategoryForExtension(ext, perceived));

    [Fact]
    public void IsGuid()
    {
        Assert.True(CommandLine.IsGuid("{D969A300-E7FF-11d0-A93B-00A0C90F2719}"));
        Assert.False(CommandLine.IsGuid("D969A300-E7FF-11d0-A93B-00A0C90F2719"));
        Assert.False(CommandLine.IsGuid("New"));
    }

    [Fact]
    public void Localization_AllKeysHaveBothLanguages()
    {
        foreach (var (k, (ru, en)) in CtxMenu.Core.Localization.Strings.Table)
        {
            Assert.False(string.IsNullOrWhiteSpace(ru), k);
            Assert.False(string.IsNullOrWhiteSpace(en), k);
        }
    }

    [Fact]
    public void Localization_EveryScopeVerbAndCriticalKeyExists()
    {
        foreach (var s in ScopeCatalog.Static) Assert.True(CtxMenu.Core.Localization.Loc.Has(s.LocKey), s.LocKey);
        foreach (var k in KnownItems.VerbLocKeys.Values) Assert.True(CtxMenu.Core.Localization.Loc.Has(k), k);
        foreach (var k in KnownItems.CriticalHandlers.Values) Assert.True(CtxMenu.Core.Localization.Loc.Has(k), k);
        foreach (var c in Enum.GetValues<MenuCategory>()) Assert.True(CtxMenu.Core.Localization.Loc.Has("Cat_" + c), c.ToString());
        foreach (var c in Enum.GetValues<DisableState>()) Assert.True(CtxMenu.Core.Localization.Loc.Has("State_" + c), c.ToString());
        foreach (var c in Enum.GetValues<EntryKind>()) Assert.True(CtxMenu.Core.Localization.Loc.Has("Kind_" + c), c.ToString());
        foreach (var c in Enum.GetValues<SubmenuKind>()) Assert.True(CtxMenu.Core.Localization.Loc.Has("Submenu_" + c), c.ToString());
    }
}

using CtxMenu.Core.Model;
using CtxMenu.Core.Registry;
using CtxMenu.Core.Scanning;
using CtxMenu.Core.Services;

namespace CtxMenu.Core.Tests;

public class ShellNewTests
{
    private const RegHive CU = RegHive.CurrentUser, LM = RegHive.LocalMachine;

    private static ShellNewService Svc(TestEnv env) => new(env.Editor, new PassThroughResolver(), Path.Combine(env.Dir, "Templates"));

    [Fact]
    public void Scan_FindsDirectAndProgIdShellNew()
    {
        using var env = new TestEnv();
        env.Reg.Set(LM, TestEnv.Cls(".txt"), "", "txtfile").Set(LM, TestEnv.Cls(@".txt\ShellNew"), "NullFile", "");
        env.Reg.Set(LM, TestEnv.Cls("txtfile"), "", "Text Document");
        env.Reg.Set(LM, TestEnv.Cls(@".docx\Word.Document.12\ShellNew"), "FileName", "winword.docx");
        env.Reg.Set(LM, TestEnv.Cls("Word.Document.12"), "", "Word Document");
        env.Reg.Set(LM, TestEnv.Cls(@".bmp\-ShellNew"), "NullFile", "");
        env.Reg.Set(LM, TestEnv.Cls(@"Folder\ShellNew"), "Directory", "");
        var items = Svc(env).Scan();
        var txt = items.Single(i => i.Extension == ".txt");
        Assert.Equal("Text Document", txt.DisplayName);
        Assert.Equal("NullFile", txt.Mode);
        Assert.True(txt.IsEnabled);
        var docx = items.Single(i => i.Extension == ".docx");
        Assert.Equal("FileName", docx.Mode);
        Assert.Equal("winword.docx", docx.Detail);
        Assert.Equal("Word Document", docx.DisplayName);
        Assert.False(items.Single(i => i.Extension == ".bmp").IsEnabled);
        Assert.True(items.Single(i => i.Extension == "Folder").IsCritical);
    }

    [Fact]
    public void Toggle_RenamesKey_AndInvalidatesCache()
    {
        using var env = new TestEnv();
        env.Reg.Set(CU, TestEnv.Cls(@".md\ShellNew"), "NullFile", "");
        env.Reg.Set(CU, ShellNewService.CacheKey, "Classes", "x");
        var svc = Svc(env);
        var item = svc.Scan().Single();
        svc.SetEnabled(item, false);
        Assert.True(env.Reg.KeyExists(CU, TestEnv.Cls(@".md\-ShellNew")));
        Assert.False(env.Reg.HasValue(CU, ShellNewService.CacheKey, "Classes"));
        Assert.False(svc.Scan().Single().IsEnabled);
        svc.SetEnabled(item, true);
        Assert.True(env.Reg.KeyExists(CU, TestEnv.Cls(@".md\ShellNew")));
    }

    [Fact]
    public void AddTemplate_CreatesProgId_AndRemoveCleansUp()
    {
        using var env = new TestEnv();
        env.Reg.CreateKey(CU, TestEnv.C);
        var svc = Svc(env);
        var tpl = Path.Combine(env.Dir, "t.md");
        File.WriteAllText(tpl, "# hi");
        svc.AddTemplate("md", "Markdown", tpl);
        var item = svc.Scan().Single();
        Assert.Equal(".md", item.Extension);
        Assert.True(item.IsCustom);
        Assert.Equal("FileName", item.Mode);
        Assert.True(File.Exists(item.Detail));
        Assert.Equal("CtxMenuManager.md", env.Reg.GetString(CU, TestEnv.Cls(".md")));
        Assert.Equal("Markdown", item.DisplayName);

        Assert.Throws<InvalidOperationException>(() => svc.AddTemplate(".md", "Again", null));

        svc.RemoveCustom(item);
        Assert.False(env.Reg.KeyExists(CU, TestEnv.Cls(".md")));
        Assert.False(env.Reg.KeyExists(CU, TestEnv.Cls("CtxMenuManager.md")));
    }

    [Fact]
    public void AddTemplate_ReusesExistingProgId_NullFile()
    {
        using var env = new TestEnv();
        env.Reg.Set(LM, TestEnv.Cls(".py"), "", "Python.File").Set(LM, TestEnv.Cls("Python.File"), "", "Python File");
        var svc = Svc(env);
        svc.AddTemplate(".py", "", null);
        Assert.Equal("NullFile", env.Reg.GetValueNames(CU, TestEnv.Cls(@".py\ShellNew")).Single(n => n == "NullFile"));
        Assert.Null(env.Reg.GetString(CU, TestEnv.Cls(".py")));
    }

    [Fact]
    public void AddTemplate_BadExtension_Throws()
    {
        using var env = new TestEnv();
        Assert.Throws<ArgumentException>(() => Svc(env).AddTemplate("a b", "x", null));
    }

    [Fact]
    public void RemoveCustom_ForeignItem_Throws()
    {
        using var env = new TestEnv();
        env.Reg.Set(LM, TestEnv.Cls(@".txt\ShellNew"), "NullFile", "");
        var svc = Svc(env);
        Assert.Throws<InvalidOperationException>(() => svc.RemoveCustom(svc.Scan().Single()));
    }
}

public class ArchiverTests
{
    private const RegHive CU = RegHive.CurrentUser;

    [Fact]
    public void SevenZip_DefaultsAllOn_AndBitsToggle()
    {
        using var env = new TestEnv();
        env.Reg.CreateKey(RegHive.LocalMachine, @"Software\7-Zip");
        var svc = new ArchiverService(env.Editor);
        var z = svc.DetectSevenZip();
        Assert.True(z.Installed);
        Assert.All(z.Items, i => Assert.True(i.IsEnabled));
        var test = z.Items.Single(i => i.Id == "Test");
        svc.SetItem(z, test, false);
        var flags = env.Reg.GetValue(CU, ArchiverService.SevenZipOptions, "ContextMenu")!.AsDWord();
        Assert.Equal(0xFFFFFFFFu & ~(1u << 4), flags);
        var z2 = svc.DetectSevenZip();
        Assert.False(z2.Items.Single(i => i.Id == "Test").IsEnabled);
        Assert.True(z2.Items.Single(i => i.Id == "Open").IsEnabled);
        env.Editor.UndoLast();
        Assert.Null(env.Reg.GetValue(CU, ArchiverService.SevenZipOptions, "ContextMenu"));
    }

    [Fact]
    public void WinRar_ItemsUseDwordValues()
    {
        using var env = new TestEnv();
        env.Reg.CreateKey(CU, @"Software\WinRAR");
        env.Reg.SetValue(CU, ArchiverService.WinRarMenuItems, "Test", RegValue.DWord(1));
        var svc = new ArchiverService(env.Editor);
        var w = svc.DetectWinRar();
        Assert.True(w.Installed);
        Assert.True(w.Items.Single(i => i.Id == "Test").IsEnabled);
        Assert.False(w.Items.Single(i => i.Id == "EmailArc").IsEnabled); // default off
        svc.SetItem(w, w.Items.Single(i => i.Id == "ExtrHere"), false);
        Assert.Equal(0u, env.Reg.GetValue(CU, ArchiverService.WinRarMenuItems, "ExtrHere")!.AsDWord());
    }

    [Fact]
    public void NotInstalled_Detected()
    {
        using var env = new TestEnv();
        Assert.All(new ArchiverService(env.Editor).Detect(), a => Assert.False(a.Installed));
    }
}

public class Win11Tests
{
    [Fact]
    public void ClassicMenu_OnOff()
    {
        using var env = new TestEnv();
        var svc = new Win11Service(env.Editor);
        Assert.False(svc.IsClassicMenuEnabled);
        svc.SetClassicMenu(true);
        Assert.True(svc.IsClassicMenuEnabled);
        Assert.Equal("", env.Reg.GetString(RegHive.CurrentUser, Win11Service.ClassicMenuInproc));
        Assert.Null(svc.SetClassicMenu(true));
        svc.SetClassicMenu(false);
        Assert.False(env.Reg.KeyExists(RegHive.CurrentUser, Win11Service.ClassicMenuKey));
    }

    [Theory]
    [InlineData(10, 0, 19045, false)]
    [InlineData(10, 0, 22000, true)]
    [InlineData(10, 0, 26100, true)]
    public void IsWindows11(int major, int minor, int build, bool expected) =>
        Assert.Equal(expected, Win11Service.IsWindows11(new Version(major, minor, build)));
}

public class CustomEntryTests
{
    private const RegHive CU = RegHive.CurrentUser;

    [Fact]
    public void Create_SimpleVerb_IsScannedAsCustom_AndDeletable()
    {
        using var env = new TestEnv();
        var svc = new CustomEntryService(env.Editor, Path.Combine(env.Dir, "custom.json"));
        var def = new CustomEntryDefinition { Title = "Open in Notepad++", Program = @"C:\npp\notepad++.exe", Scope = "*", Icon = @"C:\npp\notepad++.exe,0", Extended = true, Position = MenuPosition.Top };
        svc.Create(def);
        var e = env.Scanner().Scan().Single(x => x.IsCustom);
        Assert.Equal("Open in Notepad++", e.DisplayName);
        Assert.Equal("\"C:\\npp\\notepad++.exe\" \"%1\"", e.Command);
        Assert.True(e.IsExtended);
        Assert.Equal("Top", e.Position);
        Assert.Equal(def.Id, e.CustomId);
        Assert.Single(svc.LoadDefinitions());
        Assert.True(svc.Exists(def));

        svc.Delete(e);
        Assert.DoesNotContain(env.Scanner().Scan(), x => x.IsCustom);
        Assert.Empty(svc.LoadDefinitions());
    }

    [Fact]
    public void Create_Group_WithChildren()
    {
        using var env = new TestEnv();
        var svc = new CustomEntryService(env.Editor, null);
        var def = new CustomEntryDefinition
        {
            Title = "Dev", Scope = @"Directory\Background", IsGroup = true,
            Children =
            {
                new CustomEntryDefinition { Title = "Terminal", Program = "wt.exe", Arguments = "-d \"%V\"" },
                new CustomEntryDefinition { Title = "Admin cmd", Program = "cmd.exe", RunAsAdmin = true },
            }
        };
        svc.Create(def);
        var g = env.Scanner().Scan().Single(x => x.IsCustom);
        Assert.Equal(SubmenuKind.ShellSubkey, g.Submenu);
        Assert.Equal(2, g.Children.Count);
        var admin = g.Children.Single(c => c.DisplayName == "Admin cmd");
        Assert.True(admin.HasShield);
        Assert.Contains("-Verb RunAs", admin.Command);
        Assert.Contains("'%V'", admin.Command!.Replace("\\\"", ""));
        Assert.Equal("\"wt.exe\" -d \"%V\"", g.Children.Single(c => c.DisplayName == "Terminal").Command);
    }

    [Fact]
    public void AddToGroup_AddsChildVerb()
    {
        using var env = new TestEnv();
        env.Reg.Set(CU, TestEnv.Cls(@"Directory\shell\Tools"), "SubCommands", "").Set(CU, TestEnv.Cls(@"Directory\shell\Tools\shell\a"), "MUIVerb", "A");
        var svc = new CustomEntryService(env.Editor, null);
        var group = env.Scanner().Scan().Single(e => e.KeyName == "Tools");
        svc.AddToGroup(group, new CustomEntryDefinition { Title = "B", Program = "b.exe" });
        Assert.Equal(2, env.Scanner().Scan().Single(e => e.KeyName == "Tools").Children.Count);
    }

    [Fact]
    public void AddToGroup_CommandStoreGroup_Throws()
    {
        using var env = new TestEnv();
        var svc = new CustomEntryService(env.Editor, null);
        var g = new ContextMenuEntry { Submenu = SubmenuKind.SubCommands };
        Assert.Throws<InvalidOperationException>(() => svc.AddToGroup(g, new CustomEntryDefinition { Title = "x", Program = "x" }));
    }

    [Theory]
    [InlineData("", "x.exe", false)]
    [InlineData("Title", "", false)]
    [InlineData("Title", "x.exe", true)]
    public void Validate(string title, string program, bool ok)
    {
        var def = new CustomEntryDefinition { Title = title, Program = program };
        if (ok) CustomEntryService.Validate(def);
        else Assert.Throws<ArgumentException>(() => CustomEntryService.Validate(def));
    }

    [Fact]
    public void Validate_EmptyGroup_Throws() =>
        Assert.Throws<ArgumentException>(() => CustomEntryService.Validate(new CustomEntryDefinition { Title = "G", IsGroup = true }));

    [Fact]
    public void DefaultArguments_DependOnScope()
    {
        Assert.Equal("\"%V\"", CustomEntryService.DefaultArguments(@"Directory\Background"));
        Assert.Equal("\"%1\"", CustomEntryService.DefaultArguments("*"));
    }

    [Fact]
    public void KeyName_IsSafeAndUnique()
    {
        var a = new CustomEntryDefinition { Title = "Открыть в \"VS Code\"!" };
        var name = CustomEntryService.MakeKeyName(a);
        Assert.StartsWith("CMM.", name);
        Assert.EndsWith("." + a.Id, name);
        Assert.DoesNotContain(' ', name);
        Assert.DoesNotContain('\\', name);
    }

    [Fact]
    public void AllUsers_WritesToHklm_AndFailsWithoutAdmin()
    {
        using var env = new TestEnv();
        env.Reg.ReadOnlyHives.Add(RegHive.LocalMachine);
        var svc = new CustomEntryService(env.Editor, null);
        var def = new CustomEntryDefinition { Title = "X", Program = "x.exe", AllUsers = true };
        Assert.Equal(RegHive.LocalMachine, CustomEntryService.HiveFor(def));
        Assert.Throws<UnauthorizedAccessException>(() => svc.Create(def));
        Assert.Empty(env.Journal.Sets);
    }

    [Fact]
    public void Delete_NonCustom_Throws()
    {
        using var env = new TestEnv();
        Assert.Throws<InvalidOperationException>(() => new CustomEntryService(env.Editor, null).Delete(new ContextMenuEntry()));
    }
}

public class ProfileTests
{
    private const RegHive CU = RegHive.CurrentUser;

    [Fact]
    public void ExportImport_RoundTrip()
    {
        // source machine: disables a verb, sets shift on another, creates a custom item
        using var src = new TestEnv();
        Seed(src);
        var srcActions = new ContextMenuActions(src.Editor);
        var srcCustom = new CustomEntryService(src.Editor, Path.Combine(src.Dir, "c.json"));
        var srcProfiles = new ProfileService(src.Editor, srcActions, srcCustom);
        var entries = src.Scanner().Scan();
        srcActions.SetEnabled(entries.Single(e => e.KeyName == "A"), false);
        srcActions.SetEnabled(entries.Single(e => e.KeyName == "H"), false);
        srcActions.SetExtended(entries.Single(e => e.KeyName == "B"), true);
        srcCustom.Create(new CustomEntryDefinition { Title = "Mine", Program = "m.exe", Scope = "*" });
        var json = srcProfiles.Export(src.Scanner().Scan());

        // target machine
        using var dst = new TestEnv();
        Seed(dst);
        var dstActions = new ContextMenuActions(dst.Editor);
        var dstCustom = new CustomEntryService(dst.Editor, Path.Combine(dst.Dir, "c.json"));
        var profile = ProfileService.Parse(json);
        Assert.Equal(2, profile.Disabled.Count);
        var result = new ProfileService(dst.Editor, dstActions, dstCustom).Apply(profile, dst.Scanner().Scan(), HandlerDisableMethod.RenameKey, exact: false);
        Assert.Equal(2, result.Disabled);
        Assert.Equal(1, result.ShiftChanged);
        Assert.Equal(1, result.CustomCreated);
        Assert.Equal(0, result.NotFound);

        var after = dst.Scanner().Scan();
        Assert.False(after.Single(e => e.KeyName == "A").IsEnabled);
        Assert.False(after.Single(e => e.KeyName.TrimStart('-') == "H").IsEnabled);
        Assert.True(after.Single(e => e.KeyName == "B").IsExtended);
        Assert.Single(after, e => e.IsCustom);

        // applying again changes nothing
        var again = new ProfileService(dst.Editor, dstActions, dstCustom).Apply(profile, after, HandlerDisableMethod.RenameKey, false);
        Assert.Equal(new ProfileImportResult(0, 0, 0, 0, 0), again);
    }

    [Fact]
    public void Exact_ReenablesUnlisted()
    {
        using var env = new TestEnv();
        Seed(env);
        var actions = new ContextMenuActions(env.Editor);
        actions.SetEnabled(env.Scanner().Scan().Single(e => e.KeyName == "A"), false);
        var svc = new ProfileService(env.Editor, actions, new CustomEntryService(env.Editor, null));
        var r = svc.Apply(new MenuProfile(), env.Scanner().Scan(), HandlerDisableMethod.RenameKey, exact: true);
        Assert.Equal(1, r.Enabled);
        Assert.All(env.Scanner().Scan(), e => Assert.True(e.IsEnabled));
    }

    [Fact]
    public void Parse_Garbage_Throws()
    {
        Assert.ThrowsAny<Exception>(() => ProfileService.Parse("not json"));
        Assert.ThrowsAny<Exception>(() => ProfileService.Parse("null"));
    }

    [Fact]
    public void NotFound_IsCounted()
    {
        using var env = new TestEnv();
        Seed(env);
        var actions = new ContextMenuActions(env.Editor);
        var svc = new ProfileService(env.Editor, actions, new CustomEntryService(env.Editor, null));
        var r = svc.Apply(new MenuProfile { Disabled = { "verb|currentuser|software\\classes\\*\\shell\\gone" } }, env.Scanner().Scan(), HandlerDisableMethod.RenameKey, false);
        Assert.Equal(1, r.NotFound);
    }

    private static void Seed(TestEnv env)
    {
        env.Reg.Set(CU, TestEnv.Cls(@"*\shell\A\command"), "", "a.exe");
        env.Reg.Set(CU, TestEnv.Cls(@"*\shell\B\command"), "", "b.exe");
        env.Reg.Set(CU, TestEnv.Cls(@"Directory\shellex\ContextMenuHandlers\H"), "", "{11111111-2222-3333-4444-555555555555}");
    }
}

public class PackagedTests
{
    private const string Manifest = """
    <?xml version="1.0" encoding="utf-8"?>
    <Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
             xmlns:desktop4="http://schemas.microsoft.com/appx/manifest/desktop/windows10/4"
             xmlns:desktop5="http://schemas.microsoft.com/appx/manifest/desktop/windows10/5"
             xmlns:com="http://schemas.microsoft.com/appx/manifest/com/windows10">
      <Identity Name="Contoso.Tool" Publisher="CN=Contoso" Version="1.0.0.0" />
      <Properties><DisplayName>Contoso Tool</DisplayName><PublisherDisplayName>Contoso Ltd</PublisherDisplayName></Properties>
      <Applications><Application Id="App"><Extensions>
        <desktop4:Extension Category="windows.fileExplorerContextMenus">
          <desktop4:FileExplorerContextMenus>
            <desktop5:ItemType Type="*"><desktop5:Verb Id="Open" Clsid="aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee" /></desktop5:ItemType>
            <desktop5:ItemType Type="Directory"><desktop5:Verb Id="Open" Clsid="aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee" /></desktop5:ItemType>
            <desktop5:ItemType Type="Directory\Background"><desktop5:Verb Id="Here" Clsid="{11111111-bbbb-cccc-dddd-eeeeeeeeeeee}" /></desktop5:ItemType>
          </desktop4:FileExplorerContextMenus>
        </desktop4:Extension>
        <com:Extension Category="windows.comServer"><com:ComServer><com:SurrogateServer>
          <com:Class Id="aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee" DisplayName="Open with Contoso" />
        </com:SurrogateServer></com:ComServer></com:Extension>
      </Extensions></Application></Applications>
    </Package>
    """;

    [Fact]
    public void Parse_ReadsVerbsAndComNames()
    {
        var m = AppxManifestParser.Parse(Manifest);
        Assert.Equal("Contoso.Tool", m.IdentityName);
        Assert.Equal("Contoso Ltd", m.PublisherDisplayName);
        Assert.Equal(3, m.Verbs.Count);
        Assert.Equal("{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}", m.Verbs[0].Clsid);
        Assert.Equal("Open with Contoso", m.Verbs[0].ComDisplayName);
    }

    [Fact]
    public void ToEntries_DedupesByClsid_AndReadsBlocked()
    {
        var m = AppxManifestParser.Parse(Manifest);
        var reg = new InMemoryRegistry();
        reg.Set(RegHive.CurrentUser, ContextMenuScanner.BlockedPath, "{11111111-BBBB-CCCC-DDDD-EEEEEEEEEEEE}", "");
        var entries = AppxManifestParser.ToEntries(new[] { (new InstalledPackage("Contoso.Tool_1.0", @"C:\Apps\Contoso", "Contoso Tool"), m) }, reg);
        Assert.Equal(2, entries.Count);
        var open = entries.Single(e => e.KeyName == "Open");
        Assert.Equal("*, Directory", open.Scope);
        Assert.Equal("Open with Contoso", open.DisplayName);
        Assert.True(open.IsThirdParty);
        Assert.True(open.IsEnabled);
        Assert.Equal(DisableState.BlockedCurrentUser, entries.Single(e => e.KeyName == "Here").State);
        Assert.Equal(MenuCategory.Background, entries.Single(e => e.KeyName == "Here").Category);
    }
}

public class SettingsTests
{
    [Fact]
    public void Settings_RoundTrip_AndCorruptFallsBack()
    {
        using var env = new TestEnv();
        var file = Path.Combine(env.Dir, "s.json");
        new AppSettings { Language = "en", Theme = AppTheme.Dark, HandlerMethod = HandlerDisableMethod.BlockClsid, ConfirmCritical = false }.Save(file);
        var s = AppSettings.Load(file);
        Assert.Equal("en", s.Language);
        Assert.Equal(AppTheme.Dark, s.Theme);
        Assert.Equal(HandlerDisableMethod.BlockClsid, s.HandlerMethod);
        Assert.False(s.ConfirmCritical);
        File.WriteAllText(file, "{{{");
        Assert.Equal("ru", AppSettings.Load(file).Language);
    }

    [Fact]
    public void Loc_SwitchesLanguage()
    {
        var loc = CtxMenu.Core.Localization.Loc.Instance;
        var prev = loc.Language;
        try
        {
            loc.Language = "en";
            Assert.Equal("Context Menu Manager", CtxMenu.Core.Localization.Loc.Get("App_Title"));
            loc.Language = "ru";
            Assert.Equal("Менеджер контекстного меню", CtxMenu.Core.Localization.Loc.Get("App_Title"));
            loc.Language = "xx";
            Assert.Equal("ru", loc.Language);
            Assert.Equal("Missing_Key", CtxMenu.Core.Localization.Loc.Get("Missing_Key"));
        }
        finally { loc.Language = prev; }
    }
}

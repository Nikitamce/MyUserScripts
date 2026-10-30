using CtxMenu.Core.Model;
using CtxMenu.Core.Registry;
using CtxMenu.Core.Scanning;
using CtxMenu.Core.Services;

namespace CtxMenu.Core.Tests;

public class ActionsTests
{
    private const RegHive CU = RegHive.CurrentUser, LM = RegHive.LocalMachine;
    private const string Clsid = "{11111111-2222-3333-4444-555555555555}";

    private static TestEnv Env()
    {
        var env = new TestEnv();
        env.Reg.Set(CU, TestEnv.Cls(@"*\shell\Verb"), "MUIVerb", "My verb").Set(CU, TestEnv.Cls(@"*\shell\Verb\command"), "", "x.exe \"%1\"");
        env.Reg.Set(CU, TestEnv.Cls(@"*\shellex\ContextMenuHandlers\Ext"), "", Clsid);
        env.Reg.Set(LM, TestEnv.Cls(@"Directory\shell\Sys\command"), "", "y.exe");
        return env;
    }

    private static ContextMenuEntry Find(TestEnv env, string key) => env.Scanner().Scan().Single(e => e.KeyName.TrimStart('-') == key);

    [Fact]
    public void Verb_DisableEnable_ViaLegacyDisable()
    {
        using var env = Env();
        var a = new ContextMenuActions(env.Editor);
        var v = Find(env, "Verb");
        Assert.NotNull(a.SetEnabled(v, false));
        Assert.True(env.Reg.HasValue(CU, v.KeyPath, "LegacyDisable"));
        Assert.Equal(DisableState.LegacyDisable, Find(env, "Verb").State);
        Assert.Null(a.SetEnabled(v, false)); // no-op
        a.SetEnabled(v, true);
        Assert.False(env.Reg.HasValue(CU, v.KeyPath, "LegacyDisable"));
        Assert.True(Find(env, "Verb").IsEnabled);
    }

    [Fact]
    public void Handler_RenameMethod_AddsDashPrefix_AndUndoRestores()
    {
        using var env = Env();
        var a = new ContextMenuActions(env.Editor);
        var h = Find(env, "Ext");
        var set = a.SetEnabled(h, false, HandlerDisableMethod.RenameKey)!;
        Assert.True(env.Reg.KeyExists(CU, TestEnv.Cls(@"*\shellex\ContextMenuHandlers\-Ext")));
        Assert.False(env.Reg.KeyExists(CU, TestEnv.Cls(@"*\shellex\ContextMenuHandlers\Ext")));
        Assert.Equal(DisableState.RenamedKey, Find(env, "Ext").State);
        env.Editor.Undo(set);
        Assert.True(env.Reg.KeyExists(CU, TestEnv.Cls(@"*\shellex\ContextMenuHandlers\Ext")));
        Assert.True(Find(env, "Ext").IsEnabled);
    }

    [Fact]
    public void Handler_RenamedKey_CanBeReEnabled()
    {
        using var env = Env();
        var a = new ContextMenuActions(env.Editor);
        a.SetEnabled(Find(env, "Ext"), false);
        var h = Find(env, "Ext");
        a.SetEnabled(h, true);
        Assert.True(env.Reg.KeyExists(CU, TestEnv.Cls(@"*\shellex\ContextMenuHandlers\Ext")));
        Assert.Equal("Ext", h.KeyName);
    }

    [Fact]
    public void Handler_BlockMethod_WritesBlockedValue()
    {
        using var env = Env();
        var a = new ContextMenuActions(env.Editor);
        a.SetEnabled(Find(env, "Ext"), false, HandlerDisableMethod.BlockClsid);
        Assert.True(env.Reg.HasValue(CU, ContextMenuScanner.BlockedPath, Clsid));
        var h = Find(env, "Ext");
        Assert.Equal(DisableState.BlockedCurrentUser, h.State);
        a.SetEnabled(h, true);
        Assert.False(env.Reg.HasValue(CU, ContextMenuScanner.BlockedPath, Clsid));
    }

    [Fact]
    public void Handler_RenameConflict_Throws_AndStateIsRestored()
    {
        using var env = Env();
        env.Reg.CreateKey(CU, TestEnv.Cls(@"*\shellex\ContextMenuHandlers\-Ext"));
        var a = new ContextMenuActions(env.Editor);
        var h = env.Scanner().Scan().Single(e => e.KeyName == "Ext");
        Assert.Throws<InvalidOperationException>(() => a.SetEnabled(h, false));
        Assert.True(h.IsEnabled);
        Assert.Equal("Ext", h.KeyName);
    }

    [Fact]
    public void HklmWithoutAdmin_Throws_AndModelUnchanged()
    {
        using var env = Env();
        env.Reg.ReadOnlyHives.Add(LM);
        var a = new ContextMenuActions(env.Editor);
        var sys = Find(env, "Sys");
        Assert.Equal(LM, ContextMenuActions.TargetHive(sys, false, HandlerDisableMethod.RenameKey));
        Assert.Throws<UnauthorizedAccessException>(() => a.SetEnabled(sys, false));
        Assert.True(sys.IsEnabled);
    }

    [Fact]
    public void TargetHive_BlockMethodUsesCurrentUser()
    {
        var e = new ContextMenuEntry { Kind = EntryKind.ShellExtension, Hive = LM, Clsid = Clsid };
        Assert.Equal(CU, ContextMenuActions.TargetHive(e, false, HandlerDisableMethod.BlockClsid));
        Assert.Equal(LM, ContextMenuActions.TargetHive(e, false, HandlerDisableMethod.RenameKey));
        e.State = DisableState.BlockedLocalMachine;
        Assert.Equal(LM, ContextMenuActions.TargetHive(e, true, HandlerDisableMethod.BlockClsid));
    }

    [Fact]
    public void Bulk_IsOneUndoableChangeSet()
    {
        using var env = Env();
        var a = new ContextMenuActions(env.Editor);
        var items = env.Scanner().Scan().Where(e => e.Hive == CU).ToList();
        var set = a.SetEnabledMany(items, false, HandlerDisableMethod.RenameKey, "bulk")!;
        Assert.Single(env.Journal.Sets);
        Assert.All(env.Scanner().Scan().Where(e => e.Hive == CU), e => Assert.False(e.IsEnabled));
        env.Editor.Undo(set);
        Assert.All(env.Scanner().Scan(), e => Assert.True(e.IsEnabled));
    }

    [Fact]
    public void Extended_ProgrammaticOnly_Position()
    {
        using var env = Env();
        var a = new ContextMenuActions(env.Editor);
        var v = Find(env, "Verb");
        a.SetExtended(v, true);
        Assert.True(Find(env, "Verb").IsExtended);
        a.SetProgrammaticOnly(v, true);
        Assert.Equal(DisableState.ProgrammaticOnly, Find(env, "Verb").State);
        a.SetProgrammaticOnly(v, false);
        Assert.True(Find(env, "Verb").IsEnabled);
        a.SetPosition(v, "Top");
        Assert.Equal("Top", Find(env, "Verb").Position);
        a.SetPosition(v, null);
        Assert.Null(Find(env, "Verb").Position);
        Assert.Equal(5, env.Journal.Sets.Count);
        env.Editor.UndoAll();
        var r = Find(env, "Verb");
        Assert.False(r.IsExtended);
        Assert.True(r.IsEnabled);
    }

    [Fact]
    public void Packaged_BlockAndUnblock()
    {
        using var env = Env();
        var a = new ContextMenuActions(env.Editor);
        var p = new ContextMenuEntry { Kind = EntryKind.Packaged, Clsid = Clsid, DisplayName = "Pkg" };
        a.SetEnabled(p, false);
        Assert.Equal("Pkg", env.Reg.GetString(CU, ContextMenuScanner.BlockedPath, Clsid));
        a.SetEnabled(p, true);
        Assert.False(env.Reg.HasValue(CU, ContextMenuScanner.BlockedPath, Clsid));
    }

    [Fact]
    public void ExportReg_ContainsKey()
    {
        using var env = Env();
        var text = new ContextMenuActions(env.Editor).ExportReg(Find(env, "Verb"));
        Assert.Contains(@"[HKEY_CURRENT_USER\Software\Classes\*\shell\Verb\command]", text);
    }
}

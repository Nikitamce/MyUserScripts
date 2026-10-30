using CtxMenu.Core.Editing;
using CtxMenu.Core.Registry;

namespace CtxMenu.Core.Tests;

public class RegistryEditorTests
{
    private const RegHive CU = RegHive.CurrentUser;

    [Fact]
    public void SetValue_OnNewKey_UndoRemovesCreatedKeys()
    {
        using var env = new TestEnv();
        env.Reg.CreateKey(CU, @"Software\Classes");
        var set = env.Editor.Execute("t", tx => tx.SetString(CU, @"Software\Classes\a\b\c", "x", "1"));
        Assert.NotNull(set);
        Assert.True(env.Reg.KeyExists(CU, @"Software\Classes\a\b\c"));
        env.Editor.Undo(set!);
        Assert.False(env.Reg.KeyExists(CU, @"Software\Classes\a"));
        Assert.True(env.Reg.KeyExists(CU, @"Software\Classes"));
        Assert.True(set!.Undone);
    }

    [Fact]
    public void SetValue_OverExisting_UndoRestoresOldValue()
    {
        using var env = new TestEnv();
        env.Reg.Set(CU, @"K", "v", "old");
        var set = env.Editor.Execute("t", tx => tx.SetString(CU, "K", "v", "new"));
        Assert.Equal("new", env.Reg.GetString(CU, "K", "v"));
        env.Editor.Undo(set!);
        Assert.Equal("old", env.Reg.GetString(CU, "K", "v"));
    }

    [Fact]
    public void DeleteTree_UndoRestoresWholeSubtree()
    {
        using var env = new TestEnv();
        env.Reg.Set(CU, @"A\B", "", "def").Set(CU, @"A\B\C", "n", "1");
        env.Reg.SetValue(CU, @"A\B\C", "d", RegValue.DWord(7));
        var set = env.Editor.Execute("t", tx => tx.DeleteTree(CU, @"A\B"));
        Assert.False(env.Reg.KeyExists(CU, @"A\B"));
        env.Editor.Undo(set!);
        Assert.Equal("def", env.Reg.GetString(CU, @"A\B"));
        Assert.Equal(7u, env.Reg.GetValue(CU, @"A\B\C", "d")!.AsDWord());
    }

    [Fact]
    public void RenameKey_MovesSubtree_AndUndoMovesBack()
    {
        using var env = new TestEnv();
        env.Reg.Set(CU, @"P\name\sub", "x", "1");
        var set = env.Editor.Execute("t", tx => tx.RenameKey(CU, @"P\name", @"P\-name"));
        Assert.False(env.Reg.KeyExists(CU, @"P\name"));
        Assert.Equal("1", env.Reg.GetString(CU, @"P\-name\sub", "x"));
        env.Editor.Undo(set!);
        Assert.Equal("1", env.Reg.GetString(CU, @"P\name\sub", "x"));
        Assert.False(env.Reg.KeyExists(CU, @"P\-name"));
    }

    [Fact]
    public void FailedTransaction_IsRolledBack_AndNotJournaled()
    {
        using var env = new TestEnv();
        env.Reg.Set(CU, "K", "v", "old");
        Assert.Throws<InvalidOperationException>(() => env.Editor.Execute("t", tx =>
        {
            tx.SetString(CU, "K", "v", "new");
            tx.SetString(CU, "K2", "v", "x");
            throw new InvalidOperationException("boom");
        }));
        Assert.Equal("old", env.Reg.GetString(CU, "K", "v"));
        Assert.False(env.Reg.KeyExists(CU, "K2"));
        Assert.Empty(env.Journal.Sets);
    }

    [Fact]
    public void NoOps_ReturnsNull()
    {
        using var env = new TestEnv();
        Assert.Null(env.Editor.Execute("t", tx => tx.DeleteValue(CU, "Missing", "x")));
        Assert.Empty(env.Journal.Sets);
    }

    [Fact]
    public void Execute_WritesRegBackup_InUtf16()
    {
        using var env = new TestEnv();
        env.Reg.Set(CU, @"K", "v", "old");
        var set = env.Editor.Execute("t", tx => tx.SetString(CU, "K", "v", "new"))!;
        Assert.NotNull(set.BackupFile);
        var bytes = File.ReadAllBytes(set.BackupFile!);
        Assert.Equal(0xFF, bytes[0]);
        Assert.Equal(0xFE, bytes[1]);
        var text = File.ReadAllText(set.BackupFile!);
        Assert.StartsWith(RegFileExporter.Header, text);
        Assert.Contains(@"[HKEY_CURRENT_USER\K]", text);
        Assert.Contains("\"v\"=\"old\"", text);
    }

    [Fact]
    public void Journal_PersistsAndReloads()
    {
        using var env = new TestEnv();
        env.Editor.Execute("first", tx => tx.SetString(CU, "K", "a", "1"));
        env.Editor.Execute("second", tx => tx.SetString(CU, "K", "b", "2"));
        env.Editor.UndoLast();
        var reloaded = new ChangeJournal(Path.Combine(env.Dir, "journal.json"));
        Assert.Equal(2, reloaded.Sets.Count);
        Assert.Equal("first", reloaded.LastActive!.Description);
        Assert.True(reloaded.Sets[1].Undone);
    }

    [Fact]
    public void BrokenJournal_IsKeptAside_AndStartsFresh()
    {
        using var env = new TestEnv();
        var file = Path.Combine(env.Dir, "broken.json");
        File.WriteAllText(file, "{ not json");
        var j = new ChangeJournal(file);
        Assert.Empty(j.Sets);
        Assert.Single(Directory.GetFiles(env.Dir, "broken.json.broken-*"));
    }

    [Fact]
    public void UndoAll_RevertsEverythingNewestFirst()
    {
        using var env = new TestEnv();
        env.Reg.Set(CU, "K", "v", "0");
        env.Editor.Execute("1", tx => tx.SetString(CU, "K", "v", "1"));
        env.Editor.Execute("2", tx => tx.SetString(CU, "K", "v", "2"));
        env.Editor.Execute("3", tx => tx.RenameKey(CU, "K", "-K"));
        Assert.Equal(3, env.Editor.UndoAll());
        Assert.Equal("0", env.Reg.GetString(CU, "K", "v"));
        Assert.Null(env.Journal.LastActive);
    }

    [Fact]
    public void ReadOnlyHive_ThrowsUnauthorized()
    {
        using var env = new TestEnv();
        env.Reg.ReadOnlyHives.Add(RegHive.LocalMachine);
        Assert.Throws<UnauthorizedAccessException>(() =>
            env.Editor.Execute("t", tx => tx.SetString(RegHive.LocalMachine, "K", "v", "1")));
        Assert.Empty(env.Journal.Sets);
    }
}

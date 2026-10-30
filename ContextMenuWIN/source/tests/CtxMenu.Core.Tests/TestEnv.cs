using CtxMenu.Core.Editing;
using CtxMenu.Core.Model;
using CtxMenu.Core.Registry;
using CtxMenu.Core.Scanning;

namespace CtxMenu.Core.Tests;

internal sealed class FakeFiles : IFileInfoProvider
{
    public Dictionary<string, FileMeta> Meta { get; } = new(StringComparer.OrdinalIgnoreCase);
    public FileMeta? GetMeta(string path) => Meta.GetValueOrDefault(path);
    public string WindowsDirectory => @"C:\Windows";
}

internal sealed class TestEnv : IDisposable
{
    public const string C = ContextMenuScanner.Classes;
    public InMemoryRegistry Reg { get; } = new();
    public FakeFiles Files { get; } = new();
    public string Dir { get; } = Path.Combine(Path.GetTempPath(), "cmm-tests-" + Guid.NewGuid().ToString("N"));
    public ChangeJournal Journal { get; }
    public RegistryEditor Editor { get; }

    public TestEnv()
    {
        Directory.CreateDirectory(Dir);
        Journal = new ChangeJournal(Path.Combine(Dir, "journal.json"));
        Editor = new RegistryEditor(Reg, Journal, Path.Combine(Dir, "Backups"));
    }

    public ContextMenuScanner Scanner() => new(Reg, Files, new PassThroughResolver());

    public static string Cls(string path) => RegPath.Combine(C, path);

    public void Dispose()
    {
        try { Directory.Delete(Dir, true); } catch { }
    }
}

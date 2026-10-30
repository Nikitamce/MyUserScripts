using CtxMenu.Core.Registry;

namespace CtxMenu.Core.Tests;

public class RegFileExporterTests
{
    [Fact]
    public void Export_FormatsAllValueKinds()
    {
        var reg = new InMemoryRegistry();
        const string p = @"Software\Classes\*\shell\x";
        reg.SetValue(RegHive.CurrentUser, p, "", RegValue.Str("Title \"quoted\" C:\\path"));
        reg.SetValue(RegHive.CurrentUser, p, "d", RegValue.DWord(0x1F));
        reg.SetValue(RegHive.CurrentUser, p, "e", RegValue.Expand("%SystemRoot%"));
        reg.SetValue(RegHive.CurrentUser, p + @"\command", "", RegValue.Str("cmd"));
        var text = RegFileExporter.Export(reg, RegHive.CurrentUser, p);
        Assert.StartsWith("Windows Registry Editor Version 5.00\r\n\r\n", text);
        Assert.Contains(@"[HKEY_CURRENT_USER\Software\Classes\*\shell\x]", text);
        Assert.Contains("@=\"Title \\\"quoted\\\" C:\\\\path\"", text);
        Assert.Contains("\"d\"=dword:0000001f", text);
        Assert.Contains("\"e\"=hex(2):", text);
        Assert.Contains(@"[HKEY_CURRENT_USER\Software\Classes\*\shell\x\command]", text);
    }

    [Fact]
    public void Export_MissingKey_OnlyHeader()
    {
        var text = RegFileExporter.Export(new InMemoryRegistry(), RegHive.CurrentUser, "Nope");
        Assert.Equal(RegFileExporter.Header + "\r\n\r\n", text);
    }
}

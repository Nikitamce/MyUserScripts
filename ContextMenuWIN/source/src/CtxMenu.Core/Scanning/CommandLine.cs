namespace CtxMenu.Core.Scanning;

public static class CommandLine
{
    /// <summary>Extracts the executable (or the DLL for rundll32) from a shell command line.</summary>
    public static string? ExtractExecutable(string? command)
    {
        if (string.IsNullOrWhiteSpace(command)) return null;
        var cmd = command.Trim();
        string exe;
        string rest;
        if (cmd.StartsWith('"'))
        {
            var end = cmd.IndexOf('"', 1);
            if (end < 0) return cmd.Trim('"');
            exe = cmd[1..end];
            rest = cmd[(end + 1)..].Trim();
        }
        else
        {
            var idx = cmd.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            if (idx > 0 && (idx + 4 == cmd.Length || cmd[idx + 4] == ' '))
            {
                exe = cmd[..(idx + 4)];
                rest = cmd[(idx + 4)..].Trim();
            }
            else
            {
                var sp = cmd.IndexOf(' ');
                exe = sp < 0 ? cmd : cmd[..sp];
                rest = sp < 0 ? "" : cmd[(sp + 1)..].Trim();
            }
        }
        var file = Path.GetFileName(exe.Replace('\\', '/'));
        if (file.Equals("rundll32.exe", StringComparison.OrdinalIgnoreCase) || file.Equals("rundll32", StringComparison.OrdinalIgnoreCase))
        {
            var dll = rest.Trim('"');
            var comma = dll.IndexOf(',');
            if (comma > 0) dll = dll[..comma].Trim('"', ' ');
            if (dll.Length > 0) return dll;
        }
        return exe;
    }

    /// <summary>Parses "path,index" / "path" / "\"path\",-123" icon locations.</summary>
    public static (string path, int index) ParseIconLocation(string location)
    {
        var s = location.Trim();
        var index = 0;
        var comma = s.LastIndexOf(',');
        if (comma > 0 && int.TryParse(s[(comma + 1)..].Trim(), out var i))
        {
            index = i;
            s = s[..comma];
        }
        return (s.Trim().Trim('"'), index);
    }

    /// <summary>Removes menu accelerators: "&amp;Open" -> "Open", "A &amp;&amp; B" -> "A &amp; B".</summary>
    public static string StripAccelerators(string s) => s.Replace("&&", "\u0001").Replace("&", "").Replace("\u0001", "&");

    public static bool IsGuid(string s) => s.Length == 38 && s[0] == '{' && Guid.TryParse(s, out _);
}

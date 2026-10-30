using System.Security;
using CtxMenu.Core.Registry;
using Microsoft.Win32;

namespace CtxMenu.App.Platform;

/// <summary>Real registry, always the 64-bit view (the one 64-bit Explorer reads).</summary>
public sealed class WindowsRegistry : IRegistry
{
    private static RegistryKey Root(RegHive hive) =>
        RegistryKey.OpenBaseKey(hive == RegHive.CurrentUser ? RegistryHive.CurrentUser : RegistryHive.LocalMachine, RegistryView.Registry64);

    private static RegistryKey? Open(RegHive hive, string path, bool writable = false)
    {
        try
        {
            using var root = Root(hive);
            return path.Length == 0 ? Root(hive) : root.OpenSubKey(path, writable);
        }
        catch (SecurityException) when (!writable) { return null; }
        catch (UnauthorizedAccessException) when (!writable) { return null; }
        catch (IOException) when (!writable) { return null; }
        catch (SecurityException e) { throw new UnauthorizedAccessException(e.Message, e); }
    }

    public bool KeyExists(RegHive hive, string path)
    {
        using var k = Open(hive, path);
        return k != null;
    }

    public IReadOnlyList<string> GetSubKeyNames(RegHive hive, string path)
    {
        try
        {
            using var k = Open(hive, path);
            return k?.GetSubKeyNames() ?? Array.Empty<string>();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or SecurityException) { return Array.Empty<string>(); }
    }

    public IReadOnlyList<string> GetValueNames(RegHive hive, string path)
    {
        try
        {
            using var k = Open(hive, path);
            return k?.GetValueNames() ?? Array.Empty<string>();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or SecurityException) { return Array.Empty<string>(); }
    }

    public RegValue? GetValue(RegHive hive, string path, string name)
    {
        try
        {
            using var k = Open(hive, path);
            if (k == null) return null;
            var data = k.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            if (data == null) return null;
            var kind = k.GetValueKind(name);
            return kind switch
            {
                RegistryValueKind.String => new RegValue(RegKind.String, (string)data),
                RegistryValueKind.ExpandString => new RegValue(RegKind.ExpandString, (string)data),
                RegistryValueKind.DWord => new RegValue(RegKind.DWord, unchecked((uint)(int)data)),
                RegistryValueKind.QWord => new RegValue(RegKind.QWord, unchecked((ulong)(long)data)),
                RegistryValueKind.MultiString => new RegValue(RegKind.MultiString, (string[])data),
                RegistryValueKind.None => new RegValue(RegKind.None, data as byte[] ?? Array.Empty<byte>()),
                _ => new RegValue(RegKind.Binary, data as byte[] ?? Array.Empty<byte>()),
            };
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or SecurityException or InvalidCastException) { return null; }
    }

    private static T Write<T>(Func<T> f)
    {
        try { return f(); }
        catch (SecurityException e) { throw new UnauthorizedAccessException(e.Message, e); }
    }

    public void CreateKey(RegHive hive, string path) => Write(() =>
    {
        using var root = Root(hive);
        using var k = root.CreateSubKey(path, true);
        return 0;
    });

    public void SetValue(RegHive hive, string path, string name, RegValue value) => Write(() =>
    {
        using var root = Root(hive);
        using var k = root.CreateSubKey(path, true) ?? throw new UnauthorizedAccessException(path);
        switch (value.Kind)
        {
            case RegKind.String: k.SetValue(name, (string)value.Data, RegistryValueKind.String); break;
            case RegKind.ExpandString: k.SetValue(name, (string)value.Data, RegistryValueKind.ExpandString); break;
            case RegKind.DWord: k.SetValue(name, unchecked((int)(uint)value.Data), RegistryValueKind.DWord); break;
            case RegKind.QWord: k.SetValue(name, unchecked((long)(ulong)value.Data), RegistryValueKind.QWord); break;
            case RegKind.MultiString: k.SetValue(name, (string[])value.Data, RegistryValueKind.MultiString); break;
            case RegKind.None: k.SetValue(name, (byte[])value.Data, RegistryValueKind.None); break;
            default: k.SetValue(name, (byte[])value.Data, RegistryValueKind.Binary); break;
        }
        return 0;
    });

    public void DeleteValue(RegHive hive, string path, string name) => Write(() =>
    {
        using var k = Open(hive, path, writable: true);
        k?.DeleteValue(name, throwOnMissingValue: false);
        return 0;
    });

    public void DeleteKeyTree(RegHive hive, string path) => Write(() =>
    {
        if (string.IsNullOrWhiteSpace(path) || path.Count(c => c == '\\') < 1)
            throw new InvalidOperationException("Refusing to delete a top-level key: " + path);
        using var root = Root(hive);
        root.DeleteSubKeyTree(path, throwOnMissingSubKey: false);
        return 0;
    });
}

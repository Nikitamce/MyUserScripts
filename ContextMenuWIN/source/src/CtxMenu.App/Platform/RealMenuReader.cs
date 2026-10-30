using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CtxMenu.Core.Preview;

namespace CtxMenu.App.Platform;

/// <summary>
/// Asks Windows to build the real classic context menu (IContextMenu::QueryContextMenu) for a sample
/// object and reads the resulting HMENU: texts, separators, greyed state, submenus, verbs and bitmaps.
/// Nothing is ever invoked: the menu is destroyed right after reading.
/// </summary>
public static class RealMenuReader
{
    private const uint CMF_NORMAL = 0, CMF_EXTENDEDVERBS = 0x100;
    private const uint IdFirst = 1, IdLast = 0x7FFF;

    public static Task<List<PreviewItem>> ReadAsync(PreviewTarget target, string? extension, bool extended, TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource<List<PreviewItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try { tcs.TrySetResult(Read(target, extension, extended)); }
            catch (Exception ex) { tcs.TrySetException(ex); }
        }) { IsBackground = true, Name = "RealMenuReader" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        _ = Task.Delay(timeout).ContinueWith(_ => tcs.TrySetException(new TimeoutException("timeout")));
        return tcs.Task;
    }

    // ---------------- sample objects ----------------

    private static string SampleDir
    {
        get
        {
            var d = Path.Combine(Path.GetTempPath(), "CtxMenuPreview");
            Directory.CreateDirectory(d);
            return d;
        }
    }

    private static string SamplePath(PreviewTarget t, string? ext)
    {
        switch (t)
        {
            case PreviewTarget.Folder:
            case PreviewTarget.FolderBackground:
                var dir = Path.Combine(SampleDir, "Папка");
                Directory.CreateDirectory(dir);
                return dir;
            case PreviewTarget.Drive:
                return Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\";
            default:
                var e = PreviewTargets.EffectiveExtension(t, ext) ?? ".txt";
                var file = Path.Combine(SampleDir, "Пример" + e);
                if (!File.Exists(file)) File.WriteAllBytes(file, Array.Empty<byte>());
                return file;
        }
    }

    private static List<PreviewItem> Read(PreviewTarget target, string? extension, bool extended)
    {
        IntPtr pcm = IntPtr.Zero, hmenu = IntPtr.Zero;
        var toFree = new List<IntPtr>();
        try
        {
            pcm = GetContextMenu(target, extension, toFree);
            var cm = (IContextMenu)Marshal.GetObjectForIUnknown(pcm);
            hmenu = CreatePopupMenu();
            var hr = cm.QueryContextMenu(hmenu, 0, IdFirst, IdLast, extended ? CMF_EXTENDEDVERBS : CMF_NORMAL);
            if (hr < 0) Marshal.ThrowExceptionForHR(hr);
            IContextMenu2? cm2 = null;
            try { cm2 = cm as IContextMenu2; } catch { /* optional */ }
            var items = ReadMenu(hmenu, cm, cm2, 0);
            Marshal.ReleaseComObject(cm);
            return items;
        }
        finally
        {
            if (hmenu != IntPtr.Zero) DestroyMenu(hmenu);
            if (pcm != IntPtr.Zero) Marshal.Release(pcm);
            foreach (var p in toFree) Marshal.FreeCoTaskMem(p);
        }
    }

    private static IntPtr GetContextMenu(PreviewTarget target, string? extension, List<IntPtr> toFree)
    {
        var iidMenu = typeof(IContextMenu).GUID;
        var iidFolder = typeof(IShellFolder).GUID;
        Check(SHGetDesktopFolder(out var desktop));
        try
        {
            if (target == PreviewTarget.DesktopBackground)
            {
                Check(desktop.CreateViewObject(IntPtr.Zero, ref iidMenu, out var ppv));
                return ppv;
            }
            var path = SamplePath(target, extension);
            Check(SHParseDisplayName(path, IntPtr.Zero, out var pidl, 0, out _));
            toFree.Add(pidl);
            if (target == PreviewTarget.FolderBackground)
            {
                Check(desktop.BindToObject(pidl, IntPtr.Zero, ref iidFolder, out var pFolder));
                var folder = (IShellFolder)Marshal.GetObjectForIUnknown(pFolder);
                Marshal.Release(pFolder);
                try
                {
                    Check(folder.CreateViewObject(IntPtr.Zero, ref iidMenu, out var ppv));
                    return ppv;
                }
                finally { Marshal.ReleaseComObject(folder); }
            }
            Check(SHBindToParent(pidl, ref iidFolder, out var pParent, out var child));
            var parent = (IShellFolder)Marshal.GetObjectForIUnknown(pParent);
            Marshal.Release(pParent);
            try
            {
                Check(parent.GetUIObjectOf(IntPtr.Zero, 1, new[] { child }, ref iidMenu, IntPtr.Zero, out var ppv));
                return ppv;
            }
            finally { Marshal.ReleaseComObject(parent); }
        }
        finally { Marshal.ReleaseComObject(desktop); }
    }

    private static void Check(int hr) { if (hr < 0) Marshal.ThrowExceptionForHR(hr); }

    // ---------------- HMENU walking ----------------

    private static List<PreviewItem> ReadMenu(IntPtr hmenu, IContextMenu cm, IContextMenu2? cm2, int depth)
    {
        var list = new List<PreviewItem>();
        var count = GetMenuItemCount(hmenu);
        var defaultId = GetMenuDefaultItem(hmenu, 0, 0);
        for (uint i = 0; i < count; i++)
        {
            var buf = Marshal.AllocHGlobal(1024);
            MENUITEMINFO mii;
            string text;
            try
            {
                Marshal.WriteInt16(buf, 0);
                mii = new MENUITEMINFO
                {
                    cbSize = (uint)Marshal.SizeOf<MENUITEMINFO>(),
                    fMask = MIIM_STATE | MIIM_ID | MIIM_SUBMENU | MIIM_STRING | MIIM_BITMAP | MIIM_FTYPE | MIIM_CHECKMARKS,
                    dwTypeData = buf,
                    cch = 511,
                };
                if (!GetMenuItemInfoW(hmenu, i, true, ref mii)) continue;
                text = (mii.fType & MFT_SEPARATOR) != 0 ? "" : Marshal.PtrToStringUni(buf) ?? "";
            }
            finally { Marshal.FreeHGlobal(buf); }
            if ((mii.fType & MFT_SEPARATOR) != 0) { list.Add(PreviewItem.Separator()); continue; }

            var tab = text.IndexOf('\t');
            string? shortcut = null;
            if (tab >= 0) { shortcut = text[(tab + 1)..]; text = text[..tab]; }
            var item = new PreviewItem
            {
                Kind = PreviewItemKind.Real,
                Text = text.Replace("&&", "\u0001").Replace("&", "").Replace("\u0001", "&"),
                Shortcut = shortcut,
                IsGreyed = (mii.fState & MFS_GRAYED) != 0,
                IsChecked = (mii.fState & MFS_CHECKED) != 0,
                IsRadio = (mii.fType & MFT_RADIOCHECK) != 0,
                IsDefault = (mii.fState & MFS_DEFAULT) != 0 || (defaultId != -1 && defaultId == mii.wID),
            };
            if (mii.wID >= IdFirst && mii.wID <= IdLast) item.Verb = GetVerb(cm, mii.wID - IdFirst);
            item.IconImage = ToImage(mii.hbmpItem) ?? ToImage(mii.hbmpUnchecked);

            if (mii.hSubMenu != IntPtr.Zero)
            {
                item.IsSubmenu = true;
                if (depth < 3)
                {
                    if (cm2 != null)
                    {
                        try { cm2.HandleMenuMsg(WM_INITMENUPOPUP, mii.hSubMenu, (IntPtr)i); } catch { /* handler refused */ }
                    }
                    item.Children.AddRange(ReadMenu(mii.hSubMenu, cm, cm2, depth + 1));
                }
            }
            if (item.Text.Length == 0 && item.IconImage == null && !item.IsSubmenu) continue;
            list.Add(item);
        }
        return MenuPreviewBuilder.Clean(list);
    }

    private static string? GetVerb(IContextMenu cm, uint offset)
    {
        var mem = Marshal.AllocHGlobal(1024);
        try
        {
            Marshal.WriteInt16(mem, 0);
            var hr = cm.GetCommandString((UIntPtr)offset, GCS_VERBW, IntPtr.Zero, mem, 511);
            if (hr < 0) return null;
            var s = Marshal.PtrToStringUni(mem);
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }
        catch { return null; }
        finally { Marshal.FreeHGlobal(mem); }
    }

    private static ImageSource? ToImage(IntPtr hbmp)
    {
        // HBMMENU_* system constants are small integers, not real bitmaps
        if (hbmp == IntPtr.Zero || (long)hbmp is >= -1 and <= 11) return null;
        try
        {
            var bm = new BITMAP();
            if (GetObject(hbmp, Marshal.SizeOf<BITMAP>(), ref bm) == 0 || bm.bmWidth <= 0 || bm.bmHeight <= 0 || bm.bmWidth > 256 || bm.bmHeight > 256) return null;
            int w = bm.bmWidth, h = bm.bmHeight;
            var bmi = new BITMAPINFO { biSize = 40, biWidth = w, biHeight = -h, biPlanes = 1, biBitCount = 32 };
            var pixels = new byte[w * h * 4];
            var dc = GetDC(IntPtr.Zero);
            try
            {
                if (GetDIBits(dc, hbmp, 0, (uint)h, pixels, ref bmi, 0) == 0) return null;
            }
            finally { ReleaseDC(IntPtr.Zero, dc); }
            // Bitmaps without alpha would come out fully transparent
            var hasAlpha = false;
            for (var p = 3; p < pixels.Length; p += 4) if (pixels[p] != 0) { hasAlpha = true; break; }
            if (!hasAlpha) for (var p = 3; p < pixels.Length; p += 4) pixels[p] = 255;
            var src = BitmapSource.Create(w, h, 96, 96, PixelFormats.Pbgra32, null, pixels, w * 4);
            src.Freeze();
            return src;
        }
        catch { return null; }
    }

    // ---------------- interop ----------------

    private const uint MIIM_STATE = 0x1, MIIM_ID = 0x2, MIIM_SUBMENU = 0x4, MIIM_CHECKMARKS = 0x8, MIIM_STRING = 0x40, MIIM_BITMAP = 0x80, MIIM_FTYPE = 0x100;
    private const uint MFT_SEPARATOR = 0x800, MFT_RADIOCHECK = 0x200;
    private const uint MFS_GRAYED = 0x3, MFS_CHECKED = 0x8, MFS_DEFAULT = 0x1000;
    private const uint GCS_VERBW = 4, WM_INITMENUPOPUP = 0x117;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MENUITEMINFO
    {
        public uint cbSize, fMask, fType, fState, wID;
        public IntPtr hSubMenu, hbmpChecked, hbmpUnchecked, dwItemData;
        public IntPtr dwTypeData;
        public uint cch;
        public IntPtr hbmpItem;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAP
    {
        public int bmType, bmWidth, bmHeight, bmWidthBytes;
        public ushort bmPlanes, bmBitsPixel;
        public IntPtr bmBits;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFO
    {
        public int biSize, biWidth, biHeight;
        public short biPlanes, biBitCount;
        public int biCompression, biSizeImage, biXPelsPerMeter, biYPelsPerMeter, biClrUsed, biClrImportant;
        public int bmiColors;
    }

    [DllImport("user32.dll")] private static extern IntPtr CreatePopupMenu();
    [DllImport("user32.dll")] private static extern bool DestroyMenu(IntPtr hMenu);
    [DllImport("user32.dll")] private static extern int GetMenuItemCount(IntPtr hMenu);
    [DllImport("user32.dll")] private static extern int GetMenuDefaultItem(IntPtr hMenu, uint fByPos, uint gmdiFlags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMenuItemInfoW(IntPtr hMenu, uint item, bool fByPosition, ref MENUITEMINFO mii);
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern int GetObject(IntPtr h, int c, ref BITMAP bm);
    [DllImport("gdi32.dll")] private static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint start, uint lines, [Out] byte[] bits, ref BITMAPINFO bmi, uint usage);
    [DllImport("shell32.dll")] private static extern int SHGetDesktopFolder(out IShellFolder ppshf);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] private static extern int SHParseDisplayName(string name, IntPtr bindCtx, out IntPtr pidl, uint sfgaoIn, out uint sfgaoOut);
    [DllImport("shell32.dll")] private static extern int SHBindToParent(IntPtr pidl, ref Guid riid, out IntPtr ppv, out IntPtr ppidlLast);

    [ComImport, Guid("000214E6-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellFolder
    {
        [PreserveSig] int ParseDisplayName(IntPtr hwnd, IntPtr pbc, [MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr pchEaten, out IntPtr ppidl, IntPtr pdwAttributes);
        [PreserveSig] int EnumObjects(IntPtr hwnd, int flags, out IntPtr ppenum);
        [PreserveSig] int BindToObject(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv);
        [PreserveSig] int BindToStorage(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv);
        [PreserveSig] int CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);
        [PreserveSig] int CreateViewObject(IntPtr hwndOwner, ref Guid riid, out IntPtr ppv);
        [PreserveSig] int GetAttributesOf(uint cidl, [MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, ref uint rgfInOut);
        [PreserveSig] int GetUIObjectOf(IntPtr hwndOwner, uint cidl, [MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, ref Guid riid, IntPtr rgfReserved, out IntPtr ppv);
        [PreserveSig] int GetDisplayNameOf(IntPtr pidl, uint flags, IntPtr pName);
        [PreserveSig] int SetNameOf(IntPtr hwnd, IntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] string name, uint flags, out IntPtr ppidlOut);
    }

    [ComImport, Guid("000214E4-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IContextMenu
    {
        [PreserveSig] int QueryContextMenu(IntPtr hmenu, uint indexMenu, uint idCmdFirst, uint idCmdLast, uint uFlags);
        [PreserveSig] int InvokeCommand(IntPtr pici);
        [PreserveSig] int GetCommandString(UIntPtr idCmd, uint uType, IntPtr pReserved, IntPtr pszName, uint cchMax);
    }

    [ComImport, Guid("000214F4-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IContextMenu2
    {
        [PreserveSig] int QueryContextMenu(IntPtr hmenu, uint indexMenu, uint idCmdFirst, uint idCmdLast, uint uFlags);
        [PreserveSig] int InvokeCommand(IntPtr pici);
        [PreserveSig] int GetCommandString(UIntPtr idCmd, uint uType, IntPtr pReserved, IntPtr pszName, uint cchMax);
        [PreserveSig] int HandleMenuMsg(uint uMsg, IntPtr wParam, IntPtr lParam);
    }
}

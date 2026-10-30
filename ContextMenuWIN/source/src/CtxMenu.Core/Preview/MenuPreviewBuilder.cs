using CtxMenu.Core.Localization;
using CtxMenu.Core.Model;
using CtxMenu.Core.Scanning;
using CtxMenu.Core.Services;

namespace CtxMenu.Core.Preview;

internal enum Slot { None, Separator, Top, Registry, Bottom, Packaged, New }

/// <summary>One line of a built-in menu layout. Verb/Handler bind the line to the registry item that really provides it.</summary>
internal sealed record B(
    string Key = "",
    string? Glyph = null,
    string? Shortcut = null,
    bool Greyed = false,
    string? Verb = null,
    string? Handler = null,
    B[]? Sub = null,
    Slot Slot = Slot.None,
    bool Toolbar = false,
    bool ShowMore = false,
    bool Default = false,
    bool Checked = false,
    bool Radio = false,
    bool Dynamic = false);

/// <summary>Builds a faithful model of the menu Explorer shows for a target from the scanned registry data.</summary>
public static class MenuPreviewBuilder
{
    public static List<PreviewItem> Build(PreviewOptions o, IEnumerable<ContextMenuEntry> entries, IEnumerable<ShellNewItem> shellNew, IReadOnlySet<string> scopes)
    {
        var pool = Pool(entries, scopes);
        var layout = PreviewLayouts.For(o.Target, o.Style);
        var consumed = new HashSet<ContextMenuEntry>(ReferenceEqualityComparer.Instance);
        var bindings = new Dictionary<B, ContextMenuEntry>(ReferenceEqualityComparer.Instance);
        foreach (var b in Flatten(layout))
        {
            var e = FindBound(b, pool, consumed);
            if (e == null) continue;
            consumed.Add(e);
            bindings[b] = e;
        }

        var ctx = new Ctx(o, pool, consumed, bindings, shellNew.ToList());
        var result = new List<PreviewItem>();
        foreach (var b in layout) ctx.Emit(b, result);
        return Clean(result);
    }

    /// <summary>Items applying to the target, most specific scope first, duplicates (same verb / CLSID / HKCU+HKLM) collapsed.</summary>
    public static List<ContextMenuEntry> Pool(IEnumerable<ContextMenuEntry> entries, IReadOnlySet<string> scopes)
    {
        var ordered = entries.Where(e => PreviewTargets.Applies(e, scopes))
            .Select((e, i) => (e, i))
            .OrderBy(x => x.e.Kind == EntryKind.Packaged ? 0 : PreviewTargets.ScopeRank(x.e.Scope))
            .ThenBy(x => x.e.Hive == Registry.RegHive.CurrentUser ? 0 : 1)
            .ThenBy(x => x.i)
            .Select(x => x.e);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<ContextMenuEntry>();
        foreach (var e in ordered)
            if (seen.Add(DedupKey(e))) result.Add(e);
        return result;
    }

    private static string DedupKey(ContextMenuEntry e) => e.Kind switch
    {
        EntryKind.Verb => "v|" + e.KeyName.TrimStart('-'),
        EntryKind.ShellExtension => "h|" + (e.Clsid ?? e.KeyName.TrimStart('-')),
        _ => "p|" + e.Clsid,
    };

    private static IEnumerable<B> Flatten(IEnumerable<B> items)
    {
        foreach (var b in items)
        {
            yield return b;
            if (b.Sub != null)
                foreach (var c in Flatten(b.Sub)) yield return c;
        }
    }

    private static ContextMenuEntry? FindBound(B b, List<ContextMenuEntry> pool, HashSet<ContextMenuEntry> consumed)
    {
        if (b.Verb != null)
        {
            var verbs = b.Verb.Split('|');
            return pool.FirstOrDefault(e => e.Kind == EntryKind.Verb && !consumed.Contains(e) &&
                                            verbs.Any(v => v.Equals(e.KeyName.TrimStart('-'), StringComparison.OrdinalIgnoreCase)));
        }
        if (b.Handler != null)
        {
            var keys = b.Handler.Split('|');
            return pool.FirstOrDefault(e => e.Kind == EntryKind.ShellExtension && !consumed.Contains(e) &&
                                            keys.Any(k => k.Equals(e.Clsid, StringComparison.OrdinalIgnoreCase) ||
                                                          k.Equals(e.KeyName.TrimStart('-'), StringComparison.OrdinalIgnoreCase)));
        }
        return null;
    }

    /// <summary>Removes leading, trailing and doubled separators (recursively).</summary>
    public static List<PreviewItem> Clean(List<PreviewItem> items)
    {
        var r = new List<PreviewItem>();
        foreach (var i in items)
        {
            if (i.Kind == PreviewItemKind.Separator)
            {
                if (r.Count == 0 || r[^1].Kind == PreviewItemKind.Separator || r[^1].IsToolbar) continue;
            }
            else if (i.Children.Count > 0)
            {
                var c = Clean(i.Children);
                i.Children.Clear();
                i.Children.AddRange(c);
            }
            r.Add(i);
        }
        while (r.Count > 0 && r[^1].Kind == PreviewItemKind.Separator) r.RemoveAt(r.Count - 1);
        return r;
    }

    public static bool IsVisible(ContextMenuEntry e, PreviewOptions o) =>
        (o.ShowDisabled || e.IsEnabled) && (o.ShowExtended || !e.IsExtended);

    /// <summary>Line for a scanned registry/packaged item, with its submenu.</summary>
    public static PreviewItem FromEntry(ContextMenuEntry e, PreviewOptions o)
    {
        var item = new PreviewItem
        {
            Kind = PreviewItemKind.Entry,
            Entry = e,
            Text = string.IsNullOrWhiteSpace(e.DisplayName) ? e.KeyName.TrimStart('-') : e.DisplayName,
            IconLocation = e.IconLocation,
            Verb = e.Kind == EntryKind.Verb ? e.KeyName.TrimStart('-') : null,
            IsSubmenu = e.Submenu != SubmenuKind.None,
        };
        foreach (var c in e.Children.Where(c => IsVisible(c, o)))
            item.Children.Add(FromEntry(c, o));
        return item;
    }

    private static readonly string[] LeadingVerbs = { "open", "opennew", "explore", "edit", "play", "preview", "print" };

    private static int VerbOrder(ContextMenuEntry e)
    {
        var i = Array.FindIndex(LeadingVerbs, v => v.Equals(e.KeyName.TrimStart('-'), StringComparison.OrdinalIgnoreCase));
        return i < 0 ? LeadingVerbs.Length : i;
    }

    private static bool IsPos(ContextMenuEntry e, string pos) => string.Equals(e.Position?.Trim(), pos, StringComparison.OrdinalIgnoreCase);

    // ---------------- real menu ----------------

    /// <summary>Links lines of a real menu (read from Windows) to scanned items so they can be toggled,
    /// and appends switched-off items, which Windows no longer shows, so they can be turned back on.</summary>
    public static List<PreviewItem> MatchReal(List<PreviewItem> real, IEnumerable<ContextMenuEntry> entries, IReadOnlySet<string> scopes, PreviewOptions o)
    {
        var pool = Pool(entries, scopes);
        var used = new HashSet<ContextMenuEntry>(ReferenceEqualityComparer.Instance);
        Match(real, pool.SelectMany(e => e.SelfAndDescendants()).ToList(), used);
        var result = new List<PreviewItem>(real);
        if (o.ShowDisabled)
        {
            var off = pool.Where(e => !e.IsEnabled && !used.Contains(e) && (o.ShowExtended || !e.IsExtended)).ToList();
            if (off.Count > 0)
            {
                result.Add(PreviewItem.Separator());
                result.Add(new PreviewItem { Kind = PreviewItemKind.BuiltIn, Text = Loc.Get("Pv_OffSection"), IsGreyed = true });
                foreach (var e in off) result.Add(FromEntry(e, o));
            }
        }
        return Clean(result);
    }

    private static void Match(List<PreviewItem> items, List<ContextMenuEntry> candidates, HashSet<ContextMenuEntry> used)
    {
        foreach (var i in items)
        {
            if (i.Kind == PreviewItemKind.Separator) continue;
            var text = CommandLine.StripAccelerators(i.Text).Trim();
            var e = (i.Verb != null ? candidates.FirstOrDefault(c => c.Kind == EntryKind.Verb && !used.Contains(c) &&
                                                                     c.KeyName.TrimStart('-').Equals(i.Verb, StringComparison.OrdinalIgnoreCase)) : null)
                    ?? candidates.FirstOrDefault(c => !used.Contains(c) && text.Length > 0 &&
                                                      CommandLine.StripAccelerators(c.DisplayName).Trim().Equals(text, StringComparison.CurrentCultureIgnoreCase));
            if (e != null)
            {
                i.Entry = e;
                used.Add(e);
            }
            if (i.Children.Count > 0) Match(i.Children, candidates, used);
        }
    }

    // ---------------- emission ----------------

    private sealed class Ctx
    {
        private readonly PreviewOptions _o;
        private readonly List<ContextMenuEntry> _pool;
        private readonly HashSet<ContextMenuEntry> _consumed;
        private readonly Dictionary<B, ContextMenuEntry> _bindings;
        private readonly List<ShellNewItem> _shellNew;

        public Ctx(PreviewOptions o, List<ContextMenuEntry> pool, HashSet<ContextMenuEntry> consumed, Dictionary<B, ContextMenuEntry> bindings, List<ShellNewItem> shellNew)
        {
            _o = o; _pool = pool; _consumed = consumed; _bindings = bindings; _shellNew = shellNew;
        }

        private IEnumerable<ContextMenuEntry> Free => _pool.Where(e => !_consumed.Contains(e) && IsVisible(e, _o));

        public void Emit(B b, List<PreviewItem> into)
        {
            switch (b.Slot)
            {
                case Slot.Separator: into.Add(PreviewItem.Separator()); return;
                case Slot.Top:
                    into.AddRange(Free.Where(e => e.Kind != EntryKind.Packaged && IsPos(e, "Top")).Select(e => FromEntry(e, _o)));
                    return;
                case Slot.Bottom:
                    into.AddRange(Free.Where(e => e.Kind != EntryKind.Packaged && IsPos(e, "Bottom")).Select(e => FromEntry(e, _o)));
                    return;
                case Slot.Registry:
                    var normal = Free.Where(e => !IsPos(e, "Top") && !IsPos(e, "Bottom")).ToList();
                    into.AddRange(normal.Where(e => e.Kind == EntryKind.Verb).OrderBy(VerbOrder).Select(e => FromEntry(e, _o)));
                    into.AddRange(normal.Where(e => e.Kind == EntryKind.ShellExtension).Select(e => FromEntry(e, _o)));
                    into.AddRange(normal.Where(e => e.Kind == EntryKind.Packaged).Select(e => FromEntry(e, _o)));
                    return;
                case Slot.Packaged:
                    into.AddRange(Free.Where(e => e.Kind == EntryKind.Packaged).Select(e => FromEntry(e, _o)));
                    return;
            }

            _bindings.TryGetValue(b, out var bound);
            if (bound != null && !IsVisible(bound, _o)) return;
            var item = new PreviewItem
            {
                Kind = PreviewItemKind.BuiltIn,
                Text = Loc.Get(b.Key),
                Glyph = b.Glyph,
                Shortcut = b.Shortcut,
                IsGreyed = b.Greyed,
                IsToolbar = b.Toolbar,
                IsShowMore = b.ShowMore,
                IsDefault = b.Default,
                IsChecked = b.Checked,
                IsRadio = b.Radio,
                IsSubmenu = b.Sub != null || b.Dynamic,
                Entry = bound,
                Verb = b.Verb?.Split('|')[0],
                IconLocation = _o.Style == PreviewStyle.Classic ? bound?.IconLocation : null,
            };
            if (b.Slot == Slot.New) FillNew(item);
            else if (b.Sub != null)
                foreach (var c in b.Sub) Emit(c, item.Children);
            into.Add(item);
        }

        private void FillNew(PreviewItem menu)
        {
            menu.IsSubmenu = true;
            var items = _shellNew
                .GroupBy(i => i.Extension, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(i => i.IsEnabled).ThenBy(i => i.Hive).First())
                .Where(i => _o.ShowDisabled || i.IsEnabled)
                .ToList();

            var folder = items.FirstOrDefault(i => i.Extension.Equals("Folder", StringComparison.OrdinalIgnoreCase));
            var link = items.FirstOrDefault(i => i.Extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase));
            var anyFolder = _shellNew.Any(i => i.Extension.Equals("Folder", StringComparison.OrdinalIgnoreCase));
            var anyLink = _shellNew.Any(i => i.Extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase));
            if (folder != null || !anyFolder)
                menu.Children.Add(new PreviewItem { Kind = folder != null ? PreviewItemKind.ShellNew : PreviewItemKind.BuiltIn, NewItem = folder, Text = Loc.Get("Pv_NewFolder"), Glyph = "\uE8B7" });
            if (link != null || !anyLink)
                menu.Children.Add(new PreviewItem { Kind = link != null ? PreviewItemKind.ShellNew : PreviewItemKind.BuiltIn, NewItem = link, Text = Loc.Get("Pv_NewShortcut"), Glyph = "\uE71B" });
            menu.Children.Add(PreviewItem.Separator());
            foreach (var i in items.Where(i => i != folder && i != link).OrderBy(i => i.DisplayName, StringComparer.CurrentCultureIgnoreCase))
                menu.Children.Add(new PreviewItem { Kind = PreviewItemKind.ShellNew, NewItem = i, Text = i.DisplayName, IconLocation = i.IconLocation });
        }
    }
}

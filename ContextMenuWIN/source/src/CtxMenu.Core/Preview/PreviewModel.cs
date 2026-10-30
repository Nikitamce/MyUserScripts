using CtxMenu.Core.Model;
using CtxMenu.Core.Services;

namespace CtxMenu.Core.Preview;

/// <summary>What the user right-clicks in the live preview.</summary>
public enum PreviewTarget
{
    DesktopBackground, FolderBackground, Folder, Drive,
    File, Image, Video, Audio, Document, Archive, Executable,
}

/// <summary>Windows 11 compact menu, or the full classic one ("Показать дополнительные параметры" / Windows 10).</summary>
public enum PreviewStyle { Modern, Classic }

public enum PreviewItemKind
{
    /// <summary>Item drawn by Explorer itself (Вид, Обновить, Вставить…), possibly bound to the registry item that provides it.</summary>
    BuiltIn,
    /// <summary>Item from the registry or a packaged app (verb, shell extension, Win11 IExplorerCommand).</summary>
    Entry,
    /// <summary>Item of the "Создать" submenu.</summary>
    ShellNew,
    /// <summary>Item read from the real menu built by Windows (IContextMenu).</summary>
    Real,
    Separator,
}

public sealed class PreviewItem
{
    public PreviewItemKind Kind { get; set; }
    public string Text { get; set; } = "";
    /// <summary>Segoe Fluent Icons code point for built-in items.</summary>
    public string? Glyph { get; set; }
    public string? Shortcut { get; set; }
    public string? IconLocation { get; set; }
    /// <summary>Platform image (e.g. a bitmap taken from a real HMENU). Opaque to the core.</summary>
    public object? IconImage { get; set; }
    /// <summary>Registry item that controls this line (toggling it hides/shows the line).</summary>
    public ContextMenuEntry? Entry { get; set; }
    public ShellNewItem? NewItem { get; set; }
    /// <summary>Canonical verb (real menu) or the verb a built-in line is bound to.</summary>
    public string? Verb { get; set; }
    public List<PreviewItem> Children { get; } = new();
    /// <summary>Submenu even when the children are unknown (dynamic handlers).</summary>
    public bool IsSubmenu { get; set; }
    /// <summary>Drawn greyed out by Windows (e.g. "Вставить" with an empty clipboard).</summary>
    public bool IsGreyed { get; set; }
    public bool IsDefault { get; set; }
    public bool IsChecked { get; set; }
    public bool IsRadio { get; set; }
    /// <summary>Windows 11 icon row on top of the compact menu (Вырезать, Копировать…).</summary>
    public bool IsToolbar { get; set; }
    /// <summary>"Показать дополнительные параметры".</summary>
    public bool IsShowMore { get; set; }

    public bool HasSubmenu => IsSubmenu || Children.Count > 0;

    /// <summary>The bound item is switched off: Windows does not show this line right now.</summary>
    public bool IsHiddenByUser => (Entry != null && !Entry.IsEnabled) || (NewItem != null && !NewItem.IsEnabled);

    /// <summary>Shown only while Shift is held.</summary>
    public bool IsExtendedOnly => Entry?.IsExtended == true;

    public bool IsEditable => Entry != null || NewItem != null;

    public static PreviewItem Separator() => new() { Kind = PreviewItemKind.Separator };

    public IEnumerable<PreviewItem> SelfAndDescendants()
    {
        yield return this;
        foreach (var c in Children)
            foreach (var d in c.SelfAndDescendants())
                yield return d;
    }

    public override string ToString() => Kind == PreviewItemKind.Separator ? "----" : Text;
}

public sealed record PreviewOptions(
    PreviewTarget Target,
    string? Extension,
    PreviewStyle Style,
    bool ShowDisabled = true,
    bool ShowExtended = false);

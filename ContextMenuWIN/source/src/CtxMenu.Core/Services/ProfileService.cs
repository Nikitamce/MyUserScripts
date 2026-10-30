using CtxMenu.Core.Editing;
using CtxMenu.Core.Localization;
using CtxMenu.Core.Model;

namespace CtxMenu.Core.Services;

public sealed class MenuProfile
{
    public int Version { get; set; } = 1;
    public DateTimeOffset Created { get; set; } = DateTimeOffset.Now;
    public string Machine { get; set; } = Environment.MachineName;
    public List<string> Disabled { get; set; } = new();
    public List<string> ShiftOnly { get; set; } = new();
    public List<CustomEntryDefinition> Custom { get; set; } = new();
}

public sealed record ProfileImportResult(int Disabled, int Enabled, int ShiftChanged, int CustomCreated, int NotFound);

/// <summary>Export/import of the user's menu configuration as portable JSON (by StableId).</summary>
public sealed class ProfileService
{
    private readonly ContextMenuActions _actions;
    private readonly CustomEntryService _custom;
    private readonly RegistryEditor _editor;

    public ProfileService(RegistryEditor editor, ContextMenuActions actions, CustomEntryService custom)
    {
        _editor = editor;
        _actions = actions;
        _custom = custom;
    }

    public MenuProfile Build(IEnumerable<ContextMenuEntry> entries)
    {
        var all = entries.SelectMany(e => e.SelfAndDescendants()).ToList();
        return new MenuProfile
        {
            Disabled = all.Where(e => !e.IsEnabled && !e.IsCustom).Select(e => e.StableId).Distinct().OrderBy(x => x).ToList(),
            ShiftOnly = all.Where(e => e.IsExtended && !e.IsCustom).Select(e => e.StableId).Distinct().OrderBy(x => x).ToList(),
            Custom = _custom.LoadDefinitions(),
        };
    }

    public string Export(IEnumerable<ContextMenuEntry> entries) => ChangeJournal.Serialize(Build(entries));

    public static MenuProfile Parse(string json) =>
        ChangeJournal.Deserialize<MenuProfile>(json) ?? throw new FormatException(Loc.Get("Err_BadProfile"));

    /// <summary>Applies a profile. When <paramref name="exact"/> is set, items not listed as disabled are re-enabled.</summary>
    public ProfileImportResult Apply(MenuProfile profile, IEnumerable<ContextMenuEntry> entries, HandlerDisableMethod method, bool exact)
    {
        var all = entries.SelectMany(e => e.SelfAndDescendants()).Where(e => !e.IsCustom).ToList();
        var byId = all.GroupBy(e => e.StableId).ToDictionary(g => g.Key, g => g.ToList());
        var disabled = new HashSet<string>(profile.Disabled);
        var shift = new HashSet<string>(profile.ShiftOnly);

        var toDisable = all.Where(e => disabled.Contains(e.StableId) && e.IsEnabled).ToList();
        var toEnable = exact ? all.Where(e => !disabled.Contains(e.StableId) && !e.IsEnabled).ToList() : new List<ContextMenuEntry>();
        var notFound = profile.Disabled.Count(id => !byId.ContainsKey(id));

        if (toDisable.Count > 0) _actions.SetEnabledMany(toDisable, false, method, Loc.Get("Journal_ProfileDisable"));
        if (toEnable.Count > 0) _actions.SetEnabledMany(toEnable, true, method, Loc.Get("Journal_ProfileEnable"));

        var shiftChanged = 0;
        foreach (var e in all.Where(e => e.Kind == EntryKind.Verb))
        {
            var want = shift.Contains(e.StableId);
            if (want == e.IsExtended || (!want && !exact)) continue;
            _actions.SetExtended(e, want);
            shiftChanged++;
        }

        var created = 0;
        foreach (var def in profile.Custom.Where(d => !_custom.Exists(d)))
        {
            _custom.Create(def);
            created++;
        }
        return new ProfileImportResult(toDisable.Count, toEnable.Count, shiftChanged, created, notFound);
    }
}

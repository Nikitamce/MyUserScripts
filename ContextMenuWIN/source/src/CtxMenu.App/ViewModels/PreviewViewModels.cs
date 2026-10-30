using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using CtxMenu.App.Platform;
using CtxMenu.Core.Localization;
using CtxMenu.Core.Model;
using CtxMenu.Core.Preview;
using CtxMenu.Core.Scanning;
using CtxMenu.Core.Services;

namespace CtxMenu.App.ViewModels;

public sealed record PreviewTargetOption(PreviewTarget Target, string Glyph)
{
    public string Title => Loc.Get(PreviewTargets.LocKey(Target));
}

/// <summary>"Живой предпросмотр": draws the context menu Windows shows right now and lets you switch its items in place.</summary>
public sealed class PreviewPageViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private PreviewTargetOption _target;
    private string _extension = "";
    private PreviewStyle _style;
    private bool _showDisabled = true, _showExtended, _editMode = true, _useReal, _loading;
    private string? _realStatus;
    private int _version;
    private Dictionary<ContextMenuEntry, EntryViewModel> _lookup = new(ReferenceEqualityComparer.Instance);

    public PreviewPageViewModel(MainViewModel main)
    {
        _main = main;
        Targets = PreviewTargets.All.Select(t => new PreviewTargetOption(t, PreviewTargets.Glyph(t))).ToList();
        _target = Targets[0];
        _style = ModernAvailable ? PreviewStyle.Modern : PreviewStyle.Classic;
        RefreshCommand = new RelayCommand(Rebuild);
        StyleModernCommand = new RelayCommand(() => Style = PreviewStyle.Modern, () => ModernAvailable);
        StyleClassicCommand = new RelayCommand(() => Style = PreviewStyle.Classic);
    }

    public List<PreviewTargetOption> Targets { get; private set; }

    public PreviewTargetOption SelectedTarget
    {
        get => _target;
        set { if (value != null && Set(ref _target, value)) { OnPropertyChanged(nameof(IsFileTarget)); OnPropertyChanged(nameof(ExtensionHint)); Rebuild(); } }
    }

    public bool IsFileTarget => PreviewTargets.IsFile(_target.Target);
    public string ExtensionHint => PreviewTargets.DefaultExtension(_target.Target) ?? "";

    public string Extension
    {
        get => _extension;
        set { if (Set(ref _extension, value)) Rebuild(); }
    }

    /// <summary>The new Windows 11 menu exists (Windows 11 and the classic menu is not forced).</summary>
    public bool ModernAvailable => SystemHelper.IsWindows11 && !_main.Win11.IsClassicMenuEnabled;

    public PreviewStyle Style
    {
        get => _style;
        set
        {
            if (value == PreviewStyle.Modern && !ModernAvailable) value = PreviewStyle.Classic;
            if (!Set(ref _style, value)) return;
            OnPropertyChanged(nameof(IsModern));
            OnPropertyChanged(nameof(IsClassic));
            Rebuild();
        }
    }

    public bool IsModern { get => _style == PreviewStyle.Modern; set { if (value) Style = PreviewStyle.Modern; } }
    public bool IsClassic { get => _style == PreviewStyle.Classic; set { if (value) Style = PreviewStyle.Classic; } }

    public bool ShowDisabled { get => _showDisabled; set { if (Set(ref _showDisabled, value)) Rebuild(); } }
    public bool ShowExtended { get => _showExtended; set { if (Set(ref _showExtended, value)) Rebuild(); } }
    public bool EditMode { get => _editMode; set => Set(ref _editMode, value); }

    public bool UseReal
    {
        get => _useReal;
        set
        {
            if (!Set(ref _useReal, value)) return;
            if (value && IsModern) Style = PreviewStyle.Classic;
            Rebuild();
        }
    }

    public bool IsLoading { get => _loading; private set => Set(ref _loading, value); }
    public string? RealStatus { get => _realStatus; private set => Set(ref _realStatus, value); }

    public ObservableCollection<PreviewColumnViewModel> Columns { get; } = new();

    public string Counts { get; private set; } = "";
    public List<string> Notes { get; private set; } = new();
    public bool HasNotes => Notes.Count > 0;

    public ICommand RefreshCommand { get; }
    public ICommand StyleModernCommand { get; }
    public ICommand StyleClassicCommand { get; }

    // ---------------- building ----------------

    public void Rebuild()
    {
        if (_style == PreviewStyle.Modern && !ModernAvailable) { _style = PreviewStyle.Classic; OnPropertyChanged(nameof(IsModern)); OnPropertyChanged(nameof(IsClassic)); }
        _lookup = new Dictionary<ContextMenuEntry, EntryViewModel>(ReferenceEqualityComparer.Instance);
        foreach (var e in _main.Entries) Index(e);

        var ext = PreviewTargets.EffectiveExtension(_target.Target, _extension);
        var o = new PreviewOptions(_target.Target, ext, _style, _showDisabled, _showExtended);
        var scopes = PreviewTargets.ScopesFor(_target.Target, ext, x => _main.Scanner.GetScopesForExtension(x));
        var entries = _main.Entries.Select(e => e.Model).ToList();
        var version = ++_version;

        if (_useReal && _style == PreviewStyle.Classic && OperatingSystem.IsWindows())
        {
            IsLoading = true;
            RealStatus = Loc.Get("Pv_RealLoading");
            _ = LoadRealAsync(o, scopes, entries, version);
            return;
        }
        RealStatus = _useReal && _style == PreviewStyle.Modern ? Loc.Get("Pv_RealModernNote") : null;
        IsLoading = false;
        var items = MenuPreviewBuilder.Build(o, entries, _main.ShellNewPage.Items.Select(i => i.Model), scopes);
        Show(items);
    }

    private async Task LoadRealAsync(PreviewOptions o, HashSet<string> scopes, List<ContextMenuEntry> entries, int version)
    {
        List<PreviewItem> items;
        string status;
        try
        {
            var real = await RealMenuReader.ReadAsync(o.Target, o.Extension, o.ShowExtended, TimeSpan.FromSeconds(15));
            items = MenuPreviewBuilder.MatchReal(real, entries, scopes, o);
            status = Loc.Format("Pv_RealOk", real.SelectMany(r => r.SelfAndDescendants()).Count(r => r.Kind != PreviewItemKind.Separator),
                real.SelectMany(r => r.SelfAndDescendants()).Count(r => r.Entry != null));
        }
        catch (Exception ex)
        {
            items = MenuPreviewBuilder.Build(o, entries, _main.ShellNewPage.Items.Select(i => i.Model), scopes);
            status = Loc.Format("Pv_RealFailed", ex.Message);
        }
        if (version != _version) return;
        IsLoading = false;
        RealStatus = status;
        Show(items);
    }

    private void Index(EntryViewModel vm)
    {
        _lookup[vm.Model] = vm;
        foreach (var c in vm.Children) Index(c);
    }

    private void Show(List<PreviewItem> items)
    {
        var openPath = Columns.Skip(1).Select(c => c.Title).ToList();
        Columns.Clear();
        var root = new PreviewColumnViewModel(this, null, items.Select(i => new PreviewItemViewModel(i, this, 0)).ToList(), _style == PreviewStyle.Modern);
        Columns.Add(root);
        // keep the opened submenus open after a toggle
        var col = root;
        foreach (var title in openPath)
        {
            var next = col.Items.FirstOrDefault(i => i.HasSubmenu && i.Text == title);
            if (next == null) break;
            col = OpenInternal(next);
        }

        var all = items.SelectMany(i => i.SelfAndDescendants()).Where(i => i.Kind != PreviewItemKind.Separator).ToList();
        Counts = Loc.Format("Pv_Counts", all.Count(i => !i.IsHiddenByUser), all.Count(i => i.IsHiddenByUser), all.Count(i => i.IsEditable));
        var notes = new List<string>();
        if (!SystemHelper.IsWindows11) notes.Add(Loc.Get("Pv_Win10Note"));
        else if (_main.Win11.IsClassicMenuEnabled) notes.Add(Loc.Get("Pv_ClassicForced"));
        else if (_style == PreviewStyle.Modern) notes.Add(Loc.Get("Pv_ModernNote"));
        Notes = notes;
        OnPropertyChanged(nameof(Counts));
        OnPropertyChanged(nameof(Notes));
        OnPropertyChanged(nameof(HasNotes));
        OnPropertyChanged(nameof(ModernAvailable));
    }

    public void Open(PreviewItemViewModel item)
    {
        if (item.Model.IsShowMore) { Style = PreviewStyle.Classic; return; }
        if (!item.HasSubmenu) return;
        OpenInternal(item);
    }

    private PreviewColumnViewModel OpenInternal(PreviewItemViewModel item)
    {
        while (Columns.Count > item.Depth + 1) Columns.RemoveAt(Columns.Count - 1);
        foreach (var i in Columns[item.Depth].Items) i.IsOpen = false;
        item.IsOpen = true;
        var col = new PreviewColumnViewModel(this, item.Text, item.Model.Children.Select(c => new PreviewItemViewModel(c, this, item.Depth + 1)).ToList(), false);
        Columns.Add(col);
        return col;
    }

    public void OnLanguageChanged()
    {
        Targets = PreviewTargets.All.Select(t => new PreviewTargetOption(t, PreviewTargets.Glyph(t))).ToList();
        _target = Targets.First(t => t.Target == _target.Target);
        OnPropertyChanged(nameof(Targets));
        OnPropertyChanged(nameof(SelectedTarget));
        Rebuild();
    }

    // ---------------- editing ----------------

    internal EntryViewModel? Find(ContextMenuEntry? e) => e != null && _lookup.TryGetValue(e, out var vm) ? vm : null;

    internal ShellNewItemViewModel? Find(ShellNewItem? n) => n == null ? null : _main.ShellNewPage.Items.FirstOrDefault(i => ReferenceEquals(i.Model, n));

    internal void SetEnabled(PreviewItemViewModel item, bool enable)
    {
        if (Find(item.Model.Entry) is { } vm) _main.Toggle(vm, enable);
        else if (Find(item.Model.NewItem) is { } nvm) nvm.IsEnabled = enable;
        AfterChange();
    }

    internal void SetExtended(PreviewItemViewModel item, bool extended)
    {
        if (Find(item.Model.Entry) is { IsPackaged: false } vm) vm.IsExtended = extended;
        AfterChange();
    }

    internal void ShowInList(PreviewItemViewModel item)
    {
        var vm = Find(item.Model.Entry);
        if (vm == null)
        {
            if (item.Model.NewItem != null) _main.SelectPage(Page.ShellNew);
            return;
        }
        var root = vm;
        while (root.Parent != null) root = root.Parent;
        _main.SearchText = "";
        _main.ExtensionFilter = "";
        _main.SelectPage(Page.Entries);
        _main.Selected = root;
    }

    private void AfterChange() => _main.Later(Rebuild);
}

public sealed class PreviewColumnViewModel
{
    public PreviewColumnViewModel(PreviewPageViewModel page, string? title, List<PreviewItemViewModel> items, bool modern)
    {
        Title = title;
        Toolbar = items.Where(i => i.Model.IsToolbar).ToList();
        Items = items.Where(i => !i.Model.IsToolbar).ToList();
        IsModern = modern;
    }

    public string? Title { get; }
    public bool IsModern { get; }
    public List<PreviewItemViewModel> Toolbar { get; }
    public bool HasToolbar => Toolbar.Count > 0;
    public List<PreviewItemViewModel> Items { get; }
    public bool IsEmpty => Items.Count == 0 && Toolbar.Count == 0;
}

public sealed class PreviewItemViewModel : ObservableObject
{
    private readonly PreviewPageViewModel _page;
    private bool _open;
    private ImageSource? _icon;
    private bool _iconLoaded;

    public PreviewItemViewModel(PreviewItem model, PreviewPageViewModel page, int depth)
    {
        Model = model;
        _page = page;
        Depth = depth;
        OpenCommand = new RelayCommand(() => _page.Open(this));
        ToggleCommand = new RelayCommand(() => IsEnabled = !IsEnabled, () => IsEditable);
        ToggleShiftCommand = new RelayCommand(() => _page.SetExtended(this, !IsShift), () => Model.Entry is { Kind: not EntryKind.Packaged });
        ShowInListCommand = new RelayCommand(() => _page.ShowInList(this), () => IsEditable);
    }

    public PreviewItem Model { get; }
    public int Depth { get; }

    public bool IsSeparator => Model.Kind == PreviewItemKind.Separator;
    public string Text => Model.Text;
    public string? Shortcut => Model.Shortcut;
    public bool HasShortcut => !string.IsNullOrEmpty(Model.Shortcut);
    public string? Glyph => Model.Glyph;
    public bool IsGreyed => Model.IsGreyed;
    public bool IsHidden => Model.IsHiddenByUser;
    public bool IsShift => Model.IsExtendedOnly;
    public bool IsEditable => Model.IsEditable;
    public bool IsDefault => Model.IsDefault;
    public bool IsChecked => Model.IsChecked && !Model.IsRadio;
    public bool IsRadio => Model.IsChecked && Model.IsRadio;
    public bool HasSubmenu => Model.HasSubmenu;
    public bool IsShowMore => Model.IsShowMore;
    public bool IsOpen { get => _open; set => Set(ref _open, value); }

    public ImageSource? Icon
    {
        get
        {
            if (_iconLoaded) return _icon;
            _iconLoaded = true;
            _icon = Model.IconImage as ImageSource
                    ?? (Model.Glyph == null ? _page.Find(Model.Entry)?.Icon : null)
                    ?? (Model.NewItem != null ? IconCache.Get(Model.NewItem.IconLocation) : null)
                    ?? (Model.Glyph == null && Model.IconLocation != null ? IconCache.Get(Model.IconLocation) : null);
            return _icon;
        }
    }

    public bool HasIcon => Icon != null;
    public bool HasGlyph => !HasIcon && !string.IsNullOrEmpty(Model.Glyph);

    public bool IsEnabled
    {
        get => !Model.IsHiddenByUser;
        set { if (value != IsEnabled) _page.SetEnabled(this, value); }
    }

    public string ToolTip
    {
        get
        {
            if (Model.Entry is { } e)
            {
                var vm = _page.Find(e);
                var lines = new List<string> { vm?.Source ?? e.SourceName ?? "", vm?.ScopeDisplay ?? e.Scope, vm?.KindDisplay ?? e.Kind.ToString() };
                if (e.Kind == EntryKind.ShellExtension && Model.Kind != PreviewItemKind.BuiltIn && Model.Kind != PreviewItemKind.Real) lines.Add(Loc.Get("Pv_HandlerTip"));
                if (e.Clsid != null) lines.Add(e.Clsid);
                else lines.Add(e.KeyName);
                return string.Join("\n", lines.Where(l => !string.IsNullOrWhiteSpace(l)));
            }
            if (Model.NewItem is { } n) return $"{n.DisplayName}  ({n.Extension})";
            return Model.Kind == PreviewItemKind.BuiltIn ? Loc.Get("Pv_BuiltInTip") : Model.Verb ?? Model.Text;
        }
    }

    public ICommand OpenCommand { get; }
    public ICommand ToggleCommand { get; }
    public ICommand ToggleShiftCommand { get; }
    public ICommand ShowInListCommand { get; }
}

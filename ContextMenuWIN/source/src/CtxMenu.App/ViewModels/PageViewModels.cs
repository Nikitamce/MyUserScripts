using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using CtxMenu.App.Platform;
using CtxMenu.Core.Localization;
using CtxMenu.Core.Model;
using CtxMenu.Core.Scanning;
using CtxMenu.Core.Services;

namespace CtxMenu.App.ViewModels;

public sealed class ShellNewPageViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private string _filter = "", _newExt = "", _newName = "", _newTemplate = "";

    public ShellNewPageViewModel(MainViewModel main)
    {
        _main = main;
        View = (ListCollectionView)CollectionViewSource.GetDefaultView(Items);
        View.Filter = o => _filter.Length == 0 || o is ShellNewItemViewModel i &&
            (i.DisplayName.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) || i.Extension.Contains(_filter, StringComparison.OrdinalIgnoreCase));
        BrowseCommand = new RelayCommand(() => NewTemplate = Dialogs.OpenFile(Loc.Get("Filter_AllFiles")) ?? NewTemplate);
        AddCommand = new RelayCommand(Add, () => NewExtension.Trim().Length > 0);
        RemoveCommand = new RelayCommand(p => Remove(p as ShellNewItemViewModel), p => p is ShellNewItemViewModel { IsCustom: true });
        CopyPathCommand = new RelayCommand(p => { if (p is ShellNewItemViewModel i) System.Windows.Clipboard.SetText(i.FullPath); });
    }

    public ObservableCollection<ShellNewItemViewModel> Items { get; } = new();
    public ListCollectionView View { get; }
    public string Filter { get => _filter; set { if (Set(ref _filter, value)) View.Refresh(); } }
    public string NewExtension { get => _newExt; set => Set(ref _newExt, value); }
    public string NewName { get => _newName; set => Set(ref _newName, value); }
    public string NewTemplate { get => _newTemplate; set => Set(ref _newTemplate, value); }
    public ICommand BrowseCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand RemoveCommand { get; }
    public ICommand CopyPathCommand { get; }

    public void Load(IEnumerable<ShellNewItem> items)
    {
        Items.Clear();
        foreach (var i in items) Items.Add(new ShellNewItemViewModel(i, _main));
    }

    private void Add()
    {
        if (_main.Apply(() => _main.ShellNew.AddTemplate(NewExtension, NewName, NewTemplate.Length > 0 ? NewTemplate : null), false, null))
        {
            NewExtension = NewName = NewTemplate = "";
            Load(_main.ShellNew.Scan());
        }
    }

    private void Remove(ShellNewItemViewModel? item)
    {
        if (item == null || !Dialogs.Confirm(Loc.Format("Ask_DeleteCustom", item.DisplayName))) return;
        if (_main.Apply(() => _main.ShellNew.RemoveCustom(item.Model), item.NeedsAdmin, null)) Items.Remove(item);
    }
}

public sealed class ArchiversPageViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    public ArchiversPageViewModel(MainViewModel main) => _main = main;
    public List<ArchiverViewModel> Archivers { get; private set; } = new();
    public bool NoneInstalled => Archivers.All(a => !a.Installed);

    public void Load(List<ArchiverInfo> infos)
    {
        Archivers = infos.Select(i => new ArchiverViewModel(i, _main,
            _main.Entries.Where(e => e.ArchiverId == i.Id))).ToList();
        OnPropertyChanged(nameof(Archivers));
        OnPropertyChanged(nameof(NoneInstalled));
    }

    public void RaiseItems(ArchiverInfo info)
    {
        var vm = Archivers.FirstOrDefault(a => a.Info == info);
        if (vm == null) return;
        foreach (var i in vm.Items.Concat(vm.Options).Concat(vm.TopItems)) _main.Later(i.RaiseAll);
    }

    public void RaiseHandlers()
    {
        foreach (var a in Archivers) foreach (var h in a.Handlers) _main.Later(h.RaiseAll);
    }
}

public sealed class Win11PageViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private string? _packagedError;
    public Win11PageViewModel(MainViewModel main) => _main = main;

    public bool IsWindows11 => SystemHelper.IsWindows11;
    public string OsDescription => SystemHelper.OsDescription;

    public bool ClassicMenu
    {
        get => _main.Win11.IsClassicMenuEnabled;
        set => _main.Apply(() => _main.Win11.SetClassicMenu(value), false, this, after: () => _main.Later(_main.PreviewPage.Rebuild));
    }

    public List<EntryViewModel> Packaged => _main.Entries.Where(e => e.IsPackaged).OrderBy(e => e.DisplayName).ToList();
    public bool HasPackaged => Packaged.Count > 0;
    public string? PackagedError { get => _packagedError; private set => Set(ref _packagedError, value); }

    public void Load(string? error)
    {
        PackagedError = error;
        OnPropertyChanged(nameof(Packaged));
        OnPropertyChanged(nameof(HasPackaged));
        OnPropertyChanged(nameof(ClassicMenu));
    }
}

public sealed class ChildRow : ObservableObject
{
    private string _title = "", _program = "", _arguments = "", _icon = "";
    public string Title { get => _title; set => Set(ref _title, value); }
    public string Program { get => _program; set => Set(ref _program, value); }
    public string Arguments { get => _arguments; set => Set(ref _arguments, value); }
    public string Icon { get => _icon; set => Set(ref _icon, value); }
}

public sealed class CustomPageViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private string _title = "", _program = "", _arguments = "", _icon = "", _ext = "";
    private ScopeOption? _scope;
    private bool _extended, _runAsAdmin, _allUsers, _isGroup;
    private MenuPosition _position;
    private EntryViewModel? _targetGroup;
    public const string ExtensionScope = "<ext>";

    public CustomPageViewModel(MainViewModel main)
    {
        _main = main;
        RebuildScopes();
        BrowseProgramCommand = new RelayCommand(p =>
        {
            var f = Dialogs.OpenFile(Loc.Get("Filter_Programs"));
            if (f == null) return;
            if (p is ChildRow row) { row.Program = f; if (row.Icon.Length == 0) row.Icon = f; }
            else { Program = f; if (Icon.Length == 0) Icon = f; }
        });
        BrowseIconCommand = new RelayCommand(p =>
        {
            var f = Dialogs.OpenFile(Loc.Get("Filter_Icons"));
            if (f == null) return;
            if (p is ChildRow row) row.Icon = f; else Icon = f;
        });
        AddChildCommand = new RelayCommand(() => Children.Add(new ChildRow { Arguments = DefaultArgs }));
        RemoveChildCommand = new RelayCommand(p => { if (p is ChildRow r) Children.Remove(r); });
        CreateCommand = new RelayCommand(Create);
        ClearTargetCommand = new RelayCommand(() => TargetGroup = null);
        DeleteCommand = new RelayCommand(p => _main.DeleteCustom(p as EntryViewModel));
        SelectCommand = new RelayCommand(p => { if (p is EntryViewModel e) { _main.SelectPage(Page.Entries); _main.Selected = e; } });
        Arguments = DefaultArgs;
    }

    public List<ScopeOption> Scopes { get; private set; } = new();
    public List<MenuPosition> Positions { get; } = Enum.GetValues<MenuPosition>().ToList();
    public ObservableCollection<ChildRow> Children { get; } = new();
    public ObservableCollection<EntryViewModel> Existing { get; } = new();

    public string Title { get => _title; set => Set(ref _title, value); }
    public string Program { get => _program; set => Set(ref _program, value); }
    public string Arguments { get => _arguments; set => Set(ref _arguments, value); }
    public string Icon { get => _icon; set => Set(ref _icon, value); }
    public ScopeOption? Scope
    {
        get => _scope;
        set
        {
            var oldDefault = DefaultArgs;
            if (!Set(ref _scope, value)) return;
            if (Arguments == oldDefault) Arguments = DefaultArgs;
            OnPropertyChanged(nameof(IsExtensionScope));
        }
    }
    public bool IsExtensionScope => _scope?.Scope == ExtensionScope;
    public string Extension { get => _ext; set => Set(ref _ext, value); }
    public bool Extended { get => _extended; set => Set(ref _extended, value); }
    public MenuPosition Position { get => _position; set => Set(ref _position, value); }
    public bool RunAsAdmin { get => _runAsAdmin; set => Set(ref _runAsAdmin, value); }
    public bool AllUsers { get => _allUsers; set => Set(ref _allUsers, value); }
    public bool IsGroup
    {
        get => _isGroup;
        set { if (Set(ref _isGroup, value) && value && Children.Count == 0) Children.Add(new ChildRow { Arguments = DefaultArgs }); }
    }

    public EntryViewModel? TargetGroup
    {
        get => _targetGroup;
        set { Set(ref _targetGroup, value); OnPropertyChanged(nameof(HasTargetGroup)); OnPropertyChanged(nameof(TargetGroupText)); if (value != null) IsGroup = false; }
    }
    public bool HasTargetGroup => _targetGroup != null;
    public string? TargetGroupText => _targetGroup == null ? null : Loc.Format("Custom_TargetGroup", _targetGroup.DisplayName, _targetGroup.ScopeDisplay);

    private string DefaultArgs => CustomEntryService.DefaultArguments(_targetGroup?.Model.Scope ?? _scope?.Scope ?? "*");

    public ICommand BrowseProgramCommand { get; }
    public ICommand BrowseIconCommand { get; }
    public ICommand AddChildCommand { get; }
    public ICommand RemoveChildCommand { get; }
    public ICommand CreateCommand { get; }
    public ICommand ClearTargetCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand SelectCommand { get; }

    public void RebuildScopes()
    {
        var current = _scope?.Scope;
        Scopes = ScopeCatalog.CustomTargets.Select(s => new ScopeOption(s.Scope, Loc.Get(s.LocKey)))
            .Append(new ScopeOption(ExtensionScope, Loc.Get("Custom_ScopeExt"))).ToList();
        OnPropertyChanged(nameof(Scopes));
        _scope = Scopes.FirstOrDefault(s => s.Scope == current) ?? Scopes[1];
        OnPropertyChanged(nameof(Scope));
    }

    public void LoadExisting()
    {
        Existing.Clear();
        foreach (var e in _main.Entries.Where(e => e.IsCustom)) Existing.Add(e);
    }

    private string ResolveScope()
    {
        if (_scope?.Scope != ExtensionScope) return _scope?.Scope ?? "*";
        var ext = ScopeCatalog.NormalizeExtension(Extension);
        if (!ScopeCatalog.IsValidExtension(ext)) throw new ArgumentException(Loc.Format("Err_BadExtension", Extension));
        // SystemFileAssociations\.ext works regardless of which program owns the extension.
        return @"SystemFileAssociations\" + ext;
    }

    private void Create()
    {
        CustomEntryDefinition def;
        try
        {
            def = new CustomEntryDefinition
            {
                Title = Title.Trim(), Program = Program, Arguments = Arguments, Icon = Icon, Extended = Extended,
                Position = Position, RunAsAdmin = RunAsAdmin, AllUsers = AllUsers, IsGroup = IsGroup && TargetGroup == null,
                Scope = TargetGroup?.Model.Scope ?? ResolveScope(),
            };
            if (def.IsGroup)
                def.Children = Children.Where(c => c.Title.Trim().Length > 0).Select(c => new CustomEntryDefinition
                {
                    Title = c.Title.Trim(), Program = c.Program, Arguments = c.Arguments, Icon = c.Icon, RunAsAdmin = RunAsAdmin,
                }).ToList();
        }
        catch (ArgumentException ex) { Dialogs.Error(ex.Message); return; }

        var group = TargetGroup;
        var needsAdmin = group != null ? group.Model.SubmenuHive == Core.Registry.RegHive.LocalMachine : AllUsers;
        var ok = _main.Apply(() => group != null ? _main.Custom.AddToGroup(group.Model, def) : _main.Custom.Create(def), needsAdmin, null);
        if (!ok) return;
        Title = Program = Icon = "";
        Arguments = DefaultArgs;
        Children.Clear();
        IsGroup = false;
        TargetGroup = null;
        _ = _main.RefreshAsync();
    }
}

public sealed class JournalPageViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    public JournalPageViewModel(MainViewModel main)
    {
        _main = main;
        UndoCommand = new RelayCommand(p => { if (p is ChangeSetViewModel s) Undo(s); }, p => p is ChangeSetViewModel s && s.Model == _main.Editor.Journal.LastActive);
        OpenBackupCommand = new RelayCommand(p => { if (p is ChangeSetViewModel { BackupFile: { } f }) SystemHelper.SelectFile(f); });
        OpenBackupFolderCommand = new RelayCommand(() => SystemHelper.OpenFolder(_main.Paths.BackupDir));
    }

    public ObservableCollection<ChangeSetViewModel> Sets { get; } = new();
    public bool IsEmpty => Sets.Count == 0;
    public ICommand UndoCommand { get; }
    public ICommand OpenBackupCommand { get; }
    public ICommand OpenBackupFolderCommand { get; }

    public void Reload()
    {
        Sets.Clear();
        foreach (var s in _main.Editor.Journal.Sets.AsEnumerable().Reverse()) Sets.Add(new ChangeSetViewModel(s));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private bool TouchesHklm(IEnumerable<Core.Editing.ChangeSet> sets) =>
        sets.SelectMany(s => s.Ops).Any(o => o.Hive == Core.Registry.RegHive.LocalMachine);

    private void Undo(ChangeSetViewModel s)
    {
        if (TouchesHklm(new[] { s.Model }) && !_main.IsAdmin)
        {
            if (Dialogs.Confirm(Loc.Get("Ask_Elevate"))) _main.RelaunchAdmin();
            return;
        }
        _main.Apply(() => { _main.Editor.Undo(s.Model); return null; }, false, null);
        AfterUndo();
    }

    public void UndoLast()
    {
        var last = _main.Editor.Journal.LastActive;
        if (last == null) return;
        if (!Dialogs.Confirm(Loc.Format("Ask_UndoLast", last.Description))) return;
        Undo(new ChangeSetViewModel(last));
    }

    public void RestoreAll()
    {
        var active = _main.Editor.Journal.Active.ToList();
        if (active.Count == 0) return;
        if (!Dialogs.Warn(Loc.Format("Ask_RestoreAll", active.Count))) return;
        if (TouchesHklm(active) && !_main.IsAdmin)
        {
            if (Dialogs.Confirm(Loc.Get("Ask_Elevate"))) _main.RelaunchAdmin();
            return;
        }
        _main.Apply(() => { _main.Editor.UndoAll(); return null; }, false, null);
        AfterUndo();
    }

    private void AfterUndo()
    {
        _main.RestartSuggested = true;
        Reload();
        _ = _main.RefreshAsync();
    }
}

public sealed class SettingsPageViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    public SettingsPageViewModel(MainViewModel main) => _main = main;

    public List<AppTheme> Themes { get; } = Enum.GetValues<AppTheme>().ToList();
    public List<string> Languages { get; } = Loc.Languages.ToList();
    public List<HandlerDisableMethod> Methods { get; } = Enum.GetValues<HandlerDisableMethod>().ToList();

    public AppTheme Theme
    {
        get => _main.Settings.Theme;
        set { _main.Settings.Theme = value; _main.SaveSettings(); App.ApplyTheme(value); OnPropertyChanged(); }
    }

    public string Language
    {
        get => _main.Settings.Language;
        set { _main.Settings.Language = value; _main.SaveSettings(); Loc.Instance.Language = value; OnPropertyChanged(); }
    }

    public HandlerDisableMethod HandlerMethod
    {
        get => _main.Settings.HandlerMethod;
        set { _main.Settings.HandlerMethod = value; _main.SaveSettings(); OnPropertyChanged(); }
    }

    public bool ConfirmCritical
    {
        get => _main.Settings.ConfirmCritical;
        set { _main.Settings.ConfirmCritical = value; _main.SaveSettings(); OnPropertyChanged(); }
    }

    public string About => Loc.Format("About_Text", typeof(App).Assembly.GetName().Version?.ToString(3), SystemHelper.OsDescription,
        Loc.Get(_main.IsAdmin ? "About_Admin" : "About_User"), _main.Paths.BaseDir);
}

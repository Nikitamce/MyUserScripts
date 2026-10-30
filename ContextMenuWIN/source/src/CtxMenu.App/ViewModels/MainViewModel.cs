using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using CtxMenu.App.Platform;
using CtxMenu.Core.Editing;
using CtxMenu.Core.Localization;
using CtxMenu.Core.Model;
using CtxMenu.Core.Registry;
using CtxMenu.Core.Scanning;
using CtxMenu.Core.Services;

namespace CtxMenu.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly DispatcherTimer _searchTimer;
    private NavItem? _selectedNav;
    private Page _page = Page.Entries;
    private EntryViewModel? _selected;
    private string _search = "";
    private string _extFilter = "";
    private HashSet<string>? _extScopes;
    private bool _thirdPartyOnly, _disabledOnly, _shiftOnly, _busy, _restartSuggested;
    private string _status = "";
    private int _visibleCount;

    public MainViewModel(IRegistry registry, AppPaths paths)
    {
        Paths = paths;
        Settings = AppSettings.Load(paths.SettingsFile);
        Loc.Instance.Language = Settings.Language;
        Registry = registry;
        Editor = new RegistryEditor(registry, new ChangeJournal(paths.JournalFile), paths.BackupDir);
        var strings = new WindowsStringResolver();
        Scanner = new ContextMenuScanner(registry, new WindowsFileInfo(), strings);
        Actions = new ContextMenuActions(Editor);
        ShellNew = new ShellNewService(Editor, strings, paths.TemplatesDir);
        Archivers = new ArchiverService(Editor);
        Win11 = new Win11Service(Editor);
        Custom = new CustomEntryService(Editor, paths.CustomFile);
        Profiles = new ProfileService(Editor, Actions, Custom);
        IsAdmin = SystemHelper.IsAdmin;

        Entries = new BulkObservableCollection<EntryViewModel>();
        EntriesView = CreateView();
        Nav = BuildNav();
        _selectedNav = Nav.First(n => n.IsAll);

        ShellNewPage = new ShellNewPageViewModel(this);
        ArchiversPage = new ArchiversPageViewModel(this);
        Win11Page = new Win11PageViewModel(this);
        CustomPage = new CustomPageViewModel(this);
        JournalPage = new JournalPageViewModel(this);
        SettingsPage = new SettingsPageViewModel(this);
        PreviewPage = new PreviewPageViewModel(this);

        _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _searchTimer.Tick += (_, _) => { _searchTimer.Stop(); ApplyFilter(); };

        RefreshCommand = new RelayCommand(async () => await RefreshAsync(), () => !IsBusy);
        RestartExplorerCommand = new RelayCommand(RestartExplorer);
        UndoLastCommand = new RelayCommand(() => JournalPage.UndoLast(), () => Editor.Journal.LastActive != null);
        RestoreAllCommand = new RelayCommand(() => JournalPage.RestoreAll(), () => Editor.Journal.LastActive != null);
        ExportProfileCommand = new RelayCommand(ExportProfile);
        ImportProfileCommand = new RelayCommand(ImportProfile);
        ElevateCommand = new RelayCommand(() => RelaunchAdmin(), () => !IsAdmin);
        ToggleThemeCommand = new RelayCommand(() => SettingsPage.Theme = App.IsDarkActive ? AppTheme.Light : AppTheme.Dark);
        ToggleLanguageCommand = new RelayCommand(() => SettingsPage.Language = Loc.Instance.Language == "ru" ? "en" : "ru");
        CopyPathCommand = new RelayCommand(p => { if (p is string s && s.Length > 0) Clipboard.SetText(s); Status = Loc.Get("Status_Copied"); });
        OpenRegeditCommand = new RelayCommand(OpenRegedit, _ => Selected is { IsPackaged: false });
        ExportRegCommand = new RelayCommand(ExportReg, _ => Selected is { IsPackaged: false });
        DeleteCustomCommand = new RelayCommand(_ => DeleteCustom(Selected), _ => Selected?.IsCustom == true);
        SelectEntryCommand = new RelayCommand(p => { if (p is EntryViewModel e) Selected = e; });
        GoToArchiversCommand = new RelayCommand(() => SelectPage(Page.Archivers));
        AddToGroupCommand = new RelayCommand(() => { if (Selected != null) { CustomPage.TargetGroup = Selected; SelectPage(Page.Custom); } }, () => Selected?.CanAddToGroup == true);
        DisableVisibleCommand = new RelayCommand(() => BulkToggle(false));
        EnableVisibleCommand = new RelayCommand(() => BulkToggle(true));
        ClearFiltersCommand = new RelayCommand(() => { SearchText = ""; ExtensionFilter = ""; ThirdPartyOnly = DisabledOnly = ShiftOnly = false; });
        OpenDataFolderCommand = new RelayCommand(() => SystemHelper.OpenFolder(Paths.BaseDir));
        SelectedPropertyChanged();

        Loc.LanguageChanged += OnLanguageChanged;
    }

    // ---------- services ----------
    public AppPaths Paths { get; }
    public AppSettings Settings { get; }
    public IRegistry Registry { get; }
    public RegistryEditor Editor { get; }
    public ContextMenuScanner Scanner { get; }
    public ContextMenuActions Actions { get; }
    public ShellNewService ShellNew { get; }
    public ArchiverService Archivers { get; }
    public Win11Service Win11 { get; }
    public CustomEntryService Custom { get; }
    public ProfileService Profiles { get; }
    public bool IsAdmin { get; }
    public string WindowTitle => Loc.Get("App_Title") + (IsAdmin ? " · " + Loc.Get("App_Admin") : "");

    // ---------- pages ----------
    public ShellNewPageViewModel ShellNewPage { get; }
    public ArchiversPageViewModel ArchiversPage { get; }
    public Win11PageViewModel Win11Page { get; }
    public CustomPageViewModel CustomPage { get; }
    public JournalPageViewModel JournalPage { get; }
    public SettingsPageViewModel SettingsPage { get; }
    public PreviewPageViewModel PreviewPage { get; }

    public ObservableCollection<NavItem> Nav { get; }
    public BulkObservableCollection<EntryViewModel> Entries { get; }
    public ListCollectionView EntriesView { get; }

    public NavItem? SelectedNav
    {
        get => _selectedNav;
        set
        {
            if (value == null || value.IsHeader) { Later(() => OnPropertyChanged()); return; }
            if (!Set(ref _selectedNav, value)) return;
            CurrentPage = value.Page;
            if (value.Page == Page.Entries) ApplyFilter();
            if (value.Page == Page.Preview) PreviewPage.Rebuild();
        }
    }

    public Page CurrentPage { get => _page; private set => Set(ref _page, value); }

    public void SelectPage(Page page) => SelectedNav = Nav.First(n => n.Page == page && !n.IsHeader && (page != Page.Entries || n.IsAll));

    public EntryViewModel? Selected
    {
        get => _selected;
        set { if (Set(ref _selected, value)) SelectedPropertyChanged(); }
    }

    private void SelectedPropertyChanged() => OnPropertyChanged(nameof(HasSelection));
    public bool HasSelection => _selected != null;

    public string SearchText { get => _search; set { if (Set(ref _search, value)) Debounce(); } }

    public string ExtensionFilter
    {
        get => _extFilter;
        set
        {
            if (!Set(ref _extFilter, value)) return;
            var ext = value.Trim();
            _extScopes = ext.Length > 0 && ScopeCatalog.IsValidExtension(ScopeCatalog.NormalizeExtension(ext)) ? Scanner.GetScopesForExtension(ext) : null;
            OnPropertyChanged(nameof(ExtensionFilterHint));
            Debounce();
        }
    }

    public string? ExtensionFilterHint => _extScopes == null ? null : Loc.Format("Filter_ExtHint", ScopeCatalog.NormalizeExtension(_extFilter));

    public bool ThirdPartyOnly { get => _thirdPartyOnly; set { if (Set(ref _thirdPartyOnly, value)) ApplyFilter(); } }
    public bool DisabledOnly { get => _disabledOnly; set { if (Set(ref _disabledOnly, value)) ApplyFilter(); } }
    public bool ShiftOnly { get => _shiftOnly; set { if (Set(ref _shiftOnly, value)) ApplyFilter(); } }
    public bool IsBusy { get => _busy; private set { if (Set(ref _busy, value)) CommandManager.InvalidateRequerySuggested(); } }
    public string Status { get => _status; set => Set(ref _status, value); }
    public bool RestartSuggested { get => _restartSuggested; set => Set(ref _restartSuggested, value); }
    public int VisibleCount { get => _visibleCount; private set => Set(ref _visibleCount, value); }
    public string CurrentTitle => SelectedNav?.Title ?? "";

    // ---------- commands ----------
    public ICommand RefreshCommand { get; }
    public ICommand RestartExplorerCommand { get; }
    public ICommand UndoLastCommand { get; }
    public ICommand RestoreAllCommand { get; }
    public ICommand ExportProfileCommand { get; }
    public ICommand ImportProfileCommand { get; }
    public ICommand ElevateCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public ICommand ToggleLanguageCommand { get; }
    public ICommand CopyPathCommand { get; }
    public ICommand OpenRegeditCommand { get; }
    public ICommand ExportRegCommand { get; }
    public ICommand DeleteCustomCommand { get; }
    public ICommand SelectEntryCommand { get; }
    public ICommand GoToArchiversCommand { get; }
    public ICommand AddToGroupCommand { get; }
    public ICommand DisableVisibleCommand { get; }
    public ICommand EnableVisibleCommand { get; }
    public ICommand ClearFiltersCommand { get; }
    public ICommand OpenDataFolderCommand { get; }

    // ---------- navigation ----------
    private static ObservableCollection<NavItem> BuildNav()
    {
        var nav = new ObservableCollection<NavItem>
        {
            new() { Page = Page.Entries, IsHeader = true, Glyph = "", LocKey = "Nav_HeaderMenu" },
            new() { Page = Page.Entries, IsAll = true, Glyph = "\uE71D", LocKey = "Nav_All" },
            new() { Page = Page.Entries, Glyph = "\uE7EE", LocKey = "Nav_ThirdParty", Category = (MenuCategory)(-1) },
            new() { Page = Page.Entries, Glyph = "\uE7BA", LocKey = "Nav_Disabled", Category = (MenuCategory)(-2) },
            new() { Page = Page.Entries, IsHeader = true, Glyph = "", LocKey = "Nav_HeaderWhere" },
        };
        foreach (var c in Enum.GetValues<MenuCategory>())
            nav.Add(new NavItem { Page = Page.Entries, Category = c, Glyph = Glyphs.For(c), LocKey = "Cat_" + c });
        nav.Add(new NavItem { Page = Page.Entries, IsHeader = true, Glyph = "", LocKey = "Nav_HeaderTools" });
        nav.Add(new NavItem { Page = Page.Preview, Glyph = "\uE7B3", LocKey = "Nav_Preview" });
        nav.Add(new NavItem { Page = Page.ShellNew, Glyph = "\uE710", LocKey = "Nav_ShellNew" });
        nav.Add(new NavItem { Page = Page.Archivers, Glyph = "\uE7B8", LocKey = "Nav_Archivers" });
        nav.Add(new NavItem { Page = Page.Win11, Glyph = "\uE782", LocKey = "Nav_Win11" });
        nav.Add(new NavItem { Page = Page.Custom, Glyph = "\uE70F", LocKey = "Nav_Custom" });
        nav.Add(new NavItem { Page = Page.Journal, Glyph = "\uE81C", LocKey = "Nav_Journal" });
        nav.Add(new NavItem { Page = Page.Settings, Glyph = "\uE713", LocKey = "Nav_Settings" });
        return nav;
    }

    private ListCollectionView CreateView()
    {
        var view = (ListCollectionView)CollectionViewSource.GetDefaultView(Entries);
        view.CustomSort = new EntryComparer();
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(EntryViewModel.ScopeDisplay)));
        view.Filter = o => Matches((EntryViewModel)o);
        return view;
    }

    private sealed class EntryComparer : IComparer
    {
        public int Compare(object? x, object? y)
        {
            var a = (EntryViewModel)x!;
            var b = (EntryViewModel)y!;
            var c = a.Model.Category.CompareTo(b.Model.Category);
            if (c != 0) return c;
            c = string.Compare(a.ScopeDisplay, b.ScopeDisplay, StringComparison.CurrentCultureIgnoreCase);
            return c != 0 ? c : string.Compare(a.DisplayName, b.DisplayName, StringComparison.CurrentCultureIgnoreCase);
        }
    }

    private bool Matches(EntryViewModel e)
    {
        var nav = SelectedNav;
        if (_extScopes != null)
        {
            if (!_extScopes.Contains(e.Model.Scope) && !(e.IsPackaged && PackagedMatchesExt(e.Model.Scope))) return false;
        }
        else if (nav != null && !nav.IsAll && nav.Category is { } cat)
        {
            if ((int)cat == -1) { if (!e.IsThirdParty) return false; }
            else if ((int)cat == -2) { if (e.Model.SelfAndDescendants().All(x => x.IsEnabled)) return false; }
            else if (e.Model.Category != cat) return false;
        }
        if (_thirdPartyOnly && !e.IsThirdParty) return false;
        if (_disabledOnly && e.IsEnabled) return false;
        if (_shiftOnly && !e.IsExtended) return false;
        if (_search.Length > 0)
        {
            var hit = e.SearchText.Contains(_search, StringComparison.CurrentCultureIgnoreCase)
                      || e.Children.Any(c => c.SearchText.Contains(_search, StringComparison.CurrentCultureIgnoreCase));
            if (!hit) return false;
        }
        return true;
    }

    private bool PackagedMatchesExt(string itemTypes) =>
        itemTypes.Split(", ").Any(t => t == "*" || _extScopes!.Contains(t));

    private void Debounce() { _searchTimer.Stop(); _searchTimer.Start(); }

    public void ApplyFilter()
    {
        EntriesView.Refresh();
        VisibleCount = EntriesView.Count;
        OnPropertyChanged(nameof(CurrentTitle));
    }

    private void UpdateCounts()
    {
        foreach (var n in Nav.Where(n => n.Page == Page.Entries && !n.IsHeader))
        {
            n.Count = n.IsAll ? Entries.Count
                : (int)n.Category! == -1 ? Entries.Count(e => e.IsThirdParty)
                : (int)n.Category! == -2 ? Entries.Count(e => e.Model.SelfAndDescendants().Any(x => !x.IsEnabled))
                : Entries.Count(e => e.Model.Category == n.Category);
        }
        Nav.First(n => n.Page == Page.ShellNew).Count = ShellNewPage.Items.Count;
        Nav.First(n => n.Page == Page.Custom).Count = Entries.Count(e => e.IsCustom);
        Nav.First(n => n.Page == Page.Journal).Count = Editor.Journal.Active.Count();
    }

    // ---------- loading ----------
    public async Task RefreshAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Status = Loc.Get("Status_Scanning");
        var selectedId = Selected?.Model.StableId;
        try
        {
            var win11 = SystemHelper.IsWindows11;
            var data = await Task.Run(() =>
            {
                var entries = Scanner.Scan();
                string? packagedError = null;
                var packaged = new List<ContextMenuEntry>();
                if (win11)
                {
                    try { packaged = PackageScanner.Scan(Registry, CancellationToken.None); }
                    catch (Exception ex) { packagedError = ex.Message; }
                }
                return (entries, packaged, packagedError, shellNew: ShellNew.Scan(), archivers: Archivers.Detect());
            });

            Selected = null;
            Entries.ReplaceAll(data.entries.Concat(data.packaged).Select(e => new EntryViewModel(e, this)).ToList());
            ShellNewPage.Load(data.shellNew);
            ArchiversPage.Load(data.archivers);
            Win11Page.Load(data.packagedError);
            CustomPage.LoadExisting();
            JournalPage.Reload();
            if (ExtensionFilter.Length > 0) _extScopes = Scanner.GetScopesForExtension(ExtensionFilter);
            ApplyFilter();
            UpdateCounts();
            PreviewPage.Rebuild();
            Selected = selectedId == null ? null : Entries.FirstOrDefault(e => e.Model.StableId == selectedId);
            Status = Loc.Format("Status_Scanned", Entries.Count, data.entries.Count(e => e.IsThirdParty), data.entries.Count(e => !e.IsEnabled));
        }
        catch (Exception ex)
        {
            Status = Loc.Get("Status_ScanFailed");
            Dialogs.Error(Loc.Get("Status_ScanFailed") + "\n\n" + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ---------- changes ----------
    public void Later(Action a) => Application.Current?.Dispatcher.BeginInvoke(a, DispatcherPriority.Background);

    public void Toggle(EntryViewModel vm, bool enable)
    {
        if (!enable && vm.IsCritical && Settings.ConfirmCritical && !Dialogs.Warn(Loc.Format("Warn_Critical", vm.DisplayName, vm.CriticalReason)))
        {
            Later(vm.RaiseAll);
            return;
        }
        var needsAdmin = ContextMenuActions.TargetHive(vm.Model, enable, Settings.HandlerMethod) == RegHive.LocalMachine;
        Apply(() => Actions.SetEnabled(vm.Model, enable, Settings.HandlerMethod), needsAdmin, vm, after: () =>
        {
            vm.Parent?.RaiseAll();
            ArchiversPage.RaiseHandlers();
        });
    }

    /// <summary>Runs a registry change with elevation handling, error reporting and UI refresh.</summary>
    public bool Apply(Func<ChangeSet?> action, bool needsAdmin, ObservableObject? refresh, Action? after = null)
    {
        try
        {
            if (needsAdmin && !IsAdmin)
            {
                if (Dialogs.Confirm(Loc.Get("Ask_Elevate"))) RelaunchAdmin();
                return false;
            }
            var set = action();
            if (set != null)
            {
                RestartSuggested = true;
                Status = Loc.Format("Status_Done", set.Description);
                JournalPage.Reload();
            }
            after?.Invoke();
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            if (Dialogs.Confirm(Loc.Get("Ask_Elevate"))) RelaunchAdmin();
            return false;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or IOException or FormatException)
        {
            Dialogs.Error(ex.Message);
            return false;
        }
        finally
        {
            if (refresh != null) Later(refresh.RaiseAll);
            Later(UpdateCounts);
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private void BulkToggle(bool enable)
    {
        var targets = EntriesView.Cast<EntryViewModel>()
            .Where(e => e.IsEnabled != enable && (enable || !e.IsCritical)).Select(e => e.Model).ToList();
        if (targets.Count == 0) { Status = Loc.Get("Status_NothingToDo"); return; }
        if (!Dialogs.Confirm(Loc.Format(enable ? "Ask_EnableVisible" : "Ask_DisableVisible", targets.Count))) return;
        var needsAdmin = targets.Any(t => ContextMenuActions.TargetHive(t, enable, Settings.HandlerMethod) == RegHive.LocalMachine);
        if (needsAdmin && !IsAdmin)
        {
            var userOnly = targets.Where(t => ContextMenuActions.TargetHive(t, enable, Settings.HandlerMethod) == RegHive.CurrentUser).ToList();
            if (Dialogs.Confirm(Loc.Format("Ask_ElevateBulk", targets.Count - userOnly.Count))) { RelaunchAdmin(); return; }
            targets = userOnly;
            if (targets.Count == 0) return;
        }
        Apply(() => Actions.SetEnabledMany(targets, enable, Settings.HandlerMethod, Loc.Format(enable ? "Journal_BulkOn" : "Journal_BulkOff", targets.Count)), false, null,
            after: () => { foreach (var e in Entries) e.RaiseAll(); ApplyFilter(); });
    }

    public void RelaunchAdmin()
    {
        if (SystemHelper.RelaunchElevated("--page " + CurrentPage)) Application.Current.Shutdown();
    }

    private void RestartExplorer()
    {
        if (!Dialogs.Confirm(Loc.Get("Ask_RestartExplorer"))) return;
        Task.Run(SystemHelper.RestartExplorer).ContinueWith(_ => Later(() => { RestartSuggested = false; Status = Loc.Get("Status_ExplorerRestarted"); }));
    }

    private void OpenRegedit(object? _)
    {
        if (Selected == null) return;
        try { SystemHelper.OpenInRegedit(Selected.Model.Hive, Selected.Model.KeyPath); }
        catch (Exception ex) { Dialogs.Error(ex.Message); }
    }

    private void ExportReg(object? _)
    {
        if (Selected == null) return;
        var file = Dialogs.SaveFile("Registry (*.reg)|*.reg", Sanitize(Selected.DisplayName) + ".reg");
        if (file == null) return;
        RegFileExporter.WriteFile(file, Actions.ExportReg(Selected.Model));
        Status = Loc.Format("Status_Exported", file);
    }

    public void DeleteCustom(EntryViewModel? e)
    {
        if (e == null || !e.IsCustom) return;
        if (!Dialogs.Confirm(Loc.Format("Ask_DeleteCustom", e.DisplayName))) return;
        if (Apply(() => Custom.Delete(e.Model), e.NeedsAdmin, null))
        {
            Entries.Remove(e);
            if (Selected == e) Selected = null;
            CustomPage.LoadExisting();
        }
    }

    private void ExportProfile()
    {
        var file = Dialogs.SaveFile("JSON (*.json)|*.json", $"context-menu-profile-{DateTime.Now:yyyyMMdd}.json");
        if (file == null) return;
        File.WriteAllText(file, Profiles.Export(Entries.Select(e => e.Model)));
        Status = Loc.Format("Status_Exported", file);
    }

    private async void ImportProfile()
    {
        var file = Dialogs.OpenFile("JSON (*.json)|*.json");
        if (file == null) return;
        MenuProfile profile;
        try { profile = ProfileService.Parse(File.ReadAllText(file)); }
        catch (Exception ex) { Dialogs.Error(Loc.Get("Err_BadProfile") + "\n" + ex.Message); return; }
        if (!Dialogs.Confirm(Loc.Format("Ask_ImportProfile", profile.Machine, profile.Created.LocalDateTime, profile.Disabled.Count, profile.Custom.Count))) return;
        ProfileImportResult? result = null;
        Apply(() => { result = Profiles.Apply(profile, Entries.Select(e => e.Model), Settings.HandlerMethod, exact: false); return null; }, false, null);
        await RefreshAsync();
        if (result != null)
        {
            RestartSuggested = true;
            Dialogs.Info(Loc.Format("Info_ProfileApplied", result.Disabled, result.Enabled, result.ShiftChanged, result.CustomCreated, result.NotFound));
        }
    }

    private void OnLanguageChanged()
    {
        foreach (var n in Nav) n.RaiseAll();
        foreach (var e in Entries) { e.RaiseAll(); foreach (var c in e.Children) c.RaiseAll(); }
        ShellNewPage.RaiseAll();
        foreach (var i in ShellNewPage.Items) i.RaiseAll();
        ArchiversPage.Load(ArchiversPage.Archivers.Select(a => a.Info).ToList());
        CustomPage.RaiseAll();
        CustomPage.RebuildScopes();
        JournalPage.Reload();
        Win11Page.RaiseAll();
        PreviewPage.OnLanguageChanged();
        RaiseAll();
        ApplyFilter();
    }

    public static string Sanitize(string s)
    {
        var bad = Path.GetInvalidFileNameChars();
        var r = new string(s.Select(c => bad.Contains(c) ? '_' : c).ToArray()).Trim();
        return r.Length == 0 ? "entry" : r;
    }

    public void SaveSettings() => Settings.Save(Paths.SettingsFile);
}

namespace CtxMenu.Core.Model;

public sealed class AppPaths
{
    public AppPaths(string baseDir)
    {
        BaseDir = baseDir;
        Directory.CreateDirectory(baseDir);
    }

    public string BaseDir { get; }
    public string JournalFile => Path.Combine(BaseDir, "journal.json");
    public string SettingsFile => Path.Combine(BaseDir, "settings.json");
    public string CustomFile => Path.Combine(BaseDir, "custom-entries.json");
    public string BackupDir => Path.Combine(BaseDir, "Backups");
    public string TemplatesDir => Path.Combine(BaseDir, "Templates");

    public static AppPaths Default() =>
        new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ContextMenuManager"));
}

public enum AppTheme { System, Light, Dark }

public sealed class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.System;
    public string Language { get; set; } = "ru";
    public HandlerDisableMethod HandlerMethod { get; set; } = HandlerDisableMethod.RenameKey;
    public bool ConfirmCritical { get; set; } = true;
    public bool ShowRegistryNames { get; set; }

    public static AppSettings Load(string file)
    {
        try
        {
            if (File.Exists(file))
                return Editing.ChangeJournal.Deserialize<AppSettings>(File.ReadAllText(file)) ?? new AppSettings();
        }
        catch (Exception) { /* corrupted settings: fall back to defaults */ }
        return new AppSettings();
    }

    public void Save(string file)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, Editing.ChangeJournal.Serialize(this));
    }
}

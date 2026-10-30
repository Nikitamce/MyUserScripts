namespace CtxMenu.Core.Localization;

/// <summary>All UI strings: key → (Russian, English).</summary>
internal static class Strings
{
    public static readonly Dictionary<string, (string ru, string en)> Table = Build();

    private static Dictionary<string, (string ru, string en)> Build()
    {
        var d = new Dictionary<string, (string, string)>(StringComparer.Ordinal);
        void A(string k, string ru, string en) => d.Add(k, (ru, en));

        // ---- app / dialogs ----
        A("App_Title", "Менеджер контекстного меню", "Context Menu Manager");
        A("App_Admin", "Администратор", "Administrator");
        A("Dlg_Warning", "Внимание", "Warning");
        A("Dlg_Error", "Ошибка", "Error");
        A("Lang_Short", "RU", "EN");
        A("Lang_ru", "Русский", "Russian");
        A("Lang_en", "English", "English");
        A("Theme_System", "Как в системе", "System");
        A("Theme_Light", "Светлая", "Light");
        A("Theme_Dark", "Тёмная", "Dark");
        A("Method_RenameKey", "Переименовать ключ (\"-имя\")", "Rename key (\"-name\")");
        A("Method_BlockClsid", "Заблокировать CLSID (Blocked)", "Block CLSID (Blocked list)");
        A("MenuPos_Default", "Обычная", "Default");
        A("MenuPos_Top", "Вверху меню", "Top of menu");
        A("MenuPos_Bottom", "Внизу меню", "Bottom of menu");

        // ---- scopes ----
        A("Scope_DesktopBackground", "Пустое место на рабочем столе", "Desktop background");
        A("Scope_DirectoryBackground", "Пустое место в папке", "Folder background");
        A("Scope_LibraryBackground", "Пустое место в библиотеке", "Library background");
        A("Scope_LibraryFolder", "Библиотеки", "Libraries");
        A("Scope_Directory", "Папки (файловые)", "Directories");
        A("Scope_Folder", "Все папки и оболочки", "All folders and shell folders");
        A("Scope_Drive", "Диски", "Drives");
        A("Scope_AllFiles", "Все файлы (*)", "All files (*)");
        A("Scope_AllFilesystemObjects", "Все файлы и папки", "All files and folders");
        A("Scope_Unknown", "Файлы без программы", "Unassociated files");
        A("Scope_SfaImage", "Все изображения", "All images");
        A("Scope_SfaVideo", "Всё видео", "All videos");
        A("Scope_SfaAudio", "Всё аудио", "All audio");
        A("Scope_SfaText", "Все текстовые файлы", "All text files");
        A("Scope_SfaDocument", "Все документы", "All documents");
        A("Scope_SfaCompressed", "Все архивы", "All archives");
        A("Scope_RecycleBin", "Корзина", "Recycle Bin");
        A("Scope_ThisPC", "Этот компьютер", "This PC");
        A("Scope_Network", "Сеть", "Network");
        A("Scope_NetServer", "Сетевые компьютеры", "Network computers");
        A("Scope_NetShare", "Сетевые папки", "Network shares");
        A("Scope_Printers", "Принтеры", "Printers");
        A("Scope_Packaged", "Новое меню Windows 11: {0}", "Windows 11 modern menu: {0}");
        A("Scope_SfaExt", "Файлы {0} (для любой программы)", "{0} files (any program)");
        A("Scope_Ext", "Файлы {0}", "{0} files");
        A("Scope_Clsid", "Объект оболочки {0}", "Shell object {0}");
        A("Scope_ProgId", "Тип файла {0} ({1})", "File type {0} ({1})");

        // ---- categories ----
        A("Cat_Desktop", "Рабочий стол", "Desktop");
        A("Cat_Background", "Пустое место в папке", "Folder background");
        A("Cat_Folder", "Папки", "Folders");
        A("Cat_Drive", "Диски", "Drives");
        A("Cat_AllFiles", "Любые файлы", "Any file");
        A("Cat_AllObjects", "Файлы и папки", "Files and folders");
        A("Cat_Images", "Изображения", "Images");
        A("Cat_Video", "Видео", "Video");
        A("Cat_Audio", "Аудио", "Audio");
        A("Cat_Documents", "Документы и текст", "Documents and text");
        A("Cat_Archives", "Архивы", "Archives");
        A("Cat_Executables", "Программы и скрипты", "Programs and scripts");
        A("Cat_OtherFileTypes", "Другие типы файлов", "Other file types");
        A("Cat_RecycleBin", "Корзина", "Recycle Bin");
        A("Cat_ThisPC", "Этот компьютер", "This PC");
        A("Cat_Library", "Библиотеки", "Libraries");
        A("Cat_Network", "Сеть", "Network");
        A("Cat_Printers", "Принтеры", "Printers");
        A("Cat_Other", "Прочее", "Other");

        // ---- kinds / states / submenus ----
        A("Kind_Verb", "Команда (shell)", "Command (shell verb)");
        A("Kind_ShellExtension", "Расширение оболочки (shellex)", "Shell extension (shellex)");
        A("Kind_Packaged", "Пакет MSIX/Store", "Packaged (MSIX/Store)");
        A("State_Enabled", "Включено", "Enabled");
        A("State_LegacyDisable", "Отключено", "Disabled");
        A("State_ProgrammaticOnly", "Скрыто из меню", "Hidden from menu");
        A("State_RenamedKey", "Отключено", "Disabled");
        A("State_BlockedCurrentUser", "Заблокировано", "Blocked");
        A("State_BlockedLocalMachine", "Заблокировано (для всех)", "Blocked (all users)");
        A("State_On", "вкл.", "on");
        A("State_Off", "выкл.", "off");
        A("Submenu_None", "Нет подменю", "No submenu");
        A("Submenu_ShellSubkey", "Подменю (вложенные команды)", "Submenu (nested commands)");
        A("Submenu_ExtendedSubCommandsKey", "Подменю (ExtendedSubCommandsKey)", "Submenu (ExtendedSubCommandsKey)");
        A("Submenu_SubCommands", "Подменю (системные команды)", "Submenu (CommandStore)");
        A("Pos_Default", "Обычная", "Default");
        A("Src_Windows", "Windows", "Windows");
        A("Packaged_PathHint", "Пакетное приложение: блокируется через Shell Extensions\\Blocked", "Packaged app: blocked via Shell Extensions\\Blocked");

        // ---- well-known verbs ----
        A("Verb_open", "Открыть", "Open");
        A("Verb_opennew", "Открыть в новом окне", "Open in new window");
        A("Verb_openas", "Открыть с помощью", "Open with");
        A("Verb_edit", "Изменить", "Edit");
        A("Verb_print", "Печать", "Print");
        A("Verb_printto", "Печать на…", "Print to…");
        A("Verb_runas", "Запуск от имени администратора", "Run as administrator");
        A("Verb_runasuser", "Запуск от имени другого пользователя", "Run as different user");
        A("Verb_explore", "Проводник", "Explore");
        A("Verb_find", "Поиск", "Search");
        A("Verb_cmd", "Открыть окно команд здесь", "Open command window here");
        A("Verb_powershell", "Открыть PowerShell здесь", "Open PowerShell here");
        A("Verb_play", "Воспроизвести", "Play");
        A("Verb_preview", "Просмотр", "Preview");
        A("Verb_pintohome", "Закрепить на панели быстрого доступа", "Pin to Quick access");
        A("Verb_opennewwindow", "Открыть в новом окне", "Open in new window");
        A("Verb_opennewprocess", "Открыть в новом процессе", "Open in new process");
        A("Verb_opennewtab", "Открыть в новой вкладке", "Open in new tab");
        A("Verb_wallpaper", "Сделать фоновым изображением", "Set as desktop background");
        A("Verb_rotateleft", "Повернуть влево", "Rotate left");
        A("Verb_rotateright", "Повернуть вправо", "Rotate right");
        A("Verb_enqueue", "Добавить в список воспроизведения", "Add to play queue");
        A("Verb_mount", "Подключить", "Mount");
        A("Verb_burn", "Записать образ диска", "Burn disc image");
        A("Verb_install", "Установить", "Install");
        A("Verb_uninstall", "Удалить", "Uninstall");
        A("Verb_format", "Форматировать", "Format");
        A("Verb_properties", "Свойства", "Properties");
        A("Verb_emptybin", "Очистить корзину", "Empty Recycle Bin");
        A("Verb_wsl", "Открыть оболочку Linux здесь", "Open Linux shell here");
        A("Verb_anycode", "Открыть с помощью Code", "Open with Code");

        // ---- critical reasons ----
        A("Crit_DefaultVerb", "Это основная команда Windows (например «Открыть»). Если её отключить, двойной щелчок и запуск файлов могут перестать работать.",
            "This is a core Windows command (e.g. Open). Disabling it can break double-click and launching files.");
        A("Crit_New", "Это меню «Создать». Без него из контекстного меню нельзя будет создавать файлы и папки.",
            "This is the New menu. Without it you cannot create files and folders from the context menu.");
        A("Crit_OpenWith", "Это «Открыть с помощью». Без него нельзя будет выбрать программу для файла.",
            "This is Open with. Without it you cannot choose an app for a file.");
        A("Crit_SendTo", "Это меню «Отправить». Без него пропадут пункты отправки и сжатия в ZIP.",
            "This is Send to. Without it the Send to and Compressed folder items disappear.");
        A("Crit_Sharing", "Это пункты общего доступа Windows.", "These are Windows sharing items.");
        A("Crit_Previous", "Это «Восстановить прежнюю версию» (теневые копии и история файлов).",
            "This is Restore previous versions (shadow copies and File History).");
        A("Crit_Generic", "Системный пункт Windows. Отключайте, только если понимаете последствия.",
            "Built-in Windows item. Disable only if you understand the consequences.");

        // ---- journal ----
        A("Journal_Enable", "Включено: {0}", "Enabled: {0}");
        A("Journal_Disable", "Отключено: {0}", "Disabled: {0}");
        A("Journal_ShiftOn", "Только с Shift: {0}", "Shift only: {0}");
        A("Journal_ShiftOff", "Всегда видно (без Shift): {0}", "Always visible (no Shift): {0}");
        A("Journal_Programmatic", "Скрытие из меню: {0}", "Hide from menu: {0}");
        A("Journal_Position", "Позиция в меню: {0}", "Menu position: {0}");
        A("Journal_ClassicOn", "Windows 11: включено классическое меню", "Windows 11: classic menu on");
        A("Journal_ClassicOff", "Windows 11: возвращено новое меню", "Windows 11: modern menu restored");
        A("Journal_NewOn", "«Создать»: включено {0}", "New menu: enabled {0}");
        A("Journal_NewOff", "«Создать»: отключено {0}", "New menu: disabled {0}");
        A("Journal_NewAdd", "«Создать»: добавлено {0}", "New menu: added {0}");
        A("Journal_NewRemove", "«Создать»: удалено {0}", "New menu: removed {0}");
        A("Journal_Archiver", "{0}: {1} ({2})", "{0}: {1} ({2})");
        A("Journal_CustomAdd", "Добавлен свой пункт: {0}", "Custom item added: {0}");
        A("Journal_GroupAdd", "Добавлено «{0}» в группу «{1}»", "Added \"{0}\" to group \"{1}\"");
        A("Journal_CustomRemove", "Удалён свой пункт: {0}", "Custom item removed: {0}");
        A("Journal_ProfileDisable", "Профиль: отключение пунктов", "Profile: disabling items");
        A("Journal_ProfileEnable", "Профиль: включение пунктов", "Profile: enabling items");
        A("Journal_BulkOn", "Включено пунктов: {0}", "Enabled items: {0}");
        A("Journal_BulkOff", "Отключено пунктов: {0}", "Disabled items: {0}");
        A("Journal_Undone", "отменено", "undone");
        A("Journal_Active", "действует", "active");
        A("Journal_Desc", "Каждое изменение записывается сюда вместе с резервной копией .reg. Любое действие можно отменить.",
            "Every change is recorded here with a .reg backup. Any action can be undone.");
        A("Journal_Empty", "Изменений пока нет", "No changes yet");
        A("Journal_Ops", "операций:", "operations:");
        A("Journal_Details", "Подробности", "Details");

        // ---- errors ----
        A("Err_TargetExists", "Ключ уже существует: {0}", "Key already exists: {0}");
        A("Err_BadExtension", "Некорректное расширение: {0}", "Invalid extension: {0}");
        A("Err_ShellNewExists", "Для {0} пункт «Создать» уже есть", "A New menu item already exists for {0}");
        A("Err_NotCustom", "Удалять можно только пункты, созданные этой программой", "Only items created by this app can be deleted");
        A("Err_NoProgram", "Укажите программу", "Choose a program");
        A("Err_NoTitle", "Укажите название", "Enter a title");
        A("Err_NoScope", "Выберите, где показывать пункт", "Choose where to show the item");
        A("Err_EmptyGroup", "Добавьте в группу хотя бы одну команду", "Add at least one command to the group");
        A("Err_NotEditableGroup", "В это подменю нельзя добавлять команды", "Commands cannot be added to this submenu");
        A("Err_BadProfile", "Файл профиля повреждён или имеет неверный формат", "Profile file is damaged or has a wrong format");

        // ---- ShellNew ----
        A("ShellNew_Folder", "Папку", "Folder");
        A("ShellNew_FileSuffix", "файл", "file");
        A("NewMode_NullFile", "пустой файл", "empty file");
        A("NewMode_FileName", "из шаблона", "from template");
        A("NewMode_Data", "файл с данными", "file with data");
        A("NewMode_Command", "команда", "command");
        A("NewMode_Directory", "папка", "folder");
        A("NewMode_Handler", "обработчик", "handler");
        A("NewMode_Unknown", "неизвестно", "unknown");
        A("ShellNew_Desc", "Пункты подменю «Создать» в пустом месте папки и на рабочем столе. Отключайте лишнее или добавляйте свои типы файлов.",
            "Items of the New submenu on folder and desktop background. Turn off the clutter or add your own file types.");
        A("ShellNew_AddTitle", "Добавить свой тип в «Создать»", "Add a file type to New");
        A("ShellNew_AddHint", "Например: .md, .py, .json. Без шаблона создаётся пустой файл; с шаблоном новый файл будет его копией.",
            "For example: .md, .py, .json. Without a template an empty file is created; with a template the new file is a copy of it.");

        // ---- archivers ----
        A("Arc_Desc", "Отдельные пункты меню WinRAR и 7-Zip. Программа меняет настройки самих архиваторов, поэтому они остаются после обновлений.",
            "Individual WinRAR and 7-Zip menu items. The app changes the archivers' own settings, so they survive updates.");
        A("Arc_None", "WinRAR и 7-Zip не найдены.", "Neither WinRAR nor 7-Zip was found.");
        A("Arc_Items", "Пункты меню", "Menu items");
        A("Arc_Options", "Вид меню", "Menu style");
        A("Arc_TopItems", "Новое меню Windows 11", "Windows 11 modern menu");
        A("Arc_Handlers", "Расширение оболочки целиком", "Whole shell extension");
        A("Arc_HandlersHint", "Выключите, чтобы убрать всё меню архиватора сразу.", "Turn off to remove the whole archiver menu at once.");
        A("Arc_Cascaded", "Вложенное подменю", "Cascaded submenu");
        A("Arc_Icons", "Значки в меню", "Icons in menu");
        A("Arc_Hint7z", "Настройки хранятся в HKCU\\Software\\7-Zip\\Options (значение ContextMenu).", "Settings live in HKCU\\Software\\7-Zip\\Options (ContextMenu value).");
        A("Arc_HintRar", "Настройки хранятся в HKCU\\Software\\WinRAR\\Setup\\MenuItems.", "Settings live in HKCU\\Software\\WinRAR\\Setup\\MenuItems.");
        A("WinRar_ExtrTo", "Извлечь в папку «имя\\»", "Extract to \"name\\\"");
        A("WinRar_ExtrHere", "Извлечь в текущую папку", "Extract here");
        A("WinRar_Extr", "Извлечь файлы…", "Extract files…");
        A("WinRar_ExtrSep", "Извлечь каждый архив в отдельную папку", "Extract each archive to separate folder");
        A("WinRar_OpenArc", "Открыть в WinRAR", "Open with WinRAR");
        A("WinRar_OpenSFX", "Открывать SFX-архивы", "Open SFX archives");
        A("WinRar_AddTo", "Добавить в «имя.rar»", "Add to \"name.rar\"");
        A("WinRar_AddArc", "Добавить в архив…", "Add to archive…");
        A("WinRar_EmailArc", "Сжать и отправить по email…", "Compress and email…");
        A("WinRar_EmailOpt", "Сжать в «имя.rar» и отправить", "Compress to \"name.rar\" and email");
        A("WinRar_Test", "Проверить архив", "Test archive");
        A("WinRar_Convert", "Преобразовать архив", "Convert archive");
        A("WinRar_NewRAR", "Создать: архив RAR", "New: RAR archive");
        A("WinRar_NewZIP", "Создать: архив ZIP", "New: ZIP archive");
        A("SevenZip_Open", "Открыть архив", "Open archive");
        A("SevenZip_OpenAs", "Открыть архив как…", "Open archive as…");
        A("SevenZip_Extract", "Распаковать…", "Extract files…");
        A("SevenZip_ExtractHere", "Распаковать здесь", "Extract here");
        A("SevenZip_ExtractTo", "Распаковать в «имя\\»", "Extract to \"name\\\"");
        A("SevenZip_Test", "Тестировать", "Test archive");
        A("SevenZip_Compress", "Добавить к архиву…", "Add to archive…");
        A("SevenZip_CompressTo7z", "Добавить к «имя.7z»", "Add to \"name.7z\"");
        A("SevenZip_CompressToZip", "Добавить к «имя.zip»", "Add to \"name.zip\"");
        A("SevenZip_CompressEmail", "Сжать и отправить по email…", "Compress and email…");
        A("SevenZip_CompressTo7zEmail", "Сжать в «имя.7z» и отправить", "Compress to \"name.7z\" and email");
        A("SevenZip_CompressToZipEmail", "Сжать в «имя.zip» и отправить", "Compress to \"name.zip\" and email");
        A("SevenZip_CRC", "CRC SHA (контрольные суммы)", "CRC SHA (checksums)");
        A("SevenZip_CRCCascaded", "CRC SHA во вложенном меню", "CRC SHA as cascaded menu");

        // ---- navigation ----
        A("Nav_HeaderMenu", "МЕНЮ", "MENU");
        A("Nav_All", "Все пункты", "All items");
        A("Nav_ThirdParty", "От сторонних программ", "From third-party apps");
        A("Nav_Disabled", "Отключённые", "Disabled");
        A("Nav_HeaderWhere", "ГДЕ ПОЯВЛЯЕТСЯ", "WHERE IT APPEARS");
        A("Nav_HeaderTools", "ИНСТРУМЕНТЫ", "TOOLS");
        A("Nav_ShellNew", "Меню «Создать»", "New menu");
        A("Nav_Archivers", "WinRAR и 7-Zip", "WinRAR and 7-Zip");
        A("Nav_Win11", "Windows 11", "Windows 11");
        A("Nav_Custom", "Свои пункты и группы", "Custom items and groups");
        A("Nav_Journal", "Журнал и откат", "History and undo");
        A("Nav_Settings", "Настройки", "Settings");

        // ---- filters / dialogs ----
        A("Filter_ExtHint", "Показаны пункты, которые появляются для файлов {0}", "Showing items that appear for {0} files");
        A("Filter_AllFiles", "Все файлы (*.*)|*.*", "All files (*.*)|*.*");
        A("Filter_Programs", "Программы (*.exe;*.bat;*.cmd;*.ps1)|*.exe;*.bat;*.cmd;*.ps1;*.vbs;*.lnk|Все файлы (*.*)|*.*",
            "Programs (*.exe;*.bat;*.cmd;*.ps1)|*.exe;*.bat;*.cmd;*.ps1;*.vbs;*.lnk|All files (*.*)|*.*");
        A("Filter_Icons", "Значки (*.ico;*.exe;*.dll)|*.ico;*.exe;*.dll|Все файлы (*.*)|*.*", "Icons (*.ico;*.exe;*.dll)|*.ico;*.exe;*.dll|All files (*.*)|*.*");

        // ---- status ----
        A("Status_Copied", "Скопировано в буфер обмена", "Copied to clipboard");
        A("Status_Scanning", "Сканирование реестра…", "Scanning the registry…");
        A("Status_Scanned", "Найдено пунктов: {0}, от сторонних программ: {1}, отключено: {2}", "Found {0} items, {1} from third-party apps, {2} disabled");
        A("Status_ScanFailed", "Ошибка сканирования", "Scan failed");
        A("Status_Done", "Готово: {0}", "Done: {0}");
        A("Status_NothingToDo", "Нечего менять", "Nothing to change");
        A("Status_ExplorerRestarted", "Проводник перезапущен", "Explorer restarted");
        A("Status_Exported", "Сохранено: {0}", "Saved: {0}");

        // ---- warnings / questions ----
        A("Warn_Critical", "«{0}»\n\n{1}\n\nВсё равно отключить? Изменение можно будет отменить в журнале.",
            "\"{0}\"\n\n{1}\n\nDisable anyway? You can undo it in the history.");
        A("Warn_CriticalNew", "«{0}» относится к стандартным пунктам «Создать». Отключить?", "\"{0}\" is a standard New menu item. Disable it?");
        A("Ask_Elevate", "Этот пункт записан в HKEY_LOCAL_MACHINE (для всех пользователей). Нужны права администратора.\n\nПерезапустить программу от имени администратора?",
            "This item lives in HKEY_LOCAL_MACHINE (all users). Administrator rights are required.\n\nRestart the app as administrator?");
        A("Ask_ElevateBulk", "{0} пунктов требуют прав администратора. Перезапустить программу от имени администратора?\n\n«Нет»: изменить только пункты текущего пользователя.",
            "{0} items need administrator rights. Restart the app as administrator?\n\nNo: change only current-user items.");
        A("Ask_EnableVisible", "Включить все показанные пункты ({0})?", "Enable all shown items ({0})?");
        A("Ask_DisableVisible", "Отключить все показанные пункты ({0})? Критичные системные пункты будут пропущены.",
            "Disable all shown items ({0})? Critical system items will be skipped.");
        A("Ask_RestartExplorer", "Перезапустить Проводник? Окна папок закроются, панель задач мигнёт.",
            "Restart Explorer? Folder windows will close and the taskbar will blink.");
        A("Ask_DeleteCustom", "Удалить свой пункт «{0}»?", "Delete custom item \"{0}\"?");
        A("Ask_ImportProfile", "Применить профиль?\n\nКомпьютер: {0}\nСоздан: {1}\nОтключаемых пунктов: {2}\nСвоих пунктов: {3}",
            "Apply the profile?\n\nMachine: {0}\nCreated: {1}\nItems to disable: {2}\nCustom items: {3}");
        A("Ask_UndoLast", "Отменить последнее изменение?\n\n{0}", "Undo the last change?\n\n{0}");
        A("Ask_RestoreAll", "Отменить все изменения ({0}) и вернуть меню к исходному виду?", "Undo all changes ({0}) and restore the original menu?");
        A("Info_ProfileApplied", "Профиль применён.\n\nОтключено: {0}\nВключено: {1}\nИзменено Shift: {2}\nСоздано своих пунктов: {3}\nНе найдено на этом ПК: {4}",
            "Profile applied.\n\nDisabled: {0}\nEnabled: {1}\nShift changed: {2}\nCustom items created: {3}\nNot found on this PC: {4}");

        // ---- custom page ----
        A("Custom_TargetGroup", "Новая команда будет добавлена в подменю «{0}» ({1})", "The new command will be added to submenu \"{0}\" ({1})");
        A("Custom_ScopeExt", "Файлы с расширением…", "Files with extension…");
        A("Custom_Desc", "Добавьте свои команды в контекстное меню: открыть файл в нужной программе, запустить скрипт, сделать целую группу со своими пунктами.",
            "Add your own commands: open a file in a chosen app, run a script, or build a whole submenu group.");
        A("Custom_ArgsHint", "%1: выбранный файл или папка, %V: папка (для пустого места), %W: рабочая папка.",
            "%1: selected file or folder, %V: folder (for background), %W: working folder.");
        A("Custom_Children", "Команды в группе", "Commands in the group");
        A("Custom_Existing", "Созданные пункты", "Created items");
        A("Custom_NoExisting", "Вы ещё ничего не добавили", "You have not added anything yet");

        // ---- about ----
        A("About_Text", "Версия {0}\nСистема: {1}\nРежим: {2}\nДанные, журнал и резервные копии: {3}\n\nПрограмма ничего не удаляет из системного меню: она только отключает пункты обратимыми способами и сохраняет резервную копию перед каждым изменением.",
            "Version {0}\nSystem: {1}\nMode: {2}\nData, history and backups: {3}\n\nThe app never deletes system menu items: it only disables them reversibly and saves a backup before every change.");
        A("About_Admin", "администратор", "administrator");
        A("About_User", "обычный пользователь (пункты HKLM только для чтения)", "standard user (HKLM items are read-only)");

        // ---- XAML: buttons ----
        A("Btn_Refresh", "Обновить", "Refresh");
        A("Btn_Undo", "Отменить", "Undo");
        A("Btn_UndoThis", "Отменить", "Undo");
        A("Btn_RestoreAll", "Вернуть всё", "Restore all");
        A("Btn_RestartExplorer", "Перезапустить Проводник", "Restart Explorer");
        A("Btn_Elevate", "Права администратора", "Run as admin");
        A("Btn_EnableVisible", "Включить все", "Enable all");
        A("Btn_DisableVisible", "Отключить все", "Disable all");
        A("Btn_ClearFilters", "Сбросить фильтры", "Clear filters");
        A("Btn_GoArchivers", "Настроить пункты архиватора", "Configure archiver items");
        A("Btn_AddToGroup", "Добавить команду в это подменю", "Add a command to this submenu");
        A("Btn_Regedit", "Открыть в regedit", "Open in regedit");
        A("Btn_CopyPath", "Копировать путь", "Copy path");
        A("Btn_ExportReg", "Экспорт .reg", "Export .reg");
        A("Btn_Delete", "Удалить", "Delete");
        A("Btn_Browse", "Обзор…", "Browse…");
        A("Btn_Add", "Добавить", "Add");
        A("Btn_AddChild", "Добавить команду", "Add command");
        A("Btn_Create", "Создать пункт", "Create item");
        A("Btn_Cancel", "Отмена", "Cancel");
        A("Btn_Show", "Показать в списке", "Show in list");
        A("Btn_BackupFolder", "Папка резервных копий", "Backup folder");
        A("Btn_OpenBackup", "Показать резервную копию .reg", "Show .reg backup");
        A("Btn_DataFolder", "Открыть папку данных", "Open data folder");

        // ---- XAML: tooltips ----
        A("Tip_Refresh", "Пересканировать реестр (F5)", "Rescan the registry (F5)");
        A("Tip_Undo", "Отменить последнее изменение (Ctrl+Shift+Z)", "Undo the last change (Ctrl+Shift+Z)");
        A("Tip_RestoreAll", "Отменить все изменения, сделанные программой", "Undo every change made by this app");
        A("Tip_RestartExplorer", "Некоторые изменения видны только после перезапуска Проводника", "Some changes show up only after Explorer restarts");
        A("Tip_ExportProfile", "Сохранить профиль (отключённые и свои пункты) в файл", "Save a profile (disabled and custom items) to a file");
        A("Tip_ImportProfile", "Применить профиль из файла", "Apply a profile from a file");
        A("Tip_Theme", "Светлая / тёмная тема", "Light / dark theme");
        A("Tip_Language", "Сменить язык", "Switch language");
        A("Tip_Elevate", "Перезапустить с правами администратора, чтобы менять пункты для всех пользователей (HKLM)", "Restart as administrator to change all-users (HKLM) items");
        A("Tip_EnableVisible", "Включить все пункты, показанные в списке", "Enable every item shown in the list");
        A("Tip_DisableVisible", "Отключить все пункты, показанные в списке (кроме критичных)", "Disable every shown item (except critical ones)");
        A("Tip_ExtFilter", "Введите расширение (например .mp4), чтобы увидеть всё меню для такого файла", "Type an extension (e.g. .mp4) to see the whole menu for such a file");
        A("Tip_Shift", "Пункт будет виден только при щелчке правой кнопкой с зажатым Shift", "The item shows only on Shift + right-click");
        A("Tip_Programmatic", "Скрыть из меню, но оставить доступным для программ (ProgrammaticAccessOnly)", "Hide from the menu but keep it available to programs (ProgrammaticAccessOnly)");
        A("Tip_AllUsers", "Записать в HKLM для всех пользователей (нужны права администратора)", "Write to HKLM for all users (requires administrator)");

        // ---- XAML: misc ----
        A("Banner_Restart", "Часть изменений появится после перезапуска Проводника.", "Some changes will appear after Explorer restarts.");
        A("Badge_Submenu", "подменю", "submenu");
        A("Badge_Critical", "важный", "critical");
        A("Badge_Custom", "мой", "custom");
        A("Badge_ThirdParty", "сторонний", "third-party");
        A("Badge_AdminTip", "Для изменения нужны права администратора (HKLM)", "Changing requires administrator rights (HKLM)");
        A("Lbl_Shown", "Показано:", "Shown:");
        A("Lbl_Extension", "Расширение", "Extension");
        A("Lbl_NewName", "Название в меню", "Name in menu");
        A("Lbl_Template", "Файл-шаблон (необязательно)", "Template file (optional)");
        A("Lbl_Title", "Название", "Title");
        A("Lbl_Scope", "Где показывать", "Where to show");
        A("Lbl_Program", "Программа", "Program");
        A("Lbl_Arguments", "Аргументы", "Arguments");
        A("Lbl_Icon", "Значок (файл .ico, .exe или .dll,индекс)", "Icon (.ico, .exe or .dll,index)");
        A("Lbl_Position", "Позиция", "Position");
        A("Ph_Search", "Поиск по названию, программе, команде, CLSID…", "Search by name, program, command, CLSID…");
        A("Ph_Extension", "Расширение: .mp4", "Extension: .mp4");
        A("Chk_ThirdParty", "Только сторонние", "Third-party only");
        A("Chk_Disabled", "Только отключённые", "Disabled only");
        A("Chk_ShiftOnly", "Только Shift-пункты", "Shift-only items");
        A("Chk_ShiftItem", "Показывать только с Shift", "Show only with Shift");
        A("Chk_Programmatic", "Скрыть, оставив доступ программам", "Hide, keep available to programs");
        A("Chk_IsGroup", "Это группа (подменю с несколькими командами)", "This is a group (submenu with several commands)");
        A("Chk_Extended", "Только с Shift", "Shift only");
        A("Chk_RunAsAdmin", "От имени администратора", "Run as administrator");
        A("Chk_AllUsers", "Для всех пользователей", "For all users");
        A("Details_Empty", "Выберите пункт, чтобы увидеть подробности:\nоткуда он, какая программа его добавила и что он запускает.",
            "Select an item to see details:\nwhere it comes from, which app added it and what it runs.");
        A("Details_ArchiverHint", "Это меню архиватора. Отдельные команды внутри него (например «Добавить в архив») можно отключать на странице архиваторов.",
            "This is an archiver menu. Individual commands inside it (like Add to archive) can be turned off on the archivers page.");
        A("Details_NoChildren", "Вложенные команды не найдены (их формирует сама программа).", "No nested commands found (the app builds them itself).");
        A("Prop_Where", "Где появляется", "Where it appears");
        A("Prop_Category", "Категория", "Category");
        A("Prop_Kind", "Тип", "Type");
        A("Prop_Extensions", "Расширения", "Extensions");
        A("Prop_Product", "Программа", "Application");
        A("Prop_Company", "Издатель", "Publisher");
        A("Prop_Command", "Команда", "Command");
        A("Prop_Module", "Модуль (DLL/EXE)", "Module (DLL/EXE)");
        A("Prop_KeyName", "Имя ключа", "Key name");
        A("Prop_Registry", "Путь в реестре", "Registry path");
        A("Prop_Position", "Позиция", "Position");
        A("Win11_System", "Система:", "System:");
        A("Win11_NotWin11", "Это не Windows 11: настройки нового меню не действуют, классическое меню и так используется.",
            "This is not Windows 11: modern menu settings have no effect, the classic menu is already used.");
        A("Win11_ClassicTitle", "Классическое контекстное меню", "Classic context menu");
        A("Win11_ClassicDesc", "Сразу показывать полное меню, как в Windows 10, без пункта «Показать дополнительные параметры». Вступает в силу после перезапуска Проводника.",
            "Show the full Windows 10 style menu right away, without Show more options. Takes effect after Explorer restarts.");
        A("Win11_PackagedTitle", "Пункты приложений из Microsoft Store (MSIX)", "Microsoft Store (MSIX) app items");
        A("Win11_PackagedDesc", "Эти пункты добавляют пакетные приложения (например Terminal, PowerToys, Notepad). Они отключаются блокировкой CLSID для текущего пользователя.",
            "These come from packaged apps (e.g. Terminal, PowerToys, Notepad). They are turned off by blocking the CLSID for the current user.");
        A("Win11_NoPackaged", "Пакетных пунктов меню не найдено.", "No packaged menu items found.");
        A("Set_Theme", "Тема оформления", "Theme");
        A("Set_ThemeDesc", "Светлая, тёмная или как в Windows", "Light, dark or follow Windows");
        A("Set_Language", "Язык интерфейса", "Language");
        A("Set_Method", "Способ отключения расширений оболочки", "How to disable shell extensions");
        A("Set_MethodDesc", "Переименование ключа надёжнее и работает для одного типа файлов. Блокировка CLSID убирает расширение отовсюду сразу.",
            "Renaming the key is safest and affects one file type. Blocking the CLSID removes the extension everywhere at once.");
        A("Set_Confirm", "Предупреждать о важных пунктах", "Warn about critical items");
        A("Set_ConfirmDesc", "Спрашивать подтверждение перед отключением системных пунктов Windows", "Ask before disabling core Windows items");
        A("Set_About", "О программе", "About");
        return d;
    }
}

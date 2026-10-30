@echo off
chcp 65001 >nul
rem Сборка FirefoxContextMenuHider.exe. Нужен Python 3.9+ с tkinter (обычный установщик с python.org).
cd /d "%~dp0"
python -m pip install --upgrade pyinstaller || goto :error
python -m PyInstaller --noconfirm --clean --onefile --windowed ^
  --name FirefoxContextMenuHider ^
  --icon icon.ico ^
  --version-file version_info.txt ^
  --exclude-module unittest --exclude-module pydoc --exclude-module test ^
  firefox_context_menu_hider.py || goto :error
echo.
echo Готово: dist\FirefoxContextMenuHider.exe
exit /b 0
:error
echo Сборка не удалась.
exit /b 1

# Context Menu Hider для Firefox

Скрывает лишние пункты контекстных меню Firefox, Floorp, LibreWolf, Waterfox и Zen через `userChrome.css`.

- **Скачать:** [`bin/FirefoxContextMenuHider.exe`](bin/FirefoxContextMenuHider.exe) ([прямая ссылка](https://github.com/Nikitamce/MyUserScripts/raw/main/firefox-context-menu-hider/bin/FirefoxContextMenuHider.exe)). Установка и Python не нужны, Windows 10/11 x64.
- **Исходный код и сборка:** папка [`source`](source).

В разделе «Предпросмотр меню» меню видно так, как его покажет браузер. Пункты можно скрывать щелчком, а пункты и разделители перетаскивать, чтобы поменять порядок.

Перед первым запуском включите в `about:config` параметр `toolkit.legacyUserProfileCustomizations.stylesheets` (значение `true`). После «Применить» полностью перезапустите браузер.

Windows SmartScreen может предупредить о неизвестном издателе, потому что exe не подписан. Нажмите «Подробнее», затем «Выполнить в любом случае».

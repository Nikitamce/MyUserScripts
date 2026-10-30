#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Firefox / Floorp / LibreWolf / Waterfox / Zen — Context Menu Hider

Версия 4.

Что нового:
* Боковая навигация вместо тесных вкладок: категории сгруппированы
  по действиям («Ссылки», «Выделение текста», «Вкладки»…) и по меню
  браузера (боковая панель, закладки, панели инструментов…).
* «Все пункты» — единый список вообще всех найденных пунктов с поиском.
* «Выбранные» — только те пункты, которые уже отмечены к скрытию.
* Тёмная и светлая темы, сохранение профиля, темы и своих ID.
* Пункты меню читаются из omni.ja установленного браузера и подписываются
  по-русски; после обновления браузера новые пункты помечаются «новое».
* Расширения читаются из extensions.json профиля. Одна галочка скрывает
  все пункты расширения, в том числе созданные динамически.
"""

import os
import re
import sys
import json
import zlib
import struct
import shutil
import zipfile
import subprocess
from pathlib import Path
from datetime import datetime

import tkinter as tk
from tkinter import ttk, filedialog, font as tkfont

APP_NAME = "Context Menu Hider"
APP_VERSION = "4.5.1"

# ==================== КАТЕГОРИИ ====================

CAT_LINKS = "Ссылки"
CAT_IMAGES = "Изображения"
CAT_MEDIA = "Медиа"
CAT_TEXT = "Выделение текста"
CAT_INPUTS = "Поля ввода"
CAT_PAGE = "Страница"
CAT_FRAMES = "Фреймы"
CAT_PDF = "PDF-просмотрщик"
CAT_TABS = "Вкладки"
CAT_NAV = "Навигация"

CAT_SIDEBAR = "Боковая панель"
CAT_PLACES = "Закладки и история"
CAT_SIDEBAR_TABS = "Вкладки в панели"
CAT_TAB_GROUPS = "Группы вкладок"
CAT_TOOLBAR = "Панели инструментов"
CAT_CUSTOMIZE = "Настройка панели"
CAT_EXT_BUTTON = "Кнопка расширений"
CAT_PAGE_ACTION = "Действия страницы"
CAT_DOWNLOADS = "Загрузки"
CAT_PIP = "Картинка в картинке"
CAT_PAGE_OTHER = "Прочее меню страницы"
CAT_CODE = "Прочее (из кода)"
CAT_OTHER_MENUS = "Другие меню"

CAT_EXTENSIONS = "Расширения"
CAT_CUSTOM = "Свои ID"
CAT_MISSING = "Нет в этой версии"
CAT_NEW = "Новое"

CATEGORY_META = {
    CAT_LINKS: {
        "desc": "Правый клик по ссылке: открыть, скопировать адрес, сохранить.",
        "section": "actions",
    },
    CAT_IMAGES: {
        "desc": "Правый клик по картинке: открыть, сохранить, скопировать.",
        "section": "actions",
    },
    CAT_MEDIA: {
        "desc": "Видео и аудио: воспроизведение, звук, скорость, сохранение.",
        "section": "actions",
    },
    CAT_TEXT: {
        "desc": "Выделенный текст: поиск, копирование, перевод, ссылка на выделение.",
        "section": "actions",
    },
    CAT_INPUTS: {
        "desc": "Правый клик в текстовом поле: отмена, вставка, орфография, пароли.",
        "section": "actions",
    },
    CAT_PAGE: {
        "desc": "Правый клик по пустому месту страницы: сохранить, исходный код, инструменты.",
        "section": "actions",
    },
    CAT_FRAMES: {
        "desc": "Правый клик внутри встроенного фрейма.",
        "section": "actions",
    },
    CAT_PDF: {
        "desc": "Контекстное меню встроенного PDF-просмотрщика.",
        "section": "actions",
    },
    CAT_TABS: {
        "desc": "Правый клик по вкладке: закрыть, закрепить, дублировать, переместить.",
        "section": "actions",
    },
    CAT_NAV: {
        "desc": "Кнопки «Назад», «Вперёд», «Обновить» в меню страницы.",
        "section": "actions",
    },
    CAT_SIDEBAR: {
        "desc": "Правый клик по самой боковой панели.",
        "section": "browser",
    },
    CAT_PLACES: {
        "desc": "Закладки и история: открыть, изменить, удалить, скопировать ссылку.",
        "section": "browser",
    },
    CAT_SIDEBAR_TABS: {
        "desc": "Списки открытых и синхронизированных вкладок в боковой панели.",
        "section": "browser",
    },
    CAT_TAB_GROUPS: {
        "desc": "Меню открытых и сохранённых групп вкладок.",
        "section": "browser",
    },
    CAT_TOOLBAR: {
        "desc": "Правый клик по панелям и кнопкам вокруг адресной строки.",
        "section": "browser",
    },
    CAT_CUSTOMIZE: {
        "desc": "Меню режима настройки панели и палитры элементов.",
        "section": "browser",
    },
    CAT_EXT_BUTTON: {
        "desc": "Правый клик по кнопке меню расширений.",
        "section": "browser",
    },
    CAT_PAGE_ACTION: {
        "desc": "Меню значков, которые расширения добавляют в адресную строку.",
        "section": "browser",
    },
    CAT_DOWNLOADS: {
        "desc": "Правый клик по записи в списке загрузок.",
        "section": "browser",
    },
    CAT_PIP: {
        "desc": "Кнопка и окно «Картинка в картинке».",
        "section": "browser",
    },
    CAT_PAGE_OTHER: {
        "desc": "Остальные пункты меню страницы, которые не попали в группы выше.",
        "section": "browser",
    },
    CAT_CODE: {
        "desc": "ID, найденные в коде браузера без разметки. Обычно создаются динамически.",
        "section": "browser",
    },
    CAT_OTHER_MENUS: {
        "desc": "Меню, найденные автоматически. Их состав зависит от версии браузера.",
        "section": "browser",
    },
    CAT_EXTENSIONS: {
        "desc": "Одна галочка скрывает все пункты расширения — текущие и будущие.",
        "section": "extra",
    },
    CAT_CUSTOM: {
        "desc": "Свои ID или CSS-селекторы, которых нет во встроенных списках.",
        "section": "extra",
    },
    CAT_MISSING: {
        "desc": "Пункты из встроенного списка, которых нет в установленной версии браузера.",
        "section": "extra",
    },
    CAT_NEW: {
        "desc": "Пункты, появившиеся после обновления браузера.",
        "section": "extra",
    },
}

CATEGORY_ORDER = [
    CAT_LINKS, CAT_IMAGES, CAT_MEDIA, CAT_TEXT, CAT_INPUTS, CAT_PAGE,
    CAT_FRAMES, CAT_PDF, CAT_TABS, CAT_NAV,
    CAT_SIDEBAR, CAT_PLACES, CAT_SIDEBAR_TABS, CAT_TAB_GROUPS,
    CAT_TOOLBAR, CAT_CUSTOMIZE, CAT_EXT_BUTTON, CAT_PAGE_ACTION,
    CAT_DOWNLOADS, CAT_PIP, CAT_PAGE_OTHER, CAT_CODE, CAT_OTHER_MENUS,
]

NAV_SECTIONS = [
    ("Обзор", ["__all__", "__selected__", "__new__"]),
    ("По действиям", [
        CAT_LINKS, CAT_IMAGES, CAT_MEDIA, CAT_TEXT, CAT_INPUTS,
        CAT_PAGE, CAT_FRAMES, CAT_PDF, CAT_TABS, CAT_NAV,
    ]),
    ("Меню браузера", [
        CAT_SIDEBAR, CAT_PLACES, CAT_SIDEBAR_TABS, CAT_TAB_GROUPS,
        CAT_TOOLBAR, CAT_CUSTOMIZE, CAT_EXT_BUTTON, CAT_PAGE_ACTION,
        CAT_DOWNLOADS, CAT_PIP, CAT_PAGE_OTHER, CAT_CODE, CAT_OTHER_MENUS,
    ]),
    ("Дополнительно", [CAT_EXTENSIONS, CAT_CUSTOM, CAT_MISSING]),
]

CATEGORY_SYNONYMS = {
    CAT_LINKS: "link url ссылка",
    CAT_IMAGES: "image picture картинка фото изображение",
    CAT_MEDIA: "video audio видео аудио",
    CAT_TEXT: "text selection выделение текст",
    CAT_INPUTS: "input field поле ввод пароль орфография",
    CAT_PAGE: "page страница",
    CAT_FRAMES: "frame iframe фрейм",
    CAT_PDF: "pdf просмотрщик",
    CAT_TABS: "tab вкладка",
    CAT_NAV: "back forward reload навигация",
    CAT_SIDEBAR: "sidebar панель",
    CAT_PLACES: "bookmark history закладки история",
    CAT_SIDEBAR_TABS: "tabs вкладки панель",
    CAT_TAB_GROUPS: "tab group группа вкладок",
    CAT_TOOLBAR: "toolbar панель",
    CAT_CUSTOMIZE: "customize настройка",
    CAT_EXT_BUTTON: "extension расширение кнопка",
    CAT_PAGE_ACTION: "page action действие страницы",
    CAT_DOWNLOADS: "download загрузка",
    CAT_PIP: "picture-in-picture картинка в картинке",
}

# ==================== ИЗВЕСТНЫЕ ПУНКТЫ ====================
# Русские подписи и категории. Список самих пунктов берётся из сканирования
# браузера; словарь нужен для человеческих названий и группировки.

KNOWN_MENU_ITEMS = {
    CAT_LINKS: [
        ("context-openlinkintab", "Открыть ссылку в новой вкладке"),
        ("context-openlinkincontainertab", "Открыть ссылку в контейнере"),
        ("context-openlinkinusercontext-menu", "Открыть ссылку в новом контейнере"),
        ("context-openlinkincurrent", "Открыть ссылку"),
        ("context-openlink", "Открыть ссылку в новом окне"),
        ("context-openlinkprivate", "Открыть ссылку в приватном окне"),
        ("context-openlinkinsplitview", "Открыть ссылку в разделённом окне"),
        ("context-openlinksmartwindow", "Открыть ссылку в новом окне (Smart Window)"),
        ("context-previewlink", "Предпросмотр ссылки"),
        ("context-bookmarklink", "Добавить ссылку в закладки…"),
        ("context-savelink", "Сохранить ссылку как…"),
        ("context-savelinktopocket", "Сохранить ссылку в Pocket"),
        ("context-copylink", "Копировать ссылку"),
        ("context-stripOnShareLink", "Копировать чистую ссылку"),
        ("context-copyemail", "Копировать адрес email"),
        ("context-copyphone", "Копировать номер телефона"),
        ("context-sendlinktodevice", "Отправить ссылку на устройство"),
        ("open_in_private_container", "Открыть в приватном контейнере"),
        ("context_openLinkInExternalBrowser", "Открыть ссылку во внешнем браузере"),
        ("context_openInWorkspace", "Открыть ссылку в другом рабочем пространстве"),
        ("context-sep-open", "Разделитель: открыть"),
        ("context-sep-copylink", "Разделитель: копировать ссылку"),
        ("context-sep-sendlinktodevice", "Разделитель: отправить ссылку"),
    ],
    CAT_IMAGES: [
        ("context-viewimage", "Открыть изображение в новой вкладке"),
        ("context-saveimage", "Сохранить изображение как…"),
        ("context-copyimage", "Копировать адрес изображения"),
        ("context-copyimage-contents", "Копировать изображение"),
        ("context-imagetext", "Копировать текст с изображения"),
        ("context-sendimage", "Отправить изображение…"),
        ("context-setDesktopBackground", "Сделать фоном рабочего стола…"),
        ("context-viewimageinfo", "Информация об изображении"),
        ("context-viewimagedesc", "Описание изображения"),
        ("context-reloadimage", "Обновить изображение"),
        ("context-viewbgimage", "Открыть фоновое изображение"),
        ("context-visual-search", "Визуальный поиск"),
        ("context-sep-setbackground", "Разделитель: фон"),
    ],
    CAT_MEDIA: [
        ("context-media-play", "Воспроизвести"),
        ("context-media-pause", "Пауза"),
        ("context-media-mute", "Выключить звук"),
        ("context-media-unmute", "Включить звук"),
        ("context-media-playbackrate", "Скорость воспроизведения"),
        ("context-media-playbackrate-050x", "Скорость 0,5×"),
        ("context-media-playbackrate-100x", "Скорость 1,0×"),
        ("context-media-playbackrate-125x", "Скорость 1,25×"),
        ("context-media-playbackrate-150x", "Скорость 1,5×"),
        ("context-media-playbackrate-200x", "Скорость 2,0×"),
        ("context-media-showcontrols", "Показать элементы управления"),
        ("context-media-hidecontrols", "Скрыть элементы управления"),
        ("context-media-loop", "Повторять"),
        ("context-media-eme-learnmore", "Подробнее о DRM…"),
        ("context-video-fullscreen", "Во весь экран"),
        ("context-video-saveimage", "Сохранить кадр…"),
        ("context-video-pictureinpicture", "Смотреть в картинке в картинке"),
        ("context-viewvideo", "Открыть видео в новой вкладке"),
        ("context-savevideo", "Сохранить видео как…"),
        ("context-saveaudio", "Сохранить аудио как…"),
        ("context-copyvideourl", "Копировать адрес видео"),
        ("context-copyaudiourl", "Копировать адрес аудио"),
        ("context-sendvideo", "Отправить видео…"),
        ("context-sendaudio", "Отправить аудио…"),
        ("context-media-eme-separator", "Разделитель: DRM"),
        ("context-media-sep-commands", "Разделитель: медиа"),
        ("context-media-sep-video-commands", "Разделитель: видео"),
    ],
    CAT_TEXT: [
        ("context-searchselect", "Искать выделенное"),
        ("context-searchselect-private", "Искать выделенное в приватном окне"),
        ("context-translate-selection", "Перевести выделенный текст"),
        ("context-copy", "Копировать"),
        ("context-selectall", "Выделить всё"),
        ("context-viewpartialsource-selection", "Показать исходный код выделения"),
        ("context-print-selection", "Печать выделения…"),
        ("context-copy-link-to-highlight", "Копировать ссылку на выделение"),
        ("context-copy-clean-link-to-highlight", "Копировать чистую ссылку на выделение"),
        ("context-remove-highlight", "Удалить выделение"),
        ("context-ask-chat", "Спросить в чате"),
        ("context-bidi-text-direction-toggle", "Направление текста справа налево"),
        ("context-sep-selectall", "Разделитель: выделение"),
        ("context-sep-highlights", "Разделитель: выделения"),
        ("context-sep-bidi", "Разделитель: направление текста"),
    ],
    CAT_INPUTS: [
        ("context-undo", "Отменить"),
        ("context-redo", "Повторить"),
        ("context-cut", "Вырезать"),
        ("context-paste", "Вставить"),
        ("context-paste-no-formatting", "Вставить без форматирования"),
        ("context-delete", "Удалить"),
        ("context-reveal-password", "Показать пароль"),
        ("context-sep-redo", "Разделитель: отменить/повторить"),
        ("fill-login", "Заполнить сохранённым логином"),
        ("fill-login-generated-password", "Предложить надёжный пароль…"),
        ("manage-saved-logins", "Управление паролями"),
        ("passwordmgr-items-separator", "Разделитель: пароли"),
        ("use-relay-mask", "Использовать маску email"),
        ("spell-check-enabled", "Проверка орфографии"),
        ("spell-dictionaries", "Словари"),
        ("spell-add-dictionaries", "Добавить словари…"),
        ("spell-add-dictionaries-main", "Добавить словари…"),
        ("spell-add-to-dictionary", "Добавить слово в словарь"),
        ("spell-undo-add-to-dictionary", "Отменить добавление слова"),
        ("spell-no-suggestions", "Нет вариантов"),
        ("spell-language-separator", "Разделитель: языки"),
        ("spell-separator", "Разделитель: орфография"),
        ("spell-suggestions-separator", "Разделитель: варианты"),
    ],
    CAT_PAGE: [
        ("context-savepage", "Сохранить страницу как…"),
        ("context-sendpagetodevice", "Отправить страницу на устройство"),
        ("context-sendpage", "Отправить страницу…"),
        ("context-print", "Печать…"),
        ("context-viewsource", "Показать исходный код страницы"),
        ("context-viewinfo", "Информация о странице"),
        ("context-inspect", "Исследовать элемент"),
        ("context-inspect-a11y", "Свойства доступности"),
        ("inspect-separator", "Разделитель: инструменты разработчика"),
        ("context-take-screenshot", "Сделать скриншот"),
        ("context-leave-dom-fullscreen", "Выйти из полноэкранного режима"),
        ("context-add-engine", "Добавить поисковую систему"),
        ("context-create-aitab", "Создать ИИ-вкладку"),
        ("context-use-mini-window", "Переместить раздел в мини-окно"),
        ("context-bidi-page-direction-toggle", "Направление страницы справа налево"),
        ("context-viewsource-goToLine", "Перейти к строке"),
        ("context-viewsource-highlightSyntax", "Подсветка синтаксиса"),
        ("context-viewsource-wrapLongLines", "Перенос длинных строк"),
        ("context-sep-viewsource-commands", "Разделитель: исходный код"),
        ("context-sep-screenshots", "Разделитель: скриншоты"),
        ("context-sep-sharing", "Разделитель: поделиться"),
        ("context-sep-sendpagetodevice", "Разделитель: отправить страницу"),
    ],
    CAT_FRAMES: [
        ("context-showonlythisframe", "Показать только этот фрейм"),
        ("context-openframeintab", "Открыть фрейм в новой вкладке"),
        ("context-openframe", "Открыть фрейм в новом окне"),
        ("context-openframeprivate", "Открыть фрейм в приватном окне"),
        ("context-bookmarkframe", "Добавить фрейм в закладки…"),
        ("context-saveframe", "Сохранить фрейм как…"),
        ("context-printframe", "Печать фрейма…"),
        ("context-reloadframe", "Обновить фрейм"),
        ("context-viewframesource", "Исходный код фрейма"),
        ("context-viewframeinfo", "Информация о фрейме"),
        ("frame", "Этот фрейм"),
        ("frame-sep", "Разделитель: фрейм"),
        ("open-frame-sep", "Разделитель: открыть фрейм"),
        ("print-frame-sep", "Разделитель: печать фрейма"),
    ],
    CAT_PDF: [
        ("context-pdfjs-copy", "Копировать"),
        ("context-pdfjs-cut", "Вырезать"),
        ("context-pdfjs-paste", "Вставить"),
        ("context-pdfjs-delete", "Удалить"),
        ("context-pdfjs-undo", "Отменить"),
        ("context-pdfjs-redo", "Повторить"),
        ("context-pdfjs-select-all", "Выделить всё"),
        ("context-pdfjs-highlight-selection", "Выделить текст"),
        ("context-pdfjs-comment-selection", "Добавить комментарий"),
        ("context-pdfjs-save-page", "Сохранить выделение как…"),
        ("context-pdfjs-copy-page", "Копировать страницу"),
        ("context-pdfjs-cut-page", "Вырезать страницу"),
        ("context-pdfjs-delete-page", "Удалить страницу"),
        ("context-sep-pdfjs-redo", "Разделитель: отменить/повторить"),
        ("context-sep-pdfjs-save-page", "Разделитель: сохранить"),
        ("context-sep-pdfjs-select-all", "Разделитель: выделение"),
    ],
    CAT_TABS: [
        ("context_openANewTab", "Новая вкладка справа"),
        ("context_reloadTab", "Обновить вкладку"),
        ("context_reloadSelectedTabs", "Обновить выбранные вкладки"),
        ("context_pinTab", "Закрепить вкладку"),
        ("context_unpinTab", "Открепить вкладку"),
        ("context_pinSelectedTabs", "Закрепить выбранные вкладки"),
        ("context_unpinSelectedTabs", "Открепить выбранные вкладки"),
        ("context_duplicateTab", "Дублировать вкладку"),
        ("context_duplicateTabs", "Дублировать выбранные вкладки"),
        ("context_selectAllTabs", "Выбрать все вкладки"),
        ("context_bookmarkTab", "Добавить вкладку в закладки…"),
        ("context_bookmarkSelectedTabs", "Добавить выбранные вкладки в закладки…"),
        ("context_toggleMuteTab", "Вкл/выкл звук вкладки"),
        ("context_toggleMuteSelectedTabs", "Вкл/выкл звук выбранных вкладок"),
        ("context_playTab", "Воспроизвести вкладку"),
        ("context_playSelectedTabs", "Воспроизвести выбранные вкладки"),
        ("context_unloadTab", "Выгрузить вкладку"),
        ("context_closeTab", "Закрыть вкладку"),
        ("context_closeDuplicateTabs", "Закрыть дубликаты вкладок"),
        ("context_closeTabOptions", "Закрыть несколько вкладок"),
        ("context_closeOtherTabs", "Закрыть другие вкладки"),
        ("context_closeTabsToTheStart", "Закрыть вкладки слева"),
        ("context_closeTabsToTheEnd", "Закрыть вкладки справа"),
        ("context_undoCloseTab", "Восстановить закрытую вкладку"),
        ("context_moveToStart", "Переместить в начало"),
        ("context_moveToEnd", "Переместить в конец"),
        ("context_moveTabOptions", "Переместить вкладку"),
        ("context_openTabInWindow", "Переместить в новое окно"),
        ("context_openTabInMiniWindow", "Переместить в мини-окно"),
        ("context_moveTabToNewGroup", "Переместить в новую группу"),
        ("context_moveTabToGroup", "Переместить в группу"),
        ("context_moveTabToGroupNewGroup", "Новая группа"),
        ("context_moveTabToSavedGroup", "Переместить в закрытую группу"),
        ("context_moveTabToSplitView", "Открыть в разделённом окне"),
        ("context_moveSplitViewToNewGroup", "Переместить разделение в новую группу"),
        ("context_reverseSplitView", "Поменять вкладки местами"),
        ("context_separateSplitView", "Разделить вкладки"),
        ("context_ungroupSplitView", "Разъединить разделение"),
        ("context_ungroupTab", "Убрать вкладку из группы"),
        ("context_reopenInContainer", "Открыть в контейнере"),
        ("context_toggleToPrivateContainer", "Переоткрыть в приватном контейнере"),
        ("context_openInExternalBrowser", "Открыть вкладку во внешнем браузере"),
        ("context_MoveTabToOtherWorkspace", "Переместить вкладку в другое рабочее пространство"),
        ("floorp_addPaneToSplitView", "Добавить область в разделённый вид"),
        ("floorp_moveTabToPane", "Переместить в область"),
        ("context_sendTabToDevice", "Отправить вкладку на устройство"),
        ("context_shareSelectedTabs", "Создать ссылку для обмена"),
        ("context_addNote", "Добавить заметку"),
        ("context_editNote", "Изменить заметку"),
        ("context_askChat", "Спросить в чате"),
        ("context_askChatSummarize", "Суммировать"),
        ("context_createAITab", "Создать ИИ-вкладку"),
        ("context_fullscreenAutohide", "Скрыть панели"),
        ("context_fullscreenExit", "Выйти из полноэкранного режима"),
        ("context_openAndOrganizeSeparator", "Разделитель: открыть и упорядочить"),
        ("context_selectAllSeparator", "Разделитель: выбрать все"),
        ("context_tabStateSeparator", "Разделитель: состояние вкладки"),
        ("context_tabToolsSeparator", "Разделитель: инструменты"),
        ("context_moveTabToGroupSeparator", "Разделитель: переместить в группу"),
        ("context_sendTabToDeviceSeparator", "Разделитель: отправить вкладку"),
        ("context_shareSelectedTabsSeparator", "Разделитель: поделиться"),
        ("context_aiSeparator", "Разделитель: ИИ"),
        ("context_fullscreenSeparator", "Разделитель: полный экран"),
        ("moveTabSeparator", "Разделитель: перемещение"),
        ("open-tab-groups-separator-lower", "Разделитель: группы вкладок"),
        ("open-tab-groups-separator-upper", "Разделитель: группы вкладок"),
    ],
    CAT_NAV: [
        ("context-navigation", "Группа «Назад / Вперёд»"),
        ("context-back", "Назад"),
        ("context-forward", "Вперёд"),
        ("context-reload", "Обновить"),
        ("context-stop", "Остановить"),
        ("context-bookmarkpage", "Добавить страницу в закладки…"),
        ("context-sep-navigation", "Разделитель: навигация"),
    ],
    CAT_SIDEBAR: [
        ("sidebar-context-menu-customize-sidebar", "Настроить боковую панель"),
        ("sidebar-context-menu-enable-vertical-tabs", "Включить вертикальные вкладки"),
        ("sidebar-context-menu-hide-sidebar", "Скрыть боковую панель"),
        ("sidebar-context-menu-manage-extension", "Управление расширением"),
        ("sidebar-context-menu-remove-extension", "Удалить расширение"),
        ("sidebar-context-menu-report-extension", "Пожаловаться на расширение"),
        ("sidebar-context-menu-unpin-extension", "Убрать из боковой панели"),
    ],
    CAT_PLACES: [
        ("placesContext_open", "Открыть"),
        ("placesContext_open:newtab", "Открыть в новой вкладке"),
        ("placesContext_open:newcontainertab", "Открыть в контейнере"),
        ("placesContext_open:newprivatewindow", "Открыть в приватном окне"),
        ("placesContext_open:newwindow", "Открыть в новом окне"),
        ("placesContext_openBookmarkContainer:tabs", "Открыть все закладки"),
        ("placesContext_openBookmarkLinks:tabs", "Открыть все закладки"),
        ("placesContext_openContainer:tabs", "Открыть всё в новых вкладках"),
        ("placesContext_openLinks:tabs", "Открыть всё в новых вкладках"),
        ("placesContext_new:bookmark", "Добавить закладку…"),
        ("placesContext_createBookmark", "Добавить закладку…"),
        ("placesContext_new:folder", "Добавить папку…"),
        ("placesContext_new:separator", "Добавить разделитель"),
        ("placesContext_copy", "Копировать"),
        ("placesContext_cut", "Вырезать"),
        ("placesContext_paste", "Вставить"),
        ("placesContext_paste_group", "Вставить"),
        ("placesContext_delete", "Удалить"),
        ("placesContext_deleteBookmark", "Удалить закладку"),
        ("placesContext_deleteFolder", "Удалить папку"),
        ("placesContext_delete_history", "Удалить из истории"),
        ("placesContext_deleteHost", "Забыть этот сайт…"),
        ("placesContext_show:info", "Изменить…"),
        ("placesContext_show_bookmark:info", "Изменить закладку…"),
        ("placesContext_show_folder:info", "Изменить папку…"),
        ("placesContext_showInFolder", "Показать в папке"),
        ("placesContext_showAllBookmarks", "Управление закладками"),
        ("placesContext_sortBy:name", "Сортировать по имени"),
        ("placesContext_removeTag", "Удалить тег"),
        ("placesContext_deleteSeparator", "Разделитель: удалить"),
        ("placesContext_editSeparator", "Разделитель: изменить"),
        ("placesContext_newSeparator", "Разделитель: создать"),
        ("placesContext_openSeparator", "Разделитель: открыть"),
        ("placesContext_pasteSeparator", "Разделитель: вставить"),
        ("contentsharing_sharefolder", "Поделиться папкой"),
        ("sidebar-bookmarks-context-add-bookmark", "Добавить закладку…"),
        ("sidebar-bookmarks-context-add-folder", "Добавить папку…"),
        ("sidebar-bookmarks-context-add-separator", "Добавить разделитель"),
        ("sidebar-bookmarks-context-copy", "Копировать"),
        ("sidebar-bookmarks-context-copy-link", "Копировать ссылку"),
        ("sidebar-bookmarks-context-cut", "Вырезать"),
        ("sidebar-bookmarks-context-delete-bookmark", "Удалить закладку"),
        ("sidebar-bookmarks-context-edit-bookmark", "Изменить закладку…"),
        ("sidebar-bookmarks-context-open-all-bookmarks", "Открыть все закладки"),
        ("sidebar-bookmarks-context-open-in-container-tab", "Открыть в контейнере"),
        ("sidebar-bookmarks-context-open-in-private-window", "Открыть в приватном окне"),
        ("sidebar-bookmarks-context-open-in-tab", "Открыть в новой вкладке"),
        ("sidebar-bookmarks-context-open-in-window", "Открыть в новом окне"),
        ("sidebar-bookmarks-context-paste", "Вставить"),
        ("sidebar-bookmarks-context-show-in-folder", "Показать в папке"),
        ("sidebar-bookmarks-context-sort-by-name", "Сортировать по имени"),
        ("sidebar-history-context-bookmark-page", "Добавить страницу в закладки…"),
        ("sidebar-history-context-copy-link", "Копировать ссылку"),
        ("sidebar-history-context-delete-page", "Удалить страницу из истории"),
        ("sidebar-history-context-delete-pages", "Удалить страницы из истории"),
        ("sidebar-history-context-forget-site", "Удалить все данные сайта…"),
        ("sidebar-history-context-menu-container-tab", "Открыть в контейнере"),
        ("sidebar-history-context-open-all-in-tabs", "Открыть всё в новых вкладках"),
        ("sidebar-history-context-open-in-private-window", "Открыть в приватном окне"),
        ("sidebar-history-context-open-in-tab", "Открыть в новой вкладке"),
        ("sidebar-history-context-open-in-window", "Открыть в новом окне"),
        ("sidebar-history-clear", "Очистить историю"),
        ("sidebar-history-sort-by-date", "Сортировать по дате"),
        ("sidebar-history-sort-by-date-and-site", "Сортировать по дате и сайту"),
        ("sidebar-history-sort-by-last-visited", "Последние посещённые"),
        ("sidebar-history-sort-by-most-visited", "Самые посещаемые"),
        ("sidebar-history-sort-by-site", "Сортировать по сайту"),
    ],
    CAT_SIDEBAR_TABS: [
        ("sidebar-opentabs-context-close-tab", "Закрыть вкладку"),
        ("sidebar-opentabs-context-copy-link", "Копировать ссылку"),
        ("sidebar-opentabs-context-open-in-private-window", "Открыть в приватном окне"),
        ("sidebar-opentabs-context-open-in-window", "Открыть в новом окне"),
        ("sidebar-opentabs-connect-another-device", "Подключить другое устройство"),
        ("sidebar-opentabs-sort-by-order", "Сортировать: по порядку вкладок"),
        ("sidebar-opentabs-sort-by-recency", "Сортировать: недавно активные"),
        ("syncedTabsBookmarkSelected", "Добавить вкладку в закладки…"),
        ("syncedTabsCopySelected", "Копировать"),
        ("syncedTabsManageDevices", "Управление устройствами…"),
        ("syncedTabsOpenAllInTabs", "Открыть всё в новых вкладках"),
        ("syncedTabsOpenSelected", "Открыть"),
        ("syncedTabsOpenSelectedInContainerTab", "Открыть в контейнере"),
        ("syncedTabsOpenSelectedInPrivateWindow", "Открыть в приватном окне"),
        ("syncedTabsOpenSelectedInTab", "Открыть в новой вкладке"),
        ("syncedTabsOpenSelectedInWindow", "Открыть в новом окне"),
        ("syncedTabsRefresh", "Синхронизировать сейчас"),
        ("syncedTabsRefreshFilter", "Синхронизировать сейчас"),
        ("sidebar-context-menu-close-remote-tab", "Закрыть вкладку на устройстве"),
        ("sidebar-synced-tabs-context-bookmark-tab", "Добавить вкладку в закладки…"),
        ("sidebar-synced-tabs-context-connect-another-device", "Подключить другое устройство"),
        ("sidebar-synced-tabs-context-copy-link", "Копировать ссылку"),
        ("sidebar-synced-tabs-context-manage-this-device", "Управление этим устройством"),
        ("sidebar-synced-tabs-context-menu-container-tab", "Открыть в контейнере"),
        ("sidebar-synced-tabs-context-open-all-in-tabs", "Открыть всё в новых вкладках"),
        ("sidebar-synced-tabs-context-open-in-private-window", "Открыть в приватном окне"),
        ("sidebar-synced-tabs-context-open-in-tab", "Открыть в новой вкладке"),
        ("sidebar-synced-tabs-context-open-in-window", "Открыть в новом окне"),
    ],
    CAT_TAB_GROUPS: [
        ("open-tab-group-context-menu_delete", "Удалить группу"),
        ("open-tab-group-context-menu_moveToNewWindow", "Переместить группу в новое окно"),
        ("open-tab-group-context-menu_moveToThisWindow", "Переместить группу в это окно"),
        ("open-tab-group-context-menu_share", "Поделиться группой"),
        ("saved-tab-group-context-menu_delete", "Удалить сохранённую группу"),
        ("saved-tab-group-context-menu_openInNewWindow", "Открыть группу в новом окне"),
        ("saved-tab-group-context-menu_openInThisWindow", "Открыть группу в этом окне"),
    ],
    CAT_TOOLBAR: [
        ("toolbar-context-customize", "Настроить панель…"),
        ("toolbar-context-customize-sidebar", "Настроить боковую панель"),
        ("toolbar-context-toggle-vertical-tabs", "Включить вертикальные вкладки"),
        ("toolbar-context-full-screen-autohide", "Скрыть панели"),
        ("toolbar-context-full-screen-exit", "Выйти из полноэкранного режима"),
        ("toolbar-context-bookmarkSelectedTab", "Добавить вкладку в закладки…"),
        ("toolbar-context-bookmarkSelectedTabs", "Добавить выбранные вкладки в закладки…"),
        ("toolbar-context-reloadSelectedTab", "Обновить вкладку"),
        ("toolbar-context-reloadSelectedTabs", "Обновить выбранные вкладки"),
        ("toolbar-context-selectAllTabs", "Выбрать все вкладки"),
        ("toolbar-context-undoCloseTab", "Восстановить закрытую вкладку"),
        ("toolbar-context-openANewTab", "Новая вкладка"),
        ("toolbar-context-pin-to-toolbar", "Закрепить на панели"),
        ("toolbar-context-move-to-panel", "Закрепить в меню переполнения"),
        ("toolbar-context-remove-from-toolbar", "Убрать с панели"),
        ("toolbar-context-manage-extension", "Управление расширением"),
        ("toolbar-context-remove-extension", "Удалить расширение"),
        ("toolbar-context-report-extension", "Пожаловаться на расширение"),
        ("toolbar-context-always-open-downloads-panel", "Показывать панель при загрузке"),
        ("toolbar-context-autohide-downloads-button", "Скрывать кнопку загрузок, когда пусто"),
        ("toolbar-context-always-show-extensions-button", "Всегда показывать кнопку расширений"),
        ("toggle_statusBar", "Строка состояния"),
    ],
    CAT_CUSTOMIZE: [
        ("customizationPanelItemContextMenuManageExtension", "Управление расширением"),
        ("customizationPanelItemContextMenuPin", "Закрепить в меню переполнения"),
        ("customizationPanelItemContextMenuUnpin", "Открепить от меню переполнения"),
        ("customizationPanelItemContextMenuRemove", "Убрать с панели"),
        ("customizationPanelItemContextMenuRemoveExtension", "Удалить расширение"),
        ("customizationPanelItemContextMenuReportExtension", "Пожаловаться на расширение"),
        ("customizationPaletteItemContextMenuAddToPanel", "Добавить в меню переполнения"),
        ("customizationPaletteItemContextMenuAddToToolbar", "Добавить на панель"),
    ],
    CAT_EXT_BUTTON: [
        ("unified-extensions-context-menu-manage-extension", "Управление расширением"),
        ("unified-extensions-context-menu-move-widget-down", "Переместить вниз"),
        ("unified-extensions-context-menu-move-widget-up", "Переместить вверх"),
        ("unified-extensions-context-menu-pin-to-toolbar", "Закрепить на панели"),
        ("unified-extensions-context-menu-remove-extension", "Удалить расширение"),
        ("unified-extensions-context-menu-report-extension", "Пожаловаться на расширение"),
    ],
    CAT_PAGE_ACTION: [
        ("pageActionContextMenuManageExtension", "Управление расширением…"),
        ("pageActionContextMenuRemoveExtension", "Удалить расширение"),
    ],
    CAT_PIP: [
        ("context_HidePictureInPictureToggle", "Скрыть кнопку «Картинка в картинке»"),
        ("context_MovePictureInPictureToggle", "Переместить кнопку влево"),
        ("menu_copy", "Копировать"),
        ("menu_selectall", "Выделить всё"),
    ],
    CAT_OTHER_MENUS: [
        ("unloadWebpanelMenu", "Выгрузить эту веб-панель"),
        ("muteMenu", "Вкл/выкл звук веб-панели"),
        ("changeZoomLevelMenu", "Масштаб веб-панели"),
        ("zoomInMenu", "Приблизить"),
        ("zoomOutMenu", "Отдалить"),
        ("resetZoomMenu", "Сбросить масштаб"),
        ("changeUAWebpanelMenu", "Сменить User-Agent веб-панели"),
        ("deleteWebpanelMenu", "Удалить веб-панель"),
    ],
}

KNOWN_LABEL = {}
KNOWN_CAT = {}
KNOWN_POS = {}
for _cat, _items in KNOWN_MENU_ITEMS.items():
    for _pos, (_iid, _lbl) in enumerate(_items):
        KNOWN_LABEL[_iid] = _lbl
        KNOWN_CAT[_iid] = _cat
        KNOWN_POS[_iid] = _pos

POPUP_CATEGORY_MAP = {
    "toolbar-context-menu": CAT_TOOLBAR,
    "placesContext": CAT_PLACES,
    "pageActionContextMenu": CAT_PAGE_ACTION,
    "unified-extensions-context-menu": CAT_EXT_BUTTON,
    "downloadsContextMenu": CAT_DOWNLOADS,
    "customizationPanelItemContextMenu": CAT_CUSTOMIZE,
    "customizationPaletteItemContextMenu": CAT_CUSTOMIZE,
    "pictureInPictureToggleContextMenu": CAT_PIP,
    "picontext": CAT_PIP,
    "open-tab-group-context-menu": CAT_TAB_GROUPS,
    "saved-tab-group-context-menu": CAT_TAB_GROUPS,
    "tabGroupContextMenu": CAT_TAB_GROUPS,
    "webpanel-context": CAT_OTHER_MENUS,
    "workspaces-toolbar-item-context-menu": CAT_OTHER_MENUS,
    "floorp-stack-kind-menu": CAT_TAB_GROUPS,
}

CAT_DYNAMIC = CAT_CODE

SPECIAL_ALL_EXT = "__HIDE_ALL_EXTENSION_ITEMS__"
SPECIAL_EXT_SEP = "__HIDE_EXTENSION_SEPARATORS__"
EXT_PREFIX = "ext:"
SEL_PREFIX = "sel:"

BEGIN_MARK = "/* BEGIN CONTEXT MENU HIDER */"
END_MARK = "/* END CONTEXT MENU HIDER */"
CUSTOM_SECTION_COMMENT = "/* Свои ID и селекторы */"

ITEM_TAGS = {"menuitem", "menu", "menuseparator", "menugroup"}

# ==================== ЧТЕНИЕ omni.ja ====================


class Omni:
    """Читает omni.ja. У Mozilla нестандартная раскладка (центральный каталог
    в начале файла), из-за которой zipfile иногда отказывается открывать архив."""

    def __init__(self, path):
        self.path = str(path)
        self.entries = {}
        self._zf = None
        try:
            self._parse_manual()
        except Exception:
            self.entries = {}
            self._zf = zipfile.ZipFile(self.path)

    def _parse_manual(self):
        with open(self.path, "rb") as f:
            f.seek(0, os.SEEK_END)
            size = f.tell()
            tail_len = min(size, 70000)
            f.seek(size - tail_len)
            tail = f.read(tail_len)
            i = tail.rfind(b"PK\x05\x06")
            if i < 0:
                raise ValueError("no EOCD")
            _, _, _, count, _, cd_size, cd_off, _ = struct.unpack("<IHHHHIIH", tail[i:i + 22])
            f.seek(cd_off)
            cd = f.read(cd_size)
        pos = 0
        for _ in range(count):
            (sig, _vm, _vn, _fl, method, _mt, _md, _crc, comp_size, _us,
             nlen, elen, clen, _ds, _ia, _ea, loc) = struct.unpack(
                "<IHHHHHHIIIHHHHHII", cd[pos:pos + 46])
            if sig != 0x02014B50:
                raise ValueError("bad central directory")
            name = cd[pos + 46:pos + 46 + nlen].decode("utf-8", "replace")
            self.entries[name] = (method, comp_size, loc)
            pos += 46 + nlen + elen + clen

    def names(self):
        return list(self.entries) if self.entries else self._zf.namelist()

    def read(self, name):
        if self._zf:
            return self._zf.read(name)
        method, comp_size, loc = self.entries[name]
        with open(self.path, "rb") as f:
            f.seek(loc)
            hdr = f.read(30)
            nlen, elen = struct.unpack("<HH", hdr[26:30])
            f.seek(loc + 30 + nlen + elen)
            data = f.read(comp_size)
        return zlib.decompress(data, -15) if method == 8 else data

    def read_text(self, name):
        return self.read(name).decode("utf-8", "replace")


# ==================== ПОИСК УСТАНОВКИ БРАУЗЕРА ====================

PROGRAM_DIR_GUESSES = [
    "Mozilla Firefox", "Ablaze Floorp", "Floorp", "LibreWolf", "Waterfox",
    "Zen Browser", "Mozilla Firefox ESR", "Firefox Developer Edition",
]


def read_ini_value(path, key):
    try:
        for line in Path(path).read_text(encoding="utf-8", errors="ignore").splitlines():
            if line.startswith(key + "="):
                return line.split("=", 1)[1].strip()
    except OSError:
        pass
    return None


def find_omni_files(install_dir):
    """Возвращает (browser_omni, platform_omni); любой из них может быть None."""
    d = Path(install_dir)
    if d.name.lower() == "browser":
        d = d.parent
    b = d / "browser" / "omni.ja"
    p = d / "omni.ja"
    b = b if b.is_file() else None
    p = p if p.is_file() else None
    if b or p:
        return (b, p)
    return None


def find_install_dir(profile):
    """Ищет папку установки браузера. Первый источник — compatibility.ini
    профиля (там записан путь к тому браузеру, который его использовал)."""
    cands = []
    profile = Path(profile)
    for key in ("LastPlatformDir", "LastAppDir"):
        v = read_ini_value(profile / "compatibility.ini", key)
        if v:
            cands.append(Path(v))
    roots = []
    for env in ("ProgramFiles", "ProgramFiles(x86)", "LOCALAPPDATA"):
        v = os.environ.get(env)
        if v:
            roots.append(Path(v))
            roots.append(Path(v) / "Programs")
    for r in roots:
        for g in PROGRAM_DIR_GUESSES:
            cands.append(r / g)
    if sys.platform.startswith("linux"):
        for g in ("/usr/lib/firefox", "/usr/lib64/firefox", "/opt/floorp",
                  "/opt/librewolf", "/usr/lib/librewolf"):
            cands.append(Path(g))
    elif sys.platform == "darwin":
        for g in ("Firefox", "Floorp", "LibreWolf", "Zen"):
            cands.append(Path(f"/Applications/{g}.app/Contents/Resources"))
    for c in cands:
        if find_omni_files(c):
            return c.parent if c.name.lower() == "browser" else c
    return None


def find_profiles():
    """Ищет профили Firefox и родственных сборок. Новые — первыми."""
    roots = []
    appdata = os.environ.get("APPDATA")
    if appdata:
        roots.append(Path(appdata))
    else:
        home = Path.home()
        roots += [
            home / ".mozilla", home / ".floorp", home / ".librewolf",
            home / ".waterfox", home / "Library" / "Application Support",
        ]
    # «mozilla» нужен для обычного Firefox: его профили лежат
    # в %APPDATA%\Mozilla\Firefox\Profiles, а не в папке с «firefox» в имени.
    names = ("mozilla", "firefox", "floorp", "librewolf", "waterfox", "zen")
    found, seen = [], set()

    def add_one(p):
        try:
            if p.is_dir() and ((p / "prefs.js").exists() or (p / "extensions.json").exists()):
                s = str(p)
                if s not in found:
                    found.append(s)
        except OSError:
            pass

    def add_from_ini(base):
        """Профили из profiles.ini, в том числе созданные в другой папке."""
        ini = base / "profiles.ini"
        try:
            if not ini.is_file():
                return
            lines = ini.read_text(encoding="utf-8", errors="ignore").splitlines()
        except OSError:
            return
        path, relative = None, True
        for line in lines + ["["]:
            line = line.strip()
            if line.startswith("["):
                if path:
                    add_one((base / path) if relative else Path(path))
                path, relative = None, True
            elif line.startswith("Path="):
                path = line[5:].strip().replace("/", os.sep)
            elif line.startswith("IsRelative="):
                relative = line[11:].strip() != "0"

    def add_profiles(base):
        if not base.is_dir() or base in seen:
            return
        seen.add(base)
        try:
            for p in base.iterdir():
                if p.is_dir() and ((p / "prefs.js").exists() or (p / "extensions.json").exists()):
                    s = str(p)
                    if s not in found:
                        found.append(s)
        except OSError:
            pass

    for root in roots:
        if not root.exists():
            continue
        add_profiles(root)
        add_profiles(root / "Profiles")
        try:
            for item in root.iterdir():
                if item.is_dir() and any(x in item.name.lower() for x in names):
                    add_from_ini(item)
                    add_from_ini(item / "Firefox")
                    add_profiles(item)
                    add_profiles(item / "Profiles")
                    add_profiles(item / "Firefox" / "Profiles")
                    try:
                        for sub in item.iterdir():
                            if sub.is_dir():
                                add_profiles(sub / "Profiles")
                    except OSError:
                        pass
        except OSError:
            pass

    def mtime(p):
        # prefs.js браузер переписывает при каждом закрытии, поэтому его
        # время точнее показывает, каким профилем пользовались последним.
        for probe in (Path(p) / "prefs.js", Path(p)):
            try:
                return probe.stat().st_mtime
            except OSError:
                continue
        return 0

    found.sort(key=mtime, reverse=True)
    return found


PROFILE_MARKERS = ("prefs.js", "extensions.json", "compatibility.ini", "times.json")


def normalize_profile(path):
    """Принимает профиль или его папку chrome, возвращает папку профиля."""
    try:
        p = Path(str(path)).expanduser()
    except (TypeError, ValueError):
        return None
    if not p.is_absolute():
        p = p.resolve()
    if p.name.lower() == "chrome" and (p.parent / "prefs.js").is_file():
        p = p.parent
    if not p.is_dir():
        return None
    return p if any((p / m).is_file() for m in PROFILE_MARKERS) else None


def read_browser_version(install_dir):
    d = Path(install_dir)
    if d.name.lower() == "browser":
        d = d.parent
    for rel in ("application.ini", "browser/application.ini", "platform.ini"):
        v = read_ini_value(d / rel, "Version") or read_ini_value(d / rel, "Milestone")
        if v:
            return v
    return "unknown"


def browser_display_name(install_dir):
    if not install_dir:
        return ""
    name = Path(install_dir).name
    low = name.lower()
    if "floorp" in low:
        return "Floorp"
    if "librewolf" in low:
        return "LibreWolf"
    if "waterfox" in low:
        return "Waterfox"
    if "zen" in low:
        return "Zen"
    if "firefox" in low:
        return "Firefox"
    return name


# ==================== СКАНИРОВАНИЕ МЕНЮ БРАУЗЕРА ====================

TAG_RE = re.compile(r'<(/?)([A-Za-z][\w:.-]*)((?:"[^"]*"|\'[^\']*\'|[^>"\'])*?)(/?)>', re.S)
ATTR_RE = re.compile(r'([\w:.-]+)\s*=\s*(?:"([^"]*)"|\'([^\']*)\')')
POPUP_TAGS = {"menupopup", "popup"}
RELEVANT_POPUP_RE = re.compile(r"context|sidebar-.*menu|split-view-menu|picontext|popupset", re.I)
JS_ID_RE = re.compile(r'["\'](context[-_][A-Za-z0-9_-]*[A-Za-z0-9_])["\']')
FTL_KEY_RE = re.compile(r"^([A-Za-z][\w-]*)\s*=\s*(.*)$")
FTL_ATTR_RE = re.compile(r"^\s+\.([\w-]+)\s*=\s*(.*)$")


def parse_ftl(text, out):
    key = None
    for line in text.splitlines():
        m = FTL_KEY_RE.match(line)
        if m:
            key = m.group(1)
            out.setdefault(key, {})["value"] = m.group(2).strip()
            continue
        m = FTL_ATTR_RE.match(line)
        if m and key:
            out[key][m.group(1)] = m.group(2).strip()


def clean_ftl_text(s):
    if not s:
        return ""
    if "->" in s:
        variants = re.findall(r"\[[^\]]+\]\s*([^{}]+?)(?=\s*\[|$)", s, re.S)
        if variants:
            s = " ".join(v.strip() for v in variants)
            return re.sub(r"\s+", " ", s).strip(" :")
    s = re.sub(r"\{[^}]*\}", "", s)
    s = re.sub(r"\(\s*\)", "", s)
    return re.sub(r"\s+", " ", s).strip(" :")


def ftl_label(ftl, l10n_id):
    entry = ftl.get(l10n_id)
    if not entry:
        return ""
    for k in ("label", "aria-label", "value", "tooltiptext"):
        t = clean_ftl_text(entry.get(k, ""))
        if t:
            return t
    return ""


def load_ftl_from(omni, ftl):
    if not omni:
        return
    try:
        names = omni.names()
    except Exception:
        return
    for n in names:
        if n.startswith("localization/en-US/") and n.endswith(".ftl"):
            try:
                parse_ftl(omni.read_text(n), ftl)
            except Exception:
                continue


# ---------- Floorp: подписи и пункты, которые вставляет JS ----------

FLOORP_LOCALE_RE = re.compile(r'(?:const|let|var)\s+([A-Za-z_$][\w$]*)\s*=\s*\{')
CYRILLIC_RE = re.compile(r"[\u0400-\u04FF]")
FLOORP_ADD_CTX_RE = re.compile(
    r'addContextBox\(\s*["\']([\w-]+)["\']\s*,\s*["\']([\w.-]+)["\']'
    r'\s*,\s*["\']([\w-]+)["\']')
FLOORP_CREATE_RE = re.compile(
    r'(?:^|[;{,(]\s*|\b(?:var|let|const)\s+)([A-Za-z_$][\w$]*)\s*=\s*'
    r'(?:document\s*\??\.\s*)?create(?:XUL)?Element\s*\??\.?\s*\(\s*'
    r'["\'](?:xul:)?(menuitem|menu|menuseparator|menugroup)["\']\s*\)')
FLOORP_DK_RE = re.compile(
    r'setProp\(\s*([A-Za-z_$][\w$]*)\s*,\s*["\']data-floorp-context-menu-key["\']'
    r'\s*,\s*["\']([^"\']+)["\']'
    r'|([A-Za-z_$][\w$]*)\s*\.\s*setAttribute\(\s*["\']data-floorp-context-menu-key["\']'
    r'\s*,\s*["\']([^"\']+)["\']')
FLOORP_TKEY_RE = re.compile(r'(?:const|let|var)\s+(translationKeys[\w$]*)\s*=\s*\{')
FLOORP_L10N_RE = re.compile(
    r'setProp\(\s*VAR\s*,\s*["\']data-l10n-id["\']\s*,\s*["\']([\w.-]+)["\']'
    r'|VAR\s*\.\s*setAttribute\(\s*["\']data-l10n-id["\']\s*,\s*["\']([\w.-]+)["\']')
FLOORP_LABEL_PREFIX = (r'setProp\(\s*VAR\s*,\s*["\']label["\']\s*,\s*'
                       r'|VAR\s*\.\s*setAttribute\(\s*["\']label["\']\s*,\s*')
FLOORP_MAKEITEM_RE = re.compile(
    r'makeItem\(\s*["\'](floorp\.[\w.-]+)["\']\s*,\s*["\']([^"\']+)["\']')

# None — пункт живёт не в контекстном меню (меню «Инструменты» и т. п.).
FLOORP_POPUP_PREFIXES = (
    ("floorp.webpanel.", "webpanel-context"),
    ("floorp.downloadbar.", "downloadsContextMenu"),
    ("floorp.tab-stacks.", "floorp-stack-kind-menu"),
    ("floorp.workspaces.move-tab", "tabContextMenu"),
    ("floorp.workspaces.open-link", "contentAreaContextMenu"),
    ("floorp.workspaces.", "workspaces-toolbar-item-context-menu"),
    ("floorp.external-browser.open-tab", "tabContextMenu"),
    ("floorp.external-browser.tab.", "tabContextMenu"),
    ("floorp.external-browser.open-link", "contentAreaContextMenu"),
    ("floorp.external-browser.link.", "contentAreaContextMenu"),
    ("floorp.split-view.layout.", None),
    ("floorp.split-view.", "tabContextMenu"),
    ("floorp.private-container.reopen-tab", "tabContextMenu"),
    ("floorp.statusbar.toggle", "toolbar-context-menu"),
    ("floorp.zen-mode.", None),
    ("floorp.share-mode.", None),
)

FLOORP_LABEL_OVERRIDES = {
    "floorp.tab-stacks.switch-kind": "Переключить тип: стек / группа",
    "floorp.tab-stacks.reload": "Обновить стек",
    "floorp.tab-stacks.new-tab": "Новая вкладка в стеке",
    "floorp.tab-stacks.manage": "Управление стеком…",
    "floorp.tab-stacks.ungroup": "Разъединить стек",
    "floorp.tab-stacks.close": "Закрыть стек",
    "floorp.downloadbar.pause": "Приостановить загрузку",
    "floorp.downloadbar.resume": "Продолжить загрузку",
    "floorp.downloadbar.unblock": "Разблокировать загрузку",
    "floorp.downloadbar.open-system-viewer": "Открыть в системном приложении",
    "floorp.downloadbar.always-open-system-viewer": "Всегда открывать в системном приложении",
    "floorp.downloadbar.always-open-similar-files": "Всегда открывать похожие файлы",
    "floorp.downloadbar.show": "Показать в папке",
    "floorp.downloadbar.open-referrer": "Перейти на страницу загрузки",
    "floorp.downloadbar.copy-location": "Копировать ссылку на загрузку",
    "floorp.downloadbar.delete-file": "Удалить файл",
    "floorp.downloadbar.remove-from-history": "Удалить из истории",
    "floorp.downloadbar.clear-list": "Очистить список",
    "floorp.downloadbar.clear-downloads": "Очистить загрузки",
}


def _flatten_locale(obj, prefix=""):
    out = {}
    if isinstance(obj, dict):
        for k, v in obj.items():
            key = f"{prefix}.{k}" if prefix else str(k)
            if isinstance(v, dict):
                out.update(_flatten_locale(v, key))
            elif isinstance(v, str):
                out[key] = v
    return out


def load_floorp_i18n(omni):
    """Русские подписи Floorp из core.js: 'privateContainer.openInPrivateContainer'."""
    try:
        text = omni.read_text("chrome/noraneko/nora-content/core.js")
    except Exception:
        return {}
    decoder = json.JSONDecoder()
    groups = {}
    for m in FLOORP_LOCALE_RE.finditer(text):
        try:
            obj, _end = decoder.raw_decode(text, m.end() - 1)
        except Exception:
            continue
        if not isinstance(obj, dict):
            continue
        flat = _flatten_locale(obj)
        if not flat:
            continue
        cyr = sum(len(CYRILLIC_RE.findall(v)) for v in flat.values())
        groups.setdefault(m.group(1).split("$", 1)[0], []).append((cyr, flat))
    out = {}
    for base, variants in groups.items():
        _cyr, flat = max(variants, key=lambda p: p[0])
        for key, value in flat.items():
            out[f"{base}.{key}"] = value
    return out


def clean_floorp_label(s):
    if not s:
        return ""
    s = re.sub(r"\\u([0-9a-fA-F]{4})",
               lambda mm: chr(int(mm.group(1), 16)), s)
    s = re.sub(r"\{\{.*?\}\}", "", s)
    s = re.sub(r"\{\s*[A-Z][A-Z0-9_()]*\s*->[^}]*\}", "", s)
    s = re.sub(r"\s+", " ", s).strip(" :")
    if re.search(r"[{}]|->", s):
        return ""
    return s


def floorp_popup_for_key(key):
    for prefix, popup in FLOORP_POPUP_PREFIXES:
        if key.startswith(prefix):
            return popup
    return ""


def _parse_js_maps(text):
    """Карты translationKeys и свойств texts() конкретного JS-файла."""
    tmaps = {}
    for mm in FLOORP_TKEY_RE.finditer(text):
        start = text.find("{", mm.start())
        depth = 0
        j = start
        while j < len(text):
            if text[j] == "{":
                depth += 1
            elif text[j] == "}":
                depth -= 1
                if depth == 0:
                    break
            j += 1
        tmaps[mm.group(1)] = {
            km.group(1): km.group(2)
            for km in re.finditer(r'(\w+)\s*:\s*["\']([\w.-]+)["\']',
                                  text[start:j + 1])
        }
    propkeys = {}
    for table in tmaps.values():
        for prop, key in table.items():
            propkeys.setdefault(prop, key)
    return tmaps, propkeys


def _next_js_arg(text, i, limit=400):
    depth = 0
    j = i
    quote = None
    while j < len(text) and j - i < limit:
        c = text[j]
        if quote:
            if c == "\\":
                j += 2
                continue
            if c == quote:
                quote = None
        elif c in "\"'`":
            quote = c
        elif c in "([{":
            depth += 1
        elif c in ")]}":
            if depth == 0:
                break
            depth -= 1
        elif c == "," and depth == 0:
            break
        j += 1
    return text[i:j].strip()


def _js_creation(text, var, before):
    best = None
    for mm in FLOORP_CREATE_RE.finditer(text, 0, before):
        if mm.group(1) == var:
            best = mm
    return (best.group(2), best.start()) if best else (None, None)


def _js_id_for_var(text, var, start, end):
    segment = text[start:end]
    patterns = (
        r'setProp\(\s*' + re.escape(var) + r'\s*,\s*["\']id["\']\s*,\s*["\']([^"\']+)["\']',
        r'(?<![\w$.])' + re.escape(var) + r'\s*\.\s*id\s*=\s*["\']([^"\']+)["\']',
        re.escape(var) + r'\s*\.\s*setAttribute\(\s*["\']id["\']\s*,\s*["\']([^"\']+)["\']',
    )
    for pattern in patterns:
        mm = re.search(pattern, segment)
        if mm:
            return mm.group(1)
    return None


def _js_signal_label(text, var, anchor, i18n):
    best = None
    pattern = (r'\[\s*' + re.escape(var) + r'\s*,\s*\w+\s*\]\s*=\s*createSignal\(\s*'
               r'(?:instance\.t|t[\w$]*)\(\s*([^)]{1,200})\)')
    for mm in re.finditer(pattern, text):
        distance = abs(mm.start() - anchor)
        if best is None or distance < best[0]:
            best = (distance, mm.group(1))
    if best is None:
        return ""
    arg = best[1]
    km = re.search(r'["\']([\w.-]+)["\']', arg)
    if km and i18n.get(km.group(1)):
        return i18n[km.group(1)]
    vm = re.fullmatch(r"([A-Za-z_$][\w$]*)\s*", arg)
    if vm:
        km = re.search(r'(?:const|let|var)\s+' + re.escape(vm.group(1))
                       + r'\s*=\s*["\']([\w.-]+)["\']', text)
        if km:
            return i18n.get(km.group(1), "")
    return ""


def _js_resolve_var(text, var, anchor, i18n, tmaps, propkeys):
    best = None
    for mm in re.finditer(re.escape(var) + r'\s*=\s*([^;,\n]{0,260})', text):
        distance = abs(mm.start() - anchor)
        if best is None or distance < best[0]:
            best = (distance, mm.group(1))
    if best is None:
        return ""
    expr = best[1]
    mm = re.search(r'(?:instance\.t|translate|getTranslatedText|t[\w$]*)\s*'
                   r'\(\s*["\']([\w.-]+)["\']', expr)
    if mm:
        return i18n.get(mm.group(1), "")
    props = re.findall(r'texts\(\)\.(\w+)', expr)
    if props:
        key = propkeys.get(props[-1])
        if key:
            return i18n.get(key, "")
    mm = re.search(r'translationKeys[\w$]*\.(\w+)', expr)
    if mm:
        for table in tmaps.values():
            if mm.group(1) in table:
                return i18n.get(table[mm.group(1)], "")
    if "label()" in expr:
        return _js_signal_label(text, "label", anchor, i18n)
    return ""


def _js_label_for_var(text, var, pos, tag, i18n, tmaps, propkeys, ftl):
    if tag == "menuseparator":
        return ""
    rx = re.compile(FLOORP_LABEL_PREFIX.replace("VAR", re.escape(var)))
    best = None
    for mm in rx.finditer(text):
        if best is None or abs(mm.start() - pos) < abs(best.start() - pos):
            best = mm
    if best is not None:
        expr = _next_js_arg(text, best.end())
        mm = re.search(r'(?:instance\.t|translate|getTranslatedText|t[\w$]*)\s*'
                       r'\(\s*["\']([\w.-]+)["\']', expr)
        if mm:
            return i18n.get(mm.group(1), "")
        mm = re.search(r'texts\(\)\.(\w+)', expr)
        if mm:
            key = propkeys.get(mm.group(1))
            if key:
                return i18n.get(key, "")
        mm = re.search(r'translationKeys[\w$]*\.(\w+)', expr)
        if mm:
            for table in tmaps.values():
                if mm.group(1) in table:
                    return i18n.get(table[mm.group(1)], "")
        mm = re.fullmatch(r'["\']([^"\']{2,})["\']', expr)
        if mm:
            return mm.group(1)
        mm = re.fullmatch(r"([A-Za-z_$][\w$]*)", expr)
        if mm:
            label = _js_resolve_var(text, mm.group(1), best.start(),
                                    i18n, tmaps, propkeys)
            if label:
                return label
        if expr.endswith("()"):
            label = _js_signal_label(text, expr[:-2], best.start(), i18n)
            if label:
                return label
    rx = re.compile(FLOORP_L10N_RE.pattern.replace("VAR", re.escape(var)))
    for mm in rx.finditer(text):
        if abs(mm.start() - pos) < 6000:
            l10n_id = mm.group(1) or mm.group(2)
            label = clean_floorp_label(ftl_label(ftl, l10n_id))
            if label:
                return label
    return ""


def _merge_dynamic_item(found, key, label, tag, popup, selector=None):
    info = found.get(key)
    if info is None:
        info = {
            "popup": popup, "label": label, "tag": tag,
            "dynamic": True, "hidden": False,
        }
        if selector:
            info["selector"] = selector
        found[key] = info
        return
    if label and not info.get("label"):
        info["label"] = label
    if tag and info.get("tag") in ("", "?", None):
        info["tag"] = tag
    if popup and not info.get("popup"):
        info["popup"] = popup
    info["dynamic"] = True


def scan_floorp_js(omni, ftl, found):
    """Пункты, которые Floorp создаёт из JS, включая подписи из core.js."""
    try:
        names = omni.names()
    except Exception:
        return
    i18n = load_floorp_i18n(omni)
    for name in names:
        if not (name.startswith("chrome/noraneko/") and name.endswith(".js")):
            continue
        try:
            text = omni.read_text(name)
        except Exception:
            continue
        if ("data-floorp-context-menu-key" not in text
                and "addContextBox(" not in text
                and 'makeItem("floorp.' not in text):
            continue
        tmaps, propkeys = _parse_js_maps(text)

        for mm in FLOORP_ADD_CTX_RE.finditer(text):
            iid, l10n_key, anchor = mm.groups()
            popup = "contentAreaContextMenu" if anchor.startswith("context") else ""
            label = clean_floorp_label(i18n.get(l10n_key, ""))
            _merge_dynamic_item(found, iid, label, "menuitem", popup)

        for mm in FLOORP_DK_RE.finditer(text):
            var = mm.group(1) or mm.group(3)
            key = mm.group(2) or mm.group(4)
            pos = mm.start()
            popup = floorp_popup_for_key(key)
            if popup is None:
                continue
            tag, created_at = _js_creation(text, var, pos)
            if not tag:
                continue
            iid = _js_id_for_var(text, var, created_at, pos)
            label = FLOORP_LABEL_OVERRIDES.get(key, "")
            if not label:
                label = clean_floorp_label(
                    _js_label_for_var(text, var, pos, tag, i18n,
                                      tmaps, propkeys, ftl))
            if iid:
                _merge_dynamic_item(found, iid, label, tag, popup)
            else:
                selector = '[data-floorp-context-menu-key="%s"]' % key
                _merge_dynamic_item(found, SEL_PREFIX + selector,
                                    label, tag, popup, selector)

        for mm in FLOORP_MAKEITEM_RE.finditer(text):
            key, raw_label = mm.groups()
            popup = floorp_popup_for_key(key)
            if popup is None:
                continue
            label = FLOORP_LABEL_OVERRIDES.get(
                key, clean_floorp_label(raw_label))
            selector = '[data-floorp-context-menu-key="%s"]' % key
            _merge_dynamic_item(found, SEL_PREFIX + selector,
                                label, "menuitem", popup, selector)


def scan_xhtml(text, ftl, found):
    """Разбирает один xhtml/inc: ищет пункты внутри контекстных menupopup."""
    text = re.sub(r"<!--.*?-->", "", text, flags=re.S)
    stack = []
    for m in TAG_RE.finditer(text):
        closing, name, attrs_raw, selfclose = m.groups()
        lname = name.lower()
        if lname in POPUP_TAGS:
            if closing:
                if stack:
                    stack.pop()
            elif not selfclose:
                attrs = {a[0]: (a[1] or a[2]) for a in ATTR_RE.findall(attrs_raw)}
                stack.append(attrs.get("id"))
            continue
        if closing or lname not in ITEM_TAGS:
            continue
        attrs = {a[0]: (a[1] or a[2]) for a in ATTR_RE.findall(attrs_raw)}
        iid = attrs.get("id")
        if not iid:
            continue
        root = next((p for p in stack if p and RELEVANT_POPUP_RE.search(p)), None)
        if not root:
            continue
        label = ""
        for l10n_attr in ("data-l10n-id", "data-lazy-l10n-id", "data-alt-l10n-id"):
            if attrs.get(l10n_attr):
                label = ftl_label(ftl, attrs[l10n_attr])
                if label:
                    break
        if not label and attrs.get("label") and "&" not in attrs["label"]:
            label = attrs["label"]
        hidden = attrs.get("hidden") == "true"
        prev = found.get(iid)
        if prev is None:
            found[iid] = {
                "popup": root, "label": label, "tag": lname, "hidden": hidden,
            }
        else:
            if label and not prev["label"]:
                prev["label"] = label
            if not prev.get("popup"):
                prev["popup"] = root


def scan_browser(install_dir):
    """Возвращает ({id: {popup, label, tag, hidden}}, версия браузера)."""
    omnis = find_omni_files(install_dir)
    if not omnis:
        raise FileNotFoundError("omni.ja не найден в " + str(install_dir))
    browser_omni, platform_omni = omnis
    ftl = {}
    found = {}
    js_ids = set()

    # Локализация из platform omni нужна раньше разбора xhtml: там лежат
    # подписи буфера обмена, орфографии и паролей.
    load_ftl_from(Omni(platform_omni) if platform_omni else None, ftl)
    if browser_omni:
        ob = Omni(browser_omni)
        names = ob.names()
        load_ftl_from(ob, ftl)
        for n in names:
            if n.startswith("chrome/browser/content/") and n.endswith((".xhtml", ".xul", ".inc")):
                try:
                    scan_xhtml(ob.read_text(n), ftl, found)
                except Exception:
                    continue
        for n in names:
            base = n.rsplit("/", 1)[-1]
            if n.startswith("chrome/") and base in ("nsContextMenu.sys.mjs", "browser-context.js"):
                try:
                    js_ids.update(JS_ID_RE.findall(ob.read_text(n)))
                except Exception:
                    pass

    if platform_omni:
        try:
            scan_floorp_js(Omni(platform_omni), ftl, found)
        except Exception:
            pass

    for iid in js_ids:
        if iid not in found and not iid.endswith("-popup"):
            found[iid] = {
                "popup": None, "label": "", "tag": "?", "dynamic": True, "hidden": False,
            }
    for info in found.values():
        info.setdefault("dynamic", False)
        info.setdefault("hidden", False)
        info.setdefault("popup", None)
        info.setdefault("label", "")
        info.setdefault("tag", "?")
    return found, read_browser_version(install_dir)


# ==================== КЭШ: что нового после обновления ====================


def app_data_dir():
    base = os.environ.get("APPDATA")
    root = Path(base) if base else Path.home() / ".config"
    return root / "ContextMenuHider"


def cache_path():
    return app_data_dir() / "known_ids.json"


def config_path():
    return app_data_dir() / "config.json"


def load_config():
    p = config_path()
    try:
        if p.is_file():
            data = json.loads(p.read_text(encoding="utf-8"))
            if isinstance(data, dict):
                return data
    except Exception:
        pass
    return {}


def save_config(data):
    p = config_path()
    try:
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(json.dumps(data, ensure_ascii=False, indent=1), encoding="utf-8")
    except OSError:
        pass


def update_cache_and_find_new(install_dir, version, ids):
    """Сохраняет список ID для версии и возвращает (новые ID, прошлая версия)."""
    p = cache_path()
    try:
        data = json.loads(p.read_text(encoding="utf-8")) if p.exists() else {}
    except Exception:
        data = {}
    key = str(install_dir)
    entry = data.get(key) or {}
    if entry.get("version") != version:
        entry = {
            "version": version,
            "ids": sorted(ids),
            "prev_version": entry.get("version"),
            "prev_ids": entry.get("ids", []),
        }
    else:
        entry["ids"] = sorted(ids)
    data[key] = entry
    try:
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(json.dumps(data, ensure_ascii=False, indent=1), encoding="utf-8")
    except OSError:
        pass
    prev = set(entry.get("prev_ids") or [])
    new = set(ids) - prev if prev else set()
    return new, entry.get("prev_version")


# ==================== РАСШИРЕНИЯ ====================

BUILTIN_LOCATIONS = ("app-builtin", "app-system-defaults", "app-system-addons")


def make_widget_id(ext_id):
    """Та же функция, что makeWidgetId в ext-browser.js Firefox."""
    return re.sub(r"[^a-z0-9_-]", "_", ext_id.lower())


def resolve_msg_name(addon, profile):
    name = (addon.get("defaultLocale") or {}).get("name") or addon.get("id", "?")
    if not name.startswith("__MSG_"):
        return name
    key = name[6:-2].lower()
    paths = []
    if addon.get("path"):
        paths.append(Path(addon["path"]))
    paths.append(Path(profile) / "extensions" / (addon.get("id", "") + ".xpi"))
    for xpi in paths:
        try:
            if not xpi.is_file():
                continue
            with zipfile.ZipFile(xpi) as z:
                man = json.loads(z.read("manifest.json").decode("utf-8-sig"))
                for loc in ("ru", "en_US", "en", man.get("default_locale", "en")):
                    try:
                        msgs = json.loads(
                            z.read(f"_locales/{loc}/messages.json").decode("utf-8-sig"))
                    except KeyError:
                        continue
                    for k, v in msgs.items():
                        if k.lower() == key and "message" in v:
                            return v["message"]
        except Exception:
            continue
    return addon.get("id", name)


def load_extensions(profile):
    p = Path(profile) / "extensions.json"
    if not p.is_file():
        return []
    try:
        data = json.loads(p.read_text(encoding="utf-8"))
    except Exception:
        return []
    result = []
    for a in data.get("addons", []):
        if a.get("type") != "extension" or a.get("location") in BUILTIN_LOCATIONS:
            continue
        eid = a.get("id", "")
        if not eid:
            continue
        result.append({
            "id": eid,
            "wid": make_widget_id(eid),
            "name": resolve_msg_name(a, profile),
            "active": bool(a.get("active")) and not a.get("userDisabled"),
        })
    result.sort(key=lambda e: (not e["active"], e["name"].lower()))
    return result


# ==================== КАТЕГОРИИ И ЗАПИСИ ДЛЯ ИНТЕРФЕЙСА ====================


def infer_category(iid, popup, dynamic=False, tag="?"):
    if iid in KNOWN_CAT:
        return KNOWN_CAT[iid]
    if popup == "tabContextMenu":
        return CAT_TABS
    if popup == "contentAreaContextMenu" or dynamic:
        s = iid.lower()
        if "pdfjs" in s:
            return CAT_PDF
        if "visual-search" in s or "imagetext" in s:
            return CAT_IMAGES
        if "frame" in s:
            return CAT_FRAMES
        if any(x in s for x in (
            "openlink", "savelink", "copylink", "-email", "-phone",
            "private_container", "privatecontainer", "workspace", "inworkspace",
        )):
            return CAT_LINKS
        if any(x in s for x in ("image", "bgimage", "setbackground")):
            return CAT_IMAGES
        if any(x in s for x in (
            "media", "video", "audio", "-play", "-pause", "-mute", "-unmute",
            "fullscreen", "pictureinpicture", "showcontrols", "hidecontrols", "-loop",
        )):
            if "leave-dom-fullscreen" in s:
                return CAT_PAGE
            return CAT_MEDIA
        if any(x in s for x in (
            "searchselect", "translate", "selection", "highlight", "bidi-text",
            "ask-chat", "get-email", "search",
        )):
            return CAT_TEXT
        if any(x in s for x in (
            "undo", "redo", "cut", "paste", "delete", "selectall", "spell",
            "password", "login", "relay",
        )):
            return CAT_INPUTS
        if any(x in s for x in ("tab",)):
            return CAT_TABS
        if any(x in s for x in ("back", "forward", "reload", "stop", "bookmarkpage", "navigation")):
            return CAT_NAV
        if any(x in s for x in (
            "inspect", "source", "screenshot", "savepage", "sendpage", "pageinfo",
            "print", "bidi-page", "add-engine", "aitab", "mini-window",
        )):
            return CAT_PAGE
        return CAT_PAGE_OTHER
    if popup:
        mapped = POPUP_CATEGORY_MAP.get(popup)
        if mapped:
            return mapped
        low = popup.lower()
        if low.startswith("sidebar-bookmarks") or low.startswith("sidebar-history"):
            return CAT_PLACES
        if low.startswith("sidebar-opentabs"):
            return CAT_SIDEBAR_TABS
        if low.startswith("sidebar-synced") or low.startswith("syncedtabs"):
            return CAT_SIDEBAR_TABS
        if low.startswith("sidebar"):
            return CAT_SIDEBAR
        return CAT_OTHER_MENUS
    if dynamic:
        return CAT_CODE
    return CAT_OTHER_MENUS


def label_for_item(iid, info):
    label = KNOWN_LABEL.get(iid)
    if label:
        return label
    label = (info or {}).get("label", "") if info else ""
    if label:
        return label
    tag = (info or {}).get("tag", "") if info else ""
    if tag == "menuseparator":
        return "Разделитель"
    if tag == "menugroup":
        return "Группа пунктов"
    if tag == "menu":
        return "Подменю"
    if (info or {}).get("dynamic"):
        return "Пункт из кода"
    return iid


def make_entry(key, label, id_text, category, kind="browser", tag="",
               inactive=False, missing=False, popup=None, origin=None):
    is_sep = tag == "menuseparator"
    is_menu = tag == "menu"
    search = " ".join(filter(None, [
        label, id_text, category, origin or "", popup or "", tag,
        CATEGORY_SYNONYMS.get(category, ""),
    ])).lower()
    return {
        "key": key,
        "label": label,
        "id_text": id_text,
        "category": category,
        "origin": origin or category,
        "kind": kind,
        "tag": tag,
        "is_separator": is_sep,
        "is_menu": is_menu,
        "inactive": inactive,
        "missing": missing,
        "popup": popup,
        "search": search,
    }


def build_entries(scanned, scan_ok, extensions, new_ids):
    """Собирает все пункты для интерфейса."""
    entries = {}
    # Пункты из словаря (в том числе отсутствующие в этой версии).
    for category, items in KNOWN_MENU_ITEMS.items():
        for iid, label in items:
            info = scanned.get(iid)
            if scan_ok and not info:
                e = make_entry(
                    iid, label, iid, CAT_MISSING, tag="",
                    missing=True, origin=category, popup=None,
                )
            else:
                info = info or {}
                e = make_entry(
                    iid, label, iid, category, tag=info.get("tag", ""),
                    popup=info.get("popup"),
                )
            e["new"] = iid in new_ids
            e["dynamic"] = bool((info or {}).get("dynamic"))
            entries[iid] = e

    # Найденные в браузере, но не описанные в словаре.
    for iid, info in scanned.items():
        if iid in entries:
            continue
        category = infer_category(iid, info.get("popup"), info.get("dynamic", False),
                                  info.get("tag", ""))
        label = label_for_item(iid, info)
        if iid.startswith(SEL_PREFIX):
            selector = info.get("selector") or iid[len(SEL_PREFIX):]
            e = make_entry(
                iid, label, selector, category, kind="selector",
                tag=info.get("tag", ""), popup=info.get("popup"),
            )
            e["selector"] = selector
        else:
            e = make_entry(
                iid, label, iid, category, tag=info.get("tag", ""),
                popup=info.get("popup"),
            )
        e["new"] = iid in new_ids
        e["dynamic"] = bool(info.get("dynamic"))
        entries[iid] = e

    # Особые правила расширений.
    entries[SPECIAL_ALL_EXT] = make_entry(
        SPECIAL_ALL_EXT,
        "Скрыть все пункты от расширений",
        "webextension-menuitem",
        CAT_EXTENSIONS, kind="special", tag="special",
    )
    entries[SPECIAL_ALL_EXT]["search"] += " все расширения целиком any extension hide all"
    entries[SPECIAL_EXT_SEP] = make_entry(
        SPECIAL_EXT_SEP,
        "Скрыть только разделители расширений",
        "menuseparator.webextension-menuitem",
        CAT_EXTENSIONS, kind="special", tag="special",
    )
    entries[SPECIAL_EXT_SEP]["search"] += " разделители расширения separators"

    for ext in extensions:
        key = EXT_PREFIX + ext["wid"]
        e = make_entry(
            key, ext["name"], ext["wid"], CAT_EXTENSIONS, kind="ext",
            inactive=not ext["active"], tag="ext",
        )
        e["ext_id"] = ext["id"]
        e["search"] += " " + ext["id"].lower() + " расширение extension"
        entries[key] = e
    return entries


def entry_sort_key(entry):
    cat = entry["category"]
    origin = entry.get("origin") or cat
    ext_idx = len(CATEGORY_ORDER)
    if entry.get("kind") == "special":
        return (ext_idx, 0, 0, entry["label"].lower())
    if entry.get("kind") == "ext":
        return (ext_idx, 1, 0 if not entry["inactive"] else 1,
                entry["label"].lower())
    if cat == CAT_MISSING:
        origin_idx = CATEGORY_ORDER.index(origin) if origin in CATEGORY_ORDER else 999
        return (5000, origin_idx, KNOWN_POS.get(entry["key"], 999), entry["label"].lower())
    cat_idx = CATEGORY_ORDER.index(cat) if cat in CATEGORY_ORDER else 999
    return (cat_idx, 0, KNOWN_POS.get(entry["key"], 999), entry["label"].lower())


# ==================== ГЕНЕРАЦИЯ И РАЗБОР CSS ====================

IDENT_RE = re.compile(r"-?[A-Za-z_][\w-]*$")


def id_selector(iid):
    if IDENT_RE.match(iid):
        return "#" + iid
    return '[id="%s"]' % iid.replace('"', "")


def clean_comment_text(text):
    return re.sub(r"[\r\n]+", " ", text).replace("*/", "")


def generate_css(keys, ext_names=None, raw_selectors=None):
    ext_names = ext_names or {}
    raw_selectors = [s for s in (raw_selectors or []) if s.strip()]
    keys = list(keys or [])
    normal = []
    managed_selectors = []
    seen = set()
    for k in keys:
        if k.startswith(SEL_PREFIX):
            selector = k[len(SEL_PREFIX):].strip()
            if selector and selector not in managed_selectors:
                managed_selectors.append(selector)
            continue
        if k.startswith("__") or k.startswith(EXT_PREFIX) or k in seen:
            continue
        seen.add(k)
        normal.append(k)
    exts = []
    for k in keys:
        wid = k[len(EXT_PREFIX):] if k.startswith(EXT_PREFIX) else None
        if wid and wid not in exts:
            exts.append(wid)
    now = datetime.now().strftime("%Y-%m-%d %H:%M:%S")

    lines = [
        BEGIN_MARK,
        f"/* Сгенерировано {APP_NAME} {APP_VERSION}: {now} */",
    ]
    has_rules = False
    if normal:
        has_rules = True
        lines.append("")
        lines.append(",\n".join(id_selector(i) for i in normal) + " {")
        lines.append("  display: none !important;")
        lines.append("}")

    if managed_selectors:
        has_rules = True
        lines += [
            "",
            "/* Пункты Floorp без постоянного ID */",
            ",\n".join(managed_selectors) + " {",
            "  display: none !important;",
            "}",
        ]

    if SPECIAL_ALL_EXT in keys:
        has_rules = True
        lines += [
            "",
            "/* Все пункты меню от расширений */",
            ".webextension-menuitem,",
            'menu[ext-type="top-level-menu"] {',
            "  display: none !important;",
            "}",
        ]
    if SPECIAL_EXT_SEP in keys:
        has_rules = True
        lines += [
            "",
            "/* Разделители от расширений */",
            "menuseparator.webextension-menuitem {",
            "  display: none !important;",
            "}",
        ]
    for wid in exts:
        name = clean_comment_text(str(ext_names.get(wid, wid)))
        has_rules = True
        lines += [
            "",
            f"/* Расширение: {name} */",
            f'[id^="{wid}-menuitem-"],',
            f'menu[ext-type="top-level-menu"]:has(> menupopup > [id^="{wid}-menuitem-"]) {{',
            "  display: none !important;",
            "}",
        ]

    if raw_selectors:
        has_rules = True
        lines += [
            "",
            CUSTOM_SECTION_COMMENT,
        ]
        plain = []
        full = []
        seen = set()
        for sel in raw_selectors:
            sel = sel.strip().rstrip(",")
            if not sel or sel in seen:
                continue
            seen.add(sel)
            if "{" in sel:
                full.append(sel)
            else:
                plain.append(sel)
        if plain:
            lines.append(",\n".join(plain) + " {")
            lines.append("  display: none !important;")
            lines.append("}")
        for rule in full:
            lines.append(rule)

    if not has_rules:
        lines.append("/* Нет отмеченных пунктов */")
    lines.append(END_MARK)
    lines.append("")
    return "\n".join(lines)


RULE_RE = re.compile(r"([^{}]+)\{([^{}]*)\}")


def parse_hidden(css_text):
    """Возвращает (ids, ext_wids, all_ext, ext_sep) из активных правил display:none."""
    text = re.sub(r"/\*.*?\*/", "", css_text, flags=re.S)
    ids, wids = set(), set()
    all_ext = ext_sep = False
    for m in RULE_RE.finditer(text):
        sels, body = m.group(1), m.group(2)
        if not re.search(r"display\s*:\s*none", body):
            continue
        for sel in sels.split(","):
            sel = sel.strip()
            if not sel:
                continue
            if sel == ".webextension-menuitem":
                all_ext = True
                continue
            if sel == "menuseparator.webextension-menuitem":
                ext_sep = True
                continue
            if sel.startswith("menu[ext-type"):
                continue
            wm = re.fullmatch(r'\[id\^=["\']([^"\']+)-menuitem-["\']\]', sel)
            if wm:
                wids.add(wm.group(1))
                continue
            hm = re.findall(r"#([\w-]+)(?![\w-])", sel)
            if hm:
                ids.add(hm[-1])
                continue
            am = re.fullmatch(r'\[id=["\']([^"\']+)["\']\]', sel)
            if am:
                ids.add(am.group(1))
    return ids, wids, all_ext, ext_sep


def parse_hidden_selectors(css_text):
    """Селекторы из активных display:none (нужны для пунктов без ID)."""
    text = re.sub(r"/\*.*?\*/", "", css_text or "", flags=re.S)
    found = set()
    for m in RULE_RE.finditer(text):
        if not re.search(r"display\s*:\s*none", m.group(2)):
            continue
        for sel in m.group(1).split(","):
            sel = sel.strip()
            if not sel:
                continue
            if re.fullmatch(r"#[\w-]+", sel):
                continue
            if re.fullmatch(r'\[id=["\'][^"\']+["\']\]', sel):
                continue
            found.add(sel)
    return found


def extract_custom_rules(css_text):
    """Содержимое секции «Свои ID и селекторы» для обратного чтения в редактор."""
    text = css_text or ""
    idx = text.find(CUSTOM_SECTION_COMMENT)
    if idx >= 0:
        section = text[idx + len(CUSTOM_SECTION_COMMENT):]
        end = section.find(END_MARK)
        if end >= 0:
            section = section[:end]
    else:
        section = text
    section = re.sub(r"/\*.*?\*/", "", section, flags=re.S)
    out, seen = [], set()

    def add(line):
        line = line.strip()
        if line and line not in seen:
            seen.add(line)
            out.append(line)

    for m in RULE_RE.finditer(section):
        selectors = [s.strip() for s in m.group(1).split(",") if s.strip()]
        if not selectors:
            continue
        body = re.sub(r"\s+", " ", m.group(2)).strip()
        display_only = bool(re.fullmatch(
            r"display\s*:\s*none\s*(?:!important\s*)?;?", body))
        if not display_only:
            add(", ".join(selectors) + (" { " + body + " }" if body else " {}"))
            continue
        for sel in selectors:
            if sel in (".webextension-menuitem",
                       "menuseparator.webextension-menuitem"):
                continue
            if sel.startswith("menu[ext-type"):
                continue
            if re.fullmatch(r'\[id\^=["\'][^"\']+-menuitem-["\']\]', sel):
                continue
            if re.fullmatch(r"#[\w-]+", sel):
                continue
            if re.fullmatch(r'\[id=["\'][^"\']+["\']\]', sel):
                continue
            if sel.startswith("[data-floorp-context-menu-key="):
                continue
            add(sel)
    return out


def split_managed(text):
    """(наша управляемая секция, всё остальное)"""
    if BEGIN_MARK in text and END_MARK in text:
        s = text.find(BEGIN_MARK)
        e = text.find(END_MARK) + len(END_MARK)
        return text[s:e], text[:s] + text[e:]
    return "", text


def mask_managed(text):
    """Заменяет управляемую секцию пробелами, сохраняя номера строк."""
    if BEGIN_MARK in text and END_MARK in text:
        s = text.find(BEGIN_MARK)
        e = text.find(END_MARK) + len(END_MARK)
        return text[:s] + re.sub(r"[^\n]", " ", text[s:e]) + text[e:]
    return text


CSS_ISSUE_LABELS = {
    "unclosed": "не хватает закрывающей «}» для блока, открытого здесь",
    "extra": "лишняя закрывающая «}»",
    "comment": "не закрыт комментарий /* … */",
    "string": "не закрыта кавычка",
}


def _css_scan(text):
    """Сырые ошибки CSS: (список (позиция, вид), стек открытых «{», fatal)."""
    text = text or ""
    raw = []
    stack = []
    fatal = False
    i, n = 0, len(text)
    while i < n:
        ch = text[i]
        if ch == "/" and text.startswith("/*", i):
            end = text.find("*/", i + 2)
            if end < 0:
                raw.append((i, "comment"))
                fatal = True
                break
            i = end + 2
            continue
        if ch in "\"'":
            quote = ch
            j = i + 1
            closed = False
            while j < n:
                c = text[j]
                if c == "\\":
                    j += 2
                    continue
                if c == quote:
                    closed = True
                    j += 1
                    break
                if c == "\n":
                    break
                j += 1
            if not closed:
                raw.append((i, "string"))
                fatal = True
                break
            i = j
            continue
        if ch == "{":
            stack.append(i)
        elif ch == "}":
            if stack:
                stack.pop()
            else:
                raw.append((i, "extra"))
        i += 1
    if not fatal:
        raw.extend((pos, "unclosed") for pos in stack)
    return raw, stack, fatal


def css_issues(text):
    """Структурные ошибки CSS: список (строка, вид, фрагмент строки)."""
    text = text or ""
    raw, _stack, _fatal = _css_scan(text)
    raw.sort(key=lambda item: item[0])
    lines = text.splitlines() or [""]
    out = []
    for pos, kind in raw:
        line = text.count("\n", 0, pos) + 1
        snippet = lines[line - 1].strip() if line <= len(lines) else ""
        if len(snippet) > 110:
            snippet = snippet[:107] + "…"
        out.append((line, kind, snippet))
    return out


def _repair_fragment(segment):
    """Закрывает незакрытые блоки/строки/комментарии в конце фрагмента."""
    raw, stack, _fatal = _css_scan(segment)
    if any(kind == "extra" for _pos, kind in raw):
        return None, 0
    if not raw and not stack:
        return segment, 0
    fixed = segment
    fixes = 0
    inserts = []
    for pos, kind in raw:
        if kind != "string":
            continue
        nl = fixed.find("\n", pos)
        inserts.append((nl if nl >= 0 else len(fixed), fixed[pos]))
        fixes += 1
    for at, quote in sorted(inserts, reverse=True):
        fixed = fixed[:at] + quote + fixed[at:]
    if any(kind == "comment" for _pos, kind in raw):
        fixed = fixed.rstrip() + "\n*/"
        fixes += 1
    if stack:
        fixed = fixed.rstrip() + "\n" + "\n".join("}" for _ in stack) + "\n"
        fixes += len(stack)
    return fixed, fixes


def repair_css(text):
    """Пытается исправить внешний CSS и сохранить секцию программы.

    Возвращает (исправленный текст, число исправлений) или (None, 0),
    если есть лишние «}» или структуру нельзя восстановить безопасно.
    """
    text = text or ""
    s = text.find(BEGIN_MARK)
    e = text.find(END_MARK, s if s >= 0 else 0)
    if s >= 0 and e > s:
        e += len(END_MARK)
        before, managed, after = text[:s], text[s:e], text[e:]
        fixed_before, n_before = _repair_fragment(before)
        if fixed_before is None:
            return None, 0
        fixed_after, n_after = _repair_fragment(after)
        if fixed_after is None:
            return None, 0
        if n_before and fixed_before.strip():
            fixed_before = fixed_before.rstrip("\n") + "\n\n"
        result = fixed_before + managed + fixed_after
        fixes = n_before + n_after
    else:
        result, fixes = _repair_fragment(text)
        if result is None:
            return None, 0
    if css_issues(mask_managed(result)):
        return None, 0
    return result, fixes


def repair_custom_lines(text):
    """Закрывает незакрытые блоки в однострочных правилах редактора."""
    out = []
    fixes = 0
    for line in (text or "").splitlines():
        raw, stack, _fatal = _css_scan(line)
        if any(kind != "unclosed" for _pos, kind in raw):
            return None, 0
        if stack:
            body = line.rstrip()
            if ":" not in body.rsplit("{", 1)[-1]:
                return None, 0
            line = body + "}" * len(stack)
            fixes += len(stack)
        out.append(line)
    fixed = "\n".join(out)
    if css_issues(fixed):
        return None, 0
    return fixed, fixes


def format_css_issues(issues, limit=4):
    out = []
    for line, kind, snippet in issues[:limit]:
        out.append(f"строка {line}: {CSS_ISSUE_LABELS.get(kind, kind)}")
        if snippet:
            out.append(f"    {snippet}")
    if len(issues) > limit:
        out.append(f"…и ещё {len(issues) - limit}")
    return "\n".join(out)


def parse_custom_lines(text):
    """Возвращает (ids, селекторы, строки) из редактора своих ID."""
    ids, selectors, lines = [], [], []
    seen = set()
    for raw in (text or "").splitlines():
        line = raw.strip()
        if not line or line.startswith("//"):
            continue
        line = line.rstrip(",")
        if not line:
            continue
        if line.startswith("#") and IDENT_RE.fullmatch(line[1:]):
            key = line[1:]
        elif IDENT_RE.fullmatch(line):
            key = line
        else:
            key = None
        if key:
            if key not in seen:
                seen.add(key)
                ids.append(key)
                lines.append(line)
        else:
            if line not in seen:
                seen.add(line)
                selectors.append(line)
                lines.append(line)
    return ids, selectors, lines


def userchrome_pref_state(profile, browser_name=""):
    """True/False/None — включены ли стили userChrome в этом профиле.

    user.js применяется после prefs.js, поэтому его значение главнее.
    Если параметр нигде не задан, у обычного Firefox он по умолчанию
    выключен (False); у Floorp, Zen и других сборок значение по умолчанию
    может отличаться, поэтому для них возвращается None."""
    state = None
    for name in ("prefs.js", "user.js"):
        p = Path(profile) / name
        if not p.is_file():
            continue
        try:
            text = p.read_text(encoding="utf-8", errors="ignore")
        except OSError:
            continue
        for line in text.splitlines():
            if line.lstrip().startswith("//"):
                continue
            if "toolkit.legacyUserProfileCustomizations.stylesheets" in line:
                if re.search(r",\s*true\s*\)", line):
                    state = True
                elif re.search(r",\s*false\s*\)", line):
                    state = False
    if state is None and browser_name == "Firefox":
        return False
    return state


# ==================== ИНТЕРФЕЙС ====================

PALETTES = {
    "dark": {
        "bg": "#0e1319",
        "sidebar": "#0a0f15",
        "panel": "#151c25",
        "panel_alt": "#1b2430",
        "input": "#0b1118",
        "hover": "#1b2634",
        "active": "#1f2c3c",
        "border": "#26323f",
        "border_soft": "#1d2732",
        "text": "#e7edf4",
        "muted": "#93a1b3",
        "dim": "#66748a",
        "accent": "#ff7a3d",
        "accent_hover": "#ff935f",
        "on_accent": "#1a0b03",
        "green": "#4ade80",
        "green_bg": "#10291d",
        "blue": "#7ab7ff",
        "blue_bg": "#10243a",
        "amber": "#ffc457",
        "amber_bg": "#2e2410",
        "red": "#ff8f85",
        "red_bg": "#331714",
        "scroll": "#2a3644",
        "scroll_hover": "#3a4a5d",
        "selection": "#27405e",
    },
    "light": {
        "bg": "#f3f5f8",
        "sidebar": "#e9edf3",
        "panel": "#ffffff",
        "panel_alt": "#f1f4f8",
        "input": "#ffffff",
        "hover": "#eef2f7",
        "active": "#e3eaf4",
        "border": "#d3dbe6",
        "border_soft": "#e6ebf2",
        "text": "#17202b",
        "muted": "#5b6b7f",
        "dim": "#8a97a8",
        "accent": "#e85d04",
        "accent_hover": "#ff7a1f",
        "on_accent": "#ffffff",
        "green": "#1a7f4b",
        "green_bg": "#e2f5ea",
        "blue": "#2563a8",
        "blue_bg": "#e4eefb",
        "amber": "#a16207",
        "amber_bg": "#fbf1d9",
        "red": "#b42318",
        "red_bg": "#fde8e6",
        "scroll": "#c3cedb",
        "scroll_hover": "#a9b8c9",
        "selection": "#cfe3ff",
    },
}


def enable_dpi_awareness():
    if sys.platform != "win32":
        return
    try:
        import ctypes
        ctypes.windll.shcore.SetProcessDpiAwareness(1)
    except Exception:
        try:
            import ctypes
            ctypes.windll.user32.SetProcessDPIAware()
        except Exception:
            pass


def apply_tk_scaling(root):
    try:
        dpi = root.winfo_fpixels("1i")
        root.tk.call("tk", "scaling", max(1.0, dpi / 72.0))
    except Exception:
        pass


def set_windows_dark_titlebar(root, dark):
    if sys.platform != "win32":
        return
    try:
        import ctypes
        hwnd = ctypes.windll.user32.GetParent(root.winfo_id())
        value = ctypes.c_int(1 if dark else 0)
        for attr in (20, 19):
            if ctypes.windll.dwmapi.DwmSetWindowAttribute(
                    hwnd, attr, ctypes.byref(value), ctypes.sizeof(value)) == 0:
                break
    except Exception:
        pass


def get_workarea(root):
    if sys.platform == "win32":
        try:
            import ctypes
            from ctypes import wintypes
            rect = wintypes.RECT()
            ctypes.windll.user32.SystemParametersInfoW(0x0030, 0, ctypes.byref(rect), 0)
            if rect.right > rect.left and rect.bottom > rect.top:
                return (rect.left, rect.top,
                        rect.right - rect.left, rect.bottom - rect.top)
        except Exception:
            pass
    return 0, 0, root.winfo_screenwidth(), root.winfo_screenheight()


def plural_ru(n, one, few, many):
    n = abs(int(n))
    if n % 10 == 1 and n % 100 != 11:
        return one
    if 2 <= n % 10 <= 4 and not 12 <= n % 100 <= 14:
        return few
    return many


def shorten(text, limit=70):
    text = str(text)
    if len(text) <= limit:
        return text
    return text[:limit - 1].rstrip() + "…"


def is_descendant(widget, ancestor):
    w = widget
    while w is not None:
        if w is ancestor:
            return True
        w = getattr(w, "master", None)
    return False


def rounded_rect(canvas, x1, y1, x2, y2, r, **kwargs):
    r = max(0, min(r, (x2 - x1) / 2, (y2 - y1) / 2))
    points = [
        x1 + r, y1, x2 - r, y1, x2, y1, x2, y1 + r,
        x2, y2 - r, x2, y2, x2 - r, y2, x1 + r, y2,
        x1, y2, x1, y2 - r, x1, y1 + r, x1, y1,
    ]
    return canvas.create_polygon(points, smooth=True, **kwargs)


class CmCheckbox(tk.Canvas):
    def __init__(self, parent, pal, checked=False, command=None, size=20):
        self.pal = pal
        self.size = size
        self.checked = bool(checked)
        self.command = command
        super().__init__(parent, width=size, height=size, bg=pal["panel"],
                         highlightthickness=0, bd=0, cursor="hand2", takefocus=0)
        self.bind("<Button-1>", self._click)
        self.bind("<Enter>", lambda e: self._draw(hover=True))
        self.bind("<Leave>", lambda e: self._draw(hover=False))
        self._draw()

    def _click(self, _event):
        self.checked = not self.checked
        self._draw()
        if self.command:
            self.command()

    def set_checked(self, value):
        self.checked = bool(value)
        self._draw()

    def set_bg(self, color):
        self.configure(bg=color)
        self._draw()

    def _draw(self, hover=False):
        p = self.pal
        s = self.size
        self.delete("all")
        if self.checked:
            rounded_rect(self, 1.5, 1.5, s - 1.5, s - 1.5, 5,
                         fill=p["accent"], outline=p["accent"])
            self.create_line(
                s * 0.27, s * 0.53, s * 0.43, s * 0.69, s * 0.74, s * 0.33,
                fill=p["on_accent"], width=2, capstyle="round", joinstyle="round",
            )
        else:
            outline = p["accent"] if hover else p["border"]
            rounded_rect(self, 1.5, 1.5, s - 1.5, s - 1.5, 5,
                         fill=p["input"], outline=outline, width=1)


class SlimScrollbar(tk.Canvas):
    def __init__(self, parent, pal, bg=None, command=None, width=12):
        self.pal = pal
        self.command = command
        self.first = 0.0
        self.last = 1.0
        self._hover = False
        self._drag = None
        base = bg or pal["bg"]
        super().__init__(parent, width=width, bg=base, highlightthickness=0,
                         bd=0, takefocus=0)
        self.bind("<Configure>", lambda e: self._redraw())
        self.bind("<Enter>", lambda e: self._redraw(hover=True))
        self.bind("<Leave>", lambda e: self._redraw(hover=False) if self._drag is None else None)
        self.bind("<Button-1>", self._press)
        self.bind("<B1-Motion>", self._motion)
        self.bind("<ButtonRelease-1>", self._release)

    def set(self, first, last):
        try:
            self.first, self.last = float(first), float(last)
        except (TypeError, ValueError):
            return
        self._redraw()

    def _thumb(self):
        h = max(self.winfo_height(), 1)
        w = max(self.winfo_width(), 1)
        top, bottom = 2, h - 2
        track = max(bottom - top, 1)
        span = max(0.0, min(1.0, self.last - self.first))
        length = max(track * span, 26)
        y1 = top + track * max(0.0, min(1.0, self.first))
        if y1 + length > bottom:
            y1 = bottom - length
        return 2, w - 3, y1, y1 + length

    def _redraw(self, hover=None):
        if hover is not None:
            self._hover = hover
        try:
            self.delete("all")
            if self.last - self.first >= 0.999:
                return
            x1, x2, y1, y2 = self._thumb()
            color = self.pal["scroll_hover"] if self._hover else self.pal["scroll"]
            rounded_rect(self, x1, y1, x2, y2, 4, fill=color, outline=color)
        except tk.TclError:
            pass

    def _press(self, event):
        x1, x2, y1, y2 = self._thumb()
        if y1 <= event.y <= y2:
            self._drag = (event.y, self.first)
        elif self.command:
            h = max(self.winfo_height() - 4, 1)
            span = max(0.0, min(1.0, self.last - self.first))
            frac = (event.y - 2) / h - span / 2
            self.command("moveto", str(max(0.0, min(1.0 - span, frac))))

    def _motion(self, event):
        if not self._drag or not self.command:
            return
        y0, first = self._drag
        h = max(self.winfo_height() - 4, 1)
        span = max(0.0, min(1.0, self.last - self.first))
        self.command("moveto", str(max(0.0, min(1.0 - span, first + (event.y - y0) / h))))

    def _release(self, _event):
        self._drag = None


class ScrollFrame(tk.Frame):
    def __init__(self, parent, pal, bg=None):
        base = bg or pal["bg"]
        super().__init__(parent, bg=base)
        self.pal = pal
        self.canvas = tk.Canvas(self, bg=base, highlightthickness=0, bd=0)
        self.inner = tk.Frame(self.canvas, bg=base)
        self._win = self.canvas.create_window((0, 0), window=self.inner, anchor="nw")
        self.sb = SlimScrollbar(self, pal, bg=base, command=self.canvas.yview)
        self.canvas.pack(side="left", fill="both", expand=True)
        self.sb.pack(side="right", fill="y")
        self.canvas.configure(yscrollcommand=self._on_yscroll)
        self.inner.grid_columnconfigure(0, weight=1)
        self.inner.bind("<Configure>", self._on_inner)
        self.canvas.bind("<Configure>", self._on_canvas)
        self.on_scroll = None

    def _on_yscroll(self, first, last):
        self.sb.set(first, last)
        if self.on_scroll is not None:
            self.on_scroll(first, last)

    def _on_inner(self, _event):
        try:
            self.canvas.configure(scrollregion=self.canvas.bbox("all"))
        except tk.TclError:
            pass

    def _on_canvas(self, event):
        self.canvas.itemconfigure(self._win, width=event.width)

    def fraction(self):
        try:
            return self.canvas.yview()[0]
        except tk.TclError:
            return 0.0

    def set_fraction(self, value):
        try:
            self.canvas.yview_moveto(max(0.0, min(1.0, value)))
        except tk.TclError:
            pass

    def to_top(self):
        self.set_fraction(0.0)


NAV_NAMES = {
    "__all__": "Все пункты",
    "__selected__": "Выбранные",
    "__new__": "Новое",
    CAT_MISSING: "Нет в этой версии",
}


class FirefoxContextMenuHider:
    def __init__(self, root):
        self.root = root
        self.cfg = load_config()
        theme = self.cfg.get("theme")
        self.theme = theme if theme in PALETTES else "dark"
        self.pal = PALETTES[self.theme]

        self.search_var = tk.StringVar()
        self.profile_var = tk.StringVar(value=str(self.cfg.get("last_profile", "")))
        self.install_var = tk.StringVar(value=str(self.cfg.get("last_install", "")))
        self.show_separators = tk.BooleanVar(
            value=bool(self.cfg.get("show_separators", False)))
        self.custom_text = str(self.cfg.get("custom", "") or "")

        self.entries = {}
        self.checks = set()
        self.saved_state = set()
        self.saved_sig = ((), ())
        self.ext_names = {}
        self.new_ids = set()
        self.scan_ok = False
        self.browser_found = False
        self.browser_name = ""
        self.browser_version = ""
        self.install_dir = None
        self.profile_ok = False
        self.css_exists = False
        self.pref_state = None
        self.profile_choices = []

        self.view_key = "__all__"
        self.rows = []
        self.row_by_key = {}
        self.nav_items = {}
        self.nav_sections = {}

        self._ui_ready = False
        self._loading = False
        self._search_timer = None
        self._toast_frame = None
        self._toast_timer = None
        self._custom_timer = None
        self._selected_timer = None
        self._custom_widget = None

        self._vl_groups = None
        self._vl_blocks = []
        self._vl_live = {}
        self._vl_pool_rows = []
        self._vl_pool_headers = []
        self._vl_paint_id = None
        self._vl_row_h = 0
        self._vl_row_content_h = 0
        self._vl_header_h = 0
        self._vl_offset = 0
        self._vl_row_count = 0
        self._badge_font_obj = None
        self._badge_h = 16
        self._title_dy = -8
        self._meta_dy = 9

        self._setup_fonts()
        self.root.title(f"{APP_NAME} {APP_VERSION}")
        self.root.configure(bg=self.pal["bg"])
        self.root.minsize(1080, 640)
        self.root.option_add("*TCombobox*Listbox.background", self.pal["panel_alt"])
        self.root.option_add("*TCombobox*Listbox.foreground", self.pal["text"])
        self.root.option_add("*TCombobox*Listbox.selectBackground", self.pal["accent"])
        self.root.option_add("*TCombobox*Listbox.selectForeground", self.pal["on_accent"])

        self._build_ui()
        self._bind_shortcuts()
        self._apply_geometry()
        self.root.protocol("WM_DELETE_WINDOW", self._on_close)
        self.root.after(80, self._startup)

    # ---------- шрифты и окно ----------

    def _setup_fonts(self):
        families = set(tkfont.families(self.root))
        ui = "Segoe UI" if "Segoe UI" in families else tkfont.nametofont(
            "TkDefaultFont").actual("family")
        mono = "Consolas" if "Consolas" in families else "Courier New"
        self.f_ui = ui
        self.f_mono = mono
        self.fonts = {
            "logo": (ui, 14, "bold"),
            "title": (ui, 15, "bold"),
            "subtitle": (ui, 9),
            "nav": (ui, 10),
            "section": (ui, 8, "bold"),
            "body": (ui, 10),
            "body_bold": (ui, 10, "bold"),
            "small": (ui, 9),
            "small_bold": (ui, 9, "bold"),
            "tiny": (ui, 8),
            "tiny_bold": (ui, 8, "bold"),
            "mono": (mono, 8),
            "mono_input": (mono, 10),
            "button": (ui, 9),
            "title_big": (ui, 16, "bold"),
        }

    def _apply_geometry(self):
        self.root.update_idletasks()
        geo = self.cfg.get("geometry")
        if isinstance(geo, str) and re.fullmatch(r"\d+x\d+[+-]\d+[+-]\d+", geo):
            try:
                self.root.geometry(geo)
                return
            except tk.TclError:
                pass
        wx, wy, ww, wh = get_workarea(self.root)
        w = min(1320, max(1080, ww - 120))
        h = min(800, max(640, wh - 40))
        x = wx + (ww - w) // 2
        y = wy + (wh - h) // 2
        self.root.geometry(f"{w}x{h}+{x}+{y}")

    def _build_ui(self):
        self._ui_ready = False
        self._reset_virtual(keep_pool=False)
        for child in self.root.winfo_children():
            child.destroy()
        pal = self.pal
        self.root.configure(bg=pal["bg"])

        style = ttk.Style(self.root)
        try:
            style.theme_use("clam")
        except tk.TclError:
            pass
        style.configure(
            "CM.TCombobox",
            fieldbackground=pal["input"], background=pal["panel_alt"],
            foreground=pal["text"], arrowcolor=pal["muted"],
            bordercolor=pal["border"], lightcolor=pal["border"],
            darkcolor=pal["border"], padding=(8, 5),
        )
        style.map(
            "CM.TCombobox",
            fieldbackground=[("readonly", pal["input"])],
            selectbackground=[("readonly", pal["input"])],
            selectforeground=[("readonly", pal["text"])],
            foreground=[("readonly", pal["text"])],
        )

        self._build_header()
        self._build_connection()
        self._build_statusbar()
        self._build_footer()
        self._build_body()
        self._update_counts()
        self._update_nav_styles()
        self._render_view()
        self._update_footer()
        self._update_pill()
        self._update_pref_hint()
        self._ui_ready = True

        try:
            self.root.iconphoto(True, self._make_icon())
        except tk.TclError:
            pass

    def _make_icon(self):
        pal = self.pal
        img = tk.PhotoImage(width=32, height=32)
        img.put(pal["bg"], to=(0, 0, 32, 32))
        img.put(pal["accent"], to=(3, 3, 29, 29))
        for y in (9, 14, 19, 24):
            img.put(pal["on_accent"], to=(8, y, 24, y + 2))
        img.put(pal["bg"], to=(18, 24, 24, 26))
        return img

    # ---------- шапка ----------

    def _build_header(self):
        pal = self.pal
        header = tk.Frame(self.root, bg=pal["bg"])
        header.pack(fill="x", padx=16, pady=(10, 8))

        logo = tk.Canvas(header, width=38, height=38, bg=pal["bg"],
                         highlightthickness=0, bd=0)
        logo.pack(side="left", padx=(2, 12), pady=2)
        rounded_rect(logo, 1, 1, 37, 37, 11, fill=pal["accent"], outline=pal["accent"])
        logo.create_text(19, 19, text="CM", fill=pal["on_accent"],
                         font=self.fonts["logo"])

        titles = tk.Frame(header, bg=pal["bg"])
        titles.pack(side="left")
        tk.Label(titles, text=APP_NAME, bg=pal["bg"], fg=pal["text"],
                 font=self.fonts["title"], anchor="w").pack(anchor="w")
        tk.Label(titles, text="Пункты контекстного меню → userChrome.css",
                 bg=pal["bg"], fg=pal["muted"], font=self.fonts["subtitle"],
                 anchor="w").pack(anchor="w")

        help_btn = self._make_button(header, "? Справка", self._show_help, "ghost")
        help_btn.pack(side="right", padx=(0, 2))
        theme_icon = "☀ Светлая" if self.theme == "dark" else "☾ Тёмная"
        theme_btn = self._make_button(header, theme_icon, self._toggle_theme, "ghost")
        theme_btn.pack(side="right", padx=(0, 8))

        pill = tk.Frame(header, bg=pal["panel_alt"],
                        highlightbackground=pal["border"], highlightthickness=1)
        pill.pack(side="right", padx=(0, 10))
        self.pill_dot = tk.Canvas(pill, width=12, height=12, bg=pal["panel_alt"],
                                  highlightthickness=0, bd=0)
        self.pill_dot.pack(side="left", padx=(10, 2), pady=6)
        self.pill_label = tk.Label(pill, text="Поиск браузера…", bg=pal["panel_alt"],
                                   fg=pal["muted"], font=self.fonts["small"])
        self.pill_label.pack(side="left", padx=(0, 10), pady=5)

    # ---------- подключение ----------

    def _build_connection(self):
        pal = self.pal
        wrap = tk.Frame(self.root, bg=pal["bg"])
        wrap.pack(fill="x", padx=16, pady=(0, 10))
        card = tk.Frame(wrap, bg=pal["panel"],
                        highlightbackground=pal["border"], highlightthickness=1)
        card.pack(fill="x")
        card.grid_columnconfigure(1, weight=1)

        label_kw = dict(bg=pal["panel"], fg=pal["muted"], font=self.fonts["small"],
                        anchor="w")

        tk.Label(card, text="Профиль", **label_kw).grid(
            row=0, column=0, sticky="w", padx=(14, 10), pady=(11, 4))
        self.profile_combo = ttk.Combobox(
            card, textvariable=self.profile_var, values=self.profile_choices,
            style="CM.TCombobox", font=self.fonts["body"])
        self.profile_combo.grid(row=0, column=1, sticky="ew", pady=(11, 4))
        self.profile_combo.bind("<<ComboboxSelected>>", self._on_profile_changed)
        self.profile_combo.bind("<Return>", self._on_profile_changed)
        detect_btn = self._make_button(card, "Найти", self._detect_profiles, "ghost")
        detect_btn.grid(row=0, column=2, padx=(8, 0), pady=(11, 4))
        browse_btn = self._make_button(card, "Обзор…", self._browse_profile, "ghost")
        browse_btn.grid(row=0, column=3, padx=(6, 14), pady=(11, 4))

        tk.Label(card, text="Браузер", **label_kw).grid(
            row=1, column=0, sticky="w", padx=(14, 10), pady=(2, 11))
        self.install_entry = self._make_entry(card, self.install_var)
        self.install_entry.grid(row=1, column=1, sticky="ew", pady=(2, 11))
        self.install_entry.bind("<Return>", lambda e: self._reload_all(confirm=True))
        inst_btn = self._make_button(card, "Обзор…", self._browse_install, "ghost")
        inst_btn.grid(row=1, column=2, padx=(8, 0), pady=(2, 11))
        scan_btn = self._make_button(card, "Пересканировать",
                                     lambda: self._reload_all(confirm=True), "subtle")
        scan_btn.grid(row=1, column=3, padx=(6, 14), pady=(2, 11))

        self.pref_hint = tk.Label(
            card, text="", bg=pal["panel"], fg=pal["amber"],
            font=self.fonts["small"], anchor="w", justify="left",
            wraplength=1000)
        self.pref_hint.grid(row=2, column=0, columnspan=4, sticky="w",
                            padx=14, pady=(0, 8))
        self.pref_hint.grid_remove()

    def _make_entry(self, parent, variable):
        pal = self.pal
        return tk.Entry(
            parent, textvariable=variable, bg=pal["input"], fg=pal["text"],
            insertbackground=pal["accent"], relief="flat", bd=0,
            highlightthickness=1, highlightbackground=pal["border"],
            highlightcolor=pal["accent"], font=self.fonts["body"],
            selectbackground=pal["selection"], selectforeground=pal["text"],
        )

    # ---------- нижняя панель и статус ----------

    def _build_statusbar(self):
        pal = self.pal
        bar = tk.Frame(self.root, bg=pal["sidebar"])
        bar.pack(side="bottom", fill="x")
        self.status_label = tk.Label(bar, text="Готово", bg=pal["sidebar"],
                                     fg=pal["dim"], font=self.fonts["tiny"],
                                     anchor="w")
        self.status_label.pack(side="left", padx=16, pady=4)
        self.version_label = tk.Label(bar, text=f"v{APP_VERSION}", bg=pal["sidebar"],
                                      fg=pal["dim"], font=self.fonts["tiny"])
        self.version_label.pack(side="right", padx=16, pady=4)

    def _build_footer(self):
        pal = self.pal
        sep = tk.Frame(self.root, bg=pal["border_soft"], height=1)
        sep.pack(side="bottom", fill="x")
        footer = tk.Frame(self.root, bg=pal["sidebar"])
        footer.pack(side="bottom", fill="x")
        left = tk.Frame(footer, bg=pal["sidebar"])
        left.pack(side="left", padx=18, pady=9)
        self.dirty_dot = tk.Canvas(left, width=10, height=10, bg=pal["sidebar"],
                                   highlightthickness=0, bd=0)
        self.dirty_dot.pack(side="left", padx=(0, 7))
        self.dirty_label = tk.Label(left, text="", bg=pal["sidebar"], fg=pal["muted"],
                                    font=self.fonts["small"], anchor="w")
        self.dirty_label.pack(side="left")

        self.apply_btn = self._make_button(footer, "Применить", self._apply, "accent")
        self.apply_btn.pack(side="right", padx=(8, 16), pady=8)
        check_btn = self._make_button(footer, "Проверить CSS", self._check_css, "ghost")
        check_btn.pack(side="right", padx=(6, 0), pady=8)
        css_btn = self._make_button(footer, "CSS", self._preview_css, "ghost")
        css_btn.pack(side="right", padx=(6, 0), pady=8)
        folder_btn = self._make_button(footer, "Папка chrome", self._open_chrome_folder,
                                       "ghost")
        folder_btn.pack(side="right", padx=(6, 0), pady=8)

    # ---------- каркас ----------

    def _build_body(self):
        pal = self.pal
        body = tk.Frame(self.root, bg=pal["bg"])
        body.pack(fill="both", expand=True, padx=16, pady=(0, 10))
        sidebar = tk.Frame(body, bg=pal["sidebar"], width=286,
                           highlightbackground=pal["border"], highlightthickness=1)
        sidebar.pack(side="left", fill="y")
        sidebar.pack_propagate(False)
        content = tk.Frame(body, bg=pal["bg"])
        content.pack(side="left", fill="both", expand=True, padx=(12, 0))
        self._build_sidebar(sidebar)
        self._build_content(content)

    def _build_sidebar(self, parent):
        pal = self.pal
        self.nav_scroll = ScrollFrame(parent, pal, bg=pal["sidebar"])
        self.nav_scroll.pack(fill="both", expand=True)
        inner = self.nav_scroll.inner
        inner.configure(bg=pal["sidebar"])
        inner.grid_columnconfigure(0, weight=1)
        self.nav_items = {}
        self.nav_sections = {}

        for index, (section_name, keys) in enumerate(NAV_SECTIONS):
            sec = tk.Frame(inner, bg=pal["sidebar"])
            sec.grid(row=index, column=0, sticky="ew", pady=(8, 0))
            head = tk.Label(sec, text=section_name.upper(), bg=pal["sidebar"],
                            fg=pal["dim"], font=self.fonts["section"], anchor="w")
            head.pack(fill="x", padx=16, pady=(0, 4))
            holder = tk.Frame(sec, bg=pal["sidebar"])
            holder.pack(fill="x")
            holder.grid_columnconfigure(0, weight=1)
            frames = []
            for item_index, key in enumerate(keys):
                item = self._make_nav_item(holder, key, item_index)
                frames.append(item)
                self.nav_items[key] = item
            self.nav_sections[section_name] = (sec, head, frames)

    def _make_nav_item(self, parent, key, index):
        pal = self.pal
        frame = tk.Frame(parent, bg=pal["sidebar"], cursor="hand2")
        frame.grid(row=index, column=0, sticky="ew")
        stripe = tk.Frame(frame, width=3, bg=pal["sidebar"])
        stripe.pack(side="left", fill="y")
        label = tk.Label(frame, text=NAV_NAMES.get(key, key), bg=pal["sidebar"],
                         fg=pal["muted"], font=self.fonts["nav"], anchor="w",
                         cursor="hand2")
        label.pack(side="left", fill="x", expand=True, padx=(11, 4), pady=6)
        count = tk.Label(frame, text="", bg=pal["panel_alt"], fg=pal["dim"],
                         font=self.fonts["tiny"], padx=6, pady=0)
        count.pack(side="right", padx=(4, 10), pady=6)

        item = {"frame": frame, "stripe": stripe, "label": label, "count": count}
        for w in (frame, stripe, label, count):
            w.bind("<Button-1>", lambda e, k=key: self._select_view(k))
            w.bind("<Enter>", lambda e, k=key: self._hover_nav(k, True))
            w.bind("<Leave>", lambda e, k=key: self._leave_nav(k))
        return item

    def _build_content(self, parent):
        pal = self.pal
        head = tk.Frame(parent, bg=pal["bg"])
        head.pack(fill="x", pady=(2, 8))
        title_row = tk.Frame(head, bg=pal["bg"])
        title_row.pack(fill="x")
        self.view_title = tk.Label(title_row, text="Все пункты", bg=pal["bg"],
                                   fg=pal["text"], font=self.fonts["title_big"],
                                   anchor="w")
        self.view_title.pack(side="left")
        self.view_count = tk.Label(title_row, text="", bg=pal["panel_alt"],
                                   fg=pal["muted"], font=self.fonts["tiny"],
                                   padx=8, pady=2)
        self.view_count.pack(side="left", padx=(10, 0), pady=(4, 0))
        self.view_desc = tk.Label(head, text="", bg=pal["bg"], fg=pal["muted"],
                                  font=self.fonts["small"], anchor="w",
                                  justify="left", wraplength=900)
        self.view_desc.pack(fill="x", pady=(1, 0))

        toolbar = tk.Frame(parent, bg=pal["panel"],
                           highlightbackground=pal["border"], highlightthickness=1)
        toolbar.pack(fill="x")
        self.toolbar = toolbar

        holder = tk.Frame(toolbar, bg=pal["input"],
                          highlightbackground=pal["border"], highlightthickness=1)
        holder.pack(side="left", fill="x", expand=True, padx=(12, 6), pady=9)
        self.search_holder = holder
        icon = tk.Label(holder, text="⌕", bg=pal["input"], fg=pal["dim"],
                        font=(self.f_ui, 13))
        icon.pack(side="left", padx=(9, 2))
        self.search_entry = tk.Entry(
            holder, textvariable=self.search_var, bg=pal["input"], fg=pal["text"],
            insertbackground=pal["accent"], relief="flat", bd=0,
            font=self.fonts["body"], selectbackground=pal["selection"],
            selectforeground=pal["text"],
        )
        self.search_entry.pack(side="left", fill="x", expand=True, pady=7)
        self.search_entry.bind("<FocusIn>", lambda e: self._sync_placeholder())
        self.search_entry.bind("<FocusOut>", lambda e: self._sync_placeholder())
        self.search_entry.bind("<Escape>", self._on_escape)
        self.search_ph = tk.Label(holder, text="Поиск по названию или ID…",
                                  bg=pal["input"], fg=pal["dim"],
                                  font=self.fonts["body"])
        self.search_ph.place(x=32, rely=0.5, anchor="w")
        self.search_ph.bind("<Button-1>", lambda e: self.search_entry.focus_set())
        icon.bind("<Button-1>", lambda e: self.search_entry.focus_set())
        self.search_clear = tk.Label(holder, text="✕", bg=pal["input"], fg=pal["muted"],
                                     font=self.fonts["small"], cursor="hand2")
        self.search_clear.bind("<Button-1>", lambda e: self._clear_search(focus=True))
        self.search_var.trace_add("write", self._search_trace)

        right = tk.Frame(toolbar, bg=pal["panel"])
        right.pack(side="right", fill="y")
        self.toolbar_right = right

        self.actions_frame = tk.Frame(right, bg=pal["panel"])
        self.actions_frame.pack(side="right", padx=(6, 12))
        self.sep_toggle_frame = tk.Frame(right, bg=pal["panel"])
        self.sep_toggle_frame.pack(side="right", padx=(0, 12))
        self.sep_cb = CmCheckbox(
            self.sep_toggle_frame, pal, checked=self.show_separators.get(),
            command=self._on_sep_toggle, size=18)
        self.sep_cb.configure(bg=pal["panel"])
        self.sep_cb.pack(side="left", padx=(0, 6))
        sep_lbl = tk.Label(self.sep_toggle_frame, text="Показывать разделители",
                           bg=pal["panel"], fg=pal["muted"],
                           font=self.fonts["small"], cursor="hand2")
        sep_lbl.pack(side="left")
        sep_lbl.bind("<Button-1>", lambda e: self._toggle_separators())

        self.results_label = tk.Label(right, text="", bg=pal["panel"],
                                      fg=pal["muted"], font=self.fonts["tiny"])
        self.results_label.pack(side="right", padx=(8, 10))

        self.list_scroll = ScrollFrame(parent, pal, bg=pal["bg"])
        self.list_scroll.pack(fill="both", expand=True, pady=(10, 0))
        self.list_scroll.on_scroll = self._on_list_scroll
        self.list_scroll.canvas.bind("<Configure>", self._on_list_viewport,
                                    add="+")

    def _make_button(self, parent, text, command, kind="ghost", compact=False):
        pal = self.pal
        if kind == "accent":
            bg, fg, hover = pal["accent"], pal["on_accent"], pal["accent_hover"]
        elif kind == "subtle":
            bg, fg, hover = parent.cget("bg"), pal["muted"], pal["hover"]
        elif kind == "danger":
            bg, fg, hover = pal["red_bg"], pal["red"], pal["red_bg"]
        else:
            bg, fg, hover = pal["panel_alt"], pal["text"], pal["hover"]
        btn = tk.Button(
            parent, text=text, command=command, bg=bg, fg=fg,
            activebackground=hover, activeforeground=fg,
            relief="flat", bd=0, highlightthickness=0, cursor="hand2",
            font=self.fonts["button"], padx=10 if compact else 14,
            pady=4 if compact else 6,
        )
        btn.bind("<Enter>", lambda e: btn.configure(bg=hover))
        btn.bind("<Leave>", lambda e: btn.configure(bg=bg))
        return btn

    # ---------- навигация ----------

    def _view_title_desc(self):
        key = self.view_key
        special = {
            "__all__": (
                "Все пункты",
                "Все найденные пункты контекстных меню. Отметьте, что скрыть, "
                "затем нажмите «Применить».",
            ),
            "__selected__": (
                "Выбранные",
                "Пункты, которые будут скрыты. Снимите отметку, чтобы вернуть пункт.",
            ),
            "__new__": (
                "Новое после обновления",
                "Пункты, появившиеся в текущей версии браузера. "
                "Проверьте их и отметьте лишние.",
            ),
            CAT_CUSTOM: (
                "Свои ID и селекторы",
                "Собственные правила, которых нет во встроенных списках. "
                "Применяются вместе с отмеченными пунктами.",
            ),
        }
        if key in special:
            return special[key]
        meta = CATEGORY_META.get(key, {})
        return key, meta.get("desc", "")

    def _select_view(self, key):
        if key == self.view_key:
            return
        if not self._ui_ready:
            self.view_key = key
            return
        if self.search_var.get():
            self._clear_search(silent=True)
        self.view_key = key
        self._render_view()
        self._update_nav_styles()

    def _category_count(self, key):
        if key == "__all__":
            return sum(1 for e in self.entries.values() if not e["missing"])
        if key == "__selected__":
            return len(self.checks) + len(parse_custom_lines(self.custom_text)[2])
        if key == "__new__":
            return sum(1 for e in self.entries.values()
                       if e.get("new") and not e["missing"])
        if key == CAT_CUSTOM:
            return len(parse_custom_lines(self.custom_text)[2])
        return sum(1 for e in self.entries.values() if e["category"] == key)

    def _update_counts(self):
        if not self.nav_items:
            return
        for key, item in self.nav_items.items():
            n = self._category_count(key)
            item["count"].configure(text=str(n) if n else "")
            show = n > 0 or key in ("__all__", "__selected__") or key == self.view_key
            if show:
                item["frame"].grid()
            else:
                item["frame"].grid_remove()
        for section_name, (sec, head, frames) in self.nav_sections.items():
            if any(f["frame"].winfo_manager() for f in frames):
                sec.grid()
            else:
                sec.grid_remove()

    def _update_nav_styles(self):
        pal = self.pal
        for key, item in self.nav_items.items():
            active = key == self.view_key
            bg = pal["active"] if active else pal["sidebar"]
            item["frame"].configure(bg=bg)
            item["label"].configure(bg=bg, fg=pal["text"] if active else pal["muted"])
            item["count"].configure(fg=pal["accent"] if active else pal["dim"])
            item["stripe"].configure(bg=pal["accent"] if active else bg)

    def _hover_nav(self, key, on):
        item = self.nav_items.get(key)
        if not item or key == self.view_key:
            return
        pal = self.pal
        bg = pal["hover"] if on else pal["sidebar"]
        item["frame"].configure(bg=bg)
        item["label"].configure(bg=bg)
        item["stripe"].configure(bg=bg)

    def _leave_nav(self, key):
        item = self.nav_items.get(key)
        if not item:
            return
        frame = item["frame"]
        frame.after_idle(lambda: self._check_nav_leave(key, frame))

    def _check_nav_leave(self, key, frame):
        try:
            if not frame.winfo_exists() or key == self.view_key:
                return
            w = self.root.winfo_containing(*self.root.winfo_pointerxy())
            if w is None or not is_descendant(w, frame):
                self._hover_nav(key, False)
        except tk.TclError:
            pass

    def _visible_nav_keys(self):
        keys = []
        for section_name, section_keys in NAV_SECTIONS:
            for key in section_keys:
                item = self.nav_items.get(key)
                if item and item["frame"].winfo_manager():
                    keys.append(key)
        return keys

    def _cycle_view(self, step):
        keys = self._visible_nav_keys()
        if not keys:
            return "break"
        try:
            index = keys.index(self.view_key)
        except ValueError:
            index = -1 if step > 0 else 0
        self._select_view(keys[(index + step) % len(keys)])
        return "break"

    def _jump_view(self, number):
        keys = self._visible_nav_keys()
        if 1 <= number <= len(keys):
            self._select_view(keys[number - 1])
        return "break"

    def _update_pill(self):
        if not hasattr(self, "pill_dot"):
            return
        pal = self.pal
        self.pill_dot.delete("all")

        def dot(color):
            self.pill_dot.create_oval(2, 2, 10, 10, fill=color, outline=color)

        if self.profile_ok and self.browser_found:
            dot(pal["green"])
            count = sum(1 for e in self.entries.values() if not e["missing"])
            text = (f"{self.browser_name or 'Браузер'} {self.browser_version} · "
                    f"{count} {plural_ru(count, 'пункт', 'пункта', 'пунктов')}")
        elif self.profile_ok:
            dot(pal["amber"])
            text = "Профиль выбран · браузер не найден"
        elif self.browser_found:
            dot(pal["amber"])
            text = f"{self.browser_name or 'Браузер'} {self.browser_version} · профиль не выбран"
        else:
            dot(pal["red"])
            text = "Профиль и браузер не найдены"
        self.pill_label.configure(text=text)

    def _update_pref_hint(self):
        if not hasattr(self, "pref_hint"):
            return
        if self.profile_ok and self.pref_state is False:
            self.pref_hint.configure(
                text="Стили userChrome.css выключены в профиле. Включите "
                     "toolkit.legacyUserProfileCustomizations.stylesheets "
                     "на about:config и перезапустите браузер.")
            self.pref_hint.grid()
        else:
            self.pref_hint.grid_remove()

    # ---------- список ----------

    def _place(self, widget, pady=3):
        widget.grid(row=self._grid_index, column=0, sticky="ew",
                    padx=16, pady=pady)
        self._grid_index += 1
        return widget

    def _render_view(self, keep_scroll=False):
        fraction = self.list_scroll.fraction() if keep_scroll else 0.0
        reuse_pool = self._vl_groups is not None and self.view_key != CAT_CUSTOM
        if self._vl_groups is not None:
            for index in list(self._vl_live):
                self._vl_release(index)
        self._reset_virtual(keep_pool=reuse_pool)
        inner = self.list_scroll.inner
        if reuse_pool:
            keep = {w["frame"] for w in self._vl_pool_rows}
            keep.update(w["frame"] for w in self._vl_pool_headers)
            for child in inner.winfo_children():
                if child not in keep:
                    child.destroy()
        else:
            for child in inner.winfo_children():
                child.destroy()
        inner.grid_propagate(True)
        self.rows = []
        self.row_by_key = {}
        self.empty_row = None
        self._custom_widget = None
        self._grid_index = 0

        title, desc = self._view_title_desc()
        self.view_title.configure(text=title)
        self.view_desc.configure(text=desc)
        self._toolbar_for_view()

        if self.view_key == CAT_CUSTOM:
            self._render_custom_view()
            n = len(parse_custom_lines(self.custom_text)[2])
            self.view_count.configure(
                text=f"{n} {plural_ru(n, 'правило', 'правила', 'правил')}")
            self._update_nav_styles()
            self._restore_scroll(keep_scroll, fraction)
            return

        self.empty_row = self._make_empty_state()
        self._render_virtual(self._view_groups(), keep_scroll, fraction)

    # ---------- виртуальный список ----------

    def _vl_active(self):
        return self._vl_groups is not None and self.view_key != CAT_CUSTOM

    def _reset_virtual(self, keep_pool=False):
        if self._vl_paint_id is not None:
            try:
                self.root.after_cancel(self._vl_paint_id)
            except tk.TclError:
                pass
            self._vl_paint_id = None
        self._vl_groups = None
        self._vl_blocks = []
        self._vl_live = {}
        if not keep_pool:
            self._vl_pool_rows = []
            self._vl_pool_headers = []
        self._vl_offset = 0
        self._vl_row_count = 0

    def _ensure_metrics(self):
        if self._vl_row_h:
            return
        body = tkfont.Font(font=self.fonts["body"])
        mono = tkfont.Font(font=self.fonts["mono"])
        tiny = tkfont.Font(font=self.fonts["tiny"])
        self._badge_font_obj = tiny
        body_h = body.metrics("linespace")
        mono_h = mono.metrics("linespace")
        self._vl_row_content_h = max(body_h + mono_h + 14, 48)
        self._vl_row_h = self._vl_row_content_h + 6
        self._title_dy = -(mono_h // 2 + 1)
        self._meta_dy = body_h // 2 + 1
        self._badge_h = tiny.metrics("linespace") + 6
        header = self._build_header_widgets()
        try:
            header["frame"].update_idletasks()
            self._vl_header_h = max(header["frame"].winfo_reqheight(), 10) + 14
        finally:
            header["frame"].destroy()

    # Виртуальный список: виджеты существуют только для видимой области.
    def _render_virtual(self, groups, keep_scroll=False, fraction=0.0):
        self._vl_groups = [(t, items) for t, items in groups if items]
        self._vl_row_count = sum(len(items) for _t, items in self._vl_groups)
        self._vl_offset = 0
        if self.view_key == "__selected__":
            self._add_custom_section()
            if self.rows:
                self.list_scroll.inner.update_idletasks()
                self._vl_offset = self.list_scroll.inner.winfo_reqheight() + 6
        self._ensure_metrics()
        self._apply_search()
        self._update_view_count()
        self._update_nav_styles()
        self._restore_scroll(keep_scroll, fraction)

    def _vl_rebuild_blocks(self, tokens, show_sep, offset):
        blocks = []
        row_h = self._vl_row_h
        header_h = self._vl_header_h
        y = offset
        total = 0
        for title, items in self._vl_groups:
            visible = []
            for entry in items:
                if tokens:
                    if not all(t in entry.get("search", "") for t in tokens):
                        continue
                elif (entry.get("is_separator") and not show_sep
                      and entry["key"] not in self.checks):
                    continue
                visible.append(entry)
            if not visible:
                continue
            if title:
                blocks.append({"kind": "header", "title": title,
                               "count": len(visible), "y": y, "h": header_h})
                y += header_h
            for entry in visible:
                blocks.append({"kind": "row", "entry": entry, "y": y,
                               "h": row_h})
                y += row_h
                total += 1
        return blocks, y, total

    def _vl_apply_layout(self, bottom):
        inner = self.list_scroll.inner
        inner.grid_propagate(False)
        inner.configure(height=max(int(bottom) + 8, 1))
        try:
            canvas = self.list_scroll.canvas
            canvas.configure(
                scrollregion=(0, 0, canvas.winfo_width(), max(int(bottom) + 8, 1)))
        except tk.TclError:
            pass
        self._vl_paint()

    def _vl_show_empty(self):
        if self.empty_row is None:
            self.empty_row = self._make_empty_state()
        title, text = self._empty_text()
        self.empty_row["title"].configure(text=title)
        self.empty_row["text"].configure(text=text)
        self.empty_row["frame"].place(x=16, y=self._vl_offset + 30,
                                      relwidth=1.0, width=-32)

    def _vl_hide_empty(self):
        if self.empty_row is not None:
            self.empty_row["frame"].place_forget()

    def _on_list_scroll(self, _first=None, _last=None):
        if not self._vl_active() or self._vl_paint_id is not None:
            return
        self._vl_paint_id = self.root.after_idle(self._vl_paint)

    def _on_list_viewport(self, _event):
        self._on_list_scroll()

    def _vl_acquire(self, index):
        block = self._vl_blocks[index]
        y = block["y"]
        if block["kind"] == "header":
            if self._vl_pool_headers:
                header = self._vl_pool_headers.pop()
            else:
                header = self._build_header_widgets()
            self._fill_header(header, block["title"], block["count"])
            header["frame"].place(x=16, y=y + 12, relwidth=1.0, width=-32)
            self._vl_live[index] = header
            return
        if self._vl_pool_rows:
            row = self._vl_pool_rows.pop()
        else:
            row = self._build_row_widgets()
        self._fill_row(row, block["entry"])
        row["group_header"] = None
        row["frame"].place(x=16, y=y + 3, relwidth=1.0, width=-32)
        self._vl_live[index] = row
        self.row_by_key[row["key"]] = row

    def _vl_release(self, index):
        widget = self._vl_live.pop(index)
        try:
            widget["frame"].place_forget()
        except tk.TclError:
            pass
        if widget["kind"] == "row":
            key = widget.get("key")
            if key is not None and self.row_by_key.get(key) is widget:
                del self.row_by_key[key]
            widget["hover"] = False
            if len(self._vl_pool_rows) < 24:
                self._vl_pool_rows.append(widget)
            else:
                widget["frame"].destroy()
        else:
            if len(self._vl_pool_headers) < 12:
                self._vl_pool_headers.append(widget)
            else:
                widget["frame"].destroy()

    def _vl_paint(self):
        self._vl_paint_id = None
        if not self._vl_active():
            return
        blocks = self._vl_blocks
        if not blocks:
            return
        canvas = self.list_scroll.canvas
        try:
            top = canvas.canvasy(0)
            height = max(canvas.winfo_height(), 1)
        except tk.TclError:
            return
        pad = height * 0.75 + 120
        lo, hi = top - pad, top + height + pad
        i0 = 0
        while i0 < len(blocks) and blocks[i0]["y"] + blocks[i0]["h"] <= lo:
            i0 += 1
        i1 = i0
        while i1 < len(blocks) and blocks[i1]["y"] < hi:
            i1 += 1
        desired = set(range(i0, i1))
        for index in [i for i in self._vl_live if i not in desired]:
            self._vl_release(index)
        missing = desired.difference(self._vl_live)
        for index in sorted(missing):
            self._vl_acquire(index)

    def _restore_scroll(self, keep, fraction):
        if keep:
            self.root.after_idle(lambda: self.list_scroll.set_fraction(fraction))
        else:
            self.list_scroll.to_top()

    def _update_view_count(self):
        if self._vl_active():
            n = self._vl_row_count
        else:
            n = sum(1 for r in self.rows if r["kind"] == "row")
        self.view_count.configure(
            text=f"{n} {plural_ru(n, 'пункт', 'пункта', 'пунктов')}")

    def _view_groups(self):
        key = self.view_key
        browser = [e for e in self.entries.values()]
        if key == "__all__":
            groups = []
            for cat in CATEGORY_ORDER + [CAT_EXTENSIONS]:
                items = [e for e in browser
                         if e["category"] == cat and not e["missing"]]
                if items:
                    items.sort(key=entry_sort_key)
                    groups.append((cat, items))
            return groups
        if key == "__new__":
            buckets = {}
            for e in browser:
                if e.get("new") and not e["missing"]:
                    buckets.setdefault(e["category"], []).append(e)
            groups = []
            for cat in CATEGORY_ORDER + [CAT_EXTENSIONS]:
                if cat in buckets:
                    buckets[cat].sort(key=entry_sort_key)
                    groups.append((cat, buckets[cat]))
            return groups
        if key == "__selected__":
            buckets = {}
            for checked_key in self.checks:
                e = self.entries.get(checked_key)
                if e:
                    buckets.setdefault(e["category"], []).append(e)
            groups = []
            for cat in CATEGORY_ORDER + [CAT_EXTENSIONS, CAT_MISSING]:
                if cat in buckets:
                    buckets[cat].sort(key=entry_sort_key)
                    groups.append((cat, buckets[cat]))
            return groups
        if key == CAT_MISSING:
            buckets = {}
            for e in browser:
                if e["category"] == CAT_MISSING:
                    buckets.setdefault(e.get("origin") or CAT_OTHER_MENUS, []).append(e)
            groups = []
            for cat in CATEGORY_ORDER:
                if cat in buckets:
                    buckets[cat].sort(key=entry_sort_key)
                    groups.append((cat, buckets[cat]))
            for cat, items in buckets.items():
                if cat not in CATEGORY_ORDER:
                    items.sort(key=entry_sort_key)
                    groups.append((cat, items))
            return groups
        if key == CAT_EXTENSIONS:
            special = [e for e in browser if e["kind"] == "special"]
            active = [e for e in browser if e["kind"] == "ext" and not e["inactive"]]
            inactive = [e for e in browser if e["kind"] == "ext" and e["inactive"]]
            groups = []
            if special:
                groups.append(("Особые правила", sorted(
                    special, key=entry_sort_key)))
            if active:
                groups.append(("Активные расширения", sorted(
                    active, key=lambda e: e["label"].lower())))
            if inactive:
                groups.append(("Отключённые расширения", sorted(
                    inactive, key=lambda e: e["label"].lower())))
            return groups
        items = [e for e in browser if e["category"] == key and not e["missing"]]
        items.sort(key=entry_sort_key)
        return [(None, items)] if items else []

    def _build_header_widgets(self):
        pal = self.pal
        frame = tk.Frame(self.list_scroll.inner, bg=pal["bg"])
        frame.grid_columnconfigure(1, weight=1)
        label = tk.Label(frame, text="", bg=pal["bg"], fg=pal["text"],
                         font=self.fonts["small_bold"], anchor="w")
        label.grid(row=0, column=0, sticky="w", padx=(2, 10))
        line = tk.Frame(frame, bg=pal["border_soft"], height=1)
        line.grid(row=0, column=1, sticky="ew", pady=(4, 0))
        count = tk.Label(frame, text="", bg=pal["bg"], fg=pal["dim"],
                         font=self.fonts["tiny"], anchor="e")
        count.grid(row=0, column=2, sticky="e", padx=(10, 2))
        return {"kind": "header", "title": "", "frame": frame,
                "label": label, "count": count, "rows": []}

    def _fill_header(self, header, title, count):
        header["title"] = title
        header["label"].configure(text=title)
        header["count"].configure(text=str(count) if count else "")
        header["rows"] = []

    def _add_group_header(self, title):
        header = self._build_header_widgets()
        self._fill_header(header, title, 0)
        self._place(header["frame"], pady=(12, 2))
        return header

    def _badge(self, parent, text, bg, fg):
        return tk.Label(parent, text=text, bg=bg, fg=fg,
                        font=self.fonts["tiny"], padx=6, pady=0)

    def _static_badges(self, entry):
        pal = self.pal
        out = []
        if entry.get("new"):
            out.append(("новое", pal["amber_bg"], pal["amber"]))
        if entry.get("missing"):
            out.append(("нет в версии", pal["panel_alt"], pal["dim"]))
        elif entry.get("dynamic"):
            out.append(("из кода", pal["blue_bg"], pal["blue"]))
        elif entry.get("is_menu"):
            out.append(("подменю", pal["panel_alt"], pal["muted"]))
        if entry.get("kind") == "ext" and entry.get("inactive"):
            out.append(("отключено", pal["panel_alt"], pal["dim"]))
        if entry.get("kind") == "special":
            out.append(("общее", pal["blue_bg"], pal["blue"]))
        return out

    def _build_row_widgets(self):
        pal = self.pal
        if not self._vl_row_h:
            self._ensure_metrics()
        row = {"kind": "row", "key": None, "entry": None,
               "hover": False, "visible": True}
        canvas = tk.Canvas(
            self.list_scroll.inner, height=self._vl_row_content_h,
            bg=pal["panel"], bd=0, highlightthickness=1,
            highlightbackground=pal["border_soft"],
            highlightcolor=pal["border_soft"], cursor="hand2", takefocus=0)
        row["frame"] = row["canvas"] = canvas
        canvas.bind("<Button-1>", lambda e: self._row_click(row))
        canvas.bind("<Button-3>", lambda e: self._row_menu(row, e))
        canvas.bind("<Enter>", lambda e: self._hover_row(row, True))
        canvas.bind("<Leave>", lambda e: self._hover_row(row, False))
        canvas.bind("<Configure>", lambda e: self._row_configure(row, e))
        return row

    def _row_configure(self, row, event):
        if abs(event.width - row.get("drawn_width", -1)) > 1:
            self._draw_row(row)

    def _row_click(self, row):
        if row.get("entry") is not None:
            self._toggle_entry(row["key"])
        return "break"

    def _row_menu(self, row, event):
        entry = row.get("entry")
        if entry is not None:
            self._show_row_menu(event, entry)
        return "break"

    def _fill_row(self, row, entry):
        row["entry"] = entry
        row["key"] = entry["key"]
        row["hover"] = False
        row["visible"] = True
        self._draw_row(row)

    def _draw_row(self, row):
        canvas = row["canvas"]
        entry = row.get("entry")
        if entry is None:
            return
        pal = self.pal
        key = row["key"]
        checked = key in self.checks
        try:
            width = canvas.winfo_width()
            if width <= 4:
                width = 640
            row["drawn_width"] = width
            height = self._vl_row_content_h
            bg = pal["hover"] if row.get("hover") else pal["panel"]
            canvas.delete("all")
            canvas.configure(bg=bg)
            if checked:
                canvas.create_rectangle(1, 1, 4, height - 1,
                                        fill=pal["accent"], outline=pal["accent"])
            box = (height - 20) / 2
            if checked:
                rounded_rect(canvas, 15.5, box + 1.5, 32.5, box + 18.5, 5,
                             fill=pal["accent"], outline=pal["accent"])
                canvas.create_line(19.4, box + 10.6, 22.6, box + 13.8,
                                   28.8, box + 6.6, fill=pal["on_accent"],
                                   width=2, capstyle="round",
                                   joinstyle="round")
            else:
                rounded_rect(canvas, 15.5, box + 1.5, 32.5, box + 18.5, 5,
                             fill=pal["input"], outline=pal["border"], width=1)
            canvas.create_text(48, height / 2 + self._title_dy,
                               text=shorten(entry["label"], 76), anchor="w",
                               fill=pal["text"], font=self.fonts["body"])
            if entry.get("kind") == "ext":
                meta_text = (f'{entry["id_text"]}  ·  '
                             f'{entry.get("ext_id", "")}')
            else:
                meta_text = entry.get("id_text", "")
            canvas.create_text(48, height / 2 + self._meta_dy,
                               text=shorten(meta_text, 68), anchor="w",
                               fill=pal["dim"], font=self.fonts["mono"])
            badges = self._static_badges(entry)
            state = self._row_state(entry)
            if state:
                badges.append((state[0], state[1], state[2]))
            self._draw_badges(canvas, width, height, badges)
        except tk.TclError:
            pass

    def _draw_badges(self, canvas, width, height, badges):
        if not badges:
            return
        font = self._badge_font_obj
        if font is None:
            font = self._badge_font_obj = tkfont.Font(font=self.fonts["tiny"])
        right = width - 13
        top = height / 2 - self._badge_h / 2
        for text, bg, fg in reversed(badges):
            left = right - font.measure(text) - 12
            if left < 40:
                break
            canvas.create_rectangle(left, top, right, top + self._badge_h,
                                    fill=bg, outline=bg)
            canvas.create_text((left + right) / 2, height / 2, text=text,
                               fill=fg, font=self.fonts["tiny"])
            right = left - 4

    def _row_state(self, entry):
        pal = self.pal
        key = entry["key"]
        if key in self.checks:
            if key in self.saved_state:
                return ("скрыто", pal["green_bg"], pal["green"])
            return ("к применению", pal["amber_bg"], pal["amber"])
        if key in self.saved_state:
            return ("было скрыто", pal["blue_bg"], pal["blue"])
        return None

    def _refresh_row(self, row):
        if "canvas" in row:
            self._draw_row(row)
            return
        entry = row["entry"]
        checked = entry["key"] in self.checks
        row["cb"].set_checked(checked)
        pal = self.pal
        base = pal["hover"] if row.get("hover") else pal["panel"]
        row["stripe"].configure(bg=pal["accent"] if checked else base)
        state = self._row_state(entry)
        badge = row["state_badge"]
        if state:
            badge.configure(text=state[0], bg=state[1], fg=state[2])
            if not badge.winfo_manager():
                badge.pack(side="left", padx=(4, 0))
        elif badge.winfo_manager():
            badge.pack_forget()

    def _bind_tree(self, widget, sequence, handler, skip=()):
        if widget not in skip:
            widget.bind(sequence, handler, add="+")
        for child in widget.winfo_children():
            self._bind_tree(child, sequence, handler, skip)

    def _hover_row(self, row, on):
        if row.get("hover") == on:
            return
        row["hover"] = on
        if "canvas" in row:
            self._draw_row(row)
            return
        pal = self.pal
        bg = pal["hover"] if on else pal["panel"]
        try:
            for w in row["bg_targets"]:
                w.configure(bg=bg)
            row["cb"].set_bg(bg)
            checked = row["key"] in self.checks
            row["stripe"].configure(bg=pal["accent"] if checked else bg)
        except tk.TclError:
            pass

    def _add_custom_section(self):
        ids, selectors, lines = parse_custom_lines(self.custom_text)
        visible_lines = []
        for line in lines:
            target = line[1:] if line.startswith("#") else line
            if target in self.checks and target in self.entries:
                continue
            visible_lines.append(line)
        if not visible_lines:
            return
        header = self._add_group_header("Свои правила")
        self.rows.append(header)
        for line in visible_lines:
            row = self._add_custom_row(line, header)
            header["rows"].append(row)
            self.rows.append(row)

    def _add_custom_row(self, line, header):
        pal = self.pal
        frame = tk.Frame(self.list_scroll.inner, bg=pal["panel"],
                         highlightbackground=pal["border_soft"],
                         highlightthickness=1, cursor="hand2")
        self._place(frame)
        frame.grid_columnconfigure(2, weight=1)
        stripe = tk.Frame(frame, width=3, bg=pal["accent"])
        stripe.grid(row=0, column=0, sticky="ns")
        texts = tk.Frame(frame, bg=pal["panel"])
        texts.grid(row=0, column=1, sticky="ew", padx=(16, 10), pady=12)
        title = tk.Label(texts, text="Своё правило", bg=pal["panel"],
                         fg=pal["text"], font=self.fonts["body"], anchor="w",
                         cursor="hand2")
        title.pack(anchor="w")
        meta = tk.Label(texts, text=shorten(line, 78), bg=pal["panel"],
                        fg=pal["dim"], font=self.fonts["mono"], anchor="w",
                        cursor="hand2")
        meta.pack(anchor="w")
        badges = tk.Frame(frame, bg=pal["panel"])
        badges.grid(row=0, column=2, padx=(8, 12), pady=10, sticky="e")
        self._badge(badges, "своё", pal["blue_bg"], pal["blue"]).pack(
            side="left", padx=(0, 8))
        remove_btn = self._make_button(
            badges, "Убрать", lambda l=line: self._remove_custom_line(l),
            "subtle", compact=True)
        remove_btn.configure(bg=pal["panel"])
        remove_btn.pack(side="left")

        pseudo = {
            "key": "__custom__::" + line, "label": line, "id_text": line,
            "search": (line + " своё правило custom").lower(),
            "is_separator": False,
        }
        row = {
            "kind": "row", "key": pseudo["key"], "entry": pseudo,
            "frame": frame, "group_header": header, "custom_line": line,
            "visible": True,
            "bg_targets": [frame, texts, title, meta, badges],
        }

        def open_custom(_event):
            self._select_view(CAT_CUSTOM)

        self._bind_tree(frame, "<Button-1>", open_custom, skip=(remove_btn,))
        self._bind_tree(frame, "<Button-3>", lambda e: self._show_custom_menu(e, line))
        self._bind_tree(frame, "<Enter>", lambda e: self._hover_custom(row, True),
                        skip=(remove_btn,))
        self._bind_tree(frame, "<Leave>",
                        lambda e: frame.after_idle(lambda: self._check_custom_leave(row)),
                        skip=(remove_btn,))
        return row

    def _hover_custom(self, row, on):
        pal = self.pal
        bg = pal["hover"] if on else pal["panel"]
        try:
            for w in row["bg_targets"]:
                w.configure(bg=bg)
        except tk.TclError:
            pass
        row["hover"] = on

    def _check_custom_leave(self, row):
        frame = row["frame"]
        try:
            if not frame.winfo_exists():
                return
            w = self.root.winfo_containing(*self.root.winfo_pointerxy())
            if w is None or not is_descendant(w, frame):
                self._hover_custom(row, False)
        except tk.TclError:
            pass

    def _make_empty_state(self):
        pal = self.pal
        frame = tk.Frame(self.list_scroll.inner, bg=pal["bg"])
        icon = tk.Label(frame, text="⌕", bg=pal["bg"], fg=pal["border"],
                        font=(self.f_ui, 28))
        icon.pack(pady=(8, 2))
        title = tk.Label(frame, text="", bg=pal["bg"], fg=pal["muted"],
                         font=self.fonts["body_bold"])
        title.pack()
        text = tk.Label(frame, text="", bg=pal["bg"], fg=pal["dim"],
                        font=self.fonts["small"], justify="center",
                        wraplength=560)
        text.pack(pady=(4, 0))
        return {"kind": "empty", "frame": frame, "title": title, "text": text}

    def _empty_text(self):
        if self.search_var.get().strip():
            return ("Ничего не найдено",
                    "Попробуйте другой запрос: название, ID или категорию.")
        key = self.view_key
        if key == "__selected__":
            return ("Пока ничего не выбрано",
                    "Отметьте пункты в разделе «Все пункты» — "
                    "они появятся здесь.")
        if key == "__new__":
            return ("Новых пунктов нет",
                    "После обновления браузера здесь появятся пункты, "
                    "которых не было раньше.")
        if key == CAT_MISSING:
            return ("Все пункты на месте",
                    "В этой версии браузера нет пунктов из встроенного списка.")
        if key == CAT_EXTENSIONS:
            return ("Расширения не найдены",
                    "В выбранном профиле нет установленных расширений.")
        if key == "__all__":
            if not self.entries:
                return ("Список пуст",
                        "Укажите профиль и папку браузера сверху, "
                        "затем нажмите «Пересканировать».")
            return ("Нет пунктов",
                    "Для этой версии браузера не нашлось пунктов меню.")
        return ("В этой категории пусто",
                "В установленной версии браузера нет таких пунктов.")

    def _apply_search(self):
        if self.view_key == CAT_CUSTOM:
            return
        if self._vl_active():
            self._apply_search_virtual()
            return
        query = self.search_var.get().strip().lower() if self._ui_ready else ""
        tokens = query.split()
        counts = {}
        visible_total = 0
        for row in self.rows:
            if row["kind"] != "row":
                continue
            entry = row["entry"]
            visible = all(t in entry.get("search", "") for t in tokens)
            if (entry.get("is_separator") and not self.show_separators.get()
                    and entry["key"] not in self.checks and not tokens):
                visible = False
            row["visible"] = visible
            if visible:
                visible_total += 1
                header = row.get("group_header")
                if header is not None:
                    counts[id(header)] = counts.get(id(header), 0) + 1
                if not row["frame"].winfo_manager():
                    row["frame"].grid()
            else:
                row["frame"].grid_remove()
        for row in self.rows:
            if row["kind"] != "header":
                continue
            n = counts.get(id(row), 0)
            row["count"].configure(text=str(n))
            if n:
                row["frame"].grid()
            else:
                row["frame"].grid_remove()
        if self.empty_row is not None:
            title, text = self._empty_text()
            self.empty_row["title"].configure(text=title)
            self.empty_row["text"].configure(text=text)
            if visible_total == 0:
                self.empty_row["frame"].grid()
            else:
                self.empty_row["frame"].grid_remove()
        if query:
            self.results_label.configure(text=f"Найдено: {visible_total}")
        else:
            self.results_label.configure(text="")
        self._sync_placeholder()

    def _apply_search_virtual(self):
        query = self.search_var.get().strip().lower() if self._ui_ready else ""
        tokens = query.split()
        blocks, bottom, total = self._vl_rebuild_blocks(
            tokens, self.show_separators.get(), self._vl_offset)
        for index in list(self._vl_live):
            self._vl_release(index)
        self._vl_blocks = blocks
        if blocks:
            self._vl_hide_empty()
            bottom = max(bottom, self._vl_offset)
        else:
            self._vl_show_empty()
            bottom = max(bottom, self._vl_offset + 150)
        if query:
            self.results_label.configure(text=f"Найдено: {total}")
        else:
            self.results_label.configure(text="")
        self._sync_placeholder()
        self._vl_apply_layout(bottom)

    # ---------- панель инструментов ----------

    def _toolbar_for_view(self):
        custom = self.view_key == CAT_CUSTOM
        if custom:
            if self.toolbar.winfo_manager():
                self.toolbar.pack_forget()
            return
        if not self.toolbar.winfo_manager():
            self.toolbar.pack(fill="x", before=self.list_scroll)
        self._build_actions()
        if not self.search_holder.winfo_manager():
            self.search_holder.pack(side="left", fill="x", expand=True,
                                    padx=(12, 6), pady=9)
        if not self.sep_toggle_frame.winfo_manager():
            self.sep_toggle_frame.pack(side="right", padx=(0, 12))
        self.results_label.configure(text="")
        if self.entries:
            if not self.actions_frame.winfo_manager():
                self.actions_frame.pack(side="right", padx=(6, 12))
        elif self.actions_frame.winfo_manager():
            self.actions_frame.pack_forget()

    def _build_actions(self):
        for child in self.actions_frame.winfo_children():
            child.destroy()
        if self.view_key == CAT_CUSTOM or not self.entries:
            return
        buttons = []
        if self.view_key == "__selected__":
            buttons.append(("Снять отметки", self._clear_all_visible))
        else:
            buttons.append(("Отметить всё", self._select_all_visible))
            buttons.append(("Снять отметки", self._clear_all_visible))
        for text, command in buttons:
            btn = self._make_button(self.actions_frame, text, command,
                                    "ghost", compact=True)
            btn.pack(side="left", padx=(4, 0), pady=6)

    # ---------- поиск ----------

    def _search_trace(self, *_args):
        if not self._ui_ready:
            return
        self._sync_placeholder()
        if self._search_timer is not None:
            self.root.after_cancel(self._search_timer)
        self._search_timer = self.root.after(160, self._run_search)

    def _run_search(self):
        self._search_timer = None
        self._apply_search()

    def _sync_placeholder(self):
        if not hasattr(self, "search_ph"):
            return
        if self.search_var.get():
            self.search_ph.place_forget()
            self.search_clear.place(relx=1, rely=0.5, x=-9, anchor="e")
        else:
            self.search_clear.place_forget()
            try:
                focused = self.root.focus_get()
            except (tk.TclError, KeyError):
                focused = None
            if focused is not self.search_entry:
                self.search_ph.place(x=32, rely=0.5, anchor="w")

    def _clear_search(self, focus=False, silent=False):
        if not self.search_var.get():
            if focus:
                self.search_entry.focus_set()
            return
        ready = self._ui_ready
        self._ui_ready = False
        self.search_var.set("")
        self._ui_ready = ready
        self._sync_placeholder()
        if not silent:
            self._apply_search()
        if focus:
            self.search_entry.focus_set()

    def _focus_search(self):
        if self.view_key == CAT_CUSTOM:
            self._select_view("__all__")
        self.search_entry.focus_set()
        self.search_entry.select_range(0, "end")
        return "break"

    def _on_escape(self, _event=None):
        if self.search_var.get():
            self._clear_search(focus=True)
        return "break"

    # ---------- переключение отметок ----------

    def _toggle_entry(self, key):
        if key not in self.entries:
            return
        if key in self.checks:
            self.checks.discard(key)
        else:
            self.checks.add(key)
        row = self.row_by_key.get(key)
        if row is not None:
            self._refresh_row(row)
        self._update_footer()
        self._update_counts()
        if self.view_key == "__selected__" and key not in self.checks:
            self._schedule_selected_rebuild()

    def _toggle_separators(self):
        self.sep_cb.checked = not self.sep_cb.checked
        self.sep_cb.set_checked(self.sep_cb.checked)
        self._on_sep_toggle()

    def _on_sep_toggle(self):
        self.show_separators.set(self.sep_cb.checked)
        self.cfg["show_separators"] = bool(self.sep_cb.checked)
        save_config(self.cfg)
        self._apply_search()

    def _visible_entry_keys(self):
        if self._vl_active():
            return [b["entry"]["key"] for b in self._vl_blocks
                    if b["kind"] == "row"]
        return [r["key"] for r in self.rows
                if r["kind"] == "row" and r.get("visible")
                and not r.get("custom_line")]

    def _select_all_visible(self):
        keys = self._visible_entry_keys()
        if not keys:
            return
        self.checks.update(keys)
        for key in keys:
            row = self.row_by_key.get(key)
            if row is not None:
                self._refresh_row(row)
        self._update_footer()
        self._update_counts()

    def _clear_all_visible(self):
        keys = self._visible_entry_keys()
        removed = [k for k in keys if k in self.checks]
        if not removed:
            return
        self.checks.difference_update(removed)
        for key in removed:
            row = self.row_by_key.get(key)
            if row is not None:
                self._refresh_row(row)
        self._update_footer()
        self._update_counts()
        if self.view_key == "__selected__":
            self._schedule_selected_rebuild()

    def _schedule_selected_rebuild(self):
        if self._selected_timer is not None:
            self.root.after_cancel(self._selected_timer)
        self._selected_timer = self.root.after(220, self._rebuild_selected)

    def _rebuild_selected(self):
        self._selected_timer = None
        if self.view_key == "__selected__":
            self._render_view(keep_scroll=True)

    def _remove_custom_line(self, line):
        out = []
        removed = False
        for raw in self.custom_text.splitlines():
            if not removed and raw.strip() == line.strip():
                removed = True
                continue
            out.append(raw)
        if not removed:
            return
        self.custom_text = "\n".join(out)
        self._persist_config()
        self._update_counts()
        self._update_footer()
        self._render_view(keep_scroll=True)
        self._toast("Правило убрано из «Свои ID»", "info")

    # ---------- контекстное меню строки ----------

    def _entry_css(self, entry):
        key = entry["key"]
        if key == SPECIAL_ALL_EXT:
            selector = ".webextension-menuitem"
        elif key == SPECIAL_EXT_SEP:
            selector = "menuseparator.webextension-menuitem"
        elif entry.get("kind") == "ext":
            selector = f'[id^="{entry["id_text"]}-menuitem-"]'
        elif entry.get("kind") == "selector":
            selector = entry.get("selector") or entry["id_text"]
        else:
            selector = id_selector(entry["id_text"])
        return f"{selector} {{\n  display: none !important;\n}}"

    def _show_row_menu(self, event, entry):
        pal = self.pal
        menu = tk.Menu(self.root, tearoff=0, bg=pal["panel_alt"], fg=pal["text"],
                       activebackground=pal["accent"], activeforeground=pal["on_accent"],
                       bd=0, relief="flat", font=self.fonts["small"])
        is_selector = entry.get("kind") == "selector"
        menu.add_command(
            label="Копировать селектор" if is_selector else "Копировать ID",
            command=lambda: self._copy_text(
                entry["id_text"],
                "Селектор скопирован" if is_selector else "ID скопирован"))
        menu.add_command(label="Копировать название",
                         command=lambda: self._copy_text(entry["label"],
                                                        "Название скопировано"))
        menu.add_command(label="Копировать CSS-правило",
                         command=lambda: self._copy_text(self._entry_css(entry),
                                                        "CSS-правило скопировано"))
        menu.add_separator()
        checked = entry["key"] in self.checks
        menu.add_command(
            label="Снять отметку" if checked else "Отметить для скрытия",
            command=lambda: self._toggle_entry(entry["key"]))
        self._row_menu = menu
        try:
            menu.tk_popup(event.x_root, event.y_root)
        finally:
            menu.grab_release()

    def _show_custom_menu(self, event, line):
        pal = self.pal
        menu = tk.Menu(self.root, tearoff=0, bg=pal["panel_alt"], fg=pal["text"],
                       activebackground=pal["accent"], activeforeground=pal["on_accent"],
                       bd=0, relief="flat", font=self.fonts["small"])
        menu.add_command(label="Копировать правило",
                         command=lambda: self._copy_text(line, "Правило скопировано"))
        menu.add_command(label="Убрать из списка",
                         command=lambda: self._remove_custom_line(line))
        self._row_menu = menu
        try:
            menu.tk_popup(event.x_root, event.y_root)
        finally:
            menu.grab_release()

    def _copy_text(self, text, message):
        self.root.clipboard_clear()
        self.root.clipboard_append(text)
        self._toast(message, "info")

    # ---------- свои ID ----------

    def _render_custom_view(self):
        pal = self.pal
        inner = self.list_scroll.inner
        outer = tk.Frame(inner, bg=pal["bg"])
        outer.grid(row=0, column=0, sticky="ew", padx=16, pady=(2, 16))
        outer.grid_columnconfigure(0, weight=1)

        card = tk.Frame(outer, bg=pal["panel"],
                        highlightbackground=pal["border"], highlightthickness=1)
        card.grid(row=0, column=0, sticky="ew")
        card.grid_columnconfigure(0, weight=1)
        body = tk.Frame(card, bg=pal["panel"])
        body.grid(row=0, column=0, sticky="ew", padx=16, pady=14)

        head = tk.Frame(body, bg=pal["panel"])
        head.pack(fill="x")
        tk.Label(head, text="Редактор правил", bg=pal["panel"], fg=pal["text"],
                 font=self.fonts["body_bold"], anchor="w").pack(side="left")
        self._badge(head, "сохраняется автоматически", pal["blue_bg"],
                    pal["blue"]).pack(side="right")

        tk.Label(body, text="Одна строка — один ID или CSS-селектор. "
                            "Пустые строки и строки с // игнорируются.",
                 bg=pal["panel"], fg=pal["muted"], font=self.fonts["small"],
                 anchor="w", justify="left", wraplength=900).pack(
            fill="x", pady=(2, 8))

        text_frame = tk.Frame(body, bg=pal["panel"])
        text_frame.pack(fill="x")
        text_frame.grid_columnconfigure(0, weight=1)
        txt = tk.Text(
            text_frame, height=9, wrap="word", undo=True,
            bg=pal["input"], fg=pal["text"], insertbackground=pal["accent"],
            selectbackground=pal["selection"], selectforeground=pal["text"],
            relief="flat", bd=0, highlightthickness=1,
            highlightbackground=pal["border"], highlightcolor=pal["accent"],
            font=self.fonts["mono_input"], padx=10, pady=8,
        )
        txt.grid(row=0, column=0, sticky="nsew")
        sb = SlimScrollbar(text_frame, pal, bg=pal["input"], command=txt.yview)
        sb.grid(row=0, column=1, sticky="ns")
        txt.configure(yscrollcommand=sb.set)
        txt.insert("1.0", self.custom_text)
        txt.edit_modified(False)
        txt.bind("<<Modified>>", lambda e, w=txt: self._on_custom_modified(w))
        self._custom_widget = txt

        hint = tk.Label(body, text="Например: context-foo, #context-foo, "
                                   "menuitem#context-foo или полное CSS-правило.",
                        bg=pal["panel"], fg=pal["dim"], font=self.fonts["tiny"],
                        anchor="w", justify="left", wraplength=900)
        hint.pack(fill="x", pady=(6, 10))

        buttons = tk.Frame(body, bg=pal["panel"])
        buttons.pack(fill="x")
        apply_btn = self._make_button(buttons, "Применить в CSS", self._apply, "accent")
        apply_btn.pack(side="left")
        preview_btn = self._make_button(buttons, "Показать CSS", self._preview_css, "ghost")
        preview_btn.pack(side="left", padx=(8, 0))
        example_btn = self._make_button(buttons, "Вставить пример",
                                        self._insert_custom_example, "ghost")
        example_btn.pack(side="left", padx=(8, 0))
        clear_btn = self._make_button(buttons, "Очистить", self._clear_custom_text,
                                      "subtle")
        clear_btn.pack(side="right")

        info = tk.Frame(outer, bg=pal["panel_alt"],
                        highlightbackground=pal["border"], highlightthickness=1)
        info.grid(row=1, column=0, sticky="ew", pady=(10, 0))
        info_body = tk.Frame(info, bg=pal["panel_alt"])
        info_body.pack(fill="x", padx=16, pady=12)
        tk.Label(info_body, text="Как это работает", bg=pal["panel_alt"],
                 fg=pal["text"], font=self.fonts["small_bold"],
                 anchor="w").pack(fill="x")
        for line in (
            "Правила из этого раздела не нужно отмечать галочкой — они применяются всегда, "
            "пока есть в редакторе.",
            "Простые строки вида context-back превращаются в #context-back "
            "{ display: none !important; }.",
            "Полные правила вставляются в CSS как есть. Для скрытия используйте "
            "display: none !important.",
            "После изменения нажмите «Применить» внизу или здесь — CSS обновится, "
            "и браузер нужно будет перезапустить.",
        ):
            tk.Label(info_body, text="•  " + line, bg=pal["panel_alt"],
                     fg=pal["muted"], font=self.fonts["small"], anchor="w",
                     justify="left", wraplength=900).pack(fill="x", pady=(4, 0))

    def _on_custom_modified(self, widget):
        try:
            if not widget.edit_modified():
                return
            widget.edit_modified(False)
            self.custom_text = widget.get("1.0", "end-1c")
        except tk.TclError:
            return
        if self._custom_timer is not None:
            self.root.after_cancel(self._custom_timer)
        self._custom_timer = self.root.after(500, self._flush_custom_config)
        self._update_counts()
        self._update_footer()
        if self.view_key == CAT_CUSTOM:
            n = len(parse_custom_lines(self.custom_text)[2])
            text = f"{n} {plural_ru(n, 'правило', 'правила', 'правил')}"
            self.view_count.configure(text=text)
            self.results_label.configure(text=text)

    def _flush_custom_config(self):
        self._custom_timer = None
        self.cfg["custom"] = self.custom_text
        save_config(self.cfg)

    def _insert_custom_example(self):
        txt = self._custom_widget
        if txt is None:
            return
        example = "context-back\n#context-forward\nmenuitem#context-reload"
        current = txt.get("1.0", "end-1c").strip()
        txt.insert("end", ("\n" if current else "") + example)
        txt.focus_set()
        txt.mark_set("insert", "end-1c")
        txt.see("end")

    def _clear_custom_text(self):
        if not self.custom_text.strip():
            return
        choice = self._ask_options(
            "Очистить свои правила",
            "Все строки в редакторе будут удалены. Отмеченные галочками пункты "
            "останутся нетронутыми.",
            [("Очистить", "yes", "danger"), ("Отмена", None, "ghost")])
        if choice != "yes":
            return
        self.custom_text = ""
        self.cfg["custom"] = ""
        save_config(self.cfg)
        if self._custom_widget is not None:
            self._custom_widget.delete("1.0", "end")
        self._update_counts()
        self._update_footer()
        self._render_view(keep_scroll=True)
        self._toast("Редактор очищен", "info")

    # ---------- состояние и подвал ----------

    def _normalized_lines(self, text):
        _ids, _selectors, lines = parse_custom_lines(text)
        out = []
        for line in lines:
            if line.startswith("#") and IDENT_RE.fullmatch(line[1:]):
                out.append("id:" + line[1:])
            elif IDENT_RE.fullmatch(line):
                out.append("id:" + line)
            else:
                out.append("sel:" + re.sub(r"\s+", " ", line).strip())
        return tuple(sorted(set(out)))

    def _signature(self):
        return (tuple(sorted(self.checks)),
                self._normalized_lines(self.custom_text))

    def _is_dirty(self):
        return self._signature() != self.saved_sig

    def _set_dot(self, canvas, color):
        try:
            canvas.delete("all")
            canvas.create_oval(1, 1, 9, 9, fill=color, outline=color)
        except tk.TclError:
            pass

    def _update_footer(self):
        if not hasattr(self, "dirty_dot"):
            return
        pal = self.pal
        n = len(self.checks) + len(parse_custom_lines(self.custom_text)[2])
        plural = plural_ru(n, "правило", "правила", "правил")
        self.apply_btn.configure(text=f"Применить ({n})" if n else "Применить")
        if not self.profile_ok:
            self._set_dot(self.dirty_dot, pal["red"])
            self.dirty_label.configure(
                text="Профиль не выбран — CSS пока некуда записывать",
                fg=pal["muted"])
        elif self._is_dirty():
            self._set_dot(self.dirty_dot, pal["amber"])
            self.dirty_label.configure(
                text=f"{n} {plural} · есть неприменённые изменения",
                fg=pal["amber"])
        else:
            self._set_dot(self.dirty_dot, pal["green"])
            if self.css_exists:
                text = f"{n} {plural} · синхронизировано с userChrome.css"
            elif n:
                text = f"{n} {plural} · CSS ещё не создан"
            else:
                text = "Изменений нет"
            self.dirty_label.configure(text=text, fg=pal["muted"])

    def _persist_config(self):
        self.cfg["theme"] = self.theme
        self.cfg["show_separators"] = bool(self.show_separators.get())
        self.cfg["custom"] = self.custom_text
        if self.profile_var.get().strip():
            self.cfg["last_profile"] = self.profile_var.get().strip()
        if self.install_var.get().strip():
            self.cfg["last_install"] = self.install_var.get().strip()
        save_config(self.cfg)

    def _set_status(self, text):
        try:
            self.status_label.configure(text=text)
        except tk.TclError:
            pass

    # ---------- загрузка данных ----------

    def _startup(self):
        try:
            self.profile_choices = find_profiles()
            self.profile_combo.configure(values=self.profile_choices)
            if not normalize_profile(self.profile_var.get()) and self.profile_choices:
                self.profile_var.set(self.profile_choices[0])
            self._reload_all()
        except Exception as exc:
            self._set_status(f"Ошибка запуска: {exc}")
        self._refresh_titlebar()

    def _merge_custom_text(self, base, extras):
        out, seen = [], set()

        def add(line):
            line = line.strip()
            if not line:
                return
            if line.startswith("//"):
                out.append(line)
                return
            if line.startswith("#") and IDENT_RE.fullmatch(line[1:]):
                key = "id:" + line[1:]
            elif IDENT_RE.fullmatch(line):
                key = "id:" + line
            else:
                key = "sel:" + re.sub(r"\s+", " ", line)
            if key in seen:
                return
            seen.add(key)
            out.append(line)

        for text in (base or "", "\n".join(extras or [])):
            for raw in text.splitlines():
                add(raw)
        return "\n".join(out)

    def _state_from_css(self, ids, wids, all_ext, ext_sep, managed,
                        selectors=frozenset()):
        checks, extras = set(), []
        for iid in sorted(ids):
            if iid in self.entries:
                checks.add(iid)
            elif IDENT_RE.match(iid):
                extras.append("#" + iid)
            else:
                extras.append('[id="%s"]' % iid.replace('"', ""))
        for selector in sorted(selectors):
            key = SEL_PREFIX + selector
            if key in self.entries:
                checks.add(key)
        for wid in sorted(wids):
            key = EXT_PREFIX + wid
            if key in self.entries:
                checks.add(key)
            else:
                extras.append(f'[id^="{wid}-menuitem-"]')
        if all_ext and SPECIAL_ALL_EXT in self.entries:
            checks.add(SPECIAL_ALL_EXT)
        if ext_sep and SPECIAL_EXT_SEP in self.entries:
            checks.add(SPECIAL_EXT_SEP)
        extras.extend(extract_custom_rules(managed))
        return checks, extras

    def _reload_all(self, confirm=False, notify_error=False):
        if confirm and not self._confirm_discard():
            return False
        self._loading = True
        self._set_status("Сканирую браузер…")
        try:
            self.root.update_idletasks()
        except tk.TclError:
            pass

        prof = normalize_profile(self.profile_var.get())
        self.profile_ok = prof is not None
        self._current_profile = str(prof) if prof else ""
        if prof:
            self.profile_var.set(str(prof))
        else:
            self.profile_var.set("")

        typed = self.install_var.get().strip()
        install = None
        if typed and find_omni_files(typed):
            candidate = Path(typed)
            if candidate.name.lower() == "browser":
                candidate = candidate.parent
            install = candidate
        if install is None:
            install = find_install_dir(prof) if prof else find_install_dir(app_data_dir())
        self.install_dir = install
        if install:
            self.install_var.set(str(install))

        scanned, version, error = {}, "", ""
        if install:
            try:
                scanned, version = scan_browser(install)
            except Exception as exc:
                error = f"{type(exc).__name__}: {exc}"
        self.scan_ok = bool(install) and not error
        self.browser_found = self.scan_ok
        self.browser_name = browser_display_name(install) if install else ""
        self.browser_version = version

        new_ids = set()
        if self.scan_ok:
            try:
                new_ids, _prev = update_cache_and_find_new(
                    str(install), version, set(scanned))
            except Exception:
                new_ids = set()

        extensions = load_extensions(prof) if prof else []
        self.entries = build_entries(scanned, self.scan_ok, extensions, new_ids)
        self.new_ids = new_ids
        self.ext_names = {ext["wid"]: ext["name"] for ext in extensions}

        applied_checks, extras = set(), []
        self.state_external = ""
        self.css_exists = False
        if prof:
            css_path = Path(prof) / "chrome" / "userChrome.css"
            if css_path.is_file():
                self.css_exists = True
                try:
                    css_text = css_path.read_text(encoding="utf-8", errors="replace")
                except OSError:
                    css_text = ""
                managed, self.state_external = split_managed(css_text)
                ids, wids, all_ext, ext_sep = parse_hidden(managed)
                applied_checks, extras = self._state_from_css(
                    ids, wids, all_ext, ext_sep, managed,
                    parse_hidden_selectors(managed))

        self.checks = set(applied_checks)
        self.saved_state = set(applied_checks)
        self.custom_text = self._merge_custom_text(
            str(self.cfg.get("custom", "") or ""), extras)
        self.saved_sig = (tuple(sorted(applied_checks)),
                          self._normalized_lines("\n".join(extras)))
        self.pref_state = (userchrome_pref_state(prof, self.browser_name)
                           if prof else None)

        self.cfg["last_profile"] = str(prof) if prof else ""
        self.cfg["last_install"] = str(install) if install else ""
        self.cfg["custom"] = self.custom_text
        save_config(self.cfg)

        try:
            self.root.title(
                f"{APP_NAME} — {self.browser_name} {self.browser_version}"
                if self.browser_found else f"{APP_NAME} {APP_VERSION}")
        except tk.TclError:
            pass

        self._update_pill()
        self._update_pref_hint()
        self._update_counts()
        self._update_nav_styles()
        self._render_view()
        self._update_footer()

        if not self.profile_ok:
            self._set_status("Профиль не выбран. Укажите его сверху — "
                             "без профиля CSS записать нельзя.")
        elif error:
            self._set_status(f"Не удалось прочитать браузер: {error}")
        elif not self.scan_ok:
            self._set_status("Браузер не найден. Показан встроенный список пунктов.")
        else:
            count = sum(1 for e in self.entries.values() if not e["missing"])
            self._set_status(
                f"Готово · {self.browser_name} {self.browser_version} · "
                f"{count} {plural_ru(count, 'пункт', 'пункта', 'пунктов')} · "
                f"профиль {Path(prof).name}")
        self._loading = False
        if notify_error and error:
            self._toast("Ошибка чтения браузера: " + error, "err")
        return True

    def _on_profile_changed(self, _event=None):
        if self._loading:
            return
        if not self._confirm_discard():
            self.profile_var.set(getattr(self, "_current_profile", "") or "")
            return
        self._reload_all()

    def _browse_profile(self):
        if not self._confirm_discard():
            return
        current = normalize_profile(self.profile_var.get())
        path = filedialog.askdirectory(
            parent=self.root, title="Выберите папку профиля браузера",
            initialdir=str(current) if current else str(Path.home()))
        if not path:
            return
        self.profile_var.set(path)
        self._reload_all()

    def _browse_install(self):
        if not self._confirm_discard():
            return
        current = self.install_dir
        path = filedialog.askdirectory(
            parent=self.root, title="Выберите папку установки браузера (где лежит omni.ja)",
            initialdir=str(current) if current else str(Path.home()))
        if not path:
            return
        self.install_var.set(path)
        self._reload_all()

    def _detect_profiles(self):
        if not self._confirm_discard():
            return
        profiles = find_profiles()
        self.profile_choices = profiles
        self.profile_combo.configure(values=profiles)
        if profiles:
            self.profile_var.set(profiles[0])
            self._reload_all()
            self._toast(f"Найдено профилей: {len(profiles)}. Выбран самый свежий.",
                        "ok")
        else:
            self._toast("Профили не найдены. Укажите папку вручную.", "warn")

    # ---------- применение ----------

    def _checked_keys_ordered(self):
        def key_fn(key):
            entry = self.entries.get(key)
            if entry is None:
                return ((9999, 0, 0, ""), key)
            return (entry_sort_key(entry), key)
        return sorted(self.checks, key=key_fn)

    def _collect_css(self):
        keys = self._checked_keys_ordered()
        ids, selectors, _lines = parse_custom_lines(self.custom_text)
        raw = []
        for iid in ids:
            if iid in self.checks:
                continue
            selector = id_selector(iid)
            if selector not in raw:
                raw.append(selector)
        for selector in selectors:
            if selector not in raw:
                raw.append(selector)
        return generate_css(keys, self.ext_names, raw)

    def _set_custom_text(self, text):
        self.custom_text = text
        widget = self._custom_widget
        if widget is not None:
            try:
                if widget.winfo_exists():
                    widget.delete("1.0", "end")
                    widget.insert("1.0", text)
            except tk.TclError:
                pass
        self._update_footer()

    def _write_css_file(self, css_path, text):
        backup = None
        css_path.parent.mkdir(parents=True, exist_ok=True)
        if css_path.is_file():
            stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
            backup = css_path.with_name(f"userChrome.css.bak-{stamp}")
            suffix = 2
            while backup.exists():
                backup = css_path.with_name(
                    f"userChrome.css.bak-{stamp}-{suffix}")
                suffix += 1
            shutil.copy2(css_path, backup)
        tmp = css_path.with_name("userChrome.css.tmp")
        tmp.write_text(text, encoding="utf-8", newline="\n")
        os.replace(tmp, css_path)
        self._rotate_backups(css_path.parent)
        return backup

    def _apply(self):
        if self._custom_widget is not None:
            try:
                if self._custom_widget.winfo_exists():
                    self.custom_text = self._custom_widget.get("1.0", "end-1c")
            except tk.TclError:
                pass
        self._update_footer()
        profile = normalize_profile(self.profile_var.get())
        if not profile:
            self._ask_options(
                "Профиль не выбран",
                "Укажите папку профиля браузера в верхней части окна. "
                "Без профиля невозможно записать userChrome.css.",
                [("Понятно", None, "ghost")])
            return False
        if self.pref_state is False:
            if not self._confirm_pref_state():
                return False

        managed = self._collect_css()
        css_path = Path(profile) / "chrome" / "userChrome.css"
        try:
            current = css_path.read_text(encoding="utf-8", errors="replace") \
                if css_path.is_file() else ""
        except OSError:
            current = ""
        _old_managed, external = split_managed(current)
        external = external.rstrip("\n")
        text = (external + "\n\n" if external.strip() else "") + managed

        external_issues = css_issues(mask_managed(current))
        custom_issues = css_issues(self.custom_text)
        fixed_note = ""
        if external_issues or custom_issues:
            ext_fixed, ext_fixes = (repair_css(current) if external_issues
                                    else (current, 0))
            cus_fixed, cus_fixes = (repair_custom_lines(self.custom_text)
                                    if custom_issues else (self.custom_text, 0))
            can_fix = (ext_fixed is not None and cus_fixed is not None
                       and not css_issues(cus_fixed))
            parts = []
            if external_issues:
                parts.append("Вне секции программы:\n" +
                             format_css_issues(external_issues))
            if custom_issues:
                parts.append("«Свои ID и селекторы»:\n" +
                             format_css_issues(custom_issues))
            parts.append("Пока ошибка не исправлена, браузер может "
                         "игнорировать секцию программы.")
            if can_fix:
                parts.append("Программа может закрыть незакрытые блоки "
                             "автоматически.")
                buttons = [("Исправить и записать", "fix", "accent"),
                           ("Записать всё равно", "yes", "ghost"),
                           ("Отмена", None, "ghost")]
            else:
                if ext_fixed is None:
                    parts.append("Во внешнем CSS есть лишние «}» — их нужно "
                                 "убрать вручную.")
                if cus_fixed is None:
                    parts.append("В правилах редактора есть ошибка, которую "
                                 "нельзя исправить автоматически.")
                buttons = [("Записать всё равно", "yes", "accent"),
                           ("Отмена", None, "ghost")]
            choice = self._ask_options("В CSS есть ошибки",
                                       "\n\n".join(parts), buttons)
            if choice == "fix":
                fixes = 0
                if external_issues and ext_fixed is not None:
                    current = ext_fixed
                    _old_managed, external = split_managed(current)
                    external = external.rstrip("\n")
                    fixes += ext_fixes
                if custom_issues and cus_fixed is not None:
                    self._set_custom_text(cus_fixed)
                    managed = self._collect_css()
                    fixes += cus_fixes
                text = (external + "\n\n" if external.strip() else "") + managed
                fixed_note = f" · исправлено: {fixes}"
            elif choice != "yes":
                self._set_status("CSS не записан: найдены ошибки")
                return False

        if current == text:
            self.css_exists = True
            self.state_external = external
            self.saved_state = set(self.checks)
            self.saved_sig = self._signature()
            self._update_footer()
            self._set_status("CSS уже актуален")
            self._toast("Изменений нет — CSS уже актуален", "info")
            return True

        try:
            backup = self._write_css_file(css_path, text)
        except OSError as exc:
            self._ask_options(
                "Не удалось записать CSS",
                f"{css_path}\n\n{exc}",
                [("Закрыть", None, "ghost")])
            self._set_status("Ошибка записи userChrome.css")
            return False

        self.css_exists = True
        self.state_external = external
        self.saved_state = set(self.checks)
        self.saved_sig = self._signature()
        self.pref_state = userchrome_pref_state(profile, self.browser_name)
        self.cfg["last_profile"] = str(profile)
        self.cfg["custom"] = self.custom_text
        save_config(self.cfg)

        for row in list(self.row_by_key.values()):
            self._refresh_row(row)
        self._update_counts()
        self._update_footer()
        self._update_pref_hint()

        note = "CSS обновлён · полностью перезапустите браузер" + fixed_note
        if backup is not None:
            note += f" · копия: {backup.name}"
        self._toast(note, "ok")
        self._set_status(f"Записано {datetime.now():%H:%M:%S} · {css_path}"
                         f"{fixed_note}")
        return True

    def _rotate_backups(self, folder):
        try:
            backups = sorted(folder.glob("userChrome.css.bak-*"),
                             key=lambda p: p.stat().st_mtime, reverse=True)
            for old in backups[10:]:
                try:
                    old.unlink()
                except OSError:
                    pass
        except OSError:
            pass

    def _confirm_pref_state(self):
        message = (
            "В выбранном профиле отключена загрузка userChrome.css.\n\n"
            "Откройте about:config, найдите параметр "
            "toolkit.legacyUserProfileCustomizations.stylesheets, "
            "установите true и перезапустите браузер. Иначе стили не применятся.\n\n"
            "Записать CSS всё равно?")
        return self._ask_options(
            "Проверьте загрузку userChrome.css", message,
            [("Записать всё равно", "yes", "accent"),
             ("Отмена", None, "ghost")]) == "yes"

    def _confirm_discard(self):
        if not self._is_dirty():
            return True
        return self._ask_options(
            "Есть неприменённые изменения",
            "Отметки ещё не записаны в userChrome.css. Продолжить без сохранения?",
            [("Продолжить", "yes", "danger"),
             ("Отмена", None, "ghost")]) == "yes"

    def _manual_reload(self, _event=None):
        self._reload_all(confirm=True, notify_error=True)
        return "break"

    def _hotkey_apply(self, _event=None):
        self._apply()
        return "break"

    # ---------- окна ----------

    def _center(self, top, width, height):
        top.update_idletasks()
        rx, ry = self.root.winfo_rootx(), self.root.winfo_rooty()
        rw, rh = self.root.winfo_width(), self.root.winfo_height()
        x = rx + max(0, (rw - width) // 2)
        y = ry + max(0, (rh - height) // 2)
        top.geometry(f"{width}x{height}+{x}+{y}")

    def _ask_options(self, title, message, buttons):
        pal = self.pal
        top = tk.Toplevel(self.root)
        top.title(title)
        top.configure(bg=pal["bg"])
        top.transient(self.root)
        top.resizable(False, False)
        card = tk.Frame(top, bg=pal["panel"],
                        highlightbackground=pal["border"], highlightthickness=1)
        card.pack(fill="both", expand=True)
        body = tk.Frame(card, bg=pal["panel"])
        body.pack(fill="both", expand=True, padx=24, pady=20)
        tk.Label(body, text=title, bg=pal["panel"], fg=pal["text"],
                 font=self.fonts["title"], anchor="w", justify="left",
                 wraplength=540).pack(fill="x")
        tk.Label(body, text=message, bg=pal["panel"], fg=pal["muted"],
                 font=self.fonts["small"], anchor="w", justify="left",
                 wraplength=540).pack(fill="x", pady=(8, 0))
        row = tk.Frame(body, bg=pal["panel"])
        row.pack(fill="x", pady=(18, 0))
        result = {"value": None}

        def close(value):
            result["value"] = value
            try:
                top.grab_release()
            except tk.TclError:
                pass
            top.destroy()

        for label, value, kind in buttons:
            btn = self._make_button(row, label, lambda v=value: close(v),
                                    kind or "ghost")
            btn.pack(side="right", padx=(8, 0))
        top.protocol("WM_DELETE_WINDOW", lambda: close(None))
        top.bind("<Escape>", lambda e: close(None))
        top.update_idletasks()
        width = max(top.winfo_reqwidth(), 560)
        height = top.winfo_reqheight()
        self._center(top, width, height)
        top.grab_set()
        self.root.wait_window(top)
        return result["value"]

    def _text_window(self, title, subtitle, body, mono=True):
        pal = self.pal
        top = tk.Toplevel(self.root)
        top.title(title)
        top.configure(bg=pal["bg"])
        top.transient(self.root)
        header = tk.Frame(top, bg=pal["bg"])
        header.pack(fill="x", padx=18, pady=(14, 8))
        tk.Label(header, text=title, bg=pal["bg"], fg=pal["text"],
                 font=self.fonts["title"], anchor="w").pack(fill="x")
        tk.Label(header, text=subtitle, bg=pal["bg"], fg=pal["muted"],
                 font=self.fonts["small"], anchor="w", justify="left",
                 wraplength=800).pack(fill="x", pady=(2, 0))
        frame = tk.Frame(top, bg=pal["panel"],
                         highlightbackground=pal["border"], highlightthickness=1)
        frame.pack(fill="both", expand=True, padx=16, pady=(0, 12))
        frame.grid_columnconfigure(0, weight=1)
        frame.grid_rowconfigure(0, weight=1)
        txt = tk.Text(frame, wrap="word", bg=pal["input"], fg=pal["text"],
                      insertbackground=pal["accent"], relief="flat", bd=0,
                      font=self.fonts["mono_input" if mono else "small"],
                      padx=12, pady=10, selectbackground=pal["selection"],
                      selectforeground=pal["text"])
        txt.grid(row=0, column=0, sticky="nsew")
        sb = SlimScrollbar(frame, pal, bg=pal["input"], command=txt.yview)
        sb.grid(row=0, column=1, sticky="ns")
        txt.configure(yscrollcommand=sb.set)
        txt.insert("1.0", body)
        txt.configure(state="disabled")
        buttons = tk.Frame(top, bg=pal["bg"])
        buttons.pack(fill="x", padx=18, pady=(0, 14))
        close_btn = self._make_button(buttons, "Закрыть", top.destroy, "ghost")
        close_btn.pack(side="right")
        copy_btn = self._make_button(
            buttons, "Копировать CSS",
            lambda: self._copy_text(body, "CSS скопирован"), "accent")
        copy_btn.pack(side="right", padx=(8, 0))
        top.bind("<Escape>", lambda e: top.destroy())
        self._center(top, 880, 620)
        top.grab_set()
        self.root.wait_window(top)

    def _check_css(self):
        if self._custom_widget is not None:
            try:
                if self._custom_widget.winfo_exists():
                    self.custom_text = self._custom_widget.get("1.0", "end-1c")
            except tk.TclError:
                pass
        profile = normalize_profile(self.profile_var.get())
        if not profile:
            self._ask_options(
                "Профиль не выбран",
                "Укажите папку профиля браузера в верхней части окна.",
                [("Понятно", None, "ghost")])
            return
        css_path = Path(profile) / "chrome" / "userChrome.css"
        try:
            current = css_path.read_text(encoding="utf-8", errors="replace") \
                if css_path.is_file() else ""
        except OSError as exc:
            self._ask_options(
                "Не удалось прочитать CSS",
                f"{css_path}\n\n{exc}",
                [("Закрыть", None, "ghost")])
            return

        external_issues = css_issues(mask_managed(current))
        custom_issues = css_issues(self.custom_text)
        has_errors = bool(external_issues or custom_issues)
        ext_fixed = cus_fixed = None
        ext_fixes = cus_fixes = 0
        can_fix = False
        if has_errors:
            ext_fixed, ext_fixes = (repair_css(current) if external_issues
                                    else (current, 0))
            cus_fixed, cus_fixes = (repair_custom_lines(self.custom_text)
                                    if custom_issues else (self.custom_text, 0))
            can_fix = (ext_fixed is not None and cus_fixed is not None
                       and not css_issues(cus_fixed))
        parts = []
        if external_issues:
            parts.append("Вне секции программы:\n" +
                         format_css_issues(external_issues))
        if custom_issues:
            parts.append("«Свои ID и селекторы»:\n" +
                         format_css_issues(custom_issues))
        if has_errors:
            parts.append("Правила после такой ошибки браузер может "
                         "игнорировать, включая секцию программы.")
            if can_fix:
                parts.append("Можно исправить автоматически.")
            elif ext_fixed is None:
                parts.append("Во внешнем CSS есть лишние «}» — их нужно "
                             "убрать вручную.")
            else:
                parts.append("В правилах редактора есть ошибка, которую "
                             "нельзя исправить автоматически.")
        else:
            parts.append("Ошибок не найдено: скобки, кавычки и комментарии "
                         "сбалансированы.")
            if css_path.is_file():
                parts.append(f"Файл: {css_path}")
            else:
                parts.append("Файл userChrome.css ещё не создан — проверены "
                             "только правила из редактора.")
        if self.pref_state is False:
            parts.append("Внимание: в профиле отключена загрузка userChrome.css "
                         "(toolkit.legacyUserProfileCustomizations.stylesheets = false).")
        buttons = ([("Исправить", "fix", "accent"),
                    ("Закрыть", None, "ghost")] if can_fix
                   else [("Закрыть", None, "ghost")])
        choice = self._ask_options("Проверка CSS", "\n\n".join(parts), buttons)
        if choice != "fix":
            self._set_status("CSS проверен: найдены ошибки" if has_errors
                             else "CSS проверен: ошибок нет")
            return

        fixes = 0
        wrote = None
        custom_changed = False
        try:
            if external_issues and ext_fixed is not None:
                wrote = self._write_css_file(css_path, ext_fixed)
                fixes += ext_fixes
            if custom_issues and cus_fixed is not None:
                self._set_custom_text(cus_fixed)
                fixes += cus_fixes
                custom_changed = True
        except OSError as exc:
            self._ask_options(
                "Не удалось записать CSS",
                f"{css_path}\n\n{exc}",
                [("Закрыть", None, "ghost")])
            self._set_status("Ошибка записи userChrome.css")
            return
        done = [f"Исправлено: {fixes}."]
        if wrote is not None:
            done.append(f"Файл userChrome.css перезаписан. Копия: {wrote.name}.")
            done.append("Перезапустите браузер или пересоберите стили Floorp.")
        if custom_changed:
            done.append("Правила в редакторе обновлены. Нажмите «Применить», "
                        "чтобы записать их в CSS.")
        self._ask_options("CSS исправлен", "\n\n".join(done),
                          [("Закрыть", None, "ghost")])
        self._set_status(f"CSS исправлен · ошибок: {fixes}")

    def _preview_css(self):
        managed = self._collect_css()
        profile = normalize_profile(self.profile_var.get())
        if profile:
            subtitle = str(Path(profile) / "chrome" / "userChrome.css")
        else:
            subtitle = "Профиль не выбран — показан только текст, который будет записан."
        self._text_window("Предпросмотр CSS", subtitle, managed, mono=True)

    def _show_help(self):
        pal = self.pal
        top = tk.Toplevel(self.root)
        top.title("Справка")
        top.configure(bg=pal["bg"])
        top.transient(self.root)
        header = tk.Frame(top, bg=pal["bg"])
        header.pack(fill="x", padx=18, pady=(14, 8))
        tk.Label(header, text=f"{APP_NAME} {APP_VERSION}", bg=pal["bg"],
                 fg=pal["text"], font=self.fonts["title"], anchor="w").pack(fill="x")
        tk.Label(header, text="Справка по разделам, настройке и безопасности.",
                 bg=pal["bg"], fg=pal["muted"], font=self.fonts["small"],
                 anchor="w").pack(fill="x", pady=(2, 0))
        frame = tk.Frame(top, bg=pal["panel"],
                         highlightbackground=pal["border"], highlightthickness=1)
        frame.pack(fill="both", expand=True, padx=16, pady=(0, 12))
        frame.grid_columnconfigure(0, weight=1)
        frame.grid_rowconfigure(0, weight=1)
        txt = tk.Text(frame, wrap="word", bg=pal["input"], fg=pal["text"],
                      relief="flat", bd=0, font=self.fonts["small"],
                      padx=18, pady=12, selectbackground=pal["selection"],
                      selectforeground=pal["text"])
        txt.grid(row=0, column=0, sticky="nsew")
        sb = SlimScrollbar(frame, pal, bg=pal["input"], command=txt.yview)
        sb.grid(row=0, column=1, sticky="ns")
        txt.configure(yscrollcommand=sb.set)
        txt.tag_configure("h1", font=self.fonts["title_big"],
                          foreground=pal["accent"], spacing3=8)
        txt.tag_configure("h2", font=self.fonts["body_bold"],
                          foreground=pal["text"], spacing1=14, spacing3=4)
        txt.tag_configure("p", font=self.fonts["small"], foreground=pal["muted"],
                          spacing3=6)
        txt.tag_configure("code", font=self.fonts["mono_input"],
                          foreground=pal["blue"], background=pal["panel_alt"],
                          spacing1=4, spacing3=6, lmargin1=12, lmargin2=12)
        sections = [
            ("h1", f"{APP_NAME} {APP_VERSION}"),
            ("p", "Программа скрывает пункты контекстных меню Firefox, Floorp, "
                  "LibreWolf, Waterfox и Zen через userChrome.css."),
            ("h2", "Как пользоваться"),
            ("p", "1. Проверьте профиль и папку браузера сверху — обычно они "
                  "находятся автоматически.\n"
                  "2. Выберите раздел слева: по действию (ссылки, текст, вкладки) "
                  "или по меню браузера (боковая панель, закладки, панели).\n"
                  "3. Отметьте пункты, которые нужно скрыть. Отметки можно ставить "
                  "в любом разделе, в том числе в «Все пункты» и «Нет в этой версии».\n"
                  "4. Нажмите «Применить» внизу (Ctrl+S).\n"
                  "5. Полностью перезапустите браузер — userChrome.css читается "
                  "только при запуске."),
            ("h2", "Разделы"),
            ("p", "«Все пункты» — единый список всего, что найдено, с поиском.\n"
                  "«Выбранные» — только отмеченные пункты и свои правила.\n"
                  "«Новое» — пункты, появившиеся после обновления браузера.\n"
                  "«Расширения» — общие правила для пунктов расширений и отдельные "
                  "расширения.\n"
                  "«Свои ID» — собственные ID и CSS-селекторы.\n"
                  "«Нет в этой версии» — пункты из встроенного списка, которых нет "
                  "в установленном браузере. Их можно отметить заранее."),
            ("h2", "Важно: userChrome.css"),
            ("p", "Firefox загружает userChrome.css только если параметр "
                  "toolkit.legacyUserProfileCustomizations.stylesheets включён "
                  "(about:config, значение true). После изменения параметра нужен "
                  "перезапуск. Программа не меняет prefs.js самостоятельно."),
            ("h2", "Свои ID и селекторы"),
            ("p", "Одна строка — одно правило. Можно писать ID без решётки, с решёткой "
                  "или готовый CSS-селектор:"),
            ("code", "context-foo\n#context-foo\nmenuitem#context-foo"),
            ("p", "Строки с // игнорируются. Правила сохраняются автоматически и "
                  "записываются в CSS по кнопке «Применить»."),
            ("h2", "Безопасность"),
            ("p", "Перед каждой записью создаётся резервная копия "
                  "userChrome.css.bak-ГГГГММДД-ЧЧММСС (хранятся последние 10). "
                  "Управляемая секция между маркерами заменяется целиком, остальной "
                  "CSS сохраняется без изменений. Если что-то пошло не так, верните "
                  "резервную копию на место userChrome.css.\n"
                  "Перед записью проверяется баланс скобок: если в вашем CSS есть "
                  "незакрытый блок, программа предупредит и предложит «Исправить "
                  "и записать» — закроет скобки, комментарии и кавычки, сохранив "
                  "резервную копию. Кнопка «Проверить CSS» делает такую проверку "
                  "и исправление вручную."),
            ("h2", "Горячие клавиши"),
            ("p", "Ctrl+F — поиск · Ctrl+S — применить · F5 — пересканировать\n"
                  "Ctrl+Tab — следующий раздел · Ctrl+Shift+Tab — предыдущий\n"
                  "Ctrl+1…9 — раздел по номеру · Esc — очистить поиск"),
        ]
        for tag, text in sections:
            txt.insert("end", text + "\n", tag)
        txt.configure(state="disabled")
        buttons = tk.Frame(top, bg=pal["bg"])
        buttons.pack(fill="x", padx=18, pady=(0, 14))
        self._make_button(buttons, "Закрыть", top.destroy, "ghost").pack(side="right")
        top.bind("<Escape>", lambda e: top.destroy())
        self._center(top, 780, 660)
        top.grab_set()
        self.root.wait_window(top)

    def _toast(self, text, kind="ok"):
        pal = self.pal
        if self._toast_timer is not None:
            try:
                self.root.after_cancel(self._toast_timer)
            except tk.TclError:
                pass
            self._toast_timer = None
        if self._toast_frame is not None:
            try:
                self._toast_frame.destroy()
            except tk.TclError:
                pass
            self._toast_frame = None
        colors = {
            "ok": (pal["green_bg"], pal["green"]),
            "info": (pal["blue_bg"], pal["blue"]),
            "warn": (pal["amber_bg"], pal["amber"]),
            "err": (pal["red_bg"], pal["red"]),
        }
        bg, fg = colors.get(kind, colors["info"])
        frame = tk.Frame(self.root, bg=bg,
                         highlightbackground=fg, highlightthickness=1)
        self._badge(frame, "●", bg, fg).pack(side="left", padx=(10, 0), pady=8)
        tk.Label(frame, text=text, bg=bg, fg=pal["text"],
                 font=self.fonts["small"], justify="left",
                 wraplength=480).pack(side="left", padx=(6, 12), pady=8)
        frame.place(relx=1.0, rely=1.0, x=-22, y=-84, anchor="se")
        self._toast_frame = frame
        self._toast_timer = self.root.after(3800, self._hide_toast)

    def _hide_toast(self):
        self._toast_timer = None
        if self._toast_frame is not None:
            try:
                self._toast_frame.destroy()
            except tk.TclError:
                pass
            self._toast_frame = None

    # ---------- тема, выход, клавиши ----------

    def _refresh_titlebar(self):
        try:
            self.root.after(60, lambda: set_windows_dark_titlebar(
                self.root, self.theme == "dark"))
        except tk.TclError:
            pass

    def _toggle_theme(self):
        if self._custom_widget is not None:
            try:
                if self._custom_widget.winfo_exists():
                    self.custom_text = self._custom_widget.get("1.0", "end-1c")
            except tk.TclError:
                pass
        self.theme = "light" if self.theme == "dark" else "dark"
        self.pal = PALETTES[self.theme]
        self.cfg["theme"] = self.theme
        save_config(self.cfg)
        self._ui_ready = False
        geometry = self.root.geometry()
        state = self.root.state()
        if self._search_timer is not None:
            self.root.after_cancel(self._search_timer)
            self._search_timer = None
        self.search_var.set("")
        if self._toast_frame is not None:
            try:
                self._toast_frame.destroy()
            except tk.TclError:
                pass
            self._toast_frame = None
        self._build_ui()
        try:
            if state == "zoomed":
                self.root.state("zoomed")
            elif state == "normal":
                self.root.geometry(geometry)
        except tk.TclError:
            pass
        self._refresh_titlebar()
        self._set_status("Тема изменена")

    def _open_chrome_folder(self):
        profile = normalize_profile(self.profile_var.get())
        if not profile:
            self._ask_options("Профиль не выбран",
                              "Сначала укажите профиль браузера.",
                              [("Понятно", None, "ghost")])
            return
        folder = Path(profile) / "chrome"
        try:
            folder.mkdir(parents=True, exist_ok=True)
            if sys.platform == "win32":
                os.startfile(str(folder))
            elif sys.platform == "darwin":
                subprocess.Popen(["open", str(folder)])
            else:
                subprocess.Popen(["xdg-open", str(folder)])
        except OSError as exc:
            self._toast(f"Не удалось открыть папку: {exc}", "err")

    def _on_close(self):
        if self._custom_widget is not None:
            try:
                if self._custom_widget.winfo_exists():
                    self.custom_text = self._custom_widget.get("1.0", "end-1c")
            except tk.TclError:
                pass
        self.cfg["custom"] = self.custom_text
        self.cfg["geometry"] = self.root.geometry()
        save_config(self.cfg)
        if self._is_dirty():
            n = len(self.checks) + len(parse_custom_lines(self.custom_text)[2])
            choice = self._ask_options(
                "Выйти из программы?",
                f"Есть неприменённые изменения ({n} "
                f"{plural_ru(n, 'правило', 'правила', 'правил')}). "
                "Записать их в userChrome.css перед выходом?",
                [("Сохранить и выйти", "save", "accent"),
                 ("Выйти без сохранения", "discard", "ghost"),
                 ("Отмена", None, "ghost")])
            if choice == "save":
                if not self._apply():
                    return
            elif choice != "discard":
                return
        try:
            self.root.destroy()
        except tk.TclError:
            pass

    def _bind_shortcuts(self):
        root = self.root
        root.bind("<Control-f>", lambda e: self._focus_search())
        root.bind("<Control-F>", lambda e: self._focus_search())
        root.bind("<Control-s>", self._hotkey_apply)
        root.bind("<Control-S>", self._hotkey_apply)
        root.bind("<F5>", self._manual_reload)
        root.bind("<Control-Tab>", lambda e: self._cycle_view(1))
        root.bind("<Control-Shift-Tab>", lambda e: self._cycle_view(-1))
        root.bind("<Control-ISO_Left_Tab>", lambda e: self._cycle_view(-1))
        for number in range(1, 10):
            root.bind(f"<Control-Key-{number}>",
                      lambda e, n=number: self._jump_view(n))
        root.bind("<Escape>", self._on_escape)
        root.bind_all("<MouseWheel>", self._on_wheel, add="+")

    def _on_wheel(self, event):
        try:
            widget = self.root.winfo_containing(event.x_root, event.y_root)
        except (tk.TclError, KeyError):
            return
        while widget is not None:
            if isinstance(widget, tk.Text):
                return
            if isinstance(widget, ScrollFrame):
                delta = event.delta
                if delta:
                    steps = int(-delta / 120)
                    if steps == 0:
                        steps = -1 if delta > 0 else 1
                    try:
                        widget.canvas.yview_scroll(steps * 3, "units")
                    except tk.TclError:
                        pass
                return "break"
            widget = getattr(widget, "master", None)
        return None


def main():
    enable_dpi_awareness()
    root = tk.Tk()
    apply_tk_scaling(root)
    FirefoxContextMenuHider(root)
    root.mainloop()


if __name__ == "__main__":
    main()



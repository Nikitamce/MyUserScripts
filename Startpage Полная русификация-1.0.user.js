// ==UserScript==
// @name         Startpage Полная русификация
// @namespace    https://github.com/yourname
// @version      1.0
// @description  Полная русификация Startpage.com (все фильтры + сообщения)
// @author       Nikita
// @match        https://www.startpage.com/*
// @match        https://startpage.com/*
// @grant        none
// @run-at       document-idle
// @license MIT
// ==/UserScript==

(function () {
    'use strict';

    const dict = {
        // === Новые фильтры из твоих скринов ===
        "Filter explicit content": "Фильтровать явный контент",
        "Strict": "Строгий",
        "Moderate": "Умеренный",
        "Off": "Выкл",

        "Filter by license": "Фильтровать по лицензии",
        "Any license": "Любая лицензия",
        "Public domain": "Общественное достояние",
        "Free to share": "Можно делиться",
        "Free to share commercially": "Можно делиться коммерчески",
        "Free to modify": "Можно изменять",
        "Free to modify commercially": "Можно изменять коммерчески",

        "Filter by image type": "Фильтровать по типу изображения",
        "Any type": "Любой тип",
        "Animated Gif": "Анимированный Gif",
        "Clip Art": "Клип-арт",
        "Line Drawing": "Линейный рисунок",
        "Photograph": "Фотография",
        "Transparent Background": "Прозрачный фон",

        "Filter by color": "Фильтровать по цвету",
        "Any color": "Любой цвет",
        "Color only": "Только цветное",
        "Black & white": "Чёрно-белое",

        "Filter by size": "Фильтровать по размеру",
        "Any size": "Любой размер",
        "Small": "Маленький",
        "Medium": "Средний",
        "Large": "Большой",
        "Wallpaper": "Обои",
        "Larger than:": "Больше чем:",
        "Select image size": "Выберите размер изображения",
        "Update": "Обновить",

        "Filter by video length": "Фильтровать по длительности видео",
        "Any length": "Любая длительность",
        "Short": "Короткое",
        "Medium": "Среднее",
        "Long": "Длинное",

        "Filter by relevance": "Фильтровать по релевантности",
        "Relevant": "Релевантные",
        "Popular": "Популярные",
        "Recent": "Недавние",

        "Choose time period": "Выберите период времени",
        "Past 24 hours": "За последние 24 часа",
        "Past week": "За последнюю неделю",
        "Past month": "За последний месяц",
        "Past year": "За последний год",

        // Сообщение об отсутствии результатов
        "Uh-oh, there are no results for this search.": "Упс, по этому запросу ничего не найдено.",
        "Let's see, it could be due to:": "Давайте посмотрим, это может быть из-за:",
        "The search string might be too specific. Try a more generic search.": "Поисковый запрос может быть слишком специфичным. Попробуйте более общий запрос.",
        "Spelling might be incorrect. Check that all words are spelled correctly.": "Возможно, опечатка. Проверьте правильность написания всех слов.",
        "Your filter settings might be too strict and could be blocking results. Try changing those in the toolbar above.": "Ваши настройки фильтров могут быть слишком строгими и блокировать результаты. Попробуйте изменить их в панели инструментов выше.",

        // === Все предыдущие переводы (полностью сохранены) ===
        "Don’t want a cookie? Copy the Настройки URL below, then bookmark or set as homepage.": "Не хотите cookie? Скопируйте URL Настроек ниже, затем добавьте в закладки или установите как домашнюю страницу.",
        "See the full list of our Instant Answers.": "Посмотреть полный список наших Мгновенных ответов.",

        "All regions": "Все регионы",
        "Safe Search: Moderate": "Безопасный поиск: Умеренный",
        "Any time": "За любое время",
        "Web results": "Веб-результаты",

        "Save your settings": "Сохранить настройки",
        "This will save your settings with a browser cookie. It will reset if you delete your cookies.": "Это сохранит ваши настройки с помощью cookie браузера. Настройки сбросятся, если вы удалите cookie.",
        "Copy": "Копировать",

        "Privacy and Safety": "Конфиденциальность и безопасность",
        "Safe Search": "Безопасный поиск",
        "Helps reduce the amount of adult content returned in Startpage searches, images, and videos": "Помогает уменьшить количество контента для взрослых в результатах поиска Startpage, изображениях и видео",
        "HTTP request method": "Метод HTTP-запроса",
        "POST hides your query from the URL and browser tab. GET includes them.": "POST скрывает ваш запрос из URL и вкладки браузера. GET включает их.",
        "Show safety suggestion": "Показывать предложение по безопасности",
        "Anonymous view only: When enabled, Startpage warns you if the site you are trying to view has been flagged as containing malware.": "Только анонимный просмотр: При включении Startpage предупреждает вас, если сайт, который вы пытаетесь просмотреть, помечен как содержащий вредоносное ПО.",
        "Server region": "Регион серверов",
        "Startpage will only connect to servers in the selected region": "Startpage будет подключаться только к серверам в выбранном регионе",
        "Closest / Fastest": "Ближайший / Самый быстрый",

        "Appearance": "Внешний вид",
        "Instant Answers": "Мгновенные ответы",
        "Show instant answers and relevant news, image, or video widgets on the search results page.": "Показывать мгновенные ответы и релевантные виджеты новостей, изображений или видео на странице результатов поиска.",
        "Theme": "Тема",
        "Change the look of Startpage": "Изменить внешний вид Startpage",
        "System default": "Системная по умолчанию",
        "Promotional messaging": "Промо-сообщения",
        "Turns on Startpage promotional content on homepage and search results page": "Включает промо-контент Startpage на главной странице и странице результатов поиска",
        "Results per page": "Результатов на странице",
        "Select between 10 or 20 results per page": "Выберите между 10 или 20 результатами на странице",
        "Unit of temperature": "Единица температуры",
        "Show temperature in Celsius or Fahrenheit": "Показывать температуру в Цельсиях или Фаренгейтах",
        "Date and time format": "Формат даты и времени",
        "Choose preference": "Выберите предпочтение",

        "General": "Общие",
        "Startpage language": "Язык Startpage",
        "Preferred language for headlines, buttons, and other text from Startpage": "Предпочитаемый язык для заголовков, кнопок и другого текста из Startpage",
        "Search language": "Язык поиска",
        "Preferred language for your search results": "Предпочитаемый язык для результатов поиска",
        "Preferred location for your search results. Startpage does not store or share your data.": "Предпочитаемое местоположение для результатов поиска. Startpage не хранит и не передаёт ваши данные.",
        "Region filter": "Фильтр региона",
        "Filter web results from this region": "Фильтровать веб-результаты из этого региона",
        "Open search in a new tab": "Открывать поиск в новой вкладке",
        "Clicking on a search result will open that page in a new tab": "При нажатии на результат поиска страница откроется в новой вкладке",
        "Search suggestions": "Предложения поиска",
        "Show search suggestions as you type. Suggestions are completely private.": "Показывать предложения поиска по мере ввода. Предложения полностью приватны.",

        "Feedback": "Обратная связь",
        "All": "Все",
        "Images": "Изображения",
        "Videos": "Видео",
        "News": "Новости",
        "Maps": "Карты",
        "Private Search": "Приватный поиск",
        "Next": "Далее",
        "Results provided by YouTube": "Результаты предоставлены YouTube",
        "Viewing video results is privacy protected by Startpage. When you visit YouTube video links, you are subject to the": "Просмотр результатов поиска по видео защищен политикой конфиденциальности Startpage. При переходе по ссылкам на видео YouTube вы подчиняетесь",


        "CAPTCHA Verification": "Проверка CAPTCHA",
        "To continue using Startpage, please enter in the characters you see below.": "Чтобы продолжить использовать Startpage, введите символы, которые вы видите ниже.",
        "Why am I seeing CAPTCHA?": "Почему я вижу CAPTCHA?",
        "Enter image characters": "Введите символы с картинки",
        "The characters in the image are case-sensitive.": "Символы на изображении чувствительны к регистру.",
        "Have trouble reading the CAPTCHA?": "Не можете прочитать CAPTCHA?",
        "Get new image": "Получить новое изображение",
        "Need additional help?": "Нужна дополнительная помощь?",
        "Contact us": "Связаться с нами",
        "Submit": "Отправить",

        "Privacy starts here.": "Приватность начинается здесь.",
        "Privacy starts here": "Приватность начинается здесь",
        "Desktop extension": "Настольное расширение",
        "Mobile browser app": "Мобильный браузер",
        "Vanish: Private AI": "Vanish: Приватный ИИ",

        "Search": "Поиск",
        "Search privately": "Поиск приватно",
        "The world’s most private search engine.": "Самая приватная поисковая система в мире.",
        "Search the web without giving up your privacy. Startpage delivers quality search results without tracking, profiling, or storing your personal information.": "Ищите в интернете, не жертвуя приватностью. Startpage выдаёт качественные результаты поиска без отслеживания, профилирования и хранения вашей личной информации.",
        "Startpage gives you the answers you need, without collecting your data.": "Startpage даёт вам нужные ответы, не собирая ваши данные.",
        "Browse privately with our mobile app.": "Просматривайте интернет приватно с нашим мобильным приложением.",
        "Make Startpage your default search engine.": "Сделайте Startpage поисковой системой по умолчанию.",
        "Learn more": "Узнать больше",

        "Our Products": "Наши продукты",
        "Get the Extension": "Установить расширение",
        "Get the extension": "Установить расширение",
        "Get the app": "Скачать приложение",
        "Try Vanish": "Попробовать Vanish",

        "Search privately without tracking": "Приватный поиск без отслеживания",
        "No saving, sharing, or selling your data": "Не сохраняем, не передаём и не продаём ваши данные",
        "No third-party trackers or cookies dropped": "Без сторонних трекеров и куки",
        "The easiest way to use Startpage on mobile": "Самый простой способ использовать Startpage на телефоне",
        "Search and browse in total privacy": "Поиск и просмотр в полной приватности",
        "Unbiased, unprofiled results": "Непредвзятые результаты без профилирования",
        "Private access to models like GPT-5, Claude & Sonar": "Приватный доступ к моделям вроде GPT-5, Claude и Sonar",
        "Conversations are never tracked": "Разговоры никогда не отслеживаются",
        "Chats stay on your device, not in the cloud": "Чаты остаются на вашем устройстве, а не в облаке",

        "Features": "Возможности",
        "Search, without surveillance.": "Поиск без слежки.",
        "No search history": "Без истории поиска",
        "AI is optional": "ИИ — по желанию",
        "Unbiased results": "Непредвзятые результаты",
        "GDPR privacy": "Защита по GDPR",
        "Private local results": "Приватные локальные результаты",
        "Tracker protection": "Защита от трекеров",

        "Searches aren't stored or tied to you.": "Поиски не сохраняются и не привязываются к вам.",
        "All AI features are a choice.": "Все функции ИИ — по выбору.",
        "Results based on your query, not your search history.": "Результаты основаны на вашем запросе, а не на истории поиска.",
        "Protected by strict European privacy laws.": "Защищено строгими европейскими законами о приватности.",
        "Local results without precise location sharing.": "Локальные результаты без точной передачи местоположения.",
        "We won't let ads follow you around the internet.": "Мы не дадим рекламе преследовать вас по интернету.",

        "The Startpage difference": "Отличие Startpage",
        "Blocks price trackers": "Блокирует трекеры цен",
        "Never records personal data or search history": "Никогда не записывает личные данные и историю поиска",
        "Protects your IP address": "Защищает ваш IP-адрес",
        "Prevents third-party retargeting ads": "Блокирует ретаргетинговую рекламу",
        "Delivers unprofiled search results": "Выдаёт результаты без профилирования",
        "Lets you visit websites anonymously": "Позволяет посещать сайты анонимно",

        "Private searches": "Приватных поисков",
        "Star rating": "Рейтинг",
        "Privacy protection": "Защита приватности",

        "Settings": "Настройки",
        "Products": "Продукты",
        "Resources": "Ресурсы",
        "About Us": "О нас",
        "Privacy Policy": "Политика конфиденциальности",
        "Press": "Пресса",
        "Support": "Поддержка",
        "Blog": "Блог",
        "Featured in": "Упоминается в",
        "Install on Firefox": "Установить на Firefox",
        "Install on Chrome": "Установить на Chrome",
        "Browser Extension": "Расширение для браузера",
        "Mobile Browser": "Мобильный браузер",
        "Hide promotional messaging": "Скрыть промо-сообщения",
        "Got it!": "Понятно!",
        "Add to Home Screen": "Добавить на главный экран",
        "© 2026 Startpage. All rights reserved.": "© 2026 Startpage. Все права защищены.",
        "All rights reserved.": "Все права защищены.",
    };

    function normalize(text) {
        return text.replace(/\s+/g, ' ').trim();
    }

    function isSafeToTranslate(text) {
        if (text.length > 15 && /[{}[\]"':]/.test(text)) return false;
        if (text.includes('function') || text.includes('return')) return false;
        return true;
    }

    function translate() {
        const walker = document.createTreeWalker(
            document.body,
            NodeFilter.SHOW_TEXT,
            {
                acceptNode: function(node) {
                    const parent = node.parentNode;
                    if (!parent) return NodeFilter.FILTER_REJECT;
                    const tag = parent.tagName;
                    if (tag === 'SCRIPT' || tag === 'STYLE' || tag === 'NOSCRIPT') {
                        return NodeFilter.FILTER_REJECT;
                    }
                    return NodeFilter.FILTER_ACCEPT;
                }
            },
            false
        );

        let node;
        while (node = walker.nextNode()) {
            const original = node.textContent;
            const norm = normalize(original);

            if (!isSafeToTranslate(norm)) continue;

            if (dict[norm]) {
                node.textContent = dict[norm];
                continue;
            }

            for (const [en, ru] of Object.entries(dict)) {
                if (en.length >= 8 && norm.includes(en) && isSafeToTranslate(norm)) {
                    node.textContent = original.replace(en, ru);
                    break;
                }
            }
        }

        document.querySelectorAll('h1, h2, h3, h4, [class*="title"], [class*="heading"], [class*="card"], [class*="tab"]:not(script):not(style)').forEach(el => {
            const text = normalize(el.textContent);
            if (dict[text] && isSafeToTranslate(text)) {
                el.textContent = dict[text];
            }
        });

        document.querySelectorAll('button, a, [role="button"], [class*="button"]:not(script):not(style)').forEach(el => {
            const text = normalize(el.textContent);
            if (dict[text] && isSafeToTranslate(text)) {
                el.textContent = dict[text];
            }
        });

        document.querySelectorAll('input[placeholder]:not(script):not(style)').forEach(el => {
            const ph = normalize(el.placeholder);
            if (dict[ph]) {
                el.placeholder = dict[ph];
            }
        });
    }

    setTimeout(translate, 600);
    setTimeout(translate, 1500);
    setTimeout(translate, 3000);

    const observer = new MutationObserver(() => {
        setTimeout(translate, 400);
    });
    observer.observe(document.body, { childList: true, subtree: true });

    console.log('%cStartpage Russian Localization v3.3 загружен (все фильтры переведены)', 'color:#00ff88; font-weight:bold');
})();
// ==UserScript==
// @name         Ecosia — русский интерфейс
// @namespace    nikitamce.ecosia-ru
// @version      2.4.3
// @description  Русификация интерфейса Ecosia: фильтры, меню, настройки, профиль, коллекция, AI, cookie
// @match        https://www.ecosia.org/*
// @match        https://ecosia.org/*
// @grant        none
// @run-at       document-idle
// @license MIT

// ==/UserScript==

(() => {
  'use strict';

  const translations = new Map([
    // Основной интерфейс
    ['Search', 'Поиск'],
    ['Search the web', 'Поиск в интернете'],
    ['Search the web and plant trees', 'Ищите в интернете и сажайте деревья'],
    ['Images', 'Картинки'],
    ['Videos', 'Видео'],
    ['News', 'Новости'],
    ['Maps', 'Карты'],
    ['Shopping', 'Покупки'],
    ['More', 'Ещё'],
    ['Settings', 'Настройки'],
    ['Search settings', 'Настройки поиска'],
    ['General settings', 'Основные настройки'],
    ['Privacy settings', 'Настройки приватности'],
    ['Appearance', 'Внешний вид'],
    ['Privacy', 'Конфиденциальность'],
    ['About', 'О сервисе'],
    ['Feedback', 'Обратная связь'],
    ['Sign in', 'Войти'],
    ['Log in', 'Войти'],
    ['Log out', 'Выйти'],
    ['Account', 'Аккаунт'],
    ['Menu', 'Меню'],
    ['Open menu', 'Открыть меню'],
    ['Close', 'Закрыть'],
    ['Clear', 'Очистить'],
    ['Clear search', 'Очистить запрос'],
    ['Save', 'Сохранить'],
    ['Save settings', 'Сохранить настройки'],
    ['Cancel', 'Отмена'],
    ['Back', 'Назад'],
    ['Next', 'Далее'],
    ['Previous', 'Назад'],
    ['All', 'Все'],
    ['Web', 'Веб'],
    ['Web results', 'Результаты из интернета'],
    ['Search results', 'Результаты поиска'],
    ['Related searches', 'Похожие запросы'],
    ['Related search', 'Похожие запросы'],
    ['No results found', 'Ничего не найдено'],
    ['No results', 'Нет результатов'],
    ['Did you mean', 'Возможно, вы имели в виду'],
    ['Safe Search', 'Безопасный поиск'],
    ['SafeSearch', 'Безопасный поиск'],
    ['Region', 'Регион'],
    ['Search language', 'Язык поиска'],
    ['Language', 'Язык'],
    ['Website language', 'Язык сайта'],
    ['Theme', 'Тема'],
    ['Dark', 'Тёмная'],
    ['Light', 'Светлая'],
    ['System default', 'Как в системе'],
    ['On', 'Вкл.'],
    ['Off', 'Выкл.'],
    ['Enabled', 'Включено'],
    ['Disabled', 'Выключено'],
    ['Open in new tab', 'Открывать в новой вкладке'],
    ['Open links in a new tab', 'Открывать ссылки в новой вкладке'],
    ['Anonymous View', 'Анонимный просмотр'],
    ['Anonymous view', 'Анонимный просмотр'],
    ['Proxy', 'Прокси'],
    ['Family filter', 'Семейный фильтр'],
    ['Strict', 'Строгий'],
    ['Moderate', 'Умеренный'],
    ['Time', 'Время'],
    ['Any time', 'За всё время'],
    ['Past hour', 'За последний час'],
    ['Past 24 hours', 'За последние 24 часа'],
    ['Past week', 'За последнюю неделю'],
    ['Past month', 'За последний месяц'],
    ['Past year', 'За последний год'],
    ['Reset', 'Сбросить'],
    ['Reset settings', 'Сбросить настройки'],
    ['Learn more', 'Подробнее'],
    ['Learn More', 'Подробнее'],
    ['Terms', 'Условия'],
    ['Privacy Policy', 'Политика конфиденциальности'],
    ['Cookies', 'Файлы cookie'],

    // Фильтры
    ['Search region: Russia', 'Регион поиска: Россия'],
    ['Search region: All Regions', 'Все регионы'],
    ['All regions', 'Все регионы'],
    ['Filter by time', 'Фильтр по времени'],
    ['Filter by type', 'Фильтр по типу'],
    ['Filter by color', 'Фильтр по цвету'],
    ['Filter by size', 'Фильтр по размеру'],
    ['Filter by layout', 'Фильтр по формату'],
    ['Filter by usage rights', 'Фильтр по правам использования'],
    ['Filter by duration', 'Фильтр по длительности'],
    ['Filter by resolution', 'Фильтр по разрешению'],
    ['All colors', 'Все цвета'],
    ['Color only', 'Только цветные'],
    ['Black and white', 'Чёрно-белые'],
    ['All types', 'Все типы'],
    ['Photo', 'Фото'],
    ['Clip art', 'Клипарт'],
    ['Line drawing', 'Линейный рисунок'],
    ['GIF', 'GIF'],
    ['Transparent', 'Прозрачные'],
    ['All sizes', 'Все размеры'],
    ['Small', 'Маленький'],
    ['Medium', 'Средний'],
    ['Large', 'Большой'],
    ['Extra large', 'Очень большой'],
    ['All layouts', 'Все форматы'],
    ['Square', 'Квадратный'],
    ['Wide', 'Широкий'],
    ['Tall', 'Высокий'],
    ['All usage rights', 'Все права использования'],
    ['Free to share and use', 'Можно свободно использовать и распространять'],
    ['Free to share and use commercially', 'Можно свободно использовать и распространять в коммерческих целях'],
    ['Free to modify, share and use', 'Можно свободно изменять, использовать и распространять'],
    ['Free to modify, share and use commercially', 'Можно свободно изменять, использовать и распространять в коммерческих целях'],
    ['Public domain', 'Общественное достояние'],
    ['All resolutions', 'Все разрешения'],
    ['All durations', 'Любая длительность'],
    ['All durations', 'Любая длительность'],
    ['Any duration', 'Любая длительность'],
    ['Short', 'Короткие'],
    ['Long', 'Длинные'],

    // Аккаунт
    ['Guest user', 'Гостевой пользователь'],
    ['Level 1 · Ecocurious', 'Уровень 1 · Эколюбопытный'],
    ['See your impact grow', 'Следите за своим вкладом'],
    ["Reach new levels and see the climate impact you're having through your own Ecosia account.", 'Повышайте уровень и следите за своим вкладом в защиту климата через аккаунт Ecosia.'],
    ['Create an account', 'Создать аккаунт'],
    ['Use Ecosia', 'Используйте Ecosia'],
    ['Desktop Browser', 'Браузер для компьютера'],
    ['Android App', 'Приложение для Android'],
    ['iOS App', 'Приложение для iOS'],
    ['Ecosia for Companies', 'Ecosia для компаний'],

    // Стартовая страница
    ['We make our financial reports public', 'Мы публикуем наши финансовые отчёты'],
    ['Ask, search, browse...', 'Спросите, ищите, просматривайте…'],
    ['Add to Android — it’s free', 'Установить на Android — бесплатно'],
    ['Add to Android - it’s free', 'Установить на Android — бесплатно'],
    ['trees planted by Ecosia', 'деревьев посажено Ecosia'],
    ['dedicated to climate action', 'направлено на защиту климата'],

    // Cookie-диалог
    ['Cookies help us improve your search experience', 'Cookie помогают улучшать поиск'],
    ['Essential cookies help make sure Ecosia works and is easy to use.', 'Обязательные cookie обеспечивают работу Ecosia и удобство использования.'],
    ['Non-essential cookies, including third-party cookies from our search', 'Необязательные cookie, включая сторонние cookie от нашего поиска'],
    ['Manage cookies', 'Управлять cookie'],
    ['Reject non-essential', 'Отклонить необязательные'],
    ['Accept all', 'Принять все'],

    // Карточки и сведения о результатах
    ['Source:', 'Источник:'],
    ['This search result is provided by Google', 'Этот результат поиска предоставлен Google'],
    ['Report result', 'Сообщить о результате'],
    ['Powered by', 'Работает на базе'],
    ['About Ecosia', 'Об Ecosia'],
    ['Plant trees while searching the web', 'Сажайте деревья, выполняя поиск в интернете'],

    // Cookie-диалог и согласие
    ['With your agreement, we and our 5 partners use cookies or similar technologies to store, access, and process personal data like your visit on this website, IP addresses and cookie identifiers.', 'С вашего согласия мы и наши 5 партнёров используем cookie и аналогичные технологии для хранения, доступа и обработки персональных данных: посещений сайта, IP-адресов и идентификаторов cookie.'],
    ['Some partners do not ask for your consent to process your data and rely on their legitimate business interest.', 'Некоторые партнёры обрабатывают данные без отдельного согласия, опираясь на законный деловой интерес.'],
    ['You can withdraw your consent or object to data processing based on legitimate interest at any time by clicking on', 'Вы можете в любое время отозвать согласие или возразить против обработки данных через'],
    ['We and our partners process data for the following purposes:', 'Мы и наши партнёры обрабатываем данные в следующих целях:'],
    ['Analytics', 'Аналитика'],
    ['Marketing', 'Маркетинг'],
    ['Personalised advertising and content', 'Персонализированная реклама и контент'],
    ['advertising and content measurement', 'Измерение эффективности рекламы и контента'],
    ['audience research and services development', 'Исследование аудитории и развитие сервисов'],
    ['Personalization', 'Персонализация'],

    // Выбор региона
    ['Search region', 'Регион поиска'],
    ['All Regions', 'Все регионы'],
    ['Russia', 'Россия'],
    ['Argentina', 'Аргентина'],
    ['Australia', 'Австралия'],
    ['Austria', 'Австрия'],
    ['Belgium (Dutch)', 'Бельгия (нидерландский)'],
    ['Belgium (French)', 'Бельгия (французский)'],
    ['Brazil', 'Бразилия'],
    ['Bulgaria', 'Болгария'],

    // Учётная запись, Chat и нижнее меню
    ['Transparency information (MStV)', 'Информация о прозрачности (MStV)'],
    ['Help and Legal', 'Помощь и правовая информация'],
    ['New Chat', 'Новый чат'],
    ['Chat history', 'История чатов'],
    ['Something went wrong while contacting the server.', 'Произошла ошибка при обращении к серверу.'],
    ['Try again', 'Попробовать снова'],
    ['Ask anything...', 'Спросите что угодно…'],
    ['Powered by renewable energy. Check important answers.', 'Работает на возобновляемой энергии. Проверяйте важные ответы.'],

    // Настройки
    ['Your search results come from this region', 'Результаты поиска будут поступать из этого региона'],
    ['Changes the language across the Ecosia website', 'Изменяет язык интерфейса сайта Ecosia'],
    ['Preferred search provider', 'Предпочитаемый поставщик поиска'],
    ['You can learn more about our search providers', 'Подробнее о поставщиках поиска'],
    ['No preference', 'Без предпочтений'],
    ['Sensitive content', 'Чувствительный контент'],
    ['Blocks objectionable (mostly adult) material from your results. Toggle between SafeSearch modes', 'Скрывает нежелательный, преимущественно взрослый, контент в результатах поиска. Переключайте режимы SafeSearch'],
    ['Themes', 'Темы'],
    ['Changes overall look and feel', 'Изменяет общий вид интерфейса'],
    ['System', 'Системная'],
    ['Auto-save search', 'Автосохранение поиска'],
    ['Auto-redirection', 'Автоперенаправление'],
    ['Automatically open best results of AI Chat based on your query', 'Автоматически открывает лучший результат AI Chat по вашему запросу'],
    ['New tab', 'Новая вкладка'],
    ['Open search results in a new tab', 'Открывать результаты поиска в новой вкладке'],
    ['Auto suggestions', 'Автоподсказки'],
    ['Show suggestions under the search box as you type', 'Показывать подсказки под строкой поиска во время ввода'],
    ['Notifications', 'Уведомления'],
    ['Get notified when there is news from the tree planting projects', 'Получать уведомления о новостях проектов по посадке деревьев'],
    ['What to know how we spend our profits?', 'Хотите узнать, как мы тратим прибыль?'],
    ['Want to know how we spend our profits?', 'Хотите узнать, как мы тратим прибыль?'],
    ['See financial reports', 'Посмотреть финансовые отчёты'],
    ['Check out the latest updates about our planting projects', 'Посмотрите последние обновления о наших проектах по посадке деревьев'],
    ['See tree updates', 'Новости о деревьях'],
    ['Imprint', 'Правовая информация'],
    ['Cookie preferences', 'Настройки cookie'],
    ['Send feedback', 'Отправить отзыв'],

    // Главная страница и промо-блоки
    ['Join 20 million people making a difference every day.', 'Присоединяйтесь к 20 миллионам людей, которые каждый день меняют мир к лучшему.'],
    ['Download Ecosia Browser', 'Скачать браузер Ecosia'],
    ['How Ecosia works', 'Как работает Ecosia'],
    ['Like other search engines, we make money from ads.', 'Как и другие поисковые системы, мы зарабатываем на рекламе.'],
    ['We then use 100% of our profits for the planet.', 'Затем мы направляем 100% прибыли на помощь планете.'],
    ['The result: 200M trees and counting!', 'Результат: более 200 миллионов деревьев — и счёт продолжается!'],
    ['See our financial reports', 'Посмотреть наши финансовые отчёты'],

    // Страница коллекции и профиль
    ['Your Ecosia', 'Ваш Ecosia'],
    ['Your Profile', 'Ваш профиль'],
    ['Your profile', 'Ваш профиль'],
    ['Collectibles', 'Коллекция'],
    ['Sign out', 'Выйти из аккаунта'],
    ['Account settings', 'Настройки аккаунта'],
    ['Personal info', 'Личные данные'],
    ['Name', 'Имя'],
    ['Email', 'Электронная почта'],
    ['Data and privacy', 'Данные и конфиденциальность'],
    ['Delete account', 'Удалить аккаунт'],
    ['Collect different tree species, animals and tools to discover more about them and Ecosia’s projects.', 'Собирайте разные виды деревьев, животных и инструменты, чтобы больше узнать о них и проектах Ecosia.'],
    ['Seeds available to spend:', 'Семян доступно для использования:'],
    ['The Originals', 'Основная коллекция'],
    ['Around the World', 'Вокруг света'],
    ['items collected', 'предметов собрано'],

    // Уровень и климатический вклад
    ['Level', 'Уровень'],
    ['Progress', 'Прогресс'],
    ['You gain growth points every day you use Ecosia.', 'Вы получаете очки роста каждый день, когда пользуетесь Ecosia.'],
    ['Level up to get 25 extra seeds.', 'Повышайте уровень, чтобы получить 25 дополнительных семян.'],
    ['Your climate impact', 'Ваш вклад в климат'],
    ['These counters show the estimated average impact anyone at this level has.', 'Эти показатели отражают приблизительный средний вклад пользователей этого уровня.'],
    ['How does it work?', 'Как это работает?'],
    ['2 trees planted', 'Посажено 2 дерева'],
    ['Your trees help attract pollinators like bees and butterflies.', 'Ваши деревья помогают привлекать опылителей: пчёл и бабочек.'],
    ['760 Wh energy generated', 'Выработано 760 Вт·ч энергии'],
    ['1 hour of tree care', '1 час ухода за деревьями'],
    ['New', 'Новое'],
    ['Area restored', 'Восстановленная территория'],
    ['Unlocked at level 10', 'Откроется на 10 уровне'],
    ['Your collection', 'Ваша коллекция'],
    ['Collective impact', 'Общий вклад'],
    ['Want to know the details?', 'Хотите узнать подробности?'],
    ['Read our financial report', 'Читать финансовый отчёт'],

    // Окна коллекции и повышения уровня
    ['Start your collection', 'Начните коллекцию'],
    ['You can now use your seeds to unlock collectibles. Collect animals, trees and tools to find out more about them and your impact!', 'Теперь вы можете использовать семена, чтобы открывать предметы коллекции. Собирайте животных, деревья и инструменты, чтобы узнать больше о них и своём вкладе!'],
    ['Check it out', 'Посмотреть'],
    ['+18 seeds', '+18 семян'],
    ["You've reached level 5!", 'Вы достигли 5 уровня!'],
    ['You’ve gained 18 extra seeds!', 'Вы получили 18 дополнительных семян!'],
    ['Learn more about your growing impact in your profile.', 'Подробнее о растущем вкладе — в вашем профиле.'],
    ['Take me there', 'Перейти'],

    // Дополнения: главная страница, настройки и профиль
    ['Search the web...', 'Поиск в интернете…'],
    ['AI-free searching', 'Поиск без ИИ'],
    ['Turns off all AI related features for a more traditional search experience', 'Отключает все функции, связанные с ИИ, для более традиционного поиска.'],
    ['Automatically open Web results or AI Chat based on your query', 'Автоматически открывать результаты поиска или AI Chat в зависимости от запроса.'],
    ['Get notified when there is news from the trees', 'Получать уведомления, когда появляются новости о деревьях.'],
    ['You can learn more about our search providers', 'Подробнее о наших поставщиках поиска'],
    ['search providers', 'поставщиках поиска'],
    ['Blocks objectionable (mostly adult) material from your results.', 'Скрывает нежелательный, преимущественно взрослый, контент из результатов поиска.'],
    ['Learn more about SafeSearch', 'Подробнее о безопасном поиске'],
    ['Level 5 - Biodiversity bestie', 'Уровень 5 — Друг биоразнообразия'],
    ['Level 5 · Biodiversity bestie', 'Уровень 5 — Друг биоразнообразия'],
    ['Biodiversity bestie', 'Друг биоразнообразия'],
    ['WHY CHOOSE ECOSIA?', 'ПОЧЕМУ ECOSIA?'],
    ['What we stand for', 'Наши принципы'],
    ['100% of profits for the planet', '100% прибыли — для планеты'],
    ['We use all our profits for climate action, with the majority going into tree-planting projects around the world.', 'Всю прибыль мы направляем на защиту климата, в основном — на проекты по посадке деревьев по всему миру.'],
    ['Secure and private', 'Безопасно и конфиденциально'],
    ['We respect your privacy like we respect the planet. We only collect data necessary to deliver a great product, and not a byte more.', 'Мы так же уважаем вашу конфиденциальность, как и планету. Мы собираем только данные, необходимые для качественной работы сервиса.'],
    ['AI that answers to the planet', 'ИИ, полезный для планеты'],
    ['We use smaller, faster AI models that deliver accurate answers from reliable sources, while using less energy.', 'Мы используем более компактные и быстрые модели ИИ, которые дают точные ответы из надёжных источников и потребляют меньше энергии.'],
    ['Beyond neutral', 'Больше, чем углеродная нейтральность'],
    ['We produce more clean energy than it takes to power all searches and AI queries on Ecosia, leading to more renewable energy in the grid.', 'Мы производим больше чистой энергии, чем требуется для всех поисковых запросов и обращений к ИИ в Ecosia, увеличивая долю возобновляемой энергии в сети.'],
    ['MONTHLY FINANCIAL REPORT', 'ЕЖЕМЕСЯЧНЫЙ ФИНАНСОВЫЙ ОТЧЁТ'],
    ["We're fully transparent", 'Мы полностью прозрачны'],
    ['We publish financial reports every month so you can see exactly how much we made and how we spent it. Total transparency, every month.', 'Мы публикуем финансовые отчёты каждый месяц, чтобы вы видели, сколько мы заработали и как потратили средства. Полная прозрачность — каждый месяц.'],
    ['total income this month', 'общий доход за этот месяц'],
    ['See our reports', 'Посмотреть отчёты'],
    ['OUR TREE PLANTING APPROACH', 'НАШ ПОДХОД К ПОСАДКЕ ДЕРЕВЬЕВ'],
    ['We restore and protect biodiversity hotspots', 'Мы восстанавливаем и защищаем ключевые территории биоразнообразия'],
    ['Instead of monocultures, we grow over 900 different native species where they are needed most. Always shoulder-to-shoulder with local communities.', 'Вместо монокультур мы выращиваем более 900 местных видов там, где они нужнее всего, работая вместе с местными сообществами.'],
    ['Trees planted by the Ecosia community', 'Деревьев посажено сообществом Ecosia'],
    ['people using Ecosia', 'пользуются Ecosia'],
    ['native species', 'местных видов'],
    ['countries involved', 'стран участвует'],
    ['active projects', 'активных проектов'],
    ['Discover all projects', 'Посмотреть все проекты'],

    // Исправления для профиля, климатического вклада и общего вклада
    ['Your trees help attract pollinators like bees and butterflies. Small shrubs may begin growing in the shaded, enriched soil.', 'Ваши деревья помогают привлекать опылителей — пчёл и бабочек. Небольшие кустарники могут начать расти в затенённой и обогащённой почве.'],
    ['With Ecosia’s solar panels, you’ve helped us produce enough to power an energy-efficient washing machine for 1 full load.', 'Солнечные панели Ecosia помогли выработать достаточно энергии для одного полного цикла работы энергоэффективной стиральной машины.'],
    ["With Ecosia's solar panels, you've helped us produce enough to power an energy-efficient washing machine for 1 full load.", 'Солнечные панели Ecosia помогли выработать достаточно энергии для одного полного цикла работы энергоэффективной стиральной машины.'],
    ['You’ve empowered 1 hour of tree planting efforts. This includes for example planting, weeding, watering and protecting trees in our projects.', 'Вы обеспечили один час работ по посадке деревьев: посадку, прополку, полив и защиту деревьев в наших проектах.'],
    ["You've empowered 1 hour of tree planting efforts. This includes for example planting, weeding, watering and protecting trees in our projects.", 'Вы обеспечили один час работ по посадке деревьев: посадку, прополку, полив и защиту деревьев в наших проектах.'],
    ['Help farmers grow food through agroforestry and support biodiversity with conservation projects.', 'Помогаем фермерам выращивать продукты с помощью агролесоводства и поддерживаем биоразнообразие природоохранными проектами.'],
    ['growth points', 'очков роста'],
    ['collected', 'собрано'],
    ['The global collective impact you are having as part of the Ecosia community.', 'Ваш общий вклад как части сообщества Ecosia.'],
    ['Tree planting & regenerative agriculture', 'Посадка деревьев и регенеративное сельское хозяйство'],
    ['We restore and protect biodiversity hotspots across the planet', 'Мы восстанавливаем и защищаем ключевые территории биоразнообразия по всей планете.'],
    ['Renewable energy', 'Возобновляемая энергия'],
    ['Our solar panels produce enough energy to power all searches on Ecosia twice over', 'Наши солнечные панели вырабатывают достаточно энергии для двойного покрытия всех поисковых запросов Ecosia.'],
    ['Green innovation', 'Зелёные инновации'],
    ['We finance companies that help the planet with innovative solutions', 'Мы финансируем компании, которые помогают планете инновационными решениями.'],
    ['Climate activism', 'Климатический активизм'],
    ['We partner with NGOs to fight for political change', 'Мы сотрудничаем с НКО для продвижения политических изменений.'],
    ['20 million people using Ecosia', '20 миллионов пользователей Ecosia'],
    ['250 million+ trees planted', 'Посажено более 250 миллионов деревьев'],
    ['20 000+ solar panels installed', 'Установлено более 20 000 солнечных панелей'],
    ['150 000 hectares restored', 'Восстановлено 150 000 гектаров'],
    ['35+ countries involved', 'Более 35 стран участвуют'],
    ['70+ active projects', 'Более 70 активных проектов'],
    ['Certified', 'Сертифицированная'],
    ['Certification Notice', 'Сведения о сертификации'],
    ['WHAT WE DO', 'ЧЕМ МЫ ЗАНИМАЕМСЯ'],
    ['Ecosia Browser', 'Браузер Ecosia'],
    ['Ecosia Search', 'Поиск Ecosia'],
    ['Financial reports', 'Финансовые отчёты'],
    ['Careers', 'Вакансии'],
    ['Press', 'Пресса'],
    ['Blog', 'Блог'],
    ['LEGAL', 'ПРАВОВАЯ ИНФОРМАЦИЯ'],
    ['Privacy policy', 'Политика конфиденциальности'],
    ['Terms of service', 'Условия использования'],
    ['APPS', 'ПРИЛОЖЕНИЯ'],
    ['Ecosia GmbH does not assume responsibility for the content of sites to which it links and the way in which search results are displayed. To learn more please read our privacy policy and our terms of service.', 'Ecosia GmbH не несёт ответственности за содержание сайтов, на которые она ссылается, и за способ отображения результатов поиска. Подробнее читайте в политике конфиденциальности и условиях использования.'],

    // v2.1: фрагменты, которые Ecosia выводит отдельными DOM-узлами
    // Level up popup
   ["You've gained 18 extra seeds! Learn more about your growing impact in your profile.", "Вы получили 18 дополнительных семян! Подробнее о растущем вкладе — в вашем профиле."],
   ["You’ve gained 18 extra seeds! Learn more about your growing impact in your profile.", "Вы получили 18 дополнительных семян! Подробнее о растущем вкладе — в вашем профиле."],

// На случай, если цифра вынесена в отдельный узел
    ["You've gained ", "Вы получили "],
    ["You’ve gained ", "Вы получили "],
    [" extra seeds! Learn more about your growing impact in your profile.", " дополнительных семян! Подробнее о растущем вкладе — в вашем профиле."],
    ['Level 5', 'Уровень 5'],
    ['275 / 500 growth points', '275 / 500 очков роста'],
    ['Level up to get', 'Повышайте уровень и получите'],
    ['25 extra seeds.', '25 дополнительных семян.'],
    ['4 / 22 items collected', 'Собрано: 4 из 22'],
    ['2 / 12 collected', 'Собрано: 2 из 12'],
    ['2 / 10 collected', 'Собрано: 2 из 10'],
    ['70% Tree planting & regenerative agriculture', '70% Посадка деревьев и регенеративное сельское хозяйство'],
    ['15% Renewable energy', '15% Возобновляемая энергия'],
    ['10% Green innovation', '10% Зелёные инновации'],
    ['5% Climate activism', '5% Климатический активизм'],
    ['250 million+ trees planted', 'Посажено более 250 миллионов деревьев'],
    ['20 000+ solar panels installed', 'Установлено более 20 000 солнечных панелей'],
    ['150 000 hectares restored', 'Восстановлено 150 000 гектаров'],
    ['You can learn more about our', 'Подробнее о наших'],
    ['Ecosia GmbH does not assume responsibility for the content of sites to which it links and the way in which search results are displayed.', 'Ecosia GmbH не несёт ответственности за содержание сайтов, на которые она ссылается, и за способ отображения результатов поиска.'],
    ['To learn more please read our', 'Подробнее читайте в нашей'],
    ['privacy policy', 'политике конфиденциальности'],
    ['and our', 'и'],
    ['terms of service.', 'условиях использования.'],
    ['terms of service', 'условиях использования'],

    // v2.2: устранение разрывов
    ['We', 'Мы'],
    ['restore and protect biodiversity hotspots', 'восстанавливаем и защищаем ключевые территории биоразнообразия'],
    ['Blocks objectionable (mostly adult) material from your results. Learn more about', 'Скрывает нежелательный, преимущественно взрослый, контент из результатов поиска. Подробнее о'],
    ['Learn more about', 'Подробнее о'],
    ['20 million', '20 миллионов'],

    // v2.3 / v2.4: фрагменты и статистика
    ['Your trees help', 'Ваши деревья помогают'],
    ['attract pollinators like bees and butterflies', 'привлекать опылителей — пчёл и бабочек'],
    ['. Small shrubs may begin growing in the shaded, enriched soil.', '. Небольшие кустарники могут начать расти в затенённой и обогащённой почве.'],
    ['With Ecosia’s solar panels, you’ve helped us produce enough to', 'Солнечные панели Ecosia помогли выработать достаточно энергии, чтобы'],
    ["With Ecosia's solar panels, you've helped us produce enough to", 'Солнечные панели Ecosia помогли выработать достаточно энергии, чтобы'],
    ['power an energy-efficient washing machine for 1 full load.', 'обеспечить один полный цикл работы энергоэффективной стиральной машины.'],
    ['You’ve empowered 1 hour of tree planting efforts. This includes for example', 'Вы обеспечили один час работ по посадке деревьев, включая, например,'],
    ["You've empowered 1 hour of tree planting efforts. This includes for example", 'Вы обеспечили один час работ по посадке деревьев, включая, например,'],
    ['planting, weeding, watering and protecting trees in our projects.', 'посадку, прополку, полив и защиту деревьев в наших проектах.'],
    ['250 million+', '250 миллионов+'],
    ['trees planted', 'деревьев посажено'],
    ['20 000+', '20 000+'],
    ['solar panels installed', 'солнечных панелей установлено'],
    ['150 000', '150 000'],
    ['hectares restored', 'гектаров восстановлено'],

    // v2.4: точное соответствие
    ['attract pollinators like bees and butterflies. Small shrubs may begin growing in the shaded, enriched soil.', 'привлекать опылителей — пчёл и бабочек. Небольшие кустарники могут начать расти в затенённой и обогащённой почве.'],
    ['power an energy-efficient washing machine for 1 full load.', 'обеспечить один полный цикл работы энергоэффективной стиральной машины.'],
    ['planting, weeding, watering and protecting trees in our projects.', 'посадку, прополку, полив и защиту деревьев в наших проектах.'],

    // Прочие
    ['Is this helpful?', 'Это было полезно?'],
    ['Search region:', 'Регион поиска:'],

    // v2.4.1–2.4.3: исправления по скриншотам
    ['Our tree planting approach', 'Наш подход к посадке деревьев'],
    ['OUR TREE PLANTING APPROACH', 'НАШ ПОДХОД К ПОСАДКЕ ДЕРЕВЬЕВ'],
    ['Monthly financial report', 'Ежемесячный финансовый отчёт'],
    ['MONTHLY FINANCIAL REPORT', 'ЕЖЕМЕСЯЧНЫЙ ФИНАНСОВЫЙ ОТЧЁТ'],
    ['Why choose Ecosia?', 'Почему Ecosia?'],
    ['WHY CHOOSE ECOSIA?', 'ПОЧЕМУ ECOSIA?'],
    // Кнопка
    ['Unlock more collectibles', 'Открыть больше предметов'],

// Описание коллекции (оба варианта апострофа)
    ["Collect different tree species, animals and tools to discover more about them and Ecosia's projects.", "Собирайте разные виды деревьев, животных и инструменты, чтобы больше узнать о них и проектах Ecosia."],
    ['Collect different tree species, animals and tools to discover more about them and Ecosia’s projects.', 'Собирайте разные виды деревьев, животных и инструменты, чтобы больше узнать о них и проектах Ecosia.'],

      // Сообщение о недостатке семян
      ['Not enough seeds', 'Недостаточно семян'],
      ["You don't have enough seeds to unlock this item. Use Ecosia every day to keep collecting more.", "У вас недостаточно семян, чтобы открыть этот предмет. Пользуйтесь Ecosia каждый день, чтобы собирать больше."],
      ["You don’t have enough seeds to unlock this item. Use Ecosia every day to keep collecting more.", "У вас недостаточно семян, чтобы открыть этот предмет. Пользуйтесь Ecosia каждый день, чтобы собирать больше."],

// Другие строки
      ['Endangered', 'Вымирающий'],
      ['Not endangered', 'Не находится под угрозой исчезновения'],
      ['Forest regeneration', 'Восстановление леса'],
      ['Mountain protection', 'Защита гор'],
      ['Desert restoration', 'Восстановление пустыни'],
      ['Vulnerable', 'Уязвимый'],

// Счётчик предметов
    ['2 / 10 items collected', 'Собрано: 2 из 10'],
    ['item collected', 'предметов собрано'],
      // Универсальные для счётчиков коллекции
    ['items collected', 'предметов собрано'],

      // Подписи на фото
   ['Before', 'До'],
   ['After', 'После'],

// Заголовок
   ['Help the planet while you browse', 'Помогайте планете, пока вы в интернете'],

// Описание
   ['Download the Ecosia browser to support our mission of reforesting the planet whenever you browse. We use 100% of our profits for climate action.', 'Скачайте браузер Ecosia, чтобы поддерживать нашу миссию по восстановлению лесов каждый раз, когда вы пользуетесь интернетом. Мы направляем 100% прибыли на защиту климата.'],

   ['Ecosia GmbH does not assume responsibility for the content of sites to which it links and the way in which search results are displayed. To learn more please read our', 'Ecosia GmbH не несёт ответственности за содержание сайтов, на которые она ссылается, и за способ отображения результатов поиска. Подробнее читайте в нашей'],

   ['Download the Ecosia browser to support our mission of reforesting the planet whenever you browse.', 'Скачайте браузер Ecosia, чтобы поддерживать нашу миссию по восстановлению лесов каждый раз, когда вы пользуетесь интернетом.'],
   ['We use 100% of our profits for climate action.', 'Мы направляем 100% прибыли на защиту климата.'],

    // "Searches related to"
    ['Searches related to', 'Похожие запросы к'],
    ['Searches related to ', 'Похожие запросы к '],
  ]);




  // ====================== РЕГУЛЯРНЫЕ ВЫРАЖЕНИЯ ======================
  const regexTranslations = [
    {
      pattern: /(\d+)\s*\/\s*(\d+)\s*items collected/i,
      replace: 'Собрано: $1 из $2'
    },
    {
      pattern: /(\d+)\s*\/\s*(\d+)\s*collected/i,
      replace: 'Собрано: $1 из $2'
    },
    {
      pattern: /(\d+)\s*\/\s*(\d+)\s*growth points/i,
      replace: '$1 / $2 очков роста'
    },
      {
  pattern: /^(January|February|March|April|May|June|July|August|September|October|November|December)\s+(\d{1,2}),\s+(\d{4})$/i,
  replace: (match, month, day, year) => {
    const months = {
      january: 'января',
      february: 'февраля',
      march: 'марта',
      april: 'апреля',
      may: 'мая',
      june: 'июня',
      july: 'июля',
      august: 'августа',
      september: 'сентября',
      october: 'октября',
      november: 'ноября',
      december: 'декабря'
    };
    return `${day} ${months[month.toLowerCase()]} ${year}`;
  }
}
  ];


  const skipTags = new Set(['SCRIPT', 'STYLE', 'NOSCRIPT', 'TEXTAREA', 'CODE', 'PRE', 'OPTION']);

  function normalize(text) {
    return text.replace(/\s+/g, ' ').trim();
  }

  function translateTextNode(node) {
    const original = node.nodeValue;
    if (!original) return;

    const normalized = normalize(original);
    if (!normalized) return;

    // Контекст для Medium
    if (normalized === 'Medium') {
      const context = node.parentElement?.closest('[role="dialog"], [class], section, div')?.textContent || '';
      const translated = /duration|длительност/i.test(context) ? 'Средние' : 'Средний';
      const leading = original.match(/^\s*/)?.[0] ?? '';
      const trailing = original.match(/\s*$/)?.[0] ?? '';
      node.nodeValue = `${leading}${translated}${trailing}`;
      return;
    }

    // 1. Точное совпадение
    if (translations.has(normalized)) {
      const translated = translations.get(normalized);
      const leading = original.match(/^\s*/)?.[0] ?? '';
      const trailing = original.match(/\s*$/)?.[0] ?? '';
      node.nodeValue = `${leading}${translated}${trailing}`;
      return;
    }

    // 2. Регулярные выражения
// 2. Регулярные выражения
for (const rule of regexTranslations) {
  if (rule.pattern.test(normalized)) {
    const translated = typeof rule.replace === 'function'
      ? normalized.replace(rule.pattern, rule.replace)
      : normalized.replace(rule.pattern, rule.replace);

    const leading = original.match(/^\s*/)?.[0] ?? '';
    const trailing = original.match(/\s*$/)?.[0] ?? '';
    node.nodeValue = `${leading}${translated}${trailing}`;
    return;
  }
}

    // Специальная замена
    if (original.includes('Searches related to')) {
      node.nodeValue = original.replace('Searches related to', 'Похожие запросы к');
    }
  }

  function translateAttributes(root) {
    const selectors = '[placeholder], [title], [aria-label], [alt], input[type="submit"], input[type="button"]';
    root.querySelectorAll?.(selectors).forEach((element) => {
      for (const attribute of ['placeholder', 'title', 'aria-label', 'alt', 'value']) {
        const value = element.getAttribute(attribute);
        if (!value) continue;
        const normalized = normalize(value);
        if (translations.has(normalized)) {
          element.setAttribute(attribute, translations.get(normalized));
        }
      }
    });
  }

  function translateTree(root = document.body) {
    if (!root) return;
    const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT, {
      acceptNode(node) {
        const parent = node.parentElement;
        if (!parent || skipTags.has(parent.tagName)) {
          return NodeFilter.FILTER_REJECT;
        }
        return NodeFilter.FILTER_ACCEPT;
      }
    });
    const nodes = [];
    while (walker.nextNode()) nodes.push(walker.currentNode);
    nodes.forEach(translateTextNode);
    translateAttributes(root);
  }

  let queued = false;
  function scheduleTranslation() {
    if (queued) return;
    queued = true;
    requestAnimationFrame(() => {
      queued = false;
      translateTree();
    });
  }

  scheduleTranslation();

  const observer = new MutationObserver(scheduleTranslation);
  observer.observe(document.documentElement, {
    childList: true,
    subtree: true,
    characterData: true
  });
})();

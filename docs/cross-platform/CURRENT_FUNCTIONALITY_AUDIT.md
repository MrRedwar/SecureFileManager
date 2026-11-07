# Этап 12. Аудит фактической Windows-версии

Дата: 07.11.2026. Снимок исходников: `9515151` (11.4). Начальное рабочее дерево чистое.
Это аудит кода и существующих доказательств, не новая сертификация работающего приложения.
Слово «реализовано» ниже означает наличие пути UI → сервис → адаптер/БД и соответствующих
проверок в исходниках; непроведённые ручные проверки отдельно отмечены.

## 1. Область и доказательства

Изучены AGENTS, TECHNICAL_SPEC, DESIGN_SPEC, TASKS, README, DECISIONS, PROGRESS,
STORAGE_ARCHITECTURE, ARCHITECTURE, TEST_REPORT, дополнительные COMPUTER_BROWSER,
EXTERNAL_EDITOR, EXTERNAL_OPERATIONS, EXTERNAL_ARCHIVES_TRASH, DB_UPDATE_1102,
STORAGE_CONFIGURATION, ASSETS; solution, четыре проекта, контракты/сервисы Core,
EF mapping/репозитории/транзакционные stores/миграции, Windows IO, Views/VM и тесты.
Открыт `design/egyptian-ui-concept.png`. Исторические отчёты не заменяют текущий код.

Read-only проверка среды: Windows 10.0.19045 x64, SDK 9.0.200, MSBuild 17.13.8,
Visual Studio installationVersion 17.13.35818.85, `MSSQL$SQLEXPRESS` Running.
Это не новая проверка SQL-подключения/версии схемы рабочей БД. Remote — GitHub
(адрес и конфигурационные секреты в отчёт не копировались).

Последняя последовательность коммитов:

| Commit | Дата | Добавление |
|---|---|---|
| `14c08b0` | 03.11 | заключительные материалы исходного MVP |
| `42e66e9` | 07.11 | 11.01, общий браузер локальных папок |
| `4ea3a94` | 07.11 | 11.02, внешний редактор и ExternalDocuments |
| `c51c09e` | 07.11 | 11.03, create/copy/move/rename |
| `9515151` | 07.11 | 11.04, корзина/restore и внешний ZIP |

Существующий `artifacts/external-final-verified-20261107/stage08.trx` прочитан:
total/executed/passed **194**, failed/notExecuted **0**. PROGRESS указывает 108 SQL
integration + 86 остальных cases, финальный прогон 07.11, 14:51:58–14:52:37 МСК.
Это сохранённый результат прошлого запуска, не запуск этапа 12 и не доказательство
ручной приёмки. Debug/Release 0 warnings/errors — также исторические результаты.

В этапе 12 не запускались build/restore/test/приложение, SQL-запросы, миграции,
Docker и операции над данными. Причина: разрешено создавать только документы;
`SqlDatabaseFixture` при включении SQL-тестов вызывает MigrateAsync и затем удаляет
собственную GUID-БД. Даже изолированные миграции сейчас запрещены поручением.

## 2. Реальное решение

| Проект | Фактическая роль |
|---|---|
| Core, net9.0 | сущности, результаты, auth/session, ownership, файловые оркестраторы, валидация, ZIP, журнал; Identity.Core 9.0.22, без WPF/EF |
| Data, net9.0 | EF SqlServer/Design 9.0.22, AppDbContext, repositories, IdentityStore, FileTransactionStore, ExternalDocumentStore, probes, три миграции |
| Wpf, net9.0-windows | XAML/MVVM, DI, конфигурация Storage, Windows handles и файловые адаптеры, системные диалоги; DI 9.0.22 |
| Tests, net9.0-windows, UseWPF | xUnit 2.9.2, runner 2.8.2, Test SDK 17.12.0; ссылка на Wpf/Data, Windows/SQL/STA и чистые проверки вместе |

`global.json` закрепляет SDK 9.0.200/latestPatch. Веб-проекта, MySQL-провайдера,
Dockerfile/Compose и CI-конвейера в проверенном дереве нет. Байты файлов не хранятся в SQL.

## 3. Два режима доступа

### Личное пространство

`FileService`, `UserStorageService`, `PersonalStorage`, `WindowsFileStorage`:
`StorageRoot/Users/<GUID>`. Actor берётся из IUserSession, дополнительно SQL-active user,
Revision, owner-scoped ID/relative path, физическая граница. Другой app-логин не имеет
прикладного доступа к этому каталогу. Это не отдельный Windows token и не обещание
защиты от прямого доступа владельца ОС/SQL-администратора.

В личном режиме доступны:

- список с Managed/Untracked/Missing/Changed/Unsafe; root/up/refresh, breadcrumb;
- создание папок и TXT/JSON/XML (начальные данные: пусто, `{}`, `<document />`);
- чтение/редактирование/валидация/сохранение/reload поддерживаемого текста;
- удаление файла или **пустой** папки с подтверждением: физическая очистка после commit,
  metadata soft-delete, история остаётся; пользовательской корзины/restore здесь нет;
- импорт **одного** явно выбранного внешнего локального файла до 100 МиБ: копия,
  исходник не меняется, не превращается в разрешённое расположение; ярлыки
  `.lnk/.url/.website`, сеть, ссылки и источники из Storage запрещены;
- явная Synchronize выбранного объекта: учесть untracked, обновить metadata,
  отсутствующий объект пометить удалённым; без физического удаления;
- Recover незавершённых личных операций;
- ZIP нескольких выбранных учтённых файлов/папок, preview, extract в новую личную
  папку, progress и cancel;
- неразрушающая диагностика своего хранилища.

**В IFileService/FilesViewModel нет copy/move/rename и команды экспорта.** Наличие этих
операций во внешнем режиме не распространяет их на «Мои файлы». Обычный бинарный файл
можно импортировать/учесть/удалить/архивировать, но открыть в редакторе нельзя:
UI показывает имя/размер/ограничение форматов. Запуска через ассоциированную ОС-программу нет.

### Общее внешнее пространство

`ComputerBrowseService`, `ExternalDocumentService`, `ExternalFileOperationService` и
Windows-адаптеры. ExternalDocuments не имеет OwnerId. Все активные app-пользователи
одного Windows-процесса получают общую видимость по **одному** Windows token.
Отдельных grants/StorageLocations/Windows impersonation в текущем коде нет.
Личный Storage закрыт для общего браузера и внешних операций.

| Вопрос | Точная граница текущей реализации |
|---|---|
| Какие диски | WindowsDriveService вызывает DriveInfo.GetDrives: имя, label, тип, ФС, ready, total/free/used/percent; недоступный носитель — явный статус |
| Отличие браузера | WindowsComputerDirectoryReader в списке корней пропускает DriveType.Network; Normalize отвергает Network/Unknown/NoRootDirectory |
| Произвольные пути | Адресная строка принимает абсолютный локальный `X:\...`; только существующий доступный путь, без private Storage, dot segments, ADS, UNC/device namespace и `.sfm-*` |
| Между дисками | Через «Этот компьютер»/карточку «Открыть диск» или новый адрес. Истории back/forward нет |
| Внешние носители | Готовый локально представленный USB/removable может перечисляться и обслуживаться теми же правилами; неготовый даёт отказ/статус. Не проверен отдельный физический USB-прогон |
| Права | Реальные open/list/write/delete handles и ошибки ОС; ACL проверяет Windows при IO. Код не редактирует ACL, не повышает права, не выдаёт app-логину другую OS identity |
| Системные каталоги | Отдельного универсального blacklist Windows/System32 нет; доступ определяют ОС, фильтры пути и безопасность объекта. Не обещан запрет записи во все системные папки, если token уже имеет права |
| Навигация | список дисков, address/Go/Enter, double click/Enter/Open папки, Up, Refresh, текущий список до 10000, предупреждение усечения |
| Создание | файл произвольного допустимого имени (неподдерживаемый формат — пустой), папка; JSON/XML получают корректный начальный текст |
| Copy | один выбранный файл или дерево с бинарными файлами/пустыми папками; между локальными томами возможно; no overwrite/merge/self-copy |
| Move/rename | один объект, только тот же том; без case-only rename, без overwrite/merge; известные metadata GUID/история сохраняются |
| Delete | rename в соседний `.sfm-trash-<GUID>`, с подтверждением; не Windows Shell Recycle Bin, не стирание, место не освобождается |
| Restore | полный исходный путь + квитанция в StorageRoot/.external-trash; свободное имя, существующий родитель, совпавшее дерево. Несколько поколений исходного пути — Conflict |
| ZIP | один выбранный файл/дерево → ZIP; отдельные preview и extract в новую папку. ZIP не открывается обычной командой текстового редактора, используются пункты меню |
| Импорт/экспорт | Отдельного external import/export нет: copy между внешними расположениями; WPF browser upload/download отсутствуют. Личный Import — отдельный сценарий выше |
| Приватные функции | ownership, Files soft-delete, Synchronize/diagnostics личного каталога, multiple selection ZIP и его cancel не являются внешними командами |

Дерево внешней операции: 1000 узлов, 100 МиБ/файл, 1 ГиБ суммарно, реальный streaming
budget; лимит относится также к snapshot для move/trash. Copy наследует ACL назначения,
не переносит исходные ACL/все атрибуты/timestamps. Внешние новые команды имеют busy
индикатор; отдельной кнопки отмены в UI нет.

Ссылки: `WindowsComputerDirectoryReader` показывает reparse-объект с примечанием,
не входит в него. `SafeNativeFile.Open` использует FILE_FLAG_OPEN_REPARSE_POINT,
проверяет attributes/type, Links==1 у файла, GetFinalPathNameByHandle, удерживает предков.
Symlink/junction/reparse и многоссылочные файлы для содержимого/мутаций запрещены.
Реальные junction/hardlink есть в тестах; отдельная физическая file symlink и все виды
reparse/ФС аппаратно не сертифицированы. Карточка диска не доказывает право чтения его файлов.

`WindowsDriveService` проверяет app-session/revision, **не** SQL-active/audit probe на каждый
запрос характеристик. Внешний directory listing, напротив, проверяет SQL-active и выдаёт
результат только после подтверждённого аудита. Не объединять эти два пути в отчёте.

## 4. Форматы — фактическая матрица

Источник: [DocumentValidation.cs](../../src/SecureFileManager.Core/Files/DocumentValidation.cs),
[FileService.cs](../../src/SecureFileManager.Core/Files/FileService.cs),
[ExternalDocumentService.cs](../../src/SecureFileManager.Core/Files/ExternalDocumentService.cs),
[ZipEngine.cs](../../src/SecureFileManager.Core/Files/ZipEngine.cs).

| Формат | Open/view/edit | Create/save | Validation и защита |
|---|---|---|---|
| TXT | общий редактор в обоих режимах, строгая UTF-8 | да, пустой TXT допустим | 10 МиБ фактических UTF-8 bytes с BOM, NUL/невалидный Unicode запрещён |
| JSON | тот же редактор; синтаксически битый можно открыть для исправления | `{}` при создании; невалидный save отклонён | System.Text.Json, depth 64, без comments/trailing commas; безопасные line/byte-position |
| XML | тот же редактор; обычный битый XML доступен для исправления, prohibited DTD/encoding отклоняются | `<document />`; только валидный save | XmlReader, DTD Prohibit, resolver null, UTF-8 declaration, depth64/200000 nodes/1024 attributes; no XSD/XInclude execution |
| YAML/YML | **нет** определения в DocumentFormat/Supports, валидатора, editor route или пакета | только внешний пустой generic-файл/копирование/импорт как байтов | YAML-редактора нет; JSON не считается YAML |
| ZIP | preview entries/declared sizes/warnings; не редактор | create Store/UTF-8, extract Store/Deflate | 250 entries, 25 МиБ/entry,100 МиБ total,100:1; envelope101 МиБ, central1 МиБ; actual bytes/CRC на extract |
| Прочие | список/metadata; бинарные copy/import/ZIP; без содержательного preview | внешний generic empty create, копирование; личный Create только TXT/JSON/XML | редакторов изображений/PDF/CSV/бинарного/7z/RAR нет |

Текст: существующий UTF-8 BOM сохраняется; новые документы без BOM, окончания строк
не нормализуются. Validate/Reload/Save повторяют серверную (сервисную) авторизацию,
физический fingerprint + SQL RowVersion; старый буфер при Conflict остаётся.
Автоформатирования/подсветки синтаксиса/автосохранения нет.

ZIP отвергает traversal, absolute/drive/UNC/ADS, backslash, reserved/.sfm names,
case-insensitive duplicates, file-parent conflicts, symlink/device/reparse attributes,
encryption, ZIP64/multi-volume/unsupported methods, несогласованные заголовки/CRC.
Preview не проверяет CRC/реальную распаковку. Staging → publish только после проверки,
no overwrite/merge. Во внешнем режиме адаптер ограничивает физический ZIP 100 МиБ для
операции (preview envelope101 МиБ); результат ровно100 МиБ данных может превысить предел
за счёт заголовков. Это разные лимиты, не обещание обработки любого ZIP до101 МиБ.

## 5. Остальные возможности

| Область | Реализация и ограничения |
|---|---|
| Регистрация | login3–50, password10–128 + confirmation, unique CI username, GUID-каталог, User+audit; автоматического входа нет |
| Пароли | PasswordSecurity: IdentityV3 PasswordHasher, PBKDF2,600000 iterations по умолчанию, salt/version/rehash; dummy verify неизвестного/недоступного аккаунта |
| Вход | единая безопасная ошибка, 5 неудач в sliding15min → lock15min, SQL сериализация счётчиков; успех сбрасывает/обновляет hash при необходимости |
| Сессия | в памяти UserSession Current/Revision/Changed, singleton одного desktop-клиента; logout очищает даже при ошибке аудита; редакторы/выбор/фильтры сбрасываются, поздние результаты отбрасываются |
| Поиск | только имя в загруженной текущей папке, не рекурсивный full-text/indexer; private CurrentCultureIgnoreCase, external OrdinalIgnoreCase |
| Сортировка | имя/тип/числовой размер/дата; private direction/ComboBox и таблица, external заголовки DataGrid; журнал серверный time+ID ordering |
| Журнал | только actor текущего пользователя, type/result/date/search по personal/external snapshot,50 строк UI, контракт1–100, pages0–10000; NULL-user system rows не выдаются |
| Здоровье аудита | rollback SELECT/INSERT probe, latched failure, срок2min; UI timer15s только обновляет отображение; нет фонового SQL polling и гарантии следующего commit |
| Настройки | read-only версия/пользователь/storage/политика/лимиты, «Проверить журнал», диагностика; нет UI смены root/ACL/пароля/глобальных ролей |
| Storage config | обязательный явный root из выбранного JSON/приоритет SFM_STORAGE_ROOT; без AppData fallback; далее Users/GUID; секреты вне Git |
| SQL config | SFM_ENVIRONMENT/CONFIG_DIRECTORY/CONNECTION_STRING; SqlConnectionStringBuilder, TLS mandatory, ограниченное Development исключение доверия local Express cert; startup проверяет schema readiness, не мигрирует |
| Ошибки | FileStatus/RepositoryStatus/AuthOutcome, локализованные безопасные сообщения; audit failure → fail closed без обещания сохранённого события |
| Согласованность | private .sfm-internal, external .external-editor/.external-operations/.external-trash; durable plans/backups, correlation commit check, verified compensation/recovery; общего SQL+FS atomic commit нет |
| Диагностика | bounded10000, untracked/missing/size-mtime-kind mismatch/soft-delete leftovers/pending/unknown staging/audit mismatch; не ремонтирует и без hash baseline не замечает same-size/same-mtime content change |

Внешний save пишет in-place через write handle для сохранения ACL/identity, имеет before/
after/plan и восстановление mtime. Частичная запись после crash с неизвестными байтами
может потребовать ручного разбора. Не обещаны encryption at rest, криптографически
защищённый аудит, защита от OS/SQL admin, power-loss durability или одновременного прямого
изменения файлов субъектом с тем же OS token. Backup содержит plaintext документа.

## 6. Полный UI-каталог для переноса

Проверены все 15 XAML Views и соответствующие VM/координаторы. Общий shell:
header/logo/winged sun, left six-section navigation + Anubis/current user,
papyrus content с heading/storage/audit cards, bottom logout/ornament.

| View / окно | Поведение для воспроизведения |
|---|---|
| AuthenticationWindow + AuthenticationWindowViewModel | connection status/retry, switching Login/Register, scroll/errors |
| LoginView + LoginViewModel | login/password, submit, busy/validation, hidden password |
| RegisterView + RegisterViewModel | login/password/confirm, submit, сообщение, переход к входу |
| MainWindow + MainWindowViewModel | шесть разделов, session status, logout/dirty close coordination; native caption/minimize/maximize/resize относятся только Windows |
| DrivesView + DrivesViewModel | вкладки «Накопители»/«Файлы компьютера», drive cards + ready/free usage, refresh/open disk |
| ComputerBrowserView + ComputerBrowserViewModel | address/Go, computer/up/refresh/open, search, table Name/Type/Size/Modified/Note, recover editor;11 menu actions create file/folder/copy/move/rename/trash/restore/ZIP create/preview/extract/recover operations |
| FilesView + FilesViewModel | root/up/refresh/sync/recover, create file/folder/open/delete/import, multi-select ZIP/create/preview/extract/cancel, search/sort, state/selection counts |
| FormatsView + FormatsViewModel | повторное использование FilesView с JSON/XML+папки filter, не отдельный редактор |
| ArchivesView + ArchivesViewModel | ZIP+папки browser и вкладка источников архива; тот же FilesView |
| JournalView + JournalViewModel | date/type/result/search/direction filters, refresh/reset, next/previous, status/table, отсутствие локальной пересортировки одной страницы |
| SettingsView + SettingsViewModel | cards/version/session/policy/health probe, diagnostics issues grid/partial status |
| TextEditorWindow + TextEditorViewModel | mono text, Save/Validate/Reload, Ctrl+S, UTF-8/format/dirty/error, conflict keeps buffer, Save/Discard/Cancel on close/logout |
| NameDialog | имя/начальное значение/OK/Cancel; реальная проверка в сервисе |
| ArchiveTargetWindow | личный directory picker, root/up/refresh, новое имя результата |
| ArchivePreviewWindow | entries, declared/compressed bytes/warnings/summary, next только CanExtract; Cancel |
| WindowsFileDialogs + dialog ports | OpenFileDialog импорт, OpenFolderDialog внешнее назначение, MessageBox confirmations с No default, общий реестр editor scope+GUID, закрытие диалогов при logout |

ResourceDictionary: Colors/Brushes/Typography/Controls/DataGrid/Icons/Navigation/States/
Converters/Views. 18 встроенных PNG: eye-of-horus; header-winged-sun/footer-lotus-ibis;
anubis-sidebar; papyrus/dark-stone;12 icons (drives/files/archives/json-xml/journal/settings/
create/delete/search/security/user/database). Исходный icon-sheet и concept не являются UI.
Цвета из Colors.xaml: navy #192C36/#233944,teal #177781/#115D67,gold #BA8C45/#E4D0AD,
sand #F5F0E6,paper #FBF9F5; text #1F2933/#5D6267/#F7F1E6,border #D7CCBC;
success #287A50,warning #9A630B,danger #AA3434. Системные Segoe UI/Georgia/Consolas
не поставляются с программой. CroppedBitmap SourceRect требуется воспроизвести CSS crop,
текстуры opacity .25. Плиточного вида, drag/drop и фонового watcher нет.

## 7. Тестовое покрытие и ограничения

Каталог `tests/SecureFileManager.Tests/`:

| Файлы | Что доказывают исходные assertions |
|---|---|
| PasswordTests,SecurityIntegrationTests | KDF/rehash/validation, lockout/concurrency, registration,logout,owner/private boundaries,SQL failures |
| PersistenceTests,DatabaseConfigurationTests,StorageConfigurationTests | FK/check/unique/soft-delete/RowVersion/UTC, конфигурация/DI/root failures |
| FileServiceTests | personal CRUD/import/foreign IDs/unsafe paths/links/races/audit rollback/recovery/editor dirty |
| DocumentValidationTests,DocumentEditorIntegrationTests | TXT/JSON/XML строгие кодировки/размер/XXE/depth, invalid save/versions/BOM/session |
| ZipEngineTests,ArchiveIntegrationTests | malicious paths/duplicates/links/encryption/ZIP64/declared+actual budgets/CRC,round-trip/rollback/cancel/recovery |
| JournalTests,JournalIntegrationTests | scoped filters/pages/redaction/health expiration/diagnostics/audit/SQL injection |
| DriveTests,ComputerBrowserTests,ComputerBrowserIntegrationTests | actual DriveInfo/unready,shared listing/session/audit/private/junction,late VM response |
| ExternalDocumentIntegrationTests |9 SQL/FS случаев:shared identity,versions/writer,invalid/UTF8/BOM,audit rollback+mtime,recovery,session/private/read-only,schema readiness |
| ExternalOperationIntegrationTests |10 SQL/FS случаев:create/tree copy/move/rename/GUID,conflicts/links/private,SQL rollback,recovery,limits/concurrent/cross-volume rejection |
| ExternalArchivesTrashIntegrationTests |9 SQL/FS случаев:trash/restore IDs/history,rollback/changed data/other actor,ZIP binary/empty directories,unsafe/CRC/preview conflicts/SQL/session |
| NavigationTests,WpfShellTests | navigation, real DI/XAML/resources/STA/layout/binding checks,11 menu commands; не native keyboard/system DPI |
| SqlDatabaseFixture | GUID test database guards, opt-in SFM_RUN_SQL_TESTS, migrations only test schema, cleanup only owned database; не безопасный для текущего запрета запуск |

Файлы External*IntegrationTests являются partial `FileServiceTests`: группировка TRX
не совпадает с именами source-файлов. 194 — число cases с theory data, не число методов.
TEST_INVENTORY со160 строками относится к MVP, не полная инвентаризация HEAD11.04.
Новые native trash/ZIP, финальный DPI/keyboard/clean install, physical USB/disk full/
power loss не подтверждены. Документированное пользовательское «вроде норм» относится11.03.

## 8. Расхождения и существенные риски

| Наблюдение | Следствие/приоритет |
|---|---|
| AGENTS §5.1 и старые SEC-02/03,README capabilities/ARCHITECTURE body утверждают только private; сверху ряда документов есть разрешённые дополнения11 | Реальный код имеет shared external режим. Это формальный конфликт старого правила и расширенного объёма, не скрытый обход. Перед будущими изменениями утвердить актуальную модель; в этапе12 только фиксируется |
| README/TECHNICAL_SPEC/STORAGE_ARCHITECTURE сохраняют «11.02–11.04 будущие»,«delete/ZIP не включены» в старых абзацах | Исторические снимки. Реализованы в HEAD; не использовать эти строки как актуальный backlog |
| ARCHITECTURE/README installation body местами называют две миграции | В коде три, включая AddExternalDocuments; DB_UPDATE_1102 сообщает контролируемое применение. Живая БД здесь не перепроверялась |
| TASKS09–10/11.04 имеют открытые ручные проверки | «Разработка завершена» не означает полную приёмку; веб-план не закрывает Windows DoD |
| Прежний план B описывал Locations/grants; реализованный external режим общий без них | Нельзя утверждать наличие app-level grants в Windows. Для веба они обязательны по новому требованию |
| Core компилируется cross-platform, но содержит ExternalPathSyntax X:\, uppercase path keys, '\\' concatenation, CI comparisons | Прямая загрузка Core на Linux не обеспечивает корректность/безопасность путей |
| Data содержит SQL-specific mapping/locks/error numbers/Joined.UseSqlServer | Переключить один UseSqlServer недостаточно |
| App.CreateServices singleton UserSession/auth/VM | Прямой перенос в ASP.NET даёт риск межпользовательской утечки; до веб-работы заменить lifetime/context |
| .NET9 SDK/runtime старой установки, завершение поддержки10.11.2026 | Требуется отдельное обновление и проверенная матрица провайдеров, см.DATABASE_PROVIDER_PLAN |
| Нет системного path blacklist, encryption, общей FS/SQL атомарности; in-place save и backups | Явные эксплуатационные границы, не доказанные новые exploit-дефекты. Проверки не исправлялись |

Подтверждённого нового воспроизводимого дефекта данных в этом статическом аудите не
установлено. Это не заключение об отсутствии уязвимостей. Блокирующие перенос риски:
singleton identity, Windows-only physical guarantees, отсутствие веб-grants и SQL dialect.

## 9. Результат

Функциональный baseline для полноценной веб-версии — обе группы выше, включая внешние
деревья/trash/recovery/ZIP. Ограничение лишь Storage потеряет уже существующие функции.
Полная построчная матрица переноса: [WINDOWS_LINUX_PARITY](WINDOWS_LINUX_PARITY.md).
Проектные решения и открытые вопросы: [CROSS_PLATFORM_ARCHITECTURE](CROSS_PLATFORM_ARCHITECTURE.md),
[DEVELOPMENT_ROADMAP](DEVELOPMENT_ROADMAP.md). Код и рабочие данные не изменены.

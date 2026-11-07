# Журнал фактического прогресса

## 2026-11-07 — 11.01, общий просмотр существующих файлов компьютера

Пользователь разрешил расширение программы и общую видимость существующих внешних
файлов всем app-пользователям. Исходный Git чистый, HEAD14c08b0. Прежний запретB
изменён прямым поручением в TECHNICAL_SPEC/DESIGN_SPEC/DECISIONS/TASKS.
Внешний read-only браузер расположен в новой вкладке существующего раздела «Диски»:
открыть диск, адрес/Enter, папки/двойной щелчок, вверх, refresh, поиск и сортировка,
тип/размер/дата/примечание. Шесть разделов и ресурсы оформления сохранены.

Core: ComputerBrowseService/DTO/read-only port, active SQL user/session/revision,
обязательный аудит до выдачи списка. Wpf: WindowsComputerDirectoryReader с pinned
предками и запретом reparse/private Storage/namespace обходов, ограниченным перечислением;
ComputerBrowserView/VM со сбросом/отменой/защитой от поздних ответов. Files/ownership/SQL
schema не менялись, внешние объекты не усваиваются в personal Files. Журнал знает
фиксированный COMPUTER_DIRECTORY_LISTED, абсолютные пути в personal snapshot не пишутся.

Среда: Windows/SDK9.0.200, MSSQL$SQLEXPRESS Running. Debug и Release build в
artifacts/computer-browser — PASS, 0 warnings/errors. Первая компиляция нашла неверную
форму параметризованного CanExecute; добавлен optional predicate в прежний AsyncRelayCommand,
существующие вызовы не меняют поведение. 4 новых unit/VM и2 новых SQL/FS теста.
Первые два точечных прогона4/5 выявили высоту таблицы в1024×640; исправлены Stretch и
компактная панель, без ослабления assertion. Затем5/5. Compact render визуально открыт;
WPF fixture/bitmap150 не объявляются настоящим native DPI/UI-прогоном.

Полный scripts/Test-Stage08.ps1 -ArtifactsPath D:/SecureFileManager/artifacts/computer-browser
-ResultsDirectory D:/SecureFileManager/artifacts/computer-browser-full-20261107:
166 passed /0 failed /0 skipped, VSTest42с; 80 SQL и86 прочих cases. Новая GUID-БД fixture
и TEMP/run-16b84b841bc2427cbd7a59f8bf51024b очищены только своим cleanup. Рабочие
БД/Storage/User.Id/хеши/пользовательские файлы и чужие результаты не менялись.
Опциональный ResultsDirectory в test script позволил сохранить этот TRX отдельно,
без перезаписи прежних stage08-результатов. После полного прогона сортировка колонок
уточнена на raw bytes/UTC, затронутый WPF-тест повторён:1/1 PASS, включая numeric sort
assert, TRX artifacts/computer-browser-targeted/browser-final-wpf.trx.

Документация: COMPUTER_BROWSER с контрактами/границами/командами/короткой ручной
приёмкой; обновлены README/ARCHITECTURE/STORAGE_ARCHITECTURE/TEST_REPORT как дополнения
к историческому MVP, а не замена его доказательств. 11.01 автоматически проверен,
checkbox остаётся открыт до native-приёмки;11.02–11.04 запланированы, не реализованы.
Нет Python/масштабного рефакторинга/новых пакетов/миграций/коммитов.

## 2026-11-03 — Заключительный этап 09–10 (текущий итог)

Поручение пользователя объединяет 09 и 10. Исходное дерево чистое, HEAD 598b175,
предыдущие commits 7c10f83/2410ae0/01893f4. Сверены документы, четыре проекта,
security/auth/session/storage/CRUD/import/editor/XML/ZIP/journal/error paths,
EF mapping/две миграции, DI/VM/XAML/resources и существующая тестовая база.
README/итоговых архитектуры/отчёта не было; подготовлены сейчас. PNG-концепт открыт.
Работающие код/XAML/схема/пакеты/тесты не менялись, новые дублирующие тесты не нужны.

Фактические проверки Windows 10 x64, SDK9.0.200, VS2022 17.13.1, SQL Express Running:

- dotnet restore SecureFileManager.sln — PASS после разрешённого доступа к Windows SDK.
  В sandbox MSB4184 (нет доступа к AppData/Local/Microsoft SDKs).
- Первый Debug --no-restore с новым artifacts/final-verify дал NETSDK1004; повтор
  dotnet build SecureFileManager.sln -c Debug --artifacts-path artifacts/final-verify
  с restore — PASS, 0 warnings/errors, 15.14 с. Это ошибка команды, не кода.
- scripts/Test-Stage08.ps1 -ArtifactsPath D:/SecureFileManager/artifacts/final-verify —
  PASS 160/160, 0 failed/skipped, 35 с VSTest, 37.428 с по TRX start/finish.
  78 SQL integration, 82 прочих cases (включая WPF/Windows FS). Только новая GUID-БД
  fixture и TEMP/SecureFileManager.Stage08/run-c05278e59dca4d97b09b0d2d5cc6e5ec.
  Cleanup собственного запуска подтверждён. Рабочий Storage/БД и чужие тестовые
  каталоги/БД не удалялись. Скрипт сообщил перезапись своего фиксированного stage08.trx;
  текущий результат дополнительно сохранён artifacts/final-evidence/final-debug.trx.
  Все 160 имён/исходников/результатов сохранены в docs/TEST_INVENTORY.md.
- dotnet build SecureFileManager.sln -c Release --artifacts-path artifacts/final-verify —
  PASS, 0 warnings/errors, 7.63 с. Полный Release SQL test NOT RUN, ненужный дубль.
- dotnet tool restore — PASS, dotnet-ef9.0.22. EF has-pending-model-changes с Data startup,
  Release — PASS, model changes нет. database update над рабочей БД не выполнялся.
- dotnet publish src/SecureFileManager.Wpf/SecureFileManager.Wpf.csproj -c Release
  --artifacts-path artifacts/final-verify --self-contained false
  -o artifacts/publish/final-win --no-restore — PASS. Проверен состав: нет Development
  конфига, БД, Storage/demo; WPF XAML/PNG встроены, runtime dependencies присутствуют.
- Read-only SELECT SQL ProductVersion/Edition — 15.0.2000.5 Express64. ODBC sandbox
  не получил Windows credentials, разрешённый повтор успешен; секреты не выводились.
- Опубликованный exe запущен из TEMP с абсолютным внешним config directory,
  обнаружены процесс30348 и окно «Secure File Manager — вход». Auth не автоматизировалась.
  Live capture BLOCKED: FrameArrived timeout, повтор после reselection/activation —
  window capture timeout. Содержимое/статус подключения native окна и screenshots
  не подтверждены. Чистая установка NOT RUN, финальный native DPI/keyboard NOT RUN.

Созданы README, ARCHITECTURE (реальная схема/диаграммы/контракты/security), TEST_REPORT
(T01–T20, SEC, regression, ограничения), TEST_INVENTORY, SCREENSHOT_CHECKLIST (20 кадров),
STAGE09_10_MANUAL_TEST, DEMO_SCENARIO (12 шагов), FINAL_REPORT_DRAFT; demo содержит
только новые искусственные TXT/JSON/XML/некорректные документы/безвредный DTD/ZIP.
Demo ZIP Store проверен по двум entries/размерам; auto-seed/копирования рабочего Storage нет.
TASKS/DECISIONS обновлены фактически. История 09 сохранена; 09.01/05/06 и
10.01/02/04/05 закрыты, остальные пункты открыты до ручного подтверждения.

Новых дефектов приложения не выявлено, исправлений кода нет. Матрица покрывает
автоматические части всех20, native аспекты отдельно NOT RUN. PRE-03 и временный
net9.0 сохранены как ограничения; Windows/SQL admin/direct token access/общая SQL+NTFS
атомарность не обещаются. Полный disk-full/power-loss/physical file symlink не проводились.
Статус: **требуется ручная приёмка**, следующий шаг — 09.02–09.04/10.03/10.06 по
STAGE09_10_MANUAL_TEST и SCREENSHOT_CHECKLIST. Python/режим B/рефакторинг/коммиты не начинались.

Финальная проверка документов/demo: локальные Markdown-ссылки разрешаются, inventory
содержит160 cases/78 SQL. Release DocumentValidator подтвердил VALID у example TXT/JSON/XML,
JSON_SYNTAX/XML_SYNTAX у invalid и XML_DTD у forbidden-dtd. Это не новые xUnit cases.
demo/safe.zip содержит только first.txt/second.txt по67 байт, Store. git diff --check —
без whitespace errors (предупреждения LF→CRLF относятся к политике Git).

Проверочный publish-процесс30348 оставлен: guard обнаружил MainWindowHandle,
принудительное завершение не выполнялось. Перед заменой публикации закрыть это окно.

## 2026-11-01 — PRE, предварительная проверка этапа 01

Изучены AGENTS.md, TECHNICAL_SPEC.md, DESIGN_SPEC.md, TASKS.md;
визуально открыт `design/egyptian-ui-concept.png`. Концепт задаёт navy/песочную
палитру, бирюзовые и золотые акценты, нативную компоновку WPF; реализация UI не начата.

### Среда и команды (Windows, PowerShell)

| Проверка | Фактический результат |
|---|---|
| `dotnet --info` | Windows 10.0.19045, win-x64; SDK 9.0.200; SDK MSBuild 17.13.8 |
| `dotnet --list-sdks` | Только 9.0.200, `C:\Program Files\dotnet\sdk` |
| `vswhere -all -products '*' -format json` | VS Community 2022 17.13.1, complete/launchable, не preview |
| `vswhere -all -products '*' -requires Microsoft.VisualStudio.Workload.ManagedDesktop -property installationVersion` | 17.13.35818.85: desktop workload установлен |
| `vswhere -all -products '*' -find 'MSBuild\**\Bin\MSBuild.exe'` | Найден MSBuild Visual Studio |
| VS `MSBuild.exe -version -nologo` | 17.13.9.7704 |
| `dotnet new list wpf` | Шаблон приложения WPF и WPF-библиотек доступен |
| Проверка WindowsDesktop reference packs | 6.0.36, 8.0.13, 9.0.2; WindowsDesktop runtime 9.0.2 установлен |
| `Test-Path` для SDK WindowsDesktop `Microsoft.WinFX.targets` | True |
| `Get-Service` MSSQL/SQLBrowser/SQLWriter | `MSSQL$SQLEXPRESS` Running; SQLBrowser Stopped; SQLWriter Running |
| Реестр `Instance Names\SQL` и `MSSQL15.SQLEXPRESS\Setup` | SQLEXPRESS, Express Edition, Setup Version 15.0.2000.5 |

Отсутствие workload в `dotnet --info` не означает отсутствия WPF:
его инструменты отдельно подтверждены VS Installer, reference packs и XAML targets.
Компиляция XAML ещё не проверена, так как решение не создано.

### Проверка SQL без секретов и изменений данных

Команда с Windows Authentication, обязательным шифрованием и без обхода сертификата:

```powershell
sqlcmd -S '.\SQLEXPRESS' -E -N -l 8 -b -Q "SET NOCOUNT ON; SELECT SERVERPROPERTY('ProductVersion') AS ProductVersion, SERVERPROPERTY('Edition') AS Edition; SELECT encrypt_option, auth_scheme FROM sys.dm_exec_connections WHERE session_id = @@SPID;"
```

Первая попытка в песочнице: exit code 1, ODBC Driver 17 сообщил
«Клиент не поддерживает шифрование» и «В пакете безопасности отсутствуют учетные данные».
Повтор той же read-only команды вне песочницы: exit code 1,
«Цепочка сертификатов выпущена центром сертификации, не имеющим доверия».
Это подтверждает блокер доверия TLS; успешный вход/результат SELECT не получен.
Проверка через SSMS не выполнялась. Пароли не запрашивались и не выводились.

### Git

`git rev-parse --show-toplevel`: `D:/SecureFileManager`; ветка `main`.
`git log -5 --oneline`: два существующих коммита, HEAD `0172594`.
До работы: `.gitignore` пуст; нет solution, csproj, исходников, тестов, миграций,
global.json и NuGet.Config. Есть документы и PNG-концепт.
`git status --porcelain=v1 -uall` показал исходные пользовательские изменения:
неотслеживаемые AGENTS.md и `design/egyptian-ui-concept.png`, удаление прежнего PNG
с длинным русским именем. Эти изменения не перезаписывались и не индексировались.

### Результат и следующий шаг

PRE-01 выполнен, конфигурация записана в DECISIONS.md. PRE-02 блокирован TLS.
PRE-03 остаётся открытым формальным вопросом. PRE-04 не отмечен: commit не разрешён.
Этап 01 не реализован; restore/build/test/запуск WPF не выполнялись.
Новых NuGet-пакетов нет. SEC-10: секреты не использовались; проверки сертификата
сохранены. Негативные тесты SEC-01–09 неприменимы к отсутствующему коду.

Для устранения блокера владельцу среды:

1. Подготовить сертификат Server Authentication для SQL Server с именем сервера
   в CN/SAN, соответствующим имени подключения, и доверенной цепочкой.
2. Через SQL Server Configuration Manager назначить его экземпляру SQLEXPRESS,
   предоставив службе доступ к закрытому ключу. Установить проверенный сертификат
   выдающего CA/промежуточные сертификаты в соответствующие хранилища доверия клиента.
   Не импортировать непроверенные сертификаты. Перезапуск службы согласовать с её
   владельцем; в этой проверке он не выполнялся.
3. Повторить команду выше с именем сервера, соответствующим сертификату, а также
   подключение в SSMS: Windows Authentication, шифрование включено, Trust server
   certificate выключено. Ожидание: SELECT выполнен, encrypt_option = TRUE.
4. После успешного PRE-02 вернуться только к этапу 01. При обновлении до VS 2026
   установить workload «Разработка классических приложений .NET» и SDK .NET 10,
   затем повторить проверку совместимости. Ответ по PRE-03 остаётся необходимым
   формальным уточнением перед реализацией.

Инструкции Microsoft:
- [Назначение сертификата SQL Server](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/configure-sql-server-encryption)
- [Требования к сертификатам](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/certificate-requirements)
- [Ошибка недоверенного CA](https://learn.microsoft.com/en-us/troubleshoot/sql/database-engine/connect/error-message-when-you-connect)

## 2026-11-01 — Повтор PRE-02 и реализация только этапа 01

Пользователь явно разрешил локальное исключение TrustServerCertificate=True при
Encrypt=True и поручил продолжить на VS 2022/net9.0-windows. Предыдущая запись о
блокировке сохранена как история; текущий блокер подключения снят.

### PRE-02: реальный SQL Server

```powershell
sqlcmd -S '.\SQLEXPRESS' -E -N -C -l 8 -b -W -Q "SET NOCOUNT ON; SELECT CONVERT(nvarchar(40),SERVERPROPERTY('ProductVersion')) AS ProductVersion, CONVERT(nvarchar(80),SERVERPROPERTY('Edition')) AS Edition; SELECT encrypt_option, auth_scheme FROM sys.dm_exec_connections WHERE session_id = @@SPID;"
```

Выполнено вне песочницы с учётными данными Windows пользователя. Exit code 0:

```text
ProductVersion Edition
15.0.2000.5     Express Edition (64-bit)
encrypt_option auth_scheme
TRUE           NTLM
```

`-C` соответствует разрешённому локальному доверию сертификату; `-N` сохраняет TLS.
Пароли не использованы. Выполнены только SELECT; БД SecureFileManagerDB не создавалась,
миграции не запускались. SSMS отдельно не проверялся. В TASKS PRE-02 уточнён по
фактическому способу проверки из текущего поручения — sqlcmd вместо GUI SSMS.

### Реализация

Созданы SecureFileManager.sln и четыре проекта с направлением Wpf→Core/Data,
Data→Core, Tests→Wpf. Core/Data — net9.0; Wpf/Tests — net9.0-windows.
Созданы каталоги будущих слоёв с .gitkeep без фиктивной бизнес-логики.

Wpf: App.xaml/App.xaml.cs с Microsoft DI, ObservableObject/SectionViewModel/
MainWindowViewModel, Views/MainWindow, Resources/StarterTheme.xaml.
Окно использует нативные контролы, системную рамку, русский текст, палитру концепта,
ListBox с binding SelectedSection и шесть явно обозначенных экранов-заготовок.
Навигация проверена unit-тестами; Window/ViewModel создаются через DI.
Views не содержат SQL/IO/проверок прав. Прикладные операции и команды не реализованы.
Нет фиктивных файлов, пользователя admin или утверждений о работающем аудите/БД.

Добавлены global.json, .editorconfig (UTF-8), .gitignore, общий безопасный
appsettings.example.json и docs/DEVELOPMENT.md. Локальный
appsettings.Development.local.json с Windows Authentication и Encrypt=True;
TrustServerCertificate=True исключён из Git и MSBuild items. Приложение ещё не
загружает настройки БД — это этап 02. В общем примере TrustServerCertificate=False.

Пакеты: Microsoft.EntityFrameworkCore.SqlServer 9.0.22 (EF Core/Relational транзитивно
9.0.22), Microsoft.Extensions.DependencyInjection 9.0.22, xUnit 2.9.2,
xunit.runner.visualstudio 2.8.2, Microsoft.NET.Test.Sdk 17.12.0.
Пакеты стабильные; назначение и совместимость описаны в DEVELOPMENT.md.

### Проверки на Windows

| Команда/проверка | Результат |
|---|---|
| `dotnet new sln/wpf/classlib/xunit --no-restore` и добавление проектов | Созданы solution и четыре проекта |
| `dotnet restore SecureFileManager.sln` | Успех, восстановлены все четыре проекта |
| `dotnet build SecureFileManager.sln -c Debug --no-restore` | Успех, 0 предупреждений, 0 ошибок |
| `dotnet build SecureFileManager.sln -c Release --no-restore` | Успех, 0 предупреждений, 0 ошибок |
| `dotnet test SecureFileManager.sln -c Debug --no-build` | 2/2 пройдено, 0 пропущено |
| `dotnet list SecureFileManager.sln package --vulnerable --include-transitive` | По текущим NuGet-источникам известных уязвимых пакетов не найдено |
| `git check-ignore src/SecureFileManager.Wpf/appsettings.Development.local.json` | Локальный файл игнорируется |
| Поиск appsettings в Wpf/bin/Debug и Release | Локального файла в выходных каталогах нет |
| `git diff --check` | Ошибок whitespace нет; Git сообщает о нормализации LF→CRLF |

Тесты проверяют переключение всех разделов с PropertyChanged и сохранение текущего
экрана при null/неизвестном/повторном выборе. SQL/пользовательские файлы тестами не затрагиваются.

Первоначальная оценка WPF-проекта в песочнице сообщила Access denied на
AppData/Local/Microsoft SDKs. Restore/build/test выполнены вне песочницы с реальными
Windows SDK, без обхода компиляции XAML. При повторной сборке запущенный нами
тестовый exe блокировал копирование apphost (MSB3027/MSB3021); завершён только этот
процесс после проверки полного пути. Повторная Debug-сборка успешна: 0/0.

### Запуск GUI и ограничения

Собранный Debug exe запущен через computer-use. Найдено окно
«Secure File Manager — стартовый каркас», прочитано дерево доступности:
заголовок, шесть разделов, начальный экран «Мои файлы», описание заготовки,
честный статус об отсутствии runtime-проверки БД. Дерево сохранено в
`docs/STARTUP_UI_TREE.txt`; это подтверждает создание окна на Windows.

Захват изображения не удался: `FrameArrived timed out`, после обновления привязки —
`window capture timed out`. Accessibility-чтение без изображения работает.
Попытка клика по элементу возвращает `coordinate input geometry is unavailable`;
Tab не дал подтверждённой смены фокуса. Скриншот, визуальная оценка, ручное
переключение мышью/клавиатурой, размеры 1024×640 и DPI 150% не подтверждены.
Эти ограничения не заменены утверждением об успешной визуальной проверке.
Функция выбора секций проверена двумя unit-тестами, а запуск — реальным окном.
Ручную визуальную проверку нужно повторить на этапе 03/при доступном захвате экрана.
После проверки тестовый процесс завершён, чтобы не блокировать последующие сборки.

### Безопасность, статус, границы

SEC-10: секреты не записывались; конфигурация Windows Authentication без пароля.
TLS подтверждён encrypt_option=TRUE; доверие сертификату применяется только к явно
разрешённой локальной проверке/локальному файлу. Production-исключение не добавлено.
SEC-01–09 бизнес-модулям ещё не применимы: ни файловых действий, ни таблиц/схемы,
ни аудита/аутентификации пока нет. Это не результат негативного security-тестирования.

01.01–01.06 отмечены по созданным артефактам, сборке, тестам, запуску и реальному SQL.
PRE-03 остаётся формальным риском; пользователь разрешил реализацию, согласование
с преподавателем не заявляется. Изменения пользователя в Git сохранены, commit не создан.
Этапы 02–10 не реализовывались. Следующий шаг — 02 только после нового разрешения пользователя.

## 2026-11-01 — Этап 02: модели, база и Persistence

Перед изменениями перечитаны AGENTS.md, ТЗ, TASKS, DECISIONS и PROGRESS; изучены
существующие проекты/ссылки/конфигурация/тесты. Git был чистым, HEAD d32bc5c.
SDK по-прежнему 9.0.200, SQL Express 15.0.2000.5, Windows Authentication, TLS TRUE.
Схема/таблицы предыдущих баз не изменялись; регистрация/вход/новый UI не реализованы.

### Реализация 02.01–02.06

- Core: User, FileRecord, Operation; FileKind/OperationType/OperationResult;
  IUserRepository/IFileRepository/IOperationRepository, RepositoryResult/RepositoryStatus.
- Data: AppDbContext с mapping, FK NO ACTION, SQL CHECK, уникальными индексами,
  UTC-конвертацией, глобальным soft-delete фильтром, rowversion Files и защитой аудита
  от update/delete через SaveChanges; отдельные репозитории и safe error mapping.
- Конфигурация SQL и design-time factory находятся в Data; AddSecureFileManagerData
  регистрируется только в composition root Wpf, Views не выполняют SQL/IO.
- Локальный dotnet-ef и Design package 9.0.22; EF/provider/Relational той же версии.
  Назначение, конфигурация и миграции описаны в DEVELOPMENT/PERSISTENCE/DECISIONS.
- Отслеживаемый локальный JSON удалён; подключение перенесено в игнорируемый
  appsettings.Development.json. Пароля в нём нет. Encrypt=True обязательно;
  доверие сертификату разрешено только для явного Development и локального SQLEXPRESS.
  Production использует проверку сертификата; локальный файл не входит в MSBuild items.

### Подключение и новая база

До создания дважды выполнена проверка DB_ID/sys.databases: SecureFileManagerDB
отсутствовала. Затем выполнены команды (вне песочницы, Windows credentials):

```powershell
sqlcmd -S '.\SQLEXPRESS' -d master -E -N -C -b -W -Q "SELECT name,state_desc FROM sys.databases WHERE name=N'SecureFileManagerDB'; SELECT encrypt_option,auth_scheme FROM sys.dm_exec_connections WHERE session_id=@@SPID;"
dotnet new tool-manifest
dotnet tool install dotnet-ef --version 9.0.22
dotnet ef migrations add InitialCreate --project src/SecureFileManager.Data --startup-project src/SecureFileManager.Data --output-dir Migrations --no-build
$env:SFM_ENVIRONMENT = 'Development'
$env:SFM_CONFIG_DIRECTORY = 'D:\SecureFileManager\src\SecureFileManager.Wpf'
dotnet ef migrations script --project src/SecureFileManager.Data --startup-project src/SecureFileManager.Data --output docs/sql/initial-create.sql
dotnet ef database update --project src/SecureFileManager.Data --startup-project src/SecureFileManager.Data --no-build
```

Проверен код Up перед применением: только создание новой схемы, PK/FK/CHECK/индексов.
Применена 20261101094955_InitialCreate. Существует Users, Files, Operations и
__EFMigrationsHistory с версией 9.0.22. Все три FK NO_ACTION, включены и доверены;
filtered unique index Files WHERE IsDeleted=0 и unique Users.Username присутствуют.
Рабочие таблицы пусты (0/0/0), seed и пользовательские данные не создавались.
Down/EnsureDeleted/DROP DATABASE не запускались.

### Итоговые проверки

```powershell
dotnet restore SecureFileManager.sln
dotnet build SecureFileManager.sln -c Debug --no-restore
$env:SFM_ENVIRONMENT = 'Development'
$env:SFM_CONFIG_DIRECTORY = 'D:\SecureFileManager\src\SecureFileManager.Wpf'
$env:SFM_RUN_SQL_TESTS = '1'
dotnet test SecureFileManager.sln -c Debug --no-build --logger 'trx;LogFileName=stage02-final.trx'
dotnet ef migrations has-pending-model-changes --project src/SecureFileManager.Data --startup-project src/SecureFileManager.Data --no-build
dotnet list SecureFileManager.sln package --vulnerable --include-transitive
sqlcmd -S '.\SQLEXPRESS' -d SecureFileManagerDB -E -N -C -b -W -w 240 -f 65001 -i docs/sql/verify-stage02.sql -o docs/sql/stage02-verification.txt
git diff --check
```

| Проверка | Результат |
|---|---|
| Restore | Все проекты восстановлены |
| Debug build | Успех: 0 ошибок, 0 предупреждений |
| xUnit | 16/16 пройдено, 0 ошибок, 0 пропусков; восемь SQL-тестов реального Engine |
| EF model vs migration | No changes have been made to the model since the last migration |
| NuGet audit | По текущим источникам известных уязвимых пакетов не найдено |
| Подключение к рабочей/тестовой БД | Encrypt TRUE, auth NTLM, пароль не используется |
| Schema check | Таблицы, столбцы, FK, индексы и CHECK подтверждены реальным каталогом SQL |
| Данные рабочей/итоговой тестовой БД | Users/Files/Operations = 0/0/0 |
| Local config output | appsettings.Development.json игнорируется Git и отсутствует в Wpf/bin |
| diff --check | Ошибок whitespace нет; предупреждение Git о LF→CRLF |

Итоговый TRX — tests/SecureFileManager.Tests/TestResults/stage02-final.trx (игнорируется Git).
Сводка без provider diagnostics — docs/STAGE02_TESTS.md.
Проверены: CRUD метаданных, фильтр владельца по ID/пути, soft-delete и повтор пути,
сохранность связанных событий, NULL UserId/FileId, case-insensitive логины и пути,
FK/CHECK/unique на реальном SQL, rowversion-конфликты, некорректные относительные пути,
не-UTC даты, запрет изменения/удаления аудита и безопасная ошибка недоступной БД.
Это проверка Persistence, а не готовой аутентификации/файловой безопасности приложения.

Тесты запускались с несколькими прогонами разработки; каждый создавал новую БД,
каждый тест откатывал свои записи. Остались только тестовые схемы с историей миграции:
- SecureFileManagerTests_acddbe084166425f9635fd822e5de512
- SecureFileManagerTests_66318155c2374aea88801e981637319a
- SecureFileManagerTests_956bec2580c742cd840383a3522226c0
- SecureFileManagerTests_f24acc1b2016437ea5d6cf8df557ecc7 (итоговый прогон).

Базы не удалялись. Итоговая пустота/TLS подтверждены в docs/sql/stage02-test-verification.txt.
Отдельная _missing база для теста отказа не создавалась, туда выполнялся только SELECT.
Повтор полного набора создаст ещё одну новую тестовую БД; автоматического удаления нет.

### Найденные и исправленные ошибки

1. В SQL-отчёте RowCount был использован как неэкранированный алиас (ошибка 156).
   Исправлен на [RowCount], повторный запуск exit code 0.
2. EF SQL execution strategy оборачивает ошибку отсутствующей БД в InvalidOperationException
   с InnerException SqlException. Первый добавленный тест отказа это обнаружил (15/16);
   RepositoryBase теперь возвращает InfrastructureError без сырых диагностик. После
   исправления и финального Development guard весь набор прошёл (16/16).
3. Локальная конфигурация после пользовательского коммита оказалась отслеживаемой Git;
   перенесена в игнорируемое имя, старый файл удалён из дерева. Строки с паролем не было.

### WPF и ограничения 02.07

Итоговый Debug exe запущен через computer-use. Найдено одно окно «Secure File Manager —
стартовый каркас»; доступны те же шесть разделов и начальный экран «Мои файлы».
Дерево доступности сохранено в docs/STAGE02_STARTUP_UI_TREE.txt. Затем завершён только
этот тестовый процесс после проверки полного пути. Приложение не мигрирует БД на старте,
не выполняет пользовательских SQL/файловых действий и не заявляет активный журнал.
Скриншот/DPI/визуальная регрессия на этом этапе не проверялись, UI не перерабатывался.

02.07 проверен программно: полный read-only скрипт для ручного SSMS 19 находится
в docs/sql/verify-stage02.sql; фактический SQL-отчёт — docs/sql/stage02-verification.txt.
Ручной GUI-прогон SSMS не выполнялся, поэтому чекбокс 02.07 оставлен открытым.
Реализация этапа 02 и автоматические проверки завершены, формальная ручная часть — частично.

### Безопасность и следующий шаг

SEC-01: LINQ/параметры; SQL injection-подобный поиск не изменяет выборку.
SEC-03: persistence фильтрует переданный OwnerId; аутентификация вызывающего — этап 04.
SEC-08: конфликт rowversion проверен; физические файловые гонки здесь не затрагивались.
SEC-09: история сохраняется при soft-delete, NULL FK допускаются, журнал append-only
через контекст; отказ SQL не возвращает успех. Полный аудит файловых сценариев — этап 05/08.
SEC-10: нет паролей/хешей/строк подключения в диагностике репозиториев; TLS сохранён,
исключение сертификата ограничено Development/local. Текст Details должен безопасно
формировать сервис — произвольные секреты автоматически из текста не распознаются.
SEC-02/04/05/06/07 физических модулей ещё не реализованы и не выдаются за проверенные.

Обновлены TASKS, DECISIONS, DEVELOPMENT; добавлены PERSISTENCE, SQL-отчёты и тестовая сводка.
Полный список изменений — docs/STAGE02_FILES.md. Git-коммит не создавался,
существующие пользовательские данные не удалялись. Следующий шаг: ручная 02.07;
к этапу 03 можно переходить только после нового разрешения пользователя.

## 2026-11-01 — Завершение ручной проверки 02.07 и этапа 02

Пользователь подтвердил успешную ручную проверку SecureFileManagerDB через SSMS 19:
таблицы Users, Files, Operations и связи присутствуют, проверочные SQL-запросы
выполнены без ошибок. Источник результата — сообщение пользователя; Codex не
выполнял эту ручную проверку самостоятельно и не создавал новые скриншоты/SQL-отчёты.

На основании этого подтверждения задача 02.07 отмечена выполненной в TASKS.md.
Вместе с ранее пройденными сборкой и 16/16 тестами это закрывает приёмку этапа 02.
Статус этапа 02: **завершён**. Предыдущие записи об открытой ручной проверке
сохранены как история и заменены настоящим результатом.

В этом обновлении изменены только TASKS.md и docs/PROGRESS.md. Код, схема БД
и настройки безопасности не менялись; повторная сборка и тесты не требовались.
Проверка документации: git diff --check. Этап 03 не начат, Git-коммит не создавался.
Следующий шаг — этап 03 только после явного разрешения пользователя.

## 2026-11-01 — Этап 03: тематический WPF-каркас

Пользователь разрешил только этап 03 (03.01–03.07). Перед изменениями прочитаны
AGENTS, TECHNICAL_SPEC, DESIGN_SPEC, TASKS, DECISIONS, PROGRESS, ASSETS_README;
открыт концепт и основные PNG. Git был чистым, HEAD c8c043e (assets).
Решение, VS 2022, .NET 9/net9.0-windows, Core/Data и миграции сохранены.

### Реализация

Главное окно заменено тематической оболочкой с WindowChrome, логотипом,
подзаголовком, орнаментом солнца, шестью пунктами меню с иконками, Анубисом,
неавторизованным пользователем, папирусной рабочей областью и нижним орнаментом.
Отдельные Files/Drives/Archives/Formats/Journal/Settings View и ViewModel;
ContentControl/DataTemplate переключает экраны в одном окне. Навигация и состояние
находятся в VM, регистрацию сохраняет существующий Microsoft DI.
Code-behind содержит InitializeComponent и только три системных оконных команды.

Созданы общие словари Colors/Brushes/Icons/Typography/Controls/DataGrid/Navigation/
Converters/States/Views. Палитра — точные токены DESIGN_SPEC; системные шрифты.
Стили охватывают кнопки, поля, ComboBox, таблицы, CheckBox, ScrollBar, навигацию,
карточки, состояния и будущий DialogWindow. Подготовлены loading/empty/error,
Hover/Pressed/Selected/Disabled/Focus. Имена для экранного диктора — на русском.
Старый неиспользуемый StarterTheme.xaml удалён.

Файловые действия, фильтры/поиск, загрузка событий, выбор документа и ZIP отключены
с причинами. Реальной регистрации/авторизации/файловых/форматных операций нет.
Нет demo-файлов, admin, фиктивных событий и ложного «Журнал активен».
Настройки показывают реальную версию сборки, но не заявляют успешное runtime-соединение.

### Графические ресурсы

Все 19 входных PNG успешно декодированы, проверены размеры и диапазон alpha.
18 нужных файлов скопированы в Wpf/Assets; SHA256 каждой копии совпадает с оригиналом.
Среди них 16 с alpha и две непрозрачные текстуры. Встроены как Resource с pack URI;
общий лист и полный концепт в сборку не включены. Иконки 256×256, логотип/текстуры
1254×1254, Анубис 1086×1448, орнаменты 2172×724.

Для лишних полей применён CroppedBitmap/SourceRect вокруг заметного alpha с запасом
8 px — только отображение, без изменения PNG. Пропорции сохраняются; фоновые текстуры
малоинтенсивны и не образуют повторяющийся узор. Все изображения реально применены.
Карта: docs/ASSETS.md; инвентаризация: docs/screenshots/stage03/asset-inspection.txt.
Отсутствующих/повреждённых ресурсов и временных замен нет, пакетов/шрифтов не добавлено.

### Проверки на Windows

```powershell
dotnet restore SecureFileManager.sln
dotnet build SecureFileManager.sln -c Debug
$env:SFM_ENVIRONMENT = 'Development'
$env:SFM_CONFIG_DIRECTORY = 'D:\SecureFileManager\src\SecureFileManager.Wpf'
$env:SFM_RUN_SQL_TESTS = '1'
$env:SFM_UI_ARTIFACT_DIRECTORY = 'D:\SecureFileManager\docs\screenshots\stage03'
dotnet test SecureFileManager.sln -c Debug --logger 'trx;LogFileName=stage03-final.trx'
git diff --check
```

| Проверка | Итог |
|---|---|
| Restore | Успешно |
| Debug build | Успешно, 0 ошибок / 0 предупреждений |
| xUnit, итоговый прогон | 18/18 пройдено, 0 ошибок / 0 пропусков |
| Регрессия Persistence | Все восемь реальных SQL-тестов пройдены |
| Ресурсы | Загружены 18 BitmapSource, размеры положительны; копии PNG не изменены |
| Навигация WPF | Команда меняет VM, SelectedItem и тип представления; все шесть экранов |
| UI состояния | Пустые таблицы, disabled-команды, loading/error и сохранение выбора проверены |
| Привязки | DataBinding trace: ошибок нет |
| Размеры | Реальное WPF-дерево отрендерено при 1440×900 и 1024×640 DIP для всех экранов |
| Экспорт 150% | Два дополнительных WPF-рендера Files при 144 DPI; не проверка DPI Windows |
| Desktop запуск | Итоговый exe открыл одно окно Secure File Manager, UIA-дерево сохранено |
| Native DPI/keyboard/window manager | Не подтверждены, нужны ручные проверки 03.06 |
| Git whitespace | Ошибок нет, Git сообщает нормализацию LF→CRLF |

TRX: tests/SecureFileManager.Tests/TestResults/stage03-final.trx (игнорируется Git).
STA-тест создаёт собственное окно вне видимой области рабочего стола, применяет
шаблоны и проверяет реальное визуальное дерево, не внешнюю UI Automation.
Рендеры — 14 PNG в docs/screenshots/stage03/, пояснения — README.md этой папки.
Это экспорт WPF, а не desktop-скриншоты и не генерация макета вместо приложения.

В полном прогоне создана отдельная новая тестовая БД
SecureFileManagerTests_f6a712c6c8ae4098b2c9b2a34dcccf78; предварительный полный прогон
оставил SecureFileManagerTests_9808bd916fc8499f84ff5e24499fa1b6. Фикстура этапа 02
откатывает записи и не удаляет БД. Рабочая SecureFileManagerDB проверялась только SELECT:
Users/Files/Operations = 0/0/0, прежняя InitialCreate 20261101094955 (EF 9.0.22),
TLS TRUE, Windows Authentication NTLM. Отчёт: persistence-regression.txt в папке рендеров.
Нет изменений Git в Core, Data, solution, global.json и миграциях.

### Найденные и исправленные проблемы

- ResourceKey сначала объявлялся после загрузки шаблонов; runtime XamlParseException
  исправлен отдельным словарём Converters, загружаемым первым.
- Поле поиска обрезало текст из-за двойного учёта Padding в шаблоне; исправлено.
- Базовые стили Window/UserControl назначены производным типам явно, чтобы применить
  шрифты/цвета; при 640 DIP пустое состояние компактное, поиск переносится на строку.
- Первый offscreen-тест не подключал окно, и колонки сохраняли ширину прошлого размера.
  Тест теперь показывает собственное окно вне рабочего стола; нативный перерасчёт
  работает. Дополнительная логика ресайза таблиц в продукт не добавлялась.

### Соответствие концепту и оставшиеся проверки

Все шесть экранов визуально просмотрены в обоих размерах, Files также при 150% экспорта.
Совпадают композиция, палитра, изображения, размещение инструментов и таблиц.
Отличия: нет вымышленных рабочих файлов/пользователя/статусов; предоставленные
иллюстрации подробнее исходных; нет неработающего переключателя вида; при малом окне
поиск переносится и Анубис уменьшается. Подробное сравнение — docs/STAGE03_REPORT.md.

Computer-use подтвердил запуск и UIA-структуру, включая имена меню и disabled-кнопки.
Захват native-окна завершался window capture timed out; Tab не дал достоверного
подтверждения смены фокуса. Нельзя считать проверки drag/resize/maximize/minimize/
restore/Alt+F4 и системного DPI выполненными. Код WindowChrome/команды реализован,
но нативное взаимодействие требует ручного подтверждения. Ни настройки Windows,
ни её масштаб автоматически не менялись.

03.01–03.05 и 03.07 отмечены выполненными; 03.06 оставлена открытой с точными
ручными шагами в STAGE03_REPORT.md (100%/150%, оба размера, клавиатура, оконный менеджер).
Статус этапа: Частично по приёмке, реализация и автоматические проверки завершены.
Полный перечень файлов — docs/STAGE03_FILES.md.

### Безопасность и границы

SEC-01/03/08/09/10 Persistence повторно проверены существующими SQL-тестами;
GUI не вызывает SQL/IO, не расширяет доступ и не отключает проверки. Неподключённые
сервисы/аудит/пользователь отображаются честно. Секретов/строк подключения в GUI нет.
Пользовательские БД/файлы не удалялись. Функции этапов 04–08 не реализованы.
Git-коммит не создавался. Следующий шаг: ручная приёмка 03.06; этап 04 только после
отдельного разрешения пользователя.

## 2026-11-01 — Ручная приёмка 03.06 и завершение этапа 03

Пользователь сообщил, что запустил Secure File Manager в Visual Studio, проверил
внешний вид и навигацию; видимых проблем интерфейса не обнаружено. По его явному
указанию результат принят как ручная приёмка задачи 03.06, задача отмечена выполненной.
Источник результата — сообщение пользователя, а не новая автоматическая проверка Codex.
Конкретные параметры ручного прогона дополнительно не заявляются проверенными агентом.

Проверен актуальный TASKS.md: остальные задачи 03.01–03.05 и 03.07 уже выполнены.
Теперь закрыты все задачи 03.01–03.07; общий статус этапа 03 обновлён на **Завершён**.
Ранее полученные результаты сборки и 18/18 тестов остаются основанием автоматической
приёмки. Прежние записи о частичном статусе и ограничениях desktop-захвата сохранены
как история; текущий итог приёмки зафиксирован этой записью и в TASKS.md.

Изменены только TASKS.md и docs/PROGRESS.md. Код, БД и настройки безопасности
не менялись; повторная сборка и тесты не запускались для изменения документации.
Проверка: git diff --check. Этап 04 не начат, Git-коммит не создавался.
Следующий шаг — этап 04 только после отдельного разрешения пользователя.

## 2026-11-01 — Этап 03.5: архитектурная подготовка хранения

Прочитаны AGENTS, TECHNICAL_SPEC, DESIGN_SPEC, TASKS, DECISIONS, PROGRESS, PERSISTENCE.
Изучены csproj, модели/контракты Core, mapping/migration snapshot и репозитории Data,
DI, WPF VM и существующие тесты. Исходное дерево Git чистое, HEAD dd3f24c (3 etap).

Вывод: слои подходят как основа расширения A → B, но B не подключается одной заменой
пути. Для текущего A нет потребности менять работающие контракты/структуру сейчас.
Auth/session/file policy ещё отсутствуют; новые интерфейсы на этапе 03.5 не создавались.

Создан docs/STORAGE_ARCHITECTURE.md: принципы A/B, фактические компоненты и десять
направлений оценки, ответственность предлагаемых интерфейсов, путь операции, границы
Windows ACL, импорт, ограничения RelativePath и уникальности, риски и поэтапный переход.
Обнаружены A-специфичные owner-фильтры и audit actor==owner, отсутствие LocationId,
ограничение пути 400/CI_AS, отсутствие физического path-policy и транзакционной
координации независимых репозиториев. Они документированы как точки расширения;
существующие ограничения не удалялись и не выдаются за полноценную физическую защиту.

Обновлены DECISIONS и TASKS: этап 03.5 завершён как анализ/план, импорт 05.10 запланирован,
но не реализован. До будущего импорта/B должны быть явно уточнены FR/SEC границы:
действующее ТЗ запрещает произвольные внешние файловые операции. Сейчас только A
остаётся ближайшим режимом; исходное TECHNICAL_SPEC.md не изменено.

Проверка согласованности: rg --files, чтение перечисленных исходников и csproj,
поиск Storage/Users, IPathPolicy/IFileService/IAuthenticationService/IStorage/IFileAccess/
IUserSession, RelativePath, IgnoreQueryFilters, File/Directory/DriveInfo и ссылок Data
в Wpf. В Core/Interfaces действительно три репозиторных интерфейса; остальные
названия в новом документе явно обозначены будущими предложениями. Проверены
owner-фильтры, unique-индекс, синтаксическая проверка пути, ограничение FileId аудита,
singleton VM и отсутствие SQL/IO в Views/VM. Windows-модель доступа/имён/ссылок сверена
с официальной документацией Microsoft (ссылки в STORAGE_ARCHITECTURE.md).

Изменены только четыре Markdown-файла: STORAGE_ARCHITECTURE (новый), TASKS,
DECISIONS, PROGRESS. Код, UI, csproj/TFM, тесты, миграции и конфигурация не менялись.
Restore/build/test, SQL-команды и GUI в этом этапе не запускались; прежние 18/18
тестов не выдаются за новый прогон. К базе и пользовательским данным не обращались.
Проверки документации: git diff --check, проверка ссылок на реальные исходные файлы,
наличия всех разделов плана, нового блока 03.5 и незакрытых задач 04/05.

Статус 03.5: **Готово** (архитектурный анализ и план). Архитектура готова к разработке
этапа 04 для A с описанными границами; B потребует будущей модели расположений/прав,
новых миграций и отдельных тестов. Этап 04 не начат. Git-коммит не создан.
Следующий шаг — этап 04 только после отдельного разрешения пользователя.


## 2026-11-01 — Этап 04: реализация и автоматическая проверка

Исходное состояние чистое, HEAD 60aa75b (3.5 etap). Изучены актуальные AGENTS,
TECHNICAL_SPEC, DESIGN_SPEC, TASKS, STORAGE_ARCHITECTURE, DECISIONS, PROGRESS,
PERSISTENCE, concept/ASSETS_README; существующие Core/Data/Wpf/Tests, DI и миграции.
Пользователь явно разрешил этап 04; этап 05/B не начинались, Git-коммита нет.

Реализованы 04.01–04.06: PBKDF2 IdentityV3 с солью/rehash, полноценная регистрация,
вход и sliding lockout 5/15/15, транзакционный обязательный SQL-аудит, read-only Session,
выход и очистка VM, GUID storage/политика ownership/проверка физических путей.
Добавлены египетские формы входа/регистрации, async/busy команды, PasswordBox,
стартовая проверка SQL и повтор. Главное окно/шесть разделов сохранены.

Новый пакет Core Microsoft.Extensions.Identity.Core 9.0.22 соответствует текущему
.NET 9, нужен для штатного PasswordHasher; Core не получил EF/WPF/SQL.
Новая миграция 20261101122237_AddLoginFailureWindow добавляет единственную nullable
колонку Users.FailedLoginWindowStartUtc. Применена к существующей рабочей БД после
проверки истории/состояния; initial migration, FK/индексы, Files/Operations не изменены.
Users/Files/Operations до/после 0/0/0, encrypt_option TRUE. Тестовых аккаунтов в рабочей
БД нет. Прежний Production TLS guard сохранён, секреты/локальная конфигурация не добавлены.

Проверки в Windows:
- dotnet restore — успешно.
- dotnet build SecureFileManager.sln -c Debug — успешно, 0 warnings/errors.
- dotnet test SecureFileManager.sln -c Debug с SFM_RUN_SQL_TESTS=1,
  SFM_ENVIRONMENT=Development — 40/40, 0 skipped (TRX stage04.trx).
- После уточнения UI fixture WpfShellTests повторно 1/1 (stage04-wpf.trx).
- dotnet ef migrations has-pending-model-changes (Data/startup Data) — изменений нет.
- SQL read-only: обе миграции/новая колонка/TLS/сохранность рабочей базы подтверждены.
- Изолированная SecureFileManagerTests_40b6d9ba82af4e82931a07f373c75bc1:
  13 тестовых Users, 1 Files, 56 Operations; есть все security-event типы и NULL UserId/FileId.
  База и временные каталоги оставлены для анализа; DROP/EnsureDeleted/recursive delete нет.

Тесты подтвердили CI/concurrent регистрацию, hash salt/format/rehash, валидацию,
неверный/неизвестный/неактивный вход, точное sliding window и истечение lockout,
конкурентные счётчики, смену двух пользователей/очистку VM, разные корни, чужие ID,
traversal/absolute, фактическую NTFS junction/hard link, аудит/rollback SQL, каталог-сбой,
недоступную БД и сохранность sentinel-файлов. Старые 18 тестов сохранены.
При недоступном аудите logout всё равно очищает Session, не заявляя успешную запись.

Wpf.exe действительно запущен; UI Automation подтвердила AuthenticationWindow,
PasswordBox и успешный статус стартового SQL-подключения. Проверочный процесс остановлен,
реальные аккаунты/пароли через GUI не вводились. WpfShellTests отрисовал обе формы,
переключил шесть разделов, проверил 18 ресурсов и отсутствие DataBinding errors;
сохранены 22 RenderTargetBitmap PNG в screenshots/stage04. Это не desktop-скриншоты.
UI fixture содержит только явно тестовую сессию и пустые таблицы.
Desktop capture: window capture timed out; ввод: coordinate input geometry is unavailable.
Настоящий DPI Windows, системные кнопки/клавиатура и полный GUI A → logout → B не приняты.

Первый прогон остановлен из-за захвата чужого Application Dispatcher в VM; исправлен
захват собственного dispatcher, после этого прогоны успешны. Smoke-exe однажды удерживал
apphost при сборке; остановлен только свой экземпляр, финальная сборка успешна.
Гонки FS, наследуемые ACL/прямой доступ одним Windows пользователем и orphan GUID после
SQL rollback задокументированы, не выданы за полную OS-изоляцию.

Обновлены DECISIONS/STORAGE_ARCHITECTURE/PERSISTENCE, создан STAGE04_REPORT с полным
перечнем файлов, STAGE04_MANUAL_TEST, SQL-миграция/проверки/вывод и README рендеров.
04.01–04.06 отмечены выполненными. 04.07 оставлен открытым только для ручной GUI-приёмки;
автоматическая часть полностью пройдена. Общий статус этапа: **Частично**.
Следующий шаг: ручная приёмка 04.07 по инструкции, затем фиксация результата.
Этап 05 и Git-коммит без отдельного разрешения запрещены.

Финальная проверка граничных случаев: logout с заранее отменённым токеном также
очищает Session, а корень диска/путь с нормализацией к корню диска отклоняется
отдельным StorageConfigurationTests. Окончательный полный набор: 40/40, 0 skipped.

Дополнительно проверен видимый интерактивный запуск (WindowStyle Normal) и активация
окна: desktop capture снова вернул window capture timed out. Скрытый запуск не является
единственной причиной ограничения; свой проверочный процесс остановлен без ввода данных.
Финальные проверки Git: diff --check без ошибок, все 81 ссылки перечня файлов существуют,
HEAD остался 60aa75b, локальная конфигурация и Storage игнорируются.


## 2026-11-02 — Исправление подключения при ручном запуске этапа 04

Задача пользователя: устранить «БД недоступна» при регистрации в WPF/VS.
Исходный Git чистый, HEAD fbd04ee (4 etap). Изучены текущие AGENTS/TECHNICAL_SPEC/TASKS,
код загрузки конфигурации, csproj, тесты, инструкции запуска и прежние результаты.

Диагностика без секретов:
- MSSQL$SQLEXPRESS запущен; SecureFileManagerDB доступна через Windows Authentication.
- Обе миграции присутствуют; sqlcmd с TLS и локальным доверием сертификату успешен,
  encrypt_option=TRUE.
- Без локального доверия сервер отклонён: цепочка сертификата не доверена.
- В Wpf нет профиля запуска, SFM_ENVIRONMENT/SFM_CONFIG_DIRECTORY не заданы.
  Локальный appsettings.Development.json существует, Windows Authentication/TLS включены,
  SQL-пароля нет; файл не копируется в bin. Обычный запуск попадал в Production
  и строгую проверку сертификата вместо ранее согласованной локальной конфигурации.

Исправлено:
- Добавлен Wpf/Properties/launchSettings.json: профиль SecureFileManager (Development)
  с окружением и относительным каталогом конфигурации, без строки подключения/секретов.
- В Wpf.csproj установлен RunWorkingDirectory для SDK 9 CLI, в профиле рабочий каталог
  VS задан ProjectDir. Локальные настройки читаются из каталога проекта, не копируются.
- Обновлены STAGE04_MANUAL_TEST, DECISIONS и сводная строка 04 в TASKS
  (там оставалось устаревшее «Не начат»). Ручная 04.07 остаётся открытой.

Проверки:
- dotnet build SecureFileManager.sln -c Debug — 0 errors/warnings.
- dotnet test SecureFileManager.sln -c Debug --no-build с SFM_RUN_SQL_TESTS=1,
  SFM_ENVIRONMENT=Development — 40/40, 0 skipped; TRX stage04-launch-fix.trx (игнорируется).
  Регистрация/аудит проверены на новой изолированной БД существующей fixture.
- dotnet run --project src/SecureFileManager.Wpf --no-build
  --launch-profile "SecureFileManager (Development)" из корня решения, без предварительных
  SFM_ENVIRONMENT/SFM_CONFIG_DIRECTORY/SFM_CONNECTION_STRING — запущен именно новый профиль.
- UI Automation реального WPF-окна подтвердила:
  «Подключение к БД проверено. Вход требует проверки пароля и аудита».
  Пароли/логины не вводились, рабочие аккаунты через GUI не создавались.
- TLS/Production-guard негативные тесты входят в успешный прогон.
  Миграции, рабочая схема, данные, хеширование и авторизация не менялись.

Исправление подключения готово. Для повторной ручной проверки остановить старый
экземпляр, выбрать в VS профиль SecureFileManager (Development) и запустить F5.
Весь этап 04 остаётся Частично до ручной приёмки 04.07. Этап 05 не начат, коммита нет.


## 2026-11-02 — Новый физический StorageRoot перед этапом 05

Объём: перенастроить уже перенесённый пользователем Storage, без повторного перемещения,
удаления файлов, новой БД и изменения User.Id. На старте сохранены незакоммиченные
изменения предыдущего исправления запуска (TASKS/DECISIONS/PROGRESS/MANUAL/csproj/profile).
Прочитаны AGENTS, требования Storage/SEC из TECHNICAL_SPEC, TASKS, архитектура,
конфигурация/DI, адаптер/сервисы и тесты.

Исходная реализация брала только SFM_STORAGE_ROOT либо неявно AppData. Теперь
StorageConfiguration читает StorageRoot из прежнего appsettings-файла по
SFM_ENVIRONMENT/SFM_CONFIG_DIRECTORY; непустой SFM_STORAGE_ROOT имеет приоритет.
В игнорируемом локальном Development JSON задан D:\SecureFileManagerData\Storage,
остальные параметры, включая SQL, сохранены без вывода секретов. Example содержит
пустой обязательный StorageRoot. Общая Production-конфигурация локального пути не получила.

App регистрирует PersonalStorage.FromConfiguration; адаптер формирует Users/{GUID}
и проверяет доступность диска перед mkdir. Нет AppData fallback, нового переноса,
горячего переключения, изменения ACL или обхода owner/session/path/reparse/SQL-аудита.

Подтверждено read-only:
- D:\SecureFileManagerData\Storage\Users существует, структура не содержит лишних Storage/Users.
- В рабочей БД по-прежнему один активный пользователь; его прежний GUID совпадает с папкой.
  Files=0, обе прежние миграции на месте; пользователь и его Id не менялись.
- Собранный Wpf-адаптер загрузил реальный JSON с новым StorageRoot и Resolve нашёл каталог
  без вызова Prepare/mkdir. Время создания каталога сохранилось, файлы не читались.
- Storage приложения в AppData отсутствовал до/после; Storage внутри Git отсутствует.
  Игнорируемый read-only probe находится в artifacts/storage-root-check.

Проверки в Windows:
- dotnet restore — успешно.
- dotnet build SecureFileManager.sln -c Debug — 0 warnings/errors.
- dotnet test SecureFileManager.sln -c Debug --no-build — 47/47, 0 skipped.
  TRX: tests/SecureFileManager.Tests/TestResults/storage-root-change.trx (игнорируется).
- SQL fixture повторно использовала ранее созданную
  SecureFileManagerTests_783137af4cea46fbae3bb2495e3782bd по SFM_EXISTING_TEST_DATABASE;
  создание/миграция для режима reuse запрещены. Новых баз не создавали.
- SFM_STORAGE_TEST_ROOT=D:\SecureFileManagerData\IsolatedTests: новая регистрация, GUID-папки,
  конфигурационные файлы, sentinel, junction/hard link — только в изолированных каталогах D:.
  Тестовые данные оставлены для анализа, пользовательские каталоги не затрагивались.
- Новые проверки: выбор окружения/override, missing/empty/malformed config, отсутствие
  fallback, повторное использование папки, сохранность sentinel, недоступный диск,
  разные GUID и границы. Прежние ownership/session/audit/SQL/reparse тесты проходят.

Выявленные при проверке проблемы тестовой среды исправлены:
- Старые два persistence-теста считали БД пустой и выбирали все Files/Operations.
  Теперь запросы ограничены собственными UserId/Id; существующие записи не удаляются.
- Пустая переменная окружения считается незаданным override, согласованно с SQL-конфигом.
- Старый WPF-тест зависел от native resize скрытого окна: получал 1440x900 вместо
  1024x640, desktop pump также зависал. Теперь измеряется логический клиентский layout
  без Window.Show/desktop pump. Сохранены проверки размеров/ресурсов/привязок и
  loading/error Visibility. UI приложения не менялся. Native DPI/resize не выдаётся
  за новую автоматическую проверку.

Обновлены STORAGE_ARCHITECTURE/DECISIONS, создан STORAGE_CONFIGURATION с приоритетами,
локальным путём, запуском VS и прямого exe, проверками и ручной инструкцией.
TASKS отражает сообщение пользователя о завершении этапа 04; источник внешней приёмки —
его текущее поручение, не новый GUI-прогон Codex. Этап 05 остаётся не начат.
Итог перенастройки: готово. Вручную осталось перезапустить WPF и войти существующим
пользователем; реальный пароль автоматически не вводился. Коммита нет.


## 02.11.2026 — этап 05, реализация и автоматические проверки

Реализованы 05.01–05.08 и 05.10: реальные DriveInfo, личная навигация, TXT/папки,
чтение/редактор/сохранение/удаление, поиск/сортировка, явный импорт. Прежний дизайн,
сессии и конфигурация D:\SecureFileManagerData\Storage сохранены. Нет новых пакетов,
миграций, режима B или функциональности этапа 06. Коммит не создавался.

SQL/FS согласуются через общий transaction store и private plan/staging/backup.
Проверены rollback при отказе обязательного аудита, неоднозначный commit, recovery,
неожиданная внешняя замена, конкуренция, stale content, отсутствие БД и отказы IO.
При проверке исправлены native rename (NUL в FILE_RENAME_INFO), pin родительских
каталогов (LIST_DIRECTORY для запрета rename), повторное закрытие окна и выдача
устаревшего результата при смене сессии после await. Успешный commit не откатывается
из-за UI observer. SQL child-query ограничен текущей папкой; сопоставление линейное.

Фактически выполнены dotnet restore, dotnet build SecureFileManager.sln -c Debug
(0 предупреждений/ошибок), dotnet test SecureFileManager.sln -c Debug через
scripts/Test-Stage05.ps1 с изолированной БД/каталогом: 66 passed, 0 failed, 0 skipped.
Предыдущие тесты не удалены. Test TRX: tests/SecureFileManager.Tests/TestResults/stage05.trx.
Тестовый скрипт удалил только созданную этим запуском БД и run-каталог; прежние ресурсы
сохранены. Read-only проверка рабочей SQL: Users=1, Files=0, две миграции,
соединение зашифровано; существующий GUID-каталог найден. Пользовательские файлы
и идентификаторы не менялись, операции проверялись в изолированном хранилище.

WPF STA проверяет реальный DI graph/инициализацию, навигацию и логический layout.
В screenshots/stage05/final сохранены 23 рендера; TEST-данные явно отделены от рабочих.
Проверены глазами компактные toolbar/таблица/empty state при 1024×640.
Процесс Development запускался, но целевое native окно недоступно через CUA:
интерактивный вход, системные диалоги, resize/DPI/клавиатура требуют ручной приёмки.
Junction/hard links и запрет rename предка подтверждены тестами. Отдельная file symlink
не создана в доступной среде; изменение привилегий Windows не выполнялось.
Реальный full disk/power loss не проверялся; использованы контролируемые ошибки IO.

Итог: частично. 05.09 открыт до ручной приёмки по STAGE05_MANUAL_TEST.md;
полный отчёт/список файлов — STAGE05_REPORT.md. Этап 06 не начат.

## 02.11.2026 — исправление ошибки ручной приёмки «Диски»

Пользователь предоставил XamlParseException: TwoWay/OneWayToSource не может записать
DriveCard.Percent (read-only). Ошибка не связана с подключением к БД. В DrivesView
для ProgressBar.Value задан явный Mode=OneWay: индикатор только отображает процент.

Прежний WPF layout-тест использовал пустой список накопителей, поэтому DataTemplate
карточки не создавался. Теперь он заполняется двумя явно тестовыми карточками:
готовой (75%) и недоступной (0%). Проверяются материализация шаблона, значения,
направление привязки и отсутствие binding errors в обоих размерах окна.
До исправления новый сценарий упал с тем же XamlParseException; после прошёл.
TRX: drive-binding-before.trx и drive-binding-after.trx в TestResults.

Повторная сборка Debug --no-restore: 0 ошибок, 0 предупреждений.
Полный повтор scripts/Test-Stage05.ps1: 66/66, 0 skipped (39 секунд).
Изолированные тестовые ресурсы очищены скриптом/fixture. Рабочие БД, хранилище,
конфигурация и механизмы доступа не изменялись. git diff --check без ошибок.
TASKS 05.09 остаётся открытым: требуется повторить ручной переход в «Диски»
после остановки старого процесса и пересборки. Этап 06 не начат, коммита нет.
## 02.11.2026 — ручная приёмка: создание TXT и ошибка после импорта

Исправлены два воспроизведённых дефекта UI:
1. NameDialog.InitializeComponent падал с XamlParseException/ArgumentNullException
   в Controls.xaml: WindowStartupLocation — обычное CLR-свойство, недопустимое в Setter.
   Setter удалён, CenterOwner установлен непосредственно в NameDialog.xaml.
2. Успешный импорт сопровождался ошибкой обновления списка: Items.Add внутри
   View.DeferRefresh вызывал InvalidOperationException при обращении CollectionView
   к CurrentPosition. Новый список теперь подготавливается целиком и публикуется
   одним Reset без поэлементных событий во время отложенного Refresh. Поиск/сортировка
   и выделение проверены при повторной загрузке. Ошибка загрузки списка имеет отдельный
   текст и больше не объявляет ранее подтверждённую файловую операцию неподтверждённой.

Read-only проверка рабочей БД подтвердила существующую запись импортированного TXT
(66 байт, IsDeleted=0) и FILE_CREATED/Success/FILE_TX_Import. Служебная папка содержит
только owner/lock, незавершённых транзакционных подкаталогов не найдено.
Пользовательский файл не читался, не удалялся и не импортировался повторно.

WpfShellTests теперь создаёт оба NameDialog, выполняет UI-сценарий импорта/создания
TXT/папки/повторного обновления с привязанным DataGrid и инициализирует TXT-редактор.
Используется только UI fixture; реальный SQL/FS покрыт прежними интеграционными тестами.
Оба исходных исключения воспроизведены до исправления; после UI-тест проходит.
В первом полном прогоне 65/66: старый тест смены пользователей использовал
WPF CollectionView на разных async worker-потоках. Тест сохранён и исправлен:
вся его последовательность выполняется на одном STA Dispatcher, проверки не ослаблены.

Итоговый полный прогон scripts/Test-Stage05.ps1: 66/66, 0 failed, 0 skipped,
42 секунды. Созданные запуском тестовые ресурсы очищены. Сборка Debug --no-restore:
0 ошибок, 0 предупреждений. Restore выполнялся dotnet test автоматически.
Обновлены TASKS/отчёт; полная ручная приёмка 05.09 остаётся открытой.
Бизнес-логика, БД, миграции, StorageRoot и правила доступа не менялись.
Этап 06 не начат, Git-коммит не создавался.
## 02.11.2026 — итоговая ручная приёмка этапа 05

Пользователь сообщил: «провел ручную проверку и проблем не заметил».
Это подтверждение повторной ручной проверки после исправления карточек дисков,
диалога создания TXT и обновления списка после импорта. Результат принят:
проблем при проверке пользователем не обнаружено. Задача 05.09 отмечена выполненной,
все задачи 05.01–05.10 закрыты; общий статус этапа 05 — завершён / готово.

Источник ручной приёмки — сообщение пользователя; Codex не заявляет новый GUI-прогон
или отдельное подтверждение всех специальных сценариев DPI/symlink/power loss.
Ранее задокументированные ограничения остаются в силе. Последний фактический
автоматический результат — 66/66 без пропусков, сборка без ошибок и предупреждений.
В этой задаче менялась только документация, сборка и тесты повторно не запускались.
Обновлены TASKS.md, PROGRESS.md, STAGE05_REPORT.md и STAGE05_MANUAL_TEST.md.
Этап 06 не начат и требует отдельного разрешения. Git-коммит не создавался.
## 02.11.2026 — этап 06: TXT / JSON / XML

По явному поручению пользователя реализованы только 06.01–06.06. До кода восстановлен
контекст документов/Git/всех четырёх проектов, сервисов identity/session/access/path,
SQL persistence/Operations, staging/backup/recovery, текущего TXT-редактора и UI/тестов.
Открыт design/egyptian-ui-concept.png. HEAD 01893f4; исходные незакоммиченные изменения
TASKS/PROGRESS/STAGE05_MANUAL_TEST/STAGE05_REPORT сохранены, приёмка 05 не отменена.

Расширены прежние IFileService/FileService, WindowsFileStorage и TextEditorViewModel/Window.
Новый DocumentValidator не содержит IO: JsonDocument strict/depth 64; XmlReader
DtdProcessing.Prohibit/XmlResolver=null/10 МиБ/ограничения сложности. Один редактор
для обоих разделов, повторный FileId активирует существующее окно. Formats переиспользует
FilesView/VM с фильтром JSON/XML/каталогов и корректным сбросом сессии.
Save всегда валидирует в сервисе и использует прежний Stage/Publish/Settle + Files/Operations.
Новые JSON/XML создаются явно пользователем как {} / <document />. Форматирование не добавлено.

Исправлены потеря UTF-8 BOM, принятие токена внешнего изменения после Save, дубликаты
окон FileId, игнорирование отказа failure-аудита и тёмный фон под тёмным текстом редактора.
Есть понятный конфликт без force overwrite и явный Reload с подтверждением потери буфера.
10 МиБ и strict UTF-8 для всех трёх форматов; BOM/CRLF/LF сохраняются сервисом.
Подробные решения/лимиты — DECISIONS, FILE_OPERATIONS, STORAGE_ARCHITECTURE и STAGE06_REPORT.

Фактические проверки, Windows:
- dotnet --info: SDK 9.0.200; vswhere ManagedDesktop: VS 2022 17.13.35818.85; SQL Express Running.
- dotnet restore SecureFileManager.sln — успешно вне песочницы. В песочнице Microsoft SDKs Access denied.
- dotnet build SecureFileManager.sln -c Debug --no-restore — первоначальные сборки 0 warnings/errors.
- scripts/Test-Stage06.ps1 — 92/92, затем 95/95, без пропусков; все прежние 66 сохранены.
- dotnet run --project src/SecureFileManager.Wpf --no-build --launch-profile "SecureFileManager (Development)"
  — окно входа появилось; CUA затем увидел защищённую оболочку с реальным личным списком.
  Агент не вводил пароли, не выполнял интерактивный auth/редактирование рабочих документов.
- CUA-клик штатного закрытия: coordinate input geometry is unavailable. Alt+F4 не подтвердил закрытие.
  Принудительного завершения не выполняли. Открытый exe заблокировал последующую обычную
  пересборку apphost: MSB3027/MSB3021 (10 повторов), не ошибка компиляции.
- Финальный код: dotnet build SecureFileManager.sln -c Debug --artifacts-path artifacts/stage06-verify
  — успешно, 0 warnings/errors, без остановки открытого приложения.
- Финальный полный прогон:
  $env:SFM_UI_ARTIFACT_DIRECTORY = Join-Path $PWD 'docs/screenshots/stage06'
  ./scripts/Test-Stage06.ps1 -ArtifactsPath (Join-Path $PWD 'artifacts/stage06-verify')
  — 95 passed, 0 failed, 0 skipped, 41 с; TRX tests/SecureFileManager.Tests/TestResults/stage06.trx.
  SQL fixture и скрипт очистили только собственные новые тестовую БД/run-GUID без следования ссылкам.
- Реальные SQL Save/round-trip/metadata/audit, отказ SQL/аудита, IO после Publish/компенсация,
  foreign IDs/read/save/validate/reload, конфликт/Reload, закрытие Cancel/Save/Discard,
  UTF-8/BOM/CRLF/LF/пустые/битые/большие документы проверены изолированно.
- XXE: XmlReader запрещает DTD, test-only local sentinel открыт FileShare.None,
  ни его текст не выдан, ни loopback TcpListener не получает соединение; внешние ресурсы не разрешены.
- WPF STA: DI/ресурсы/шесть разделов/фильтр Formats/сессии/реальные TextBox bindings/dirty state;
  29 PNG RenderTargetBitmap, compact TXT/JSON/XML и JSON/XML 1024×640 просмотрены глазами.
  Это не проверка системного DPI/клавиатуры/оконных диалогов.
- Read-only рабочая SQL: 2 миграции, Users=2, Files=3, TLS TRUE/NTLM. Хеши/пароли/содержимое не выбирались.
  Вывод только StorageRoot из существующего локального JSON: D:\SecureFileManagerData\Storage.
  Конфигурация, рабочая схема/миграции/корень не менялись, новая рабочая БД не создавалась.

Созданы STAGE06_MANUAL_TEST (22 основных шага + конфликт/общий редактор/регрессия),
STAGE06_REPORT (карта компонентов, 06.01–06.06, матрица 30 сценариев, полный список файлов),
явно тестовые JSON/XML в docs/test-data/stage06, README рендеров, Test-Stage06.ps1.
Изменения безопасности SEC-01/02/03/04/08/09/10 покрыты проверками; ZIP SEC-05/06 не начат.

Итог: Частично по приёмке. 06.02/03/05/06 отмечены по автоматическим доказательствам;
06.01/04 и общий этап остаются открыты до ручной GUI-приёмки. Нативный input ограничен
CUA-геометрией; computer-use skill запрещает автоматизацию пользовательского auth.
Нужно закрыть старый процесс, пересобрать обычный Debug/F5 и пройти STAGE06_MANUAL_TEST.
Full disk/power loss/отдельная file symlink/администратор Windows не выдаются за проверенные.
Этап 07 не начат. Коммита нет; существующие пользовательские файлы/рабочая БД не удалялись.

Финальная проверка документации: git -c core.safecrlf=false diff --check — exit 0,
ошибок whitespace нет (убрана лишняя пустая строка в тестовом файле).
Git diff по Data/миграциям/App composition root пуст; новых пакетов/секретов нет.
Полный перечень собственных изменений: 19 изменённых и 40 созданных файлов,
включая 29 PNG-рендеров; два исходных изменённых файла приёмки 05 не тронуты.

## 02.11.2026 — подтверждение ручной приёмки этапа 06

Пользователь сообщил: «Провел тестирование. файлы сохраняются форматы проверяются»
и поручил отметить ручную проверку пройденной. Подтверждение принято как завершение
ручной приёмки этапа 06. Задачи 06.01 и 06.04 отмечены выполненными; вместе с ранее
закрытыми 06.02/03/05/06 все пункты этапа завершены. Статус этапа 06 — Готово.

Обновлены TASKS.md, STAGE06_MANUAL_TEST.md, STAGE06_REPORT.md и эта запись PROGRESS.md.
Источник результата — сообщение пользователя, не новый GUI-прогон Codex. Отдельный
протокол по каждому шагу/DPI/специальным отказам не предоставлен; прежние технические
ограничения и исторические результаты сохранены. Автоматический результат остаётся
95/95, без ошибок и пропусков; сборка и тесты в этой документационной задаче повторно
не запускались. Проверка изменений: git diff --check.

Код, конфигурация, БД и пользовательские файлы не менялись. Этап 07 не начат,
для него необходимо отдельное разрешение. Git-коммит не создавался.

## 02.11.2026 — этап 07: безопасное ZIP-архивирование и распаковка

Пользователь разрешил только 07.01–07.07. Перед кодом восстановлен контекст по AGENTS,
TECHNICAL_SPEC, DESIGN_SPEC, TASKS, DECISIONS, PROGRESS, архитектуре/persistence,
документации/ручным инструкциям 05–06, ассетам и PNG-концепту. Сверены solution/csproj,
Core/auth/session/access/path, Windows handles/links, CRUD/import/editor, staging/recovery,
SQL/репозитории/аудит, DI/Views/VM/resources и тесты. Git был чистым, HEAD 2410ae0.
Этап 06 принят; специальных native/DPI/power-loss доказательств сверх имеющихся не заявляем.

Реализованы ZIP-команды в том же FileService (partial, IArchiveService — DI alias),
ZipEngine/System.IO.Compression, неизменяемые ZipLimits, OpenBinary/StageGenerated,
IFilePreparation и Tree в прежнем FileJournal. Старые plans совместимы; Data/схема/миграции
не менялись. Workspace/Run/gate/Session/Revision/SQL-owner lock/Files/Operations переиспользованы.
Создание ZIP — Store/NoCompression (не создаёт собственный anti-bomb ratio violation).
Источники повторно проверены по owner IDs/метаданным/ФС, untracked/changed дети не усваиваются,
пересечения выбора дедуплицируются, self-inclusion/overwrite/reparse запрещены.

Preview: bounded EOCD/central/local header preflight до ZipArchive entries allocation,
список/размеры/предупреждения без извлечения. Store/Deflate, без ZIP64/multipart/encryption.
Entry paths strict Windows: no traversal/absolute/drive/UNC/mixed separators/ADS/reserved
names/special links/case-insensitive collisions/file-parent conflicts. 250 entries/output nodes,
25 МиБ/entry, 100 МиБ total, 100:1; actual bytes/declared length/CRC32 потоково, буфер 64 КиБ.
Распаковка готовит дерево в private new и публикует НОВУЮ папку в выбранном личном родителе
одним rename. Без merge/overwrite. Files всего дерева и ZIP_EXTRACTED коммитятся вместе;
ZIP_CREATED/EXTRACTED имеют FILE_TX_* correlation для прежнего ambiguous commit recovery.
Tree rollback сначала проверяет весь состав и версии с удержанием handles; внешнее изменение
сохраняется, не удаляется слепо. Incomplete plan/ошибка cleanup — RecoveryRequired/ручной разбор.

WPF: Extended selection, существующая «Архивировать», открытие ZIP/preview/extract/cancel,
личный folder picker/имя/подтверждения, async status. Archives переиспользует FilesView/VM
в двух вкладках; source/doc editor не дублируется. Сессии очищают состояние/закрывают
ZIP-диалоги/отменяют подготовку, поздний progress не выдаёт ложный Success.

Найденные/исправленные дефекты: собственные pins конфликтовали с DELETE handles при
публикации дерева; глубокий staging превышал обычный Win32 path limit; бинарный поток
после fingerprint требовал Position=0; компактная архивная панель оставляла таблицу без
высоты; пустые строки сообщений занимали место. Добавлены regression assertions.
Проверка границы staging различает компонент .. и допустимое имя ..allowed.txt.
Тестовый dispatcher изолирован от автоматически поставленного production App startup;
при этом используются реальные App.xaml/DI/Views/ResourceDictionary.

Проверки, Windows:
- dotnet --info: SDK 9.0.200. vswhere ManagedDesktop: VS 2022 17.13.35818.85. MSSQL$SQLEXPRESS Running.
- dotnet restore SecureFileManager.sln — успешно вне песочницы с Windows SDK/NuGet.
- dotnet build SecureFileManager.sln -c Debug --no-restore — 0 warnings/errors до последнего
  уточнения boundary. Финальный dotnet build SecureFileManager.sln -c Debug
  --artifacts-path artifacts/stage07-verify --no-restore — 0 warnings/errors.
- Первые ZIP-прогоны 127/129 и 128/129 выявили дефекты; они исправлены, тесты не удалены.
- Полный обычный scripts/Test-Stage07.ps1: 140 passed, 0 failed, 0 skipped, 37 с.
- Итоговый полный scripts/Test-Stage07.ps1 -ArtifactsPath <repo>/artifacts/stage07-verify:
  140 passed, 0 failed, 0 skipped, 33 с. TRX tests/SecureFileManager.Tests/TestResults/stage07.trx.
  45 новых ZIP-кейсов + все прежние 95. Точечные прогоны теперь не затирают полный TRX.
- Реальная GUID-тестовая БД и новый run-GUID каждого запуска очищены fixture/скриптом,
  без следования reparse и без удаления старых баз/каталогов/рабочих данных.
- SQL integration: mixed files/folders/empty/deep/round-trip/Files+Operations, access/session,
  traversal/junction, callback cancel, controlled disk-full/IO, missing DB/trigger audit failure,
  CRC/limit cleanup, recovery/ambiguous commit/external replacement/retry/concurrent names.
- Unit: 18 path inputs, 4 collisions, symlink/special attrs, 251 tiny entries, stricter small
  budgets, zero compressed, forged/truncated/encrypted/unsupported headers, actual streaming guards.
- WPF STA: alias identity, multiple selection/disabled single actions, source/ZIP browsers,
  preview denial, destination header/rows, layout min table height, binding trace clean.
  34 PNG (RenderTargetBitmap, не desktop/DPI certification) сохранены в screenshots/stage07;
  compact Archives/ZIP preview/destination просмотрены глазами.
- Read-only рабочая SQL: MigrationCount=2, TLS TRUE, NTLM. Выведен только StorageRoot:
  D:\SecureFileManagerData\Storage; конфигурация не менялась.
- dotnet run --project src/SecureFileManager.Wpf --no-build --launch-profile
  "SecureFileManager (Development)": реальное окно входа; CUA прочёл подтверждённый статус
  подключения к БД. Учётные данные не вводились, рабочие документы не редактировались.
  Проверочный экземпляр оставлен на окне входа; перед F5/сборкой закрыть его.
- git -c core.safecrlf=false diff --check — без ошибок whitespace.

Созданы STAGE07_REPORT (анализ, 07.01–07.07, безопасность, матрица 46 сценариев, все файлы),
STAGE07_MANUAL_TEST (основные 17 шагов, negatives/cancel/регрессия/SQL), тестовые ZIP <1 КиБ
в docs/test-data/stage07 (явный fixture, не seed), README рендеров, Test-Stage07.ps1.
Обновлены TASKS/DECISIONS/FILE_OPERATIONS/STORAGE_ARCHITECTURE.

Итог: Частично по ручной приёмке. 07.02/03/04/05/07 закрыты по автоматическим доказательствам;
07.01/06 остаются открытыми до native GUI/диалогов/прогресса/keyboard/DPI проверки пользователем.
Computer-use skill запрещает автоматизацию пользовательского auth; read-only запуск проверен.
Не заявлены real full disk/power loss/отдельная physical file symlink/защита от администратора.
SQL/NTFS не являются распределённо атомарными. Режим B и новые службы/пакеты не добавлены.
Этап 08 не начат. Git-коммита нет. Рабочая БД/пользовательские данные не удалялись.

## 02.11.2026 — подтверждение ручной приёмки этапа 07

Пользователь сообщил, что вручную проверил функции архива и ошибок не обнаружил,
и поручил отметить, что всё в порядке. Подтверждение принято как завершение ручной
приёмки этапа 07. Задачи 07.01 и 07.06 отмечены выполненными; все пункты 07.01–07.07
закрыты, общий статус этапа 07 — Готово.

Обновлены TASKS.md, STAGE07_REPORT.md, STAGE07_MANUAL_TEST.md и эта запись PROGRESS.md.
Источник результата — сообщение пользователя, не новый GUI-прогон Codex. Отдельного
протокола каждого шага/DPI/специальных отказов нет; прежние технические ограничения
сохранены. Последние автоматические результаты — 140/140, без ошибок и пропусков.
Сборка и тесты для этой документационной задачи повторно не запускались.
Проверка изменений документации: git diff --check.

Код, конфигурация, БД и пользовательские файлы не менялись. По прямому указанию
пользователя этап 08 не начат, Git-коммит не создавался.

## 03.11.2026 — этап 08: журнал и сквозная безопасность

Новое задание пользователя из вложения разрешило этап 08; запрет относится к 09.
HEAD перед работой 7c10f83 «7 etap», дерево чистое. Прочитаны актуальные AGENTS/specs/tasks,
решения/progress/persistence/storage architecture, файловые/auth/editor/ZIP отчёты и ручные
инструкции 04–07, assets; открыт PNG-концепт. Сверен код четырёх проектов, текущие миграции,
DI/репозитории/session/KDF/lockout/path-policy/native IO/staging/recovery/Views/VM/tests.
Презентаций преподавателя ppt/pptx/pdf в репозитории не найдено; их изучение не заявляется.

Реализован IJournalService/JournalService без UI UserId, серверная QueryAsync прежнего
OperationRepository: owner/type/result/date/search, enum/bool sort, 50 UI rows, HasMore,
страницы. System UserId=NULL скрыты; active User/Revision проверяются, late results
отбрасываются. Исходный Details не передаётся UI: только известные безопасные коды,
локализованные описания; UTC хранится прежним, UI local dates/time. История удалённых Files
остаётся. JournalView/VM теперь рабочие, с filters/search/paging/loading/empty/error.

AuditHealth/AuditProbe/AuditStatusViewModel — один реальный status для шапки/журнала/настроек:
Unknown/Checking/Active/Unavailable. SELECT+INSERT+rollback не оставляет fake rows;
feedback от реального identity/file/audit commit/failure, freshness 2 минуты, UI timer15 с
без SQL polling. Failure latch до успешной новой пробы, version против запоздалого результата.
IStorageDiagnostics — alias того же FileService, не второй IO-сервис. Неразрушающий bounded
обход/SQL до 10000 объектов: untracked/missing/changed/soft-delete remnants, pending staging
и SQL correlation, audit mismatch. Неисправления явно отображаются, неизвестное не удаляется/
не присваивается. Неполный обход помечается; без hash baseline одинаковые size/mtime не
считаются доказательством неизменности. Старые Synchronize/Recover сохранены.

Минимальные исправления: unknown internal file теперь блокирует команды; LOGIN_LOCKED
уточняет действовавшую блокировку без нового enum; неизвестный подозрительный login payload
не сохраняется в actor snapshot. Во время разработки выявлен/исправлен SQL collation
conflict нового diagnostic query через fixed COLLATE (без миграции). Компактная раскрытая
панель фильтров ограничена прокруткой, чтобы оставалась строка таблицы; добавлена регрессия.

Фактические проверки Windows:
- SDK9.0.200, VS2022 17.13.35818.85, MSSQL$SQLEXPRESS Running.
- dotnet restore SecureFileManager.sln — success (после SDK sandbox denial, тот же запуск
  разрешён вне sandbox). Пакеты/TFM/рабочая конфигурация не менялись.
- Обычный Debug build — success, 0 warnings/errors.
- Финальный dotnet build SecureFileManager.sln -c Debug --artifacts-path artifacts/stage08-verify
  --no-restore -p:RunAnalyzersDuringBuild=true -p:AnalysisLevel=latest — success, 0/0.
- dotnet list SecureFileManager.sln package --vulnerable --include-transitive — ни один из
  четырёх проектов не содержит известных уязвимых пакетов по текущим источникам.
- Первый full 153 passed/3 failed/0 skipped выявил collation; три diagnosis-теста после
  исправления прошли. Затем full159/159; дополнительный UI layout assert выявил малую
  высоту таблицы при expanded filters, исправлено и проверено повторно.
- Финальный scripts/Test-Stage08.ps1 -ArtifactsPath <repo>/artifacts/stage08-verify:
  160 passed, 0 failed, 0 skipped, 34 с. Все прежние140 + новые20 (15 SQL,5 unit), прежний
  WPF test расширен DI/status/journal/paging/filter/date/session/layout asserts.
  TRX: tests/SecureFileManager.Tests/TestResults/stage08.trx (ignored), counters проверены XML.
- Скрипт/fixture очистили только свои новые run-GUID и GUID-БД. Старые/чужие базы/каталоги
  не удалялись. Нет реальных bombs/disk filling/остановки общего SQL Engine.
- Автоматический approval дважды отклонил SQL-прогон, применив прежний запрет на08.
  После отдельного прямого ответа пользователя «Разрешаю запуск тестов этапа 08» тот же
  скрипт прошёл approval и выполнен. Обход отказа не использовался.
- Read-only SELECT рабочей SQL: migrations2, encrypt TRUE, NTLM, Users2/Files9/Operations117
  на момент проверки. StorageRoot прежний D:/SecureFileManagerData/Storage. Ошибка Windows
  credentials в sandbox снята разрешённым повтором того же SELECT. Никаких update/drop/delete.
- Native dotnet run (Development) открыл окно входа; computer-use прочитал статус проверенного
  подключения. Учётные данные не вводились; навык запрещает автоматизацию auth dialogs.
  Проверочное окно оставлено на входе: перед обычной пересборкой закрыть его. Текущий финальный
  код собран в отдельном artifacts/stage08-verify, не перезаписывая открытый exe.
- WPF STA/RenderTargetBitmap: текущие styles/resources/DI, empty и populated journal,
  1024×640/1440×900, bitmap150%, без binding errors. Избранные5PNG+README в screenshots/stage08,
  полные renders в ignored artifacts/stage08-renders. Это не native DPI certification.

Созданы STAGE08_SECURITY_AUDIT (компоненты, 08.01–06, фактическая audit map, угрозы,
матрица60 сценариев, дефекты, ограничения, полный file list) и STAGE08_MANUAL_TEST (21 шаг,
status/outage/diagnostics/negative/regression/SELECT). Обновлены TASKS/DECISIONS/
STORAGE_ARCHITECTURE/FILE_OPERATIONS. Схема/миграции/Data entities не менялись.

08.02/03/05 закрыты по коду и тестам. 08.01/04/06 оставлены открытыми до ручной проверки
авторизованного журнала/настроек/диагностики/keyboard/native DPI. Общий статус Частично.
Ограничения: прямой Windows/SQL admin доступ, TOCTOU внешних изменений после snapshot,
SQL+NTFS не атомарны, physical disk-full/power-loss/отдельный file-symlink не проводились;
есть безопасные doubles, реальные junction/hardlinks и bounded parsers. PRE-03 открыт.
Этап 09 не начат. Git-коммитов нет. Рабочие данные/Operations не очищались.

Финальное уточнение 08.06: empty SQL work/пустой Recover не подтверждает audit write.
ObservedAudit оборачивает тот же репозиторий и отмечает успешный Add; только после commit
и при наличии Add обновляется Active. EmptyFileTransactionDoesNotClaimAuditWriteAvailability
проверяет эту границу. Окончательно160/160 (15 новых SQL +5unit), 34 с, без failed/skip.
Повторный read-only SELECT после тестов: рабочие migrations2/Users2/Files9/Operations117
совпадают с исходной проверкой. Общий статус и ручные ограничения не изменились.

## 03.11.2026 — ручная проверка журнала и исправление обрезанного описания

Пользователь сообщил, что вручную проверил журнал: в целом он выглядит нормально,
но текст «Описание» обрезается даже после горизонтальной прокрутки (приложен скриншот).
Это подтверждение проверки журнала с замечанием, а не безусловная приёмка всего этапа08.

Причина: DataGridTextColumn шириной340 выводил описание одной строкой; продолжение
обрезалось внутри ячейки. Горизонтальная прокрутка уже достигала границы столбца.
Исправлено локально в JournalView: template TextBlock с Wrap и полной ToolTip,
автоматическая высота строки (Double.NaN), минимум48. Общие стили других таблиц,
сервисы, фильтры, аудит и БД не менялись. Длинный текст теперь занимает несколько строк.

Существующий WpfShellTests расширен проверкой реального layout после ScrollIntoView
последнего столбца: описание многострочное, целиком помещается в высоту строки,
ToolTip содержит полное безопасное описание. Первый прогон выявил, что RowHeight="Auto"
не поддерживается Double-конвертером DataGrid; исправлено на NaN. Финальный Debug
build в artifacts/stage08-verify и точечный WPF test: 1 passed, 0 failed, 0 skipped,
без предупреждений компиляции; 4с. SQL-тесты повторно не запускались для XAML-изменения.
Рендеры — ignored artifacts/stage08-description-renders. Native повтор нового исправления
ожидается после пересборки. Общий этап08 остаётся Частично, этап09 не начат, коммита нет.

## 03.11.2026 — подтверждение ручной приёмки этапа 08

После исправления обрезанного описания пользователь сообщил: «вроде проблем не заметил.
отметь что все в порядке». Подтверждение принято как завершение ручной приёмки этапа 08.
Все 08.01–08.06 закрыты, общий статус — Готово. Предыдущие записи об открытой приёмке
описывают состояние до этого подтверждения и сохранены как история.

Источник результата — сообщение пользователя; новый GUI/SQL-прогон Codex не выполнялся.
Отдельный протокол каждого сценария, native DPI и специальных отказов не предоставлен;
прежние технические ограничения не отменяются. Полный автоматический прогон160/160
проведён до последнего XAML-исправления, после него затронутый WPF-тест1/1; эти результаты
не подменяются новым полным прогоном. В этой задаче изменена только документация,
сборка/тесты повторно не запускались. Проверка: git diff --check.

Этап 09 не начат. Git-коммит не создавался. Код, конфигурация, БД и пользовательские
файлы в этой документационной задаче не менялись.

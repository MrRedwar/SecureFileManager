# Журнал фактического прогресса

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

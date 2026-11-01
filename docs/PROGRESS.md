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

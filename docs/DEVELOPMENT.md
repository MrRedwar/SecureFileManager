# Разработка Secure File Manager — этапы 01–02

Windows, VS 2022 17.13.1 (desktop .NET workload), SDK 9.0.200.
Wpf/Tests: net9.0-windows; Core/Data: net9.0. SDK закреплён global.json.
В Visual Studio откройте SecureFileManager.sln, стартовый проект SecureFileManager.Wpf.

```powershell
dotnet tool restore
dotnet restore SecureFileManager.sln
dotnet build SecureFileManager.sln -c Debug --no-restore
dotnet test SecureFileManager.sln -c Debug --no-build
dotnet run --project src/SecureFileManager.Wpf -c Debug --no-build
```

Без отдельного разрешающего флага SQL-тесты явно пропускаются. Полная проверка с БД:

```powershell
$env:SFM_ENVIRONMENT = 'Development'
$env:SFM_CONFIG_DIRECTORY = (Resolve-Path src/SecureFileManager.Wpf).Path
$env:SFM_RUN_SQL_TESTS = '1'
dotnet test SecureFileManager.sln -c Debug --no-build
```

WPF пока предоставляет только навигацию по шести экранам-заготовкам. Persistence
зарегистрирован через DI; при старте окно не запускает запросы и миграции,
не выполняет вход, не предоставляет файловых действий и просмотра журнала.
Подключение и схема проверены отдельно реальными SQL-тестами и sqlcmd.

## Конфигурация подключения

По умолчанию SFM_ENVIRONMENT=Production. SFM_CONNECTION_STRING задаёт подключение
из переменной среды (не выводится в диагностику). При отсутствии переменной читается
appsettings.json из SFM_CONFIG_DIRECTORY, либо из каталога приложения.
Если файла нет, используется Windows Authentication к локальному SQL Express,
SecureFileManagerDB, Encrypt=True;TrustServerCertificate=False. Это безопасный
fallback для регистрации DI, но он не обходит недоверенный сертификат сервера.

Только при явном SFM_ENVIRONMENT=Development читается appsettings.Development.json.
Для текущей машины он находится в src/SecureFileManager.Wpf, игнорируется Git
и исключён из MSBuild items (в build/publish не копируется). На новой машине:
скопируйте appsettings.example.json в appsettings.Development.json и измените
TrustServerCertificate на True исключительно для локального Development.
Старый отслеживаемый appsettings.Development.local.json удалён из дерева проекта.
Общий пример сохраняет Encrypt=True;TrustServerCertificate=False.

Конфигурация Data отвергает Encrypt=False; доверие сертификату вне Development,
на удалённом сервере или другом экземпляре, а также выбор системной БД.
Допустимые локальные адреса исключения: .\SQLEXPRESS, (local)\SQLEXPRESS,
localhost\SQLEXPRESS и <имя-компьютера>\SQLEXPRESS. В production необходим
доверенный сертификат и TrustServerCertificate=False. Секреты только в пользовательской
среде; не добавляйте логины/пароли или локальные JSON в Git.

Проверка локального соединения (без изменения данных):

```powershell
sqlcmd -S '.\SQLEXPRESS' -d master -E -N -C -l 8 -b -W -Q "SELECT encrypt_option,auth_scheme FROM sys.dm_exec_connections WHERE session_id=@@SPID;"
```

-N сохраняет TLS; -C — исключительно разрешённое локальное доверие сертификату;
-E — Windows Authentication. Ожидается TRUE. В ограниченном процессе Windows
Authentication может быть недоступна; используйте обычный PowerShell пользователя.

Схема, репозитории, миграции, тесты и запросы SSMS описаны в [PERSISTENCE.md](PERSISTENCE.md).
Core не зависит от WPF/EF; Data не зависит от WPF. Новых шрифтов и изображений нет.
Финальная тема/иконки/DPI относятся к этапу 03; регистрация и вход — к этапу 04.

# Локальная разработка — этап 01

## Среда и запуск

Windows, Visual Studio 2022 17.13.1 с workload «Разработка классических приложений .NET»,
.NET SDK 9.0.200. `global.json` фиксирует ветку SDK 9.0.2xx с обновлением patch.
Wpf и Tests используют `net9.0-windows`; Core/Data — `net9.0`.
Откройте `SecureFileManager.sln`, выберите SecureFileManager.Wpf стартовым проектом.

Из корня репозитория:

```powershell
dotnet restore SecureFileManager.sln
dotnet build SecureFileManager.sln -c Debug --no-restore
dotnet test SecureFileManager.sln -c Debug --no-build
dotnet run --project src/SecureFileManager.Wpf -c Debug --no-build
```

Доступны шесть экранов-заготовок, переключаемых выбором раздела в списке
(мышью или стрелками после перехода фокуса Tab). Действий с пользовательскими
файлами, входа, журнала и подключения к БД из приложения пока нет.
SQL-проверка ниже выполняется отдельно от приложения. Не воспринимать каркас
как готовый защищённый файловый менеджер.

## Подключение к локальному SQL Server Express

Локальный Engine: `.\SQLEXPRESS`. Используется Windows Authentication, пароль не нужен.
`src/SecureFileManager.Wpf/appsettings.example.json` содержит общий пример с
`Encrypt=True;TrustServerCertificate=False`.

Только для локальной разработки пользователь разрешил исключение:
`Encrypt=True;TrustServerCertificate=True`. Оно хранится в
`src/SecureFileManager.Wpf/appsettings.Development.local.json`, исключённом из Git
и из MSBuild items Wpf. На новой машине создайте этот локальный файл из общего
примера и измените только TrustServerCertificate на True. Не переносите его в
производственную конфигурацию. Не добавляйте пароли в исходники/JSON под Git.

На этапе 01 оба файла — заготовки конфигурации для этапа 02, приложение их ещё
не загружает. Автоприменения миграций или создания SecureFileManagerDB нет.
В этапе 02 локальное исключение должно загружаться только при явном выборе
Development; production сохраняет Encrypt=True и TrustServerCertificate=False.

Read-only проверка через установленный sqlcmd (ODBC 17):

```powershell
sqlcmd -S '.\SQLEXPRESS' -d master -E -N -C -l 8 -b -W -Q "SET NOCOUNT ON; SELECT CONVERT(nvarchar(40),SERVERPROPERTY('ProductVersion')) AS ProductVersion; SELECT encrypt_option, auth_scheme FROM sys.dm_exec_connections WHERE session_id = @@SPID;"
```

`-N` запрашивает шифрование TLS, `-C` соответствует локальному доверию сертификату,
`-E` использует Windows Authentication. Эта команда с `-C` предназначена исключительно
для локального экземпляра разработки. Ожидается encrypt_option = TRUE.
В ограниченном процессе Windows Authentication может быть недоступна; тогда проверку
нужно выполнить из обычного PowerShell пользователя. Успех соединения не подтверждает
наличие схемы приложения — её создание относится к этапу 02.

Для эксплуатации нужен доверенный сертификат SQL Server и проверка его имени/цепочки:
[настройка сертификата Microsoft](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/configure-sql-server-encryption).

## Архитектура и зависимости

- Wpf → Core; Wpf → Data только для будущего composition root. DI сейчас собирает окно и VM.
- Data → Core. Подготовлены пустые каталоги Repositories/Migrations/DependencyInjection.
- Core без EF/WPF/SQL; подготовлены Entities/Interfaces/Services/Security/Results.
- Tests → Wpf для тестирования VM без создания окон. Тесты не обращаются к SQL/Storage.

EF Core SqlServer 9.0.22 добавлен только в Data; транзитивные EF Core/Relational также
9.0.22. Он нужен для SQL Server persistence на следующем этапе, сейчас не выполняет запросов.
Microsoft.Extensions.DependencyInjection 9.0.22 собирает и освобождает контейнер приложения.
Оба пакета стабильные и совместимы с выбранными TFM; preview и дополнительные MVVM/ORM
фреймворки не используются. xUnit 2.9.2, runner VS 2.8.2 и Test SDK 17.12.0 — только Tests.

Каталоги с `.gitkeep` обозначают структуру будущих модулей, не готовую реализацию.
Использованы только системные шрифты и XAML-кисти, новых изображений/шрифтов нет.
Стартовая палитра соответствует концепту; финальные орнаменты, иконки, стили контролов
и проверка DPI относятся к этапу 03. Скриншот концепта в приложение не встроен.

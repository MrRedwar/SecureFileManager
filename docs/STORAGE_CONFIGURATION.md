# Настройка физического корня Storage

Актуально: 02.11.2026. Перенос выполнен пользователем; приложение ничего не перемещает.

## Настройки и приоритет

Единый источник JSON — тот же, что используется подключением SQL:
SFM_ENVIRONMENT (Development или Production; по умолчанию Production) и
SFM_CONFIG_DIRECTORY (по умолчанию каталог собранного приложения).

1. Непустой SFM_STORAGE_ROOT — явное переопределение, сохранённое для совместимости.
2. Иначе StorageRoot в appsettings.Development.json для Development либо appsettings.json
   для Production, в указанном каталоге конфигурации.
3. Отсутствие/ошибка значения — ошибка конфигурации. Fallback в AppData удалён.
   Недоступность диска/прав также не выбирает другой корень.

В текущем игнорируемом src/SecureFileManager.Wpf/appsettings.Development.json задано:

```json
{
  "StorageRoot": "D:\\SecureFileManagerData\\Storage"
}
```

Это фрагмент; существующие ConnectionStrings сохранены. Локальный файл не добавляется
в Git и не копируется в bin/publish. В общей example-конфигурации StorageRoot пустой:
для новой установки его необходимо явно заполнить. Для Production путь задаётся
отдельно администратором, локальный D-путь туда автоматически не переносится.

Перед изменением корня необходимо остановить старый процесс приложения: объект
PersonalStorage получает настройки при составлении DI, без горячего переключения.

## Формирование каталогов

StorageRoot уже обозначает папку Storage. Адаптер добавляет **только**
Users/{User.Id:D}, где Id взят из SQL Server:

```text
D:\SecureFileManagerData\Storage\Users\{UserId}\
```

Не указывать в StorageRoot уже добавленный Users или GUID. Storage/Storage и
Users/Users приложение не добавляет. Существующий GUID-каталог используется повторно,
его файлы не перезаписываются. Для нового пользователя каталог создаёт прежний
PersonalStorage.PrepareAsync через сервис регистрации, в согласовании с SQL-аудитом.

Никакого второго перемещения, удаления старых каталогов, изменения GUID/схемы/ACL нет.
Изменение StorageRoot не меняет владельцев/RelativePath в SQL. В реальной базе
один активный User; соответствующий перенесённый GUID-каталог найден read-only.
Программа проверки не вызывала Prepare для этого пользователя и не читала его файлы.

## Запуск через Visual Studio

Выбрать профиль SecureFileManager (Development), остановить старый процесс, затем F5.
Профиль задаёт Development, SFM_CONFIG_DIRECTORY=. и рабочий каталог проекта.
Параметр StorageRoot читается из существующего локального JSON; проверьте отсутствие
устаревшего SFM_STORAGE_ROOT в окружении отладчика, поскольку override имеет приоритет.

CLI из корня решения использует тот же профиль:

```powershell
dotnet run --project src/SecureFileManager.Wpf --launch-profile "SecureFileManager (Development)"
```

## Прямой запуск собранного exe

Профили VS не применяются при двойном щелчке по exe. Для локального Development-запуска
передать окружение и путь к тому же локальному файлу явно:

```powershell
$env:SFM_ENVIRONMENT = 'Development'
$env:SFM_CONFIG_DIRECTORY = (Resolve-Path src/SecureFileManager.Wpf).Path
& '.\src\SecureFileManager.Wpf\bin\Debug\net9.0-windows\SecureFileManager.Wpf.exe'
```

Без Development применяются Production-настройки appsettings.json рядом с exe
или в явно заданном SFM_CONFIG_DIRECTORY. Нет автоматического поиска локального
конфига разработчика, ослабления TLS или перехода к старому AppData-хранилищу.

## Проверки и безопасность

Выполнены restore, Debug-build (0 warnings/errors), 47/47 тестов, 0 skipped.
Новые проверки: выбор файла окружения/override, обязательность StorageRoot,
некорректный JSON/пустое значение, повторное использование каталога и сохранность
test-only файла, разные GUID, недоступный диск, запрет traversal/absolute пути.
Прежние SQL/ownership/session/audit/reparse/junction/hard-link проверки проходят.

Реальная регистрация в тестах выполнена на D: в
D:\SecureFileManagerData\IsolatedTests, отдельно от рабочего Storage.
Новая БД не создавалась: использована ранее созданная тестовая БД, только её явно
разрешённое имя передаётся через SFM_EXISTING_TEST_DATABASE. Проверка наличия и актуальности
схемы выполняется без миграции. Без этой переменной сохраняется прежний режим fixture
с новой уникальной тестовой БД, поэтому при запрете создания БД её обязательно указывать.
SFM_STORAGE_TEST_ROOT задаёт исключительно базу тестовых директорий, не конфигурацию приложения.
Тестовые данные оставляются для анализа и не удаляются автоматически.

Read-only проверка собранного адаптера с настоящим Development JSON подтвердила новый
StorageRoot, наличие существующего пользователя и разрешение пути его каталога без
создания директории; User.Id и время создания каталога сохранились. Старый Storage в
AppData отсутствовал до/после, Storage внутри Git-репозитория отсутствует.

WPF-регрессия теперь измеряет клиентскую разметку без открытия скрытых native-окон;
это не доказательство реального DPI/системного resize. Такое уточнение устраняет зависание
desktop-сессии и не меняет XAML приложения. Прежние проверки ресурсов/привязок/экранов
сохранены. Реальные системные кнопки/DPI остаются ручной проверкой.

## Что проверить вручную

1. Перезапустить приложение в VS с Development-профилем; войти существующим пользователем.
   Не регистрировать его заново. Главное окно должно открыться с тем же логином.
2. В Проводнике убедиться, что используется прежний GUID-каталог на D:, файлы на месте;
   новый Storage в AppData не появился. Полноценный список файлов в UI относится к этапу 05.
3. При необходимости повторить вход при прямом запуске exe с окружением выше.
4. Не отключать реальный D и не менять ACL для проверки отказа. Недоступный накопитель,
   ошибки каталога и отказ аудита проверены только на изолированных test-only объектах.


## Файлы, изменённые при перенастройке

- src/SecureFileManager.Wpf/App.xaml.cs
- src/SecureFileManager.Wpf/Infrastructure/FileSystem/PersonalStorage.cs
- src/SecureFileManager.Wpf/Infrastructure/FileSystem/StorageConfiguration.cs (новый)
- src/SecureFileManager.Wpf/appsettings.example.json
- src/SecureFileManager.Wpf/appsettings.Development.json (локальный, игнорируется Git)
- tests/SecureFileManager.Tests/StorageConfigurationTests.cs
- tests/SecureFileManager.Tests/SqlDatabaseFixture.cs
- tests/SecureFileManager.Tests/SecurityIntegrationTests.cs
- tests/SecureFileManager.Tests/PersistenceTests.cs
- tests/SecureFileManager.Tests/WpfShellTests.cs
- TASKS.md
- docs/STORAGE_ARCHITECTURE.md
- docs/DECISIONS.md
- docs/PROGRESS.md
- docs/STAGE04_MANUAL_TEST.md
- docs/STORAGE_CONFIGURATION.md (новый)

Файлы профиля запуска и csproj, уже изменённые предыдущим исправлением, сохранены.

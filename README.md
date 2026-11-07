# Secure File Manager

**Этап11.03:** во внешней папке доступны **«Действия → Создать файл / Создать папку /
Копировать в… / Переместить в… / Переименовать»**. Копирование папок сохраняет вложенность
и пустые каталоги; существующие объекты не перезаписываются. Move — в пределах одного тома;
между томами доступно копирование. Новая миграция не нужна.185/185 тестов, Debug/Release
PASS; native-приёмка открыта. Лимиты и восстановление: [EXTERNAL_OPERATIONS](docs/EXTERNAL_OPERATIONS.md).

**Этап11.02:** добавлен редактор внешних TXT/JSON/XML — «Диски → Файлы компьютера →
Открыть», Ctrl+S, валидация и защита от конфликтов. На этой машине 07.11.2026 миграция
`20261107101211_AddExternalDocuments` применена к рабочей БД после backup/VERIFYONLY,
сохранность прежних данных и реальный EF-путь проверены: [отчёт](docs/DB_UPDATE_1102.md).
Для других установок добавочную миграцию применяют после резервной копии БД. Инструкция, схема и ограничения
in-place сохранения/восстановления: [EXTERNAL_EDITOR](docs/EXTERNAL_EDITOR.md).
Параграф11.01 ниже описывает предыдущий снимок read-only браузера.

**Расширение от 07.11.2026:** «Диски → Файлы компьютера» показывает общие внешние
локальные файлы/папки по правам Windows всем вошедшим app-пользователям.
«Открыть диск», адресная строка, Enter/двойной щелчок по папке, вверх/refresh и поиск
доступны в новом браузере. Сейчас это просмотр сведений: редактирование/копирование/
перемещение/переименование/удаление внешних файлов — следующие задачи11.02–11.04.
Личный Storage открывается через «Мои файлы». См. [COMPUTER_BROWSER](docs/COMPUTER_BROWSER.md).
Результаты160/160 ниже относятся к снимку MVP от03.11, не к новой версии браузера.

Учебное настольное приложение для безопасного управления личными файлами в Windows.
Текущая C#-версия: WPF/MVVM, SQL Server, выделенные пользовательские каталоги.
Итоговая проверка от 03.11.2026: Debug и Release собраны, 160/160 автоматических тестов
пройдены. **Требуется финальная ручная приёмка**, см. [TEST_REPORT](docs/TEST_REPORT.md).

## Возможности

- Регистрация, вход, выход, ограничение неудачных попыток входа.
- Личное хранилище `StorageRoot/Users/<User.Id>/`, контроль владельца в сервисах.
- Просмотр характеристик дисков без файловых операций над произвольными дисками.
- Создание папок и TXT/JSON/XML, чтение, сохранение, удаление с подтверждением;
  удаление только пустых папок, поиск и сортировка в текущей папке.
- Импорт одного явно выбранного локального файла копированием, до 100 МиБ.
- Общий редактор строгой UTF-8 до 10 МиБ, проверка JSON/XML, защита несохранённых изменений.
- Создание ZIP, просмотр состава, распаковка в новую личную папку.
- Собственный журнал с фильтрами, поиском, страницами; состояние обязательного аудита.
- Явная сверка метаданных, восстановление собственных незавершённых операций,
  неразрушающая диагностика расхождений.

## Технологии и среда

| Компонент | Фактическая версия |
|---|---|
| C# / .NET SDK | C# 13 по умолчанию SDK; SDK 9.0.200 закреплён в global.json |
| Core / Data | net9.0 |
| WPF / Tests | net9.0-windows |
| EF Core SqlServer / Design, Identity.Core, DI | 9.0.22 |
| SQL в проверенной среде | SQL Server 2019 Express, локальный SQLEXPRESS |
| Тесты | xUnit 2.9.2, runner 2.8.2, Microsoft.NET.Test.Sdk 17.12.0 |
| IDE в проверенной среде | Visual Studio Community 2022 17.13.1, .NET desktop development |

Текущий TFM оставлен по принятому решению; обновление SDK/TFM — отдельная задача.
Подробности и открытый вопрос согласования WPF/SQL Server с преподавателем:
[DECISIONS](docs/DECISIONS.md). Наличие SSMS само по себе не означает наличие SQL Engine.

## Требования для запуска

Windows с .NET Desktop Runtime 9 x64 для опубликованной framework-dependent сборки.
Для сборки — SDK из global.json и Visual Studio с поддержкой .NET 9/WPF либо CLI.
Нужен работающий SQL Server Engine (проверен Express 2019); другие версии отдельно
не сертифицированы. Нужны права Windows на создание/чтение/изменение файлов выбранного
локального StorageRoot и SQL-права приложения на Users/Files/Operations.
Создание БД и миграции требуют дополнительных SQL DDL-прав при установке.

## Установка из исходников

1. Получите копию репозитория (`git clone <адрес-репозитория>`) и перейдите в его корень.
2. Откройте `SecureFileManager.sln`, стартовый проект — `SecureFileManager.Wpf`.
3. Установите/запустите SQL Server Express Engine; уточните имя экземпляра.
4. Скопируйте `src/SecureFileManager.Wpf/appsettings.example.json` в локальный
   `appsettings.Development.json` в той же папке. Этот файл игнорируется Git.
   Задайте StorageRoot и имя своей БД. Например, конфигурация для сервера с доверенным сертификатом:

   ```json
   {
     "StorageRoot": "C:\\SecureFileManagerData\\Storage",
     "ConnectionStrings": {
       "SecureFileManager": "Server=.\\SQLEXPRESS;Database=SecureFileManagerDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=False"
     }
   }
   ```

   Выберите каталог, доступный вашей Windows-учётной записи; путь в примере не обязателен.
   При локальном самоподписанном сертификате разрешено **явно** заменить
   `TrustServerCertificate=False` на `True` только в Development для локального SQLEXPRESS.
   Это исключение пропускает проверку подлинности сертификата; TLS остаётся включён.
   В Production нужен доверенный сертификат и `TrustServerCertificate=False`.
5. В PowerShell из корня подготовьте инструменты и окружение:

   ```powershell
   dotnet --info
   dotnet tool restore
   dotnet restore SecureFileManager.sln
   $env:SFM_ENVIRONMENT = 'Development'
   $env:SFM_CONFIG_DIRECTORY = (Resolve-Path src/SecureFileManager.Wpf).Path
   dotnet ef migrations list --project src/SecureFileManager.Data --startup-project src/SecureFileManager.Data
   dotnet ef migrations has-pending-model-changes --project src/SecureFileManager.Data --startup-project src/SecureFileManager.Data
   ```

6. Для **новой установки**, убедившись, что имя БД выбрано правильно, примените миграции:

   ```powershell
   dotnet ef database update --project src/SecureFileManager.Data --startup-project src/SecureFileManager.Data
   ```

   Существующую БД сначала резервируют и сверяют с `__EFMigrationsHistory`.
   Актуальны `20261101094955_InitialCreate` и `20261101122237_AddLoginFailureWindow`.
   Приложение не выполняет миграции при запуске. Не пересоздавайте рабочую БД и Storage.
7. StorageRoot должен быть абсолютным локальным каталогом, не корнем диска/UNC/ссылкой.
   Приложение создаёт `Users/<GUID>` при регистрации. Не подменяйте GUID и не переносите
   существующие каталоги независимо от БД. Локальный путь разработчика
   `D:\SecureFileManagerData\Storage` — только настройка этой машины; диск D не требуется.
8. Соберите и запустите:

   ```powershell
   dotnet build SecureFileManager.sln -c Debug
   dotnet run --project src/SecureFileManager.Wpf --launch-profile 'SecureFileManager (Development)'
   ```

9. Проверьте статус подключения в окне входа. Если БД отсутствует/недоступна, исправьте
   установку и нажмите повтор проверки. Вход/файловые операции не получают обхода аудита.
10. На новой установке откройте регистрацию: логин 3–50 символов, пароль 10–128,
    подтверждение пароля. Затем войдите. Для существующего аккаунта используйте вход.

## Конфигурация и секреты

`SFM_ENVIRONMENT`: Production по умолчанию, либо явный Development.
`SFM_CONFIG_DIRECTORY`: каталог JSON, по умолчанию каталог exe; используйте абсолютный путь
для независимости от текущей рабочей папки. Development читает `appsettings.Development.json`,
Production — `appsettings.json`. `SFM_CONNECTION_STRING` имеет приоритет для SQL,
непустой `SFM_STORAGE_ROOT` — для хранилища. Удалите устаревшие overrides перед диагностикой.
StorageRoot обязателен, fallback в AppData отсутствует. Для SQL при отсутствии настройки
есть безопасный локальный fallback с обязательной проверкой сертификата.

Не коммитьте локальные настройки, пароли, токены или SQL Authentication-секреты.
Предпочтительна Windows Authentication. Production JSON, если содержит секреты,
храните вне репозитория с ограниченными Windows ACL. Development-конфиг не копируется
сборкой/публикацией; для прямого exe окружение и конфигурация задаются отдельно.

## Использование

В «Диски» доступны только сведения. В «Мои файлы» создайте папку, откройте её,
создайте файл с расширением `.txt`, `.json` или `.xml`. Редактор сохраняет по Ctrl+S,
проверяет JSON/XML; закрытие с изменениями требует решения. «Импортировать» создаёт
копию выбранного локального файла. «Архивировать» работает с выбранными Ctrl/Shift
объектами; ZIP нельзя создавать внутри выбранного исходного каталога.
«Открыть архив» показывает состав; «Распаковать» требует новую папку назначения.
«Журнал» показывает только события текущего пользователя. При RecoveryRequired
восстановите доступ к прежним SQL/диску и используйте «Восстановить»;
не удаляйте `.sfm-internal` вручную.

## Проверки и публикация

Обычный `dotnet test SecureFileManager.sln -c Debug` без разрешающего флага пропускает
SQL-тесты. Для полного безопасного прогона используйте существующий скрипт:

```powershell
./scripts/Test-Stage08.ps1 -ArtifactsPath "$PWD/artifacts/final-verify"
```

Он создаёт новую GUID-БД на локальном SQLEXPRESS и временный run-GUID, затем очищает
только собственные объекты. Нужны права создания/удаления тестовой БД. Скрипт использует
фиксированное имя `stage08.trx`: предварительно сохраните прежний TRX, если он нужен.
Полная инвентаризация и результаты: [TEST_REPORT](docs/TEST_REPORT.md),
[TEST_INVENTORY](docs/TEST_INVENTORY.md).

Проверенная команда публикации из корня:

```powershell
dotnet publish src/SecureFileManager.Wpf/SecureFileManager.Wpf.csproj -c Release --self-contained false -o artifacts/publish/final-win
```

Бинарные файлы остаются в ignored artifacts. Перенесите **всю** выходную папку,
не только exe. Зависимости SqlClient и runtime assets необходимы.
Настройте внешний конфиг и .NET Desktop Runtime 9 x64; запуск:

```powershell
$env:SFM_ENVIRONMENT = 'Production'
$env:SFM_CONFIG_DIRECTORY = 'C:\SfmConfig' # собственный каталог с appsettings.json
& '.\artifacts\publish\final-win\SecureFileManager.Wpf.exe'
```

Публикация framework-dependent, без single-file/trimming. PNG и XAML включены как
WPF-ресурсы. В опубликованный каталог не включаются БД, demo, Storage и локальный
Development JSON. Перед передачей проверьте состав папки и уберите свои локальные настройки.
Чистая установка ещё NOT RUN: процедура в [итоговой ручной проверке](docs/STAGE09_10_MANUAL_TEST.md).

## Ограничения

Приложение обеспечивает прикладной контроль доступа; пользователи приложения могут
работать под одним Windows token. Нет шифрования файлов на диске и защиты от владельца
процесса, Windows/SQL-администратора или прямого доступа к Storage/БД.
SQL и NTFS не образуют атомарную распределённую транзакцию: используются staging,
компенсация и проверяемое восстановление. Полная устойчивость к отключению питания не заявляется.
Создаваемый ZIP — Store; чтение — Store/Deflate, без ZIP64, encryption, multipart.
Лимиты: 250 записей/узлов, 25 МиБ на запись, 100 МиБ суммарно, ratio 100:1.
Распаковка без merge/overwrite; произвольные ACL/timestamps не восстанавливаются.
Полные ограничения, ручные проверки и формальные риски — в TEST_REPORT.

## Материалы проекта

[Архитектура](docs/ARCHITECTURE.md) · [Сценарий защиты](docs/DEMO_SCENARIO.md) ·
[Демонстрационные файлы](demo/README.md) · [Скриншоты](docs/SCREENSHOT_CHECKLIST.md) ·
[Основа отчёта](docs/FINAL_REPORT_DRAFT.md). Перенос на Python в текущую работу не входит.

# Этап 06 — отчёт TXT / JSON / XML

Дата: 02.11.2026. **Статус: Готово — ручная проверка подтверждена пользователем.**
Пользователь сообщил: «Провел тестирование. файлы сохраняются форматы проверяются»
и явно поручил отметить ручную проверку пройденной. Это источник завершения приёмки 06.01/06.04;
новый ручной прогон Codex и отдельное подтверждение каждого специального сценария не заявляются.
Этап 07 не начат, Git-коммит не создан. Рабочие документы/база не удалялись и не пересоздавались.

## 1. Восстановленный контекст и карта компонентов

Изучены AGENTS, TECHNICAL_SPEC, DESIGN_SPEC, TASKS, DECISIONS, PROGRESS,
STORAGE_ARCHITECTURE, PERSISTENCE, FILE_OPERATIONS, STORAGE_CONFIGURATION,
STAGE05_MANUAL_TEST, отчёты предыдущих этапов и ASSETS_README. Открыт PNG-концепт.
Проверены solution/четыре csproj, модели/контракты, auth/session/access/path-policy,
файловый адаптер, Win32 handles, staging/recovery, репозитории/SQL transaction store,
DI/навигация/Views/VM/styles, конфигурация и тесты. Git log: 01893f4 (05), d8d54c5,
fbd04ee (04), 60aa75b (03.5), dd3f24c (03), c360cc9 (02).

На старте уже изменены TASKS, PROGRESS, STAGE05_MANUAL_TEST и STAGE05_REPORT:
это пользовательская фиксация приёмки 05, она сохранена. Последние два файла в этом этапе не редактировались.

| Уже существовало в 01–05 | Использование в 06 |
|---|---|
| Solution, Core/Data/Wpf/Tests, net9.0/net9.0-windows, Microsoft DI | Сохранены проекты/TFM/пакеты и направления зависимостей |
| User/FileRecord/Operation, три репозиторных интерфейса, EF mapping и две миграции | Переиспользованы без изменения SQL-схемы |
| PasswordSecurity, RegistrationService, AuthenticationService, UserSession/Revision, lockout | Переиспользованы, код auth не менялся |
| PersonalFileAccessPolicy, RelativePathSyntax, PersonalStorage, StorageConfiguration | Прежняя owner/path/ссылочная граница A; конфигурация корня сохранена |
| FileService, IPhysicalFileStorage/IFileWorkspace, WindowsFileStorage/SafeNativeFile | Расширены поддерживаемые текстовые форматы, запись выполняется только этим путём |
| FileTransactionStore, Files/Operations repositories, SecurityAudit | Прежние SQL-active/Serializable/owner-lock, метаданные + обязательный аудит |
| Private .sfm-internal, Stage/Publish/Settle, резервный old, SHA-256/identity/mtime/size, RowVersion | Прежний контроль версии, компенсация и восстановление |
| TextEditorViewModel/TextEditorWindow, Ctrl+S, dirty flag, Yes/No/Cancel, закрытие при смене сессии | Расширен один редактор TXT/JSON/XML; подтверждения и изоляция сохранены |
| FilesView/FilesViewModel, импорт/создание/удаление/сверка, WindowsFileDialogs | Открывают все три формата; браузер переиспользуется в JSON/XML |
| FormatsView/FormatsViewModel как disabled-заготовка | Теперь фильтр личных каталогов и JSON/XML, документ хранится только в общем редакторе |
| ResourceDictionary/PNG-ассеты, навигация, DriveInfo, 66 тестов | Сохранены и прошли регрессию |

Созданы с нуля только чистый DocumentValidator/DocumentValidation/DocumentFormat,
два файла тестов, скрипт запуска проверок, документация и явно тестовые примеры/рендеры.
Новых файловых сервисов, ORM, контейнеров, пакетов, миграций, мест хранения и режима B нет.

Обнаруженные проблемы и исправления:

1. Существующая TXT-запись удаляла UTF-8 BOM. Stage теперь определяет BOM по уже проверенному исходному handle и сохраняет его.
2. После сохранения редактор мог принять токен повторно прочитанного внешнего изменения. Теперь принимает его только при совпадении текста; иначе предлагает Reload.
3. OpenEditor создавал второе окно одного FileId. Теперь активирует существующее.
4. Ошибка записи failure-аудита ранее игнорировалась. Теперь возвращается DatabaseUnavailable, запись в журнал не объявляется успешной.
5. У редактора тёмный наследуемый фон конфликтовал с тёмным текстом. Теперь используются существующие песочные ресурсы.

Изменения совместимости ограничены IFileService.ValidateAsync/ReloadAsync, optional
Validation в DTO/result и виртуальным UpdateSession для корректной передачи сессии
в браузер Formats. Единственный тестовый IFileService-адаптер обновлён; новая файловая реализация не создана.

## 2. Выполнение 06.01–06.06

| Пункт | Реализация | Приёмка |
|---|---|---|
| 06.01 | Существующий WPF-редактор: UTF-8, имя/формат, `*` и текст dirty state, сохранение/Ctrl+S, подтверждение закрытия, ошибки, сессия, конфликт и явное перечитывание | VM/STA/SQL пройдены; ручная приёмка подтверждена пользователем 02.11.2026 |
| 06.02 | JsonDocument, depth 64, строгий синтаксис, подробная безопасная ошибка со строкой/позицией, создание `{}`, исправление и round-trip | Автотесты пройдены |
| 06.03 | XmlReader с Prohibit/null resolver, лимитами символов/глубины/узлов/атрибутов, запрет DTD/внешних сущностей/другой encoding declaration | Автотесты, включая локальный sentinel и отсутствие loopback connection, пройдены |
| 06.04 | Оба раздела используют FilesViewModel → IFileService.ReadAsync → WindowsFileDialogs → один TextEditorWindow. Formats не содержит текста документа | Фильтр/сессия/привязки пройдены; ручная приёмка подтверждена пользователем 02.11.2026 |
| 06.05 | До чтения проверяется handle.Length; фактический поток ограничен 10 МиБ; строгая UTF-8, BOM, пустые/битые документы, ограниченный разбор | Автотесты всех трёх форматов пройдены |
| 06.06 | JSON_VALIDATED, XML_VALIDATED, FILE_UPDATED, OPERATION_FAILED, ACCESS_DENIED через прежние Operations/SecurityAudit | Все 95 тестов пройдены, без Skip |

JSON/XML проверяются при открытии, команде «Проверить» и **обязательно в сервисе перед Save**.
Синтаксически битый/пустой JSON/XML можно открыть для исправления с ошибкой в редакторе;
сохранение блокируется. XML с DTD либо объявленной не-UTF-8 кодировкой не выдаётся редактору.
TXT может быть пустым. Другие расширения показывают прежние сведения, не запускаются в ОС.

Ограничения: 10×1024×1024 байт UTF-8 с учётом BOM; JSON depth 64;
XML depth <=64, до 200000 Read-узлов, до 1024 атрибутов на элемент,
MaxCharactersInDocument=10 МиБ, MaxCharactersFromEntities=1024.
DTD запрещён независимо от дополнительного entity-лимита; XmlResolver=null,
внешние схемы/DTD/ресурсы не загружаются, XInclude не исполняется.
Валидатор работает в Task.Run, SQL/IO асинхронны, редактирование на время команды отключается.
Текстовый Undo ограничен 20 действиями. Это ограниченный обычный TextBox, не потоковый редактор больших файлов.

UTF-8 декодируется без замены неправильных байтов. UTF-16/другие кодировки,
NUL/непарные суррогаты отклоняются. BOM исходного UTF-8 сохраняется, новые файлы без BOM.
Сервис сохраняет текст/переводы строк буквально; STA проверяет отсутствие изменения LF при привязке TextBox.
Байт-в-байт сохранность CRLF/LF/BOM проверена на сервисном уровне. Форматирование не реализовано (опционально);
никакой автоматической нормализации JSON/XML нет.

Сохранение: сессия/активный SQL User/owner/path → формат → RowVersion и физическая версия
→ прежний Stage/Publish → Files + Operations в одной SQL-транзакции → Settle → Success.
При ошибке валидации Stage не вызывается; исходные байты/метаданные остаются.
При IO/SQL failure используется прежняя компенсация/recovery. В неоднозначных случаях нет ложного успеха.
Отдельного force overwrite нет. «Перечитать» обновляет метаданные/версию через тот же workspace,
только после подтверждения; при ошибке UI сохраняет старый буфер.

Аудит хранит фиксированные коды, UTC, пользователя и разрешённый FileId/относительный snapshot.
Отказ по чужому ID не раскрывает FileId. Сырые JsonException/XmlException.Message не логируются.
После rollback ошибочной Save проверка/OPERATION_FAILED записываются через прежний IAuditService;
если аудит недоступен, возвращается DatabaseUnavailable. Успех проверки при Save и FILE_TX_SAVE
коммитятся вместе с метаданными. Это по-прежнему не распределённая транзакция SQL/NTFS.

## 3. Проверки и доказательства

Среда: Windows 10, SDK 9.0.200, VS 2022 17.13.35818.85 с ManagedDesktop workload;
MSSQL$SQLEXPRESS работает. Совместимость прежнего TFM сохранена.

| Проверка/команда | Факт |
|---|---|
| `dotnet --info`, vswhere, Get-Service | Указанная Windows/.NET/VS/Engine подтверждены |
| `dotnet restore SecureFileManager.sln` | Успешно вне песочницы; внутри SDK lookup получил Access denied на Microsoft SDKs |
| `dotnet build SecureFileManager.sln -c Debug --no-restore` | Первоначальные сборки успешны, 0 warnings/errors |
| `scripts/Test-Stage06.ps1` | Первые прогоны 92/92, затем 95/95, без Skip, реальный SQL |
| Обычная пересборка после запуска GUI | MSB3027/MSB3021: apphost заблокирован запущенным SecureFileManager.Wpf; это не компиляционная ошибка |
| `dotnet build SecureFileManager.sln -c Debug --artifacts-path artifacts/stage06-verify` | Финальный код: успешно, **0 warnings/errors** |
| `scripts/Test-Stage06.ps1 -ArtifactsPath <repo>/artifacts/stage06-verify` с SFM_UI_ARTIFACT_DIRECTORY=docs/screenshots/stage06 | Финальный код: **95 passed, 0 failed, 0 skipped**, 41 с |
| SQL read-only, `sqlcmd -S '.\SQLEXPRESS' -d SecureFileManagerDB -E -N -C` | Прежние 2 миграции, Users=2, Files=3 на момент SELECT; TLS TRUE, NTLM. Документы/хеши/пароли не выбирались |
| Локальный StorageRoot, вывод только этого поля JSON | `D:\SecureFileManagerData\Storage`, не изменён |
| `dotnet run --project src/SecureFileManager.Wpf --no-build --launch-profile "SecureFileManager (Development)"` | Окно входа появилось; затем CUA наблюдал защищённую главную оболочку/список личных объектов. Учётные данные агент не вводил |
| CUA native взаимодействие | Клик закрытия: coordinate input geometry is unavailable; Alt+F4 не дал подтверждённого закрытия. Принудительного завершения не было |
| WPF STA | Реальный DI graph/XAML/resources, все разделы 1024×640/1440×900, editor 600×440/860×660, фильтр Formats, TextBox/dirty state/команды; binding trace без ошибок |
| WPF рендеры | 29 PNG; визуально проверены compact TXT/JSON/XML и JSON/XML section 1024×640. Это экспорт, не native DPI-проверка |

TRX: `tests/SecureFileManager.Tests/TestResults/stage06.trx` (игнорируется Git).
Финальный isolated build — `artifacts/stage06-verify` (игнорируется Git).
При повторной проверке после закрытия открытого приложения выполнять обычную Debug-сборку/F5.

SQL-тесты используют только новую SecureFileManagerTests_<GUID> fixture данного запуска,
не SecureFileManagerDB. Fixture удаляет только созданную ею БД; скрипт в finally удаляет
только собственный новый run-GUID, не следуя reparse points. Ранее созданные базы/каталоги
и рабочий Storage не очищались. Unit XXE-файл имеет собственный GUID и удаляется в finally.

### Покрытие обязательных сценариев

Имена ниже — методы существующего/расширенного FileServiceTests, DocumentValidationTests и WpfShellTests.
Все соответствующие автотесты входят в итоговые 95/95.

| № | Сценарий | Доказательство |
|---|---|---|
| 1–3 | Открытие/сохранение/пустой TXT | EmptyListAndCompleteFileFolderCrudPersistMetadataAndAudit; BomAndLineEndingsPreservedForAllFormats |
| 4–5 | JSON open/edit/save | StructuredDocumentsRoundTripWithMetadataAndExistingAudit |
| 6–8 | Битый/пустой/глубокий JSON | StrictSyntaxAndEmptyDocuments; DepthNodeAttributeByteAndUnicodeLimitsAreBounded; InvalidStructuredSavesKeepOriginalBytesMetadataAndAuditSafe |
| 9 | Форматирование JSON | Опционально, не реализовано, тест не требуется |
| 10–11 | XML open/edit/save | StructuredDocumentsRoundTripWithMetadataAndExistingAudit |
| 12–14 | Битый/пустой XML, DTD | StrictSyntaxAndEmptyDocuments; ForbiddenXmlRejectedAtOpenWhileMalformedDocumentsCanBeCorrected |
| 15–16 | Local/network XXE | ExternalEntitiesCannotReadExclusivelyLockedLocalFileOrConnectToLoopback: test-only sentinel открыт с FileShare.None, результат не содержит sentinel; TcpListener на уникальном loopback-порту не получает соединений. Prohibit/null resolver применяются фактическим XmlReader |
| 17–20 | 10 МиБ, UTF-8, отсутствующий/занятый файл | AllFormatsRejectOversizeCorruptUtf8AndMissingOrLockedFiles; TextEncodingAndSizeLimitsAreEnforcedBeforeReplacingOriginal |
| 21 | Чужой файл | ForeignStructuredIdsCannotReadValidateReloadOrSave; ForeignIdsAndSessionChangeCannotReadWriteDeleteOrLeakResults |
| 22 | Traversal | InvalidNamesTraversalAndRootDeletionAreRejected; прежние SecurityIntegrationTests |
| 23–24 | Несохранённые изменения/отмена | EditorHonorsCancelSaveDiscardAndClearsOnSessionEnd; EditorConflictAndExplicitReloadPreserveUnsavedBufferUntilConfirmed |
| 25 | Внешний конфликт | EditorConflictAndExplicitReloadPreserveUnsavedBufferUntilConfirmed; StaleEditorAndLockedFilesCannotDamageOriginal |
| 26 | SQL failure при Save | StructuredSaveFailuresAndForeignSessionNeverDamageOriginal; StructuredAuditFailureCannotPublishOrReportValidationSuccess; AuditFailureRollsBackPhysicalSaveAndCreationAndKeepsHistory |
| 27 | Disk failure при Save | StructuredSaveFailuresAndForeignSessionNeverDamageOriginal: контролируемый IOException после Publish + реальная компенсация |
| 28 | Сохранность при validation failure | InvalidStructuredSavesKeepOriginalBytesMetadataAndAuditSafe: сравнение физических байтов/метаданных/нет pending |
| 29 | Аудит | StructuredDocumentsRoundTripWithMetadataAndExistingAudit; InvalidStructuredSavesKeepOriginalBytesMetadataAndAuditSafe; ForeignStructuredIdsCannotReadValidateReloadOrSave |
| 30 | Повторное открытие JSON/XML | StructuredDocumentsRoundTripWithMetadataAndExistingAudit; BomAndLineEndingsPreservedForAllFormats |

Регрессия 04: прежние SQL auth/registration/logout/lockout/session/foreign-owner тесты сохранены и прошли.
Регрессия 05: CRUD/папки/import/search/sort/drives/metadata/audit, stale editor,
recovery/ambiguous commit, SQL/IO failure, traversal/junction/hard link прошли.
Регрессия 03: шесть разделов/иконки/стили/навигация/layout/ошибки привязок прошли.
Это автоматическая регрессия, не новая ручная приёмка всех этих возможностей.

## 4. Полный перечень файлов

Изменены в этом этапе:

```text
TASKS.md
docs/DECISIONS.md
docs/FILE_OPERATIONS.md
docs/PROGRESS.md
docs/STORAGE_ARCHITECTURE.md
src/SecureFileManager.Core/Files/FileContracts.cs
src/SecureFileManager.Core/Files/FileService.cs
src/SecureFileManager.Wpf/Infrastructure/Dialogs/WindowsFileDialogs.cs
src/SecureFileManager.Wpf/Infrastructure/FileSystem/WindowsFileStorage.cs
src/SecureFileManager.Wpf/ViewModels/FilesViewModel.cs
src/SecureFileManager.Wpf/ViewModels/FormatsViewModel.cs
src/SecureFileManager.Wpf/ViewModels/SectionViewModel.cs
src/SecureFileManager.Wpf/ViewModels/TextEditorViewModel.cs
src/SecureFileManager.Wpf/Views/FilesView.xaml
src/SecureFileManager.Wpf/Views/FormatsView.xaml
src/SecureFileManager.Wpf/Views/NameDialog.xaml.cs
src/SecureFileManager.Wpf/Views/TextEditorWindow.xaml
tests/SecureFileManager.Tests/FileServiceTests.cs
tests/SecureFileManager.Tests/WpfShellTests.cs
```

Созданы:

```text
src/SecureFileManager.Core/Files/DocumentValidation.cs
tests/SecureFileManager.Tests/DocumentValidationTests.cs
tests/SecureFileManager.Tests/DocumentEditorIntegrationTests.cs
scripts/Test-Stage06.ps1
docs/STAGE06_REPORT.md
docs/STAGE06_MANUAL_TEST.md
docs/test-data/stage06/valid.json
docs/test-data/stage06/invalid.json
docs/test-data/stage06/valid.xml
docs/test-data/stage06/forbidden-dtd.xml
docs/screenshots/stage06/README.md
docs/screenshots/stage06/01-1024x640-render.png
docs/screenshots/stage06/01-1440x900-render.png
docs/screenshots/stage06/02-1024x640-render.png
docs/screenshots/stage06/02-1440x900-render.png
docs/screenshots/stage06/03-1024x640-render.png
docs/screenshots/stage06/03-1440x900-render.png
docs/screenshots/stage06/04-1024x640-render.png
docs/screenshots/stage06/04-1440x900-render.png
docs/screenshots/stage06/05-1024x640-render.png
docs/screenshots/stage06/05-1440x900-render.png
docs/screenshots/stage06/06-1024x640-render.png
docs/screenshots/stage06/06-1440x900-render.png
docs/screenshots/stage06/editor-Json-600x440-150pct-render.png
docs/screenshots/stage06/editor-Json-860x660-150pct-render.png
docs/screenshots/stage06/editor-Txt-600x440-150pct-render.png
docs/screenshots/stage06/editor-Txt-860x660-150pct-render.png
docs/screenshots/stage06/editor-Xml-600x440-150pct-render.png
docs/screenshots/stage06/editor-Xml-860x660-150pct-render.png
docs/screenshots/stage06/files-1024x640-150pct-render.png
docs/screenshots/stage06/files-1440x900-150pct-render.png
docs/screenshots/stage06/files-TEST-fixture-render.png
docs/screenshots/stage06/login-520x600-100pct-render.png
docs/screenshots/stage06/login-520x600-150pct-render.png
docs/screenshots/stage06/login-640x760-100pct-render.png
docs/screenshots/stage06/login-640x760-150pct-render.png
docs/screenshots/stage06/register-520x600-100pct-render.png
docs/screenshots/stage06/register-520x600-150pct-render.png
docs/screenshots/stage06/register-640x760-100pct-render.png
docs/screenshots/stage06/register-640x760-150pct-render.png
```

Исходные незакоммиченные docs/STAGE05_MANUAL_TEST.md и docs/STAGE05_REPORT.md сохранены,
новых изменений в них этим этапом не внесено. Скомпилированные outputs/TRX не предназначены для Git.

## 5. Ограничения, безопасность и следующий шаг

Ручной протокол STAGE06_MANUAL_TEST.md принят по подтверждению пользователя 02.11.2026:
файлы сохраняются, форматы проверяются. Ниже сохранён перечень сценариев повторного прогона;
отдельное выполнение каждого пункта не выводится из общего подтверждения:
реальный вход/выход двумя пользователями, оба маршрута открытия, один экземпляр редактора,
кнопки/диалоги, native keyboard/resize/DPI 150%, сохранение и проверка тестовых документов в Проводнике/SSMS.
Автоматизация пользовательской аутентификации не выполнялась: computer-use skill запрещает
`Do not automate user authentication dialogs.` Нативные клики также ограничены недоступной геометрией.
Запущенное приложение не завершалось принудительно; перед F5 его нужно закрыть самостоятельно.

Опциональное форматирование/подсветка/Save As/force overwrite отсутствуют. Кодировки кроме UTF-8
не поддерживаются. Нет гарантии мгновенной работы TextBox на предельном размере.
Фактический full disk/power loss, отдельная file symlink и атака привилегированного локального
пользователя не проверены/не обещаются. SQL/FS остаются двумя ресурсами с прежним recovery-протоколом.
Риск согласования WPF/SQL Server преподавателем PRE-03 и временный net9.0 сохранены в DECISIONS.

SEC-01/02/03/04/08/09/10: параметризованные репозитории и owner/path/handle проверки сохранены,
XXE/ошибки/конфликты/аудит проверены; содержимое документов/секреты в журнал не добавляются.
SEC-05/06 ZIP в этом этапе не реализованы. Рабочий StorageRoot/схема/данные не заменены.

**Этап 06 — Готово.** 06.01–06.06 закрыты с учётом подтверждения ручной проверки пользователем.
Переход к этапу 07 только после отдельного разрешения пользователя.

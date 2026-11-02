# Этап 07 — безопасные ZIP-операции

Дата: 02.11.2026. **Статус: Готово — ручная проверка подтверждена пользователем.**
Код 07.01–07.07 реализован, 140/140 автотестов пройдены без Skip. Пользователь сообщил,
что вручную проверил функции архива и ошибок не обнаружил, и поручил отметить приёмку пройденной.
Источник результата — подтверждение пользователя, не новый GUI-прогон Codex. Отдельный
протокол каждого шага/DPI/специальных отказов не предоставлен; такие подробности не заявляются автоматически.
Этап 08 не начат, Git-коммит не создан. Рабочие файлы и SecureFileManagerDB не удалялись/не пересоздавались.

## 1. Анализ существующего проекта

Перед кодом проверены AGENTS, TECHNICAL_SPEC, DESIGN_SPEC, TASKS, DECISIONS, PROGRESS,
STORAGE_ARCHITECTURE, PERSISTENCE, FILE_OPERATIONS, отчёты/ручные инструкции 05–06,
Storage configuration, ASSETS_README; открыт design/egyptian-ui-concept.png.
Git до работы чистый, HEAD 2410ae0 (6 etap). Изучена история 05/04/03.5/03/02.
Сверены все четыре csproj/solution, модели, auth/session, access/path policy, Win32 links/pins,
CRUD/import/редактор, репозитории/SQL-транзакции/аудит, DI/навигация/Views/resources и тесты.

Этап 06 завершён по подтверждению пользователя. Его специальные native/DPI/power-loss
ограничения остаются ограничениями доказательств; блокирующей незавершённой функции для ZIP не найдено.

| Компонент 01–06 | Что использовано/расширено |
|---|---|
| Core User/FileRecord/Operation, SessionIdentity/Revision, PersonalFileAccessPolicy | Прежняя идентичность/активность/owner-проверка каждой ZIP-команды; модели SQL не менялись |
| FileService.Run/gate/Record/DirectoryAllowed/Match/Audit/TouchParents/NewRecord | Общее выполнение ZIP, отмена, транзакция, ошибки, права, уведомления, метаданные |
| FileTransactionStore, FileRepository, OperationRepository, SecurityAudit | Прежний SQL owner lock/Serializable, Files + обязательный Operations и проверка ambiguous commit |
| PersonalStorage/RelativePathSyntax/StorageConfiguration/SafeNativeFile | Прежний корень/имена/физическая граница/reparse/hard-link policy; минимальные доработки pins/длинных путей |
| WindowsFileStorage.Workspace, .sfm-internal, Stage/Publish/Settle/Pending/Recover | Генерируемые бинарные файлы/деревья встроены в тот же протокол, без второй системы файловых операций |
| FilesView/FilesViewModel и WindowsFileDialogs | Множественный выбор, существующая «Архивировать», ZIP preview/extract/cancel, выбор личного назначения |
| ArchivesView/ArchivesViewModel | Заготовка заменена вкладками ZIP+каталоги и выбора источников на том же FilesView |
| TextEditorWindow/VM, auth, drives, ресурсы | Не переписаны; прошли прежнюю регрессию |

Новые типы: IArchiveService (фасад того же FileService), ZipLimits, ArchiveItem/Preview/Progress/Source,
BinaryDocument, IFilePreparation, PreparedEntry, ZipEngine, ArchiveTarget/TargetViewModel,
IArchiveDialogs и два WPF-диалога. Новые partial-файлы расширяют существующие классы,
не создают независимые FileService/Storage/аудит. NuGet-пакеты, TFM, SQL-схема и миграции сохранены.

Найденные и исправленные проблемы:

- При работе с деревом собственные directory handles с DELETE конфликтовали с повторным pin.
  Pin теперь переиспользует уже проверенный exclusive handle; перед tree settle внутренние
  read-only pins заменяются проверяемыми handles. Внешние rename/reparse-проверки сохранены.
- Win32 CreateFile не принимал глубокие staging-пути. Open/Rename теперь используют extended
  локальную форму пути; сравнение final handle path и ограничения относительного пути сохранены.
- После fingerprint бинарный handle мог находиться в EOF. OpenBinary явно ставит Position=0;
  создание ZIP дополнительно сверяет число скопированных байтов с длиной источника.
- Множественный выбор потребовал явного отключения одиночных команд и сброса выделения.
- UI первоначально оставлял таблице архивов нулевую высоту при 1024×640. Компактные панели,
  удаление пустых строк сообщений и проверка минимальной высоты таблицы исправили это.
- Тестовый dispatcher мог запускать автоматически поставленный startup App. STA-тест
  останавливает только constructor startup, сохраняя реальные App.xaml/DI/Views для layout.

## 2. Выполнение 07.01–07.07

| Пункт | Реализация и доказательства |
|---|---|
| 07.01 | DataGrid Extended, Ctrl/Shift, файлы/папки/смешанный набор; имя ZIP и личный родитель в WPF browser, подтверждение. CheckArchiveUi проверяет мультивыбор/команды, SQL-тест — смешанный набор. Ручная приёмка подтверждена пользователем 02.11.2026 |
| 07.02 | ZipArchive/Create + ZipArchiveEntry, относительное дерево/пустые папки, no self-inclusion, StageGenerated/CreateNew/publish, Files+ZIP_CREATED. Положительный round-trip, concurrent same-name, locked/missing input и IO compensation пройдены |
| 07.03 | ZipArchive/Read, предварительный список, сжатые/заявленные размеры, файлы/папки/total/предупреждения. Corrupt/unsupported/encrypted/unsafe/limit — разные безопасные статусы. Preview не извлекает; UI unsafe-кнопка отключена |
| 07.04 | Strict entry paths, Windows имена, case-insensitive collisions, file-parent conflicts, Unix special/symlink и DOS reparse flags; физические pins/handles. 18 path-кейсов, 4 collision-кейса, symlink attrs, реальная junction и прежние hard-link тесты прошли |
| 07.05 | 250/25 МиБ/100 МиБ/100:1, preflight до ZipArchive allocations, фактические byte counters, zero-compressed rule, длина/CRC32, ограниченный буфер. Проверены малые лимиты, 251 entry, forged/truncated headers, streaming excess и CRC failure |
| 07.06 | Потоковая подготовка в private new, новая папка публикуется одним rename, Tree-plan/компенсация/recovery, async status/cancel. SQL/IO/cancel/revoke/ambiguous commit/replacement/retry прошли; ручная приёмка подтверждена пользователем 02.11.2026 |
| 07.07 | Прежняя Operations: ZIP_CREATED, ZIP_EXTRACTED, FILE_READ preview, ACCESS_DENIED/OPERATION_FAILED. Метаданные и успешный аудит коммитятся вместе, unsafe failures не содержат текста записей. Реальные SQL assertions/trigger отказа аудита прошли |

«Архивы» показывает ZIP и каталоги **текущей личной папки** с навигацией; вкладка
«Создать архив: выбрать файлы» открывает общий браузер источников. Создание выполняется
«Архивировать». «Мои файлы» использует тот же сервис/диалоги. Одиночные Open/Delete/Сверить
недоступны при множественном выборе. Изменения сессии очищают браузеры/выбор, закрывают
ZIP-диалоги, отменяют подготовку; запоздалые progress/results не переносятся в другой сеанс.

## 3. Безопасность и протокол

### Создание

1. Run проверяет сессию/Revision и реального active SQL User, открывает тот же workspace.
2. Каждый source FileId читается owner-scoped; корни/дети сопоставляются с Files и ФС.
   Untracked/изменённые/пропавшие дети не усваиваются автоматически. Reparse/hard links отвергаются.
3. Выбор «папка + её потомок» дедуплицируется. Коллизии entry names и target внутри source запрещены.
4. ZIP пишется в прежний private new. Создание — **Store/NoCompression**, чтобы собственные ZIP
   не нарушали 100:1 на повторяющемся тексте. Исходники читаются потоково по проверенным handles,
   не изменяются. Лимиты источников также 250/25 МиБ/100 МиБ.
5. После Flush(true)/plan архив публикуется handle-bound rename без overwrite; Files и ZIP_CREATED
   (`FILE_TX_ZIP_CREATED`) коммитятся в SQL; только затем очистка/Success/Changed.

### ZIP Slip и предварительная проверка

Не используется ExtractToDirectory или строковый StartsWith(root) как граница.
EntryPath требует `/`, отвергает backslash/mixed separators, `..` компоненты, absolute/UNC/drive,
ADS, Windows reserved names/опасные окончания и служебные .sfm- пути. Итоговый относительный
путь под новым каталогом тоже ограничен 400 символами. Допустимый `..allowed.txt` не путается с traversal.
OrdinalIgnoreCase учитывает коллизии Windows; файл не может быть родителем другой записи.
Специальные Unix file modes и DOS reparse/device flags запрещены. ACL/исполняемые атрибуты
и ссылки из ZIP не восстанавливаются. XAML не интерпретируется из имён ZIP, UI выводит только текст.

До создания ZipArchive проверяются EOCD, количество/размер central directory, central/local
headers, одинаковые names/flags/methods/размеры, диапазоны compressed data и descriptor.
ZIP64/multipart/encrypted/методы кроме Store/Deflate отклоняются. Это сознательно ограниченный
ZIP-профиль. Неизвестная структура отвергается, а не «чинится» ослаблением настроек.

### ZIP Bomb

ZipLimits — immutable параметры сервиса с безопасными дефолтами; через DI можно только
ужесточать их. UI-настроек нет. Входной ZIP <=101 МиБ, central directory <=1 МиБ.
До ZipArchive allocations проверяется <=250 записей; синтезированные выходные папки
тоже входят в предел <=250 узлов. Запись <=25 МиБ, сумма <=100 МиБ, ratio <=100:1.
Ненулевой output при compressed=0 запрещён. Эти значения проверяются и по metadata,
и по фактическим распакованным байтам **перед записью каждого блока**; буфер 64 КиБ.
Фактическая длина должна совпасть с заявленной, CRC32 рассчитывается и сравнивается с central CRC.
Весь ZIP/entry в RAM не загружается. SHA-256 входа и snapshot тоже используют ограниченный буфер.
Preview показывает metadata, не обещает CRC/фактическую проверку содержимого до Extract.

### Staging, SQL/FS и восстановление

Extract выполняется только в **новую папку в выбранном родителе**, без merge/overwrite.
Все записи предварительно проверяются, затем потоково создаются в том же private new;
имена/родители/reparse/создание CreateNew перепроверяются непосредственно при IO.
После подготовки Tree-plan хранит состав, вид, identity каталогов и identity/size/mtime/SHA-256 файлов.
План <=1 МиБ, прежние планы без Tree совместимы. Один root rename публикует дерево.
В той же SQL-транзакции создаются все Files и ZIP_EXTRACTED/`FILE_TX_ZIP_EXTRACTED`.

При известном rollback всё дерево сначала сравнивается с планом с удержанием handles,
затем удаляется только совпадающее собственное дерево. Неожиданное изменение сохраняется
и даёт RecoveryRequired. При неизвестном commit прежний FileTransactionStore проверяет
UserId/CorrelationId/FILE_TX_*; недоступный SQL сохраняет артефакты и блокирует новые команды.
«Восстановить» завершает committed результат либо снимает неизменённый uncommitted результат.
Неполный plan/ошибка cleanup не повод удалять служебную папку: требуется ручной разбор.

Публикация дерева одним rename не делает SQL и NTFS распределённо атомарными.
Есть промежуток между publish и SQL commit, durability при любом power loss не обещается.
Pin/handles ограничивают обычные filesystem races; защита от администратора/того же Windows token
с прямым доступом к диску/SQL не заявляется. Реальный disk-full/power-loss не воспроизводился.

## 4. Проверки

Среда: Windows 10, SDK 9.0.200; VS 2022 17.13.35818.85 + ManagedDesktop; SQL Express Running.

| Проверка | Фактический результат |
|---|---|
| `dotnet restore SecureFileManager.sln` | Успех с доступом к установленным Windows SDK/NuGet вне песочницы |
| `dotnet build SecureFileManager.sln -c Debug --no-restore` | Успех, 0 warnings/errors до последнего уточнения boundary-проверки |
| `dotnet build SecureFileManager.sln -c Debug --artifacts-path artifacts/stage07-verify --no-restore` | Последний код: успех, 0 warnings/errors |
| `scripts/Test-Stage07.ps1` | Полный обычный Debug-прогон 140/140, 0 failed/skipped; 37 с |
| `scripts/Test-Stage07.ps1 -ArtifactsPath <repo>/artifacts/stage07-verify` | Итоговый код/усиленная mixed-selection регрессия: 140 passed, 0 failed, 0 skipped; 33 с |
| SQL integration | Новая GUID-БД только текущего запуска; реальные repositories/transact/audit, controlled trigger/IO/missing DB/ambiguous commit; cleanup своего запуска выполнен |
| Рабочая БД, read-only `sqlcmd -E -N -C` | Две прежние миграции, encrypt_option TRUE, auth_scheme NTLM; новая рабочая БД не создавалась |
| Вывод только StorageRoot из локального JSON | D:\SecureFileManagerData\Storage, не менялся |
| `dotnet run --project src/SecureFileManager.Wpf --no-build --launch-profile "SecureFileManager (Development)"` | Окно входа появилось; CUA прочёл «Подключение к БД проверено. Вход требует проверки пароля и аудита»; пароли не вводились |
| WPF STA | DI aliases, реальные XAML/resources, Extended selection, mixed UI selection, команды, ZIP-фильтр/session reset, unsafe preview button, folders picker/header/rows; binding trace без ошибок |
| Layout/render | 34 PNG, 1024×640/1440×900, ZIP dialogs 600×440/900×620; экспорт 150%, компактные архивы/preview/target визуально просмотрены |
| `git diff --check` | Ошибок whitespace нет; LF→CRLF warnings Git — политика окончаний строк, не предупреждения компилятора |

Итоговый TRX: `tests/SecureFileManager.Tests/TestResults/stage07.trx`, 140/140.
Точечные повторы используют stage07-targeted.trx. Outputs/TRX игнорируются Git.
Скрипт в finally очищает только свой новый run-GUID, не следуя reparse; fixture удаляет
только свою новую GUID-БД. Чужие старые тестовые БД/каталоги и рабочий Storage не очищались.

Первые проверки нашли два функциональных сбоя, затем один остаточный сбой глубокого пути;
тесты не удалялись/не ослаблялись. Промежуточные 127/129 и 128/129 не считаются итогом.
Исправлены handles/extended paths/позиция потока, добавлены содержательные round-trip assertions.
Layout-тест усилен проверкой высоты таблицы и текста/ширины колонок folder picker.

### Матрица обязательных сценариев

Все перечисленные автоматические методы входят в итоговые 140/140.

| № | Сценарий | Проверка |
|---|---|---|
| 1–2 | Один/несколько файлов | ZipSingleMultipleFilesAndSelfInclusionAndExistingDestination |
| 3–6 | Каталог/вложенность/структура/пустые папки | ZipCreationPreviewExtractionPreservesTreeFilesMetadataAndAudit; DeflateRoundTrip unit |
| 7 | Preview без извлечения | PreviewDoesNotExtractAndDeflateRoundTripPreservesStructureAndEmptyFolders; SQL asserts отсутствия target до Extract |
| 8–10 | Extract/совпадение/Files+Operations | ZipCreationPreviewExtractionPreservesTreeFilesMetadataAndAudit |
| 11 | Self inclusion | ZipSingleMultipleFilesAndSelfInclusionAndExistingDestination |
| 12–16 | Чужой source/destination/root/no session/tampered path | ZipAccessSessionTraversalAndForeignDestinationAreDeniedAndAudited; ReparseDestinationAndSourceCannotEscapeOwnerRoot |
| 17–21 | ../, absolute, drive, UNC, mixed separators | DangerousWindowsPathsAreRejectedBeforeExtraction (18 вариантов) |
| 22–24 | Коллизии/дубли/Windows names | DuplicateAndFileDirectoryCollisionsAreRejected; DangerousWindowsPathsAreRejectedBeforeExtraction |
| 25–26 | Symlink/reparse | SymlinkAndReparseAttributesAreRejected (Unix symlink/special/DOS reparse); реальная junction в ReparseDestinationAndSourceCannotEscapeOwnerRoot. Отдельная physical file symlink не создавалась |
| 27 | >250 entries | EnvelopeBoundsEntryCountBeforeZipArchiveAllocation (251 маленькая пустая запись) |
| 28–31 | Per-entry/total/ratio/zero compressed | DeclaredEntryTotalAndRatioLimitsAreChecked; DeclaredLimitsAndCrcMismatchLeaveNoFinalTree; малые лимиты без реального ZIP Bomb |
| 32–33 | Forged headers/actual streaming excess | CrcAndTruncatedOrForgedDataAreRejected; StreamingGuardsActualEntryTotalRatioAndDeclaredBytesBeforeWriting |
| 34–35 | Corrupt/unsupported/encrypted | Zip64AndMalformedNonZipAreRejected; EncryptedAndUnsupportedCompressionAreDistinct |
| 36 | Недоступный файл | ConcurrentArchiveNameOnlyPublishesOneAndLockedMissingInputsFailSafely |
| 37 | Disk-full | SimulatedDiskFullAfterPublishCompensatesArchiveAndPreservesSources: контролируемый IOException 0x80070070, не заполнение диска |
| 38 | IO failure | ZipPublishIoFailureCompensatesTreeAndRetrySucceeds |
| 39 | SQL/audit failure | MissingSqlCannotCreateOrExtractArchive; ExtractAuditFailureRollsBackAllMetadataAndPublishedTree |
| 40–41 | Cancel/очистка staging | CancellationDuringExtractionCleansStageAndSessionChangePreventsPublication; CRC/IO rejection tests |
| 42–44 | Existing data/no false metadata/audit отказов | UnsafeZipDoesNotPublishAnythingOrRegisterSuccess; ExtractAuditFailureRollsBackAllMetadataAndPublishedTree; existing destination conflict |
| 45 | Retry/recovery/ambiguous commit | ZipPublishIoFailureCompensatesTreeAndRetrySucceeds; UncommittedTreeRecoveryAndUnexpectedReplacementPreserveData; AmbiguousZipCommitsRecoverWithoutDeletingCommittedTree |
| 46 | Concurrent same-name | ConcurrentArchiveNameOnlyPublishesOneAndLockedMissingInputsFailSafely |

Регрессия 01–06: все прежние 95 тестов сохранены и пройдены (auth/lockout/logout/session,
owner/path/links, drives/CRUD/import/search/sort, staging/SQL/recovery, TXT/JSON/XML/XXE,
BOM/limits/dirty-close/conflict, WPF navigation/styles/resources). Это автоматическая
регрессия, а не новая ручная приёмка каждого предыдущего сценария.

## 5. Ограничения и ручная часть

- Ручная приёмка STAGE07_MANUAL_TEST.md закрыта по подтверждению пользователя. Для повторных прогонов: Ctrl/Shift, выбор назначения/подтверждения,
  preview/extract/cancel, Проводник/SSMS, два пользователя, Windows DPI/keyboard.
- Native ZIP workflow агентом не выполнялся. Использован computer-use для read-only проверки
  запуска; skill запрещает `Do not automate user authentication dialogs.` Пользовательские документы не редактировались.
- Создание Store без сжатия; чтение Store/Deflate. ZIP64/encrypted/multipart/прочие методы не поддерживаются.
- Распаковка только в новый каталог, без merge/overwrite. Исходные timestamps/ACL/специальные
  атрибуты записей не восстанавливаются. Preview не гарантирует CRC/реальную длину до Extract.
- Некоторые hash/handle/commit/recovery фазы не отменяются мгновенно; committed результат при поздней отмене сохраняется.
- Подтверждённый OS disk-full/power-loss, отдельная physical file symlink и привилегированный
  локальный обход не заявляются. Общие ограничения PRE-03/временного net9.0 остаются в DECISIONS.
- Проверочный exe оставлен на окне входа; перед пересборкой/F5 закрыть старый экземпляр.
  Последний код проверен в artifacts/stage07-verify; обычный профиль после пересборки использует тот же StorageRoot.

SEC-01/02/03/05/06/08/09/10 затронуты и покрыты указанными проверками; прежний SEC-04/07
прошёл регрессию. Безопасность не ограничена скрытыми UI-кнопками, решения повторяются в сервисе/IO.

## 6. Статус

**Этап 07 — Готово.** 07.01–07.07 закрыты с учётом подтверждения ручной проверки пользователем.
При ручной проверке функций архива пользователь ошибок не обнаружил. Известные технические ограничения сохранены.
Этап 08 — только после отдельного разрешения. Git-коммит не создавался.

## Полный список изменений

Ниже перечислены все файлы текущей работы; исходный Git был чистым. Сборочные outputs/TRX
не включены, поскольку игнорируются Git.

Изменены:

- TASKS.md
- docs/DECISIONS.md
- docs/FILE_OPERATIONS.md
- docs/PROGRESS.md
- docs/STORAGE_ARCHITECTURE.md
- src/SecureFileManager.Core/Files/FileContracts.cs
- src/SecureFileManager.Core/Files/FileService.cs
- src/SecureFileManager.Wpf/App.xaml.cs
- src/SecureFileManager.Wpf/Infrastructure/Dialogs/WindowsFileDialogs.cs
- src/SecureFileManager.Wpf/Infrastructure/FileSystem/SafeNativeFile.cs
- src/SecureFileManager.Wpf/Infrastructure/FileSystem/WindowsFileStorage.cs
- src/SecureFileManager.Wpf/ViewModels/ArchivesViewModel.cs
- src/SecureFileManager.Wpf/ViewModels/FilesViewModel.cs
- src/SecureFileManager.Wpf/Views/ArchivesView.xaml
- src/SecureFileManager.Wpf/Views/FilesView.xaml
- src/SecureFileManager.Wpf/Views/FilesView.xaml.cs
- tests/SecureFileManager.Tests/FileServiceTests.cs
- tests/SecureFileManager.Tests/WpfShellTests.cs

Созданы:

- docs/STAGE07_MANUAL_TEST.md
- docs/STAGE07_REPORT.md
- docs/screenshots/stage07/01-1024x640-render.png
- docs/screenshots/stage07/01-1440x900-render.png
- docs/screenshots/stage07/02-1024x640-render.png
- docs/screenshots/stage07/02-1440x900-render.png
- docs/screenshots/stage07/03-1024x640-render.png
- docs/screenshots/stage07/03-1440x900-render.png
- docs/screenshots/stage07/04-1024x640-render.png
- docs/screenshots/stage07/04-1440x900-render.png
- docs/screenshots/stage07/05-1024x640-render.png
- docs/screenshots/stage07/05-1440x900-render.png
- docs/screenshots/stage07/06-1024x640-render.png
- docs/screenshots/stage07/06-1440x900-render.png
- docs/screenshots/stage07/README.md
- docs/screenshots/stage07/editor-Json-600x440-150pct-render.png
- docs/screenshots/stage07/editor-Json-860x660-150pct-render.png
- docs/screenshots/stage07/editor-Txt-600x440-150pct-render.png
- docs/screenshots/stage07/editor-Txt-860x660-150pct-render.png
- docs/screenshots/stage07/editor-Xml-600x440-150pct-render.png
- docs/screenshots/stage07/editor-Xml-860x660-150pct-render.png
- docs/screenshots/stage07/files-1024x640-150pct-render.png
- docs/screenshots/stage07/files-1440x900-150pct-render.png
- docs/screenshots/stage07/files-TEST-fixture-render.png
- docs/screenshots/stage07/login-520x600-100pct-render.png
- docs/screenshots/stage07/login-520x600-150pct-render.png
- docs/screenshots/stage07/login-640x760-100pct-render.png
- docs/screenshots/stage07/login-640x760-150pct-render.png
- docs/screenshots/stage07/register-520x600-100pct-render.png
- docs/screenshots/stage07/register-520x600-150pct-render.png
- docs/screenshots/stage07/register-640x760-100pct-render.png
- docs/screenshots/stage07/register-640x760-150pct-render.png
- docs/screenshots/stage07/zip-preview-False-600x440-150pct-render.png
- docs/screenshots/stage07/zip-preview-False-900x620-150pct-render.png
- docs/screenshots/stage07/zip-preview-True-600x440-150pct-render.png
- docs/screenshots/stage07/zip-preview-True-900x620-150pct-render.png
- docs/screenshots/stage07/zip-target-600x440-150pct-render.png
- docs/test-data/stage07/README.md
- docs/test-data/stage07/corrupt.zip
- docs/test-data/stage07/duplicate.zip
- docs/test-data/stage07/safe.zip
- docs/test-data/stage07/unsafe-path.zip
- scripts/Test-Stage07.ps1
- src/SecureFileManager.Core/Files/ArchiveContracts.cs
- src/SecureFileManager.Core/Files/FileService.Archives.cs
- src/SecureFileManager.Core/Files/ZipEngine.cs
- src/SecureFileManager.Wpf/Infrastructure/FileSystem/WindowsFileStorage.Prepared.cs
- src/SecureFileManager.Wpf/ViewModels/ArchiveDialogs.cs
- src/SecureFileManager.Wpf/Views/ArchivePreviewWindow.xaml
- src/SecureFileManager.Wpf/Views/ArchivePreviewWindow.xaml.cs
- src/SecureFileManager.Wpf/Views/ArchiveTargetWindow.xaml
- src/SecureFileManager.Wpf/Views/ArchiveTargetWindow.xaml.cs
- tests/SecureFileManager.Tests/ArchiveIntegrationTests.cs
- tests/SecureFileManager.Tests/ZipEngineTests.cs

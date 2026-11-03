# Итоговый отчёт тестирования этапов 09–10

Дата: **03.11.2026**. Версия: HEAD `598b175` (`8 etap`), исходно чистый Git.
В текущем этапе исполняемый код, XAML, пакеты, модели, миграции и тесты не изменены.
Статусы: PASS — фактически выполнено; FAIL — выполнено с ошибкой; NOT RUN — не запускалось;
BLOCKED — внешняя причина помешала проверке. Документированный старый результат
не считается новым запуском. **Итог: требуется ручная приёмка.**

## Цель, контекст и ранее полученные результаты

Подтвердить обязательные сценарии и регрессию 01–08 без повторной разработки,
не затрагивая рабочие Users/Files/Operations и Storage. Изучены AGENTS, ТЗ, дизайн,
план, решения/прогресс, persistence/storage/file operations, отчёты 02–08 и ручные
протоколы, исходники четырёх проектов, DI, ресурсы, migrations/model snapshot и тесты.
README/ARCHITECTURE/итогового TEST_REPORT ранее не было — созданы в этом этапе.
Изображение design/egyptian-ui-concept.png открыто; оформление не менялось.
Git: `598b175`, `7c10f83`, `2410ae0`, `01893f4` — этапы 08/07/06/05;
чужих незакоммиченных изменений на старте не обнаружено. Коммиты не создавались.

Реализованы auth/KDF/lockout/session, owner-scoped личные каталоги, read-only диски,
CRUD/import, TXT/JSON/XML, bounded XML/ZIP, staging/recovery, SQL metadata/audit,
собственный журнал, фактическое состояние аудита и неразрушающая диагностика.

| Предыдущая проверка | Достоверный источник / версия | Результат |
|---|---|---|
| Этап 02 | STAGE02_TESTS.md, 01.11 | 16/16, включая 8 SQL |
| Этапы 04/05 | STAGE04_REPORT.md / STAGE05_REPORT.md | 47/47 / 66/66 |
| Этап 06 | STAGE06_REPORT.md, 02.11; последующий commit 2410ae0 | 95/95, общая ручная приёмка пользователя |
| Этап 07 | STAGE07_REPORT.md, 02.11; commit 7c10f83 | 140/140, общая ручная приёмка пользователя |
| Этап 08 | STAGE08_SECURITY_AUDIT.md и PROGRESS, 03.11; commit 598b175 | 160/160 до последнего XAML, затем WPF 1/1; пользователь принял исправленное описание |

Общая ручная приёмка предыдущих этапов сохранена как исторический PASS в её заявленном
объёме. Отдельные размеры/native DPI/keyboard/специальные IO-отказы этими сообщениями
не подтверждены. Нынешний полный прогон включает последнюю версию XAML.

## Среда

Windows 10 10.0.19045 x64; Visual Studio Community 2022 17.13.1
(17.13.35818.85); SDK 9.0.200, MSBuild 17.13.8; установлен WindowsDesktop runtime 9.0.2.
Wpf/Tests net9.0-windows, Core/Data net9.0. EF Core/SqlServer/Design/Identity/DI 9.0.22;
xUnit 2.9.2, runner 2.8.2, Test SDK/VSTest 17.12.0.
Реальный локальный SQL Express: 15.0.2000.5 Express Edition (64-bit), служба Running;
версия подтверждена read-only SELECT. SQL-тесты: Windows Authentication, Encrypt Mandatory,
явное локальное Development-исключение TrustServerCertificate=true. Настройки сервера не менялись.

## Сборки, инструменты, публикация

| Проверка | Статус | Фактический результат |
|---|---|---|
| dotnet restore SecureFileManager.sln | PASS | все проекты восстановлены |
| dotnet build SecureFileManager.sln -c Debug --artifacts-path artifacts/final-verify | PASS | 0 warnings, 0 errors; 15.14 с |
| dotnet build SecureFileManager.sln -c Release --artifacts-path artifacts/final-verify | PASS | 0 warnings, 0 errors; 7.63 с |
| dotnet tool restore | PASS | dotnet-ef 9.0.22 |
| dotnet ef migrations has-pending-model-changes --project src/SecureFileManager.Data --startup-project src/SecureFileManager.Data --configuration Release | PASS | No changes have been made to the model since the last migration; БД не обновлялась |
| dotnet publish src/SecureFileManager.Wpf/SecureFileManager.Wpf.csproj -c Release --artifacts-path artifacts/final-verify --self-contained false -o artifacts/publish/final-win --no-restore | PASS | framework-dependent Release, Windows x64 host |
| Проверка состава publish | PASS | exe/dll/deps/runtimeconfig и runtime assets, нет Development JSON/БД/Storage/demo; XAML/PNG встроены WPF Resource |
| Старт опубликованного exe из TEMP с абсолютным внешним config directory | PASS | процесс 30348, обнаружено окно «Secure File Manager — вход»; без ввода логина/пароля |
| Содержимое native окна / настоящий скриншот | BLOCKED | computer-use: FrameArrived timeout, повтор после reselection/activation — window capture timeout |
| Запуск на чистом профиле/ПК | NOT RUN | отдельной чистой среды нет; процедура в STAGE09_10_MANUAL_TEST |
| Полный Release test | NOT RUN | не дублируется неизменённый SQL-прогон; Release build/publish проверены отдельно |

Предварительные ошибки проверки, исправленные без изменений приложения:
restore внутри sandbox — MSB4184 (закрыт доступ к Windows SDK), повтор с разрешённым
доступом успешен. Первый build --no-restore в **новом** artifacts path — NETSDK1004;
повтор с restore для этого пути успешен. Read-only sqlcmd внутри sandbox не получил
Windows credentials; тот же SELECT с разрешённым доступом успешен.
Это не дефекты приложения и не скрытые успешные проверки.

## Автоматические тесты

Выполнен существующий `scripts/Test-Stage08.ps1 -ArtifactsPath D:\SecureFileManager\artifacts\final-verify`.
Он запускает `dotnet test SecureFileManager.sln -c Debug` с SQL-флагом, отдельным
artifacts path и TRX logger. Новые дублирующие тесты не добавлялись.

**Всего 160; executed 160; passed 160; failed 0; skipped/notExecuted 0.**
Консоль VSTest: **35 с** выполнения; TRX start 17:59:02.4859165+03:00,
finish 17:59:39.9140200+03:00, интервал **37.428 с** с инфраструктурой запуска.
Локальный сохранённый итог: `artifacts/final-evidence/final-debug.trx` (ignored).
Подробный список каждого test case, исходный файл/строка и результат сохранён
в [TEST_INVENTORY.md](TEST_INVENTORY.md); summary не зависит от наличия ignored TRX.
Stage08-скрипт использует фиксированный stage08.trx, при запуске он сообщил о перезаписи
прежнего файла этого имени; итог дополнительно сохранён под final-debug.trx.
Предыдущие опубликованные отчёты не изменялись, чужие БД/каталоги не очищались.

78 test cases — реальные SQL integration (включая partial FileServiceTests из отдельных
файлов editor/archive/journal). Остальные 82 — unit/config/navigation/parser/VM,
Windows storage/drive и один составной WPF STA/layout test. Не все 82 являются чистыми unit.

| Группа в TRX | Passed | Тип |
|---|---:|---|
| PersistenceTests | 8 | SQL integration |
| SecurityIntegrationTests | 14 | SQL + Windows FS/auth/session |
| FileServiceTests (partial) | 56 | SQL + реальный FS; CRUD 18, editor 9, archives 14, journal 15 |
| DatabaseConfigurationTests | 6 | конфигурация/DI |
| DocumentValidationTests | 20 | pure parser/security |
| ZipEngineTests | 31 | ZIP/потоки/лимиты |
| PasswordTests | 7 | KDF/форма/валидация |
| NavigationTests | 3 | VM |
| JournalTests | 5 | health/VM/безопасная проекция/XML |
| StorageConfigurationTests | 8 | config + изолированные Windows каталоги |
| DriveTests | 1 | настоящий DriveInfo + контролируемые ошибки |
| WpfShellTests | 1 | STA, DI/XAML/resources/layout/bindings |

Новая SQL-БД имеет имя SecureFileManagerTests_<32 hex>, fixture проверяет отсутствие
до миграции и удаляет только БД своего запуска. Для existing-test mode удаления нет.
ФС: новый `TEMP/SecureFileManager.Stage08/run-c05278e59dca4d97b09b0d2d5cc6e5ec`,
cleanup скрипта подтверждён, без следования reparse. Рабочая SecureFileManagerDB
не является target fixture. В тестах нет остановки общего Engine/заполнения диска.

## Матрица T01–T20

Все автоматические PASS ниже — **новый запуск 03.11.2026, HEAD 598b175, Debug**, среда выше.
Шаг — действие из TASKS; actual — выполненные asserts теста, expected — описание ниже.
Доказательство для каждой строки — TEST_INVENTORY + final-debug.trx и названный метод.
Полное имя файла: `tests/SecureFileManager.Tests/<File>`. Ручная часть не приравнена
к автоматическому PASS. «Не требуется» означает, что угрозу проверяет сервисный тест;
итоговый GUI smoke остаётся отдельным обязательством. Дефектов этого прогона нет.

| ID / шаг и expected | Автотест (файл: метод) / actual | Авто | Ручная часть / итог покрытия |
|---|---|---|---|
| T01 регистрация → вход → собственная сессия | SecurityIntegrationTests.cs: RegisterStoresOnlyHashPreparesGuidDirectoryAndRejectsDuplicate; SuccessfulLoginUpgradesOldHashAndStorageRepairRequiresAudit — hash/GUID/audit/session проверены | PASS | Исторический общий PASS этапов 04–08; финальный smoke NOT RUN |
| T02 неправильный пароль → одинаковый отказ и аудит | SecurityIntegrationTests.cs: WrongUnknownInactiveAndLockedLoginsUseSameMessageAndAuditNullableFile; FiveFailuresBlockForFifteenMinutesAndSuccessResets | PASS | Сервисное требование покрыто; отдельная ручная не нужна |
| T03 диски → реальные готовые/неготовые сведения | DriveTests.cs: RealDriveDescriptionsAreBoundedAndNonReadyDriveDoesNotThrow — реальное перечисление и контролируемый unavailable | PASS | Общая приёмка этапа 05; итоговый UI smoke NOT RUN |
| T04 create → FS+Files+audit | FileServiceTests.cs: EmptyListAndCompleteFileFolderCrudPersistMetadataAndAudit | PASS | Общая приёмка 05; итоговый smoke NOT RUN |
| T05 save → байты+метаданные | FileServiceTests.cs: EmptyListAndCompleteFileFolderCrudPersistMetadataAndAudit; DocumentEditorIntegrationTests.cs: StructuredDocumentsRoundTripWithMetadataAndExistingAudit | PASS | Общая приёмка 05/06; итоговый smoke NOT RUN |
| T06 delete → soft-delete/история, UI confirmation | FileServiceTests.cs: EmptyListAndCompleteFileFolderCrudPersistMetadataAndAudit; PersistenceTests.cs: MetadataCrudSoftDeletePreservesAuditAndAllowsPathReuse | PASS | FS/SQL покрыты; native confirmation в финальной версии NOT RUN |
| T07 valid/broken JSON → принятие/ошибка без порчи | DocumentValidationTests.cs: StrictSyntaxAndEmptyDocuments; DocumentEditorIntegrationTests.cs: InvalidStructuredSavesKeepOriginalBytesMetadataAndAuditSafe | PASS | Общий ручной PASS 06; отдельный итоговый smoke NOT RUN |
| T08 valid/broken/XXE XML → запрет внешнего IO | DocumentValidationTests.cs: ExternalEntitiesCannotReadExclusivelyLockedLocalFileOrConnectToLoopback; JournalTests.cs: BillionLaughsRejectedBeforeExpansion | PASS | Угроза покрыта изолированно; GUI общий PASS 06 |
| T09 ZIP create/extract → совпадение байтов/дерева/SQL | ArchiveIntegrationTests.cs: ZipCreationPreviewExtractionPreservesTreeFilesMetadataAndAudit | PASS | Общий ручной PASS 07; итоговый smoke NOT RUN |
| T10 ../ ZIP Slip → нет внешнего файла | ZipEngineTests.cs: DangerousWindowsPathsAreRejectedBeforeExtraction; ArchiveIntegrationTests.cs: UnsafeZipDoesNotPublishAnythingOrRegisterSuccess | PASS | Покрыто, повтор руками не нужен |
| T11 ZIP Bomb → отказ и cleanup | ZipEngineTests.cs: EnvelopeBoundsEntryCountBeforeZipArchiveAllocation; StreamingGuardsActualEntryTotalRatioAndDeclaredBytesBeforeWriting; ArchiveIntegrationTests.cs: DeclaredLimitsAndCrcMismatchLeaveNoFinalTree | PASS | Малые безопасные payload/limits; реальная бомба не нужна |
| T12 traversal/absolute → отказ | FileServiceTests.cs: InvalidNamesTraversalAndRootDeletionAreRejected | PASS | Покрыто; реальный root не изменялся |
| T13 SQLi login/search → literal SQL | JournalIntegrationTests.cs: SqlInjectionLoginIsLiteralAndLockoutCodeRevealsNoPassword; JournalServerFiltersPagesSortsAndNeverIncludesForeignOrSystemEvents; PersistenceTests.cs: UsernameUniqueCaseInsensitiveAndInputIsParameterized | PASS | LINQ/parameter binding + реальные SQL asserts, не только статический поиск |
| T14 чужой ID → отказ до чтения | FileServiceTests.cs: ForeignIdsAndSessionChangeCannotReadWriteDeleteOrLeakResults; DocumentEditorIntegrationTests.cs: ForeignStructuredIdsCannotReadValidateReloadOrSave | PASS | Две сессии/отказ/отсутствие утечки покрыты |
| T15 журнал → верный actor/result/UTC/object | JournalIntegrationTests.cs: JournalServerFiltersPagesSortsAndNeverIncludesForeignOrSystemEvents; JournalDeletedHistorySurvivesAndInvalidQueriesDoNotBypassSession | PASS | Исторический общий PASS 08 после description fix; финальный smoke NOT RUN |
| T16 DB/FS error → fail closed/compensation, UI не падает | FileServiceTests.cs: MissingSqlCannotPerformFilesystemChanges; FilesystemFailureAfterPublishIsCompensatedAndSessionRevocationIsChecked; JournalTests.cs: JournalVmDisplaysDatabaseFailureAndDoesNotKeepOldRows | PASS | Сервисы/VM проверены; native сообщение при отказе и slow UI NOT RUN |
| T17 dirty close → Save/Discard/Cancel | FileServiceTests.cs: EditorHonorsCancelSaveDiscardAndClearsOnSessionEnd | PASS | VM покрыта; настоящий MessageBox/фокус итогового окна NOT RUN |
| T18 concurrent create → один успех, нет порчи | FileServiceTests.cs: DuplicateAndConcurrentCaseInsensitiveCreateNeverOverwrite; JournalIntegrationTests.cs: ConcurrentSavesAndDeleteDuringLockedReadPreserveData | PASS | Изолированная реальная гонка; повтор на рабочих данных не нужен |
| T19 1024×640/150% → доступный UI | WpfShellTests.cs: AllScreensResourcesAndLayoutsRenderWithoutBindingErrors — measure/layout/resources/binding trace двух размеров; bitmap150 есть в исторических отчётах, в этом запуске export не включался | PASS | **NOT RUN** native Windows DPI 100/150; полный T19 не закрыт |
| T20 Tab/Enter/Esc/Ctrl+S → корректные действия/фокус | тот же WpfShellTests, NavigationTests — наличие команд/привязок, не системный ввод | PASS | **NOT RUN** native keyboard; полный T20 не закрыт |

17 P0 сценариев имеют автоматическое подтверждение. Это не утверждение прохождения
всех ручных аспектов этих кейсов. T17/T19/T20 требуют итогового native подтверждения.
Итоговая ручная матрица и clean install остаются открытыми.

## Безопасность и межмодульная регрессия

| Требование / угроза | Статус и фактическое подтверждение |
|---|---|
| SEC-01 SQL Injection | PASS: EF LINQ/FromSqlInterpolated, реальные SQLi login/name/search tests, DB constraints preserved |
| SEC-02 Path Traversal, SEC-03 ownership | PASS: path/foreign IDs/current user/active SQL/revision; реальные junction/hardlinks и ancestor replacement |
| SEC-04 XML/XXE | PASS: DTD prohibit/resolver null, locked sentinel/loopback без чтения/соединения, bounded complexity |
| SEC-05 ZIP Slip / SEC-06 ZIP Bomb | PASS: declared+actual byte limits, path/link/header/CRC rejection, stage cleanup; без реальной бомбы |
| SEC-07 пароли | PASS: StandardHasherUsesSaltVersionAndRehash, lockout/concurrent failures, no plaintext output |
| SEC-08 races/TOCTOU | PASS в заданной модели: CreateNew, pinned handles, version/hash, конкурирующие create/save/delete; абсолютная защита от внешнего владельца процесса не заявляется |
| SEC-09 аудит / consistency | PASS: failed/denied/success, probe rollback, failure latch, empty tx не заявляет audit write; реальные SQL transaction/controlled trigger failures, missing DB, IO failures, ambiguous commit/recovery |
| SEC-10 секреты/исключения | PASS: safe projections/parser messages, no document/external-source payload in audit |
| Физический disk-full / power loss / отдельная physical file symlink | NOT RUN: controlled IOException 0x80070070, реальные junction/hardlinks и reparse flags не подменяют эти отдельные проверки |

Все прежние тесты прошли одним полным запуском, включая auth→files, session→VM,
editor→save/metadata, ZIP→tree/SQL, audit→error/rollback. Последний XAML description fix
прошёл WPF layout assertion с многострочным текстом. Масштабный рефакторинг не выполнялся.
Сценарии полной остановки рабочего SQL, изменения рабочего Storage/ACL и заполнения
диска намеренно не запускаются: требования ошибки проверяются изолированными средствами.

## Дефекты, ограничения, готовность

Новых воспроизведённых дефектов приложения в итоговом прогоне нет; исправлений кода нет.
Исторические дефекты и их тесты: stage05 Percent binding/dialog setter/import refresh,
stage07 handles/extended path/stream positioning, stage08 collation/filter layout/
description wrapping — источники STAGE05_REPORT, STAGE07_REPORT, STAGE08_SECURITY_AUDIT,
PROGRESS. Их не следует считать вновь найденными/исправленными в этапе 09–10.

Открыто: native UI/DPI/keyboard/slow-operation smoke, настоящие итоговые GUI/SSMS
скриншоты, чистая установка. Скриншотный helper не смог захватить окно; не заявляется
успешный UI или проверенное содержимое окна по одному заголовку.
Формальный PRE-03 (допустимость WPF/SQL Server) открыт.
Модель угроз не покрывает Windows/SQL admin, прямой доступ одного token к Storage/SQL;
нет at-rest encryption/криптографического аудита/общей SQL+NTFS атомарности.
Preview ZIP не доказывает actual CRC до Extract; диагностике без baseline недоступно
сравнение содержимого одинакового размера/mtime. Прямой exe требует явной конфигурации.
Используется прежний временный net9.0, его обновление не выполнено.

**Автоматическая стабилизация и материалы подготовлены. Secure File Manager C# требует
ручной приёмки; этапы 09–10 целиком завершёнными не объявляются.**

Дополнительная проверка артефактов документации: все локальные Markdown-ссылки новых
документов разрешаются; TEST_INVENTORY содержит 160 строк с 78 SqlFact. Новые demo-файлы
проверены настоящим DocumentValidator из Release Core: TXT/JSON/XML — VALID,
invalid.json — JSON_SYNTAX, invalid.xml — XML_SYNTAX, forbidden-dtd.xml — XML_DTD.
Это отдельная проверка набора demo, не дополнительные xUnit test cases к числу160.
demo/safe.zip содержит first.txt и second.txt по67 байт, Store, без внешних путей.

Проверочный publish-процесс30348 оставлен запущенным: проверка безопасной очистки
обнаружила MainWindowHandle и не завершила интерактивное окно принудительно.
Перед заменой/пересборкой этой публикации пользователь закрывает проверочное окно.

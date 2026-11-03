# Этап 08 — журнал и сквозной аудит безопасности

Дата: 03.11.2026. Статус: **Готово — ручная приёмка подтверждена пользователем после исправления описания**.
Новых проблем пользователь не заметил. Это пользовательское подтверждение, не новый GUI-прогон Codex; отдельное выполнение каждого шага/DPI/специального отказа не утверждается. Прежние ограничения и исторические результаты ниже сохранены.
Этап 09 не начат. Коммиты не создавались. Рабочая база/пользовательские данные не удалялись.

## 1. Границы и восстановление контекста

Фактическая база работы: чистое дерево, HEAD 7c10f83 «7 etap», предшествующие коммиты
2410ae0/01893f4/d8d54c5/fbd04ee/60aa75b/dd3f24c. Изучены AGENTS, TECHNICAL_SPEC,
DESIGN_SPEC, TASKS, DECISIONS, PROGRESS, PERSISTENCE, STORAGE_ARCHITECTURE,
FILE_OPERATIONS, отчёты и ручные инструкции 04–07, ASSETS_README и PNG-концепт.
Сверены четыре проекта, csproj, DI, модели, миграции, репозитории, identity/session/KDF/lockout,
path-policy, Windows handles, CRUD/import/editor/XML/ZIP, staging/recovery, Views/VM/styles и тесты.
Презентации «7 8 - угрозы Python» и «9 10 - безопасный FileManager практика» в репозитории
не найдены (поиск ppt/pptx/pdf); их чтение не заявляется. Реализация остаётся C#/.NET 9.

Модель угроз: проверки сервисов защищают от обходов через приложение. Приложения могут
работать под одним Windows token. Пользователь с прямым доступом к SQL/Storage, владелец
процесса и администратор ОС/SQL не являются изолированными противниками данной WPF-архитектуры.
Нет NTFS-изоляции app-пользователей, криптографической цепочки аудита или серверного enforcement.
Формальный риск допустимости WPF/SQL Server преподавателем PRE-03 остаётся открытым.

## 2. Карта предыдущих компонентов

| Компонент | Фактическое состояние до 08 и переиспользование |
|---|---|
| Core User/FileRecord/Operation, enums, nullable FileId/UserId, TargetPathSnapshot | Работают, UTC и soft-delete; не менялись |
| AuthenticationService/RegistrationService, UserSession/Revision | KDF IdentityV3 PBKDF2, соль, production cost 600000, dummy verify, 5 ошибок/15 мин, SQL serialized counters; сохранены |
| IFileAccessPolicy/PersonalStorage/RelativePathSyntax/SafeNativeFile | Права в сервисе и native handle boundary, reparse/hard links запрещены, физические предки pin-ятся |
| FileService.Run + FileTransactionStore | SQL active User/owner lock, общий staging/commit/recovery для CRUD/editor/ZIP, fail-closed; переиспользованы диагностикой |
| Files/Operations repositories, AppDbContext | EF LINQ/параметризованные UPDLOCK/HOLDLOCK, append-only audit на уровне приложения; расширены запросами |
| DocumentValidator / общий редактор | Strict JSON, UTF-8 10 МиБ, XmlReader DTD prohibit/resolver null + complexity limits; сохранены |
| ZipEngine / IArchiveService того же FileService | Strict path/headers/CRC/actual byte budgets, staging whole-tree publish; сохранены |
| JournalView/VM | Пустая коллекция и disabled filters; теперь подключены к SQL |
| Settings и шапка | Сведения только о входе / «Просмотр недоступен»; заменены общим реальным состоянием |

Проверки не ограничены кнопками: пользователь/ревизия/active User проверяются в Core-сервисах,
ownership — также в SQL запросах. UI не принимает доверенный UserId, физический root или SQL sort string.
Права репозитория сами по себе не являются аутентификацией; публичный UI вызывает сервисы.

## 3. Реализация 08.01–08.06

| Пункт | Реализация и подтверждение | Статус |
|---|---|---|
| 08.01 | IJournalService/JournalService, OperationRepository.QueryAsync, JournalView/VM: own scope, тип/результат/локальные календарные даты/relative search, сортировка, refresh, 50 строк и Next/Previous, empty/error, удалённые snapshots. JournalServerFiltersPagesSortsAndNeverIncludesForeignOrSystemEvents, JournalDeletedHistorySurvivesAndInvalidQueriesDoNotBypassSession, WPF STA | Код/автотесты выполнены; ручной UI открыт |
| 08.02 | Карта ниже; 14 существующих типов, регистрация прежним DIRECTORY_CREATED/PERSONAL_STORAGE_PREPARED, блокировка LOGIN_FAILED/LOGIN_LOCKED. Details фиксированные; неизвестный historical Details отсекается сервисом. JournalRedactsUnrecognizedHistoricalDetailsAndAuditsDeniedRead, SqlPayloadFileNameIsLiteralAndAuditContainsNoDocumentPayloadOrExternalSource | Проверено автоматически/кодом |
| 08.03 | EF LINQ, FromSqlInterpolated; fixed enum/bool sort; current session, active User, owner filter, strict path/handles; реальные сервисные SQLi/access/path/XML/ZIP регрессии | Проверено в границах модели угроз |
| 08.04 | IStorageDiagnostics реализован тем же FileService: bounded non-destructive traversal, Files incl soft-delete, history checks, pending plans/SQL confirmation; старое Recover/Synchronize сохранено | Код/автотесты выполнены; ручная приёмка открыта |
| 08.05 | Негативные сценарии прежних 140 + новые security/journal/diagnostic/health тесты; см. матрицу | Проверено автоматически; специальные физические ограничения указаны |
| 08.06 | AuditHealth/AuditProbe/AuditStatusViewModel, общие DI aliases в шапке/журнале/настройках, probe SELECT+INSERT+rollback; реальные SQL/identity/file/audit сбои меняют статус; stale через 2 мин | Код/автотесты выполнены; ручная UI-приёмка открыта |

### Журнал и время

JournalService не принимает UserId: только критерии. Перед запросом проверяет текущую
сессию/active SQL User, после await — ревизию. System UserId=NULL и чужие записи не возвращаются.
Отказ чтения журнала аудитируется ACCESS_DENIED/JOURNAL_ACCESS_DENIED, без чужого FileId.
Ошибки соединения дают безопасный InfrastructureError, старые строки UI очищаются.
Запросы выполняют Where/OrderBy/Skip/Take на сервере; PageSize <=100, UI 50; max Page=10000.
Take(size+1) определяет HasMore без загрузки таблицы или полного COUNT. Время UTC в БД
не меняется, UI использует ToLocalTime; даты включают локальный конечный день и передаются
как полуинтервал UTC. Equal-time порядок стабилизируется Id. Между страницами нет общего
snapshot: параллельные вставки могут сдвигать offset. Фильтры применяются явной кнопкой.
Поиск только по относительному снимку объекта, не по Details/тексту/секретам.

Пользовательский сервис отдаёт лишь разрешённые диагностические коды, безопасный путь
и собственное имя; неизвестные historical Details не выдаются. Описания UI фиксированные,
не raw exception/parser messages. Исторические исходные коды SQL не переписываются:
FILE_TX_* нужен старому recovery. В UI нет edit/delete/export-all/admin операций.

### Состояние аудита

IAuditHealth — общий snapshot Unknown/Checking/Active/Unavailable с UTC evidence time.
Проба под действующей сессией проверяет SQL SELECT и реальную INSERT/constraints/triggers,
затем rollback. Схема и записи сохраняются, фиктивного события не остаётся. Это не простая
проверка существования Operations. Проба не доказывает будущий commit любой операции:
реальные Files/Operations/identity по-прежнему обязаны пройти собственную транзакцию.
Succeeded после реального commit/standalone audit подтверждает доступность. Известная
ошибка остаётся Unavailable до новой успешной полной пробы, даже если сообщение об ошибке
удалось записать. Version защищает от поздней пробы, перекрывающей более свежий сбой.
После 2 минут подтверждение устаревает; UI timer 15 с только обновляет надпись, не делает
фоновых SQL-запросов. Смена сессии сбрасывает состояние, фильтры, строки и диагностику.

### Неразрушающая диагностика и восстановление

Диагностика проходит прежний Run/owner SQL transaction/workspace lock/revision/path-policy.
Это не параллельная система IO. Использует List/Inspect/Pending и bounded SQL projection:
до 10001 записи для обнаружения лимита 10000, до 10000 физических объектов.
Известный тип/размер/mtime сравниваются. Reparse/unsafe не обходятся. Неизвестные объекты
не присваиваются. Удалённый путь с новой активной Files не считается ошибкой старой записи.
При усечённой выборке нельзя объявлять объект вне БД: выводится METADATA_NOT_CHECKED.
Ошибки/лимиты обхода маркируют отчёт неполным, а не успешной полной проверкой.

| Код | Что обнаружено / действие |
|---|---|
| UNTRACKED | Физический объект без Files; автоматического insert нет |
| MISSING | Активная Files без физического объекта; запись не удаляется |
| METADATA_CHANGED | Тип/размер/mtime отличаются; данные не переписываются |
| DELETED_PRESENT | Только soft-deleted запись, но объект существует; не удаляется |
| UNSAFE_OBJECT / DIRECTORY_UNAVAILABLE | Ссылка/небезопасный/недоступный объект; не обходится, полнота ограничена |
| PENDING_OPERATION | Сохранился проверяемый план; дополнительно PENDING_COMMITTED/UNCOMMITTED по прежнему FILE_TX_* correlation |
| INCOMPLETE_STAGING | Повреждённый/неполный план, неизвестный служебный файл/каталог; не очищается |
| AUDIT_METADATA_MISMATCH | Нет соответствующего успешного mutation audit, либо последний FILE_DELETED противоречит active Files |
| METADATA_NOT_CHECKED | Предел SQL-выборки; нет вывода об отсутствии метаданных |

Для ZIP потомки связываются с ZIP_EXTRACTED/relative target, поскольку прежний FileId
события указывает на архив. Сравнение явно COLLATE Latin1_General_100_CI_AS, согласованное
с Files; schema/migrations не изменяются. Это диагностическая эвристика, не доказательство
целостности всех historical transitions. Direct SQL tampering под привилегиями не покрыто.
Содержимое одинакового размера/mtime невозможно сравнить с историей без stored baseline;
диагностика это не обещает. Editor Save отдельно проверяет SHA-256/identity/RowVersion.
Служебный workspace при первом открытии может подготовить marker/lock по прежнему протоколу;
диагностика не исправляет пользовательские файлы, Files или историю и не вызывает Settle.
Её собственный FILE_READ/STORAGE_DIAGNOSED либо STORAGE_DIAGNOSIS_PARTIAL сохраняется в Operations.

Исправление — только прежние явные команды. Synchronize требует подтверждения, не удаляет
физические неизвестные файлы. Recover проверяет plan/identity/hash и SQL correlation, завершает
committed либо компенсирует только собственный неизменённый uncommitted результат. Подмена
или недоступная БД дают RecoveryRequired, неизвестные данные сохраняются для ручного разбора.
Общей атомарной транзакции SQL+NTFS нет. Полный power-loss recovery не гарантируется.

## 4. Фактическая карта событий

| Действие | Operations | Безопасные сведения / подтверждение |
|---|---|---|
| Регистрация | DIRECTORY_CREATED / Success | PERSONAL_STORAGE_PREPARED, Guid, UTC; RegistrationService + RegisterStoresOnlyHashPreparesGuidDirectoryAndRejectsDuplicate |
| Вход | LOGIN_SUCCESS / Success | LOGIN; identity commit предшествует Session.Start |
| Неверный/неизвестный/неактивный вход | LOGIN_FAILED / Failed | INVALID_PASSWORD_COUNTED или INVALID_CREDENTIALS, неизвестный UserId=NULL, не raw invalid login payload |
| Достижение блокировки/повторный заблокированный вход | LOGIN_FAILED / Failed | LOGIN_LOCKED; прежний тип, не новая миграция |
| Выход | LOGOUT / Success | LOGOUT; при сбое SQL подтверждения нет, но локальная сессия всегда очищается |
| Подготовка/ремонт личного корня | DIRECTORY_CREATED / Success | PERSONAL_STORAGE_REPAIRED; failure OPERATION_FAILED |
| Файл / папка / импорт | FILE_CREATED / DIRECTORY_CREATED | FILE_TX_Create/Directory/Import; source absolute path и содержимое не журналируются |
| Список/открытие/перечитывание | FILE_READ | DIRECTORY_LISTED/TEXT_READ/EDITOR_RELOADED; собственный относительный путь |
| Сохранение | FILE_UPDATED / Success | FILE_TX_SAVE; с Files в общей транзакции |
| Удаление файла/пустой папки | FILE_DELETED / Success | FILE_TX_DELETE; soft-delete и snapshot оставляют историю |
| Явная сверка | FILE_UPDATED / Success | METADATA_SYNCHRONIZED; даже soft-delete отсутствующего объекта не стирает историю |
| JSON/XML корректен/ошибочен | JSON_VALIDATED / XML_VALIDATED Success/Failed | VALID/JSON_SYNTAX/XML_SYNTAX/XML_DTD/EMPTY/SIZE/ENCODING/COMPLEXITY, без parser payload |
| Ошибочная/опасная структура | OPERATION_FAILED / Failed | DOCUMENT_* или FILE_COMMAND_*; невалидный Save не публикует изменения |
| Создание/распаковка ZIP | ZIP_CREATED/ZIP_EXTRACTED / Success | FILE_TX_ZIP_*, correlation нужен recovery; имена entry и содержимое в Details не пишутся |
| Просмотр ZIP | FILE_READ/Success либо OPERATION_FAILED/Denied | ZIP_PREVIEWED/ZIP_PREVIEW_REJECTED, без извлечения |
| Нарушение path/ownership/ZIP Slip | ACCESS_DENIED / Denied | FILE_COMMAND_Denied/Invalid/ArchiveUnsafe, без foreign FileId/raw path |
| ZIP Bomb/IO/SQL/отмена/конфликт | OPERATION_FAILED / Failed | FILE_COMMAND_*; при отказе самой БД запись не обещается |
| Восстановление | FILE_UPDATED / Success | RECOVERY_FINALIZED/RECOVERY_ROLLED_BACK |
| Диагностика | FILE_READ / Success | STORAGE_DIAGNOSED/PARTIAL; success означает завершение запроса, не отсутствие расхождений |
| Отказ просмотра журнала | ACCESS_DENIED / Denied | JOURNAL_ACCESS_DENIED, nullable actor |

Клиентская проверка формы регистрации до обращения к хранилищу не создаёт аккаунт/аудит;
конфликт занятого логина не является успешной регистрацией. Не каждое нажатие фильтра —
новая audit operation. Нет скрытого неограниченного повторения файловых callbacks.
Ошибочная валидация и OPERATION_FAILED — два разных предусмотренных события, а не дубли успеха.
При откате успешные промежуточные события откатываются вместе с Files; failure записывается отдельно.
SQL failure, делающий запись невозможной, возвращает неподтверждённый результат и состояние,
а не вымышленную запись. Аудит не защищён от администратора SQL.

## 5. Таблица угроз и механизмов

| Угроза | Защита | Класс | Автотест / результат |
|---|---|---|---|
| SQLi | LINQ и FromSqlInterpolated, enum/bool sort | repositories, IdentityStore, FileTransactionStore | JournalServerFiltersPagesSortsAndNeverIncludesForeignOrSystemEvents, SqlInjectionLoginIsLiteralAndLockoutCodeRevealsNoPassword, PersistenceTests — проходят |
| Пароли/brute force | IdentityV3 salt/PBKDF2, dummy, serialized lockout | PasswordSecurity, AuthenticationService | PasswordTests, FiveFailuresBlockForFifteenMinutesAndSuccessResets, ConcurrentFailuresAcrossServiceInstancesDoNotLoseCounterUpdates — проходят |
| Ownership/session | active SQL user, owner filter, revision after awaits | FileService, UserStorageService, JournalService | ForeignIdsAndSessionChangeCannotReadWriteDeleteOrLeakResults, JournalDiscardsInFlightResultAfterSessionSwitch — проходят |
| Traversal/ADS/links | strict names/relative paths, handles, no reparse/hardlinks | RelativePathSyntax, PersonalStorage, SafeNativeFile | InvalidNamesTraversalAndRootDeletionAreRejected, JunctionHardLinksAndAncestorReplacementAreRejectedByFileAdapter — проходят |
| XXE/Billion Laughs | DTD Prohibit, XmlResolver=null, bounded reader | DocumentValidator | ExternalEntitiesCannotReadExclusivelyLockedLocalFileOrConnectToLoopback, BillionLaughsRejectedBeforeExpansion — проходят |
| ZIP Slip | strict entries, collision/special type checks, handles | ZipEngine/FileService.Archives | ZipEngineTests, UnsafeZipDoesNotPublishAnythingOrRegisterSuccess — проходят |
| ZIP Bomb | metadata и actual byte/ratio/CRC guards | ZipEngine | StreamingGuardsActualEntryTotalRatioAndDeclaredBytesBeforeWriting, DeclaredLimitsAndCrcMismatchLeaveNoFinalTree — проходят |
| TOCTOU | owner SQL lock + workspace lock + pinned ancestors + checked handles + versions | FileService/WindowsFileStorage | ConcurrentSavesAndDeleteDuringLockedReadPreserveData, AmbiguousCommitIsVerifiedAndNeverRollsBackCommittedUserData — проходят |
| Частичный сбой | durable plan/backup/staging/compensation, no overwrite | FileTransactionStore/Workspace | AuditFailureRollsBackPhysicalSaveAndCreationAndKeepsHistory, RealAuditFailureUpdatesHealthAndDeleteRestoresOriginalFile — проходят |
| Утечка журнала | safe codes, projection, scoped query, no raw exception | SecurityAudit/JournalService/AuditListItem | JournalRedactsUnrecognizedHistoricalDetailsAndAuditsDeniedRead, JournalProjectionNeverDisplaysRawExceptionDetailsOrAbsolutePaths — проходят |
| Ложный Active | реальный rollback probe + actual commit/error feedback + freshness/version | AuditHealth/AuditProbe | AuditProbeRollsBackAndDetectsInsertFailureThenRecovers, HealthExpiresLatchesFailuresAndDoesNotLetOldProbeHideWriteFailure — проходят |

## 6. Матрица 60 требуемых негативных сценариев

Ссылки на имена тестов ниже — исполняемые xUnit, не утверждение о 60 новых отдельных методах.
Комбинированные тесты/теории проверяют несколько случаев; прежние тесты не дублировались.

| № | Сценарий | Подтверждение |
|---|---|---|
| 1–3 | Неверный пароль, блокировка, нет пользователя | WrongUnknownInactiveAndLockedLoginsUseSameMessageAndAuditNullableFile; FiveFailuresBlockForFifteenMinutesAndSuccessResets |
| 4–7 | Нет сессии, logout, смена пользователя, чужое хранилище | AccessPolicyRejectsForeignIdPathsAndUnauthenticatedCallsWithoutTouchingFiles; LogoutClearsSingletonViewModelsAndTwoUsersHaveSeparateRoots; ForeignIdsAndSessionChangeCannotReadWriteDeleteOrLeakResults |
| 8 | SQLi login | SqlInjectionLoginIsLiteralAndLockoutCodeRevealsNoPassword |
| 9 | SQLi filename | SqlPayloadFileNameIsLiteralAndAuditContainsNoDocumentPayloadOrExternalSource |
| 10–12 | Journal search/filter/sort | JournalServerFiltersPagesSortsAndNeverIncludesForeignOrSystemEvents; JournalDeletedHistorySurvivesAndInvalidQueriesDoNotBypassSession; WPF JournalUi |
| 13 | Invalid IDs | ForeignIdsAndSessionChangeCannotReadWriteDeleteOrLeakResults; Journal query criteria validation |
| 14–16 | Traversal/absolute/foreign user | InvalidNamesTraversalAndRootDeletionAreRejected; AccessPolicyRejectsForeignIdPathsAndUnauthenticatedCallsWithoutTouchingFiles |
| 17–19 | Symlink/junction/reparse | Реальная junction/hard link: JunctionHardLinksAndAncestorReplacementAreRejectedByFileAdapter; архивные symlink attrs: SymlinkAndReparseAttributesAreRejected. Отдельная физическая file-symlink не создавалась; это ограничение, не пройденный отдельный native тест |
| 20–21 | Normalized conflicts/root delete | DuplicateAndConcurrentCaseInsensitiveCreateNeverOverwrite; InvalidNamesTraversalAndRootDeletionAreRejected |
| 22–26 | Malformed XML/DTD/entities/local/network | StrictSyntaxAndEmptyDocuments; ExternalEntitiesCannotReadExclusivelyLockedLocalFileOrConnectToLoopback; ForbiddenXmlRejectedAtOpenWhileMalformedDocumentsCanBeCorrected |
| 27–28 | Billion Laughs/XML limits | BillionLaughsRejectedBeforeExpansion; DepthNodeAttributeByteAndUnicodeLimitsAreBounded |
| 29–33 | ZIP traversal/absolute/UNC/collision/links | DangerousWindowsPathsAreRejectedBeforeExtraction; DuplicateAndFileDirectoryCollisionsAreRejected; SymlinkAndReparseAttributesAreRejected; UnsafeZipDoesNotPublishAnythingOrRegisterSuccess |
| 34–37 | ZIP count/entry/total/ratio | EnvelopeBoundsEntryCountBeforeZipArchiveAllocation; DeclaredEntryTotalAndRatioLimitsAreChecked; StreamingGuardsActualEntryTotalRatioAndDeclaredBytesBeforeWriting |
| 38–40 | Bad ZIP/staging/preservation | CrcAndTruncatedOrForgedDataAreRejected; ZipPublishIoFailureCompensatesTreeAndRetrySucceeds; ExtractAuditFailureRollsBackAllMetadataAndPublishedTree |
| 41–42 | Concurrent create/save | DuplicateAndConcurrentCaseInsensitiveCreateNeverOverwrite; ConcurrentSavesAndDeleteDuringLockedReadPreserveData |
| 43–44 | Delete during read/external change | ConcurrentSavesAndDeleteDuringLockedReadPreserveData; EditorConflictAndExplicitReloadPreserveUnsavedBufferUntilConfirmed |
| 45 | DB create failure | MissingSqlCannotPerformFilesystemChanges; AuditFailureRollsBackPhysicalSaveAndCreationAndKeepsHistory |
| 46–47 | DB delete/audit failure | RealAuditFailureUpdatesHealthAndDeleteRestoresOriginalFile; AuditFailureRollsBackLoginAndLogoutStillClearsSession |
| 48–50 | FS/disk-full/extract failure | FilesystemFailureAfterPublishIsCompensatedAndSessionRevocationIsChecked; SimulatedDiskFullAfterPublishCompensatesArchiveAndPreservesSources; ZipPublishIoFailureCompensatesTreeAndRetrySucceeds |
| 51–53 | Own/foreign/scoped filter | JournalServerFiltersPagesSortsAndNeverIncludesForeignOrSystemEvents; JournalDiscardsInFlightResultAfterSessionSwitch |
| 54 | Deleted history | JournalDeletedHistorySurvivesAndInvalidQueriesDoNotBypassSession |
| 55–56 | Errors/denied audit | JournalVmDisplaysDatabaseFailureAndDoesNotKeepOldRows; JournalRedactsUnrecognizedHistoricalDetailsAndAuditsDeniedRead |
| 57–58 | No password/content | SqlInjectionLoginIsLiteralAndLockoutCodeRevealsNoPassword; SqlPayloadFileNameIsLiteralAndAuditContainsNoDocumentPayloadOrExternalSource; JournalProjectionNeverDisplaysRawExceptionDetailsOrAbsolutePaths |
| 59–60 | DB UI/status truth | JournalAndDiagnosticsMissingDatabaseFailClosedAndHealthIsUnavailable; AuditProbeRollsBackAndDetectsInsertFailureThenRecovers; HealthExpiresLatchesFailuresAndDoesNotLetOldProbeHideWriteFailure; WPF STA bindings |

## 7. Обнаруженные дефекты и исправления

1. Журнал был незавершённой заготовкой, фильтры disabled. Реализована реальная выборка;
   это недостающая функция, не объявленная выдуманной уязвимостью.
2. Статус настроек описывал лишь проверку при входе, а шапка не давала реального статуса.
   Добавлены общая health-модель/проба/feedback и исключение stale Active.
3. Блокировка входа уже действовала, но её событие не имело отдельного безопасного кода.
   LOGIN_LOCKED в прежнем LOGIN_FAILED, без изменения KDF/счётчиков/схемы.
4. Pending() не замечал неизвестный файл непосредственно в .sfm-internal. Теперь такой
   файл блокирует обычные операции и показывается диагностикой; сам файл сохраняется.
5. При разработке нового cross-table запроса обнаружен collation conflict: Files задаёт
   Latin1_General_100_CI_AS, snapshot Operations наследует default Cyrillic_General_CI_AS.
   Исправлено выражение сравнения через fixed EF.Functions.Collate, без миграции.
6. При новом layout-тесте раскрытая панель фильтров оставляла слишком мало места строкам.
   Ограничена высота её ScrollViewer, Apply и пагинация остаются снаружи; есть regression assertion.

Новых известных критических обходов внутри заявленной границы не обнаружено. Отсутствие
найденных обходов/предупреждений не является доказательством полной безопасности.

## 8. Проверки и результаты

Windows 10, SDK 9.0.200, VS 2022 17.13.35818.85; SQLEXPRESS Running. Четыре проекта
с прежними TFM и EF/DI 9.0.22, xUnit 2.9.2. Новых NuGet-пакетов/миграций нет.

- `dotnet restore SecureFileManager.sln` — успешно; в sandbox SDK недоступен, разрешённый
  запуск с Windows SDK вне sandbox прошёл.
- `dotnet build SecureFileManager.sln -c Debug --no-restore` — успешно, 0 warnings/errors.
- `dotnet build ... -p:RunAnalyzersDuringBuild=true -p:AnalysisLevel=latest` — 0 warnings/errors.
- `dotnet list SecureFileManager.sln package --vulnerable --include-transitive` — для всех
  четырёх проектов известных уязвимых пакетов в текущих источниках не обнаружено; версии не менялись.
- Первый full: 153 passed / 3 failed / 0 skipped; collation issue диагностики исправлен.
- После исправления: полный `scripts/Test-Stage08.ps1` — 159 passed / 0 failed / 0 skipped, 36 с.
- Последующий WPF layout regression выявил недостаток места при раскрытых фильтрах;
  после корректировки точечный WPF STA повтор прошёл. Финальный полный результат уточняется ниже.
- Новые 20 тестов (15 SQL + 5 unit), прежние 140 проходят. WpfShellTests дополнен
  реальными Views/resources/DI, readonly grid, server paging/filter/date bindings, status и session reset.
- SQL: тесты создают только собственную новую GUID-БД, проверяют отсутствие до создания,
  fixture удаляет только её. Script создаёт/очищает только свой run-GUID без следования links.
  Исходные чужие тестовые базы и рабочий Storage не чистились. Тестовый disk-full — контролируемый IOException,
  не заполнение накопителя. Missing database/SQL triggers имитируют отказ без остановки общего Engine.
- Read-only рабочая SecureFileManagerDB: 2 migrations, encrypt TRUE, NTLM, Users=2,
  Files=9, Operations=117 на момент проверки. Только агрегаты/без хешей; конфигурация прежняя,
  StorageRoot = D:/SecureFileManagerData/Storage. Ошибка ODBC/credentials внутри sandbox снята
  разрешённым запуском того же SELECT; TLS не отключался.
- `dotnet run --project src/SecureFileManager.Wpf --no-build --launch-profile "SecureFileManager (Development)"`
  открыл native окно входа. Computer-use прочитал «Подключение к БД проверено. Вход требует
  проверки пароля и аудита». Пароль не вводился. Авторизованный native GUI не проверен:
  навык computer-use запрещает автоматизацию auth dialogs. Приложение оставлено на входе;
  перед обычной пересборкой закрыть этот проверочный экземпляр. Финальные артефакты проверяются
  в artifacts/stage08-verify, чтобы не закрывать окно и не блокироваться на exe.
- WPF RenderTargetBitmap: два размера 1024×640/1440×900, масштаб bitmap 100%/150%,
  populated journal/expanded filters, binding traces чистые; это не фактическая смена Windows DPI.
  Избранные PNG и README — docs/screenshots/stage08; полный набор — ignored artifacts/stage08-renders.
- `git -c core.safecrlf=false diff --check` используется для проверки whitespace.

Автоматический review дважды ошибочно применил старый запрет на этап 08 к SQL-скрипту.
После отдельного прямого подтверждения пользователя «Разрешаю запуск тестов этапа 08»
тот же скрипт разрешён; обход отказа не использовался. Сейчас блокера автоматического approval нет.

## 9. Ограничения и следующие ручные проверки

Нужны native authenticated journal/settings/diagnostics, keyboard, реальные 100%/150% DPI,
ручная сверка SQL UI по STAGE08_MANUAL_TEST.md. Это основание оставить 08.01/08.04/08.06
открытыми и весь этап Частично до приёмки, без начала этапа 09.

Специально не воспроизводились: реальное заполнение диска, снятие D: во время записи,
power loss, отдельный physical file-symlink, произвольные аппаратные/сетевые сбои.
Есть безопасные doubles, реальные junction/hard links, SQL triggers и missing DB.
Locked-file тест доказывает отсутствие успешного внешнего чтения XML; отсутствие resolver
и Prohibit обеспечено кодом, loopback listener подтверждает отсутствие обращения к сети.
Гонки с прямым внешним изменением после диагностики/администратором исключить нельзя;
отчёт — snapshot, перед мутацией действуют собственные повторные handle/version проверки.

Dependency scan зависит от данных источника в момент запуска и не заменяет update среды.
TFM/SDK/SQL patching не менялись; их плановое обновление отдельно. Полная проверка historical
целостности без внешнего baseline/подписи/серверной границы не заявляется.

## 10. Изменённые/новые файлы

Полный перечень этого незакоммиченного изменения приводится в конце документа после финальной проверки.

## Уточнение результатов защиты входа

Существующий failed-login snapshot для несуществующего имени мог сохранять до 50 символов
пунктуации из подозрительного запроса, если вход проходил общий синтаксический validator логина.
Это не SQL injection (параметризация уже работала), но избыточное сохранение raw input.
Теперь unknown ActorNameSnapshot допускает только буквы/цифры/._-, иначе NULL. Само событие
LOGIN_FAILED остаётся, существующий подтверждённый пользователь сохраняет собственное имя.
SqlInjectionLoginIsLiteralAndLockoutCodeRevealsNoPassword проверяет также отсутствие такого payload
в системных actor snapshots. Существующие исторические записи не удалялись и не переписывались.

## Финальный результат

Финальный код (включая фильтры, unknown-login snapshot и DI assertions) проверен:
`dotnet build SecureFileManager.sln -c Debug --artifacts-path artifacts/stage08-verify --no-restore -p:RunAnalyzersDuringBuild=true -p:AnalysisLevel=latest`
— 0 ошибок/предупреждений;
`./scripts/Test-Stage08.ps1 -ArtifactsPath <repo>/artifacts/stage08-verify`
— **160 passed, 0 failed, 0 skipped**, 34 с, TRX counters проверены.
Прежние140 сохранены, новые20 проходят. Workspace root/DB/schema/packages не изменены.
Общий статус — **Этап 08: Частично (ручная приёмка нового UI открыта)**.
Этап 09 не начат, коммит не создавался.

## Полный список файлов текущего изменения

Изменённых: 23; созданных: 17; всего: 40. Статус M/A ниже относится к рабочему дереву, коммит не создавался.

### Core

- A `src/SecureFileManager.Core/Files/StorageDiagnostics.cs`
- M `src/SecureFileManager.Core/Interfaces/IFileRepository.cs`
- M `src/SecureFileManager.Core/Interfaces/IOperationRepository.cs`
- A `src/SecureFileManager.Core/Journal/AuditHealth.cs`
- A `src/SecureFileManager.Core/Journal/JournalContracts.cs`
- A `src/SecureFileManager.Core/Journal/JournalService.cs`
- M `src/SecureFileManager.Core/Services/AuthenticationService.cs`
- M `src/SecureFileManager.Core/Services/SecurityAudit.cs`

### Data

- M `src/SecureFileManager.Data/DependencyInjection/DataServiceCollectionExtensions.cs`
- M `src/SecureFileManager.Data/Files/FileTransactionStore.cs`
- A `src/SecureFileManager.Data/Identity/AuditProbe.cs`
- M `src/SecureFileManager.Data/Identity/IdentityStore.cs`
- M `src/SecureFileManager.Data/Repositories/FileRepository.cs`
- M `src/SecureFileManager.Data/Repositories/OperationRepository.cs`

### Wpf

- M `src/SecureFileManager.Wpf/App.xaml.cs`
- M `src/SecureFileManager.Wpf/Infrastructure/FileSystem/WindowsFileStorage.cs`
- A `src/SecureFileManager.Wpf/ViewModels/AuditStatusViewModel.cs`
- M `src/SecureFileManager.Wpf/ViewModels/JournalViewModel.cs`
- M `src/SecureFileManager.Wpf/ViewModels/MainWindowViewModel.cs`
- M `src/SecureFileManager.Wpf/ViewModels/SettingsViewModel.cs`
- M `src/SecureFileManager.Wpf/Views/JournalView.xaml`
- M `src/SecureFileManager.Wpf/Views/MainWindow.xaml`
- M `src/SecureFileManager.Wpf/Views/SettingsView.xaml`

### Tests

- A `tests/SecureFileManager.Tests/JournalIntegrationTests.cs`
- A `tests/SecureFileManager.Tests/JournalTests.cs`
- M `tests/SecureFileManager.Tests/WpfShellTests.cs`

### Documentation

- M `docs/DECISIONS.md`
- M `docs/FILE_OPERATIONS.md`
- M `docs/PROGRESS.md`
- A `docs/screenshots/stage08/05-1024x640-render.png`
- A `docs/screenshots/stage08/05-1440x900-render.png`
- A `docs/screenshots/stage08/06-1024x640-render.png`
- A `docs/screenshots/stage08/journal-populated-filters-False-150pct-render.png`
- A `docs/screenshots/stage08/journal-populated-filters-True-150pct-render.png`
- A `docs/screenshots/stage08/README.md`
- A `docs/STAGE08_MANUAL_TEST.md`
- A `docs/STAGE08_SECURITY_AUDIT.md`
- M `docs/STORAGE_ARCHITECTURE.md`

### Scripts / root

- A `scripts/Test-Stage08.ps1`
- M `TASKS.md`

### Финальная регрессия истинности статуса

Дополнительно обнаружен крайний случай нового health feedback: успешный commit пустого
Recover мог выставить Active без INSERT в Operations. Исправлен через ObservedAudit того же
OperationRepository: только успешный Add + подтверждённый SQL commit дают Succeeded.
Новая проверка EmptyFileTransactionDoesNotClaimAuditWriteAvailability проверяет пустой SQL work,
пустой Recover и последующее действительное создание файла. Последний full — 160/160,
0 failed/0 skipped, 34 с; .NET analyzers build — 0/0. Рабочие агрегаты повторно SELECT после
тестов не изменились: migrations2/Users2/Files9/Operations117. Этот итог заменяет прежние159
как окончательный результат; предыдущие прогоны сохранены выше как история разработки.

## Ручное замечание 03.11.2026: описание события

Пользователь проверил журнал вручную: общий вид нормальный, но длинное описание
обрезалось внутри фиксированного столбца даже в конце горизонтальной прокрутки.
JournalView исправлен: перенос строк, автоматическая высота строки с минимумом48,
подсказка с полным безопасным Description. Строка не раскрывает исходный raw Details.
WpfShellTests проверяет многострочный layout после перехода к последнему столбцу,
высоту строки и полный tooltip; финальный точечный прогон1/1, без skip,4с.
Полная прежняя регрессия160/160 относится к предыдущему коду; после этого локального
XAML-исправления повторён только затронутый WPF test. Нужен ручной повтор после пересборки.
Этап08 пока Частично,09 не начат. SQL/schema/storage не менялись, коммита нет.

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

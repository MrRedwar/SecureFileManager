# Матрица Windows → Ubuntu Web

Дата07.11.2026,baseline `9515151`. «Да» = реализация в исходниках,не подтверждение нового
GUI-прогона. Весь столбец Ubuntu — **план**,ни одна функция Web сейчас не реализована.
Реальные пределы и доказательства — [CURRENT_FUNCTIONALITY_AUDIT](CURRENT_FUNCTIONALITY_AUDIT.md).

Обозначения тестов: существующие файлы находятся в `tests/SecureFileManager.Tests/`:
AUTH=SecurityIntegrationTests/PasswordTests;PERSIST=PersistenceTests;
FILES=FileServiceTests;DOC=DocumentValidationTests/DocumentEditorIntegrationTests;
ZIP=ZipEngineTests/ArchiveIntegrationTests;JOURNAL=JournalTests/JournalIntegrationTests;
BROWSE=ComputerBrowserTests/ComputerBrowserIntegrationTests;EXTDOC=ExternalDocumentIntegrationTests;
EXTOP=ExternalOperationIntegrationTests;EXTZIP=ExternalArchivesTrashIntegrationTests;
UI=WpfShellTests/NavigationTests. «Новый» означает будущий тест,не старый PASS.

| Функция Windows | Реализована сейчас | Повторное использование | Реализация в Ubuntu | Нужные изменения | Тест |
|---|---|---|---|---|---|
| Регистрация login3–50,password10–128,confirmation | Да | RegistrationService/IdentityValidation | Razor/HTTP POST форма | CSRF/rate-limit/scoped actor | AUTH + новый web registration |
| GUID user и личный каталог | Да | User/UserStorageService | volume Users/GUID | Linux provisioner,OS permissions | AUTH/StorageConfiguration + Linux roots |
| Unique username CI/AS | Да,SQL collation | правило/validation | normalized login key/unique | точная Unicode policy,MySQL mapping | PERSIST + real MySQL duplicate/race |
| PBKDF2 IdentityV3/salt600000/rehash | Да | PasswordSecurity | тот же формат,server-side verify | обновить пакет,не пересчитывать без пароля | PasswordTests + old hash Linux login |
| Единая ошибка unknown/inactive/wrong/locked | Да | AuthenticationService | cookie login endpoint | safe HTTP response,rates | AUTH + web uniform error |
| Sliding lockout5/15min,lock15min | Да | LoginProtectionPolicy/алгоритм | MySQL serialized transaction | FOR UPDATE/error dialect | AUTH concurrent + MySQL lockout |
| Вход только после audit commit | Да | transaction decision | cookie выдаётся после commit | cookie/session adapter | AUTH audit rollback + no cookie |
| Выход даже при отказе audit | Да | намерение fail closed | revoke server session/delete cookie | circuit/other tabs invalidation | AUTH + web multi-tab logout |
| Session Current/Revision | Да,desktop singleton | контракт/late-response rule | HTTP/circuit scoped actor | singleton не переносить | AUTH/UI + two browser users |
| Очистка фильтров/выбора/editor при logout | Да | поведение | scoped state reset | Blazor lifecycle/revocation | UI/EXTDOC + E2E |
| Диски name/label/type/FS/capacity/free/% | Да,DriveInfo | DriveSnapshot/display | только предоставленные locations/statvfs | Linux mount catalog,не container rootfs | DriveTests + mount fixture |
| Неготовый/недоступный диск | Да | safe statuses | Unavailable/marker mismatch | no fallback mkdir | DriveTests + absent mount |
| Просмотр network drive cards | Возможно DriveInfo перечисляет | DTO | сетевые FS не baseline | явно отдельная разрешённая location при будущем scope | новый no-network/default deny |
| Навигация внешнего диска | Да,local only | ComputerBrowseService | location browser | каталог mounts/grants | BROWSE + real Linux dirs |
| Address absolute Windows path | Да,ограниченный X:\ | navigation intent | logical location:/relative | raw host path не доверять | BROWSE + forged root/ID |
| Этот компьютер/root/up/refresh | Да | DTO/navigation rules | location root/up/refresh | Razor buttons/state | UI/BROWSE + E2E |
| Enter/double click/Open folder | Да | поведение | HTML row activation/keyboard | accessibility/focus | UI + browser keyboard |
| Готовый USB/removable | Через local drive rules;отдельная physical приёмка не доказана | DTO | pre-mounted explicitly exposed root | VBox passthrough/operator mount/marker | новый manual USB/absence |
| Автомонтирование/форматирование устройств | Нет | отсутствует | не baseline;optional host-agent | отдельное задание/permissions | отдельная acceptance при запросе |
| External list10000/truncated warning | Да | FileRules/ComputerDirectory | bounded list и warning | Linux reader/grant filtering | BROWSE + >10000 fixture |
| Metadata name/type/bytes/mtime/note | Да | DTO/FileDisplay | same fields + mount availability | UTC/client timezone;safe encoding | BROWSE + binary/date sorting |
| Общие external файлы всем active app users по OS token | Да | shared object identity/history | shared roots только по явным grants | deny-default,один UID не изолирует app users | EXTDOC + two users different grants |
| ACL фактического Windows token | Да,OS IO checks | принцип OS upper bound | UID/GID/POSIX/ACL | новые Linux adapters,no impersonation | EXTDOC/EXTOP + OS deny |
| Закрытые системные папки | Safe OS error,не universal blacklist | safe result | системные roots не выдаются | allowlist,skip /proc/dev/sys/etc | BROWSE + denied root |
| Запрет private Storage в external browser | Да | граница/negative scenarios | personal/state/keys/db исключены | overlap/alias/mount checks | BROWSE/EXTDOC/EXTOP/EXTZIP |
| Symlink/junction/reparse | Не следуются;metadata note | policy/tests intent | symlink/magic-link deny | openat2,fstat,root FD | FILES/BROWSE + actual Linux symlink |
| Hardlinks | File handles Links!=1 denied | policy | regular nlink>1 denied | Linux fstat | AUTH/FILES + link fixture |
| UNC/device/ADS/traversal reject | Да | negative cases | root segments/decoded HTTP validation | Windows syntax выделить,deny special files | FILES/EXTOP + encoding/FIFO/device |
| Personal list/state Managed/Untracked/Missing/Changed/Unsafe | Да | FileService.ListAsync | same reconciliation view | Linux workspace,case comparer | FILES + Linux actual FS |
| Personal breadcrumb/root/up | Да | navigation | /files component | WPF state→Blazor | UI + E2E |
| Создать personal folder | Да | FileService.CreateAsync | same | Linux CreateNew/mkdirat | FILES + no replace |
| Создать personal TXT/JSON/XML | Да | defaults/validator | same | stream/FS adapter | FILES/DOC + Linux round-trip |
| Personal create arbitrary binary | Нет в Create;есть import | импорт byte flow | оставить ограничение Create | не приписывать существующий binary editor | новый parity negative |
| Personal delete file/empty folder | Да,физическая очистка+soft-delete | FileService/store intent | то же,no recursive deletion | Linux staged delete/recovery | FILES/PERSIST + nonempty rejection |
| Personal trash/restore | Нет пользовательской команды | нет | не добавлять под видом parity | Recover!=restore deleted | новый UI absence/parity |
| Personal copy/move/rename | Нет | external алгоритмы отдельно | не обязательство personal parity | только отдельное расширение при разрешении | source inventory + no false menu |
| Personal single file Import<=100МиБ | Да,Windows picker | bounded copy/staging | browser upload в personal | ImportSelection→stream port,CSRF/budgets | FILES + upload fake length/rollback |
| Personal export/download | Нет | binary stream/authorize pattern | download attachment,новая web необходимость | stream endpoint/audit,не static root | новый HTTP download/IDOR/abort |
| Personal Synchronize выбранного объекта | Да,явная команда | FileService | same explicit action | grants/owner,Linux timestamp semantics | FILES/JOURNAL + no silent adopt |
| Personal Recover | Да | stage/correlation protocol | same with Linux plans | versioned recovery schema/fsync | FILES/ZIP + crash checkpoints |
| External create generic file/folder | Да | ExternalFileOperationService intent | arbitrary permitted empty name + folder | LocationRef/native FS/permissions | EXTOP ExternalOperationsCreateFilesFoldersAndAuditWithoutPersonalMetadata + Linux |
| External copy file/tree/empty dirs/binary | Да,один выбор | orchestration/limits | same,including cross-FS | '/' paths,root grants,stream budget | EXTOP copy/limits + real two FS |
| Copy1000nodes/100МиБfile/1ГиБtree | Да | ExternalOperationRules | same baseline limits | actual bytes/quota/disk reserve | EXTOP limits + fake length |
| External move/rename same volume | Да | metadata/GUID intent | same-FS renameat2 no-replace | EXDEV/mount identity,source+target grants | EXTOP rename/move/concurrent + Linux |
| Cross-volume move | Нет;Unsupported,copy available | explicit limitation | EXDEV→Unsupported;copy source kept | no unsafe copy+delete fallback | EXTOP cross-volume + Linux mounted FS |
| Case-only rename | Нет | limitation | baseline не обещает;Linux policy утвердить | comparer explicit | новый A.txt/a.txt/case rename |
| Overwrite/merge/self-copy | Запрещены | checks | запрещены | descriptor-relative no-replace | EXTOP conflicts + race |
| External delete tree→app trash | Да,same-volume rename | protocol/metadata/audit | same-FS .sfm-trash-ID | Linux receipts/location/auth/identity | EXTZIP ExternalTrashPreservesTreeIdsHistoryAndRestoresWithoutOverwrite + Linux |
| Restore by original full path | Да,receipt | protocol | location+logical original path | no host arbitrary path,grant recheck | EXTZIP restore/conflict/other actor |
| Multiple trash generations | Conflict,нет selector | limitation | explicit Conflict | отдельный UI не обязательство parity | новый multiple receipt case |
| Trash purge/free space | Нет | отсутствует | не вводить auto purge | текст о сохранённых bytes | EXTZIP + manual storage usage |
| External GUID/history preserved on move/trash | Да | ExternalDocumentStore intent | same stable object within location | new location-aware key/update | EXTOP/EXTZIP + MySQL identity checks |
| External operation recovery | Да | correlation/verified compensation | same,per authorized location | Linux plans/locks/no blind delete | EXTOP/EXTZIP audit faults/changed data |
| External editor recovery | Да | ExternalDocumentService | same | Linux storage/in-place durability | EXTDOC committed/uncommitted cases |
| Search filenames current loaded directory | Да,private/external | rules | same,не recursive/fulltext | explicit comparer/UI state | FILES/BROWSE + case/Unicode |
| Sort name/type/size/date | Да | numeric DTO fields | HTML table/controlled sorting | no sort formatted sizes | UI + 2KiB/10KiB/date tests |
| TXT open/view/edit/save | Да,оба режима | validator/Core services | editor textarea10МиБ | web token/Linux IO | DOC/EXTDOC + E2E |
| JSON open/view/edit/create/validate/save | Да | System.Text.Json | same | server validation every save | DOC + malformed/depth/comments |
| XML open/view/edit/create/validate/save | Да | XmlReader validation | same | no inline execution,secure server parser | DOC + XXE/DTD/limits |
| Invalid JSON/XML editable correction | Да,кроме prohibited XML | validation outcomes | show safe line/position | reject invalid save,keep buffer | DOC/EXTDOC + E2E |
| UTF8/BOM/NUL10МиБ | Да | parser/limits | same byte constraints | HTTP body/buffers/circuit budget | DOC/EXTDOC + BOM/nonASCII limits |
| YAML editor/validator | **Нет** | отсутствует | не заявлять/не включать в parity как готовый | optional отдельное задание | Supports('.yaml')==false |
| YAML/other binary copy/import/ZIP | Байтовые операции,без редактора | generic flows | same byte operations | no content execution | FILES/EXTOP/EXTZIP |
| Structured-only section | Да,JSON/XML+folders | filter intent | /formats reusable browser | Razor filter | UI + folder/extension test |
| Dirty Save/Discard/Cancel | Да | editor behavior | internal navigation/dialog + beforeunload | браузер не гарантирует custom close/save/crash | FILES/EXTDOC + browser close/reconnect |
| Reload confirmation/conflict buffer retention | Да | version/dirty contract | same,409/ETag | avoid force overwrite after concurrency | DOC/EXTDOC + two users edit |
| Ctrl+S | Да,editor binding | UX | scoped keyboard handler | prevent browser save default within editor | новый E2E keyboard |
| One editor buffer(scope,GUID) | Да,WindowsFileDialogs | identity intent | keyed tabs/route within circuit | no singleton cross-user buffers | UI/EXTDOC + simultaneous sessions |
| Binary/photo/PDF preview/native app launch | Нет | отсутствует | attachment download,не inline editor | functional alternative browser limitation | новый download headers |
| Personal multi-selection ZIP | Да | FileService.Archives | reusable ZIP source selection | Linux workspace/tree auth | ZIP round-trip/foreign IDs |
| External single-root ZIP create | Да | External service + ZipEngine | same | root relative policy/native storage | EXTZIP binary/empty-folder |
| ZIP preview | Да,declared sizes;не CRC | ZipEngine.Inspect/DTO | modal/table | authorization/version context | ZIP/EXTZIP preview version |
| ZIP extract new directory only | Да,оба режима | stream engine/orchestration | same | Linux no-replace staging | ZIP/EXTZIP tree compare |
| ZIP Slip/links/duplicates/ZIP64/encrypted reject | Да | ZipEngine | same conservative names | Linux physical output boundary | ZIP/EXTZIP malicious inputs |
| ZIP Bomb actual bytes/CRC | Да | ZipEngine limits | same | quotas/concurrency/tmpfs budgets | ZIP streaming guard/CRC + Linux |
| Private ZIP progress/cancel | Да | ArchiveProgress/ct | UI progress,cancel prepublish | operation IDs/reconnect/no repeat IO | ZIP cancel + E2E |
| External ZIP busy only | Да,без отдельной cancel | service/limits | busy + truthful final state | новая cancel лишь отдельное улучшение | UI/EXTZIP + disconnect |
| Journal actor-only type/result/date/search | Да | JournalService/query | same | MySQL SQL translation,location redaction | JOURNAL + real MySQL |
| Journal pages50,contract<=100,time+ID | Да | JournalQuery | same server pagination | UTC boundaries/browser timezone | JOURNAL + DST/paging |
| Audit health probe/expiry/latch | Да | AuditHealth/probe design | same evidence status | MySQL transaction probe,safe live/ready | JournalTests + DB outage |
| Settings read-only version/policy/limits | Да | content | /settings cards | web assembly version/nonsecret info | UI + E2E |
| Explicit StorageRoot/no fallback | Да | config intent | fixed volume roots/options | Linux config,validate mount/root | StorageConfiguration + missing volume |
| UI смены root/ACL/password management | Нет | отсутствует | не добавлять без задачи | operator provisioning documented | новый unauthorized settings |
| Diagnostics bounded/no repair | Да,personal | StorageDiagnostics | same personal report | Linux traversal/read budgets | JOURNAL diagnosis cases |
| Same-size/mtime content diagnosis | Не гарантируется без baseline | limitation | честно сохранить ограничение | нельзя объявить checksum scan готовым | новый unchanged-size fixture |
| SQL metadata/audit transactional consistency | Да,без FS+DB atomicity | orchestration/contracts | MySQL InnoDB transaction + compensation | dialect/concurrency/DDL split | PERSIST/FILES/EXTOP + MySQL fault tests |
| SQL Server schema/history | Да,3 migrations | retain Windows branch/provider | separate MySQL schema/migrations | не переносить T-SQL migrations | provider model/schema tests |
| Appended audit/history/soft-delete | Да | entities/query rules | same | MySQL indexes/checks/least grants | PERSIST + MySQL audit immutability |
| SQL RowVersion | Да,Files/ExternalDocuments | opaque token semantics | app-managed concurrency token in MySQL | IsConcurrencyToken regeneration | PERSIST/EXTDOC stale update |
| Parameterized SQL/error redaction | Да | LINQ/safe statuses | same | FOR UPDATE dialect/error translator | SQLi original + MySQL |
| SQL unavailable fail closed | Да для файловых services | mandatory audit policy | same +web readiness/status | streaming late error nuance | FILES/EXTZIP + HTTP audit failure |
| Win32 handle pin/share-deny race protection | Да в bounded threat model | safety intent | Linux dirfd/openat2/fstat/renameat2 | не считать flock абсолютной изоляцией | FILES/EXTOP + Linux hostile parent |
| Egyptian18PNG/colors/layout | Да | существующие assets/tokens | CSS Grid/variables/local PNG | WPF pack URIs/CroppedBitmap→CSS | UI + screenshot comparison |
| Native caption/resize/system dialogs | Да,Windows only | не код | browser chrome/HTML modals/server picker | WindowChrome/OpenFolderDialog неприменимы | browser focus/keyboard |
| 1024×640/150% accessibility | Layout tests/history,полная native приёмка открыта | UX criteria | browser zoom/keyboard/ARIA | responsive wrapping/local fonts | новый browser E2E/manual |
| Docker/MySQL/Web/CI | Нет | только исходная бизнес-основа | отдельные поэтапные компоненты | все readiness/security/deploy checks | roadmap WP01–WP12 |

Приёмка Web требует результата **каждой применимой строки**,не только API smoke. Строки
«нет» сохраняют границы исходной версии и отличают новые browser upload/download от
несуществующего WPF экспорта. Windows-specific поведение заменяется указанным функциональным
аналогом;неподтверждённый аппаратный/native PASS не переносится в Ubuntu.

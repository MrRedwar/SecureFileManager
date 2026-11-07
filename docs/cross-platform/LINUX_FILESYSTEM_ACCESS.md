# Доступ к файловой системе Ubuntu через Docker

Дата:07.11.2026. Статус: архитектурный план,ничего не смонтировано/не изменено.
Основа Windows: ComputerBrowseService,ExternalDocumentService,ExternalFileOperationService,
WindowsComputerDirectoryReader,Windows*Storage,SafeNativeFile. Их реальные границы —
[CURRENT_FUNCTIONALITY_AUDIT](CURRENT_FUNCTIONALITY_AUDIT.md).

## 1. Четыре разные файловые области

| Область | Что видит приложение |
|---|---|
| Container rootfs | runtime,библиотеки,wwwroot,свои /etc,/proc; не пользовательский файловый менеджер для rootfs |
| Ubuntu host Docker Engine | только явно предоставленные bind mounts/volumes; `/home/...` контейнера не означает `/home/...` VM |
| Диски Ubuntu VM | разделы/USB должны быть сначала смонтированы Ubuntu; web получает каталог точки монтирования,не устройство /dev |
| Browser client | файл,явно выбранный upload picker; сервер не узнаёт/не открывает произвольный client absolute path |

Здесь Docker host — Ubuntu в VirtualBox,не Windows-компьютер с WPF. Windows-разделы не
становятся Ubuntu-дисками автоматически. VirtualBox shared folders/USB passthrough —
отдельная настройка оператора; доступность устройства,права vboxsf и свойства ФС
проверяются вручную. Никаких обещаний показать все физические диски Windows из VM.

## 2. Каталог разрешённых расположений

Предлагается trusted configuration `StorageLocation`:
opaque Id,DisplayName,ContainerRoot,Mode(read-only/read-write),PathComparison,
ExpectedFilesystemIdentity,Availability,GrantPolicyVersion. Host source path остаётся в
операторском deployment manifest; не принимается от пользователя браузера.
Идентичность FS — проверяемый device/fs marker/mount identity,не только строка пути.

Пример **только карты**,не созданного Compose:

| Host source / volume | Container target | Режим/назначение |
|---|---|---|
| named `sfm_files` | /srv/sfm/files | rw;personal Users/<GUID>,не root общего browser |
| named `sfm_state` | /srv/sfm/state | rw;plans,before/after,receipts/locks;никакого UI-доступа |
| named `sfm_keys` | /srv/sfm/keys | rw;ASP.NET Data Protection keys,закрытые permissions |
| /home/<operator>/SfmWorkspace | /mnt/sfm/workspace | rw;явно разрешённая рабочая папка,не весь home |
| /mnt/data/Teaching | /mnt/sfm/data | rw;выделенная папка дополнительного раздела |
| /media/<operator>/<usb>/Share | /mnt/sfm/usb | ro по умолчанию;rw только явной настройкой и тестом ФС |
| named `sfm_mysql` | /var/lib/mysql в db | только MySQL,web его не получает |

Несколько mounts — независимые locations. UI показывает display name + logical relative
path,например `Рабочая папка:/Проекты`. Ввод полного пути Ubuntu допускается только как
UI-псевдоним,который сервер сопоставляет с уже выданным location; не произвольный Open(path).
Пользователь не может через settings зарегистрировать `/`,`/etc`,другой home или новый bind.

Регистрация location выполняется оператором вне пользовательского UI: проверить source
существует/ожидаемый filesystem,путь без ссылок,назначение не overlap с другим root,
Storage/state/keys/db/repository/docker socket/секреты исключены,выдать OS и app права.
Изменение root/grants увеличивает policy version; открытые handles/caches/operations
перепроверяются. Nested/overlapping roots и host aliases одного дерева по умолчанию
запрещены: иначе read-only root можно обойти через rw-псевдоним или чужой grant.

## 3. Что показывать как «диски»

Без host-agent показывать **разрешённые и фактически доступные locations**,их mount
type/capacity/free/readonly/availability,дедуплицируя общую ёмкость одного FS. Linux
адаптер может читать `/proc/self/mountinfo` и statvfs для собственных mounts; это
mount namespace контейнера,не полная таблица host mounts. Остальные overlay/proc/sys/
служебные volumes исключить из пользовательского каталога.

Host operator может предоставить очищенный read-only manifest host mount metadata
(display name,stable disk marker,mountpoint,status),без доступа к данным других путей.
Так показываются «не предоставлен контейнеру»/«носитель отсутствует»,а не фиктивные
папки. Информация о дисках без grant также может быть чувствительной: каталог выдаётся
по разрешениям,не весь manifest всем пользователям.

Динамическое подключение: baseline — Ubuntu монтирует носитель → оператор проверяет
marker/права → явное обновление allowlist/Compose и пересоздание web. Private bind
propagation не обещает hotplug. Не включать rshared ради удобства; recursive nested mounts
не должны незаметно расширять location. Для заранее известных mounts отдельно проверить
поведение propagation/recursive read-only на целевом ядре,иначе не поддерживать их.
Если USB исчез — location Unavailable,операции блокируются,plans сохраняются. Пустой
каталог на прежней mountpoint не считается тем же диском. Bind source должен существовать:
предпочесть long syntax с `create_host_path:false`,не создавать случайную папку вместо
непримонтированного диска. Не выполнять auto-mkdir в host mountpoint при сбое.

## 4. UID/GID,POSIX и ACL

Web запускается непривилегированным фиксированным UID/GID (конкретные значения выбрать
после проверки Ubuntu). Не полагаться на совпадение username: Linux проверяет numeric
identity. Для bind roots выдать узкий общий group либо POSIX ACL сервисному UID:
traverse(x) по предкам,list(r+x) папки,read/write файла,create/delete/rename(w+x) родителя.
Default ACL/umask определяют новые объекты. Не запускать `chmod -R 777`/`chown -R home`.
MySQL использует собственный отдельный UID,не root web. Для named volumes нужен отдельный
контролируемый provisioning шаг с нужным владельцем,без root runtime приложения.

Если Docker rootless/userns-remap,сопоставление host UID меняется — проверять фактический
UID map и доступ read/write,не копировать rootful ACL-команды. ext4/POSIX — первый
поддержанный сценарий. vfat/exfat/NTFS/vboxsf могут иначе выполнять chmod/ACL/locks/
case sensitivity/fsync/rename; не называть их эквивалентными ext4 без отдельной матрицы.

У всех app-аккаунтов одного процесса **одинаковые OS credentials**. POSIX/ACL — верхний
предел процесса,а не разграничение web users. Отдельные app grants обязательны. Пример:
UID sfm имеет rw к workspace; A получает List/Read/Download,B получает Create/Write;
попытка A вызвать Save напрямую запрещается до IO,несмотря на rw OS. Отсутствие ACL
не заменяется наличием grant; оба условия необходимы. Нельзя менять UID/chmod по запросу
браузера или объявлять HTTP UserId Linux-пользователем.

При более сильной изоляции пользователей нужны отдельные процессы/контейнеры/OS
идентичности или тщательно ограниченный host-agent. Это отдельное усложнение,не baseline.

## 5. Прикладная авторизация

Будущие LocationGrant(UserId,LocationId,PermissionSet) выдаёт доверенный оператор,
не первый открывший папку пользователь. Default deny; shared grants нескольким аккаунтам
реализуют текущую Windows-семантику общих каталогов только там,где это явно разрешено.
Глобальных all-files admin/grants по `/` не вводить.

Входной адрес команды: LocationId + validated relative segments либо opaque object ID.
Сервер получает actor из cookie/circuit context,не body.UserId. Для каждого метода:

1. Проверить session/revocation/expiry,SQL active User,grant и требуемое действие.
2. Lookup FileId/DocumentId/ReceiptId только в этом доступном location; не выдавать
   чужой абсолютный путь в NotFound/Denied. Personal дополнительно owner==actor.
3. Проверить текущую регистрацию root/mount identity,ro/rw flag,физический тип/границу.
4. Проверить source+destination permissions,version/concurrency,quota/limits.
5. Подготовить операцию; повторить authorization/policy version перед publish/выдачей.
6. Commit metadata+audit с actor/location/relative snapshots/correlation,выдать результат
   только после подтверждения; ambiguity → recovery,без слепого rollback.

Copy — Read source + Create destination,для download отдельный grant,ZIP create — Read
всех источников + Archive/Create target,extract — Read ZIP + Archive/Create directory.
Rename/Move требуют изменения source parent и создания target. Delete/Restore ограничены
location; read-only root не допускает trash. Recover — отдельное право в текущем root:
знание квитанции другого location не даёт доступа. Другой разрешённый actor может
завершить shared committed operation; её commit lookup не фильтровать по текущему actor,
но каталог плана/доступ к файлам всегда авторизовать. Историю показывает только actor UI;
при отзыве grant маскировать более недоступные external path snapshots,а не утекать ими.

## 6. Linux path semantics и защита от подмен

Не использовать ExternalPathSyntax для `X:\` на Linux. Новый LocationRef хранит `/`-
segments без root prefix;reject absolute paths,dot/dot-dot,NUL,control chars,служебный
namespace,разделители и недопустимые byte lengths. URL decoding выполняется ровно один
раз framework;encoded/double-encoded traversal,Unicode/confusables тестировать. Ограничить
имя255 **байт ФС**,глубину/суммарный путь/длину metadata;400 UTF-16 chars Windows не равно
Linux PATH_MAX/NAME_MAX. Не нормализовать регистр/Unicode так,чтобы смешать разные файлы.

Предлагается external Linux exact ordinal/case-sensitive identity: `A.txt` и `a.txt`
разные на ext4. Personal может сохранить консервативную CI policy ради совместимости,
но comparison должна быть явной per location во всех словарях/DB/locks/recovery/ZIP.
На первой версии ZIP entry policy остаётся консервативной CI/Windows-compatible; архив
двух case-variant файлов честно отклоняется,а не молча теряет один. Решение о расширении
ZIP допустимых имён — отдельная задача. Не обещать редактирование каждого Linux filename,
который невозможно безопасно представить в контракте UTF-8/UI;строка отмечается unsupported.

Нужен narrow native Linux adapter: root directory FD,`openat2` с RESOLVE_BENEATH,
RESOLVE_NO_SYMLINKS,RESOLVE_NO_MAGICLINKS;при поддержке/политике RESOLVE_NO_XDEV для
запрета незарегистрированных nested mounts (каждый разрешённый root открывается отдельно).
Флаги O_NOFOLLOW/O_CLOEXEC,для create O_CREAT|O_EXCL,проверка fstat regular file/directory,
st_dev/st_ino/nlink (hardlinked regular files deny),не открывать FIFO/socket/device как
обычный файл. O_NONBLOCK/безопасная type probe предотвращают зависание на FIFO.
Никаких `realpath`+`StartsWith` как единственной защиты и silent unsafe fallback при
недоступном syscall/seccomp. Проверять kernel/capabilities на readiness,отказывать,если
обязательная гарантия недоступна. Не использовать новейшие необязательные флаги ядра,
которых может не быть в выбранной Ubuntu.

Create/publish — descriptor-relative mkdirat/openat/renameat2(RENAME_NOREPLACE),не
обычный rename с возможностью replace. Операции с parents через проверенные directory
FD,финальный leaf identity повторно сверяется;checks до/после bounded streaming,
lock по location/object,expected version/hash/length/mtime,SQL concurrency token.
Linux flock/advisory locks не равны Windows share-deny и не запрещают запись сторонним
редактором. Pin directory FD также **не** запрещает внешнее перемещение directory.
Перед namespace mutation нужна повторная проверка привязки ancestor/root/leaf;враждебный
процесс с write к parents всё равно создаёт остаточную гонку. Для rw locations baseline
доверяет OS владельцу,ограничивает parent mutation ACL и не обещает защиту от root/того же
UID. Недоверенные общие parents обслуживать read-only либо через более сильную отдельную
границу/agent после threat review. Одного openat2 недостаточно для заявления «TOCTOU решён».

Save external сохраняет требование **права записи самого файла**: разрешение rename
родителя не должно разрешать обход read-only файла. Первый вариант — in-place + durable
before/after/plan,как текущий Windows режим,с риском частичного crash;если выбрана atomic
replacement,явно проверить исходное право записи и сохранение ownership/mode/ACL/xattrs,
иначе отказ. Не копировать привилегированные xattrs/setuid bits. Linux identity/hash
fingerprint и timestamp precision должны быть определены до сохранения version token.

## 7. Полные операции и recovery

| Операция | Linux-правило |
|---|---|
| List/navigation/search/sort | несколько разрешённых roots,current-directory snapshot<=10000,filter/sort;не рекурсивный индекс |
| Read/edit/create | TXT/JSON/XML10МиБ/generic external empty create;strict UTF8,версии,all grants |
| Copy | bounded tree1000 nodes/100МиБ file/1ГиБ total,source unchanged,no overwrite/merge,staging в target FS |
| Move/rename | same filesystem/compatible location,no replace;cross-FS возвращает Unsupported(EXDEV),предложить copy с сохранением источника |
| Delete private | прежнее удаление файла/пустой папки +soft-delete/history,не новый recursive delete |
| Trash external | соседнее скрытое `.sfm-trash-ID` на том же FS,receipt в state,сохранение GUID/history;не freedesktop trash,не освобождение места |
| Restore | receipt + authorized original location/free name/parent/unchanged tree;UI может принимать logical source path вместо host absolute |
| ZIP | existing stream engine/limits/CRC,create/extract staging в target FS,new directory only,preview-version check |
| Browser upload/download |streamed client bytes↔authorized server resource;не абсолютный путь клиента;см.архитектуру |
| Diagnose/sync/recover | personal scopes сохранены,external plans per-location authorized,unknown changes не стирать |

Root location не перемещать/не удалять;корни/предки state/private/key/db и overlaps
не выдавать. Служебные пути скрыты и недоступны через **все** routes,не только list.
Trash/staging внутри host directories — реальные данные:backup должна учитывать
hidden files и state receipts вместе,иначе Restore теряется. Копирование ACL исходника
не обещается;назначение наследует ограниченную target policy,что отображается в help.

Планы хранить с versioned schema,actor/location/relative source/target,expected FS marker,
identity/hash/tree/correlation,а не Windows absolute string. Flush/fsync данных **и
каталога** для plan/publication transitions;границы durability проверить на ext4. SQL
и FS не атомарны. Неопределённый commit проверяется SQL correlation,не повторяет IO.
После crash cleanup только доказанно собственных неизменённых данных. Storage root
или диск заменены/plan corrupted → RecoverRequired,сохранить обе версии/ручной разбор.

## 8. Docker Compose deployment design

| Сервис/аспект | План |
|---|---|
| web | multi-stage build,publish only runtime,не root;UID/GID согласованы с bind ACL;listen8080,publish127.0.0.1:8080:8080 |
| mysql | MySQL8.4 LTS как сервер-кандидат,точный patch/image digest после проверки;данные named volume,service account без global/root rights,порт наружу не открыт |
| migrate (one-shot) | тот же versioned release,DDL credentials только ему;проверить target/backup/историю;не запускать SQLServer migrations против MySQL |
| startup | mysql actual authenticated readiness→migrate success→web;web ready после schema/grants/required roots+audit probe,live лишь процесс |
| networks | private internal DB network web↔mysql;web frontend publish отдельно;отсутствует host network/DB published port |
| writable | только files/state/keys/allowed rw mounts и bounded tmpfs;container rootfs readonly;MySQL собственные требуемые writable paths |
| privileges | cap_drop ALL,no-new-privileges,без privileged/SYS_ADMIN/devices/docker.sock;ограничить pids/memory/cpu/disk quotas на host |
| secrets | runtime files /run/secrets для DB credentials,DP keys persistent private volume;env только nonsecret config;Compose secrets не называются encrypted vault |
| health | таймауты/retries/start period;MySQL «ping process» недостаточен для schema/app grants;probe не печатает пароль/connection string |
| shutdown | graceful stop с достаточным timeout,pre-publish cancellation,планы сохраняются;startup не удаляет unknown data и не мигрирует implicit |
| rollback/backup | immutable image by commit,DB backup + files/state consistent snapshot/quiesce;откат образа только при schema compatibility,никогда down -v |

Build context должен исключать Storage/artifacts/.git/local appsettings/secrets/demo
user data. Healthcheck command зависит от выбранного runtime (не предполагать curl в
slim/chiseled image);предпочесть включённый минимальный probe/известный инструмент.
MySQL image init/preparation может требовать отдельного provisioning ownership,это
не повод запускать runtime web/root или бездумно переопределять image user так,что DB
не стартует. Реальную non-root runtime схему проверить до приёмки.

## 9. Необязательный host-agent

Нужен лишь если отдельно потребуются полный host-mount inventory,динамическое подключение/
отключение физического носителя или управление mount. Текущая Windows-версия не умеет
монтировать/форматировать диски,поэтому это не обязательство parity.

Agent на Ubuntu может выдавать очищенный inventory read-only через Unix socket;
не давать web /dev или SYS_ADMIN. Для mount control потребуются отдельное разрешение,
минимальная allowlist stable device IDs,mount targets/options,Polkit/OS policy,peer
authentication/socket permissions,обязательный audit/rate limits. Никаких произвольных
shell commands/путей из браузера,общего root API или Docker socket. Agent компрометация
расширяет threat surface;baseline обходится операторским монтированием без него.

## 10. Критерии приёмки и ручная проверка

Авто: два app users при одном UID с разными grants;IDOR/receipt/recovery/source+target;
traversal/encoding/symlink/hardlink/FIFO/device/nested bind;race leaf/parent replacement;
stale hash/concurrent writes;actual upload/copy/ZIP budgets;read-only FS;ENOSPC controlled;
no overwrite/merge;ambiguous commit/crash checkpoints;неправильный disk marker/USB absent.
Ни один тест не использует home/рабочие mounts как очистку fixture.

Ручные Ubuntu проверки: filesystem type/mount map/UID namespace/ACL,два разрешённых
каталога на разных разделах,ro+rwx rules,доступ пользователяA/B,USB только demo-папка,
извлечение→Unavailable/возврат с marker,VM restart/Compose recreate с сохранением DB/
files/state/keys,download/upload,Ctrl+S/dirty/conflicts,localhost accessibility и отказ
с другой машины. Kernel/seccomp openat2/renameat2 фактически доступны. Все это NOT RUN.

## 11. Основания

Официальные технические источники проверены07.11.2026;план выше — проектные выводы:

- [Docker bind mounts](https://docs.docker.com/engine/storage/bind-mounts/): явные host paths,ro и propagation;это не доступ ко всем дискам.
- [Compose startup](https://docs.docker.com/compose/how-tos/startup-order/): healthy/completed dependencies.
- [Linux openat2](https://man7.org/linux/man-pages/man2/openat2.2.html): descriptor-relative resolution и flags;поддержку проверять на целевом kernel.
- [Linux rename/renameat2](https://man7.org/linux/man-pages/man2/rename.2.html): no-replace и cross-FS ограничения.

Открытые решения:actual Ubuntu/kernel/ФС,список roots/ACL/UID/GID,case policy,право
публикации общих директорий нескольким аккаунтам,USB lifecycle. Полный `/` rw и
privileged containers не являются предложенными вариантами по умолчанию.

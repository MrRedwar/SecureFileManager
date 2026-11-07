# План SQL Server + MySQL

Дата07.11.2026. Только проектирование;пакеты не устанавливались,MySQL не подключался,
схемы/данные SQL Server не менялись. На Ubuntu предлагается отдельная новая MySQL БД,
без обязательной миграции Windows-пользователей/Storage.

## 1. Фактическая схема и провайдерская связанность

Источник: [AppDbContext](../../src/SecureFileManager.Data/AppDbContext.cs),
[Entities](../../src/SecureFileManager.Core/Entities/User.cs),
[migrations](../../src/SecureFileManager.Data/Migrations/AppDbContextModelSnapshot.cs).

| Таблица | Реальные поля/ограничения/индексы |
|---|---|
| Users | GUID Id;Username<=50,unique Latin1_General_100_CI_AS;PasswordHash<=1024;CreatedAtUtc;IsActive default true;FailedLoginCount>=0;FailedLoginWindowStartUtc/LockoutUntilUtc nullable;trim/length/nonempty hash CHECK |
| Files | GUID Id,OwnerId FK Users;Name255,RelativePath400 CI/AS;Kind string File/Directory,Extension32;SizeBytes>=0;Created/Modified/Deleted UTC;IsDeleted/DeletedAt consistent;SQL rowversion;OwnerId index;unique(OwnerId,RelativePath) filtered IsDeleted=0;global query filter !IsDeleted |
| ExternalDocuments | GUID Id;Path4000,PathKey64 ASCII SHA256 uppercase Windows path unique;SizeBytes>=0;ModifiedAtUtc;Version256;SQL rowversion;OwnerId отсутствует,папки здесь не строки |
| Operations | GUID Id,nullable UserId/FileId/ExternalDocumentId FK NO ACTION;ActorNameSnapshot50;TargetPathSnapshot400;ExternalPathSnapshot4000;OccurredAtUtc;OperationType14 enum strings32;Result enum strings16;Details512;CorrelationId;indexes(UserId,OccurredAtUtc),(OperationType,Result),CorrelationId,FK indexes |

FK не каскадные,история FileId сохраняется при soft-delete. Контекст запрещает
изменение/удаление Operation через SaveChanges,Remove(FileRecord) превращает в soft-delete,
не-UTC DateTime отклоняет,Kind UTC восстанавливает ValueConverter. Это прикладная append-only
гарантия,не защита от SQL-admin или raw SQL вне контекста. Два file FK одновременно запрещены.
External snapshot допускается без FK лишь для известных external operation codes.

Три существующих миграции:

1. `20261101094955_InitialCreate`.
2. `20261101122237_AddLoginFailureWindow`.
3. `20261107101211_AddExternalDocuments`.

DB_UPDATE_1102 сообщает backup/контролируемое применение третьей к рабочей БД07.11.
Здесь живое состояние не перепроверялось. Не запускать Down/EnsureDeleted/повторный initial.

**Одной смены провайдера недостаточно:**

- AppDbContext: `[column]`,LEN/DATALENGTH,datetime2,rowversion,SQL collation,filtered index;
- IdentityStore/FileTransactionStore/ExternalDocumentStore: Serializable и
  `FROM [Users] WITH (UPDLOCK,HOLDLOCK)`;
- Joined factories в IdentityStore/FileTransactionStore явно UseSqlServer на общей connection;
- RepositoryBase и stores: SqlException,unique2601/2627,deadlock1205,constraint547/2628/8152/515;
- ExternalDocumentStore tree query содержит `path + "\\"`,fixed COLLATE,uppercase PathKey;
- DatabaseConfiguration/connection check/probe/design-time factory/DI — SQL-specific;
- diagnostics FileService query использует fixed SQL collation;SQL tests содержат
  T-SQL triggers/SqlConnection/temp database lifecycle;
- AppDbContext.ValidateChanges использует Windows ExternalPathSyntax даже для metadata.

Пользовательские параметры в текущем FromSqlInterpolated параметризованы;это не SQLi
находка. Проблема переноса — диалект/locking/errors/model,не отключение параметризации.

## 2. Проверенная совместимость версий

Проверка официальных package pages/документации07.11.2026,без restore/запуска:

| Вариант | Подтверждение | Решение |
|---|---|---|
| Текущий .NET9 + EF/SqlServer9.0.22 | csproj/global.json и dotnet --info | baseline Windows сохранить;для новой долгой разработки риск EOL10.11.2026 |
| Pomelo9.0.0 + EF9.0.x + MySqlConnector>=2.4.0 | release matrix,tested MySQL8.4/8.0 | совместимый технический fallback,не заявлять поддержку EF10 этим provider |
| .NET10 + Oracle MySql.EntityFrameworkCore10.0.12 | stable package10.0.12+MySQL26.10.0,net10 dependency group EF/Relational>=10.0.12,MySql.Data>=26.10.0 | предпочтительный **кандидат** для новой Web ветки |
| EF SqlServer10.0.12 | package targets net10 | тот же major/patch в SQL provider build candidate,без обновления live schema |
| Oracle11 preview/.NET11 preview | виден в каталоге,не нужен | не выбирать preview |

Источники:

- [Pomelo compatibility](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql) и [release9.0.0](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/releases/tag/9.0.0).
- [Oracle provider10.0.12,dependencies](https://www.nuget.org/packages/MySql.EntityFrameworkCore/10.0.12).
- [Connector/NET support table](https://dev.mysql.com/doc/connector-net/en/connector-net-entityframework-core.html).
- [SqlServer provider10.0.12](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer/10.0.12).
- [Microsoft .NET8/9 EOL10.11.2026](https://devblogs.microsoft.com/dotnet/dotnet-8-9-end-of-support/).

Вывод: кандидат .NET10 LTS/EF10 + Oracle stable,MySQL8.4 LTS,точный server patch/image digest
выбрать на WP01. Это проверка заявленных зависимостей,**не** проверенная сборка проекта,
совместимость всех LINQ/миграций,отсутствие CVE или окончательный package lock. До выбора
нужен spike на новой disposable DB:restore/build/model/migration/CRUD/transactions/locks/
constraint tests. При выявленном ограничении Oracle — заново сравнить актуальные stable
провайдеры;не повышать Pomelo9 до EF10 через подавление dependency warnings.
Oracle package license GPL-2.0-only WITH Universal-FOSS-exception-1.0 видна в NuGet;
проверить применимость к способу распространения учебного проекта. Это не объявление
лицензии всего приложения автоматически совместимой.

В установленной среде SDK10 нет,VS2022 не подходит для WPF net10 в IDE. Будущий CLI
build Web на UbuntuSDK10 не зависит от VS;Windows совместимость общих проектов проверять
отдельно. Ветка переноса может обновить common projects до net10 после разрешения и
дать Wpf net10 сборку только SDK10/VS2026. Существующий Windows baseline оставить на
исходном commit/TFM. Если нужна активная VS2022 сборка той же ветки,отдельно утвердить
multi-target net9/net10 с conditional package references;не делать это скрыто и не
использовать EF10 из net9-проекта. Автоперенос рабочей БД при TFM upgrade не нужен.

## 3. Поддержка двух провайдеров

Предлагается оставить Core/entities и repository contracts общими,Data иметь common
mapping/validation + provider profile. `DatabaseProvider=SqlServer|MySql` — trusted
startup config;не переключатель пользователя. Одна запущенная app instance использует
один provider. Не хранить SqlConnectionStringBuilder как универсальный parser MySQL.

Отдельные migration assemblies/contexts snapshots: `Data.Migrations.SqlServer` и
`Data.Migrations.MySql`. Существующие SQL миграции сохранить с прежними ID и history;
не регенерировать их SQL из MySQL snapshot. Новый MySQL initial строится из общей
логической модели и MySQL mapping. Если для сохранения SQL snapshot требуется отдельный
SqlServerAppDbContext,разделение проверить через pending-model/schema diff,не подменять
DbContext имя вслепую. В design-time factory явно задавать provider/migrations assembly.
Общий context mapping provider-dependent должен корректно кэшироваться:providers имеют
разные options/service providers;если добавлен свой profile — включить его в model cache
key или разнести concrete context types. Тест создания двух providers в одном test
process обязателен.

[Microsoft multiple-provider migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers)
поддерживает отдельные наборы миграций. Это основание для разделения,не готовая
автоматическая конвертация SQL migrations.

Новые инфраструктурные порты (проектные): provider options/dialect/exception translator,
transaction-context factory и location path policy. Provider-specific SQL:User row lock,
probe,concurrency update и schema checks. Обычные repository queries оставить LINQ.
Не тащить второй ORM или raw SQL в Core/View.

## 4. Mapping MySQL: решения для acceptance spike

| Аспект | Предлагаемая реализация | Обязательная проверка |
|---|---|---|
| Engine | InnoDB,strict SQL mode,utf8mb4 | actual engine/check constraints/transactions |
| GUID | единое binary(16) с фиксированным converter **или** ASCII char(36);предпочесть char36 для первого простого deployment | round-trip всех FK,не смешать MySql GUID byte order при возможном импорте |
| UTC | datetime(6),Kind Utc восстановить,connection/session TZ UTC;не MySQL timestamp как token | round-trip UTC и precision,timezone/DST/local UI |
| Physical mtime | отдельный точный version fingerprint ticks/hash/identity | микросекунды DB не должны сделать текущий файл «Changed» немедленно после save |
| Names | display utf8mb4;login normalized key с явной CI/AS политикой;relative keys per location | Unicode/accent/case/trailing spaces comparisons,не server default ai_ci |
| Personal active path unique | nullable generated ActivePathKey=CASE WHEN !IsDeleted THEN canonical key ELSE NULL END;unique(OwnerId,ActivePathKey) | duplicate active запрещён,несколько deleted поколения допустимы,reactivation conflict |
| External path identity | unique(LocationId,PathKey),PathKey exact canonical relative bytes for Linux;Windows key separate policy | A.txt/a.txt distinct ext4,alias/no private leakage,hash collision проверка actual path |
| Long strings/index | Path4000 не unique напрямую;key hash ASCII/binary,400-relative*4 +GUID помещаемость проверить | utf8mb4 index byte budget,длинные Cyrillic/emoji names |
| SQL RowVersion | SQL mapping оставить IsRowVersion;MySQL application-generated random byte token IsConcurrencyToken на каждом insert/update | WHERE old token,два update→один conflict,delete/save/rollback |
| CHECK/default | MySQL-specific quoted SQL:CHAR_LENGTH/TRIM/OCTET_LENGTH/boolean/date checks | DB прямой INSERT invalid отвергается,не только Core validator |
| FK/history | no cascade,nullable actor/object как сейчас | history after soft-delete/reuse,FK violation mapping |
| Audit indexes | actor+UTC/correlation/type/result,location/object indexes по новым запросам | paging/query plan на realistic bounded fixtures |

Generated active key и нормализация — предложения,не готовые миграции. Hash collision
не приравнивать к одинаковому файлу:compare исходный canonical path и возвращать Conflict,
не присваивать существующий ID. Personal uniqueness по строковому CI SQL сейчас шире
обычного invariant uppercase:до импорта протестировать Unicode и назвать отличия,не
заявлять полную эквивалентность без tests. Допустим application normalized key,но это
изменение правила требуется зафиксировать/согласовать до входа старых пользователей.

Текущая точная mtime в datetime2 может храниться с большей точностью,чем datetime(6).
Для list equality либо хранить raw FS ticks как bigint,либо согласованно округлять обе
сравниваемые стороны,сохранив hash/version для сохранений. DateTime Kind сам по себе
не исправляет потерю точности. Это отдельный acceptance test,а не косметический mapping.

## 5. Транзакции,блокировки и аудит

IdentityStore lock row через MySQL `SELECT ... FOR UPDATE` и explicit transaction.
Уникальный username защищён DB unique index;race регистрации не решать одним select.
Failed login counters/sliding audit observations сохранять в одной транзакции.
File/external stores сохраняют SQL metadata+audit на одной connection/transaction,
Joined factories получают provider options и UseTransaction вместо UseSqlServer.
Serializable либо обоснованный row lock/isolation для MySQL подтвердить конкурентными
tests;не считать SQL Server UPDLOCK эквивалентом произвольного LINQ query.

Deadlock/lock-timeout/duplicate/FK/check/truncation/concurrency errors маппить provider
translator в прежние безопасные статусы. Identity-only retry ограничен/idempotent;
callback,уже меняющий FS,не повторять автоматически при transient retry strategy.
MySQL auto reconnect/ambiguous COMMIT — прежний FileStoreException uncertainty,lookup
success по correlation+location+target. External commit lookup не owner-filtered,но
recovery caller всё равно должен иметь location permission.

Новый portable Version token — opaque,не используется как timestamp или привилегия.
User lockout concurrency надо защищать locks/atomic updates независимо от File tokens.
AuditHealth probe:реальный INSERT+ROLLBACK под app credentials,без fake success rows.
Runtime DB user SELECT/INSERT/ограниченные UPDATE для metadata/identity,Operations без
UPDATE/DELETE при возможности отдельных grants;DDL только migration identity.

FS и MySQL — два ресурса. Сохранить staging→durable plan→publish→metadata+audit COMMIT→
verified settle. Schema evolution для новых Location/Grant/Session/operation state —
отдельные MySQL миграции после initial,явно отделённые от смены provider. Нужны операции
с обоими source/target roots,revoked grants,recovery и audit object mappings,а не просто
таблица внешних файлов с новым separator.

## 6. Возможная миграция пользователей позже

По умолчанию Ubuntu имеет новую БД и собственную регистрацию. Рабочая SQL БД остаётся
на месте. Если отдельно потребуется перенос:

1. Backup SQL + files/state на согласованной точке,копия для rehearsal,Windows не трогать.
2. Сохранить User.Id и **точную строку PasswordHash**,не повторно хешировать hash и не
   требовать plaintext. PasswordSecurity проверяет IdentityV3 независимо от СУБД.
3. Сохранить IsActive/lockout timestamps/UTC/actor GUID,проверить CI login collisions.
   Если версия KDF устарела,rehash только при успешном вводе настоящего пароля.
4. Новые cookies/sessions создать заново,desktop sessions не переносить. DB RowVersion
   не копировать как действующий MySQL token:сгенерировать новые tokens,старые buffers invalid.
5. Personal Users/<GUID> копировать только отдельной разрешённой процедурой с hash/
   metadata/count checks. Windows external X:\ не превращается в Linux path:нужна явная
   location mapping,unsupported originals/history оставить как legacy snapshots.
6. Проверить BOM/filename case collisions/Unicode/ACL/permissions/mtime precision,FK/
   audit/history/soft-delete. Неразрешённые/нетранслируемые пути не включать в grants.
7. Rehearsal login old user на копии,сверка counts/digests без вывода hashes,rollback
   план без удаления исходной SQL БД. Никакой синхронизации двух active writers.

## 7. Приёмка provider этапа

Новые isolated MySQL tests:initial migration+schema checks,all repository CRUD,FK/check/
unique/soft-delete/history,UTC precision,GUID/hash round-trip,stale token,concurrent
registration/lockout/saves,DB unavailable/audit failure/unknown commit,LINQ journal paging/
path search/collation,generated active uniqueness,key collision defense. Настоящая MySQL
обязательна;InMemory/SQLite не заменяют интеграцию.

SQL provider regression на отдельной тестовой БД и unchanged SQL migration history,
existing194 Windows cases после соответствующей будущей задачи. Startup не выполняет
auto-DDL. Migration job проверяет target provider/server/db/history,создаёт backup для
existing deployment,не down/up reset. Secret connection не логируется.

Ручные проверки:MySQL8.4 runtime non-root volume permissions,TLS/cert profile/least
grants,container recreate с persistence,backup restore новой БД,две app sessions/
concurrent edits. Все проверки этого раздела **будущие,NOT RUN**.

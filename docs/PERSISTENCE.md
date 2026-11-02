# Persistence — этап 02

| Таблица | Столбцы и ограничения |
|---|---|
| Users | Id GUID PK; Username nvarchar(50), required, unique; PasswordHash nvarchar(1024), required; CreatedAtUtc datetime2; IsActive bit default true; FailedLoginCount int >= 0; LockoutUntilUtc nullable datetime2 |
| Files | Id GUID PK; OwnerId GUID FK Users; Name nvarchar(255); RelativePath nvarchar(400); Kind File/Directory; Extension nullable nvarchar(32); SizeBytes bigint >= 0; CreatedAtUtc/ModifiedAtUtc datetime2; IsDeleted bit; DeletedAtUtc nullable datetime2; RowVersion rowversion |
| Operations | Id GUID PK; UserId nullable GUID FK Users; ActorNameSnapshot nullable nvarchar(50); FileId nullable GUID FK Files; OccurredAtUtc datetime2; OperationType nvarchar(32), все 14 событий ТЗ; Result Success/Denied/Failed; TargetPathSnapshot nullable nvarchar(400); Details nullable nvarchar(512); CorrelationId GUID |

Все три FK — NO ACTION, каскадного удаления нет. Индексы: Users.Username unique;
Files.OwnerId; Files.(OwnerId,RelativePath) unique **WHERE IsDeleted=0**;
Operations.(UserId,OccurredAtUtc), (OperationType,Result), FileId, CorrelationId.
CHECK проверяют длину/пустые логины, наличие хеша, счётчик ошибок, размеры,
вид файла, соответствие IsDeleted/DeletedAtUtc, порядок дат и enum-значения журнала.

Username и RelativePath используют Latin1_General_100_CI_AS: регистр игнорируется,
диакритика учитывается. Пути метаданных имеют единый разделитель `/`; длина 400
помещает составной ключ в лимит 900 байт (GUID 16 + nvarchar 800 = 816).
Длины — решения проекта, не новые требования методички.
AppDbContext отклоняет не-UTC DateTime; чтение datetime2 восстанавливает Kind=Utc.
SQL datetime2 сам не хранит часовой пояс.

## Контракты и границы

Core содержит только модели, интерфейсы и RepositoryResult<T>; зависимостей EF/WPF/SQL нет.
Data содержит mapping, фабрики контекста, конфигурацию, миграции и репозитории.
Wpf использует Data только в App.xaml.cs для composition root.

IUserRepository: добавление и поиск по ID/логину; регистрация/хеширование/вход ещё отсутствуют.
IFileRepository: добавление, чтение по ID/пути, список владельца, изменение, soft-delete.
IOperationRepository: добавление и чтение собственных событий (до 500 за запрос).
Неизвестный неудачный вход сохраняется с UserId=NULL и FileId=NULL;
общий просмотр таких событий пользовательскому UI не предоставляется.

Результаты: Success, Validation, Unauthorized (зарезервирован), NotFound, Conflict,
InfrastructureError. Результат не содержит исключение, строку подключения или хеш.
CancellationToken передаётся в EF; отмена пробрасывается как отмена.
SQL duplicate key/rowversion → Conflict; CHECK/FK/truncation → Validation;
ошибки соединения и обёртка SQL transient failure → InfrastructureError.

Каждый метод создаёт/освобождает свой DbContext через IDbContextFactory.
Репозитории не обещают общей транзакции с файловой системой или автоматического аудита
каждого вызова. Согласование файловых действий/метаданных/аудита — этап 05.
Здесь изменение метаданных и запись события — отдельные persistence-примитивы.

Файлы фильтруются !IsDeleted. IFileRepository/DbContext.Remove помечает строку,
не удаляет её; путь можно создать заново под новым ID. RowVersion обязателен для
update/delete и обнаруживает устаревшее состояние. Обновление не меняет владельца
и CreatedAtUtc. Аудит остаётся связанным со старым FileId, снимок пути сохраняется.
Изменение/удаление Operations и удаление Users через SaveChanges запрещены.
Это защита слоя приложения, не запрет администратору SQL выполнять собственные команды.

ownerId должен поступать из будущей сессии сервиса, не из UI. Репозитории фильтруют
по переданному владельцу, но не аутентифицируют вызывающего. Storage/физические пути/
ссылки и полная авторизация здесь отсутствуют. Проверка синтаксиса пути метаданных
не заменяет IPathPolicy этапа 04. TargetPathSnapshot — безопасный относительный путь
либо NULL; опасный ввод сервис журналирует безопасным кодом, не копирует сырой путь.
Details/ActorNameSnapshot обязан безопасно формировать вызывающий сервис:
репозиторий не может распознать произвольный секрет внутри текста.

## Миграции и SQL-проверка

База SecureFileManagerDB отсутствовала при двух проверках перед созданием.
Применена только `20261101094955_InitialCreate`; таблицы приложения пусты.
Миграции не выполняются автоматически при запуске WPF. Seed/demo-данных нет.
Не выполнять database update 0, Down, DROP DATABASE или EnsureDeleted для этой базы.

Из корня репозитория, PowerShell:

```powershell
dotnet tool restore
$env:SFM_ENVIRONMENT = 'Development'
$env:SFM_CONFIG_DIRECTORY = (Resolve-Path src/SecureFileManager.Wpf).Path
dotnet ef migrations list --project src/SecureFileManager.Data --startup-project src/SecureFileManager.Data
dotnet ef migrations has-pending-model-changes --project src/SecureFileManager.Data --startup-project src/SecureFileManager.Data
```

Для первоначального создания на **новой, предварительно проверенной** установке:

```powershell
dotnet ef database update --project src/SecureFileManager.Data --startup-project src/SecureFileManager.Data
```

Если база уже есть, сначала изучить sys.tables, __EFMigrationsHistory и данные;
не применять изменения вслепую. Этой машине повторная initial migration не нужна.
SQL миграции — docs/sql/initial-create.sql (не содержит CREATE DATABASE).
Read-only запросы SSMS 19 — docs/sql/verify-stage02.sql; результат запуска через
sqlcmd — docs/sql/stage02-verification.txt. Ручной GUI-прогон SSMS ещё не выполнен.

## Реальные SQL-тесты

```powershell
dotnet restore SecureFileManager.sln
dotnet build SecureFileManager.sln -c Debug --no-restore
$env:SFM_ENVIRONMENT = 'Development'
$env:SFM_CONFIG_DIRECTORY = (Resolve-Path src/SecureFileManager.Wpf).Path
$env:SFM_RUN_SQL_TESTS = '1'
dotnet test SecureFileManager.sln -c Debug --no-build
```

Без SFM_RUN_SQL_TESTS=1 восемь SQL-тестов явно Skip; это не проверка БД.
При включении создаётся новая SecureFileManagerTests_<GUID> только на локальном
SQL Express, Windows Authentication, TLS. До миграции проверяется отсутствие базы.
Каждый тест использует свою транзакцию, общую для тестовых контекстов/репозиториев,
и откатывает данные; рабочая SecureFileManagerDB не используется.
База не удаляется: остаётся схема и история миграции для проверки.
Права SQL на создание базы нужны только для миграций/этих тестов.
Проверка недоступной БД выполняет лишь SELECT и проверяет безопасный результат.


## Актуализация этапа 04 (01.11.2026)

Новая миграция 20261101122237_AddLoginFailureWindow добавляет в Users nullable datetime2
FailedLoginWindowStartUtc. UTC-конвертер/валидация AppDbContext распространяются и на это
поле. В SecureFileManagerDB теперь обе миграции; рабочие таблицы после применения
остались пустыми (0/0/0). InitialCreate/FK/Files/Operations/индексы не изменены.
SQL — docs/sql/stage04-migration.sql, read-only проверка — verify-stage04.sql.
Миграции не запускаются автоматически из WPF; безопасные команды остаются прежними,
для существующей базы предварительно проверяются history/sys.columns и backup.
Ручная SSMS-приёмка этапа 02 ранее подтверждена пользователем; новая GUI-приёмка 04 открыта.

Core содержит IIdentityStore/IIdentityTransaction — узкий контракт транзакций identity.
Реализация Data/Identity/IdentityStore сериализует регистрацию/вход, атомарно сохраняет
счётчики/хеш и Operations через прежний OperationRepository на общей SQL connection/
transaction. Отдельные публичные вызовы прежних репозиториев общей транзакции сами
по себе не получают. Координация будущих файловых операций остаётся на этапе 05.

Проверка синтаксиса MetadataPath делегирует Core/Security/RelativePathSyntax; физические
проверки выполняются IUserStorageService/IPathPolicy, не репозиториями. ownerId должен
поступать из текущей IUserSession. Прикладные сервисы проверяют активность/ownership/
Revision и не считают repository owner-filter полноценной аутентификацией.

Security SQL-тесты коммитят данные только в уникальной новой тестовой БД fixture,
старые persistence-тесты сохраняют rollback транзакций. Fixture ничего не удаляет;
после security-тестов база содержит явно тестовые аккаунты и события и не считается пустой.
Фактические агрегаты: stage04-test-verification.txt. Вывод паролей/хешей запрещён.


## Дополнение этапа 05 (02.11.2026)

Схема Users/Files/Operations и две существующие миграции сохранены.
FileTransactionStore объединяет существующие репозитории через общий DbContext/connection
и Serializable-транзакцию с owner UPDLOCK/HOLDLOCK. Метаданные и обязательный аудит
фиксируются вместе; нет независимого успеха Files без Operations и нет автоматического
повтора файлового callback. UserId/CorrelationId + Details FILE_TX_* используются
для проверки неоднозначного commit. Политика восстановления описана в FILE_OPERATIONS.md.

ListChildrenAsync ограничивает LINQ-запрос текущим владельцем и непосредственными детьми
папки; выборка ограничена 10001 строкой для обнаружения превышения лимита 10000.
Delete использует прежний soft-delete, audit FK/история сохраняются. Явная сверка
не удаляет физические данные и не присваивает посторонние файлы автоматически.

Новые SQL-интеграционные тесты используют отдельную GUID-БД, созданную fixture данного
запуска. Только такая БД удаляется при Dispose. SFM_EXISTING_TEST_DATABASE по-прежнему
никогда не удаляется. Старые, ранее существовавшие тестовые БД не затронуты.
Ручные SELECT без хешей/секретов — sql/verify-stage05.sql.

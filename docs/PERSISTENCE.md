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

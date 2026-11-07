# 11.02 — Редактирование внешних TXT/JSON/XML

Дополнение11.03: добавлены [внешние файловые операции](EXTERNAL_OPERATIONS.md).
Move/rename через приложение сохраняет GUID известных файлов и обновляет путь;
reload старого редактора теперь обновляет имя/формат. Перемещение сторонней программой
по-прежнему не отслеживается автоматически. Pending11.03 блокирует открытие/сохранение
внешних документов до «Действия → Проверить операции», а editor-pending защищает источник
от новых copy/move. Исторический протокол11.02 ниже сохранён.

**Текущее состояние рабочей БД:** 07.11.2026 по прямому поручению пользователя выполнены
backup/VERIFYONLY, добавочная миграция и реальная EF-проверка с rollback-probe.
БД готова, pending migrations0; данные2/9/136 сохранены, новая ExternalDocuments пуста.
Подробности: [DB_UPDATE_1102](DB_UPDATE_1102.md). Прежние записи ниже о неприменённой
рабочей миграции описывают состояние до этого поручения; блокер БД снят.

Дата: 07.11.2026. База работы: чистый Git, HEAD42e66e9. Реализация C#/WPF;
общая видимость внешних документов по поручению пользователя сохранена.
Этап11.01 остаётся с открытой native-приёмкой;11.03/11.04 не реализуются этой задачей.

## Использование

После подготовки схемы БД войти → «Диски → Файлы компьютера» → выбрать обычный
локальный TXT/JSON/XML → «Открыть», Enter или двойной щелчок. Используется прежний
общий редактор, Ctrl+S, проверка JSON/XML, подтверждение закрытия/выхода и явное
«Перечитать». Другие типы файлов не запускаются внешними программами.
Изменённый внешней программой или другим app-пользователем файл не перезаписывается
старым буфером: редактор оставляет текст и сообщает конфликт. Перечитывание требует
подтверждения. После смены сессии текст очищается, окна закрываются.

## Схема и установка

Новая добавочная миграция: `20261107101211_AddExternalDocuments`.
Up создаёт ExternalDocuments и добавляет nullable ExternalDocumentId/ExternalPathSnapshot
в Operations, индекс/FK NO ACTION. Не изменяет Users/User.Id/пароли/личные Files,
не переносит Storage и не выполняет удаления рабочих данных. Down не запускать.
Миграция генерируется/проверяется на отдельной тестовой БД; рабочая БД автоматически
не обновляется. До применения приложение требует подготовить схему.

Для существующей установки: сохранить резервную копию БД обычными средствами SQL Server,
проверить выбранный экземпляр/InitialCatalog и две прежние записи __EFMigrationsHistory.
Из корня репозитория PowerShell (конфиг остаётся локальным, секреты не выводить):

```powershell
dotnet tool restore
$env:SFM_ENVIRONMENT = 'Development'
$env:SFM_CONFIG_DIRECTORY = (Resolve-Path src/SecureFileManager.Wpf).Path
dotnet ef migrations list --project src/SecureFileManager.Data --startup-project src/SecureFileManager.Data
dotnet ef database update 20261107101211_AddExternalDocuments --project src/SecureFileManager.Data --startup-project src/SecureFileManager.Data
```

Последняя команда предназначена для контролируемого обновления после backup/review;
на этой машине обновление уже выполнено контролируемым EF SQL с дополнительными guards.
При несовпадении history/schema сначала разобраться
с состоянием, не пересоздавать базу. На новой установке обычный database update применяет
все три Up. После обновления перезапустить приложение/F5. Если схема не подготовлена,
проверка соединения не считает приложение готовым к входу и указывает проверить миграции.

| ExternalDocuments | Назначение |
|---|---|
| Id GUID PK | стабильный общий ID документа; личного OwnerId нет |
| Path nvarchar(4000) | нормализованный абсолютный локальный путь |
| PathKey varchar(64), unique | SHA-256 пути с invariant uppercase, без слишком длинного SQL key |
| SizeBytes bigint, CHECK >=0 | фактический размер |
| ModifiedAtUtc datetime2 | UTC физического файла |
| Version nvarchar(256) | identity/length/mtime/SHA-256 версии |
| RowVersion rowversion | обнаружение устаревшей SQL-версии |

Operations.ExternalDocumentId ссылается на ExternalDocuments; ExternalPathSnapshot
сохраняет путь конкретного действия, FileId остаётся NULL. Старые личные snapshot/FK
не меняют смысл. Журнал фильтруется по текущему actor, показывает и ищет внешний путь,
не отдаёт чужую историю и не раскрывает содержимое документа. Код EXTERNAL_READ/SAVED/
VALIDATED/RECOVERED фиксированный; ошибки не содержат parser payload/секретов.
Путь является метаданными операции и может содержать пользовательское имя: скриншоты
журнала/БД для публичного отчёта делают на demo-данных.

## Контракты и контроль доступа

ITextDocumentService выделяет Read/Reload/Save/Validate из прежнего IFileService.
TextEditorViewModel использует этот узкий порт для обоих режимов; личные команды и
owner policy не изменены. IExternalDocumentService дополнительно OpenAsync(path) и
RecoverAsync(path). WindowsFileDialogs ведёт единый реестр окон с ключом scope+ID.
IExternalDocumentStore/ExternalDocumentStore координируют SQL metadata+audit, проверяют
active User под Serializable/UPDLOCK/HOLDLOCK. File callbacks не повторяются автоматически.

IExternalDocumentStorage/WindowsExternalDocumentStorage выполняют Windows IO. Путь
проходит общую ExternalPathSyntax и физические проверки: только локальный абсолютный
путь, не private Storage, не traversal/ADS/device/network/UNC/reparse/hardlink.
Удерживаются native handles файла и всех предков. Реальные Windows ACL авторитетны;
save требует GENERIC_WRITE, read-only атрибут не обходится. Повышения прав/смены ACL нет.
Сервис сверяет app-session/revision до/после await, возвращает данные только после audit
commit. Разные app-пользователи могут открыть один внешний ID, но имеют отдельный аудит.

## Сохранение и восстановление

1. Открывается исходный handle с необходимыми правами и запретом других writers/delete.
   Проверяются текущая SHA-256/identity/size/mtime и SQL RowVersion редактора.
2. В `StorageRoot/.external-editor/<SHA256-path>/` создаются before.bin, after.bin и
   ограниченный plan.json, каждый CreateNew+Flush(true). Lock сериализует доступ разных
   процессов к этому пути. Служебная область скрыта общим запретом просмотра Storage.
3. Только после durable plan выполняется запись в исходный файл через удерживаемый
   handle и FlushToDisk. Выбран in-place протокол для сохранения прежних Windows ACL,
   identity и требований фактического права записи; замена файлом с более широкими
   унаследованными ACL не выполняется.
4. Метаданные и EXTERNAL_SAVED фиксируются одной SQL-транзакцией. При подтверждённом
   commit служебные версии удаляются. При известном отказе восстанавливаются старые
   байты и исходная ModifiedAtUtc из плана; восстановление самого текущего частичного write допускается только пока
   сервис удерживает тот же file handle/identity и знает о своей попытке записи.
5. При неизвестном исходе проверяется EXTERNAL_SAVED correlation+path. Проверка не
   привязана к текущему actor: другой пользователь не должен откатить committed saveA.
   При недоступном SQL/неоднозначном файле резервные данные сохраняются.

«Восстановить» в браузере — явная проверка для выбранного поддерживаемого файла.
Committed-результат сохраняется; uncommitted восстанавливается только при известной
identity и совпадении текущих байтов с одной из подготовленных версий. Неожиданное
внешнее изменение/замена или повреждённый план сохраняются для ручного разбора,
не удаляются ради успешного статуса. Содержимое резервных файлов/планов не логируется.
Не удалять .external-editor вручную и не копировать в него чужие файлы.

**Ограничения протокола:** SQL+NTFS не атомарны. Во время in-place записи возможна
частичная видимая версия/сбой процесса; исходник сохраняется в durable backup.
Произвольный power loss и отказ накопителя не сертифицированы. После аварии с байтами,
не совпадающими ни с before, ни с after, автоматическое восстановление прекращается:
нужен ручной разбор с копиями original/backup/plan. Ошибка cleanup может оставить план
после подтверждённого save. Прямой Windows/SQL admin и владелец процесса вне threat model.
Служебные копии содержат исходный/новый текст и защищаются доступом к StorageRoot;
шифрование at rest не добавлено. Нормализованный путь — идентичность записи; внешние
перемещения/переименования будут отдельной задачей11.03, сейчас новый путь — новый ID.

## Проверки и ручная приёмка

Итог07.11.2026: **175 passed /0 failed /0 skipped**, VSTest48с.89 SQL integration и86
прочих cases.9 новых SQL/FS сценариев в ExternalDocumentIntegrationTests (partial
FileServiceTests): round-trip/общая идентичность/actor audit, stale/Windows writer conflicts,
invalid JSON/XXE/UTF-8/size/BOM, SQL-trigger audit failure+rollback/retry, uncommitted
recovery+unexpected bytes, committed recovery другим пользователем, editor dirty/close/
logout, private/traversal/junction/read-only/no session, schema readiness.
WpfShellTests расширен проверками реального DI внешнего service/dialog alias; прежние
unit/SQL/layout тесты сохранены и прошли. Нативный UI-прогон этим не подтверждён.

Команды и результаты на Windows/SDK9.0.200/SQL Express Running:

```powershell
dotnet build SecureFileManager.sln -c Debug --artifacts-path artifacts/external-editor
./scripts/Test-Stage08.ps1 -ArtifactsPath "$PWD/artifacts/external-editor" -ResultsDirectory "$PWD/artifacts/external-editor-final-metadata-20261107"
dotnet build SecureFileManager.sln -c Release --artifacts-path artifacts/external-editor
dotnet ef migrations has-pending-model-changes --project src/SecureFileManager.Data --startup-project src/SecureFileManager.Data
```

Debug/Release PASS,0 warnings/errors. EF: модель соответствует последней миграции.
Новая GUID-БД fixture и TEMP/run-f6c1c2ad10824b599a7ea1c47a78cfec использованы/очищены
только в своём запуске. TRX: artifacts/external-editor-final-metadata-20261107/stage08.trx (ignored).
Ранние targeted12/12 и полный174/174 были до последнего schema readiness теста;
После первоначальных175/175 усиленный metadata assert обнаружил дефект компенсации:
байты восстанавливались, но mtime расходилась с SQL. Plan теперь сохраняет прежнюю дату,
Settle(false) восстанавливает её через удерживаемый write handle. Тест не ослаблен:
даты/размеры проверяются после save и после audit rollback. Точечные recovery/rollback3/3,
затем повторный полный175/175 и Release0/0 выполнены на окончательном коде.
Старые TRX, включая неуспешную точечную проверку, сохранены отдельно.
Окончательный TRX start13:27:26.0685715+03:00, finish13:28:15.7637320+03:00 (49.695с).
Локальные ссылки обновлённой документации проверены;
git diff --check — без whitespace errors. Предупреждения LF→CRLF — политика Git.
Сгенерирован artifacts/external-editor-additive.sql (idempotent script от второй миграции
к третьей): просмотр подтверждает ADD колонок/таблицы/индексов/FK, без UPDATE/DELETE/DROP
пользовательских данных. Скрипт не выполнялся над рабочей БД.

Ручной сценарий **NOT RUN**:

1. После backup/добавочной миграции/F5 открыть внешний учебный TXT, сохранить Ctrl+S,
   повторно открыть и проверить UTF-8. Не использовать ценные файлы для первого прогона.
2. JSON/XML: валидные данные сохраняются, невалидные дают читаемую ошибку и оставляют
   старые байты; безвредный DTD отклонён. Проверить close Save/Discard/Cancel.
3. Изменить только demo-файл Блокнотом во время открытого редактора: Save даёт конфликт,
   буфер остаётся. «Перечитать» — только после подтверждения.
4. A → выйти → B → тот же внешний файл: общий документ доступен, чужого буфера/истории нет.
5. Журнал: свой actor, внешняя цель, правильные read/save/validation results.
6. Личные файлы/импорт/ZIP работают по-прежнему; внешний доступ в Storage запрещён.
7.1024×640/1440×900, Windows100%/150%, Tab/Enter/Ctrl+S. Не симулировать crash/disk-full
   на рабочих данных: для них предназначены изолированные автоматические тесты.

Статус: код, автоматическая проверка и контролируемое обновление рабочей БД выполнены;
открыта native-приёмка. Этап11.02 целиком готовым не объявляется до её подтверждения.

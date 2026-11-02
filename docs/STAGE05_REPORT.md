# Этап 05 — просмотр дисков и управление файлами

Дата: 02.11.2026. **Статус: Частично — реализация и автоматические проверки завершены,
полная интерактивная GUI-приёмка остаётся открытой.**

## 1. Результаты 05.01–05.10

| Пункт | Результат |
|---|---|
| 05.01 | DriveInfo.GetDrives, реальные объёмы/готовность/ФС/тип/метки/проценты, безопасный unavailable |
| 05.02 | Физический список + scoped SQL, папки двойным щелчком, root/up/refresh, относительный breadcrumb |
| 05.03 | TXT и папки, CreateNew/rename без replace, Windows names/границы/CI-конфликты, общая SQL/audit транзакция |
| 05.04 | TXT UTF-8 strict до 10 МиБ, версия handle/hash + RowVersion; остальным только сведения |
| 05.05 | TXT-редактор, Ctrl+S/*, Save/Discard/Cancel, private staging/backup, stale version, обновление UI событием |
| 05.06 | Подтверждение, физическое удаление после commit, soft-delete/история, только пустой каталог |
| 05.07 | Журнал/backup/compensation, ambiguous commit verification, explicit recovery/sync, без auto-adopt |
| 05.08 | Поиск внутри папки, числовая/UTC сортировка, виртуализированная таблица, асинхронный IO и очистка при смене сеанса |
| 05.09 | 66/66 автотестов, 0 skipped; native GUI/диалоги/DPI и отдельная file symlink не подтверждены вручную |
| 05.10 | Явный single-file import до 100 МиБ, источник сохранён, опасные источники/конфликты отклоняются |

Регистрация/вход/сессии прежних этапов прошли регрессию. .NET/solution/архитектура/миграции
не пересоздавались. JSON/XML-редактор, ZIP и режим B не реализованы.

## 2. Файлы

Полный список созданных/изменённых файлов в конце документа. Основные компоненты:
Core/Files, Data/Files/FileTransactionStore, WindowsFileStorage/SafeNativeFile,
WindowsDriveService, Files/Drives/TextEditor VM, WindowsFileDialogs и тематические окна.

## 3. Диски

DriveInfo собирается в Task.Run, ошибки одного носителя не рушат список; empty/unready
и исключение перечисления проверены. Диски не являются доступными файловыми root.
Размеры Б/КБ/МБ/ГБ/ТБ, занятый объём/прогресс настоящие; UI связан через DriveCard.

## 4. Навигация

Только текущий личный root из прежней конфигурации. Полные пути не приходят из VM
как доверенные назначения. Folder path должен принадлежать текущему owner в SQL,
служебные имена запрещены. Корень и родитель доступны кнопками, чужой FileId запрещён.
Недоступные/неучтённые/изменённые/отсутствующие объекты отмечены явно.

## 5. Создание

Backend проверяет Session/Revision/active SQL User/owner/name/path/конфликт.
Данные готовятся в private staging, публикуются без перезаписи. Files/audit сохраняются
на одной SQL connection/transaction. Конкурентное CI-создание проверено двумя сервисами;
ровно один успех, внешний объект не затирается.

## 6. Чтение и редактор

Read только TXT/UTF-8 до 10 МиБ, без автозапуска ОС. Бинарный NUL/неверная кодировка
отклоняются. Editor хранит opaque version и FileId; при новом Session текст очищается
и окно закрывается. Отмена/сохранение/отказ от несохранённых изменений протестированы
на VM с реальным файловым сервисом; native MessageBox остаётся ручным сценарием.

## 7. Сохранение

Старый файл удерживается handle без конкурентного writer, SHA-256/identity/mtime/size
и SQL RowVersion проверяются. Новый файл Flush(true), исходный в backup, новый на место;
это два rename с возможным коротким gap, не единая атомарная SQL/FS-транзакция.
При ошибке старые байты не перезаписываются. После подтверждённого commit — cleanup и Changed
для автообновления текущего списка. Семантика/пределы восстановления — FILE_OPERATIONS.md.

## 8. Удаление

Только подтверждённый объект по owner-scoped ID, пустота проверяется и в SQL, и на диске.
Удаляется собственный backup после commit; до него возможен rollback. Не удаляется
пользовательский корень, непустой каталог и Operations. Soft-deleted записи/старые FK
и повторное создание имени проверены.

## 9. SQL и рассинхронизация

Data координирует существующие репозитории в Serializable + user UPDLOCK/HOLDLOCK;
FS callback не повторяется автоматически. При ambiguous commit ищется FILE_TX_*
по UserId/CorrelationId, неизвестный исход блокирует следующие команды.
«Восстановить» сохраняет committed результат либо откатывает неподтверждённый;
внешние неожиданные байты никогда не удаляются ради уборки.

«Сверить» — только явная учётная операция над выбранным объектом; не auto-import.
Существующий файл без SQL сохраняется, missing metadata не заставляет удалять диск.
Даты managed родителей обновляются вместе с командой. Новых таблиц/миграций нет.

## 10. Поиск/сортировка

Фильтр только по текущему ограниченному списку; SQL выборка детей ограничена папкой,
построение сопоставления линейное через словари. Коллекция публикуется одним Reset,
строки виртуализированы. Сортировка SizeBytes/ModifiedUtc не зависит от форматированных
строк. Устаревшие async результаты другого пользователя отбрасываются.

## 11. Импорт

Обычный OpenFileDialog с явным выбором одного источника, без dereference ярлыков.
Только чтение локального файла в рамках Windows ACL; UNC/network/device/ADS/reparse/
hard links/shortcuts и любые источники внутри всего Storage отвергаются.
Копия до 100 МиБ, лимит фактически записанных байтов, метаданные/audit как FILE_CREATED,
Details FILE_TX_Import. Абсолютный источник не попадает в аудит/журнал.
Исходные байты/место сохранены тестами; рекурсивного импорта нет.

## 12. Тесты

Финальный набор: **66/66, 0 skipped**. Новых test cases 19 сверх прежних 47;
каждый объединяет связанные позитивные/негативные сценарии. Включены drive info/unready,
полный CRUD/UTF-8/лимиты, explicit sync, foreign ID/session revoke, concurrent create,
занятый файл, SQL unavailable, audit failure, IO failure after publish, stale editor,
interrupted transaction, unexpected replacement, ambiguous commit, реальная junction/
hard link и запрет перемещения закреплённого родителя; VM editor close/save/cancel,
UI поиск/сортировка/reset, DI и все шесть макетов/ресурсы/binding trace.

scripts/Test-Stage05.ps1 создаёт новый run-GUID, fixture — новую тестовую SQL DB.
Они удаляются после тестов только при доказанном владении этим запуском; cleanup не следует
reparse points. Ранее существующие БД/каталоги не удаляются. Свой первый диагностический
run (отдельная БД и development-check directory) также проверен и удалён.
TRX stage05.trx и probes находятся в игнорируемых artifacts/TestResults.

## 13. Сборка/рабочая среда

dotnet restore и dotnet build SecureFileManager.sln -c Debug успешны, 0 errors/warnings.
Использован .NET SDK 9.0.200/EF9.0.22/net9.0-windows. Новых NuGet-пакетов нет.
Read-only проверка существующей SecureFileManagerDB: 1 User, 0 Files, обе прежние
миграции, encrypt_option TRUE. Рабочих записей/файлов тесты не создавали.
StorageRoot по-прежнему D:/SecureFileManagerData/Storage, Users существует; fallback/Git Storage нет.

## 14. GUI и ограничения

Сохранены WPF RenderTargetBitmap в screenshots/stage05/final: два размера каркаса,
плотности для Files и форм входа, отдельно TEST-таблица. Это UI fixture, не реальные файлы.
Рендер 1024×640 проверен глазами: toolbar уплотнён, таблица/пустое состояние не перекрываются.
Цвета/иконки/орнаменты/навигация сохранены. Старые root-рендеры stage05 — промежуточные.

Процесс приложения запускался с Development-профилем; целевое окно не получено CUA.
Граф DI/инициализация окон проверены отдельно в WPF STA без SQL/файловых действий.
Нативный интерактивный вход, OpenFileDialog/MessageBox, реальный resize/DPI/клавиатура
ещё не приняты; их нельзя заменить логическим рендером.
Отдельную file symlink создать не удалось; generic reparse rule проверен junction,
но отдельный symlink не выдаётся за протестированный. Настоящий full disk/power loss
не имитировались на пользовательском диске; тесты используют контролируемые IO failures.
ACL/direct OS user/admin isolation и любые аппаратные сбои не покрываются полной гарантией.
Лимиты/исключения/случай незавершённого plan описаны в FILE_OPERATIONS.md.

## 15. Ручная приёмка

Пошаговый документ STAGE05_MANUAL_TEST.md содержит ожидаемые результаты, безопасные
test-only имена, импорт с сохранением источника, SQL-проверку, две сессии, ошибки,
DPI и восстановление. Только после фактической проверки закрыть 05.09.
Коммит не создан, этап 06 не начат.

## 16. Вариант B

IFileService, транзакционная граница, DTO, editor/commands и Windows IO можно переиспользовать,
но OwnerId-scoping/RelativePath unique и actor==owner аудита сохраняют режим A.
B потребует явных Locations/grants, миграции индекса, нового разрешения actor/file
и адаптации policy; нельзя просто передать внешний root. Authentication/Session
не привязаны к дискам, Windows token не подменяется, общего provider/LocationId сейчас нет.

## Полный перечень файлов

- TASKS.md
- TECHNICAL_SPEC.md
- docs/DECISIONS.md
- docs/PERSISTENCE.md
- docs/PROGRESS.md
- docs/STORAGE_ARCHITECTURE.md
- src/SecureFileManager.Core/Interfaces/IFileRepository.cs
- src/SecureFileManager.Core/Security/RelativePathSyntax.cs
- src/SecureFileManager.Data/DependencyInjection/DataServiceCollectionExtensions.cs
- src/SecureFileManager.Data/Repositories/FileRepository.cs
- src/SecureFileManager.Wpf/App.xaml.cs
- src/SecureFileManager.Wpf/Infrastructure/FileSystem/PersonalStorage.cs
- src/SecureFileManager.Wpf/ViewModels/DrivesViewModel.cs
- src/SecureFileManager.Wpf/ViewModels/FilesViewModel.cs
- src/SecureFileManager.Wpf/ViewModels/MainWindowViewModel.cs
- src/SecureFileManager.Wpf/ViewModels/SectionViewModel.cs
- src/SecureFileManager.Wpf/Views/DrivesView.xaml
- src/SecureFileManager.Wpf/Views/FilesView.xaml
- src/SecureFileManager.Wpf/Views/FilesView.xaml.cs
- tests/SecureFileManager.Tests/SecurityIntegrationTests.cs
- tests/SecureFileManager.Tests/SqlDatabaseFixture.cs
- tests/SecureFileManager.Tests/WpfShellTests.cs
- docs/FILE_OPERATIONS.md
- docs/STAGE05_MANUAL_TEST.md
- docs/STAGE05_REPORT.md
- docs/screenshots/stage05/01-1024x640-render.png
- docs/screenshots/stage05/01-1440x900-render.png
- docs/screenshots/stage05/02-1024x640-render.png
- docs/screenshots/stage05/02-1440x900-render.png
- docs/screenshots/stage05/03-1024x640-render.png
- docs/screenshots/stage05/03-1440x900-render.png
- docs/screenshots/stage05/04-1024x640-render.png
- docs/screenshots/stage05/04-1440x900-render.png
- docs/screenshots/stage05/05-1024x640-render.png
- docs/screenshots/stage05/05-1440x900-render.png
- docs/screenshots/stage05/06-1024x640-render.png
- docs/screenshots/stage05/06-1440x900-render.png
- docs/screenshots/stage05/README.md
- docs/screenshots/stage05/files-1024x640-150pct-render.png
- docs/screenshots/stage05/files-1440x900-150pct-render.png
- docs/screenshots/stage05/files-TEST-fixture-render.png
- docs/screenshots/stage05/final/01-1024x640-render.png
- docs/screenshots/stage05/final/01-1440x900-render.png
- docs/screenshots/stage05/final/02-1024x640-render.png
- docs/screenshots/stage05/final/02-1440x900-render.png
- docs/screenshots/stage05/final/03-1024x640-render.png
- docs/screenshots/stage05/final/03-1440x900-render.png
- docs/screenshots/stage05/final/04-1024x640-render.png
- docs/screenshots/stage05/final/04-1440x900-render.png
- docs/screenshots/stage05/final/05-1024x640-render.png
- docs/screenshots/stage05/final/05-1440x900-render.png
- docs/screenshots/stage05/final/06-1024x640-render.png
- docs/screenshots/stage05/final/06-1440x900-render.png
- docs/screenshots/stage05/final/files-1024x640-150pct-render.png
- docs/screenshots/stage05/final/files-1440x900-150pct-render.png
- docs/screenshots/stage05/final/files-TEST-fixture-render.png
- docs/screenshots/stage05/final/login-520x600-100pct-render.png
- docs/screenshots/stage05/final/login-520x600-150pct-render.png
- docs/screenshots/stage05/final/login-640x760-100pct-render.png
- docs/screenshots/stage05/final/login-640x760-150pct-render.png
- docs/screenshots/stage05/final/register-520x600-100pct-render.png
- docs/screenshots/stage05/final/register-520x600-150pct-render.png
- docs/screenshots/stage05/final/register-640x760-100pct-render.png
- docs/screenshots/stage05/final/register-640x760-150pct-render.png
- docs/screenshots/stage05/login-520x600-100pct-render.png
- docs/screenshots/stage05/login-520x600-150pct-render.png
- docs/screenshots/stage05/login-640x760-100pct-render.png
- docs/screenshots/stage05/login-640x760-150pct-render.png
- docs/screenshots/stage05/register-520x600-100pct-render.png
- docs/screenshots/stage05/register-520x600-150pct-render.png
- docs/screenshots/stage05/register-640x760-100pct-render.png
- docs/screenshots/stage05/register-640x760-150pct-render.png
- docs/sql/stage05-verification.txt
- docs/sql/verify-stage05.sql
- scripts/Test-Stage05.ps1
- src/SecureFileManager.Core/Files/FileContracts.cs
- src/SecureFileManager.Core/Files/FileService.cs
- src/SecureFileManager.Data/Files/FileTransactionStore.cs
- src/SecureFileManager.Wpf/Infrastructure/Dialogs/WindowsFileDialogs.cs
- src/SecureFileManager.Wpf/Infrastructure/FileSystem/SafeNativeFile.cs
- src/SecureFileManager.Wpf/Infrastructure/FileSystem/WindowsDriveService.cs
- src/SecureFileManager.Wpf/Infrastructure/FileSystem/WindowsFileStorage.cs
- src/SecureFileManager.Wpf/Resources/Controls.xaml
- src/SecureFileManager.Wpf/ViewModels/FileDialogs.cs
- src/SecureFileManager.Wpf/ViewModels/TextEditorViewModel.cs
- src/SecureFileManager.Wpf/Views/NameDialog.xaml
- src/SecureFileManager.Wpf/Views/NameDialog.xaml.cs
- src/SecureFileManager.Wpf/Views/TextEditorWindow.xaml
- src/SecureFileManager.Wpf/Views/TextEditorWindow.xaml.cs
- tests/SecureFileManager.Tests/DriveTests.cs
- tests/SecureFileManager.Tests/FileServiceTests.cs

Проверка только чтением рабочей БД и удаления собственных тестовых ресурсов: sql/stage05-verification.txt.

## Исправление по ручной приёмке 02.11.2026

Обнаруженная пользователем XamlParseException в карточке диска воспроизведена
автоматически. ProgressBar.Value теперь явно OneWay к read-only DriveCard.Percent.
WpfShellTests создаёт готовую/недоступную карточки вместо проверки только пустого
списка, проверяет 75%/0%, режим привязки и оба размера layout.
Новый сценарий падал до исправления и проходит после. Полная регрессия: 66/66,
без пропусков; сборка: 0 ошибок/предупреждений. Это исправляет конкретный дефект,
но не заменяет оставшуюся ручную GUI-приёмку. TASKS 05.09 остаётся открыт.

## 02.11.2026 — ручная приёмка: создание TXT и ошибка после импорта

Исправлены два воспроизведённых дефекта UI:
1. NameDialog.InitializeComponent падал с XamlParseException/ArgumentNullException
   в Controls.xaml: WindowStartupLocation — обычное CLR-свойство, недопустимое в Setter.
   Setter удалён, CenterOwner установлен непосредственно в NameDialog.xaml.
2. Успешный импорт сопровождался ошибкой обновления списка: Items.Add внутри
   View.DeferRefresh вызывал InvalidOperationException при обращении CollectionView
   к CurrentPosition. Новый список теперь подготавливается целиком и публикуется
   одним Reset без поэлементных событий во время отложенного Refresh. Поиск/сортировка
   и выделение проверены при повторной загрузке. Ошибка загрузки списка имеет отдельный
   текст и больше не объявляет ранее подтверждённую файловую операцию неподтверждённой.

Read-only проверка рабочей БД подтвердила существующую запись импортированного TXT
(66 байт, IsDeleted=0) и FILE_CREATED/Success/FILE_TX_Import. Служебная папка содержит
только owner/lock, незавершённых транзакционных подкаталогов не найдено.
Пользовательский файл не читался, не удалялся и не импортировался повторно.

WpfShellTests теперь создаёт оба NameDialog, выполняет UI-сценарий импорта/создания
TXT/папки/повторного обновления с привязанным DataGrid и инициализирует TXT-редактор.
Используется только UI fixture; реальный SQL/FS покрыт прежними интеграционными тестами.
Оба исходных исключения воспроизведены до исправления; после UI-тест проходит.
В первом полном прогоне 65/66: старый тест смены пользователей использовал
WPF CollectionView на разных async worker-потоках. Тест сохранён и исправлен:
вся его последовательность выполняется на одном STA Dispatcher, проверки не ослаблены.

Итоговый полный прогон scripts/Test-Stage05.ps1: 66/66, 0 failed, 0 skipped,
42 секунды. Созданные запуском тестовые ресурсы очищены. Сборка Debug --no-restore:
0 ошибок, 0 предупреждений. Restore выполнялся dotnet test автоматически.
Обновлены TASKS/отчёт; полная ручная приёмка 05.09 остаётся открытой.
Бизнес-логика, БД, миграции, StorageRoot и правила доступа не менялись.
Этап 06 не начат, Git-коммит не создавался.
# Этап 04 — регистрация, вход, доступ и сессии

Дата: 01.11.2026. **Статус: Частично — реализация и автоматические проверки завершены;
ручная приёмка полного GUI-сценария остаётся открытой в 04.07.**

## 1. Результат 04.01–04.07

| Пункт | Реализовано и проверено |
|---|---|
| 04.01 | Штатный PasswordHasher, соль, формат IdentityV3, проверка и обновление устаревшего хеша |
| 04.02 | Валидация, подтверждение, CI-уникальность, конкурентная регистрация, GUID-каталог, согласование SQL/IO |
| 04.03 | Вход, активность, единая ошибка, скользящее окно, блокировка, тестируемое UTC-время, SQL-блокировки |
| 04.04 | Сессия без секретов, выход с аудитом, очистка VM, возврат к новой форме входа; A → выход → B проверено сервисами |
| 04.05 | Конфигурируемый личный корень, ownership, синтаксис и физическая проверка, запрет junction/reparse и hard links |
| 04.06 | Существующая Operations, обязательный аудит входа/выхода/отказов, общая SQL-транзакция критических изменений |
| 04.07 | 40/40 автоматических тестов без пропусков; ручной GUI-сценарий с двумя пользователями/реальным DPI не принят |

Этап 05, Windows filesystem mode B и импорт не реализованы. Solution, TFM, существующие
миграции, файловые репозитории и египетский каркас не пересоздавались. Git-коммита нет.

## 2. Схема входа и регистрации

Регистрация валидирует логин 3–50 без пробельных/управляющих символов, пароль 10–128
(не только пробелы), точное совпадение подтверждения. Коллация существующего unique-индекса
Users.Username обеспечивает уникальность без учёта регистра. При гонке используется
serializable + UPDLOCK/HOLDLOCK и окончательное ограничение SQL; ровно один победитель.

Хеш вычисляется вне SQL-транзакции. Затем вставка User, подготовка GUID-каталога и
DIRECTORY_CREATED проходят до commit; SQL-вставка и аудит — в одной транзакции.
При ошибке каталога User откатывается, OPERATION_FAILED записывается отдельно без FK
к несохранённому User. При ошибке аудита пользователь не сохраняется. Ни при одном
отказе существующие файлы/каталоги не удаляются. После регистрации нужен отдельный вход.

Общей атомарной SQL/NTFS-транзакции нет: после rollback/неопределённого commit может
остаться пустой GUID-каталог. Повтор использует тот же GUID при SQL-deadlock retry;
после неопределённого результата сначала пробуют вход, затем повтор регистрации.
Для существующего пользователя отсутствующий каталог восстанавливается при открытии
рабочей области. Автоудаления сирот и перемещения корня нет. Сбой аудита такого
восстановления блокирует открытие; уже созданный каталог сохраняется для безопасного повтора.

При входе проверяются активность, блокировка, пароль и возможность commit LOGIN_SUCCESS.
До подтверждённого commit сессия не создаётся. Неизвестный/неактивный/заблокированный
логин и неверный пароль дают одинаковое «Неверный логин или пароль». Dummy PBKDF2
уменьшает простую разницу вычислительной стоимости; абсолютная защита от timing analysis
и распределённого перебора не заявляется. При SQL/аудит-сбое вход запрещён.

## 3. Хеширование

Новый пакет Core: Microsoft.Extensions.Identity.Core 9.0.22 — стандартный PasswordHasher
для текущего .NET 9; не добавляет EF/WPF/SQL в Core. IdentityV3 использует PBKDF2,
HMAC-SHA512, случайную соль и версионируемый формат с параметрами. Выбрано 600000
итераций. VerifyHashedPassword проверяет штатно; SuccessRehashNeeded приводит
к обновлению PasswordHash в транзакции успешного входа. Проверен upgrade с 100000 итераций.

PasswordBox скрывает ввод, очищается при отправке/выгрузке. PasswordInput — кратковременный
контейнер с редактированным ToString, очищается до ожидания сервиса; пароль не становится
bindable-свойством VM и не хранится в Session. Managed string невозможно гарантированно
обнулить; неизменяемые строки остаются ограничением .NET, минимизируется время ссылок.
Открытые пароли, хеши, строки подключения и содержимое файлов в аудит/диагностику не пишутся.
Константы паролей в тестах относятся только к изолированным случайным тестовым аккаунтам;
в приложении нет seed, демонстрационного или административного аккаунта.

## 4. Блокировка

LoginProtectionPolicy по умолчанию: пять ошибок в скользящих 15 минутах, блокировка 15 минут.
IClock задаёт UTC. FailedLoginWindowStartUtc — начало актуального набора ошибок;
точные последние ошибки берутся из Operations (INVALID_PASSWORD_COUNTED) с ограничением
текущим счётчиком. Успех обнуляет счётчик/окно/блокировку. После истечения блокировки
начинается новое окно; запросы во время блокировки не продлевают её.

Проверка и обновление счётчика + LOGIN_FAILED атомарны в SQL. UPDLOCK/HOLDLOCK сериализует
попытки разных экземпляров сервиса, не только UI. Deadlock 1205 повторяется максимум
дважды (три попытки всего); неопределённый commit автоматически не повторяется.
SemaphoreSlim сериализует переходы одной сессии, UI блокирует повтор отправки.
Политика вынесена в параметризуемый неизменяемый объект, в DI установлены 5/15/15.

## 5. Сессии и WPF

IUserSession хранит только SessionIdentity(Id, Username), Revision и событие Changed.
Мутировать UserSession могут сервисы Core, VM получает читаемый контракт. После входа
открывается существующий MainWindow; имя реально вошедшего пользователя отображается
в боковой панели, футере и настройках. Пароль/PasswordHash в Session отсутствуют.

Выход сначала пытается сохранить LOGOUT, затем в finally очищает Session даже при
сбое SQL/отмене. Если аудит не подтверждён, показывается ошибка, успех журнала не обещается.
Все SectionVM очищают состояние; списки/выбор/ошибки сбрасываются, выбран раздел «Мои файлы».
MainWindow закрывается; новая AuthenticationWindow создаёт новые Login/Register VM.
Закрытие главного окна завершает сеанс и приложение. Сохранённых токенов нет.

LoginView/RegisterView используют имеющиеся ResourceDictionary, Eye of Horus,
верхний/нижний орнаменты, navy/папирус/золото/бирюзу. В code-behind только PasswordBox
и передача команды; SQL/проверок доступа там нет. Enter — default button, Tab — обычная
клавиатурная навигация, есть busy/progress/русская валидация и прокрутка. При запуске
IDatabaseConnectionCheck проверяет БД, «Повторить» повторяет проверку; результат
не считается разрешением входа и не гарантирует будущую доступность аудита.

## 6. Хранилище и доступ

По умолчанию: %LOCALAPPDATA%/SecureFileManager/Storage/Users/{UserId:D}/.
SFM_STORAGE_ROOT задаёт доверенный абсолютный локальный корень Storage; Users/{GUID}
добавляет адаптер. Корень вне Git, /Storage/ дополнительно игнорируется на случай
локального размещения в репозитории. Относительный корень, UNC/сетевой накопитель
и корень диска не принимаются.

Authentication не зависит от пути. Registration использует узкий доверенный порт
IPersonalStorageProvisioner; подготовка рабочей области и проверки — IUserStorageService.
IFileAccessPolicy проверяет actor == owner. UserId из VM сравнивается с текущей Session,
для FileId запись читается только в scope текущего владельца; активность пользователя
и доступность БД перепроверяются. Revision проверяется после await, чтобы смена сессии
не разрешала действие со старым контекстом.

IPathPolicy/RelativePathSyntax проверяют имена Windows, сегменты, абсолютные/UNC/drive
пути, dot/dotdot, зарезервированные имена, длины, ADS, разделители и GetRelativePath-границу.
CheckChain проверяет reparse points во всех существующих компонентах от корня диска
до объекта перед/после создания каталога; существующие обычные файлы проверяются
handle-метаданными на число hard links. Работа Windows ACL не обходится: ошибки доступа
блокируют подготовку/проверку. ACL чужих или собственных каталогов не изменялись;
наследуются фактические ACL выбранного корня. Для корня по умолчанию это профиль Windows.

## 7. Границы безопасности

Подтверждены тестами: соль/формат/неверный хеш/rehash, валидация и CI-конкурентность,
неактивная учётная запись, одинаковые ошибки, точные временные окна, конкурентный счётчик,
две последовательные сессии и очистка VM, разные GUID-каталоги, чужие ID, traversal,
абсолютные пути, реальная NTFS junction и hard link, сохранность sentinel-файлов,
сбой каталога, недоступная БД, откат User/счётчика при невозможности аудита, выход при заранее отменённом запросе,
запрет корня диска (включая нормализованный путь) и выход fail-closed.
Существующие FK/unique/soft-delete/UTC/append-only/SQL-параметры и TLS-тесты проходят.

Не заявляются: полная изоляция двух app-пользователей от прямого доступа одного Windows
пользователя, защита от администратора/доступа к БД напрямую, handle-bound атомарная
проверка последующих файловых мутаций. Между проверкой пути и использованием возможны
TOCTOU-гонки; этап 05 должен работать с конечными handle/повторными проверками,
безопасными FileMode/temp/компенсацией. Отдельная Windows symlink не создавалась:
проверена junction как реальный reparse point; все reparse-флаги отклоняются общим правилом.
Реальные ACL-отказы/недоступный физический накопитель не воспроизводились отдельно.

## 8. Аудит и транзакции

Используется только Operations и прежний IOperationRepository. SecurityAudit формирует UTC,
определённый UserId либо NULL, безопасные коды Details и снимок имени; FileId и путь NULL
для безобъектных событий. Введённый валидный неизвестный логин сохраняется в
ActorNameSnapshot, не предоставляется глобальный просмотр неизвестных попыток в UI.

IdentityStore предоставляет минимальную транзакционную границу сценариям identity:
дочерний контекст существующего OperationRepository присоединяется к той же connection/
transaction. Не переписаны рабочие метаданные/репозитории. Граница не заменяет будущую
координацию файловых операций этапа 05. Logout очищает сессию даже при отказе аудита.
Если БД недоступна, записать событие в неё невозможно — локальный фиктивный журнал не создаётся.

## 9. SQL Server и миграция

Добавлена только 20261101122237_AddLoginFailureWindow: nullable datetime2
Users.FailedLoginWindowStartUtc. InitialCreate не изменена, Files/Operations и FK/индексы
не менялись. Миграция применена после анализа существующей SecureFileManagerDB.
Рабочие счётчики до/после: Users=0, Files=0, Operations=0. Тестовых пользователей в неё
не добавляли. История содержит обе миграции, encrypt_option=TRUE.

SQL миграции: docs/sql/stage04-migration.sql; безопасная read-only проверка:
docs/sql/verify-stage04.sql; фактический вывод — stage04-verification.txt.
Применение к другой существующей базе: сначала backup/анализ sys.columns/history/данных,
затем dotnet ef database update с прежними Data/startup Data и локальной конфигурацией.
WPF не применяет миграции автоматически; Down/database update 0 не запускались.
Development-исключение сертификата осталось локальным; Production по-прежнему
не допускает TrustServerCertificate=True, Encrypt=False запрещён.

## 10. Сборка и автоматические тесты

В Windows выполнены dotnet restore, dotnet build SecureFileManager.sln -c Debug
(0 ошибок/предупреждений), dotnet test SecureFileManager.sln -c Debug с
SFM_RUN_SQL_TESTS=1, SFM_ENVIRONMENT=Development: **40/40, 0 skipped**.
После уточнения UI fixture отдельно пройден WpfShellTests: 1/1; затем
весь финальный набор повторён: 40/40.
Результаты TRX находятся в игнорируемом tests/SecureFileManager.Tests/TestResults/.
EF has-pending-model-changes: изменений после миграции нет.

SQL-прогон использовал новую SecureFileManagerTests_40b6d9ba82af4e82931a07f373c75bc1:
13 тестовых Users, 1 Files, 56 Operations; включая NULL UserId/FileId для неизвестного
логина. Агрегированный вывод без секретов — docs/sql/stage04-test-verification.txt.
Тестовые каталоги — %TEMP%/SecureFileManager-stage04-tests/GUID. Базы/каталоги сохраняются
для анализа, автоматического DROP/recursive delete нет. Созданные тестом триггеры
сбойной записи аудита удаляются в finally исключительно в этой тестовой БД.

## 11. GUI-проверки

Запущен окончательный Wpf.exe с существующей локальной конфигурацией: UI Automation
подтверждает только AuthenticationWindow, PasswordBox, кнопки и статус
«Подключение к БД проверено. Вход требует проверки пароля и аудита».
Проверочный процесс остановлен; реальные учётные записи не вводились.

WpfShellTests в настоящем WPF/STA отрисовал обе формы в 640×760 и 520×600,
переключил все шесть разделов в 1440×900 и 1024×640; 18 ресурсов загружаются,
ошибок DataBinding trace нет. Сохранены 22 RenderTargetBitmap PNG в
docs/screenshots/stage04/. Главный каркас использует явно тестовую сессию только
в UI fixture и пустые таблицы, не рабочие данные и не встроенный demo-режим приложения.

Рендер плотности 150% не заменяет реальный Windows DPI. Desktop capture вернул
window capture timed out, ввод — coordinate input geometry is unavailable.
Повторный видимый интерактивный запуск и активация дали тот же timeout захвата.
Полный сценарий GUI регистрации/входа/выхода, клавиатура и системные кнопки при разных
DPI остаются для ручной проверки по STAGE04_MANUAL_TEST.md.
Первый тестовый прогон был остановлен из-за зависимости VM от чужого Application
Dispatcher; исправлен захват только собственного dispatcher, повторные прогоны успешны.
Одна сборка была заблокирована запущенным smoke-exe; после остановки только этого
экземпляра итоговая сборка прошла без предупреждений.

## 12. Готовность к варианту B

Session/PasswordHasher/Authentication не знают физических корней и Windows identity.
Политика владения и path adapter отделены. IStorageProvider, LocationId и общие папки
не добавлены. Files.RelativePath по-прежнему уникален среди активных файлов владельца;
аудит с FileId по-прежнему требует actor == owner. Для B потребуются locations/grants,
миграции индекса и расширение audit-scoping, но переписывать проверку пароля не нужно.
Никакого автоматического обхода Windows ACL нет. Подробности — STORAGE_ARCHITECTURE.md.

## 13. Что осталось

Пройти и зафиксировать ручную GUI-приёмку по отдельному чек-листу, затем завершить
04.07 и общий статус этапа. Реализацию Stage05/импорта/JSON/XML/ZIP не начинать
без нового разрешения. Формальный риск согласования WPF/SQL Server у преподавателя
остаётся ранее задокументированным открытым вопросом.

## 14. Созданные и изменённые файлы

Полный перечень приводится ниже; локальная конфигурация/секреты, bin/obj и TRX в него
не включены. Список относится к текущему незакоммиченному результату Stage04.

### Созданы (56)

- [docs/STAGE04_MANUAL_TEST.md](../docs/STAGE04_MANUAL_TEST.md)
- [docs/STAGE04_REPORT.md](../docs/STAGE04_REPORT.md)
- [docs/screenshots/stage04/01-1024x640-render.png](../docs/screenshots/stage04/01-1024x640-render.png)
- [docs/screenshots/stage04/01-1440x900-render.png](../docs/screenshots/stage04/01-1440x900-render.png)
- [docs/screenshots/stage04/02-1024x640-render.png](../docs/screenshots/stage04/02-1024x640-render.png)
- [docs/screenshots/stage04/02-1440x900-render.png](../docs/screenshots/stage04/02-1440x900-render.png)
- [docs/screenshots/stage04/03-1024x640-render.png](../docs/screenshots/stage04/03-1024x640-render.png)
- [docs/screenshots/stage04/03-1440x900-render.png](../docs/screenshots/stage04/03-1440x900-render.png)
- [docs/screenshots/stage04/04-1024x640-render.png](../docs/screenshots/stage04/04-1024x640-render.png)
- [docs/screenshots/stage04/04-1440x900-render.png](../docs/screenshots/stage04/04-1440x900-render.png)
- [docs/screenshots/stage04/05-1024x640-render.png](../docs/screenshots/stage04/05-1024x640-render.png)
- [docs/screenshots/stage04/05-1440x900-render.png](../docs/screenshots/stage04/05-1440x900-render.png)
- [docs/screenshots/stage04/06-1024x640-render.png](../docs/screenshots/stage04/06-1024x640-render.png)
- [docs/screenshots/stage04/06-1440x900-render.png](../docs/screenshots/stage04/06-1440x900-render.png)
- [docs/screenshots/stage04/README.md](../docs/screenshots/stage04/README.md)
- [docs/screenshots/stage04/files-1024x640-150pct-render.png](../docs/screenshots/stage04/files-1024x640-150pct-render.png)
- [docs/screenshots/stage04/files-1440x900-150pct-render.png](../docs/screenshots/stage04/files-1440x900-150pct-render.png)
- [docs/screenshots/stage04/login-520x600-100pct-render.png](../docs/screenshots/stage04/login-520x600-100pct-render.png)
- [docs/screenshots/stage04/login-520x600-150pct-render.png](../docs/screenshots/stage04/login-520x600-150pct-render.png)
- [docs/screenshots/stage04/login-640x760-100pct-render.png](../docs/screenshots/stage04/login-640x760-100pct-render.png)
- [docs/screenshots/stage04/login-640x760-150pct-render.png](../docs/screenshots/stage04/login-640x760-150pct-render.png)
- [docs/screenshots/stage04/register-520x600-100pct-render.png](../docs/screenshots/stage04/register-520x600-100pct-render.png)
- [docs/screenshots/stage04/register-520x600-150pct-render.png](../docs/screenshots/stage04/register-520x600-150pct-render.png)
- [docs/screenshots/stage04/register-640x760-100pct-render.png](../docs/screenshots/stage04/register-640x760-100pct-render.png)
- [docs/screenshots/stage04/register-640x760-150pct-render.png](../docs/screenshots/stage04/register-640x760-150pct-render.png)
- [docs/sql/stage04-migration.sql](../docs/sql/stage04-migration.sql)
- [docs/sql/stage04-test-verification.txt](../docs/sql/stage04-test-verification.txt)
- [docs/sql/stage04-verification-notes.md](../docs/sql/stage04-verification-notes.md)
- [docs/sql/stage04-verification.txt](../docs/sql/stage04-verification.txt)
- [docs/sql/verify-stage04.sql](../docs/sql/verify-stage04.sql)
- [src/SecureFileManager.Core/Interfaces/ISecurityServices.cs](../src/SecureFileManager.Core/Interfaces/ISecurityServices.cs)
- [src/SecureFileManager.Core/Results/SecurityOutcomes.cs](../src/SecureFileManager.Core/Results/SecurityOutcomes.cs)
- [src/SecureFileManager.Core/Security/IdentityValidation.cs](../src/SecureFileManager.Core/Security/IdentityValidation.cs)
- [src/SecureFileManager.Core/Security/LoginProtectionPolicy.cs](../src/SecureFileManager.Core/Security/LoginProtectionPolicy.cs)
- [src/SecureFileManager.Core/Security/PasswordSecurity.cs](../src/SecureFileManager.Core/Security/PasswordSecurity.cs)
- [src/SecureFileManager.Core/Security/RelativePathSyntax.cs](../src/SecureFileManager.Core/Security/RelativePathSyntax.cs)
- [src/SecureFileManager.Core/Services/AuthenticationService.cs](../src/SecureFileManager.Core/Services/AuthenticationService.cs)
- [src/SecureFileManager.Core/Services/RegistrationService.cs](../src/SecureFileManager.Core/Services/RegistrationService.cs)
- [src/SecureFileManager.Core/Services/SecurityAudit.cs](../src/SecureFileManager.Core/Services/SecurityAudit.cs)
- [src/SecureFileManager.Core/Services/UserSession.cs](../src/SecureFileManager.Core/Services/UserSession.cs)
- [src/SecureFileManager.Core/Services/UserStorageService.cs](../src/SecureFileManager.Core/Services/UserStorageService.cs)
- [src/SecureFileManager.Data/Identity/DatabaseConnectionCheck.cs](../src/SecureFileManager.Data/Identity/DatabaseConnectionCheck.cs)
- [src/SecureFileManager.Data/Identity/IdentityStore.cs](../src/SecureFileManager.Data/Identity/IdentityStore.cs)
- [src/SecureFileManager.Data/Migrations/20261101122237_AddLoginFailureWindow.Designer.cs](../src/SecureFileManager.Data/Migrations/20261101122237_AddLoginFailureWindow.Designer.cs)
- [src/SecureFileManager.Data/Migrations/20261101122237_AddLoginFailureWindow.cs](../src/SecureFileManager.Data/Migrations/20261101122237_AddLoginFailureWindow.cs)
- [src/SecureFileManager.Wpf/Infrastructure/FileSystem/PersonalStorage.cs](../src/SecureFileManager.Wpf/Infrastructure/FileSystem/PersonalStorage.cs)
- [src/SecureFileManager.Wpf/ViewModels/AuthenticationViewModels.cs](../src/SecureFileManager.Wpf/ViewModels/AuthenticationViewModels.cs)
- [src/SecureFileManager.Wpf/Views/AuthenticationWindow.xaml](../src/SecureFileManager.Wpf/Views/AuthenticationWindow.xaml)
- [src/SecureFileManager.Wpf/Views/AuthenticationWindow.xaml.cs](../src/SecureFileManager.Wpf/Views/AuthenticationWindow.xaml.cs)
- [src/SecureFileManager.Wpf/Views/LoginView.xaml](../src/SecureFileManager.Wpf/Views/LoginView.xaml)
- [src/SecureFileManager.Wpf/Views/LoginView.xaml.cs](../src/SecureFileManager.Wpf/Views/LoginView.xaml.cs)
- [src/SecureFileManager.Wpf/Views/RegisterView.xaml](../src/SecureFileManager.Wpf/Views/RegisterView.xaml)
- [src/SecureFileManager.Wpf/Views/RegisterView.xaml.cs](../src/SecureFileManager.Wpf/Views/RegisterView.xaml.cs)
- [tests/SecureFileManager.Tests/PasswordTests.cs](../tests/SecureFileManager.Tests/PasswordTests.cs)
- [tests/SecureFileManager.Tests/SecurityIntegrationTests.cs](../tests/SecureFileManager.Tests/SecurityIntegrationTests.cs)
- [tests/SecureFileManager.Tests/StorageConfigurationTests.cs](../tests/SecureFileManager.Tests/StorageConfigurationTests.cs)

### Изменены (25)

- [.gitignore](../.gitignore)
- [TASKS.md](../TASKS.md)
- [docs/DECISIONS.md](../docs/DECISIONS.md)
- [docs/PERSISTENCE.md](../docs/PERSISTENCE.md)
- [docs/PROGRESS.md](../docs/PROGRESS.md)
- [docs/STORAGE_ARCHITECTURE.md](../docs/STORAGE_ARCHITECTURE.md)
- [src/SecureFileManager.Core/Entities/User.cs](../src/SecureFileManager.Core/Entities/User.cs)
- [src/SecureFileManager.Core/SecureFileManager.Core.csproj](../src/SecureFileManager.Core/SecureFileManager.Core.csproj)
- [src/SecureFileManager.Data/AppDbContext.cs](../src/SecureFileManager.Data/AppDbContext.cs)
- [src/SecureFileManager.Data/DependencyInjection/DataServiceCollectionExtensions.cs](../src/SecureFileManager.Data/DependencyInjection/DataServiceCollectionExtensions.cs)
- [src/SecureFileManager.Data/Migrations/AppDbContextModelSnapshot.cs](../src/SecureFileManager.Data/Migrations/AppDbContextModelSnapshot.cs)
- [src/SecureFileManager.Wpf/App.xaml.cs](../src/SecureFileManager.Wpf/App.xaml.cs)
- [src/SecureFileManager.Wpf/Resources/Controls.xaml](../src/SecureFileManager.Wpf/Resources/Controls.xaml)
- [src/SecureFileManager.Wpf/Resources/Converters.xaml](../src/SecureFileManager.Wpf/Resources/Converters.xaml)
- [src/SecureFileManager.Wpf/Resources/Views.xaml](../src/SecureFileManager.Wpf/Resources/Views.xaml)
- [src/SecureFileManager.Wpf/ViewModels/ArchivesViewModel.cs](../src/SecureFileManager.Wpf/ViewModels/ArchivesViewModel.cs)
- [src/SecureFileManager.Wpf/ViewModels/FilesViewModel.cs](../src/SecureFileManager.Wpf/ViewModels/FilesViewModel.cs)
- [src/SecureFileManager.Wpf/ViewModels/JournalViewModel.cs](../src/SecureFileManager.Wpf/ViewModels/JournalViewModel.cs)
- [src/SecureFileManager.Wpf/ViewModels/MainWindowViewModel.cs](../src/SecureFileManager.Wpf/ViewModels/MainWindowViewModel.cs)
- [src/SecureFileManager.Wpf/ViewModels/SectionViewModel.cs](../src/SecureFileManager.Wpf/ViewModels/SectionViewModel.cs)
- [src/SecureFileManager.Wpf/ViewModels/SettingsViewModel.cs](../src/SecureFileManager.Wpf/ViewModels/SettingsViewModel.cs)
- [src/SecureFileManager.Wpf/Views/FilesView.xaml](../src/SecureFileManager.Wpf/Views/FilesView.xaml)
- [src/SecureFileManager.Wpf/Views/MainWindow.xaml](../src/SecureFileManager.Wpf/Views/MainWindow.xaml)
- [src/SecureFileManager.Wpf/Views/SettingsView.xaml](../src/SecureFileManager.Wpf/Views/SettingsView.xaml)
- [tests/SecureFileManager.Tests/WpfShellTests.cs](../tests/SecureFileManager.Tests/WpfShellTests.cs)

# Требования к будущему локальному CI/CD

Дата07.11.2026. Только план. Workflow YAML,runner,hook,контейнеры и deployment scripts
не создавались и не запускались. Текущий remote — GitHub;в репозитории есть PowerShell
stage-test scripts,но это ещё не Ubuntu CI/CD.

## 1. Что известно о задании №2

Из поручения известны Ubuntu в VirtualBox,Docker Engine/Compose,MySQL,ASP.NET Core/
Blazor,C#,Build,Test,SAST,Local Deploy,cache,artifacts,автоматическая остановка при
High/Critical,безопасные секреты. Исходного PDF/полного текста задания№2 в доступном
дереве не найдено. Поэтому соответствие проверяется **этим перечисленным требованиям**;
формальное выполнение всех пунктов преподавательской методички не подтверждено.
До реализации уточнить,считается ли act локальным CI/CD,нужен ли автоматический trigger
на push и обязателен ли конкретный scanner/runner/формат evidence.

## 2. Сравнение вариантов

| Критерий | GitHub Actions + act в Ubuntu | GitLab CI + Runner |
|---|---|---|
| Текущий GitHub repo | без переноса remote,workflow совместим по структуре | новый GitLab project/mirror либо отдельно согласованный перенос |
| Локальное исполнение | act выполняет jobs через локальный Docker | runner на VM выполняет jobs,координатор GitLab local/server |
| Полная автономность | после подготовки images/packages/rules можно работать локально с кешами;не весь GitHub API | self-managed GitLab+runner даёт локальный координатор;GitLab.com+local runner не полностью offline |
| Расход ресурсов VM | act+build/test containers,меньше отдельных сервисов | self-managed GitLab заметно усложняет VM и эксплуатацию;ресурсы измерить,не обещать конкретный минимум |
| Triggers | ручной act — не автоматическая реакция на git push | coordinator native push pipelines |
| Artifacts/cache | host directories/local services,не считать cloud upload/download автоматически доступными | нативные artifacts/cache coordinator/runner,нужна настройка хранения |
| Semantics | не полная эмуляция GitHub:concurrency/permissions/timeouts/cancel/environments имеют ограничения | native .gitlab-ci.yml stages/needs/when;executor security зависит от настройки |
| Deploy на Docker host | требует доверенного host-side helper/runner,Docker permission существенное полномочие | shell runner проще,но host isolation слабее;Docker executor тоже требует продуманного deploy доступа |
| Windows WPF tests | act Linux не выполняет WPF suite | отдельный Windows runner,если нужен CI Windows baseline |
| Сложность учебного старта | меньше при разрешённом local act | оправдано при требовании полноценного coordinator/автоматического pipeline UI |

**Рекомендация:** GitHub Actions-compatible workflow + act как простой локальный вариант,
при подтверждении допустимости преподавателем. Один общий набор будущих `ci/*.sh`
исполняет реальные build/test/scan/gate/deploy;YAML лишь задаёт порядок. Никаких шагов,
которые только печатают названия стадий. Если требуется серверная автоматизация событий
и UI pipeline,выбрать GitLab+local Runner до WP11;не поддерживать два конвейера сразу.

Возможный trigger для act после отдельного согласования:локальный bare-repo post-receive
hook доверенной ветки → host orchestration с immutable commit/isolated checkout,flock и
timeout;либо ручной `act workflow_dispatch` с полностью автоматическими последующими
стадиями,если задания достаточно. Не добавлять такой hook/службу в этапе12. Git hooks
не объявлять готовой интеграцией GitHub push,а act schedule — работающим daemon.

Документация act прямо указывает неполную совместимость:в частности нельзя полагаться
на concurrency/permissions/timeouts/environment-secret scoping и cancellation как на
границы безопасности. Эти гарантии должны обеспечивать host wrapper/права ОС,а не один
workflow. [act unsupported features](https://nektosact.com/not_supported.html).

GitLab shell executor выполняет jobs на host с ограниченной изоляцией и находится в
maintenance mode;для нового решения оценить поддерживаемый executor. Runner разрешать
только для доверенного проекта/ветки. [GitLab Shell](https://docs.gitlab.com/runner/executors/shell/),
[Runner security](https://docs.gitlab.com/runner/security/).

## 3. Стадии и блокирующие результаты

| Стадия | Реальная работа | Артефакты | Условие перехода |
|---|---|---|---|
| Preflight/Restore | pinned SDK/provider/tools/images,rules hashes,locked NuGet restore,secrets availability без вывода | version manifest,commit,lock hash,restore log | любой restore/tool/config error → FAIL |
| Build | Core/Data/Linux/Web и pure tests в Release;Windows project исключён из Linux solution filter | build log,compiler/analyzer reports |0 compilation errors,security analyzer gate passed |
| Test | portable units + настоящая MySQL/Linux FS integration + HTTP/browser E2E | TRX/JUnit,coverage,скриншоты synthetic data |0 failed,mandatory suites не skipped;отдельный evidence Win suite |
| SAST | C# local rules/analyzers,web/API/auth/path/SQL risks | SARIF/JSON,tool/rule versions,severity mapping | High/Critical→nonzero/no deploy;scanner/parser error→FAIL |
| Dependency/image/config scan | NuGet vulnerability audit + image/Compose/secrets scan | JSON/SARIF,SBOM при выборе инструмента | High/Critical и leaked secrets блокируют |
| Package | multi-stage immutable image tagged commit,не latest-only | image digest,publish output,manifest | image соответствует проверенному commit,повторно не собирать другой код для deploy |
| Local Deploy | exclusive lock,backup если existing schema,DB healthy→migration job→web ready | deploy log,version/readiness snapshot | все предыдущие gates + migration success |
| Smoke/acceptance | HTTP login/demo upload/edit/save/ZIP/download/audit,grant-denied | smoke TRX/HTTP evidence без secrets | ошибка→failed deploy,сохранить state,без destructive rollback |

SAST до публикации/deploy;image scan после build image до deploy. Стадии package/image
scan можно переставить при сохранении gate. `needs`/`stages` с success-only переходами,
никакого continue-on-error для gates. Scanner output сохраняется даже при FAIL,но
«always» используется только для артефактов/cleanup test resources,не для deploy.

## 4. SAST и High/Critical

Предпочтительный простой локальный набор:Roslyn security analyzers + Semgrep CE C#
rules,проверенные на текущем синтаксисе C#/Razor. C# поддержка указана в
[Semgrep language documentation](https://docs.semgrep.dev/supported-languages).
До фиксации инструмента проверить именно CE feature coverage/лицензию выбранных rules,
а не приписывать бесплатному движку платную interfile taint analysis. При пробеле
составить targeted rules/ручной review gate,не назвать весь проект автоматически
проверенным. Возможный CodeQL C# — альтернативный вариант после проверки лицензии
и пригодности локального исполнения,не установленный обязательный компонент.

Строго различать:

- **SAST** анализирует исходники (unsafe XML,SQL interpolation misuse,path bypass,
  missing authorization/unsafe HTML);он нужен отдельно.
- SCA/NuGet audit и Trivy filesystem/image анализируют зависимости/образы/config;
  один Trivy vulnerability report не выполняет требование SAST.

Scanner rule severity не всегда HIGH/CRITICAL:Semgrep ERROR/WARNING/INFO не имеют
автоматического CVSS-эквивалента. Нужен versioned policy mapping ruleId→risk severity
(либо explicit metadata severity),для CVEs использовать severity/CVSS из выбранной
базы с зафиксированной policy. Неизвестное правило/сломанный report/пустой report при
scanner error должны блокировать,не превращаться в «0 уязвимостей».

Gate:parse reports→проверить schema/tool exit/coverage→посчитать High/Critical по
policy→exit nonzero при любом недопущенном finding. Для остальных levels report.
Allowlist только точечный rule/location/fingerprint с причиной,owner/expiry/review;
не глобальное suppress High/Critical. Scanner/rules/dependency DB фиксировать и
обновлять контролируемо. В local scan отключить upload/metrics/remote source отправку,
правила хранить локально;network только для явно разрешённых package/image/advisory
updates,не содержимого пользовательских файлов.

Ключевая acceptance:безвредная изолированная test fixture,заведомо дающая HIGH и CRITICAL,
должна остановить конвейер **до Local Deploy**;deploy marker/image version не меняется.
Отдельно scanner crash/invalid JSON/offline missing DB →FAIL. После исправления fixture
pipeline проходит;не добавлять намеренно уязвимый runtime-код в приложение ради демонстрации.
[Trivy filesystem flags](https://trivy.dev/docs/latest/references/configuration/cli/trivy_filesystem/)
служат основанием для отдельного severity/exit-code scan,не заменяют SAST gate.

## 5. Кеширование и воспроизводимость

NuGet cache key:OS+TFM+SDK+package lock hash;не кешировать local config/passwords или
результат авторизации. Locked restore,не скрывать изменённый lockfile. Docker BuildKit
cache для restore/build layers с разделением manifest/source;никаких secrets в ARG/
layers. Semgrep rules/tool versions и advisory DB cache проверять на наличие/допустимый
возраст;offline со старой базой явно degraded/FAIL по установленному SLA,не «свежий PASS».
Первый запуск требует разрешённых downloads;полностью offline run после прогрева
не приравнивается к обновлённой проверке CVE.

Имена artifacts:commit+runId,immutable results и checksum manifest,не overwrite старого
TRX как единственный evidence. Хранить локально build/test/SAST/SCA/image/deploy logs,
SARIF/JSON,coverage,version manifest,image digest,browser screenshots только synthetic
data. Никаких working Storage,SQL dumps,user hashes,upload payloads,secrets/cookies в
артефактах. Предлагаемый retention14 дней/последние10 успешных releases уточнить по
свободному месту VM;очистка только своих CI artifacts,никогда runtime files/state/DB.

act artifacts требуют отдельной local path/server настройки или host export;не считать
`actions/upload-artifact` автоматически сохранённым в GitHub. При GitLab artifacts:when
always и expire_in задаются явно,секретные логи всё равно не публикуются.

## 6. Секреты и границы runner/deploy

Тестовые MySQL credentials одноразовые в отдельной CI Compose project/network/volume,
без mount рабочих директорий. Runtime/migration credentials разделены;deploy secret
доступен только protected trusted branch/host step,не untrusted PR/build/test container.
Secret files0600 вне Git,logs masked,без `set -x`,password CLI arguments и compose config
dump с интерполяцией секретов. `.env` не vault и не artifact. Data Protection keys runtime
persistent и не попадают в image/cache/build context.

Docker control socket даёт широкие полномочия host:не монтировать его в web/тестовый
код с недоверенным source. Для локальной сборки/deploy host operator имеет Docker
permission;это явная доверенная граница. Предпочесть отдельную CI VM/OS account либо
rootless build и ограниченный host deploy wrapper. При одной учебной VM исполнять
только reviewed trusted checkout,исключить auto-deploy foreign PR. Rootless совместимость
bind mounts/UID/grants проверять отдельно,не объявлять её включённой автоматически.

Host wrapper обеспечивает flock единственного deploy,external timeout/kill tree,
проверку immutable commit/clean checkout,validation target project/volume paths.
No `docker compose down -v` или глобальный prune в cleanup. Cleanup удаляет только CI
resources с уникальным run label/ownership marker. Параллельные runs не меняют рабочий
deployment и чужой TEST namespace.

## 7. Local Deploy и откат

После всех gates образ доступен локальному Engine (local tag/digest или load OCI artifact),
registry не обязателен. Отдельные stable Compose project names для runtime и ci-run.
Runtime:localhost web port,private MySQL network,volume DB/files/state/keys,explicit
allowlisted host roots. Health dependency и migration job описаны в
[LINUX_FILESYSTEM_ACCESS](LINUX_FILESYSTEM_ACCESS.md).

Перед deploy существующей схемы:backup+проверяемый restore rehearsal policy,выключение
mutations/quiesce для согласованной FS/state/DB копии,DDL target/history review. Первый
fresh MySQL initial — только новая явная БД. Readiness включает schema version/mandatory
audit/root permissions,не только HTTP200. Smoke использует отдельного учебного пользователя
и каталог,не рабочие файлы Windows/Ubuntu. Наблюдение container unhealthy не запускает
слепые повторные migrations/удаление plans.

При ошибке вернуть предыдущий image только если схема backward compatible;иначе оставить
maintenance/fail closed и согласованный recovery. Никаких автоматических destructive Down
или перезаписи пользовательских данных ради зелёного pipeline. Artifacts сохраняют
точку ошибки и фактически установленную version.

## 8. Матрица доказательств требований

| Требование из поручения | Планируемое доказательство при WP11–12 |
|---|---|
| Ubuntu/VirtualBox/Docker/Compose/MySQL/Web | manifest actual versions,VM browser localhost screenshot,containers healthy,provider schema |
| Build | reproducible Release log + immutable commit/image digest |
| Test | mandatory real MySQL+Linux+web suites без пропусков,Windows parity evidence отдельно |
| SAST | local source report,C# coverage/rules versions,negative gate test |
| High/Critical halt | intentionally flagged isolated fixture→nonzero pipeline,deploy unchanged |
| Local Deploy | успешная установка image после gates,readiness+functional smoke |
| Cache | второй run с cache hit и неизменёнными tests/scans,lockfile change инвалидирует key |
| Artifacts | results доступны после FAIL и PASS,checksums/retention,без user data |
| Secrets | Git/image/cache/artifact scan + least privilege,не печатать реальные значения |
| Persistence | restart/recreate сохраняет demo files/DB/receipts/keys;backup restore rehearsal |

Приёмка пока NOT RUN. Наиболее простой рекомендованный вариант условен относительно
полного текста задания;этот документ не создаёт разрешения на WP11 или запуск служб.

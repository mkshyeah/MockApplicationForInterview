# AccountingHelper — Feature Backlog (Icebox / Deferred)

> Candidate features **F1–F12** from the 2026 differentiation review. The whole point of this
> file is that nothing gets lost: I expect to **promote several of these into real phases** later,
> so every candidate is captured here with its ranking, honest solo-cost, and — crucially — its
> **phase dependency** so future ordering is trivial.
>
> **Numbering note (read this):** the **F-labels (F1–F12) are a backlog scheme, completely
> independent of Phase numbers.** "F2 idempotency keys" is **not** "Phase 2 leave management";
> "F7 observability" is **not** "Phase 7 auth". Never conflate the two schemes.
>
> **Status of the top 3:** **F7 → Phase 10**, **F1 → Phase 11**, **F9 → Phase 12** are already
> promoted (see `mentorship_plan.md` + `phase_10/11/12_plan.md`). They stay listed here for the
> full picture; the other nine remain deferred.

---

## Ranking (resume signal vs solo cost)

Effort: **S** (~days), **M** (~1–2 wks), **L** (~3–4 wks), **XL** (cross-cutting, touches all).

| Id | Feature | Signal | Solo cost | Depends on / pairs | Verdict |
|----|---------|--------|-----------|--------------------|---------|
| **F7** | OTel end-to-end tracing | High | S–M | after Phases 3–5 (the async stack) | ⭐ **Phase 10** — best ROI |
| **F1** | Retro-pay / event sourcing + bitemporal | Very high | M–L | after Phase 4 (Payslip data) | ⭐ **Phase 11** — highest ceiling |
| **F9** | Crypto-shredding PII/GDPR | High | M | after Phase 7 (identity); dovetails Phase 9 (Key Vault) | ⭐ **Phase 12** — senior tell, self-contained |
| F5 | Multi-tenancy (payroll bureau) | Very high | XL | none hard — do early or accept big refactor; pairs F10 | Biggest signal *if* appetite for cost |
| F4 | Tamper-evident audit trail | High | M | Phase 2 (MediatR hook) + Phase 7 (who accessed) | Strong compliance story |
| F3 | ABAC + separation of duties | High | M | **after Phase 7** (RBAC first); gates Phase 4 run-commit | Pairs naturally above Phase 7 |
| F6 | Scheduled payroll (Hangfire/Quartz) | Med–high | S–M | after Phase 4 (the run it schedules) | Cheap operability win |
| F8 | Pre-commit anomaly/variance check | Med–high | M | after Phase 4 (run to check); pairs Phase 3 + 5 | The honest "AI" answer |
| F2 | Idempotency keys | Medium | S | Phase 4 endpoints (run + import) are the targets | Small, high-polish |
| F12 | Outbound webhooks (GL/bank) | Medium | M | after Phase 4 (run-commit is the trigger) | Good integration story |
| F10 | Per-tenant rate limiting | Low–med | S | **only with F5**; protects Phase 7 portal | Modest alone |
| F11 | Feature flags (tax-rule rollout) | Low | S | only with real effective-dated tax-rule versioning | Borderline — risk of tech-in-a-vacuum |

---

## Deferred candidates (F2–F6, F8, F10–F12)

> F1, F7, F9 are promoted — their full stubs live in `mentorship_plan.md` (Phases 11/10/12).
> Below are the nine still in the icebox.

### F2 — Idempotency Keys on Money-Moving Endpoints
- **One-line:** `Idempotency-Key` header on payroll-run / bulk-import commands so a retried or
  double-clicked request returns the original result instead of paying twice.
- **Depends on / pairs:** Phase 4 (its run + import endpoints are the targets). Distinct from
  Phase 4's *consumer* idempotency — this is the *HTTP edge* concern (Stripe-style).
- **Tech:** idempotency-key middleware + stored request-hash/response (inbox). New entity:
  `IdempotencyRecord`.
- **Google:** `idempotency key REST API`, `Stripe idempotency`, `inbox pattern`,
  `exactly-once semantics myth`.

### F3 — Payroll Approval Chain with Separation of Duties
- **One-line:** a run must be prepared → reviewed → approved by *different* people (four-eyes),
  managers approve only their department up to a value threshold.
- **Depends on / pairs:** **after Phase 7** (needs RBAC/identity first); acts as an approval gate
  before the Phase 4 run commits money.
- **Tech:** **ABAC / policy-as-code** (Cedar/OPA-style) — roles can't express "preparer ≠ approver"
  or "own dept ≤ $X". Distinct from Phase 7's RBAC. New entities: `ApprovalStep`, policy defs.
- **Google:** `ABAC vs RBAC`, `separation of duties four-eyes`, `Cedar policy language`,
  `OPA authorization .NET`.

### F4 — Tamper-Evident Compliance Audit Trail
- **One-line:** immutable log of every change to salary/PII **and** every sensitive *read*
  ("who viewed this payslip"), hash-chained so tampering is detectable.
- **Depends on / pairs:** Phase 2 (MediatR pipeline behaviour is the clean hook) + Phase 7 (needs
  identity to record *who*). Distinct from Phase 5 activity feed (UX) and F1 ledger (financial).
- **Tech:** append-only store + hash chaining (each entry carries prev-hash) via EF `SaveChanges`
  interceptor. New entity: `AuditEntry` (with `PrevHash`).
- **Google:** `audit log design`, `EF Core SaveChanges interceptor audit`,
  `hash chain tamper evident WORM`.

### F5 — Multi-Company Tenancy (Payroll Bureau)
- **One-line:** one deployment runs payroll for many client companies, each fully isolated; tenant
  resolved from user/subdomain.
- **Depends on / pairs:** no hard dependency, but **XL and cross-cutting — cheapest done early,
  before the data grows.** Pairs with F10 (per-tenant fairness).
- **Tech:** tenant middleware + EF global query filters + **Postgres Row-Level Security** as
  defence-in-depth. New entity: `Tenant`/`Company`; tenant-scoping on every aggregate.
- **Google:** `multi-tenancy patterns`, `EF Core global query filter tenant`,
  `Postgres row level security`, `tenant per schema vs shared`.

### F6 — Scheduled Recurring Payroll & Lifecycle Reminders
- **One-line:** auto-trigger the run on the last business day; monthly leave accruals;
  probation-/contract-/visa-expiry reminders — with catch-up if the server was down at fire time.
- **Depends on / pairs:** **after Phase 4** (the run it schedules). Distinct from Phase 4: that is
  *on-demand* message-driven work; this is *time-triggered recurring* work (missed-run catch-up,
  distributed lock so two instances don't double-fire).
- **Tech:** **Hangfire** (Postgres storage + dashboard) or **Quartz.NET**. New entity:
  `ScheduledJob`/`JobRun` (or Hangfire's own).
- **Google:** `Hangfire Postgres`, `Quartz.NET cron`, `missed job catch-up`,
  `distributed lock scheduled job`.

### F8 — Pre-Commit Payroll Anomaly / Variance Check
- **One-line:** before committing a run, flag outliers — net jumped 400%, negative net, someone on
  unpaid leave getting paid, duplicate bank account, headcount delta vs last month.
- **Depends on / pairs:** **after Phase 4** (a run to check); pairs with Phase 3 (variance vs prior
  runs) and Phase 5 (surface flags live).
- **Tech:** deterministic rules + statistical variance (z-score / prior-run delta). **The stats
  core is the substance;** an LLM explanation layer is optional garnish, and *only* worth it with a
  schema-validated output + a small eval harness. New entity: `RunVarianceReport`/`PayrollAnomaly`.
- **Google:** `payroll variance report`, `anomaly detection z-score`,
  `LLM structured output validation guardrails`, `human-in-the-loop approval`.

### F10 — Per-Tenant Rate Limiting & Quotas *(lower tier)*
- **One-line:** throttle expensive import/reporting endpoints and the public self-service portal;
  per-tenant fair-use quotas.
- **Depends on / pairs:** **only makes sense with F5** (per-tenant fairness); also protects the
  Phase 7 public portal. Modest signal alone (table stakes).
- **Tech:** built-in ASP.NET Core rate limiting (token-bucket/sliding-window, partitioned per
  tenant), `429 + Retry-After`. No new entity.
- **Google:** `ASP.NET Core rate limiting`, `token bucket sliding window`,
  `per-tenant partition 429 Retry-After`.

### F11 — Feature Flags for Tax-Rule Rollout *(lowest tier — borderline)*
- **One-line:** toggle new tax-year calculation rules by jurisdiction/effective date; dark-launch a
  new formula on one department first.
- **Depends on / pairs:** **only legitimate with a real effective-dated / jurisdictional tax-rule
  versioning need** — otherwise it's tech-in-a-vacuum on a solo app. Do not promote until that need
  is concrete.
- **Tech:** `Microsoft.FeatureManagement`. No new entity.
- **Google:** `Microsoft.FeatureManagement`, `feature flags canary kill switch`.

### F12 — Outbound Webhooks to GL / Bank / Pension Systems
- **One-line:** on run commit, reliably notify external systems (accounting GL, bank payment-file,
  pension provider) with signed payloads, retries, delivery status, and replay.
- **Depends on / pairs:** **after Phase 4** (run-commit is the trigger event). External counterpart
  to Phase 4's *internal* bus — hard parts are public-webhook concerns.
- **Tech:** HMAC-signed payloads, exponential backoff, dead-lettering, idempotent receivers. New
  entities: `WebhookSubscription`, `DeliveryAttempt`.
- **Google:** `outbound webhooks design`, `HMAC signature webhook`, `retry backoff dead letter`,
  `webhook idempotency`, `Svix`.

---

## Honest pushback (kept from the review, so the reasoning isn't lost)
- **AI/LLM is table stakes as a checkbox and a *negative* signal bolted on.** Only **F8's
  statistical core** earns its place; the LLM layer is optional and must be rigorously handled.
  NL-to-SQL over reporting is deliberately **not** proposed — correctness/eval burden too high.
- **F10 and F11 are the weakest** — promote only *behind* a feature that pulls them (F5 for
  fairness; genuine tax-rule versioning for flags). Don't lead with either.
- **Kubernetes** stays a reading-only stretch (already in Phase 8); **gRPC stays rejected** —
  nothing in F1–F12 resurrects it.

## Закрыто на ветке refactor/narrow-repositories (до Phase 3)

- ✅ «унифицировать возвращаемые коллекции репозиториев» — все интерфейсы отдают `IReadOnlyList`.
- ✅ «избавиться от IRepository» — узкие интерфейсы, `IRepository<T>` удалён.
- ✅ Репозитории на tracked-обновления — `ApplyStatusAsync`, `CloseAsync`, `ApplyDecisionAsync`,
  `DebitAsync`; ни одного `State = Modified` не осталось. `ApplyTo(model, entity)` не понадобился —
  узкие методы мутируют entity напрямую.
- ✅ «переопределить SaveChangesAsync и пробежаться по ChangeTracker.Entries()» — entity помечены
  `ITimestamped`, `UpdatedAt` проставляется в одной точке; ручные простановки из четырёх
  репозиториев удалены.
- ✅ Заглушка `AfterSaveBehavior.Ignore` для `CreatedAt` снята — корень (detached-обновления)
  устранён, полный прогон интеграционных тестов зелёный.
- ✅ `CHECK (remaining_days >= 0)` — в схеме с блока 4.4 (`ck_leave_balances_remaining_days`).

## Закрыто на ветке refactor/handlers-return-dto (до блока 3 Phase 3)

- ✅ «хендлеры возвращают response-DTO вместо доменных моделей» — все 15 хендлеров, что отдавали
  домен, переведены на `*Response`; в контроллерах не осталось ни одного `ToResponse()`.
  Триггер нашёлся там, где его не ждали: блок 3 кэширует **результат хендлера**, а доменные
  `Department`/`Position` тянут за собой `ICollection<Employee>` — в кэш уехало бы лишнее поле,
  а с любым будущим `Include` сериализация получила бы граф с циклами. Плюс дашборд уже возвращал
  DTO, так что откладывать дальше значило закрепить два стиля в одном слое.
  Внешний контракт не изменился: все 78 интеграционных тестов прошли без правок — менялись только
  юнит-тесты, где `BeSameAs(entity)` перестал иметь смысл (ответ больше не та же ссылка).

## Закрыто на ветке test/reset-async-state (после Phase 4)

- ✅ «асинхронное состояние переживает `ResetAsync`» — сработал собственный триггер записи: появились
  консьюмеры, меняющие состояние в тестах (`PayrollRunConsumer`, `ImportRunConsumer`). Воспроизведено:
  после сброса в `import-run` лежало сообщение для уже удалённого job, а строка outbox переживала
  сброс при недоступном брокере. Теперь `ResetAsync` чистит outbox (`DELETE` под `lock_timeout`, не
  `TRUNCATE`) и затем делает purge всех очередей через management API. Порядок и то, почему оба
  `DELETE` в одной транзакции, — в `CLAUDE.md`. Побочный эффект: интеграционный прогон 18 с вместо
  27–30. Вероятная причина, не проверенная отдельно: поздние воркеры больше не разбирают чужие
  сообщения, а каждое такое давало `NotFound` и ретраи с захардкоженными 5 с на уровне шины.

## Закрыто на ветке fix/messaging-settings (после Phase 4)

- ✅ «`QueryDelay` занижен глобально — прод молча наследует тестовую настройку». К моменту закрытия
  запись устарела наполовину: значение уже шло из конфигурации, и у API в базовом файле стояло 5 с.
  Но у Worker секции `RabbitMq` не было вовсе, и он работал на значении по умолчанию из кода — та
  самая тестовая секунда, только тихо. Попутно нашлась ловушка серьёзнее: `ConfigurationBinder`
  дописывает элементы массива к значению по умолчанию, и у API было 10 интервалов ретраев вместо 5.
  Значения по умолчанию у `RetrySettings`/`OutboxSettings` убраны, секция добавлена в Worker,
  `QueryDelay` проверяется вручную до построения контейнера; `MessagingSettingsTests` сверяет оба
  базовых файла.

## Остаётся в айсбоксе

F-xx. Судьба Pending-заявок в момент увольнения сотрудника.
▎ Причина: FireEmployee не трогает заявки. ApproveLeaveRequest теперь отказывает уволенному (422), поэтому его Pending-заявки
▎ остаются висеть. Выход из состояния есть — reject по ним работает, — но он ручной, и никто не обязан о них вспомнить.
▎ Варианты: автоотклонять все Pending в FireEmployeeCommandHandler (тем же SaveChanges, что и смена статуса);
▎ либо считать зависшие заявки нормой и просто отфильтровывать их в очереди менеджера по статусу сотрудника.
▎ Первое честнее по данным, второе дешевле. Решать вместе с кросс-сотрудничьим списком Pending — там это станет видно.
▎ Объём: S. Ветка FireEmployee + интеграционный тест «увольнение закрывает висящие заявки».
▎ Триггер: когда появится очередь Pending для менеджера.

F-xx. Налоговая шкала в `SalaryCalculator.CalculateTaxes` не маргинальная.
▎ Причина: ставка применяется ко всей сумме, а не к части сверх порога, поэтому на границе брекета
▎ налог скачком растёт: 600 000 → 60 000 (10%), 600 001 → 120 000 (20%). Больший оклад даёт на
▎ 60 тысяч меньше на руки — это дефект модели, а не особенность. Существующие тесты его не ловят:
▎ они проверяют значения внутри брекетов, а не поведение на границе.
▎ Почему не чинилось вместе с суммой разных SalaryType (блок 2 фазы 3): та правка устраняла
▎ арифметическую бессмыслицу (сложение часовых ставок с окладами), правильный ответ там ровно один.
▎ Здесь же нужно продуктовое решение — маргинальная шкала, сглаженная, другие пороги, — а не
▎ устранение ошибки. Смешивать их в одном блоке значило бы объяснять два разных «почему цифры поехали».
▎ Варианты: маргинальная шкала (стандарт для подоходного налога, ломает текущие ожидания в тестах);
▎ либо оставить пороговую и задокументировать как осознанное упрощение учебной модели.
▎ Объём: S. Одна функция + юнит-тесты **на границы брекетов** (600 000 / 600 001 / 800 000 / 800 001),
▎ их отсутствие и есть причина, по которой дефект дожил досюда. Меняет ответ `/v1/reporting/employees/{id}/taxes`.
▎ Триггер: дашборд фазы 3 покажет суммарную налоговую нагрузку — на графике ступенька станет видимой,
▎ и первый же смотрящий спросит, почему она там.

F-xx. `DateTime.UtcNow` вызывается напрямую — время остаётся скрытой зависимостью.
▎ Причина: `FireEmployeeCommandHandler:40`, `ChangeSalaryCommandHandler:47` и валидатор создания
▎ прогона (проверка «период не в будущем») читают часы прямо из статического свойства. Пока правила
▎ от времени не зависели, это было незаметно: тесты подставляли любые даты и проверяли арифметику.
▎ В блоке 3 фазы 4 появилось первое правило, **корректность которого определяется текущей датой**, —
▎ и тест на него приходится писать с заведомо далёким годом, то есть подгонять под реальные часы,
▎ а не задавать их. «Прогон за прошлый месяц разрешён, за следующий — нет» проверить честно нельзя.
▎ Варианты: `TimeProvider` (.NET 8) в DI, `FakeTimeProvider` из `Microsoft.Extensions.TimeProvider.Testing`
▎ в тестах — тогда «сейчас» становится аргументом, а граничные случаи (последний день месяца,
▎ смена года, UTC против локали) проверяются точками, а не приблизительно; либо оставить как есть
▎ и признать, что часть временных правил тестируется только косвенно.
▎ Объём: S–M. Правка механическая, но кросс-каттинг: затрагивает все места с `UtcNow`, включая
▎ проставление `StartedAt`/`CompletedAt` у прогона. Делать одной веткой целиком, иначе половина
▎ кода читает часы, половина принимает их аргументом — и это хуже обоих вариантов.
▎ Триггер: первый баг «на границе месяца» или первый тест, который приходится писать с датой
▎ из будущего, чтобы он не протух. Второе уже случилось в блоке 3 — так что триггер, по сути, взведён.

F-xx. Раскладка по папкам разъехалась: DTO, маппинги и вспомогательные типы лежат не там, где их ищут.
▎ Причина: Application организован вертикально — `Features/<Aggregate>/Commands|Queries/<UseCase>/`,
▎ где команда, хендлер и валидатор лежат рядом. Но всё, что эти хендлеры принимают и отдают, живёт
▎ горизонтально: `DTOs/Requests`, `DTOs/Responses` и `Mapping` — плоские папки на весь проект.
▎ Одна фича размазана по трём местам, и добавление эндпоинта означает править файлы в трёх папках,
▎ хотя логически это один срез. Плюс мелкий сбой раскладки поверх этого:
▎ `Exceptions/JsonPropertyName.cs` и `Exceptions/ProblemTypes.cs` — не исключения, а хелперы
▎ форматирования ProblemDetails, попавшие в папку по соседству с единственным потребителем.
▎ Был и второй — `ImportJobMapping.cs` лёг в `Infrastructure/Data/Configurations/` среди
▎ `IEntityTypeConfiguration`; его вернули в `Infrastructure/Mapping/` ещё на ветке импорта, но сам
▎ факт показывает, что раскладка расходится на ходу, а не когда-то давно.
▎ Варианты: довести вертикальные срезы до конца — `Features/<Aggregate>/…/<UseCase>/` держит и
▎ request/response этого use case, общие DTO (`PagedResponse`, `PaginationRequest`) уезжают в
▎ `Common`, маппинг агрегата — рядом со срезами агрегата; либо оставить горизонтально и просто
▎ навести порядок в границах текущей схемы — вынести хелперы ProblemDetails из `Exceptions/`.
▎ Первое честнее по связности, второе не трогает 100+ файлов.
▎ Чего делать нельзя ни в одном из вариантов — размывать два слоя маппинга (Infrastructure:
▎ entity ↔ domain, Application: domain ↔ DTO); они называются одинаково и путаются именно из-за этого.
▎ Объём: M в первом варианте (переезд файлов + namespace по всему Application, диффы огромные,
▎ поведение не меняется — значит ветка отдельная, ничего кроме перемещений), S во втором.
▎ Триггер: ближайшая фаза, добавляющая связку эндпоинтов целиком (импорт CSV — уже она).
▎ Первый шаг сделан в ветке импорта: `ImportSettings` лежит в `Features/Imports/`, а не рядом с
▎ `Caching/CacheSettings.cs` — вертикальный срез начат отсюда, остальное Application пока горизонтально.
▎ Делать до неё или после, но не вперемешку: переезд файлов внутри фичи-ветки прячет настоящий диff.

F-xx. Две одновременные доставки одного импорта портят его отчёт — защита есть только на совпавших строках ошибок.
▎ Причина: `RunImportCommandHandler` держит счётчики job в памяти на всё время прогона, а
▎ `ApplyProgressAsync` присваивает их, а не прибавляет. Вторая доставка того же сообщения, пока
▎ первая ещё работает, проходит быстрый путь (статус `Processing`, а не `Completed`) и идёт по файлу
▎ сама. Строки, которые первая уже закоммитила, вторая видит через `GetExistingEmailsAsync` и пишет
▎ в отчёт как «already exists» — индекс по email тут даже не срабатывает. Её `ApplyProgressAsync`
▎ затирает счётчики первой, потом первая затирает их обратно — побеждает тот, кто писал последним,
▎ а лишние строки ошибок остаются. Ветка `ImportErrorRow` в части 4 ловит гонку, только если обе
▎ доставки пишут ошибку в одну и ту же строку файла — на чистом файле её нет.
▎ Чего это не задевает: дублей сотрудников не будет — `ix_employees_email` держит при любой
▎ раскладке гонки, так что зарплата и численность корректны. Испорчен только отчёт импорта.
▎ Как доставка становится одновременной: потеря соединения воркера с брокером (heartbeat) —
▎ неподтверждённое сообщение возвращается в очередь, а хендлер в процессе продолжает работать;
▎ либо `consumer_timeout` RabbitMQ (по умолчанию 30 минут) на очень длинном импорте. Перезапуск
▎ упавшего воркера одновременности не даёт — первая доставка к тому моменту уже мертва.
▎ Чего НЕ хватит: просто объявить `xmin` токеном конкурентности на `import_jobs`, как на
▎ `leave_balances`. `ApplyProgressAsync` перечитывает строку перед каждым обновлением, то есть
▎ каждый раз берёт свежий `xmin` — токен поймает запись, вклинившуюся между чтением и сохранением
▎ внутри одного вызова, но не устаревшее состояние в памяти хендлера, которое тут и есть проблема.
▎ Варианты: (а) владение прогоном — при старте job получает `RunId` (Guid этой доставки), каждое
▎ обновление прогресса идёт `ExecuteUpdate ... WHERE id = @id AND run_id = @mine`, ноль строк —
▎ «джоб перехвачен», выйти без коммита батча; (б) нести версию в доменной модели и выставлять
▎ `OriginalValue` у `xmin` из неё, а не из свежего чтения — тогда проигравший получает
▎ `DbUpdateConcurrencyException` и его батч откатывается целиком. (а) явнее и проще тестируется,
▎ (б) не добавляет колонку. В обоих случаях нужна политика перехвата: кто владеет джобом, чья
▎ доставка мертва, — иначе вторая доставка после реального падения первой тоже не сможет продолжить.
▎ Объём: S–M. Новый метод репозитория (или версия в модели), ветка в `CommitBatchAsync`, тест на
▎ моках по образцу `RunImportCommandHandlerTests`.
▎ Триггер: импорты, идущие минуты, а не секунды, или вторая реплика воркера. Сейчас файл ограничен
▎ 5 МБ, батч — один `SaveChanges` на 500 строк, и прогон на тысячах строк укладывается в секунды:
▎ окно для одновременной доставки узкое, а цена — неверный отчёт, не деньги.
▎ Связано: контрольная точка из части 5 (пропуск `Imported + Failed` записей) сужает пересечение
▎ доставок, но не закрывает его — вторая доставка прочтёт счётчики один раз на старте.

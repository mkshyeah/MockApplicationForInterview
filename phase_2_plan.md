  
# Фаза 2 — CQRS / MediatR + Feature: Leave / Time-Off Management

> Первая фаза, где технологию тянет реальная фича (принцип «no tech in a vacuum»).
> Mentor mode: код пишу я (Максим); тут — направление, ловушки и «что гуглить», без готовых реализаций.
> **Регрессионная сеть — 59 тестов** (29 unit + 30 integration). После каждого блока гоняем её:
> зелёный прогон = behavior-preserving миграция. На этом и держится вся Фаза 2 —
> мы переносим уже покрытую логику, а не переписываем поведение.

---

## Зачем вообще MediatR здесь (рамка, чтобы не cargo-cult'ить)

CQRS/MediatR решает **не** «сделать код красивее». Он даёт **шов диспетчеризации** (`ISender.Send(command)`),
в котором сквозные заботы живут как **pipeline behaviors**, и который окупается на **2b** — многошаговом
leave-workflow (validate → balance check → state transition → audit). На простом `FireEmployee` (2a) выгода
почти нулевая, и это **нормально**: 2a нужен, чтобы освоить плумбинг на знакомой земле. Не жди от 2a «вау» —
жди «понял механику».

**Ключевой сдвиг относительно Фазы 1.** Валидацию ты уже централизовал — но в `ValidationFilter` (MVC-уровень,
работает только для HTTP). `IPipelineBehavior` оборачивает **любую** отправку команды: из контроллера, из
фонового джоба, из другого хендлера. Это та же централизация сквозной заботы, что в Фазе 1, но на слой глубже
и без привязки к HTTP. Фильтр был правильным промежуточным шагом; behavior — его финальная форма.

---

## Locked-in решения (не пересматривать на пустом месте)

- **MediatR v13+ Community, без `LicenseKey`.** Проект некоммерческий → подходим под free/community tier.
  Без ключа библиотека НЕ падает и НЕ режет функциональность — только пишет одно предупреждение о лицензии
  в лог при старте. Если warning мешает: либо зарегистрировать бесплатный ключ (`cfg.LicenseKey`), либо
  приглушить категорию логирования MediatR через Serilog `MinimumLevel.Override`. Пакет переехал под опеку
  Lucky Penny Software (context7 id: `/luckypennysoftware/mediatr`).
- **CQRS без event sourcing.** Только разделение command/query + один медиатор. Никаких event store,
  отдельных read-БД, проекций в отдельное хранилище.
- **IUnitOfWork / репозитории остаются.** CQRS не требует пилить read/write-модели физически. Query-сторона
  ПОТОМ сможет ходить в проекции мимо репозиториев, но не в этой фазе.
- **Контракт ошибок сохраняется.** Хендлеры кидают те же `NotFoundException` / `BusinessRuleException` /
  `ValidationException`; MediatR их не оборачивает → `GlobalExceptionHandler` ловит как раньше →
  тот же `ProblemDetails`. Интеграционные тесты — доказательство эквивалентности.
- **Git workflow как в Фазе 1:** ветка = один логический блок, имя по типу (`feat/`, `refactor/`),
  master релизопригоден, мержим только зелёное, короткоживущие ветки.

---

## Статус блоков

| Блок | Тема | Ветка | Статус |
|---|---|---|---|
| 1 (2a) | Подключить MediatR + мигрировать `FireEmployee` → command+handler | `feat/mediatr-fire-employee` | ✅ Готово |
| 2 (2a) | `ValidationBehavior` + мигрировать остальные write-эндпоинты, удалить `ValidationFilter` | `refactor/commands-validation-behavior` | ✅ Готово |
| 3 (2a) | Мигрировать read-эндпоинты → queries | `refactor/queries-to-mediatr` | ✅ Готово |
| 4 (2b) | Feature: Leave / Time-Off management (vertical slice) | `feat/leave-management` | ✅ Готово |

> 2a = блоки 1–3 (механика на знакомой земле). 2b = блок 4 (реальная фича, ради которой всё и затевается).

---

## Prerequisite reading (уложить в голову ДО кода)

1. **CQRS ≠ event sourcing** — `CQRS without event sourcing`.
2. **Vertical slice vs layered** — сейчас у тебя слоистая (Services). Command+Handler тяготеет к
   вертикальным слайсам (фича-папка: команда, хендлер, валидатор рядом). Реши осознанно, куда едешь к 2b.
3. **Почему behavior сильнее фильтра** — см. рамку выше. `IPipelineBehavior order`.
4. **`ISender` vs `IMediator`** — в контроллере нужен именно `ISender` (только `.Send`), не весь `IMediator`.

---

## Блок 1 (2a) — Подключить MediatR + мигрировать FireEmployee

### Почему именно FireEmployee первым
Самодостаточный use case с полным покрытием: unit-тест сервиса + интеграционные (fire + `WhenEmployeeFired_Returns422`).
Знакомая земля — учишь плумбинг там, где логика уже проверена.

### Направление
- Пакет `MediatR` v13.* в `AccountingHelper.Application` (там живут хендлеры).
- `FireEmployeeCommand(Guid Id) : IRequest<Employee>` + `FireEmployeeCommandHandler`.
  Тело хендлера — **буквально** нынешний `EmployeeService.FireEmployee`, перенесённый как есть.
- Регистрация в `ApplicationServiceCollectionExtensions`:
  `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly))` — хендлеры скейнятся автоматически (transient).
- Контроллер: `await _sender.Send(new FireEmployeeCommand(id), ct)` → маппинг `ToResponse()` остаётся в контроллере.

### Хорошие практики
- **Одно изменение за раз.** В блоке 1 хендлер возвращает домен `Employee`, контроллер маппит как сейчас.
  НЕ меняй заодно формат ответа/DTO — иначе не поймёшь, что нарушило эквивалентность.
- **Behavior-preserving.** Никакой новой логики. Просто перенос вызова за `ISender.Send`.

### Ловушки
- **`ValidationFilter` НЕ трогать в этом блоке** — он ещё обслуживает остальные эндпоинты.
- **Юнит-тест `FireEmployee` целится в сервис.** После переноса логики в хендлер — перецелить unit-тест
  на **хендлер** (новая единица тестирования). Гонять 59 тестов.
- **Warning про лицензию** при старте — ожидаем, не баг (см. locked-in). Приглушить при желании.
- Не тестировать, «что MediatR диспатчит» — это тест фреймворка (см. Testing Strategy в mentorship_plan).

### Что гуглить
`MediatR AddMediatR RegisterServicesFromAssembly`, `MediatR IRequest IRequestHandler`, `ISender vs IMediator`,
`unit testing MediatR handlers`, `MediatR license key net 8`.

---

## Блок 2 (2a) — ValidationBehavior + миграция write-эндпоинтов

### Цель
Перенести валидацию из `ValidationFilter` в `ValidationBehavior<TRequest,TResponse>` (pipeline) и
мигрировать оставшиеся write-эндпоинты (`CreateEmployee`, `ChangeSalary`, on/off-vacation) в команды.
Когда все write-пути станут командами — **удалить `ValidationFilter`**.

### Направление
- Открытый generic behavior: `cfg.AddOpenBehavior(typeof(ValidationBehavior<,>))`.
- Behavior резолвит `IEnumerable<IValidator<TRequest>>`, гоняет их, при ошибках кидает тот же
  `ValidationException` → контракт `ValidationProblemDetails` сохраняется.
- **Порядок behaviors = порядок регистрации.** Валидация должна идти ДО хендлера.

### Ловушки
- **Не удаляй `ValidationFilter`, пока хоть один write-эндпоинт не мигрирован** — иначе где-то валидация
  задвоится, где-то пропадёт. Удаление фильтра — последний шаг блока, только когда все команды на месте.
- `FireEmployeeCommand` (только `id`, без тела/валидатора) — behavior на нём не сработает и это правильно:
  behavior проявит себя на `CreateEmployee`/`ChangeSalary`. Не «баг, что не валидирует».
- Проверить, что двойной валидации нет в переходный момент (фильтр + behavior одновременно).

### Что гуглить
`MediatR IPipelineBehavior validation`, `FluentValidation MediatR behavior`, `IPipelineBehavior order`,
`MediatR AddOpenBehavior`.

---

## Блок 3 (2a) — Read-эндпоинты → queries

### Цель
`GetEmployees`, `GetEmployee`, reporting-эндпоинты → `IRequest<T>` queries + handlers.
Симметрия с командной стороной; контроллеры становятся совсем тонкими.

### Ловушки
- Query-хендлеры читают через существующие репозитории — **не** вводить read-модель/проекции ради проекций.
- `GetEmployees` уже отдаёт `(Items, TotalCount)` одним запросом (Фаза 1) — не регрессировать это при переносе.
- Пагинация уже детерминирована (`ThenBy(Id)`) — тест на «ничьих» должен остаться зелёным.

### Что гуглить
`CQRS query handler MediatR`, `MediatR IRequest query`, `thin controller MediatR`.

---

## Блок 4 (2b) — Feature: Leave / Time-Off Management

> Ветка: `feat/leave-management`. Это фича, ради которой затевался MediatR — первый настоящий
> многошаговый use case. Здесь behaviors/команды перестают быть плумбингом ради плумбинга.

### Суть фичи
Заменить примитивный on/off-vacation toggle реальным workflow: сотрудники **подают заявки на отпуск**
(тип, диапазон дат), менеджер **одобряет/отклоняет**, система ведёт **баланс отпусков** и списывает его
при одобрении.

### Scope-развилки — ЗАФИКСИРОВАНЫ (взяты рекомендации, не пересматривать без причины)
1. **Длительность = календарные дни** (`(EndDate - StartDate).Days + 1`). Рабочие дни/праздники —
   осознанно ВНЕ scope (тянет календарь, отдельная фича).
2. **Actor/auth НЕ вводим** (no tech in a vacuum). Approve/reject без реального актора; `DecidedBy` —
   опциональная заглушка (nullable). Auth придёт, когда его потянет реальная фича.
3. **Баланс = фиксированный entitlement на тип** (сид при создании сотрудника или лениво при первом
   запросе). Accrual (помесячное начисление) — ВНЕ scope.
4. **Overlap-проверка (пересечение дат заявок) — ВНЕ MVP блока.** Добавим как бизнес-правило потом,
   отдельной гранулой. В первый заход не тащим, чтобы не разъехаться по scope.

### Новые сущности (эскиз)
- `LeaveRequest`: `Id`, `EmployeeId`, `LeaveType`, `StartDate`, `EndDate`, `Status`,
  аудит — `RequestedAt`, `DecidedAt?`, `DecidedBy?`. FK-скаляр `EmployeeId` → `required Guid`;
  навигация `Employee?` — nullable (конвенции домена из CLAUDE.md).
- `LeaveBalance`: `Id`, `EmployeeId`, `LeaveType`, `RemainingDays` (или `Entitled`/`Used` — реши одно).
- `LeaveType` enum: напр. `Annual`, `Sick`, `Unpaid` — **без нулевого члена** (`= 1` и далее), чтобы
  `IsInEnum()` отсекал незаданное (та же ловушка, что с `SalaryType`).
- `LeaveStatus` enum: `Pending`, `Approved`, `Rejected` — тоже с 1.

### Workflow = state machine (а не булев флаг)
```
submit  → Pending
approve → Pending → Approved   (+ списать баланс на длительность заявки)
reject  → Pending → Rejected   (баланс НЕ трогаем)
```
Переход разрешён **только из `Pending`**. Approve/reject уже решённой заявки → `BusinessRuleException` (422).
Это осознанная проверка текущего состояния, а не `if (approved) return`.

### Порядок реализации ВНУТРИ блока (не начинать с approve!)
```
4.1 Домен: LeaveRequest, LeaveBalance, enums + EF-конфигурации + миграция (--environment local)
4.2 SubmitLeaveRequestCommand → Pending. Проводим сквозь весь стек
    (команда → валидатор → хендлер → контроллер → unit + integration). Знакомая механика на новой сущности.
4.3 GetLeaveRequests / GetLeaveBalance queries (симметрия read-стороны).
4.4 ApproveLeaveCommand — многошаговый: загрузить → проверить Pending → проверить баланс →
    Approved + списать баланс → SaveChanges. Здесь concurrency (см. ниже).
4.5 RejectLeaveCommand — простой переход Pending → Rejected.
```
**Почему такой порядок:** сначала домен + submit (разложить базу на знакомой механике), потом approve —
иначе утонешь в concurrency, не имея каркаса.

### Многошаговый хендлер (где окупается MediatR)
`ApproveLeaveCommandHandler`: validate (behavior) → загрузить заявку → состояние `Pending`? →
баланс хватает? → перевести в `Approved` → списать баланс → `SaveChangesAsync`. Первая реальная цепочка,
на которой pipeline behaviors оправданы.

### Скрытая сложность — double-spend баланса
Гонки: два одобрения одной заявки одновременно, либо параллельные одобрения разных заявок одного сотрудника
за пределы баланса. Решение — **optimistic concurrency**: concurrency-токен на `LeaveBalance`
(в Postgres удобно `xmin` как rowversion, `IsRowVersion()` в конфигурации). При конфликте
`DbUpdateConcurrencyException` → мапить в **409 Conflict** (`ConflictException` уже есть).
Это тот случай, где токен закрывает реальный баг, а не «для галочки».

### Транзакционная граница
`LeaveRequest.Status` и `LeaveBalance.RemainingDays` меняются **вместе** в approve → один
`SaveChangesAsync` через существующий UoW. Разъедутся (статус Approved, баланс не списан) — данные врут.

### Ловушки
- Не смешивать «F2 idempotency keys» из бэклога с этой фичей (F-метки ≠ номера фаз).
- Валидатор `SubmitLeaveRequestCommand`: `StartDate <= EndDate`, даты не в прошлом (реши правило),
  `LeaveType` через `IsInEnum()`. Длительность в валидаторе не проверяем против баланса — это бизнес-правило
  хендлера (валидатор не ходит в БД).
- Баланс списывается **только** при approve, **только** из `Pending`. Reject/повторный approve баланс не трогают.
- Concurrency-токен нужен на сущности, где реальный конфликт (`LeaveBalance`), — не «на всякий случай» везде.
- Тесты value-first: «approve списывает баланс на длительность», «двойной approve → 422/409, баланс списан один раз»,
  «approve при нехватке баланса → отказ, статус не меняется», «reject баланс не трогает». Concurrency —
  интеграционным тестом (две параллельные approve → одна 409).

### Что гуглить
`vertical slice architecture`, `approval workflow state machine`, `EF Core optimistic concurrency token`,
`npgsql xmin concurrency token IsRowVersion`, `DbUpdateConcurrencyException handling`,
`EF Core owned vs separate entity`, `CQRS command handler transaction`.

### Мелкий долг, замеченный по ходу (в backlog, НЕ в этом блоке)
`ISalaryRepository.GetHistoryAsync` возвращает `IEnumerable<Salary>`, а `GetFilteredAsync` — `(IReadOnlyList, int)`.
Несимметрично → `.ToList().AsReadOnly()` в `GetSalaryHistoryQueryHandler`. Унификация возвращаемых коллекций
репозиториев — отдельная F-гранула, не Leave.

---

## Порядок действий
```
1. Блок 1: подключить MediatR + FireEmployee → command/handler → 59 тестов
2. Блок 2: ValidationBehavior + миграция write-эндпоинтов → удалить ValidationFilter → тесты
3. Блок 3: read-эндпоинты → queries → тесты
4. Блок 4 (2b): Leave management как эталонный vertical slice → новые тесты + сеть
```

## Задел на Phase 3 (Redis)
Aggregates дашборда (payroll cost, headcount) дороги в пересчёте → cache-aside. Пока чистишь query-сторону
в блоке 3 — держи в голове, что эти запросы позже станут кандидатами на кэш.


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
| 2 (2a) | `ValidationBehavior` + мигрировать остальные write-эндпоинты, удалить `ValidationFilter` | `refactor/commands-validation-behavior` | ⬜ Не начат |
| 3 (2a) | Мигрировать read-эндпоинты → queries | `refactor/queries-to-mediatr` | ⬜ Не начат |
| 4 (2b) | Feature: Leave / Time-Off management (vertical slice) | `feat/leave-management` | ⬜ Не начат |

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

### Суть фичи
Заменить примитивный on/off-vacation toggle реальным workflow: сотрудники **подают заявки на отпуск**
(тип, диапазон дат), менеджеры **одобряют/отклоняют**, система ведёт **баланс отпусков**.

### Новые сущности (эскиз, детали — при заходе в блок)
- `LeaveRequest` (тип, даты, статус: Pending/Approved/Rejected, аудит кто/когда).
- `LeaveBalance` (остаток дней по сотруднику/типу).
- `LeaveType` enum.

### Почему тех именно здесь
Первый по-настоящему многошаговый use case: validate → balance check → state transition → audit.
Строим как **эталонный vertical slice** — здесь pipeline behaviors доказывают свою ценность на реальной сложности.

### Скрытая сложность
Конкурентные одобрения / double-spend баланса → **optimistic concurrency** (EF Core concurrency token).
Approval — это **state machine**, а не булев флаг: продумать разрешённые переходы.

### Ловушки
- Не смешивать «F2 idempotency keys» из бэклога с этой фичей (F-метки ≠ номера фаз).
- Баланс и заявка меняются вместе → транзакционная граница (UoW уже есть).
- Тесты value-first: проверять исходы workflow (одобрение списывает баланс, двойное одобрение не проходит),
  а не «хендлер вызвался».

### Что гуглить
`vertical slice architecture`, `approval workflow state machine`, `EF Core optimistic concurrency token`,
`EF Core rowversion xmin postgres`, `CQRS command handler transaction`.

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

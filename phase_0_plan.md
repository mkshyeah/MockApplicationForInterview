
# Фаза 0 — Foundation & Hygiene: план и статус

## Статус блоков

| Блок | Тема | Статус |
|---|---|---|
| 1 | Structured Logging (Serilog) | ✅ Реализован |
| 2 | DB Robustness + Health Checks | ✅ Реализован |
| 3 | Unit Tests | ✅ Реализован |
| 4 | Integration Tests | ✅ Реализован |

> **Фаза 0 завершена.** Все 4 блока готовы, тесты зелёные: **30 unit + 28 integration = 58**.
> Сборка без варнингов. JSON-опции берутся из DI приложения (`Json`), общий `POST` вынесен
> в хелпер `CreateEmployeeAsync` базового класса — долгов не осталось.

---

## Блок 1 — Serilog ✅

### Что реализовано
- Bootstrap logger в `Program.cs` — пишет в консоль до инициализации DI контейнера
- Two-phase setup: bootstrap logger → `AddSerilog()` с `ReadFrom.Configuration()` + `ReadFrom.Services()`
- `CorrelationIdMiddleware` — генерирует или читает `X-Correlation-Id` из заголовка запроса
- `CorrelationIdLoggingMiddleware` — пушит correlation ID в `LogContext` через `LogContext.PushProperty()`
- `ICorrelationIdAccessor` — scoped-сервис, хранит correlation ID на время запроса
- `UseSerilogRequestLogging()` — структурированное логирование каждого HTTP запроса
- `GlobalExceptionHandler` — читает correlation ID из accessor, включает в `ProblemDetails.Extensions`
- `appsettings.local.json` — полная конфигурация Serilog: WriteTo Console (ExpressionTemplate с CorrelationId) + WriteTo File (CompactJsonFormatter, 14 дней ротация), minimum level overrides

### Известные компромиссы
- `ICorrelationIdAccessor` лежит в `AccountingHelper.Domain.Interfaces` — это нарушение Clean Architecture (домен не должен знать про correlation ID), но необходимо потому что `CorrelationIdDelegatingHandler` в Infrastructure слое должен иметь доступ к интерфейсу, а Infrastructure ссылается только на Domain. Варианты решения в будущем: вынести handler в API слой или добавить отдельный проект `Abstractions`.
- `appsettings.json` не имеет `WriteTo` — Serilog настроен только через `appsettings.local.json`. В не-local окружениях логи пишет только bootstrap logger. Приемлемо для текущей стадии.

### Баги для исправления
- [x] `CorrelationIdDelegatingHandler.cs` — условие исправлено на `if (!string.IsNullOrEmpty(correlationId))` (заголовок добавляется когда ID есть).

---

## Блок 2 — DB Robustness + Health Checks ✅

### Что реализовано
- `WebApplicationExtensions.ApplyLocalMigrations()` — при старте в `local` окружении применяет EF миграции с retry-логикой (5 попыток, задержка 3 сек), структурированное логирование каждого шага
- `/health/live` endpoint с кастомным JSON ответом: статус, длительность, детали каждого чека
- `AddNpgSql` health check — проверяет сетевое соединение с PostgreSQL напрямую
- `AddDbContextCheck<ApplicationDbContext>` — проверяет соединение на уровне EF Core
- Теги `live` для фильтрации (задел под kubernetes liveness probe)
- Health check возвращает `503` при `Unhealthy`, `200` при `Healthy` и `Degraded`

### Известные компромиссы
- Auto-migrate только для `local` — в production auto-migrate на старте антипаттерн (race condition при нескольких репликах, нет возможности rollback). Для этого проекта текущий подход приемлем.

---

## Блок 3 — Unit Tests ✅

### Реализовано (30 тестов)
- **`Domain/Models/SalaryTest.cs`** — `CalculateTaxes()` и `ConvertTo()` покрыты полностью,
  включая все ветки `SalaryType` (Monthly/Hourly/Daily/**Weekly**) и границы брекетов.
  Остальные доменные модели (`Employee`, `Department`, `Position`) анемичны — тестов не
  требуют (по стратегии «no test in a vacuum»).
- **`Application/EmployeeServiceTests.cs`** — fire (active / already-fired / not-found),
  vacation (on: fired / already-on; off: not-on), create (email-exists / department-not-found /
  position-not-found).
- **`Application/SalaryServiceTests.cs`** — change (not-found / fired / amount≤0 / current
  exists → CloseAsync / no current → без CloseAsync). Verify не тавтологичен: проверяется
  `CloseAsync(oldSalary.Id)` и `Add(It.Is<Salary>(…))` по полям новой зарплаты.
- **`Application/ReportServiceTests.cs`** — status (not-found / found), salary-by-type
  (not-found), taxes (not-found / correct).

### Правила написания тестов в этом проекте
- Именование: `MethodName_WhenCondition_ShouldExpectedBehavior`
- Структура: Arrange / Act / Assert с комментариями
- Assertions: FluentAssertions (`result.Should().Be(...)`, `act.Should().ThrowAsync<T>()`)
- Моки: Moq (`Mock<T>`, `Setup().Returns()`, `Verify()`)
- Тесты на исключения: `await act.Should().ThrowAsync<NotFoundException>()`
- Не тестировать инфраструктуру (репозитории, БД) — только бизнес-логику

---

## Блок 4 — Integration Tests ✅

> Готово: Testcontainers + Respawn + `WebApplicationFactory`, реальные миграции, изоляция
> через Respawn (purist: чистая БД + seed в Arrange). **28 тестов**, все зелёные:
> health, CRUD + пагинация, lifecycle (fire/vacation, guard 422), смена зарплаты + история,
> reporting (count, сумма активных, налоги + границы брекетов, 404-контракты).
>
> **Тесты вскрыли 2 реальных прод-бага (оба починены):**
> 1. `EmployeeRepository.GetStatusAsync` возвращал `default(EmployeeStatus)` вместо `null`
>    для несуществующего сотрудника → 404 не отрабатывал. Фикс: проекция `(EmployeeStatus?)`.
> 2. Контроллер декларировал `409` на lifecycle-нарушениях, а сервис отдаёт `422`. Фикс:
>    `ProducesResponseType` → `422` (метаданные OpenAPI приведены к реальному поведению).

### Стратегия (современный де-факто стандарт): Testcontainers + Respawn
```
dotnet add package Testcontainers.PostgreSql
dotnet add package Respawn
```

**Почему не in-memory провайдер.** Он не работает с этим проектом:
- `gen_random_uuid()` как SQL-дефолт для ID — PostgreSQL-специфика
- `UseSnakeCaseNamingConvention()` — не поддерживается in-memory
- цель интеграционных тестов — проверять **реальное** поведение Npgsql, а не подделку

**Почему не docker-compose + `BEGIN…ROLLBACK` (отклонено).** Откат транзакцией через
`WebApplicationFactory` требует общего соединения, `DbCommandInterceptor` для энлиста каждого
запросового `DbContext` и `AutoTransactionBehavior.Never` — хрупкая обвязка, при которой
`DbContext` в тестах ведёт себя **не как в проде**. Testcontainers + Respawn убирает всё это.

### Как это работает
- **Testcontainers** поднимает реальный PostgreSQL в одноразовом Docker-контейнере.
  **Один контейнер на весь прогон** (не на каждый тест — старт дорогой, ~5-10 сек): шарим через
  xUnit `ICollectionFixture` / общий `IAsyncLifetime`-фикстур.
- **Реальные миграции** на старте: `Database.MigrateAsync()`, **не** `EnsureCreated()` — так
  тестируется и сам код миграций (snake_case, `gen_random_uuid()`, дефолты).
- **Respawn** между тестами делает `TRUNCATE` по всем таблицам с учётом FK. `DbContext`
  коммитит как обычно, как в проде — никаких interceptor'ов и ambient state.
  Reference-данные (`HasData` в миграциях) — решить: беречь через `TablesToIgnore` или сидить
  в каждом тесте.

### Что реализовать
1. `AccountingHelper.IntegrationTests`: пакеты `Testcontainers.PostgreSql` + `Respawn`
   (убрать любые следы старого подхода — shared connection, `AmbientTxInterceptor`, `TxHolder`).
2. `WebApplicationFactory<Program>` через `ConfigureTestServices`: подменить регистрацию
   `DbContextOptions` на строку подключения контейнера.
3. Общий фикстур (`ICollectionFixture`): старт контейнера → `MigrateAsync()` → построить
   `Respawner` (`DbAdapter.Postgres`); в базовом классе — `ResetAsync()` перед каждым тестом.
4. Базовые тесты:
   - `GET /health/live` — возвращает 200 и JSON с checks
   - `POST /v1/employees` + `GET /v1/employees/{id}` — создание и получение
   - `GET /v1/employees` — пагинация работает
   - `PUT /v1/employees/{id}/salaries` — смена зарплаты закрывает предыдущую

### Что гуглить
`Testcontainers PostgreSql xUnit IAsyncLifetime`,
`WebApplicationFactory ConfigureTestServices replace DbContext`,
`Respawn Postgres ICollectionFixture ResetAsync`,
`EF Core Database.MigrateAsync integration test`,
`Testcontainers WaitStrategy postgres ready`, `Respawn TablesToIgnore seed data`.

---

## Порядок дальнейших действий

Все блоки Фазы 0 закрыты. ✅ Следующее — **Фаза 1 (Code Quality & Consistency)**,
см. `phase_1_plan.md`. Остаточную косметику тестов (см. заметку под таблицей статусов)
можно закрыть попутно в начале Фазы 1.

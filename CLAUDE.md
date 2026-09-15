# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Start Postgres, Redis and RabbitMQ (credentials read from .env)
docker compose up -d

# Run the API (local environment loads appsettings.local.json with the connection string)
dotnet run --project AccountingHelper.API --environment local

# Run the Worker (its own appsettings.local.json, also git-ignored)
dotnet run --project AccountingHelper.Worker --environment local

# Build the solution
dotnet build AccountingHelper.sln

# Run unit tests
dotnet test AccountingHelper.UnitTests

# Run integration tests (spins Postgres, Redis and RabbitMQ containers; Docker must be running)
dotnet test AccountingHelper.IntegrationTests

# EF Core migrations (always pass --environment local so the connection string is available)
dotnet ef migrations add <Name> --project AccountingHelper.Infrastructure --startup-project AccountingHelper.API -- --environment local
dotnet ef database update --project AccountingHelper.Infrastructure --startup-project AccountingHelper.API -- --environment local
dotnet ef migrations remove --project AccountingHelper.Infrastructure --startup-project AccountingHelper.API -- --environment local
```

Swagger UI is available at `http://localhost:5228/swagger` when running in the `local` environment. The RabbitMQ management UI is at `http://localhost:15672` — the fastest way to see what exchanges, queues and bindings MassTransit actually declared.

## Architecture

Clean Architecture with five projects and two test projects:

| Project | Role |
|---|---|
| `AccountingHelper.Domain` | Domain models, enums, repository interfaces, `IUnitOfWork`, exceptions shared with Infrastructure |
| `AccountingHelper.Application` | Service interfaces/implementations, DTOs, FluentValidation validators, exception types, Application-layer mappings |
| `AccountingHelper.Infrastructure` | EF Core entities, `DbContext`, repository implementations, Infrastructure-layer mappings, migrations, MassTransit registration |
| `AccountingHelper.Contracts` | Message contracts. References nothing — not even MassTransit |
| `AccountingHelper.API` | Controllers, middleware (`GlobalExceptionHandler`, `CorrelationIdMiddleware`), `ServiceCollectionExtensions`, `Program.cs` |
| `AccountingHelper.Worker` | Generic Host (Worker SDK, no HTTP) running the consumers |
| `AccountingHelper.UnitTests` | xUnit + Moq unit tests for domain models and application services |
| `AccountingHelper.IntegrationTests` | xUnit + Testcontainers tests against a real Postgres, Redis and RabbitMQ |

Dependency direction: API → Application → Domain ← Infrastructure. Worker sits beside the API — same Application and Infrastructure, no Kestrel — so `AccountingHelper.API` is never referenced from it.

**The Worker is a separate process, not a `BackgroundService` inside the API.** Otherwise "does not block HTTP" stays a slogan: a bulk import running in the API process eats the same thread pool and memory that serves the dashboard. Separate processes also scale and deploy independently. Nothing in the Worker owns a loop — `AddMassTransit` registers the hosted service that opens the connection, subscribes, and keeps the process alive; consumers are invoked by the bus.

**Migrations belong to the API.** `ApplyLocalMigrations` stays in `Program.cs` of the API and must not be copied into the Worker: two hosts racing to migrate one database is a startup-order bug that only shows up under load.

## Key patterns

**Dual mapping layers.** There are two separate mapping layers that must not be confused:
- `AccountingHelper.Infrastructure/Mapping/` — converts between EF entities (`*Entity`) and domain models. Used only inside repositories.
- `AccountingHelper.Application/Mapping/` — converts between domain models and request/response DTOs. `ToResponse()` is called **inside handlers**; `ToCommand()`/`ToQuery()` in controllers.

**Handlers return DTOs, never domain models.** Every handler's `IRequest<T>` is a `*Response` (or a scalar), so controllers only dispatch and wrap — there is no `ToResponse()` anywhere under `AccountingHelper.API`. Two reasons: the response contract belongs to Application, and block 3 caches the handler result, where a domain model would drag its navigation collections (`Department.Employees`) into the cache and risk cycles once anything adds an `Include`.

**Repository + Unit of Work.** Services never touch `ApplicationDbContext` directly — they go through `IUnitOfWork` (`_unitOfWork.Employees`, `_unitOfWork.Salaries`, etc.). `SaveChangesAsync` is called on the UoW, not on individual repositories. `Add()` and `Update()` on repositories are synchronous; only reads and deletes are async.

**Custom exceptions.** `AccountingHelperException` (in `Application/Exceptions/`) carries an HTTP `StatusCode`. Subclasses:
- `NotFoundException` (404)
- `ConflictException` (409)
- `ValidationException` (400) — holds `Dictionary<string, string[]> Errors` for `ValidationProblemDetails`
- `BusinessRuleException` (422) — for domain rule violations (e.g., changing salary of a fired employee)

`GlobalExceptionHandler` in the API layer catches all of these and writes a `ProblemDetails` response with `correlationId` in the extensions. Do not use `ErrorOr` — it was removed.

**EF entity → domain model separation.** `ToEntity()` in Infrastructure mapping must only set scalar/FK properties (`DepartmentId`, `PositionId`, etc.) and never set navigation properties (`Department`, `Position`), to avoid EF re-inserting related entities.

**Domain model property rules:**
- FK scalar properties (`EmployeeId`, `DepartmentId`, `PositionId`) → `required Guid` in domain models (always mandatory)
- Navigation properties (`Employee?`, `Department?`, `Position?`) → nullable in domain models (may not be loaded; avoids circular references during mapping)
- `Salary.Id` → plain `Guid` (not `required`) because it is always assigned by the service before `Add()` (`salary.Id = Guid.NewGuid()`)
- Collection navigations (`ICollection<Salary>`) → `= []` (initialized, never null)

**EF entity property rules:**
- FK scalar properties → plain `Guid` (no `required`) — EF handles required-ness via `IsRequired()` in `IEntityTypeConfiguration`
- Navigation properties → `Entity?` (nullable) — EF handles loading
- String/enum props → `required string` / `required Enum` is fine

**Salary lifecycle.** An employee always has at most one active salary (where `EndDate == null`). Before creating a new salary, `ISalaryRepository.CloseAsync()` must be called to set `EndDate` on the current one. The same applies when firing an employee.

**Money is calculated in memory, counts in SQL.** `SalaryCalculator` (in `Domain/Services`) owns two pure functions of `(Amount, Type)`: converting a rate to a monthly figure and applying the tax scale. Everything monetary therefore folds in memory over `ActiveSalarySnapshot`, the projection `ISalaryRepository.GetActiveSnapshotsAsync` returns. Only what needs no domain function is aggregated in the database — headcount via `GROUP BY`. Pushing the conversion into SQL would duplicate the formula (and the duplicate would start lying at the first change) or sum incomparable values, an hourly rate added to a monthly wage. The line is "does this need domain logic", not "is this expensive".

**One population for every aggregate.** `EmployeeStatuses.Employed` (not `Fired`, which *includes* `OnVacation` — people on vacation are still paid) defines who counts, in one place. This is not redundant with `EndDate == null`: "firing closes the salary" is a handler convention, not a schema constraint, so an import or a manual fix can leave a fired employee with an open salary. It also keeps payroll and headcount over the same set of people, so the ratio between them stays meaningful.

**Money rounding.** Compute exact, round once when assembling the DTO: `Math.Round(value, 2, MidpointRounding.AwayFromZero)`. Passing the mode explicitly is deliberate — the .NET default is banker's rounding (`2.5 → 2`), which is surprising for money. Each field rounds from its own exact value, so averages are not expected to add back up to a total; do not reconcile them.

**Employee creation flow.** `CreateEmployeeRequest.ToModel()` creates an `Employee` with a draft `Salary` where `EmployeeId = Guid.Empty`. `EmployeeService.CreateEmployee()` then sets `employee.Id`, `salary.Id`, and `salary.EmployeeId` before calling `_unitOfWork.Employees.Add()`.

**Correlation ID.** `CorrelationIdMiddleware` reads `X-Correlation-Id` from the request header (or generates a new one) and stores it via scoped `ICorrelationIdAccessor`. The ID is echoed back in the response header and included in every `ProblemDetails` response under `extensions.correlationId`. Serilog picks it up via `CorrelationIdLoggingMiddleware`.

**Caching is fail-open.** The cache is a derivative of Postgres, never a source of truth, so an unreachable Redis must degrade the response, not fail it:
- `AddRedis` is registered with `failureStatus: HealthStatus.Degraded`, so `/health/live` still answers 200 when the cache is down and an orchestrator does not pull the instance out of rotation.
- The Redis connection string carries `abortConnect=false` (a failed first connect must not kill the multiplexer permanently) plus short `connectTimeout`/`syncTimeout` — with the 5 s defaults every request would pay that delay before falling back to the database, and a slow success is worse than a fast failure.
- A *missing* connection string is a different case and still throws at startup: that is a configuration error, not a runtime outage, and deploying without a cache silently is worse than failing loudly.
- Integration tests run in three environments, so every branch of both policies is executable. The main collection (`IntegrationFixture`, `"Integration"`) gets live Postgres, Redis and RabbitMQ from Testcontainers. `CacheFailOpenTests` runs against a live Postgres and an endpoint that cannot answer (`DeadRedisFixture`, `"Integration:DeadRedis"`, `RedisEndpoints.Dead`), which pins fail-open down; `BrokerFailFastTests` mirrors it with live Postgres and Redis and a dead broker (`DeadBrokerFixture`, `"Integration:DeadBroker"`, `RabbitMqEndpoints.Dead`). Each dead-dependency fixture keeps its own containers rather than sharing: sharing across collection fixtures needs statics, which is the coupling being avoided, and the surviving dependencies must be genuinely healthy or the assertion would prove the wrong thing. Collection parallelization is disabled in `AssemblyInfo.cs` — several collections mean several hosts and container sets, and xUnit would otherwise run them at once, with Respawn truncating the database under someone else's test.
- A dead broker does not stop a host from starting: MassTransit retries the connection in the background rather than throwing on first connect. `BrokerFailFastTests.GetHealthLive_WhenBrokerUnavailable_Returns200` is what proves it, the same way `abortConnect=false` was checked rather than assumed for Redis.

**`HybridCache` over Redis.** `AddStackExchangeRedisCache` registers the `IDistributedCache` that `HybridCache` uses as L2. Registration *order* does not matter — `DefaultHybridCache` resolves `IDistributedCache` from the provider when it is constructed, i.e. after the container is built, so both call sites only have to run before `Build()`. What matters is that `AddStackExchangeRedisCache` runs at all: without it the L2 field is simply `null` and `HybridCache` degrades to a single-level in-process cache — no exception, no warning, so the failure mode is silent loss of the second level, not a startup error. Verified behaviour worth knowing:
- `GetOrCreateAsync` swallows an unreachable L2 and falls through to the factory — no `try/catch` of your own is needed.
- L2 writes are fire-and-forget: the key appears in Redis *after* the response is returned. Assert on observable behaviour (a second call does not invoke the factory), never on Redis contents immediately after a request.
- The effective key is `InstanceName` + the key passed in (`accounting-helper:` + `reporting:dashboard:v1`). Do not repeat the prefix in the key catalogue. `InstanceName` comes from the base `appsettings.json`, so tests see the production naming scheme; `CacheInfrastructureTests.RedisInstanceName_ComesFromBaseAppSettings` guards that.
- L1 lives in the process and the `WebApplicationFactory` is one per run, so a cached value survives both Respawn and a Redis flush and leaks into the next test. `IntegrationFixture.ResetAsync` therefore walks `CacheKeys.All` and calls `HybridCache.RemoveAsync`, which drops the entry from both levels. This works only while keys are static — a parameterized key would need tag-based removal instead.
- Defaults are 5 min L2 / 1 min L1 expiration and a 1 MiB `MaximumPayloadBytes`. Per-entry TTLs belong in `HybridCacheEntryOptions` at the call site, not in global options.

**What is cached, and the staleness that buys.** Three query handlers wrap their former body in `GetOrCreateAsync`: the dashboard (`CacheKeys.ReportingDashboard`) and both reference lists. TTLs come from the `Cache` section of `appsettings.json` — a short one for the dashboard, an hour for lookups — bound with `ValidateOnStart`, so a non-positive value fails the boot rather than silently disabling the cache. The factory is a `static` lambda taking the handler as state, which keeps it from allocating a closure per request.

Consequence, and it is a decision rather than a defect: **the dashboard may lag behind the database by up to the configured TTL.** Changing a salary does not evict anything in this phase — invalidation is TTL-only, and phase 4 replaces it with event-driven eviction keyed off the same `CacheKeys` constants. `DashboardCacheTests.GetDashboard_WithinTtl_ServesStaleValue_UntilKeyIsRemoved` documents both halves of that: stale within the TTL, fresh once the key is removed. If shortening the TTL ever feels like the fix, that is the signal the feature wants phase 4, not a smaller number.

The old reporting endpoints (`/count`, `/salaries`, `/taxes`) are deliberately left uncached.

**Unit tests use `PassthroughHybridCache`.** Handlers now take `HybridCache` as a dependency, but a unit test has no business proving that a second call skips the factory — that is cache behaviour, covered against a real Redis in `DashboardCacheTests`. The fake in `UnitTests/Fakes` runs the factory every time, so unit tests keep testing the arithmetic and nothing else.

**The broker is fail-fast, and that is the opposite of the cache on purpose.** Redis holds a derivative of Postgres, so losing it degrades an answer. RabbitMQ holds messages that exist nowhere else, so losing it means work will be dropped — an API that cannot publish must stop taking traffic. Both policies are visible in one `/health/ready` body: `redis` `Degraded` at 200, `rabbitmq-bus` `Unhealthy` at 503. `BrokerFailFastTests` and `CacheFailOpenTests` are deliberate mirrors of each other; keep them that way.

**Liveness excludes the broker; readiness includes everything.** `/health/live` filters on the `live` tag, `/health/ready` on `ready`, and the bus check carries only `ready`. Liveness answers "is this process broken, restart me" — an external dependency must never trigger that, or an orchestrator starts killing healthy instances over someone else's outage. Readiness answers "can this instance take traffic". `Degraded` maps to 200 on both.

**The bus health check comes from MassTransit, not `AspNetCore.HealthChecks.Rabbitmq`.** Since its 8.x releases that package wants an `IConnection` registered in DI, which means opening a connection by hand — exactly what is avoided with Redis and the multiplexer. MassTransit's own check also watches the connection messages actually travel over, rather than a second one opened for the probe. Name and tags are set explicitly through `ConfigureHealthCheckOptions`; the content of `/health/ready` is a public contract, like queue names.

**Messaging registration lives in one place.** `AddMessagingServices` (Infrastructure) takes an optional `Action<IBusRegistrationConfigurator>`: the API calls it without consumers, the Worker with them. Inside, order matters and is not obvious — `SetKebabCaseEndpointNameFormatter()` and the consumer registrations must both run *before* `UsingRabbitMq`, because `ConfigureEndpoints(context)` builds queues from whatever is registered by the time it runs. Registering consumers after it yields a bus with no queues and no error. Queue names are kebab-case by explicit choice, not by default: they are infrastructure's public contract.

**Publish through `IPublishEndpoint`, never `IBus`.** `IPublishEndpoint` is scoped, and the transactional outbox in a later block hooks precisely that scoped publish. An `IBus` call goes around the outbox, so the outbox becomes decoration while still looking installed. This is the most expensive trap of the phase, which is why the rule predates the outbox itself.

**Consumers are transport adapters.** A consumer pulls data out of the message and dispatches a MediatR command; the logic lives in Application. Logic inside a class that can only be reached from RabbitMQ cannot be called any other way and cannot be unit-tested without a harness — the same reason controllers stay thin.

**Integration tests run consumers in their own harness-backed provider.** `MessagingRoundTripTests` builds a `ServiceCollection` with `AddMassTransitTestHarness` pointed at the shared RabbitMQ container, instead of reusing the API host's bus. The reason is mechanical: `AddMassTransit` throws when called twice, and `Program.cs` has already registered it without consumers, so a `WebApplicationFactory` cannot add them on top. What this does **not** cover: that the API host publishes correctly, and that the Worker host boots. The first arrives with the first real publish; the second needs its own smoke test.

**No `Sleep` in messaging tests.** Wait on a condition with a timeout — `harness.Consumed.Any<T>(...)` or polling a request until a predicate holds. A delay after publishing is a flaky test with extra steps.

**A payslip is a historical record, not a projection.** `Payslip` stores computed amounts — gross, tax, deductions, net — and never points at the salary they came from. Next month's raise must not rewrite last month's slip, so joining `Payslip` to `Salary` to "avoid duplication" would destroy exactly what the table is for. The same property is the foundation of the retro-pay work in the backlog: corrections are new rows, never edits to old ones. Its employee FK is `Restrict` for the same reason; the payroll-run FK is `Cascade`, because a payslip cannot exist without its run.

**Idempotency is two unique indexes, not two `if`s.** `(year, month)` on `payroll_runs` and `(payroll_run_id, employee_id)` on `payslips` are the whole guarantee. `ExistsForPeriodAsync` in the create handler and the `Status == Completed` check in the run handler are fast paths for the common case — both are check-then-act, and two concurrent callers pass them together. What makes the money safe is that the second insert cannot physically land.

That only works if the violation is translated rather than leaking: `UnitOfWork.SaveChangesAsync` catches `DbUpdateException` whose inner is `PostgresException { SqlState: "23505" }` and rethrows `UniqueConstraintViolationException`. That type lives in **Domain**, because Infrastructure and Application share nothing else — Npgsql must not become visible in Application, and Application's HTTP-shaped exceptions are not visible in Infrastructure.

**What the caller does with that violation differs by entry point, and the difference matters.** In `CreatePayrollRunCommandHandler` it becomes a `ConflictException` — the client raced and deserves a 409 instead of a 500. In `RunPayrollCommandHandler` it is **swallowed and the message acknowledged**: a violation there means a concurrent delivery already did the work, so throwing would retry and eventually dead-letter a run that succeeded. At-least-once delivery makes "already done" a success, not a failure.

**A bad row is skipped, not fatal.** An employee with no active salary — the hole phase 3 recorded, reachable whenever an import leaves an open salary on a fired employee or closes one without firing — increments `SkippedCount` and gets no payslip. The run still completes. A batch that dies on one row is a bad batch, and the counters are how the operator finds out.

**Payroll and the dashboard count the same people.** `EmployeeStatuses.Employed` gates `GetPayrollEmployeeSnapshotsAsync` exactly as it gates headcount. Different populations would make the two reports disagree, and the ratio between payroll and headcount is the thing that makes either of them worth reading.

**Payroll arithmetic is `SalaryCalculator`, twice removed from SQL.** `Convert(amount, type, Monthly)` gives gross, `CalculateTaxes(amount, type)` gives tax — the tax is computed from the *original* rate, not from the already-converted gross, because the scale knows about pay types. `Deductions` is a deliberate zero placeholder for this phase, and `Net` keeps the full `gross - tax - deductions` formula so it stays correct once something produces them. Every field rounds from its own exact value, so `Net` may differ from the printed `Gross - Tax` by a cent; that is the phase 3 rounding rule, not a defect.

**A consumer is a transport adapter, exactly like a controller.** `PayrollRunConsumer` resolves the id from the message and calls `ISender.Send` — nothing else. Logic in a class that can only be reached from RabbitMQ cannot be invoked any other way and cannot be unit-tested without a harness. Note what this implies about processes: `ISender.Send` is an in-process call, so `RunPayrollCommandHandler` — though it lives in the Application assembly that both hosts load — executes inside the **Worker**, while `CreatePayrollRunCommandHandler` executes inside the **API**. Assembly is not process; who calls `Send` decides where the work lands, and that is the whole point of the split.

**Integration tests for the run dispatch the command directly.** `PayrollRunExecutionTests` resolves `ISender` from the test host instead of standing up a consumer: the consumer is three lines, and that the broker delivers is already proven by `MessagingRoundTripTests`. The idempotency test rewinds the run's status to `Processing` before the second pass on purpose — without that it would return at the fast path, the unique index would never fire, and the test would pass while proving nothing about at-least-once.

**Queues are shared state, and are not cleaned between tests yet.** `IntegrationFixture.ResetAsync` truncates the database and drops cache keys, but does not purge queues. That is safe only because the single consumer merely logs, and because `MessagingRoundTripTests` filters on a unique `PingId` — a leftover message from an earlier test physically cannot be mistaken for this test's own. Both halves of that are load-bearing. The first consumer that changes state, or any relaxation of that filter, needs a purge in `ResetAsync`; a per-run vhost isolates run from run, not test from test, so it is insurance against a shared broker rather than a substitute for purging.

## Configuration

- Connection strings live in `appsettings.local.json` (git-ignored), one per host: `AccountingHelper.API/` and `AccountingHelper.Worker/`. The `local` environment is the only one that loads them, and the Worker needs its own copy — it cannot see the API's.
- Docker Compose credentials (`DB_USER`, `DB_PASSWORD`, `DB_NAME`, `DB_PORT`, `REDIS_PORT`, `REDIS_PASSWORD`, `RABBITMQ_USER`, `RABBITMQ_PASSWORD`, `RABBITMQ_PORT`, `RABBITMQ_MANAGEMENT_PORT`) come from a `.env` file in the project root (git-ignored). `.env.example` lists every key and must be updated whenever a new one is added.
- Broker settings live in the `RabbitMq` section (`Host`, `Port`, `VirtualHost`, `UserName`, `Password`) and bind to `RabbitMqSettings` in `Infrastructure/Messaging` with `ValidateOnStart`. Unlike `CacheSettings` this class stays in Infrastructure: it is transport configuration, and no handler has any business reading it.
- The database is **PostgreSQL 16** (`postgres:16-alpine`). The healthcheck uses `pg_isready`.
- Cache TTLs live in the `Cache` section of `appsettings.json` (`Dashboard`, `Lookups`) and bind to `CacheSettings` in `Application/Caching`, validated on start. The options class sits in Application, not API, so handlers do not depend on the web layer.
- `Microsoft.Extensions.Logging.Abstractions` is pinned to **9.0.19** in Application: `Caching.Hybrid` requires `>= 9.0.10`, and an explicit 8.x reference downgrades it into an `NU1605` error.
- EF Core is configured with `UseNpgsql` + `UseSnakeCaseNamingConvention()` (all column and table names are snake_case in the DB).
- `gen_random_uuid()` is used as the default value for `Id` columns in migrations — PostgreSQL-specific.
- Money columns carry `HasPrecision(18, 2)`. Without it Npgsql emits an unconstrained `numeric`, which works but leaves precision up to whatever the code happened to store — unacceptable for a payslip, which is a document.
- Detached domain models cannot be updated by assignment: `GetByIdAsync` returns a model mapped off the entity, so EF knows nothing about it. Repositories expose narrow tracked-update methods instead (`ApplyStatusAsync`, `CloseAsync`, `ApplyDecisionAsync`, `ApplyProgressAsync`); no `State = Modified` anywhere. Forgetting a field in one of those is silent — the write simply does not happen.
- `EnableRetryOnFailure` (5 retries, 5 s delay) handles transient startup connection errors.
- The cache is **Redis 7** (`redis:7-alpine`), no volume — cache contents are ephemeral by design. The password is applied through `command: redis-server --requirepass ...`; the official image ignores a `REDIS_PASSWORD` env var (that is a bitnami convention). Persistence is switched off explicitly (`--save "" --appendonly no`).
- The compose healthcheck is `redis-cli ping | grep -q PONG`, not a bare `redis-cli ping`: `redis-cli` exits 0 even when `AUTH` fails, so only checking the exit code would report healthy on a broken password. `REDISCLI_AUTH` supplies the password so it never appears in the healthcheck command.
- The broker is **RabbitMQ 3** with the management plugin (`rabbitmq:3-management-alpine`), no volume. Losing it on `docker compose down` costs nothing worth keeping: MassTransit re-declares the whole topology at startup, and messages left in a dev queue are exactly what should be discarded. Adding a volume later would require `hostname:` to be pinned as well — RabbitMQ stores data under `mnesia/rabbit@<hostname>`, and the container's hostname defaults to its id, so a volume without a fixed hostname silently starts from an empty directory on every recreate.
- The RabbitMQ healthcheck is `rabbitmq-diagnostics -q ping && rabbitmqctl authenticate_user ...`, not the ping alone: ping only proves the Erlang node answers and never touches AMQP credentials, so a broken `RABBITMQ_PASSWORD` would report healthy. The same lesson as `redis-cli ping`, in a different disguise. The password comes from the container's own env so it never appears in the compose command.
- **Package versions are central.** `Directory.Packages.props` at the root sets `ManagePackageVersionsCentrally`, and every `PackageReference` in a csproj carries an `Include` only — a `Version` next to it is an error, not an override. One version per package id is the point: before this, `Microsoft.EntityFrameworkCore` was 8.0.26 in Infrastructure and 8.0.28 in the tests, which is exactly the drift that produces an NU1605 nobody can explain. Transitive pinning stays off — pinning what packages depend on is a different decision from choosing what this solution references, and it hides upgrades rather than surfacing them. The NuGet cache key in CI hashes `Directory.Packages.props` too, or a version bump would restore from a stale cache.
- Health checks: `AddNpgSql` (direct connection probe, tagged `database/postgres/live/ready`), `AddDbContextCheck<ApplicationDbContext>` (EF Core probe, tagged `database/efcore/live/ready`, registered in `InfrastructureServiceCollectionExtensions`), `AddRedis` (tagged `cache/redis/live/ready`) and MassTransit's bus check, registered inside `AddMessagingServices` as `rabbitmq-bus` (tagged `broker/ready` only). `/health/live` filters on `live`, `/health/ready` on `ready`; both map `Degraded` to 200 and `Unhealthy` to 503.

## API routes

| Method | Route | Description |
|---|---|---|
| `GET` | `/v1/employees` | List employees with filters and pagination |
| `POST` | `/v1/employees` | Create employee (with initial salary) |
| `GET` | `/v1/employees/{id}` | Get single employee |
| `PATCH` | `/v1/employees/{id}/fire` | Fire employee (closes active salary) |
| `PATCH` | `/v1/employees/{id}/on-vacation` | Send employee on vacation |
| `PATCH` | `/v1/employees/{id}/off-vacation` | Return employee from vacation |
| `GET` | `/v1/employees/{employeeId}/salaries` | Get salary history |
| `PUT` | `/v1/employees/{employeeId}/salaries` | Change salary (closes current, opens new) |
| `GET` | `/v1/reporting/employees/count` | Total employee count |
| `GET` | `/v1/reporting/employees/{id}/status` | Employee status |
| `GET` | `/v1/reporting/salaries` | Sum of all active salaries |
| `GET` | `/v1/reporting/employees/{id}/salary?type={type}` | Employee salary converted to target type |
| `GET` | `/v1/reporting/employees/{id}/taxes` | Monthly tax for employee's active salary |
| `GET` | `/v1/reporting/dashboard` | Payroll, tax, headcount per department, average salary per grade |
| `GET` | `/v1/departments` | Department reference list (flat, no pagination) |
| `GET` | `/v1/positions` | Position reference list (flat, no pagination) |
| `POST` | `/v1/payroll-runs` | Request a payroll run — `202 Accepted` + `Location` |
| `GET` | `/v1/payroll-runs/{id}` | Run status, counters and timestamps |
| `GET` | `/v1/payroll-runs/{id}/payslips` | Payslips of a run (flat, no pagination) |

## API conventions

- All routes are versioned: `v{version:apiVersion}/...` with default version `1.0`.
- URLs are forced lowercase via `RouteOptions.LowercaseUrls`. Use kebab-case in route attribute strings (e.g. `on-vacation`, `off-vacation`) — the middleware only lowercases, it does not insert hyphens.
- Enums are serialized as strings via `JsonStringEnumConverter`.
- Pagination uses `offset`/`limit` (not `page`/`pageSize`) throughout all layers. Responses are wrapped in `PagedResponse<T>` with `total`, `returned`, `offset`, `limit`.
- Validation runs in a MediatR pipeline behaviour, not in the controllers — no action injects `IValidator<T>`. Validators are picked up by `AddValidatorsFromAssembly`, failures are thrown as `ValidationException` and rendered by `GlobalExceptionHandler` as `ValidationProblemDetails`. A rule spanning two fields (`RuleFor(x => x)`) needs an explicit `WithName`, or the error key becomes the command's type name.
- Work that outlives a request answers `202 Accepted` with a `Location` pointing at a status resource, never `201`: the row exists, the work does not. The client polls that resource until the status settles.

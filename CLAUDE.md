# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Start the database (reads DB_USER, DB_PASSWORD, DB_NAME, DB_PORT from .env)
docker compose up -d

# Run the API (local environment loads appsettings.local.json with the connection string)
dotnet run --project AccountingHelper.API --environment local

# Build the solution
dotnet build AccountingHelper.sln

# Run unit tests
dotnet test AccountingHelper.UnitTests

# EF Core migrations (always pass --environment local so the connection string is available)
dotnet ef migrations add <Name> --project AccountingHelper.Infrastructure --startup-project AccountingHelper.API -- --environment local
dotnet ef database update --project AccountingHelper.Infrastructure --startup-project AccountingHelper.API -- --environment local
dotnet ef migrations remove --project AccountingHelper.Infrastructure --startup-project AccountingHelper.API -- --environment local
```

Swagger UI is available at `http://localhost:5228/swagger` when running in the `local` environment.

## Architecture

Clean Architecture with four projects and one test project:

| Project | Role |
|---|---|
| `AccountingHelper.Domain` | Domain models, enums, repository interfaces, `IUnitOfWork` |
| `AccountingHelper.Application` | Service interfaces/implementations, DTOs, FluentValidation validators, exception types, Application-layer mappings |
| `AccountingHelper.Infrastructure` | EF Core entities, `DbContext`, repository implementations, Infrastructure-layer mappings, migrations |
| `AccountingHelper.API` | Controllers, middleware (`GlobalExceptionHandler`, `CorrelationIdMiddleware`), `ServiceCollectionExtensions`, `Program.cs` |
| `AccountingHelper.UnitTests` | xUnit + Moq unit tests for domain models and application services |

Dependency direction: API → Application → Domain ← Infrastructure.

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
- Integration tests run in two environments, and both branches of the policy are executable. The main collection (`IntegrationFixture`, `"Integration"`) gets a live Redis from Testcontainers — that is what the cache tests need. `CacheFailOpenTests` runs in its own collection (`DeadRedisFixture`, `"Integration:DeadRedis"`) against a live Postgres and an endpoint that cannot answer (`RedisEndpoints.Dead`), which pins the fail-open policy down. Collection parallelization is disabled in `AssemblyInfo.cs`: two collections mean two hosts and two container sets, and xUnit would otherwise run them at once, with Respawn truncating the database under someone else's test.

**`HybridCache` over Redis.** `AddStackExchangeRedisCache` (L2) must be registered before `AddHybridCache()` so the latter picks up the `IDistributedCache` as its second level. Verified behaviour worth knowing:
- `GetOrCreateAsync` swallows an unreachable L2 and falls through to the factory — no `try/catch` of your own is needed.
- L2 writes are fire-and-forget: the key appears in Redis *after* the response is returned. Assert on observable behaviour (a second call does not invoke the factory), never on Redis contents immediately after a request.
- The effective key is `InstanceName` + the key passed in (`accounting-helper:` + `reporting:dashboard:v1`). Do not repeat the prefix in the key catalogue. `InstanceName` comes from the base `appsettings.json`, so tests see the production naming scheme; `CacheInfrastructureTests.RedisInstanceName_ComesFromBaseAppSettings` guards that.
- L1 lives in the process and the `WebApplicationFactory` is one per run, so a cached value survives both Respawn and a Redis flush and leaks into the next test. `IntegrationFixture.ResetAsync` therefore walks `CacheKeys.All` and calls `HybridCache.RemoveAsync`, which drops the entry from both levels. This works only while keys are static — a parameterized key would need tag-based removal instead.
- Defaults are 5 min L2 / 1 min L1 expiration and a 1 MiB `MaximumPayloadBytes`. Per-entry TTLs belong in `HybridCacheEntryOptions` at the call site, not in global options.

## Configuration

- Connection string lives in `AccountingHelper.API/appsettings.local.json` (git-ignored). The `local` environment is the only one that loads this file.
- Docker Compose credentials (`DB_USER`, `DB_PASSWORD`, `DB_NAME`, `DB_PORT`, `REDIS_PORT`, `REDIS_PASSWORD`) come from a `.env` file in the project root (git-ignored). `.env.example` lists every key and must be updated whenever a new one is added.
- The database is **PostgreSQL 16** (`postgres:16-alpine`). The healthcheck uses `pg_isready`.
- EF Core is configured with `UseNpgsql` + `UseSnakeCaseNamingConvention()` (all column and table names are snake_case in the DB).
- `gen_random_uuid()` is used as the default value for `Id` columns in migrations — PostgreSQL-specific.
- `EnableRetryOnFailure` (5 retries, 5 s delay) handles transient startup connection errors.
- The cache is **Redis 7** (`redis:7-alpine`), no volume — cache contents are ephemeral by design. The password is applied through `command: redis-server --requirepass ...`; the official image ignores a `REDIS_PASSWORD` env var (that is a bitnami convention). Persistence is switched off explicitly (`--save "" --appendonly no`).
- The compose healthcheck is `redis-cli ping | grep -q PONG`, not a bare `redis-cli ping`: `redis-cli` exits 0 even when `AUTH` fails, so only checking the exit code would report healthy on a broken password. `REDISCLI_AUTH` supplies the password so it never appears in the healthcheck command.
- Health checks: `AddNpgSql` (direct connection probe, tagged `database/postgres/live`), `AddDbContextCheck<ApplicationDbContext>` (EF Core probe, tagged `database/efcore/live`, registered in `InfrastructureServiceCollectionExtensions`) and `AddRedis` (tagged `cache/redis/live`). `/health/live` filters on the `live` tag and maps `Degraded` to 200.

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

## API conventions

- All routes are versioned: `v{version:apiVersion}/...` with default version `1.0`.
- URLs are forced lowercase via `RouteOptions.LowercaseUrls`. Use kebab-case in route attribute strings (e.g. `on-vacation`, `off-vacation`) — the middleware only lowercases, it does not insert hyphens.
- Enums are serialized as strings via `JsonStringEnumConverter`.
- Pagination uses `offset`/`limit` (not `page`/`pageSize`) throughout all layers. Responses are wrapped in `PagedResponse<T>` with `total`, `returned`, `offset`, `limit`.
- Validation is done via FluentValidation injected with `[FromServices] IValidator<T>`. Failures are thrown as `ValidationException` and handled by `GlobalExceptionHandler` as `ValidationProblemDetails`.

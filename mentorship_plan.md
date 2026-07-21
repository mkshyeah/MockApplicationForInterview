# AccountingHelper — Mentorship Roadmap

> Persistent roadmap for evolving this educational HR/payroll back-office project.
> Mentor mode: **I (the user) write all code.** Claude reviews, gives architectural hints,
> and "what to google" keywords — it does **not** write code files unless explicitly asked
> for a template or snippet.
>
> Per-phase detail lives in `phase_N_plan.md` (e.g. `phase_0_plan.md`, `phase_1_plan.md`).

## Guiding principle (crucial)
**No technology is introduced in a vacuum.** Every tool (Redis, RabbitMQ, SignalR, GraphQL,
Auth) must be *pulled in by a new, logical business feature* — never bolted onto a blank app.
Each feature phase therefore delivers **(A) a new user-facing HR capability** AND **(B) the
technology that capability genuinely requires.** If a feature wouldn't realistically need the
tech, we pick a better feature. This applies to *test tooling* too (see Testing Strategy).

## Locked-in decisions
- **CQRS/MediatR:** ✅ Adopt in Phase 2 as the architectural seam (validation, caching,
  logging, event-publishing pipeline behaviors hook into it).
- **Starting point:** Phase 0 (Foundation) first. ✅ **done.**
- **Testing:** Test-driven throughout — every phase adds tests for the behavior it introduces.
  Full stack and rules under **Testing Strategy** below.
- **BDD / Gherkin / Reqnroll:** ❌ Deliberately rejected. Gherkin's value is a shared
  business-readable spec between devs and non-dev stakeholders; on a solo learning project
  there is no such audience, so it would be tech in a vacuum. Plain xUnit integration tests.
- **Integration-test isolation:** ✅ **Testcontainers + Respawn** (modern de-facto standard).
  Real Postgres in a throwaway container, real migrations, Respawn `TRUNCATE` between tests.
  ❌ Rejected: in-memory provider (not real Npgsql) and docker-compose + `BEGIN…ROLLBACK` +
  command-interceptor (fragile, makes tests behave unlike prod).

## Testing Strategy (locked in)

### Testing philosophy — "no test in a vacuum" (backend dev, not QA)
Tests exist to **protect behavior whose failure would be expensive**, not to chase coverage.
A test earns its place only if it answers *yes* to at least one question:
1. Would a silent change to this logic cause a real, noticeable bug in prod?
2. Would this test catch a regression during a future refactor? (esp. the Phase 2 MediatR move
   and Phase 4 extraction to consumers — that's what the safety net is *for*.)

**The tautology trap.** If a test mocks a repo, `Setup(...)` a return, then `Verify(...)` the
service called it with what you just set up, it proves only your own mock config. Avoid.
**Never test** framework/library behavior (that EF saves, Npgsql connects, MediatR dispatches).

**Pyramid on THIS project:**
- **Mock-based unit tests only where there is real, infra-independent logic:** tax brackets,
  salary conversion, temporal salary rule (close-old/open-new), lifecycle transitions.
- **All "wiring" (EF mapping, snake_case, enum serialization, validation firing, guard →
  correct 404/409/422 ProblemDetails, correlation id) is proven with integration tests** —
  real HTTP + real Postgres. That's where bugs at the seams actually live.

### Unit tests (`AccountingHelper.UnitTests`) — ✅ done (30 tests)
- **Stack:** xUnit + Moq + FluentAssertions, strict AAA.
- **Cover the decisions** a service makes (business-rule rejections, temporal salary logic);
  pure domain logic (tax, conversion) exhaustively via `[Theory]`. Guard clauses (404/409) are
  proven at the integration level instead — don't duplicate. Verify side effects only when the
  call *is* the behavior under test (`It.IsAny<T>()` matchers), never as tautology.

### Integration tests (`AccountingHelper.IntegrationTests`) — ✅ done (28 tests)
- **Stack (modern de-facto standard):** xUnit + `WebApplicationFactory<Program>` +
  **Testcontainers** (`Testcontainers.PostgreSql`) + **Respawn**.
- **One container per run** (shared via `ICollectionFixture`); **real migrations**
  (`Database.MigrateAsync()`, not `EnsureCreated()`); **Respawn `TRUNCATE`** between tests —
  `DbContext` commits exactly like prod. Isolation is purist: clean DB + seed reference data in
  each test's Arrange.
- The factory feeds the container connection string via config and runs in a dedicated
  (non-`local`) environment so the app doesn't auto-migrate; the fixture owns migration.
- *Google:* `Testcontainers PostgreSql xUnit IAsyncLifetime`,
  `WebApplicationFactory ConfigureTestServices connection string`, `Respawn Postgres ResetAsync`.

---

## What the project is (essence)
An **HR / payroll back office**. Domain today:
- **Employee lifecycle** state machine: hire → active → on/off vacation → fired.
- **Salary is temporal**: each `Salary` has `EffectiveDate` / `EndDate`; a raise *closes* the
  current record and *opens* a new one. Full history retained.
- **Department / Position** reference data.
- **Reporting**: headcount, company salary spend, salary-type conversion, tax-by-bracket.

### Architecture already in place (evolve, don't rewrite)
Clean Architecture (Domain / Application / Infrastructure / API); domain models separate from
EF entities with mapping both ways; Repository + Unit of Work; API versioning; `ProblemDetails`
+ `GlobalExceptionHandler`; FluentValidation; `AsNoTracking`; audit fields; EF retry-on-failure;
Serilog with correlation IDs; health checks (Npgsql + EF Core probes); Testcontainers + Respawn
integration test harness.

### Why this domain fits the target tech
A payroll back office naturally has: expensive aggregate reports (→ caching), heavy periodic
batch jobs like running payroll (→ messaging/background), events managers want to watch live
(→ real-time), richly nested employee data for a UI (→ GraphQL), and sensitive personal data
(→ auth). We will surface those needs as real features.

---

## Phases

> Phases 0–1 are **prerequisite foundations** (no new user feature — stated honestly).
> Phases 2–7 each pair a **Feature** with the **Tech** it justifies. Phases 8–9 are
> deployment — their "feature" is *running the system somewhere other than one laptop*, which
> becomes a real need exactly when phases 3–5 turn the app into a multi-service stack.
> Phases 10–12 are **differentiator features** promoted from the backlog (ids **F7 / F1 / F9**,
> see `backlog.md`) — depth over stack-breadth. The rest of the backlog (F2–F6, F8, F10–F12)
> waits there until a real need pulls it into a phase.
>
> **Numbering note:** backlog **F-labels are a separate scheme from Phase numbers.**
> "F2 idempotency keys" is *not* "Phase 2 leave management" — never conflate the two.

### Phase 0 — Foundation & Hygiene  ✅ DONE *(prerequisite, no new feature)*
See `phase_0_plan.md` for detail. **58 tests green (30 unit + 28 integration).**
- **Structured logging** — ✅ Serilog, correlation IDs, request logging, wired into
  `GlobalExceptionHandler`.
- **Startup robustness** — ✅ health checks (`AddNpgSql` + `AddDbContextCheck`), migrate-on-
  startup (`ApplyLocalMigrations`, retry).
- **Automated tests** — ✅ unit (domain + services) and integration (Testcontainers + Respawn).
  The integration tests surfaced **2 real prod bugs**, both fixed: `GetStatusAsync` returned
  `default(EmployeeStatus)` instead of `null` for a missing employee (404 broken); controller
  declared `409` where the service actually returns `422`.

### Phase 1 — Code Quality & Consistency  ← current *(prerequisite, no new feature)*
See `phase_1_plan.md`. Cleanup before MediatR:
- Collapse duplicated per-controller validation (3 spots) into a single action filter.
- Fix double DB round-trip in `GetEmployees` (list + count → one query).
- Remove redundant `newSalary <= 0` check in `ChangeSalary`; validator message consistency.
- *Google:* `ASP.NET Core IAsyncActionFilter validation`, `FluentValidation manual vs auto
  validation`, `EF Core pagination total count single query`, `keyset vs offset pagination`.

### Phase 2 — CQRS/MediatR  +  Feature: **Leave / Time-Off Management**
- **Prerequisite reading:** CQRS *without* event sourcing; vertical slice vs layered; why
  pipeline behaviors replace cross-cutting filters.
- **Step 2a (mechanics on known ground):** migrate one existing, fully-tested use case
  (e.g. fire-employee) to a MediatR command + handler with a validation behavior. Learn the
  plumbing where the logic is familiar.
- **Step 2b (the feature):** replace the on/off-vacation toggle with a real workflow — employees
  *submit leave requests* (type, date range), managers *approve/reject*, system tracks
  **leave balances**. New entities: `LeaveRequest`, `LeaveBalance`, leave-type enum.
- **Why the tech here:** first genuinely multi-step use case (validate → balance check → state
  transition → audit). Build it as the reference vertical slice so pipeline behaviors prove
  their value on real complexity.
- **Hidden complexity:** concurrent approvals / balance double-spend → optimistic concurrency.
- *Google:* `MediatR pipeline behavior`, `IPipelineBehavior order`, `vertical slice architecture`,
  `CQRS without event sourcing`, `approval workflow state machine`,
  `EF Core optimistic concurrency token`, `unit testing MediatR handlers`.

### Phase 3 — Redis  +  Feature: **Company Payroll Dashboard & Analytics**
- **Feature:** dashboard aggregating total monthly payroll cost, headcount by department,
  average salary by grade, total tax liability — plus reference-data lookups for Angular dropdowns.
- **Why the tech here:** aggregates are expensive to recompute and change infrequently →
  **Redis cache-aside**; reference lists are near-static → classic cache targets.
- **Invalidation is deliberately two-step:** this phase = **TTL-based only** (accept brief
  staleness, documented). Phase 4's domain events later upgrade it to **event-driven
  invalidation** — a planned refactor, not an oversight.
- *Google:* `IDistributedCache Redis`, `cache-aside pattern`, `output caching .NET 8`,
  `cache stampede`, `StackExchange.Redis ConnectionMultiplexer singleton`, `HybridCache .NET`.

### Phase 4 — RabbitMQ/MassTransit  +  Feature: **Monthly Payroll Run & Bulk Import**
- **Prerequisite reading:** queues vs pub/sub; delivery guarantees (why exactly-once is a myth);
  why consumers must be idempotent. Steepest conceptual jump — read first.
- **Feature:** "Run Payroll for month X" — a **payslip** per active employee
  (gross → taxes → deductions → net), recorded as a **PayrollRun** with status. Plus **bulk CSV
  import**. New entities: `PayrollRun`, `Payslip`.
- **Why the tech here:** payroll for hundreds of employees (and big CSVs) is heavy and must not
  block the HTTP request → publish a job, process in a **background consumer** via RabbitMQ.
  Firing/salary-change publish domain events (`EmployeeFired`, `SalaryChanged`). **Event backbone.**
- **Closes the loop with Phase 3:** subscribe to these events to evict/refresh dashboard cache —
  replacing the TTL stopgap with event-driven invalidation.
- *Google:* `MassTransit RabbitMQ`, `transactional outbox pattern`, `domain events EF Core`,
  `idempotent consumers`, `MassTransit retry redelivery error queue`, `MassTransit test harness`,
  `CsvHelper`.

### Phase 5 — SignalR  +  Feature: **Live Activity Feed & Payroll-Run Progress**
- **Feature:** (1) real-time **HR activity feed** ("John hired", "Jane's salary changed",
  "Leave approved") managers watch live; (2) **live progress bar** for the Phase 4 payroll run.
- **Why the tech here:** both are inherently push-based and ride on the Phase 4 events — polling
  would be wasteful and laggy → **SignalR** consuming RabbitMQ messages.
- **Prerequisite reading:** WebSockets vs SSE vs long polling — know what SignalR abstracts.
- *Google:* `SignalR hubs groups`, `IHubContext send from background service`,
  `SignalR Redis backplane`, `SignalR authentication`, `MassTransit consumer SignalR`.

### Phase 6 — GraphQL/HotChocolate  +  Feature: **Employee Directory & Reporting Explorer**
- **Feature:** flexible directory / ad-hoc reporting for the Angular frontend — query employees
  with nested department, position, salary history, payslips, leave, choosing exactly which
  fields, with arbitrary filtering/sorting. (By now the aggregate is 6+ entities deep — the data
  phases 2 & 4 created; that nesting is what makes REST genuinely painful.)
- **Why the tech here:** REST over-fetches the nested aggregate or explodes into endpoints/params.
  Flexible-query is the textbook case for **GraphQL projections + filtering**, alongside (not
  replacing) REST mutations.
- *Google:* `HotChocolate filtering sorting projections`, `DataLoader N+1`,
  `HotChocolate EF Core DbContext resolver injection`, `GraphQL cursor pagination connections`,
  `GraphQL vs REST when`.

### Phase 7 — Auth  +  Feature: **Roles & Employee Self-Service Portal**
- **Feature:** role-based access (**HR-admin / manager / employee**) + **self-service**: see
  *own* payslips, submit *own* leave, view *own* salary history.
- **Why the tech here:** payslips/salaries/leave are sensitive personal data; once employees log
  in to see their own records, **auth becomes a hard requirement**. "Own records" specifically
  forces **resource-based** authorization — roles alone can't express "this employee, their data
  only". Adds CORS + contract conventions for Angular.
- **Prerequisite reading:** authn vs authz; what a JWT contains (claims, signature, why it can't
  be revoked); where OAuth2/OIDC fit vs hand-rolled JWT.
- *Google:* `ASP.NET Core JWT roles policy`, `resource-based authorization IAuthorizationHandler`,
  `refresh token rotation`, `ASP.NET Core Identity vs custom JWT`, `Azure Entra ID B2C`,
  `CORS SignalR credentials`.

### Phase 8 — Containerization & Orchestration *(infra)*
**What pulls it in:** after phases 3–5 the dev stack is **PostgreSQL + Redis + RabbitMQ + API** —
"clone and F5" no longer works, so a one-command reproducible environment is now a real need.
- Grow `docker-compose` (already has PostgreSQL) into the full stack: add Redis + RabbitMQ;
  finish the multi-stage `Dockerfile` so the API itself runs containerized.
- **Kubernetes: stretch goal only.** Concepts (deployments, services, configmaps/secrets, probes)
  worth *reading* as prep for Phase 9's Container Apps, but full local k8s isn't justified by any
  feature — skip unless curious.
- *Google:* `multi-stage dotnet Dockerfile`, `.dockerignore dotnet`,
  `compose depends_on healthcheck`, `docker compose profiles`, `liveness readiness probes`,
  `kubernetes basics dotnet` *(reading only)*.

### Phase 9 — Azure Deployment *(infra)*  + small feature: payslip storage
- Azure Container Apps / App Service, **Azure Database for PostgreSQL** (the project is Postgres —
  not Azure SQL), **Azure Cache for Redis**, **Azure Service Bus** (managed RabbitMQ alternative —
  compare), **Azure Blob Storage** for generated payslip PDFs from Phase 4, Key Vault, GitHub
  Actions CI/CD.
- *Google:* `Azure Container Apps`, `Azure Database for PostgreSQL flexible server`,
  `Azure Cache for Redis`, `Azure Service Bus vs RabbitMQ`, `Azure Blob Storage dotnet`,
  `Azure managed identity Key Vault`, `GitHub Actions deploy Azure`.

---

> Phases 10–12 below are the **top-3 differentiators** promoted from `backlog.md`. They pick
> depth over breadth — the constraint that got gRPC rejected, applied in our favour. See
> `phase_10_plan.md` / `phase_11_plan.md` / `phase_12_plan.md` for per-block detail.

### Phase 10 — Observability  +  Feature: **End-to-End Distributed Tracing**  *(backlog id F7)*
- **Depends on Phases 3–5** (the async stack it traces: HTTP → RabbitMQ → consumer → Redis → DB).
  Could honestly be pulled earlier, right after Phase 5 — kept at 10 as a deliberate depth pass.
- **Feature:** follow one payroll run as a single trace across every hop; RED metrics per
  endpoint/consumer; logs correlated to spans.
- **Why the tech here:** once the stack is multi-service, "why was this payslip wrong/slow?" is
  undebuggable without cross-service tracing → **OpenTelemetry** with context propagation through
  MassTransit, exported to Jaeger/Tempo or the .NET Aspire dashboard.
- **Resume signal:** the most direct hit on the 2026 "can you operate what you build" screen.
- **New entities:** none.
- *Google:* `OpenTelemetry .NET`, `trace context propagation MassTransit`, `OTLP Jaeger Tempo`,
  `.NET Aspire dashboard`, `RED metrics`, `Serilog trace_id span_id enrichment`.

### Phase 11 — Event Sourcing  +  Feature: **Retroactive Pay & Corrections**  *(backlog id F1)*
- **Depends on Phase 4** (needs `Payslip` / `PayrollRun` data to correct).
- **Prerequisite reading:** event sourcing *vs* event-driven; projections/folds; **scope ES to the
  pay ledger only** (not the whole app — that would be the cosplay we reject); bitemporal
  (valid-time vs transaction-time).
- **Feature:** backdated raises and payroll-error corrections that never mutate a committed
  payslip — they post adjusting entries and recompute net owed.
- **Why the tech here:** the pay ledger *is* an immutable, append-only financial record with retro
  corrections — the one place in this domain that genuinely demands **event sourcing** (Marten on
  the existing Postgres). Bitemporal answers "as-of date X, as-known-on date Y".
- **Resume signal:** event sourcing for the *right* reason + rare bitemporal modelling + money
  correctness.
- **New entities:** pay-event stream, `LedgerEntry`, `PayAdjustment`.
- *Google:* `event sourcing vs event-driven`, `Marten event store Postgres projections`,
  `bitemporal valid time transaction time`, `retroactive payroll correction`, `append-only ledger`.

### Phase 12 — Field-Level Encryption  +  Feature: **PII Encryption & GDPR Crypto-Shredding**  *(backlog id F9)*
- **Depends on Phase 7** (needs identity to scope keys) **and dovetails Phase 9** (Key Vault as
  the key store).
- **Prerequisite reading:** envelope encryption (DEK/KEK); crypto-shredding; GDPR right-to-erasure
  *vs* payroll retention duty.
- **Feature:** bank details / national id / salary encrypted per-employee at the app layer; "right
  to erasure" honored by destroying that employee's data key — the ledger stays intact for
  tax/audit, the PII becomes unrecoverable.
- **Why the tech here:** two conflicting legal duties (erase on request vs retain payroll) resolve
  cleanly only with **crypto-shredding** — envelope encryption + per-subject data keys (EF value
  converters; keys in Key Vault).
- **Resume signal:** resolving a real compliance conflict with a senior-level technique — separates
  hard from CRUD.
- **New entities:** per-employee data-key registry.
- *Google:* `crypto shredding GDPR`, `envelope encryption DEK KEK`,
  `field level encryption EF Core value converter`, `right to erasure vs retention`,
  `Azure Key Vault data keys`.

---

## Feature ↔ Technology map (at a glance)
| Phase | New HR feature | Technology it justifies |
|------|----------------|--------------------------|
| 2 | Leave / Time-Off management workflow | CQRS + MediatR |
| 3 | Payroll dashboard & analytics | Redis caching (TTL first) |
| 4 | Monthly payroll run + bulk import | RabbitMQ / MassTransit (+ event-driven cache invalidation) |
| 5 | Live activity feed + run progress | SignalR |
| 6 | Employee directory & reporting explorer | GraphQL (HotChocolate) |
| 7 | Roles + employee self-service portal | Auth (JWT/roles/resource-based) + CORS |
| 8 | One-command reproducible multi-service dev stack | Docker Compose (full), Dockerfile |
| 9 | Payslip PDF storage + running in the cloud | Azure (Container Apps, Blob, Service Bus) |
| 10 *(F7)* | End-to-end distributed tracing | OpenTelemetry (+ MassTransit context propagation) |
| 11 *(F1)* | Retroactive pay & corrections | Event sourcing (Marten) + bitemporal modelling |
| 12 *(F9)* | PII encryption & GDPR crypto-shredding | Field-level envelope encryption + key destruction |

> Column tags `*(F7/F1/F9)*` are the backlog ids — a **separate** numbering scheme from the
> Phase column. Deferred candidates F2–F6, F8, F10–F12 live in `backlog.md`.

## How it all reinforces itself
**Phase 4's events are the hub.** They upgrade **Phase 3's cache** from TTL to event-driven
invalidation, drive **Phase 5's live feed/progress**, and the data they produce (payslips, leave,
salary history) is exactly what **Phase 6 (GraphQL)** exposes and **Phase 7 (self-service)**
secures. Foundations (0–2) come first so all of this is safe to build, and the service sprawl of
3–5 is what makes **Phase 8** necessary.

---

## New domain entities introduced along the way
- `LeaveRequest`, `LeaveBalance` (Phase 2)
- `PayrollRun`, `Payslip` (Phase 4)
- Activity/audit entries feeding the feed (Phase 5)
- User/role identities (Phase 7)
- Pay-event stream, `LedgerEntry`, `PayAdjustment` (Phase 11 / F1)
- Per-employee data-key registry (Phase 12 / F9)
- *(Phase 10 / F7 adds no entities — tracing/metrics only)*

---

## Progress log
- [x] Phase 0 — Foundation & Hygiene  ✅ done (58 tests: 30 unit + 28 integration; 2 prod bugs found & fixed)
  - [x] Serilog + correlation IDs + request logging
  - [x] Health checks (Npgsql + EF Core probes)
  - [x] Migrate-on-startup (`ApplyLocalMigrations`, retry)
  - [x] Unit tests (domain models + application services)
  - [x] Integration tests (WebApplicationFactory + Testcontainers + Respawn, real migrations)
- [ ] Phase 1 — Code Quality & Consistency  ← current (see `phase_1_plan.md`)
- [ ] Phase 2 — CQRS/MediatR + Leave Management (2a: migrate one endpoint → 2b: feature)
- [ ] Phase 3 — Redis + Payroll Dashboard
- [ ] Phase 4 — RabbitMQ + Payroll Run & Bulk Import
- [ ] Phase 5 — SignalR + Activity Feed & Run Progress
- [ ] Phase 6 — GraphQL + Directory & Reporting Explorer
- [ ] Phase 7 — Auth + Roles & Self-Service Portal
- [ ] Phase 8 — Containerization & Orchestration
- [ ] Phase 9 — Azure Deployment + Payslip Storage
- [ ] Phase 10 — Observability + End-to-End Distributed Tracing (backlog F7; see `phase_10_plan.md`)
- [ ] Phase 11 — Event Sourcing + Retroactive Pay & Corrections (backlog F1; see `phase_11_plan.md`)
- [ ] Phase 12 — Field-Level Encryption + GDPR Crypto-Shredding (backlog F9; see `phase_12_plan.md`)

> **Backlog:** all 12 candidate features (F1–F12) with ranking, solo-cost, and
> phase-dependency notes live in `backlog.md`. F7/F1/F9 above are the promoted top 3;
> the remaining nine wait there until a real need pulls them into a phase.

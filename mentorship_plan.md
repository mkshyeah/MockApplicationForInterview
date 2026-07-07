# AccountingHelper — Mentorship Roadmap

> Persistent roadmap for evolving this educational HR/payroll back-office project.
> Mentor mode: **I (the user) write all code.** Claude reviews, gives architectural hints,
> and "what to google" keywords — it does **not** write code files unless explicitly asked
> for a template or snippet.

## Guiding principle (crucial)
**No technology is introduced in a vacuum.** Every tool (Redis, RabbitMQ, SignalR, GraphQL,
Auth) must be *pulled in by a new, logical business feature* — never bolted onto a blank app.
Each feature phase therefore delivers **(A) a new user-facing HR capability** AND **(B) the
technology that capability genuinely requires.** If a feature wouldn't realistically need the
tech, we pick a better feature.

## Locked-in decisions
- **CQRS/MediatR:** ✅ Adopt in Phase 2 as the architectural seam (validation, caching,
  logging, event-publishing pipeline behaviors hook into it).
- **Starting point:** Phase 0 (Foundation) first.
- **Testing:** Test-driven throughout — every phase adds tests for the behavior it introduces.

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
+ `GlobalExceptionHandler`; FluentValidation; `AsNoTracking`; audit fields; EF retry-on-failure.

### Why this domain fits the target tech
A payroll back office naturally has: expensive aggregate reports (→ caching), heavy periodic
batch jobs like running payroll (→ messaging/background), events managers want to watch live
(→ real-time), richly nested employee data for a UI (→ GraphQL), and sensitive personal data
(→ auth). We will surface those needs as real features.

---

## Phases

> Phases 0–1 are **prerequisite foundations** (no new user feature — stated honestly).
> Phases 2–7 each pair a **Feature** with the **Tech** it justifies. Phases 8–9 are deployment.

### Phase 0 — Foundation & Hygiene  ← START HERE *(prerequisite, no new feature)*
Make the system observable and testable before adding anything.
- **Structured logging** — Serilog, correlation IDs, request logging; wire into services and
  `GlobalExceptionHandler`.
- **Automated tests** — currently **zero**; biggest gap. xUnit + FluentAssertions. Easy wins:
  `ReportService` tax brackets & salary conversion; then `EmployeeService` lifecycle rules.
- **Startup robustness** — migrate-on-startup / wait-for-DB (fixes the "DB not ready" bug),
  health checks (`/health`, DB probe).
- *Google:* `Serilog enrichers`, `xUnit Moq repository testing`,
  `WebApplicationFactory integration tests`, `AspNetCore HealthChecks EF Core`.

### Phase 1 — Code Quality & Consistency *(prerequisite, no new feature)*
- Collapse duplicated per-controller validation into a pipeline/filter.
- Fix double DB round-trip in `GetEmployees` (list + count).
- Validator consistency (mixed RU/EN); remove redundant `newSalary <= 0` check in `ChangeSalary`.
- *Google:* `FluentValidation IAsyncActionFilter`, `keyset vs offset pagination`.

### Phase 2 — CQRS/MediatR  +  Feature: **Leave / Time-Off Management**
- **Feature:** Replace the simplistic on/off-vacation toggle with a real workflow — employees
  *submit leave requests* (type, date range), managers *approve/reject*, and the system tracks
  **leave balances**. New entities: `LeaveRequest`, `LeaveBalance`, leave-type enum.
- **Why the tech here:** this is the first genuinely multi-step use case (validation → balance
  check → state transition → audit). Build it as the **reference vertical slice** in MediatR so
  pipeline behaviors (validation, logging) prove their value on real complexity.
- *Google:* `MediatR pipeline behavior`, `vertical slice architecture`,
  `CQRS without event sourcing`, `approval workflow state machine`.

### Phase 3 — Redis  +  Feature: **Company Payroll Dashboard & Analytics**
- **Feature:** A dashboard endpoint aggregating total monthly payroll cost, headcount by
  department, average salary by grade, total tax liability — plus reference-data lookups
  (departments/positions) for future Angular dropdowns.
- **Why the tech here:** these aggregates are expensive to recompute on every load and change
  infrequently → **Redis cache-aside**, with **invalidation driven by the events from Phase 4**.
  Reference lists are near-static → classic cache targets. The feature *creates* the caching need.
- *Google:* `IDistributedCache Redis`, `cache-aside pattern`, `output caching .NET 8`,
  `cache stampede`.

### Phase 4 — RabbitMQ/MassTransit  +  Feature: **Monthly Payroll Run & Bulk Import**
- **Feature:** "Run Payroll for month X" — generates a **payslip** per active employee
  (gross → taxes → deductions → net), records a **PayrollRun** with status. Plus **bulk employee
  import** from CSV. New entities: `PayrollRun`, `Payslip`.
- **Why the tech here:** running payroll for hundreds of employees (and parsing big CSVs) is
  heavy and must **not** block the HTTP request → publish a job, process in a **background
  consumer** via RabbitMQ. Firing/salary-change also publish domain events
  (`EmployeeFired`, `SalaryChanged`) that downstream phases consume. This is the **event backbone**.
- *Google:* `MassTransit RabbitMQ`, `transactional outbox pattern`, `domain events EF Core`,
  `idempotent consumers`, `CsvHelper`.

### Phase 5 — SignalR  +  Feature: **Live Activity Feed & Payroll-Run Progress**
- **Feature:** (1) A real-time **HR activity feed** — "John was hired", "Jane's salary changed",
  "Leave approved" — that managers watch live. (2) A **live progress bar** for the Phase 4
  payroll run ("Processing 213/500…", then "Completed").
- **Why the tech here:** both are inherently push-based and ride on the Phase 4 events —
  polling would be wasteful and laggy → **SignalR** consuming RabbitMQ messages. The feature
  is impossible to do well without real-time.
- *Google:* `SignalR hubs groups`, `SignalR Redis backplane`, `SignalR authentication`,
  `MassTransit consumer SignalR`.

### Phase 6 — GraphQL/HotChocolate  +  Feature: **Employee Directory & Reporting Explorer**
- **Feature:** A flexible directory / ad-hoc reporting surface for the Angular frontend —
  query employees with nested department, position, and salary history, choosing exactly which
  fields, with arbitrary filtering and sorting.
- **Why the tech here:** REST either over-fetches the whole nested aggregate or needs a
  combinatorial explosion of endpoints/params. The feature's *flexible-query* nature is the
  textbook case for **GraphQL projections + filtering**, alongside (not replacing) REST mutations.
- *Google:* `HotChocolate filtering sorting projections`, `DataLoader N+1`,
  `GraphQL vs REST when`.

### Phase 7 — Auth  +  Feature: **Roles & Employee Self-Service Portal**
- **Feature:** Role-based access — **HR-admin / manager / employee** — plus an **employee
  self-service** view: see *own* payslips, submit *own* leave requests, view *own* salary history.
- **Why the tech here:** payslips, salaries, and leave are sensitive personal data; once
  employees log in to see their own records, **authentication + authorization become a hard
  requirement**, not a nice-to-have. Adds CORS + contract conventions for Angular.
- *Google:* `ASP.NET Core JWT roles policy`, `resource-based authorization`,
  `Azure Entra ID B2C`, `CORS SignalR credentials`.

### Phase 8 — Containerization & Orchestration *(infra)*
Grow `docker-compose` (already has SQL) into the full stack now that real services exist.
- Add Redis + RabbitMQ to compose; multi-stage `Dockerfile`; **Kubernetes basics**
  (deployments, services, configmaps/secrets).
- *Google:* `multi-stage dotnet Dockerfile`, `compose depends_on healthcheck`,
  `kubernetes basics dotnet`.

### Phase 9 — Azure Deployment *(infra)*  + small feature: payslip storage
- Azure Container Apps / App Service, Azure SQL, **Azure Cache for Redis**, **Azure Service Bus**
  (managed RabbitMQ alternative — compare), **Azure Blob Storage** to persist generated payslip
  PDFs from Phase 4, Key Vault, GitHub Actions CI/CD.
- *Google:* `Azure Container Apps`, `Azure Cache for Redis`, `Azure Service Bus vs RabbitMQ`,
  `Azure Blob Storage dotnet`, `GitHub Actions deploy Azure`.

---

## Feature ↔ Technology map (at a glance)
| Phase | New HR feature | Technology it justifies |
|------|----------------|--------------------------|
| 2 | Leave / Time-Off management workflow | CQRS + MediatR |
| 3 | Payroll dashboard & analytics | Redis caching |
| 4 | Monthly payroll run + bulk import | RabbitMQ / MassTransit |
| 5 | Live activity feed + run progress | SignalR |
| 6 | Employee directory & reporting explorer | GraphQL (HotChocolate) |
| 7 | Roles + employee self-service portal | Auth (JWT/roles) + CORS |
| 9 | Payslip PDF storage | Azure Blob Storage |

## How it all reinforces itself
**Phase 4's events are the hub.** They invalidate **Phase 3's cache**, drive **Phase 5's live
feed/progress**, and the data they produce (payslips, leave, salary history) is exactly what
**Phase 6 (GraphQL)** exposes and **Phase 7 (self-service)** secures. Foundations (0–2) come
first so all of this is safe to build.

---

## New domain entities introduced along the way
- `LeaveRequest`, `LeaveBalance` (Phase 2)
- `PayrollRun`, `Payslip` (Phase 4)
- Activity/audit entries feeding the feed (Phase 5)
- User/role identities (Phase 7)

---

## Progress log
- [ ] Phase 0 — Foundation & Hygiene  ← current
- [ ] Phase 1 — Code Quality & Consistency
- [ ] Phase 2 — CQRS/MediatR + Leave Management
- [ ] Phase 3 — Redis + Payroll Dashboard
- [ ] Phase 4 — RabbitMQ + Payroll Run & Bulk Import
- [ ] Phase 5 — SignalR + Activity Feed & Run Progress
- [ ] Phase 6 — GraphQL + Directory & Reporting Explorer
- [ ] Phase 7 — Auth + Roles & Self-Service Portal
- [ ] Phase 8 — Containerization & Orchestration
- [ ] Phase 9 — Azure Deployment + Payslip Storage

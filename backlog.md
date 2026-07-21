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

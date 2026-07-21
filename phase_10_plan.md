
# Фаза 10 — Observability + End-to-End Distributed Tracing: план и статус

> **Backlog id: F7** (не путать со схемой Phase-номеров — F7 ≠ Phase 7/Auth).
> Feature-фаза: сквозная трассировка. Технология приходит потому, что после Фаз 3–5 стек стал
> многосервисным (HTTP → RabbitMQ → consumer → Redis → DB), и вопрос «почему payslip неверный/
> медленный?» без кросс-сервисной трассировки не отлаживается.
> **Зависимость:** нужны Фазы 3–5 (async-стек, который трассируем). Честно: можно было бы делать
> сразу после Фазы 5 — оставлено на 10 как осознанный проход «в глубину».
> Mentor mode: код пишу я сам; тут — хинты, ловушки и «что гуглить», без готовых реализаций.

---

## Git workflow (как в Фазе 1)
Ветка = одно логическое изменение, master всегда зелёный. Префиксы по типу: `feat/`, `chore/`.
Для Фазы 10, например: `chore/otel-tracing`, `chore/otel-masstransit-propagation`,
`chore/otel-metrics-dashboard`. Короткоживущие ветки, после каждого блока — прогон всех тестов.

---

## Prerequisite reading
- OpenTelemetry: три сигнала — traces / metrics / logs; что такое span, trace, context.
- **Context propagation** через границу процессов и через брокер (traceparent в заголовках
  сообщения RabbitMQ) — как один payroll run остаётся одним trace на всех хопах.
- RED-метрики (Rate / Errors / Duration) vs USE; что мониторить на HTTP-эндпоинте и на consumer.
- OTLP как транспорт; бэкенды: Jaeger / Tempo / **.NET Aspire dashboard** (локально проще всего).

---

## Статус блоков

| Блок | Тема | Статус |
|---|---|---|
| 1 | Инструментирование API (ASP.NET Core, HttpClient, EF Core, Npgsql) | ⬜ Не начат |
| 2 | Проброс trace-context через MassTransit/RabbitMQ | ⬜ Не начат |
| 3 | Инструментирование Redis (StackExchange.Redis) | ⬜ Не начат |
| 4 | Экспорт: OTLP → Jaeger/Tempo или Aspire dashboard + RED-метрики | ⬜ Не начат |
| 5 | Корреляция логов и трейсов (Serilog trace_id/span_id) | ⬜ Не начат |

---

## Блок 1 — Инструментирование API
- **Цель:** каждый HTTP-запрос = span; вложенные span'ы для EF-запросов и исходящих HTTP.
- **Как думать:** `AddOpenTelemetry().WithTracing(...)` + встроенные instrumentation-пакеты
  (`AspNetCore`, `HttpClient`, `EntityFrameworkCore`, `Npgsql`). Осознать, что почти всё —
  готовые инструментаторы, а не ручной код.
- **Ловушка:** не логировать PII в span-атрибутах (зарплата, банковские данные) — это утечка.
- *Google:* `OpenTelemetry .NET AspNetCore instrumentation`, `OTel EF Core Npgsql instrumentation`.

## Блок 2 — Проброс context через MassTransit
- **Цель:** один payroll run — один trace, продолжающийся в background consumer.
- **Как думать:** MassTransit умеет пробрасывать `traceparent` в заголовках сообщения; проверить,
  что span consumer'а — child от span'а, опубликовавшего сообщение.
- *Google:* `trace context propagation MassTransit`, `OpenTelemetry messaging span links`.

## Блок 3 — Redis
- **Цель:** видеть cache-hit/miss как span'ы в общем trace (связь с Фазой 3).
- *Google:* `OpenTelemetry StackExchange.Redis instrumentation`.

## Блок 4 — Экспорт + метрики
- **Цель:** локальный бэкенд, где видно waterfall трейса; RED-метрики на эндпоинты и consumer'ы.
- **Как думать:** OTLP-exporter → Jaeger/Tempo, либо `.NET Aspire dashboard` одной командой.
- *Google:* `OTLP exporter Jaeger`, `.NET Aspire dashboard standalone`, `RED metrics OpenTelemetry`.

## Блок 5 — Логи ↔ трейсы
- **Цель:** из лога прыгать в trace и обратно — связать с уже существующим correlation-id (Фаза 0).
- **Как думать:** Serilog enrichment `trace_id`/`span_id`; понять соотношение correlation-id
  (бизнес-сквозной) и trace-id (телеметрия) — оставить оба, не дублировать смысл.
- *Google:* `Serilog OpenTelemetry trace_id span_id enrichment`.

---

## Тесты (по стратегии «no test in a vacuum»)
Observability — инфраструктура; юнит-тестировать почти нечего (тестировать OTel = тестировать
библиотеку, запрещено стратегией). **Максимум один интеграционный тест:** опубликованное
сообщение несёт заголовок `traceparent` (доказывает, что проброс контекста настроен). Остальное —
проверяется глазами в дашборде. Честно зафиксировать: это фаза, лёгкая на автотесты по своей
природе, и это нормально.

## Порядок действий
```
1. Блок 1 (API instrumentation) → прогнать все тесты
2. Блок 2 (MassTransit propagation) → проверить единый trace в дашборде
3. Блоки 3–4 (Redis + экспорт/метрики)
4. Блок 5 (логи ↔ трейсы), связать с correlation-id
```

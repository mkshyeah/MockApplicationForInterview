
# Фаза 11 — Event Sourcing + Retroactive Pay & Corrections: план и статус

> **Backlog id: F1** (не путать со схемой Phase-номеров — F1 ≠ Phase 1/Code Quality).
> Feature-фаза: ретроактивные начисления и исправления зарплаты. Технология (event sourcing)
> приходит потому, что pay-ledger — это по своей природе неизменяемая append-only финансовая
> запись с ретро-корректировками: коммитнутый payslip нельзя менять, можно только выпустить
> корректирующую запись.
> **Зависимость:** нужна Фаза 4 (данные `Payslip`/`PayrollRun`, которые исправляем).
> Mentor mode: код пишу я сам; тут — хинты, ловушки и «что гуглить».

---

## Git workflow (как в Фазе 1)
Префиксы: `feat/retro-pay-ledger`, `feat/pay-adjustment`, `feat/bitemporal-query`.
Короткоживущие ветки, master зелёный, после каждого блока — прогон всех тестов.

---

## Prerequisite reading (ОБЯЗАТЕЛЬНО до кода — самая концептуально ёмкая фаза)
- **Event sourcing vs event-driven** — это РАЗНЫЕ вещи (Фаза 4 = event-driven/messaging;
  здесь = event-**sourced** состояние). Не смешивать.
- Текущее состояние = **fold/проекция** над потоком событий. Событие — факт в прошлом
  (`PaySlipIssued`, `PayAdjusted`), не команда.
- **Scope дисциплина:** event sourcing только для **pay-ledger**, НЕ для всего приложения.
  ES на Employee/Department был бы ровно тем «tech in a vacuum», который проект отвергает.
- **Bitemporal:** valid-time (когда изменение действует — effective date) vs transaction-time
  (когда мы это записали). Запрос «как выглядело на дату X, как было известно на дату Y».
- **Marten** как event store поверх уже имеющегося Postgres (не тащить отдельную БД).

---

## Статус блоков

| Блок | Тема | Статус |
|---|---|---|
| 1 | Reading + решение: Marten vs рукопашный ES; границы scope (только ledger) | ⬜ Не начат |
| 2 | Моделирование pay-ledger как потока событий; проекция «текущий net» | ⬜ Не начат |
| 3 | Ретро-корректировка: backdated raise / исправление ошибки | ⬜ Не начат |
| 4 | Bitemporal: valid-time vs transaction-time, запросы as-of/as-known | ⬜ Не начат |
| 5 | Money correctness: decimal, округление, сходимость корректировок | ⬜ Не начат |

---

## Блок 1 — Решение по инструменту и границам
- **Цель:** осознанно выбрать Marten (event store на Postgres) и **жёстко очертить scope** —
  событийный только pay-ledger; остальной домен остаётся классическим EF.
- **Ловушка:** соблазн «переписать всё на ES». Зафиксировать в доке, почему нет.
- *Google:* `Marten event store Postgres`, `event sourcing bounded context scope`.

## Блок 2 — Поток событий и проекция
- **Цель:** `PaySlipIssued`, `PayAdjusted`, … как события; «сколько сотруднику причитается» =
  fold над потоком (live projection / inline projection в Marten).
- **Ловушка:** проекция — производная, единственный источник истины это поток. Не «чинить»
  проекцию руками — чинить событиями.
- *Google:* `Marten projections inline vs async`, `event sourcing fold current state`.

## Блок 3 — Ретроактивная корректировка
- **Цель:** повышение, оформленное задним числом (effective = прошлая дата), и исправление
  ошибки прошлого месяца — через корректирующие события, **без мутации коммитнутого payslip**;
  пересчёт «доплатить/удержать».
- **Ловушка:** корректировка порождает новую сумму к выплате, а не переписывает историю.
- *Google:* `retroactive payroll correction`, `append-only ledger adjusting entry`.

## Блок 4 — Bitemporal
- **Цель:** отвечать на «зарплата на дату X, как было известно на дату Y» (valid vs transaction
  time). Это и есть редкий senior-сигнал фазы.
- *Google:* `bitemporal valid time transaction time`, `as-of query event sourcing`.

## Блок 5 — Money correctness
- **Цель:** `decimal` (никогда `double`), правила округления, распределение копеек, сходимость:
  сумма корректировок + исходные payslip'ы = фактически выплачено.
- *Google:* `decimal rounding payroll banker's rounding`, `penny allocation reconciliation`.

---

## Тесты (по стратегии «no test in a vacuum»)
**Богатая цель для юнит-тестов** — fold/проекция и пересчёт ретро-доплаты это чистая,
инфраструктурно-независимая логика:
- `[Theory]`-набор сценариев корректировок (backdated raise, overpayment, несколько правок
  подряд) → ожидаемый «net owed».
- Проекция из потока событий даёт корректное текущее состояние.
- Money: округление и сходимость на граничных суммах.
- **Один интеграционный тест** на round-trip через Marten (событие записалось и читается) —
  не тестировать сам Marten, только свою интеграцию.

## Порядок действий
```
1. Prerequisite reading + Блок 1 (инструмент/границы) — зафиксировать решение в доке
2. Блок 2 (поток + проекция) → юнит-тесты проекции
3. Блок 3 (ретро-корректировка) → [Theory]-сценарии
4. Блок 4 (bitemporal), Блок 5 (money) → тесты сходимости
```

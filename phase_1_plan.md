
# Фаза 1 — Code Quality & Consistency: план и статус

> Prerequisite-фаза: новой пользовательской фичи нет, чистим долги перед CQRS/MediatR (Phase 2).
> Mentor mode: код пишу я сам; тут — хинты, ловушки и «что гуглить», без готовых реализаций.
> **Регрессионная сеть уже есть** (58 тестов Phase 0) — после каждого рефактора гоняем её,
> зелёный прогон = рефактор ничего не сломал. Именно ради этого мы её и строили.

## Статус блоков

| Блок | Тема | Статус |
|---|---|---|
| 0 | Косметика тестов (долг из Phase 0) | ⬜ Не начат |
| 1 | Валидация: убрать дублирование из контроллеров | ⬜ Не начат |
| 2 | `GetEmployees`: убрать двойной round-trip | ⬜ Не начат |
| 3 | Чистка валидаторов и сервисов (redundant checks, сообщения) | ⬜ Не начат |
| 4 | Пагинация: offset → keyset *(reading only)* | ⬜ Не начат |

---

## Блок 0 — Косметика тестов (перенесено из Phase 0)

Мелкий долг, который дешевле закрыть до того, как тестов станет больше:
- **Дубль JSON-опций** в `IntegrationTestBase`: все тесты используют статик `Json`, а DI-based
  `JsonOptions` (+ его резолв в `InitializeAsync`, `using`-и `IOptions`/`Mvc.JsonOptions`) больше
  не вызывается. Оставить одно. Рекомендация: оставить `JsonOptions` (источник правды — реальные
  опции приложения), удалить `Json`; либо наоборот, но не оба.
- **Варнинг CS8604** в `SalaryChangeTests.cs:180` — `history.Select(...)` после `history!`.
  Погасить: `history!.Select(...)` или `history.Should().NotBeNull()` перед использованием.
- **Дублирование `POST v1/employees`** почти в каждом тесте — вынести в базу хелпер
  `CreateEmployeeAsync(...)` → `EmployeeResponse`. Arrange станет про суть кейса, а не про JSON.

---

## Блок 1 — Единый механизм валидации

### Проблема (по коду)
Ручная валидация повторяется в 3 местах: `EmployeesController` (создание — стр. 77, список —
стр. 34) и `SalaryController` (стр. 37). Везде один паттерн:
```
var validationResult = await validator.ValidateAsync(request);
if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);
```

### Цель
Один сквозной механизм, чтобы контроллеры не повторяли эту болванку.

### Как думать / варианты
- Кандидаты: кастомный **`IAsyncActionFilter`** (перехватывает аргументы экшена, валидирует,
  кидает `ValidationException`) — сейчас самый прямой путь; FluentValidation auto-validation —
  осознать, что старый `AddFluentValidationAutoValidation` частично deprecated, понять почему.
- **Не тащи MediatR ради этого** — он придёт в Phase 2, и там валидация естественно уедет в
  `ValidationBehavior` пайплайна. Сейчас фильтр — промежуточный шаг, не переусложняй.

### Ловушки
- **Сохрани контракт:** `ValidationException` → `ValidationProblemDetails` (400) через
  `GlobalExceptionHandler`. Интеграционный `CreateEmployee_WithInvalidBody_Returns400…`
  должен остаться зелёным — это твоя проверка, что рефактор эквивалентен.
- Как фильтр получает нужный `IValidator<T>` из DI для произвольного типа аргумента.

### Что гуглить
`ASP.NET Core IAsyncActionFilter validation`, `FluentValidation manual validation ASP.NET Core`,
`FluentValidation auto validation deprecated`, `resolve IValidator<T> dynamically DI`,
`MediatR ValidationBehavior` *(задел на Phase 2)*.

---

## Блок 2 — `GetEmployees`: один запрос вместо двух

### Проблема (по коду)
`EmployeesController.GetEmployees` (стр. 41-42) делает два обращения к БД:
`GetEmployees(...)` + `CountEmployees(...)` — отдельный SELECT и отдельный COUNT.

### Цель
Отдавать страницу и `total` за один проход к БД.

### Как думать
- Вариант: один метод репозитория, возвращающий `(items, total)`; total через оконную функцию
  `COUNT(*) OVER()` в том же запросе. Осознать trade-off: window-count vs два запроса —
  что лучше читается и что делает БД под капотом.
- Понять, во что EF Core транслирует `COUNT(*) OVER()` и когда это дешевле двух round-trip'ов.

### Ловушки
- Интеграционный `GetEmployees_WithOffsetAndLimit_ReturnsPagedSubset` уже проверяет
  `total/returned/offset/limit/items` — он и есть страховка эквивалентности.
- Не сломать фильтры (`EmployeeFilteredRequest`) — total должен считаться **после** фильтров,
  но **до** пагинации.

### Что гуглить
`EF Core pagination total count single query`, `SQL COUNT(*) OVER() window function`,
`EF Core one query items and total`.

---

## Блок 3 — Чистка валидаторов и сервисов

### Redundant-проверка в `ChangeSalary`
`SalaryService` (стр. 32-33) вручную кидает `ValidationException` при `newSalary <= 0`, хотя
`ChangeSalaryRequestValidator` уже требует `GreaterThan(0)` — а валидатор отрабатывает в
контроллере раньше сервиса.
- **Ловушка/решение:** если убираешь проверку из сервиса — удали и юнит-тест
  `ChangeSalary_WhenSalaryAmountBelowOrEqualZero_ShouldThrowValidationException`, иначе он
  проверяет удалённое поведение. Либо осознанно оставь как defense-in-depth (сервис публичный) —
  но тогда это не «redundant», а сознательный дубль; выбери одно и зафиксируй решение.

### Консистентность валидаторов
- Часть правил с кастомными `WithMessage`, часть — на дефолтных сообщениях FluentValidation.
  Выровнять: язык (сейчас EN) и покрытие сообщениями.
- Мелочи: `NotEmpty().GreaterThan(0)` на `decimal` — `NotEmpty` для value-type проверяет
  `!= default(0)`, что пересекается с `GreaterThan(0)`; `NotEmpty()` рядом с `IsInEnum()` на
  enum — понять, что реально проверяется (для `SalaryType` без нулевого значения `NotEmpty`
  отсекает `default`).

### Что гуглить
`FluentValidation custom error messages`, `FluentValidation IsInEnum NotEmpty enum`,
`FluentValidation decimal NotEmpty vs GreaterThan`.

---

## Блок 4 — Пагинация offset → keyset *(reading only)*

Сейчас offset/limit — норм для этого проекта. Keyset (cursor) пагинация выигрывает при больших
смещениях (`OFFSET 100000` заставляет БД пролистать всё), но у тебя таких объёмов нет.
По принципу «no tech in a vacuum» — **не внедрять без реальной потребности**, только прочитать
и понять, когда переключаться.

### Что гуглить
`keyset pagination vs offset`, `cursor pagination EF Core`, `why OFFSET is slow large tables`.

---

## Prerequisite reading перед Phase 2 (CQRS/MediatR)
Блоки 1-2 — хорошая разминка перед MediatR: фильтр валидации и «тонкий контроллер» ровно те
концепты, что в Phase 2 переедут в pipeline behaviors и команды/хендлеры. Пока чистишь
контроллеры — держи в голове, что дальше их логика уедет за `ISender.Send(command)`.

## Порядок действий
```
0. Косметика тестов (Блок 0) — быстро, разогрев
1. Вынести валидацию в фильтр (Блок 1) → прогнать 58 тестов
2. Схлопнуть round-trip в GetEmployees (Блок 2) → прогнать тесты
3. Убрать redundant-проверку + причесать валидаторы (Блок 3) → прогнать тесты
4. Прочитать про keyset (Блок 4) — без внедрения
```


# Фаза 12 — Field-Level Encryption + GDPR Crypto-Shredding: план и статус

> **Backlog id: F9** (не путать со схемой Phase-номеров — F9 ≠ Phase 9/Azure).
> Feature-фаза: пофайловое (field-level) шифрование PII и «право на забвение» через
> crypto-shredding. Технология приходит из **конфликта двух юридических требований**: GDPR
> требует удалить данные по запросу, а налоговое/аудиторское право — хранить расчётку годами.
> Чисто разрешается только крипто-шреддингом: шифруем PII ключом на субъекта, «удаление» =
> уничтожение ключа (ledger/аудит целы, PII невосстановим).
> **Зависимость:** нужна Фаза 7 (идентичность, чтобы привязать ключ к субъекту) и стыкуется с
> Фазой 9 (Key Vault как хранилище ключей).
> Mentor mode: код пишу я сам; тут — хинты, ловушки и «что гуглить».

---

## Git workflow (как в Фазе 1)
Префиксы: `feat/pii-field-encryption`, `feat/envelope-keys`, `feat/gdpr-crypto-shred`.
Короткоживущие ветки, master зелёный, после каждого блока — прогон всех тестов.

---

## Prerequisite reading
- **Envelope encryption:** DEK (data key, шифрует данные) обёрнут KEK (key-encrypting key).
  Почему не шифруют всё одним ключом.
- **Crypto-shredding:** «стереть» = уничтожить DEK субъекта; зашифрованные байты остаются, но
  расшифровать нечем. Как это разрешает конфликт erasure vs retention.
- GDPR right-to-erasure vs payroll retention duty — почему их нельзя удовлетворить обычным
  `DELETE`.
- Application-level шифрование vs шифрование диска (TDE) — почему диск-уровень тут недостаточен.

---

## Статус блоков

| Блок | Тема | Статус |
|---|---|---|
| 1 | Определить PII-поля и scope шифрования | ⬜ Не начат |
| 2 | Envelope encryption: DEK на сотрудника, обёрнутый KEK | ⬜ Не начат |
| 3 | Прозрачное шифрование через EF value converters | ⬜ Не начат |
| 4 | Хранилище ключей: local dev vs Key Vault (Фаза 9) | ⬜ Не начат |
| 5 | Erasure-эндпоинт: уничтожение DEK, ledger/аудит целы | ⬜ Не начат |

---

## Блок 1 — Scope PII
- **Цель:** решить, что именно PII (банковские реквизиты, национальный ID; зарплата — обсуждаемо).
  Не шифровать всё подряд — только чувствительное.
- *Google:* `PII classification payroll`, `what data is personal GDPR`.

## Блок 2 — Envelope encryption
- **Цель:** на каждого сотрудника — свой DEK; DEK хранится **в обёрнутом виде** (зашифрован KEK).
- **Ловушка:** ключ на субъекта — обязателен для crypto-shredding (один общий ключ = нельзя
  стереть одного, не сломав всех).
- *Google:* `envelope encryption DEK KEK`, `per-tenant per-subject data key`.

## Блок 3 — EF value converters
- **Цель:** шифрование/дешифрование прозрачно на границе EF (`ValueConverter`), домен не знает.
- **Ловушка:** зашифрованное поле нельзя фильтровать/сортировать в SQL — учесть в запросах
  (связь с Фазой 6 GraphQL: такие поля не идут в filtering/projection).
- *Google:* `field level encryption EF Core value converter`, `EF Core encrypted column querying`.

## Блок 4 — Хранилище ключей
- **Цель:** local dev — ключ из secret/config; прод — **Azure Key Vault** (стыковка с Фазой 9,
  managed identity).
- *Google:* `Azure Key Vault data encryption keys dotnet`, `Key Vault managed identity`.

## Блок 5 — Право на забвение
- **Цель:** endpoint «стереть сотрудника» = уничтожить его DEK. Финансовый ledger (Фаза 11) и
  аудит-трейл остаются нетронутыми — расчётные суммы для налоговой сохраняются, PII становится
  нечитаемым.
- **Ловушка:** это ключевой момент фазы — показать, что два конфликтующих требования удовлетворены
  одновременно. Зафиксировать в доке, почему это правильнее, чем `DELETE`.
- *Google:* `crypto shredding GDPR right to erasure`, `retention vs erasure payroll`.

---

## Тесты (по стратегии «no test in a vacuum»)
- **Юнит:** round-trip converter'а (зашифровал → расшифровал = исходное); после уничтожения DEK
  дешифрование падает/возвращает недоступность (доказывает, что shredding работает).
- **Интеграция:** POST erasure → PII-поля больше не читаются, но строки ledger/аудита на месте
  (доказывает разрешение конфликта). Не тестировать саму крипто-библиотеку — только свою обвязку.

## Порядок действий
```
1. Prerequisite reading + Блок 1 (scope PII)
2. Блоки 2–3 (envelope + value converters) → юнит round-trip
3. Блок 4 (key store: local → Key Vault)
4. Блок 5 (erasure) → интеграционный тест «PII стёрт, ledger цел»
```

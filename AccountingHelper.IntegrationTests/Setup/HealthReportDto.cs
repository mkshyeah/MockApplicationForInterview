namespace AccountingHelper.IntegrationTests.Setup;

// Форма ответа /health/live. Вынесена из тестового класса, потому что читают её два
// набора с разными фикстурами (живой и мёртвый Redis), и две копии разъехались бы.
internal sealed record HealthCheckEntry(string Name, string Status);

internal sealed record HealthReportDto(string Status, List<HealthCheckEntry> Checks);
